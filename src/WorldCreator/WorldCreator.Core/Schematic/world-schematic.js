/* Dependency-free schematic presentation. Coordinates are diagram units, not game-world placement. */
class WorldSchematic {
    static layout(world) {
        if (!world || world.SchemaVersion !== 1 || typeof world.Id !== 'string' || !Array.isArray(world.RemainingScopes) ||
            world.RemainingScopes.some(s => typeof s !== 'string') || !Array.isArray(world.Nodes) || !Array.isArray(world.Connections) ||
            world.Nodes.length === 0 || world.Nodes.length > 5000 || world.Connections.length > 20000)
            throw new Error('Schematic requires 1–5000 nodes and at most 20000 connections.');
        const nodes = new Map(), children = new Map();
        for (const node of world.Nodes) {
            if (!node || typeof node.Id !== 'string' || nodes.has(node.Id) || typeof node.Kind !== 'string')
                throw new Error('Invalid or duplicate schematic node.');
            nodes.set(node.Id, node); children.set(node.Id, []);
        }
        const roots = [];
        for (const node of world.Nodes) {
            if (node.ParentId == null) roots.push(node);
            else {
                if (!nodes.has(node.ParentId)) throw new Error('Schematic parent is missing.');
                children.get(node.ParentId).push(node);
            }
        }
        if (roots.length !== 1) throw new Error('Schematic needs one root.');
        const linkIds = new Set();
        for (const link of world.Connections) {
            if (!link || typeof link.Id !== 'string' || linkIds.has(link.Id) || !nodes.has(link.FromId) || !nodes.has(link.ToId))
                throw new Error('Invalid schematic connection.');
            linkIds.add(link.Id);
        }
        if (world.MapGeometry != null) {
            const map = world.MapGeometry, depths = new Map(), active = new Set();
            const visit = (node, depth) => {
                if (depth > 32 || active.has(node.Id)) throw new Error('Invalid spatial hierarchy.');
                active.add(node.Id); depths.set(node.Id, depth);
                children.get(node.Id).forEach(child => visit(child, depth + 1));
            };
            visit(roots[0], 0);
            if (depths.size !== nodes.size || map.Unit !== 'meters' || ![map.Width,map.Height].every(v => Number.isFinite(v) && v > 0) ||
                !Array.isArray(map.Boxes) || map.Boxes.length !== nodes.size || !Array.isArray(map.Roads) || map.Roads.length > 40000)
                throw new Error('Invalid engine map geometry.');
            const seen = new Set();
            const boxes = map.Boxes.map(b => {
                if (!b || !nodes.has(b.NodeId) || seen.has(b.NodeId) || ![b.X,b.Y,b.Width,b.Height].every(Number.isFinite) ||
                    b.Width <= 0 || b.Height <= 0 || b.X < -1e-6 || b.Y < -1e-6 || b.X+b.Width > map.Width+1e-6 || b.Y+b.Height > map.Height+1e-6)
                    throw new Error('Invalid engine map box.');
                seen.add(b.NodeId);
                return { id:b.NodeId,node:nodes.get(b.NodeId),x:b.X,y:b.Y,width:b.Width,height:b.Height,
                    depth:depths.get(b.NodeId),container:children.get(b.NodeId).length > 0 };
            });
            const corridors = map.Roads.map(r => {
                if (!r || ![r.X1,r.Y1,r.X2,r.Y2,r.Width].every(Number.isFinite) || r.Width <= 0 ||
                    r.X1 < 0 || r.Y1 < 0 || r.X2 < 0 || r.Y2 < 0 || r.X1 > map.Width || r.X2 > map.Width || r.Y1 > map.Height || r.Y2 > map.Height)
                    throw new Error('Invalid engine map road.');
                return { x1:r.X1,y1:r.Y1,x2:r.X2,y2:r.Y2,width:r.Width,owner:r.Id };
            });
            return {boxes,corridors,width:map.Width,height:map.Height,world,coordinateSpace:'engine-world-meters'};
        }
        const visited = new Set(), sizes = new Map();
        const hint = node => node.Properties?.schematic ?? {};
        const number = (value, fallback, minimum, maximum) => {
            const result = value ?? fallback;
            if (!Number.isFinite(result) || result < minimum || result > maximum) throw new Error('Invalid schematic dimensions.');
            return result;
        };
        const measure = (node, depth) => {
            if (depth > 32 || visited.has(node.Id)) throw new Error('Schematic hierarchy is cyclic or too deep.');
            visited.add(node.Id);
            const h = hint(node), nested = children.get(node.Id);
            const padding = number(h.padding, depth === 0 ? 40 : 18, 8, 160);
            const gap = number(h.gap, depth === 0 ? 56 : 26, 8, 160);
            const header = number(h.header, depth === 0 ? 44 : 36, 24, 100);
            const columns = number(h.columns, Math.ceil(Math.sqrt(nested.length || 1)), 1, 100);
            if (!Number.isInteger(columns)) throw new Error('Schematic columns must be whole numbers.');
            let size;
            if (!nested.length) size = { width: number(h.width, 120, 48, 600), height: number(h.height, 78, 36, 600) };
            else {
                const measures = nested.map(child => measure(child, depth + 1));
                const cols = Math.min(columns, nested.length), rows = Math.ceil(nested.length / cols);
                const cellWidth = Math.max(...measures.map(s => s.width)), cellHeight = Math.max(...measures.map(s => s.height));
                size = { width: padding * 2 + cols * cellWidth + (cols - 1) * gap,
                    height: padding * 2 + header + rows * cellHeight + (rows - 1) * gap,
                    padding, gap, header, cols, rows, cellWidth, cellHeight };
            }
            sizes.set(node.Id, size); return size;
        };
        const root = roots[0], size = measure(root, 0);
        if (visited.size !== nodes.size) throw new Error('Schematic contains unreachable or cyclic nodes.');
        const boxes = [], corridors = [];
        const place = (node, x, y, depth) => {
            const s = sizes.get(node.Id), nested = children.get(node.Id), h = hint(node);
            const box = { id: node.Id, node, x, y, width: s.width, height: s.height, depth, container: nested.length > 0 };
            boxes.push(box);
            if (nested.length && h.corridors === true) {
                const left = x + s.padding, top = y + s.padding + s.header;
                const right = x + s.width - s.padding, bottom = y + s.height - s.padding;
                for (let c = 1; c < s.cols; c++) {
                    const cx = left + c * s.cellWidth + (c - 0.5) * s.gap;
                    corridors.push({ x1: cx, y1: top, x2: cx, y2: bottom, width: s.gap * 0.55, owner: node.Id });
                }
                for (let r = 1; r < s.rows; r++) {
                    const cy = top + r * s.cellHeight + (r - 0.5) * s.gap;
                    corridors.push({ x1: left, y1: cy, x2: right, y2: cy, width: s.gap * 0.55, owner: node.Id });
                }
            }
            nested.forEach((child, i) => {
                const childSize = sizes.get(child.Id);
                place(child, x + s.padding + (i % s.cols) * (s.cellWidth + s.gap) + (s.cellWidth - childSize.width) / 2,
                    y + s.padding + s.header + Math.floor(i / s.cols) * (s.cellHeight + s.gap) + (s.cellHeight - childSize.height) / 2, depth + 1);
            });
        };
        place(root, 24, 24, 0);
        return { boxes, corridors, width: size.width + 48, height: size.height + 48, world };
    }

    constructor(svg, world, details, status) {
        this.svg = svg; this.details = details; this.status = status; this.selected = null; this.events = [];
        this.load(world);
        svg.addEventListener('wheel', event => { event.preventDefault(); this.zoom(event.deltaY < 0 ? 1.15 : 1 / 1.15); }, { passive: false });
        let drag = null;
        svg.addEventListener('pointerdown', event => { if (event.button === 0) { this.dragged = false; drag = [event.clientX, event.clientY]; } });
        svg.addEventListener('pointermove', event => {
            if (!drag) return;
            if (!this.dragged && Math.hypot(event.clientX - drag[0], event.clientY - drag[1]) < 3) return;
            this.dragged = true; svg.setPointerCapture(event.pointerId);
            const bounds = svg.getBoundingClientRect();
            this.view[0] -= (event.clientX - drag[0]) * this.view[2] / bounds.width;
            this.view[1] -= (event.clientY - drag[1]) * this.view[3] / bounds.height;
            drag = [event.clientX, event.clientY]; this.applyView();
        });
        svg.addEventListener('pointerup', () => { drag = null; });
        svg.addEventListener('pointercancel', () => { drag = null; });
    }

    element(tag, attrs = {}, text = null, parent = this.svg) {
        const el = document.createElementNS('http://www.w3.org/2000/svg', tag);
        for (const [key, value] of Object.entries(attrs)) el.setAttribute(key, String(value));
        if (text !== null) el.textContent = text;
        parent.append(el); return el;
    }

    load(world) {
        const plan = WorldSchematic.layout(world); // Validate before replacing a working diagram.
        this.plan = plan; this.selected = null; this.svg.replaceChildren();
        this.details.replaceChildren();
        const prompt = document.createElement('p'); prompt.textContent = 'Select a region, site or constituent structure to inspect its purpose and metadata.';
        this.details.append(prompt);
        this.element('title', {}, `${world.Id}: world schematic`);
        this.element('desc', {}, 'Nested regions and locations. Use Tab and Enter to inspect nodes, or the fit and zoom controls. Layout uses diagram units.');
        const defs = this.element('defs');
        const marker = this.element('marker', { id: 'arrow', markerWidth: 8, markerHeight: 8, refX: 7, refY: 4, orient: 'auto' }, null, defs);
        this.element('path', { d: 'M0 0 L8 4 L0 8 Z', fill: '#176891' }, null, marker);
        this.groups = {};
        for (const name of ['regions', 'roads', 'sites', 'links', 'labels']) this.groups[name] = this.element('g', { 'data-layer': name });
        const safeColor = (value, fallback) => typeof value === 'string' && /^#[a-f0-9]{6}$/i.test(value) ? value : fallback;
        for (const box of plan.boxes) {
            const h = box.node.Properties?.schematic ?? {}, label = h.label || box.node.Kind;
            const group = this.element('g', { tabindex: 0, role: 'button', 'aria-label': `${label}: ${box.id}`, 'data-node-id': box.id }, null,
                box.container ? this.groups.regions : this.groups.sites);
            const rect = this.element('rect', { x: box.x, y: box.y, width: box.width, height: box.height, rx: plan.coordinateSpace ? 0 : box.container ? 10 : 4,
                fill: safeColor(h.color, box.container ? '#e6ede7' : '#b8ccd5'), stroke: '#637d82', 'stroke-width': box.depth === 0 ? 2 : 1 }, null, group);
            this.element('title', {}, `${label}\n${box.node.Purpose}`, group);
            if (!box.container && h.footprint !== false) {
                this.element('rect', { x: box.x + box.width * 0.12, y: box.y + box.height * 0.12, width: box.width * 0.76,
                    height: box.height * 0.60, rx: 2, fill: '#ffffff', 'fill-opacity': 0.36, stroke: '#637d82' }, null, group);
            }
            const labelText = this.element('text', { x: box.container ? box.x + 14 : box.x + box.width / 2,
                y: box.container ? box.y + 23 : plan.coordinateSpace ? box.y + box.height * 0.65 : box.y + box.height - 9, 'text-anchor': box.container ? 'start' : 'middle',
                'font-size': plan.coordinateSpace ? Math.min(box.container?14:6,box.width/Math.max(2,String(label).length)*1.2) : box.depth === 0 ? 22 : box.container ? 16 : 12, fill: '#18383e', 'font-weight': box.container ? 650 : 500,
                'pointer-events': 'none' }, String(label).slice(0, 44), this.groups.labels);
            const select = () => {
                if (this.dragged) return;
                if (this.selected) { this.selected.rect.setAttribute('stroke-width', this.selected.width); this.selected.rect.setAttribute('stroke', '#637d82'); }
                this.selected = { rect, width: box.depth === 0 ? '2' : '1' }; rect.setAttribute('stroke', '#135d90'); rect.setAttribute('stroke-width', '4');
                this.details.replaceChildren();
                const heading = document.createElement('h2'); heading.textContent = String(label); this.details.append(heading);
                const purpose = document.createElement('p'); purpose.textContent = box.node.Purpose; this.details.append(purpose);
                const info = document.createElement('pre'); info.textContent = JSON.stringify({ Id: box.id, Kind: box.node.Kind, Parent: box.node.ParentId,
                    Diagram: { x: box.x, y: box.y, width: box.width, height: box.height }, WorldPosition: box.node.Position,
                    Properties: box.node.Properties, StructureKey: box.node.StructureKey }, null, 2); this.details.append(info);
                this.notify('schematic.node.selected', `Selected ${label}.`);
            };
            group.addEventListener('click', select);
            group.addEventListener('keydown', event => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); this.dragged = false; select(); } });
            labelText.setAttribute('aria-hidden', 'true');
            if (h.hideLabel === true) labelText.style.display = 'none';
        }
        for (const road of plan.corridors) {
            this.element('line', { ...road, stroke: '#76878b', 'stroke-width': road.width, 'stroke-linecap': 'square' }, null, this.groups.roads);
            this.element('line', { x1: road.x1, y1: road.y1, x2: road.x2, y2: road.y2, stroke: '#f5f1d8',
                'stroke-width': 1, 'stroke-dasharray': '6 6' }, null, this.groups.roads);
        }
        const byId = new Map(plan.boxes.map(box => [box.id, box]));
        for (const link of world.Connections) {
            const a = byId.get(link.FromId), b = byId.get(link.ToId);
            const ax = a.x + a.width / 2, ay = a.y + a.height / 2, bx = b.x + b.width / 2, by = b.y + b.height / 2;
            const path = this.element('path', { d: `M${ax} ${ay} H${(ax + bx) / 2} V${by} H${bx}`, fill: 'none', stroke: '#176891',
                'stroke-width': 2, 'stroke-dasharray': '5 4', 'marker-end': link.Bidirectional ? '' : 'url(#arrow)' }, null, this.groups.links);
            this.element('title', {}, `${link.Kind}: ${link.FromId} → ${link.ToId}${link.Bidirectional ? ' (both ways)' : ''}`, path);
        }
        this.groups.links.style.display = 'none';
        this.fit(); this.notify('schematic.loaded', `${world.Nodes.length} nodes · ${world.Connections.length} relationships · ${world.Coverage}.`);
        const title = document.querySelector('#world-title'); if (title) title.textContent = world.Id.replaceAll('-', ' ');
        const scope = document.querySelector('#scope'); if (scope) scope.textContent = world.Coverage === 'PartialScope'
            ? `Partial city/world scope · remaining: ${(world.RemainingScopes || []).join(', ')}` : 'Complete declared recipe scope';
        const note = document.querySelector('#coordinate-note'); if (note) note.textContent = plan.coordinateSpace
            ? 'Exact engine-generated meter coordinates. JavaScript displays streets, parcels and footprints without relocating them. Artwork and gameplay integration are separate.'
            : 'Schematic diagram units. Boxes and road channels express the declared design; they are not measured geometry or a playable map.';
    }

    fit() { this.view = [0, 0, this.plan.width, this.plan.height]; this.applyView(); }
    applyView() { this.svg.setAttribute('viewBox', this.view.join(' ')); }
    zoom(factor) {
        const width = this.view[2] / factor, height = this.view[3] / factor;
        if (width < this.plan.width / 12 || width > this.plan.width * 3) return;
        this.view = [this.view[0] + (this.view[2] - width) / 2, this.view[1] + (this.view[3] - height) / 2, width, height]; this.applyView();
    }
    layer(name, visible) { if (this.groups[name]) this.groups[name].style.display = visible ? '' : 'none'; }
    notify(event, message, outcome = 'succeeded') {
        this.status.textContent = message;
        const entry = { event, occurredAtUtc: new Date().toISOString(), target: this.plan?.world.Id ?? null, outcome };
        this.events.push(entry); if (this.events.length > 200) this.events.shift();
        this.svg.dispatchEvent(new CustomEvent('worldcreator.operation', { bubbles: true, detail: entry }));
    }
    downloadSvg() {
        try {
        this.notify('schematic.svg.prepared', 'SVG export prepared.');
        const copy = this.svg.cloneNode(true); copy.setAttribute('viewBox', `0 0 ${this.plan.width} ${this.plan.height}`);
        copy.setAttribute('width', this.plan.width); copy.setAttribute('height', this.plan.height); copy.setAttribute('font-family', 'system-ui, sans-serif');
        this.element('metadata', {}, JSON.stringify({ RecipeSha256: this.plan.world.RecipeSha256, CatalogSha256: this.plan.world.CatalogSha256,
            CoordinateSpace: this.plan.coordinateSpace || 'schematic-diagram-units', LocalSessionEvents: this.events }), copy);
        const url = URL.createObjectURL(new Blob([new XMLSerializer().serializeToString(copy)], { type: 'image/svg+xml' }));
        const a = document.createElement('a'); a.href = url; a.download = `${this.plan.world.Id}-schematic.svg`; a.click();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
        } catch (error) { this.notify('schematic.svg.failed', 'Could not prepare the SVG export.', 'failed'); console.error('schematic.svg.failed', error); }
    }

    static start() {
        const status = document.querySelector('#status');
        try {
            const viewer = new WorldSchematic(document.querySelector('#map'), JSON.parse(document.querySelector('#world-data').textContent), document.querySelector('#details'), status);
            document.querySelector('#fit').addEventListener('click', () => viewer.fit());
            document.querySelector('#zoom-in').addEventListener('click', () => viewer.zoom(1.3));
            document.querySelector('#zoom-out').addEventListener('click', () => viewer.zoom(1 / 1.3));
            document.querySelector('#export').addEventListener('click', () => viewer.downloadSvg());
            for (const input of document.querySelectorAll('[data-toggle]')) input.addEventListener('change', () => viewer.layer(input.dataset.toggle, input.checked));
            document.querySelector('#load').addEventListener('change', async event => {
                try {
                    const file = event.target.files[0]; if (!file) return;
                    if (file.size > 16777216) throw new Error('Metadata file is too large (maximum 16 MiB).');
                    viewer.load(JSON.parse(await file.text()));
                    for (const input of document.querySelectorAll('[data-toggle]')) viewer.layer(input.dataset.toggle, input.checked);
                } catch (error) { viewer.notify('schematic.load.failed', `Cannot load schematic: ${error.message}`, 'failed'); console.error('schematic.load.failed', error); }
                finally { event.target.value = ''; }
            });
            globalThis.worldSchematic = viewer;
        } catch (error) { status.textContent = `Cannot display schematic: ${error.message}`; console.error('schematic.start.failed', error); }
    }
}
if (typeof module !== 'undefined') module.exports = WorldSchematic;
if (typeof document !== 'undefined') document.addEventListener('DOMContentLoaded', () => WorldSchematic.start());
