using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Globalization;

namespace GameFrameWork.Core.Capture;

public interface ICaptureWorldPreviewRenderer
{
    string Render(WorldDocument world,byte[] sourcePng,CancellationToken token=default);
}

/// <summary>Pairs the verified visual source with actual generated geometry; no new art is produced.</summary>
public sealed class CaptureWorldPreviewRenderer(IWorldPlanRenderer geometry) : ICaptureWorldPreviewRenderer
{
    public string Render(WorldDocument world,byte[] sourcePng,CancellationToken token=default)
    {
        var capture=world.Request.Design?.Capture ?? throw new ArgumentException("CAPTURE_SOURCE_REQUIRED");
        if(!Convert.ToHexString(SHA256.HashData(sourcePng)).Equals(capture.Source.Sha256,StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("CAPTURE_SOURCE_HASH_MISMATCH");
        var plan=geometry.Render(world,token);
        string name=HtmlEncoder.Default.Encode(capture.Source.MockupId);
        int buildings=world.Objects.Count(o=>o.VisualSource?.BuildingDefinitionId is not null);
        var focusIds=world.Request.Design!.TerrainRules.Where(r=>r.SurfaceMaterialId is not null).Select(r=>r.RegionId).ToHashSet(StringComparer.Ordinal);
        var focusOptions="<option value=\"\">Whole world</option>"+string.Concat(world.Request.Design.Blueprint.Regions.Where(r=>focusIds.Contains(r.Id)).Select(r=>
        {
            var points=r.Outline.Vertices;double x=points.Min(p=>p.X)-4,y=points.Min(p=>p.Y)-4,w=points.Max(p=>p.X)-x+4,h=points.Max(p=>p.Y)-y+4;
            string box=string.Join(" ",new[]{x,y,w,h}.Select(n=>n.ToString("0.###",CultureInfo.InvariantCulture)));
            string id=HtmlEncoder.Default.Encode(r.Id);return $"<option value=\"{id}\" data-box=\"{box}\">{id}</option>";
        }));
        return """
<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Generated city from capture</title><style>
*{box-sizing:border-box}body{margin:0;background:#14242d;color:#e4edf0;font:15px system-ui,sans-serif}header{padding:22px 28px;border-bottom:1px solid #40545f}h1{margin:0 0 10px;font-size:25px}p{line-height:1.5;color:#bfcfd7}.grid{display:block;max-width:1600px;margin:auto;padding:22px}.grid.compare{display:grid;grid-template-columns:1fr 1fr;gap:22px}section{min-width:0;background:#1c303c;padding:14px;border-radius:8px}h2{font-size:16px;margin:0 0 12px}img,svg{width:100%;height:auto;display:block}svg{background:#eddfbb}a{color:#85dcf2}.badge{color:#e7bc79;font-size:12px;letter-spacing:1px}button{padding:9px 16px;margin-right:8px;background:#2a4555;color:#e4edf0;border:1px solid #62818e;border-radius:5px;cursor:pointer}button:focus-visible{outline:2px solid #85dcf2}footer{padding:0 28px 24px}@media(max-width:900px){.grid.compare{grid-template-columns:1fr}}
</style><header><div class="badge">CAPTURE → C# ENGINE → VERIFIED WORLD DATA</div><h1>
"""+name+"""
 · generated city</h1><p>
"""+$"{buildings} buildings · {world.Request.Design!.RoadRules.Count} road paths · {world.Vegetation?.Count ?? 0} planned plants · siting {world.Siting?.Status}."+"""
</p><button type="button" data-view="engine">Generated map</button><button type="button" data-view="source">Source mockup</button><button type="button" data-view="compare">Compare</button><label>Focus generated region <select id="region-focus">
"""+focusOptions+"""
</select></label></header><div class="grid" id="views"><section id="engine"><h2>Actual generated ground geometry</h2>
"""+plan+"""
<p>Road surfaces, catalog-sized buildings, access/site bands and planting come from the exported world. Curves follow the captured polyline samples; no orthogonal snapping.</p></section><section id="source" hidden><h2>Original visual source</h2><img alt="Original source mockup, not production artwork" src="data:image/png;base64,
"""+Convert.ToBase64String(sourcePng)+"""
"><p>Source image coordinates are calibrated to an authored ground plane. Elevated roofs remain separate from ground footprints.</p></section></div><footer><p>This is an engine geometry review. GameArts production sprites, atlas approval and Godot runtime are separate gates. The named capture scope is compiled; the complete visual inventory remains partial.</p><a href="source-details.html">Detailed source extraction: trees, greens and components</a> · <a href="world.json">Generated world JSON</a> · <a href="plan.svg">Generated geometry map</a> · <a href="EXPORT_STATUS.md">Export status</a></footer><script>for(const button of document.querySelectorAll('[data-view]'))button.onclick=()=>{const view=button.dataset.view;document.getElementById('engine').hidden=view==='source';document.getElementById('source').hidden=view==='engine';document.getElementById('views').classList.toggle('compare',view==='compare')};</script></html>
"""+"<script>const focus=document.getElementById('region-focus'),map=document.querySelector('#engine svg'),full=map.getAttribute('viewBox');focus.onchange=()=>map.setAttribute('viewBox',focus.selectedOptions[0].dataset.box??full);</script>";
    }
}
