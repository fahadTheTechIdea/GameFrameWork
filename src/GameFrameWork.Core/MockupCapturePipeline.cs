using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using GameFrameWork.Core.Planning;
using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core.Capture;

public sealed record CaptureCrop(int X, int Y, int Width, int Height);
public sealed record CapturePromptTask(string FeatureId, string AssetClass, CaptureCrop Crop, string Status,
    string AnalysisPrompt, string MasterPrompt, IReadOnlyList<string> ProductionGates);
public sealed record CapturePromptPacket(string Status, CaptureSource Source,
    IReadOnlyList<CapturePromptTask> Tasks, IReadOnlyList<string> AtlasWorkflow);

/// <summary>Verified source bytes and contract validation precede any export. A capture is never an art approval.</summary>
public static class MockupCapturePipeline
{
    public static async Task<byte[]> ReadSourceAsync(MockupCaptureDocument capture, string trustedRoot, CancellationToken token = default)
    {
        RequireValid(capture, token);
        string path = ResolveSourcePath(trustedRoot, capture.Source.File);
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        if (stream.Length is < 33 or > 67108864) throw new ArgumentException("CAPTURE_IMAGE_SIZE_LIMIT");
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, token);
        if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(capture.Source.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("CAPTURE_SOURCE_HASH_MISMATCH");
        if (!bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ||
            !bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8) ||
            BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4)) != capture.Source.Width ||
            BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4)) != capture.Source.Height)
            throw new ArgumentException("CAPTURE_SOURCE_PNG_DIMENSIONS_MISMATCH");
        return bytes; // Header/hash check; decoded pixel integrity remains the image tool's responsibility.
    }

    public static string ResolveSourcePath(string trustedRoot, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Contains(':') || relative.Contains('\\'))
            throw new ArgumentException("CAPTURE_SOURCE_PATH_INVALID");
        string root = Path.GetFullPath(trustedRoot);
        string full = Path.GetFullPath(Path.Combine(root, relative));
        string prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!full.StartsWith(prefix, comparison)) throw new ArgumentException("CAPTURE_SOURCE_PATH_INVALID");
        if (!File.Exists(full)) throw new ArgumentException("CAPTURE_SOURCE_PATH_INVALID");
        // Reject links/junctions in the entire path, including ancestors of the trusted root.
        for (var current = new FileInfo(full) as FileSystemInfo; current is not null; current = current switch
        { FileInfo file => file.Directory, DirectoryInfo directory => directory.Parent, _ => null })
            if ((current.Attributes & FileAttributes.ReparsePoint) != 0) throw new ArgumentException("CAPTURE_SOURCE_LINK_REJECTED");
        if (!File.Exists(full)) throw new ArgumentException("CAPTURE_SOURCE_PATH_INVALID");
        return full;
    }

    public static string Overlay(MockupCaptureDocument capture, byte[] verifiedPng, CancellationToken token = default)
    {
        var report = RequireValid(capture, token);
        if (!Convert.ToHexString(SHA256.HashData(verifiedPng)).Equals(capture.Source.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("CAPTURE_SOURCE_HASH_MISMATCH");
        XNamespace svg = "http://www.w3.org/2000/svg";
        var root = new XElement(svg + "svg", new XAttribute("viewBox", $"0 0 {capture.Source.Width} {capture.Source.Height}"),
            new XAttribute("width", capture.Source.Width), new XAttribute("height", capture.Source.Height),
            new XElement(svg + "title", "Source-pixel capture review; validation does not convey art approval"),
            new XElement(svg + "image", new XAttribute("href", "data:image/png;base64," + Convert.ToBase64String(verifiedPng)),
                new XAttribute("width", capture.Source.Width), new XAttribute("height", capture.Source.Height)));
        string N(double n) => n.ToString("0.###", CultureInfo.InvariantCulture);
        foreach (var f in capture.Features.OrderBy(f => f.Id, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            var color = f.Review == CaptureReview.Rejected ? "#ff476a" : f.Semantic.StartsWith("vegetation.",StringComparison.Ordinal) ? "#5cf4c2" :
                f.Semantic.StartsWith("landscape.",StringComparison.Ordinal) ? "#b5ed45" : f.Road is not null || f.Semantic.StartsWith("road.",StringComparison.Ordinal) ? "#00e5ff" : f.Semantic == "building.roof" ? "#ffc247" : "#e8a8ff";
            var group = new XElement(svg + "g", new XAttribute("data-feature", f.Id), new XAttribute("data-semantic", f.Semantic),
                new XElement(svg + "title", $"{f.Id}: {f.Semantic}; {f.Evidence}; {f.Review}; uncertainty ±{N(f.UncertaintyPixels)}px. {f.Notes}"));
            if (f.Road is { } road) group.Add(new XElement(svg + "polyline", new XAttribute("points", string.Join(" ", f.Points.Select(p => $"{N(p.X)},{N(p.Y)}"))),
                new XAttribute("class", "width-band"), new XAttribute("fill", "none"), new XAttribute("stroke", color),
                new XAttribute("stroke-width", N(road.WidthPixels)), new XAttribute("opacity", "0.12")));
            XElement shape = f.Shape == CaptureShape.Point
                ? new(svg + "circle", new XAttribute("cx", N(f.Points[0].X)), new XAttribute("cy", N(f.Points[0].Y)), new XAttribute("r", N(f.VegetationPlacement?.GroundRadiusPixels ?? 5)))
                : new(svg + (f.Shape == CaptureShape.Polygon ? "polygon" : "polyline"),
                    new XAttribute("points", string.Join(" ", f.Points.Select(p => $"{N(p.X)},{N(p.Y)}"))));
            shape.Add(new XAttribute("stroke", color), new XAttribute("stroke-width", 2),
                new XAttribute("fill", f.Shape == CaptureShape.Polygon ? color : "none"),
                new XAttribute("fill-opacity", f.Semantic == "landscape.planted-ground" ? "0.3" : "0.08"));
            if (f.Evidence != CaptureEvidence.Observed || f.Occluded) shape.Add(new XAttribute("stroke-dasharray", "7 5"));
            group.Add(shape);
            group.Add(new XElement(svg + "text", new XAttribute("x", N(f.Points[0].X + 4)), new XAttribute("y", N(Math.Max(15, f.Points[0].Y - 5))),
                new XAttribute("font-family", "sans-serif"), new XAttribute("font-size", 13), new XAttribute("fill", color),
                new XAttribute("stroke", "#10181f"), new XAttribute("stroke-width", 3), new XAttribute("paint-order", "stroke"), f.Id));
            root.Add(group);
        }
        foreach(var check in capture.SurfaceChecks ?? [])
            root.Add(new XElement(svg+"circle",new XAttribute("class","surface-check"),new XAttribute("data-target",check.FeatureId),
                new XAttribute("cx",N(check.Pixel.X)),new XAttribute("cy",N(check.Pixel.Y)),new XAttribute("r",2),
                new XAttribute("fill",check.Inside?"#ffffff":"#ff597b"),new XAttribute("stroke","#111"),new XAttribute("stroke-width",0.5),
                new XElement(svg+"title",check.FeatureId+": expected "+(check.Inside?"inside":"outside")+". "+check.Notes)));
        foreach (var j in capture.Junctions)
            root.Add(new XElement(svg + "circle", new XAttribute("cx", N(j.Position.X)), new XAttribute("cy", N(j.Position.Y)),
                new XAttribute("r", 5), new XAttribute("stroke", "white"), new XAttribute("fill", j.Kind == CaptureJunctionKind.Unresolved ? "#ff476a" : "#111111"),
                new XElement(svg + "title", $"{j.Id}: {j.Kind}; {j.Review}. {j.Notes}")));
        foreach (var crossing in report.Crossings.Where(c => c.JunctionId is null))
            root.Add(new XElement(svg + "circle", new XAttribute("cx", N(crossing.Position.X)), new XAttribute("cy", N(crossing.Position.Y)),
                new XAttribute("r", 4), new XAttribute("stroke", "#ff476a"), new XAttribute("stroke-width", 2), new XAttribute("fill", "none"),
                new XElement(svg + "title", $"Unresolved projected crossing: {crossing.FirstRoadId} / {crossing.SecondRoadId}; no connection assumed.")));
        return new XDocument(root).ToString();
    }

    public static string ReviewHtml(MockupCaptureDocument capture, byte[] verifiedPng, CancellationToken token = default)
    {
        var svg = Overlay(capture, verifiedPng, token);
        var json = JsonSerializer.Serialize(capture, MockupCaptureJson.Options()); // Default JSON encoder escapes HTML delimiters in untrusted notes.
        return """
<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Mockup source capture review</title><style>
*{box-sizing:border-box}body{margin:0;background:#101a21;color:#e5edf2;font:14px system-ui,sans-serif}header{padding:18px 24px;border-bottom:1px solid #35434c}h1{font-size:22px;margin:0 0 8px}p{line-height:1.55}header p{margin:0;color:#bacad4}.layout{display:grid;grid-template-columns:minmax(0,1fr) 330px;height:calc(100vh - 102px)}main{padding:16px;overflow:auto}.tools{display:flex;gap:14px;align-items:center;flex-wrap:wrap;margin-bottom:12px}button{background:#22343f;color:inherit;border:1px solid #586a75;border-radius:6px;padding:8px 11px;cursor:pointer}button:hover,button:focus-visible{outline:2px solid #69d8ee}#map{background:#15252e;overflow:hidden;border:1px solid #4a5c65}#map svg{width:100%;height:auto;display:block}#map svg text{display:none}#map.labels svg text{display:block}#map.source-only svg g,#map.source-only svg circle{display:none}#map:not(.widths) .width-band{display:none}svg g{cursor:pointer}svg g:hover>polyline,svg g:hover>polygon{stroke:white}aside{border-left:1px solid #35434c;padding:18px;overflow:auto}aside h2{font-size:16px}#features{display:grid;gap:6px}#features button{text-align:left;font-size:12px}#detail{background:#1c2c36;padding:12px;border-radius:7px;margin:16px 0;white-space:pre-wrap;line-height:1.5}.tag{font-size:11px;letter-spacing:2px;color:#ffd080}.legend{display:flex;gap:12px;margin:12px 0;color:#bacad4;font-size:12px}.road{color:#00e5ff}.roof{color:#ffc247}.region{color:#9aff68}small{color:#bacad4;overflow-wrap:anywhere}@media(max-width:850px){.layout{display:block;height:auto}aside{border-left:0;border-top:1px solid #35434c}header{padding:14px}main{padding:10px}}
</style><header><div class="tag">SOURCE CAPTURE / PROVISIONAL</div><h1 id="heading">Mockup capture</h1><p id="summary">Original pixels with traceable annotations. Roofs, roads and regions remain distinct.</p></header>
<div class="layout"><main><div class="tools"><button id="original" type="button">Show original</button><button id="reset" type="button">Fit whole image</button><label><input id="labels" type="checkbox">Labels</label><label><input id="widths" type="checkbox">Approximate road widths</label><label><input id="samples" type="checkbox">Source boundary samples</label></div><style>#map:not(.samples) .surface-check{display:none}</style>
<div class="legend"><span class="road">Cyan: roads</span><span class="roof">Amber: roofs</span><span class="region">Green/mint: landscape/trees</span><span>Pink: other details · Dashed: inferred/occluded</span></div><div id="map">
""" + svg + """
</div><p><small>Select a feature to inspect its source coordinates and uncertainty. This is an annotation review, not an engine-generated city or production sprite sheet.</small></p></main>
<aside><h2 id="count"></h2><label>Search details<input id="search" type="search" placeholder="Tree, tank, roof, garden…"></label><label>Feature layer<select id="layer"><option value="">All observed layers</option><option value="green">Green areas, trees and sports</option><option value="vegetation">Trees and palms</option><option value="landscape">Green surfaces</option><option value="building">Buildings and components</option><option value="road">Roads and crossings</option><option value="path">Pedestrian/service paths</option><option value="vehicle">Vehicles</option><option value="industrial">Industrial equipment</option><option value="access">Entrances and docks</option></select></label><label>Inspection region<select id="area"><option value="">Whole image</option></select></label><label>Evidence<select id="evidence"><option value="Observed">Observed source details</option><option value="">All evidence</option><option value="Authored">Authored design</option><option value="Inferred">Inferred</option></select></label><p id="visible" aria-live="polite"></p><div id="detail" aria-live="polite">Choose an annotation on the image or from the list below.</div><svg id="crop" role="img" aria-label="Unaltered source crop for selected detail" hidden></svg><h2>Region work remaining</h2><ul id="region-gaps"></ul><h2>Capture gaps</h2><ul id="gaps"></ul><h2>Features</h2><div id="features"></div><p><small id="source"></small></p></aside></div><style>aside label{display:block;margin:10px 0;font-size:12px}input[type=search],select{display:block;width:100%;padding:7px;background:#22343f;color:#e5edf2;border:1px solid #617782;border-radius:4px;margin-top:5px}#crop{width:100%;max-height:220px;background:#101a21;border:1px solid #617782}.filtered{display:none}svg g.selected>polygon,svg g.selected>polyline,svg g.selected>circle{stroke:#fff;stroke-width:3}#map svg g[data-semantic^="vegetation."] circle{fill-opacity:.6}#visible{color:#a9dcb6;font-size:12px}</style><script id="capture" type="application/json">
""" + json + """
</script><script>
const data=JSON.parse(document.getElementById('capture').textContent),map=document.getElementById('map'),svg=map.querySelector('svg');
const full=`0 0 ${data.source.width} ${data.source.height}`;
document.getElementById('heading').textContent=`${data.source.mockupId} · mockup capture`;
document.getElementById('summary').textContent=`Original pixels with traceable annotations. ${data.coverage} coverage; ${data.calibration?'authored calibration supplied':'world scale unresolved'}. No art approval is conveyed.`;
document.getElementById('count').textContent=`${data.features.length} provisional features · ${data.coverage}`;
document.getElementById('source').textContent=`Source: ${data.source.file}\nSHA256: ${data.source.sha256}`;
for(const gap of data.unresolved){const li=document.createElement('li');li.textContent=gap;document.getElementById('gaps').append(li)}
const search=document.getElementById('search'),layer=document.getElementById('layer'),area=document.getElementById('area'),evidence=document.getElementById('evidence');
const groups=new Map([...map.querySelectorAll('[data-feature]')].map(g=>[g.dataset.feature,g]));
const crop=document.getElementById('crop');crop.append(svg.querySelector('image').cloneNode(true));
function frame(b){svg.setAttribute('viewBox',`${b.x} ${b.y} ${b.width} ${b.height}`)}
function select(f){const road=f.road?`\nWidth: ${f.road.widthPixels}px (${f.road.widthKind}); level: ${f.road.level??'unknown'}`:'';const p=f.points[0],c=data.calibration;const ground=f.semantic.startsWith('vegetation.')&&c?`\nTree base: source (${p.x}, ${p.y}) → world (${(c.a*p.x+c.b*p.y+c.c).toFixed(2)}, ${(c.d*p.x+c.e*p.y+c.f).toFixed(2)})m. Same source location.`:'';document.getElementById('detail').textContent=`${f.id}\n${f.semantic}\n${f.evidence} · ${f.review} · ±${f.uncertaintyPixels}px${road}${ground}\nParent: ${f.parentId??'scene'}\n${f.binding?'Bound to '+f.binding.kind+': '+f.binding.targetId:'Detail only; outside current city compilation scope'}\n\n${f.notes}${f.vegetationPlacement?'\nGround base radius: '+f.vegetationPlacement.groundRadiusPixels+'px; planted-ground parent and '+f.vegetationPlacement.protectedFeatureIds.length+' protected boundaries checked.':''}`;const xs=f.points.map(p=>p.x),ys=f.points.map(p=>p.y),pad=35;let x=Math.max(0,Math.min(...xs)-pad),y=Math.max(0,Math.min(...ys)-pad),w=Math.min(data.source.width-x,Math.max(...xs)-x+pad),h=Math.min(data.source.height-y,Math.max(...ys)-y+pad);frame({x,y,width:w,height:h});crop.hidden=false;crop.setAttribute('viewBox',`${x} ${y} ${w} ${h}`);for(const g of groups.values())g.classList.remove('selected');groups.get(f.id)?.classList.add('selected')}
for(const a of data.inspectionAreas??[]){const o=document.createElement('option');o.value=a.id;o.textContent=`${a.id} · ${a.status}`;area.append(o)}
const initial=new URLSearchParams(location.search);for(const control of [layer,area]){const value=initial.get(control.id);if(value!==null&&[...control.options].some(o=>o.value===value))control.value=value;}
function filter(){const a=data.inspectionAreas?.find(a=>a.id===area.value),allowed=a?new Set(a.featureIds):null,q=search.value.trim().toLowerCase(),list=document.getElementById('features');list.replaceChildren();let count=0;for(const f of data.features){const matchLayer=!layer.value||(layer.value==='green'?(f.semantic.startsWith('vegetation.')||f.semantic.startsWith('landscape.')||f.semantic.startsWith('sport.')):(f.semantic.startsWith(layer.value+'.')||(layer.value==='landscape'&&f.vegetationPlacement&&data.features.some(g=>g.id===f.parentId&&g.semantic.startsWith('landscape.')))));const show=matchLayer&&(!evidence.value||f.evidence===evidence.value)&&(!allowed||allowed.has(f.id))&&(!q||`${f.id} ${f.semantic} ${f.notes}`.toLowerCase().includes(q));groups.get(f.id)?.classList.toggle('filtered',!show);if(show){count++;const b=document.createElement('button');b.type='button';b.textContent=`${f.id} · ${f.semantic}`;b.onclick=()=>select(f);list.append(b)}}document.getElementById('visible').textContent=`${count} visible / ${data.features.length} annotations. Count does not measure completeness.`;const gaps=document.getElementById('region-gaps');gaps.replaceChildren();for(const gap of a?.remainingWork??['Select an inspection region to see specific remaining detail work.']){const li=document.createElement('li');li.textContent=gap;gaps.append(li)}}
search.oninput=filter;layer.onchange=filter;evidence.onchange=filter;area.onchange=()=>{filter();const a=data.inspectionAreas?.find(a=>a.id===area.value);if(a)frame(a.bounds);else svg.setAttribute('viewBox',full)};filter();const initialArea=data.inspectionAreas?.find(a=>a.id===area.value);if(initialArea)frame(initialArea.bounds);
for(const g of map.querySelectorAll('[data-feature]'))g.addEventListener('click',()=>select(data.features.find(f=>f.id===g.dataset.feature)));
document.getElementById('reset').onclick=()=>svg.setAttribute('viewBox',full);
document.getElementById('original').onclick=function(){map.classList.toggle('source-only');this.textContent=map.classList.contains('source-only')?'Show capture':'Show original'};
document.getElementById('labels').onchange=e=>map.classList.toggle('labels',e.target.checked);
document.getElementById('widths').onchange=e=>map.classList.toggle('widths',e.target.checked);
document.getElementById('samples').onchange=e=>map.classList.toggle('samples',e.target.checked);
</script></html>
""";
    }

    public static CapturePromptPacket Prompts(MockupCaptureDocument capture, CancellationToken token = default)
    {
        RequireValid(capture, token);
        var tasks = capture.Features.OrderBy(f => f.Id, StringComparer.Ordinal).Select(f =>
        {
            token.ThrowIfCancellationRequested();
            int left = Math.Max(0, (int)Math.Floor(f.Points.Min(p => p.X) - f.UncertaintyPixels - 8));
            int top = Math.Max(0, (int)Math.Floor(f.Points.Min(p => p.Y) - f.UncertaintyPixels - 8));
            int right = Math.Min(capture.Source.Width, (int)Math.Ceiling(f.Points.Max(p => p.X) + f.UncertaintyPixels + 8));
            int bottom = Math.Min(capture.Source.Height, (int)Math.Ceiling(f.Points.Max(p => p.Y) + f.UncertaintyPixels + 8));
            var crop = new CaptureCrop(left, top, right - left, bottom - top);
            string reference = $"Attach the actual source PNG {capture.Source.File}, SHA256 {capture.Source.Sha256}, and an unaltered source-pixel crop [{left},{top},{right-left},{bottom-top}] for {f.Id}. A path or hash in text does not attach an image.";
            var assetClass = f.Road is not null || f.Semantic is "sport.surface" or "road.surface" or "sidewalk.region" or "water.region" or "landscape.lawn" or "road.crosswalk" or "sport.football-pitch" or "sport.basketball-court"
                ? "terrain" : f.Semantic.StartsWith("building.", StringComparison.Ordinal) ? "static_structure"
                : f.Semantic.StartsWith("vegetation.",StringComparison.Ordinal) ? "prop"
                : f.Semantic.StartsWith("vehicle.",StringComparison.Ordinal) ? "mobile_directional"
                : f.Semantic.StartsWith("industrial.",StringComparison.Ordinal) ? "fixed_machinery"
                : f.Semantic is "water.decorative" or "prop.object" ? "prop" : "layout-only";
            string roadKit = f.Road is null ? "" : " ROAD KIT: this captured route is layout evidence, not one giant sprite. Resolve the approved ground-grid tile size and cross-section first. Generate one named terrain variant per call (straight, end, corner, T, X, roundabout segment, crossing, sidewalk/median transition as required by the reviewed topology). Preserve edge sockets, carriageway/sidewalk widths and materials across variants; assemble and validate exact tile geometry deterministically. Do not invent connecting arms from projected crossings.";
            return new CapturePromptTask(f.Id, assetClass, crop, "draft-blocked-for-production",
                $"ANALYSIS ONLY. {reference} Inspect {f.Semantic} in the full scene and crop. Preserve source coordinates; distinguish ground footprint from roof/occlusion. Report observed/inferred/unknown facts, road centerline/edges/width meaning, access and junction uncertainty where relevant. Current evidence: {f.Evidence}; uncertainty ±{f.UncertaintyPixels.ToString(CultureInfo.InvariantCulture)}px. {f.Notes} Do not fabricate hidden geometry, measured meters or approval.",
                assetClass == "layout-only"
                    ? $"LAYOUT/GRAMMAR TASK ONLY. {reference} This region or anchor is not one production sprite. Derive its child asset inventory, purposeful paths/planting/site grammars and attachment/access rules from reviewed source evidence. Classify each child using canonical GameArts asset classes before creating a master prompt. Do not rasterize a district, park, loading yard or access anchor into a generic prop. Unknown semantics require an explicit design mapping."
                    : $"ONE {assetClass} MASTER CANDIDATE, only after the production gates pass. {reference} The provisional analysis crop must be reviewed/expanded to include the complete required visible object; roof bounds alone may omit walls. Use the verified approved revision and saved GameArts style/camera/scale; current observed camera: {capture.Source.Camera}. Preserve approved visible identity, proportions, materials and palette. Specify exactly one requested change and one approved view. Separate static shell from independently animated doors/gates/wheels. Ask for one PNG on the configured GameArts opaque #FF00FF mask; no contact sheet, grid, labels or embedded JSON. Hidden sides require a design decision. Do not ask the image model to calculate atlas rectangles or physical dimensions. Verify the actual output after generation." + roadKit,
                ["Exact mockup revision explicitly approved in GameArts lifecycle", "Saved GameArts profile and scale/camera resolved", "Feature geometry, visible identity and complete production crop reviewed; analysis bounds are provisional", "Canonical GameArts reference crop prepared and attached to actual image tool call", "Master and component candidates reviewed separately"]);
        }).ToArray();
        return new("analysis-and-prompt-drafts-only", capture.Source, tasks,
            ["Approve a clean master using the actual reference crop; do not generate a whole atlas as one image.",
             "Generate/edit one isolated component pose/frame per image call, conditioned on the approved master and preceding accepted pose. Keep fixed pivots and scale; verify drift visually.",
             "Author reviewed per-frame pivot/attachment points and timing in the canonical GameArts frame manifest. Never accept default pivots as measured evidence.",
             "Run canonical GameArts scripts/build_sheet_atlas.py from individual opaque PNGs, without scaling; it computes cells, source hashes and frame rectangles from pixels.",
             "Run canonical scripts/validate_sheet_atlas.py and GameArtsAtlasValidator; preserve PNG + atlas JSON + source manifests. Structural validity does not establish visual approval.",
             "Use terrain/road topology variants (ends, straights, corners, T/X junctions, crossings and transitions) with explicit sockets and rotation policy, then test Godot TileSet margin/separation/collision/navigation and animation attachments."]);
    }

    public static CaptureGeometryDraft GeometryDraft(MockupCaptureDocument capture, CancellationToken token = default)
    {
        var report = RequireValid(capture, token);
        var selection = capture.CompilationScope?.FeatureIds.ToHashSet(StringComparer.Ordinal);
        if (selection is null && (capture.Coverage != CaptureCoverage.Complete || capture.Unresolved.Count != 0)) throw new ArgumentException("CAPTURE_COVERAGE_INCOMPLETE");
        if (capture.Calibration is null) throw new ArgumentException("CAPTURE_CALIBRATION_REQUIRED");
        var selected = capture.Features.Where(f => selection is null || selection.Contains(f.Id)).ToArray();
        bool SelectedRoads(IEnumerable<string> roads) => selection is null || roads.All(selection.Contains);
        if (selected.Any(f => f.Review != CaptureReview.Accepted || f.Evidence == CaptureEvidence.Unknown) ||
            capture.Junctions.Where(j => SelectedRoads(j.RoadIds)).Any(j => j.Review != CaptureReview.Accepted || j.Kind == CaptureJunctionKind.Unresolved) ||
            report.Crossings.Where(c => SelectedRoads([c.FirstRoadId,c.SecondRoadId])).Any(c => c.Kind == CaptureJunctionKind.Unresolved || c.JunctionId is null))
            throw new ArgumentException("CAPTURE_REVIEW_REQUIRED");
        var bound = selected.Where(f => f.Binding is not null).OrderBy(f => f.Id, StringComparer.Ordinal).ToArray();
        if (bound.Length == 0) throw new ArgumentException("CAPTURE_BINDINGS_REQUIRED");
        if (bound.Any(f => f.Road is { Level: null })) throw new ArgumentException("CAPTURE_ROAD_LEVEL_REQUIRED");
        return new("geometry-draft-not-production-world", capture.Source,
            bound.Select(f => new CaptureWorldFeature(f.Id, f.Semantic, f.Binding!, f.Shape,
                f.Points.Select(p => MockupCaptureValidator.Transform(capture.Calibration, p)).ToArray(), f.Road)).ToArray(),
            ["Verified GameArts lifecycle approval and saved profile", "Bind draft paths/regions/anchors into current MockupWorldDesign with catalog, road profiles, district/siting, terrain and composition grammars",
             "Run engine continuous/swept-surface/raster validation without snapping captured road geometry", "Production art bundle/file/atlas gates and Godot import/runtime visual verification"]);
    }

    /// <summary>Exact vegetation layout from source point annotations using the shared engine planner.
    /// Review and collision acceptance are separate gates; this draft never silently relocates plants.</summary>
    public static CaptureVegetationDraft? VegetationDraft(MockupCaptureDocument capture, CancellationToken token = default)
    {
        RequireValid(capture, token);
        var trees = capture.Features.Where(f => f.Semantic is "vegetation.tree" or "vegetation.palm").ToArray();
        if (trees.Length == 0 || capture.Calibration is null) return null;
        if (trees.Any(f => f.Shape != CaptureShape.Point || f.Evidence == CaptureEvidence.Unknown || f.Review == CaptureReview.Rejected))
            throw new ArgumentException("CAPTURE_VEGETATION_POINT_REQUIRED");
        var zone = new LandscapeZone("capture-landscape", "SourceLandscape", new WorldPolygon(
            new[] { new CapturePixel(0,0),new(capture.Source.Width,0),new(capture.Source.Width,capture.Source.Height),new(0,capture.Source.Height) }
                .Select(p => MockupCaptureValidator.Transform(capture.Calibration,p)).ToArray()), "capture-source", "same-source-position");
        var anchors = trees.Select(f => new VisualAnchor(f.Id,zone.SourceVisualRegionId,
            MockupCaptureValidator.Transform(capture.Calibration,f.Points[0]),f.Semantic)).ToArray();
        var plants = new List<PlannedVegetation>();
        foreach (var group in trees.GroupBy(f => f.Semantic).OrderBy(g => g.Key,StringComparer.Ordinal))
        {
            var grammar = new VegetationGrammar("capture-"+group.Key,zone.Id,"unresolved-"+group.Key,
                VegetationPattern.Anchored,1,0,"Tree base at captured crown center; exact source position, no relocation.",
                group.Select(f => f.Id).Order(StringComparer.Ordinal).ToArray());
            var result = VegetationCompositionPlanner.ComposeAnchors(grammar,zone,anchors,token);
            if (!result.IsValid) throw new ArgumentException(string.Join(",",result.Diagnostics));
            plants.AddRange(result.Plants);
        }
        return new("exact-position-layout-draft-not-collision-accepted",capture.Source,
            "Tree/palm base uses the captured crown center at the same source coordinate, transformed by the authored ground calibration.",plants,
            ["Review provisional source annotations and species/master asset mapping", "Reconcile detailed buildings, paths and planting surfaces with this layout; run existing clearance/raster gates without moving trees",
             "GameArts artwork approval, approved assets and Godot runtime"]);
    }

    /// <summary>Applies reviewed/calibrated geometry to the existing engine contract, preserving authored rules.
    /// This is a design handoff, not approval or successful world generation. Run the existing engine gates next.</summary>
    public static MockupWorldDesign ApplyToDesign(MockupCaptureDocument capture, MockupWorldDesign template, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (template.Blueprint?.Regions is null || template.Blueprint.Anchors is null || template.Paths is null ||
            template.Paths.Any(p => p is null) || template.Blueprint.Regions.Any(r => r is null) || template.Blueprint.Anchors.Any(a => a is null))
            throw new ArgumentException("CAPTURE_DESIGN_INVALID");
        var draft = GeometryDraft(capture, token);
        if (template.Blueprint.MockupId != capture.Source.MockupId || template.Blueprint.ApprovedRevision != capture.Source.Revision ||
            template.Blueprint.ArtStyleId != capture.Source.ArtStyleId)
            throw new ArgumentException("CAPTURE_DESIGN_SOURCE_MISMATCH");
        var paths = template.Paths.ToArray(); var regions = template.Blueprint.Regions.ToArray(); var anchors = template.Blueprint.Anchors.ToArray();
        foreach (var f in draft.Features)
        {
            token.ThrowIfCancellationRequested();
            if (f.Road is { Level: not 0 } && !(template.InfrastructureRules?.Any(r => r.AlignmentPathId == f.Binding.TargetId) ?? false))
                throw new ArgumentException("CAPTURE_DESIGN_INFRASTRUCTURE_RULE_REQUIRED:" + f.FeatureId);
            int[] matches = f.Binding.Kind switch
            {
                "path" => paths.Select((p, i) => (p.Id, i)).Where(p => p.Id == f.Binding.TargetId).Select(p => p.i).ToArray(),
                "region" => regions.Select((p, i) => (p.Id, i)).Where(p => p.Id == f.Binding.TargetId).Select(p => p.i).ToArray(),
                "anchor" => anchors.Select((p, i) => (p.Id, i)).Where(p => p.Id == f.Binding.TargetId).Select(p => p.i).ToArray(),
                _ => []
            };
            if (matches.Length != 1) throw new ArgumentException("CAPTURE_DESIGN_BINDING_MISSING_OR_AMBIGUOUS:" + f.FeatureId);
            int at = matches[0];
            switch (f.Binding.Kind)
            {
                case "path": paths[at] = paths[at] with { Centerline = new WorldPolyline(f.Points) }; break;
                case "region": regions[at] = regions[at] with { Outline = new WorldPolygon(f.Points) }; break;
                case "anchor": anchors[at] = anchors[at] with { PositionMeters = f.Points[0] }; break;
            }
        }
        return template with { Paths = paths, Blueprint = template.Blueprint with { Regions = regions, Anchors = anchors }, Capture = capture };
    }

    public static void ValidateDesignTrace(MockupWorldDesign design, CancellationToken token = default)
    {
        if (design.Capture is null) return; // Fully authored composition grammars need not originate in a pixel capture.
        var expected = ApplyToDesign(design.Capture, design, token);
        if (expected.Paths.Where((p, i) => !p.Centerline.Points.SequenceEqual(design.Paths[i].Centerline.Points)).Any() ||
            expected.Blueprint.Regions.Where((r, i) => !r.Outline.Vertices.SequenceEqual(design.Blueprint.Regions[i].Outline.Vertices)).Any() ||
            expected.Blueprint.Anchors.Where((a, i) => a.PositionMeters != design.Blueprint.Anchors[i].PositionMeters).Any())
            throw new ArgumentException("CAPTURE_DESIGN_GEOMETRY_MISMATCH");
    }

    private static CaptureReport RequireValid(MockupCaptureDocument capture, CancellationToken token)
    {
        var report = MockupCaptureValidator.Validate(capture, token);
        if (!report.Valid) throw new ArgumentException(string.Join(",", report.Issues.Select(i => i.Code + ":" + i.FeatureId)));
        return report;
    }
}
