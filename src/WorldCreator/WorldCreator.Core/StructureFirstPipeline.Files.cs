using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using WorldCreator.Core.Capture;

namespace WorldCreator.Core;

public sealed partial class StructureFirstPipeline
{
    public static async Task<T> ReadJsonAsync<T>(string path, long maximumBytes = 16777216, CancellationToken token = default)
    {
        await using var stream = File.OpenRead(path);
        if (stream.Length <= 0 || stream.Length > maximumBytes) throw new ArgumentException("STRUCTURE_JSON_SIZE_LIMIT");
        var options = WorldJson.CreateOptions();
        options.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        options.RespectRequiredConstructorParameters = true;
        return await JsonSerializer.DeserializeAsync<T>(stream, options, token) ?? throw new ArgumentException("STRUCTURE_JSON_REQUIRED");
    }

    public static async Task SaveSpecificationAsync(StructureFirstSpecification specification, string newFile, CancellationToken token = default)
    {
        string full = Path.GetFullPath(newFile);
        if (File.Exists(full) || Directory.Exists(full)) throw new IOException("STRUCTURE_SPECIFICATION_EXISTS");
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        string staging = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(staging, JsonSerializer.Serialize(specification, WorldJson.CreateOptions(true)), token);
            token.ThrowIfCancellationRequested(); File.Move(staging, full);
        }
        finally { if (File.Exists(staging)) File.Delete(staging); }
    }

    private static async Task<byte[]> ReadImageAsync(StructureImageReference image, string root, CancellationToken token)
    {
        if (image is null || image.Sha256 is not { Length: 64 } || !image.Sha256.All(Uri.IsHexDigit))
            throw new ArgumentException("STRUCTURE_IMAGE_HASH_INVALID");
        string full = MockupCapturePipeline.ResolveSourcePath(root, image.File);
        await using var stream = File.OpenRead(full);
        if (stream.Length is < 33 or > 67108864) throw new ArgumentException("STRUCTURE_IMAGE_SIZE_LIMIT");
        var bytes = new byte[(int)stream.Length]; await stream.ReadExactlyAsync(bytes, token);
        if (!Convert.ToHexStringLower(SHA256.HashData(bytes)).Equals(image.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("STRUCTURE_IMAGE_HASH_MISMATCH");
        if (!bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) || !bytes.AsSpan(12,4).SequenceEqual("IHDR"u8))
            throw new ArgumentException("STRUCTURE_IMAGE_PNG_REQUIRED");
        uint w = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16,4)), h = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20,4));
        if (w is 0 or > 16384 || h is 0 or > 16384 || (long)w*h > 100000000) throw new ArgumentException("STRUCTURE_IMAGE_DIMENSIONS_INVALID");
        return bytes; // Header and hash evidence only; no decoded-pixel/approval claim.
    }

    public async Task<StructureFirstResult> ExportAsync(WorldDocument world, StructureFirstSpecification specification,
        string assetRoot, string newDirectory, CancellationToken token = default)
    {
        var result = await BuildAsync(world, specification, assetRoot, token);
        string target = Path.GetFullPath(newDirectory);
        if (Directory.Exists(target) || File.Exists(target)) throw new IOException("STRUCTURE_OUTPUT_EXISTS");
        string parent = Path.GetDirectoryName(target)!; Directory.CreateDirectory(parent);
        string staging = Path.Combine(parent, ".structure-pipeline-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            Directory.CreateDirectory(Path.Combine(staging, "art"));
            Directory.CreateDirectory(Path.Combine(staging, "scope-layouts"));
            var copied = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            async Task<StructureImageReference> Copy(StructureImageReference source)
            {
                var bytes = await ReadImageAsync(source, assetRoot, token);
                string relative = "art/" + source.Sha256.ToLowerInvariant() + ".png";
                if (copied.TryAdd(source.Sha256, relative)) await File.WriteAllBytesAsync(Path.Combine(staging, relative), bytes, token);
                return source with { File = relative };
            }
            var entries = new List<StructureArtEntry>();
            foreach (var art in specification.Art)
                entries.Add(art.Master is null ? art : art with { Master = await Copy(art.Master) });
            var concepts = new List<StructureScopeConcept>();
            foreach (var concept in specification.Concepts ?? []) concepts.Add(concept with { Image = await Copy(concept.Image) });
            var portable = specification with { Art = entries, Concepts = concepts };
            var byId = entries.ToDictionary(e => JobId(e.DefinitionId, e.VariantId), StringComparer.Ordinal);
            var exported = result with { Assembly = result.Assembly with {
                Instances = result.Assembly.Instances.Select(i => i with { Art = byId[i.StructureJobId] }).ToArray() } };
            await Json("world.json", world);
            await Json("library.json", portable);
            await Json("instance-assembly.json", exported.Assembly);
            await Json("pipeline.json", exported);
            await Json("structure-jobs.json", result.StructureJobs);
            await Json("mockup-jobs.json", result.MockupJobs);
            string layout = new WorldPlanSvgRenderer().Render(world, token);
            await File.WriteAllTextAsync(Path.Combine(staging, "layout.svg"), layout, token);
            var regions = world.Request.Design!.Blueprint.Regions.ToDictionary(r => r.Id, StringComparer.Ordinal);
            foreach (var scope in specification.Scopes)
            {
                token.ThrowIfCancellationRequested();
                bool Under(string id)
                {
                    var item = specification.Scopes.Single(s => s.Id == id);
                    while (true) { if (item.Id == scope.Id) return true; if (item.ParentId is null) return false; item = specification.Scopes.Single(s => s.Id == item.ParentId); }
                }
                var polygons = specification.Scopes.Where(s => Under(s.Id)).SelectMany(s => s.RegionIds)
                    .Distinct().Select(id => regions[id].Outline).ToArray();
                if (polygons.Length == 0) continue;
                double x = polygons.SelectMany(p => p.Vertices).Min(p => p.X), y = polygons.SelectMany(p => p.Vertices).Min(p => p.Y);
                double w = polygons.SelectMany(p => p.Vertices).Max(p => p.X)-x, h = polygons.SelectMany(p => p.Vertices).Max(p => p.Y)-y;
                var doc = XDocument.Parse(layout); var svg = doc.Root!;
                if (scope.Kind != VisualScopeKind.World)
                {
                    svg.SetAttributeValue("viewBox", FormattableString.Invariant($"{x} {y} {w} {h}"));
                    svg.SetAttributeValue("height", Math.Ceiling(1536*h/w).ToString(CultureInfo.InvariantCulture));
                }
                await File.WriteAllTextAsync(Path.Combine(staging, "scope-layouts", scope.Id + ".svg"), doc.ToString(), token);
            }
            // A canonical GameArts frames job for reference presentation, never production acceptance.
            await Json("reference-board-job.json", new { name = "structure-reference-board", asset_id = specification.Id,
                columns = 3, padding = 8, spacing = 8, allow_near_key = true,
                frames = entries.Where(e => e.Master is not null).Select(e => new { name = JobId(e.DefinitionId,e.VariantId), file = e.Master!.File }) });
            await File.WriteAllTextAsync(Path.Combine(staging, "review.html"), ReviewHtml(exported, portable, layout), token);
            await File.WriteAllTextAsync(Path.Combine(staging, "STATUS.md"),
                $"Draft structure pipeline: {result.Assembly.Instances.Count} exact engine instances. Coverage complete: {result.CoverageComplete}.\n" +
                "Image jobs require a host agent and canonical GameArts tools. This packet grants no approval or runtime readiness. " +
                "The reference-board job permits near-key pixels only for diagnostic presentation; production uses strict GameArts gates. " +
                "library.json production bundle references retain their original trusted-root context; use the original library/root for production verification.\n" +
                string.Join("\n", result.ProductionIssues), token);
            token.ThrowIfCancellationRequested();
            if (WorldDigest(world) != specification.WorldSha256) throw new ArgumentException("STRUCTURE_WORLD_CHANGED_DURING_EXPORT");
            Directory.Move(staging, target);
            return result;
            async Task Json<T>(string name, T value) => await File.WriteAllTextAsync(Path.Combine(staging,name),
                JsonSerializer.Serialize(value, WorldJson.CreateOptions(true)), token);
        }
        finally
        {
            if (Directory.Exists(staging) && Path.GetDirectoryName(Path.GetFullPath(staging)) == parent &&
                Path.GetFileName(staging).StartsWith(".structure-pipeline-", StringComparison.Ordinal)) Directory.Delete(staging, true);
        }
    }

    private static string ReviewHtml(StructureFirstResult result, StructureFirstSpecification spec, string svg)
    {
        string E(string? s) => WebUtility.HtmlEncode(s ?? "");
        var html = new StringBuilder("<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width\"><title>Structures-first review</title><style>body{font:16px system-ui;background:#12232b;color:#e8ecec;margin:24px}a{color:#83dccc}section{display:flex;gap:24px;align-items:flex-start}svg{width:65vw;max-height:80vh;background:#eddfbb}aside{width:28vw}img{max-width:100%}button{margin:3px;padding:6px}li{margin:8px}</style>");
        html.Append($"<h1>{E(spec.Id)}</h1><p>{result.Assembly.Instances.Count} exact C# structure instances · coverage {(result.CoverageComplete ? "complete for declared plan" : "PARTIAL / planning required")} · candidate art, no production approval.</p><p>World → settlement → district → site. Click a building footprint or select an instance to inspect its source artwork.</p><ul>");
        foreach (var scope in result.Coverage) html.Append($"<li>{E(scope.ScopeId)}: {scope.StructureCount} structures; {(scope.Complete ? "covered" : "incomplete")} {E(string.Join("; ",scope.Issues))}</li>");
        html.Append("</ul><section><div id=\"map\">").Append(svg).Append("</div><aside><p id=\"selected\">Select a structure.</p><img id=\"art\" alt=\"Selected structure candidate\" hidden><p id=\"error\" role=\"alert\"></p><div id=\"choices\">");
        foreach (var item in result.Assembly.Instances) html.Append($"<button data-id=\"{E(item.Placement.Id)}\">{E(item.Placement.Id)}</button>");
        html.Append("</div></aside></section><p><a href=\"instance-assembly.json\">Exact instance assembly</a> · <a href=\"library.json\">Library</a> · <a href=\"mockup-jobs.json\">Scope mockup jobs</a> · <a href=\"STATUS.md\">Remaining gates</a></p><script>const entries=");
        html.Append(JsonSerializer.Serialize(result.Assembly.Instances.Select(i => new { id=i.Placement.Id, job=i.StructureJobId, file=i.Art.Master?.File })));
        html.Append(";function select(id){const row=entries.find(x=>x.id===id);if(!row)return;document.getElementById('selected').textContent=row.id+' · '+row.job;const img=document.getElementById('art');document.getElementById('error').textContent='';img.hidden=!row.file;if(row.file)img.src=row.file;else document.getElementById('error').textContent='Structure master missing';}document.getElementById('choices').onclick=e=>{const b=e.target.closest('button');if(b)select(b.dataset.id);};document.getElementById('map').onclick=e=>{const t=e.target.closest('polygon')?.querySelector('title');if(t)select(t.textContent);};document.getElementById('art').onerror=()=>{document.getElementById('error').textContent='Candidate image could not be loaded';};</script></html>");
        return html.ToString();
    }
}
