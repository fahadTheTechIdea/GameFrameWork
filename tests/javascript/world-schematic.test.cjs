const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const WorldSchematic = require('../../src/WorldCreator/WorldCreator.Core/Schematic/world-schematic.js');
const folder = process.argv.find(a => a.startsWith('--world='))?.slice(8) || 'generated/city-schematic';
const world = JSON.parse(fs.readFileSync(path.join(folder, 'world-metadata.json')));

test('deterministic city hierarchy, every node retained, no overlapping sibling parcels', () => {
    const plan = WorldSchematic.layout(world);
    assert.deepEqual(WorldSchematic.layout(world), plan);
    assert.equal(plan.boxes.length, world.Nodes.length);
    assert.equal(plan.boxes.filter(b => b.node.Kind === 'district').length, 6);
    assert.ok(plan.boxes.some(b => b.node.ArchetypeId === 'factory' && b.container));
    assert.ok(plan.boxes.some(b => b.node.ArchetypeId === 'workshop' && b.depth === 3));
    const byId = new Map(plan.boxes.map(b => [b.id, b]));
    for (const b of plan.boxes) {
        assert.ok([b.x, b.y, b.width, b.height].every(Number.isFinite));
        if (!b.node.ParentId) continue;
        const p = byId.get(b.node.ParentId);
        assert.ok(b.x >= p.x && b.y >= p.y && b.x + b.width <= p.x + p.width && b.y + b.height <= p.y + p.height);
        for (const other of plan.boxes.filter(o => o.id !== b.id && o.node.ParentId === b.node.ParentId))
            assert.ok(b.x + b.width <= other.x || other.x + other.width <= b.x || b.y + b.height <= other.y || other.y + other.height <= b.y);
    }
    assert.ok(plan.corridors.length > 0);
    assert.equal(world.Coverage, 'PartialScope');
    assert.ok(world.Nodes.every(n => n.Position === null)); // Display layout does not mutate world coordinates.
});

test('non-city metadata uses the same renderer without setting-specific branches', () => {
    const arbitrary = structuredClone(world);
    arbitrary.Nodes.forEach(n => { n.Kind = 'orbital-location'; n.Properties = {}; });
    assert.equal(WorldSchematic.layout(arbitrary).boxes.length, world.Nodes.length);
});

test('unknown parents, cycles, duplicate IDs, missing endpoints and unsafe layout budgets reject', () => {
    for (const mutate of [
        w => { w.Nodes[1].ParentId = 'missing'; },
        w => { w.Nodes[1].ParentId = w.Nodes[1].Id; },
        w => { w.Nodes.push(w.Nodes[1]); },
        w => { w.Connections[0].ToId = 'missing'; },
        w => { w.Nodes[0].Properties.schematic.columns = 1.2; },
        w => { w.Nodes[0].Properties.schematic.gap = Infinity; },
        w => { w.Nodes = Array.from({length:5001}, () => w.Nodes[0]); }
    ]) { const bad = structuredClone(world); mutate(bad); assert.throws(() => WorldSchematic.layout(bad)); }
});

test('exported viewer embeds exact metadata and packaged JavaScript, without image/network dependencies', () => {
    const html = fs.readFileSync(path.join(folder, 'schematic.html'), 'utf8');
    const data = /<script id="world-data" type="application\/json">([\s\S]*?)<\/script>/.exec(html);
    assert.deepEqual(JSON.parse(data[1]), world);
    assert.equal(fs.readFileSync(path.join(folder, 'schematic.js'), 'utf8'), fs.readFileSync('src/WorldCreator/WorldCreator.Core/Schematic/world-schematic.js', 'utf8'));
    assert.ok(!html.includes('__WORLD_METADATA_JSON__'));
    assert.ok(!/<(?:script|link|img)[^>]+(?:src|href)="https?:/.test(html));
});
