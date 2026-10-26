using System.Text.Json;
using WorldCreator.Core.Capture;
using WorldCreator.Core.Planning;

namespace WorldCreator.Core;

public sealed partial class StructureFirstPipeline
{
    /// <summary>Static structure layer only. Authorities must resolve actual GameArts
    /// lifecycle records; specification flags cannot supply approval.</summary>
    public async Task<StructureFirstProductionResult> BuildProductionAsync(WorldDocument world,
        StructureFirstSpecification specification, string trustedAssetRoot,
        IApprovedArtRegistry registry, IGameArtsApprovalAuthority assetApprovals,
        IGameArtsApprovalAuthority conceptApprovals, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(registry); ArgumentNullException.ThrowIfNull(assetApprovals);
        ArgumentNullException.ThrowIfNull(conceptApprovals);
        var draft = await BuildAsync(world, specification, trustedAssetRoot, token);
        var issues = draft.ProductionIssues.ToList();
        foreach (var concept in specification.Concepts ?? [])
            if (!await conceptApprovals.IsApprovedAsync(concept.MockupId, concept.Revision, token))
                issues.Add("STRUCTURE_SCOPE_CONCEPT_NOT_APPROVED:" + concept.ScopeId);
        var visual = world.Request.Design!.Blueprint;
        if (!await conceptApprovals.IsApprovedAsync(visual.MockupId, visual.ApprovedRevision, token))
            issues.Add("STRUCTURE_WORLD_VISUAL_NOT_APPROVED");
        var bindings = new List<WorldArtBinding>(); var frames = new List<StructureFramePlacement>();
        foreach (var group in draft.Assembly.Instances.GroupBy(i => i.StructureJobId))
        {
            token.ThrowIfCancellationRequested(); var art = group.First().Art;
            if (art.Production is not { } production) continue;
            if (new[] {production.AssetId,production.Revision,production.ComponentId,production.Facing,production.SheetId,production.FrameName}.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("STRUCTURE_PRODUCTION_REFERENCE_INVALID");
            string bundle = MockupCapturePipeline.ResolveSourcePath(trustedAssetRoot, production.BundlePath);
            if (new FileInfo(bundle).Length > 16777216) throw new ArgumentException("STRUCTURE_BUNDLE_SIZE_LIMIT");
            var manifest = await registry.ResolveAsync(production.AssetId, token);
            if (!WorldAssetDependencyManifestBuilder.IsComplete(manifest, production.AssetId) || manifest!.GameArtsRevision != production.Revision)
            { issues.Add("STRUCTURE_APPROVED_MANIFEST_MISSING:" + group.Key); continue; }
            var fileReport = await new GameArtsFileVerifier(trustedAssetRoot).VerifyAsync(manifest!, token);
            issues.AddRange(fileReport.Issues.Select(i => i.Code + ":" + group.Key));
            var gate = await new GameArtsProductionGate(assetApprovals).CheckAsync(bundle, production.Revision, token);
            issues.AddRange(gate.Issues.Select(i => i + ":" + group.Key));
            if (!gate.Ready || !fileReport.IsValid) continue;
            var native = await new GameArtsBundleReader().ReadAsync(bundle, token);
            if (native.Bundle!.AssetId != production.AssetId || native.Bundle.Components.Count != 1 ||
                native.Bundle.Components[0].Id != production.ComponentId || !native.Bundle.Directions.Contains(production.Facing))
            { issues.Add("STRUCTURE_STATIC_COMPONENT_RUNTIME_REQUIRED:" + group.Key); continue; }
            var sheet = manifest!.Sheets.SingleOrDefault(s => s.Id == production.SheetId && s.ComponentId == production.ComponentId && s.Directions.Contains(production.Facing));
            if (sheet is null) { issues.Add("STRUCTURE_SHEET_MISSING:" + group.Key); continue; }
            string atlasFile = MockupCapturePipeline.ResolveSourcePath(trustedAssetRoot, sheet.AtlasPath);
            string bundleAtlas = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(bundle)!, native.Bundle.Components[0].Atlases[production.Facing]));
            if (!string.Equals(atlasFile, bundleAtlas, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            { issues.Add("STRUCTURE_BUNDLE_SHEET_MISMATCH:" + group.Key); continue; }
            string imageFile = MockupCapturePipeline.ResolveSourcePath(trustedAssetRoot, sheet.ImagePath);
            _ = await ReadImageAsync(new(sheet.ImagePath, sheet.ImageSha256), trustedAssetRoot, token);
            if (new FileInfo(atlasFile).Length > 16777216) throw new ArgumentException("STRUCTURE_ATLAS_SIZE_LIMIT");
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(atlasFile, token));
            var atlas = document.RootElement;
            string atlasImage = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(atlasFile)!, atlas.GetProperty("image").GetString()!));
            if (!string.Equals(imageFile, atlasImage, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            { issues.Add("STRUCTURE_ATLAS_IMAGE_MISMATCH:" + group.Key); continue; }
            if (atlas.TryGetProperty("allow_near_key", out var relaxed) && relaxed.GetBoolean())
            { issues.Add("STRUCTURE_DIAGNOSTIC_ATLAS_REJECTED:" + group.Key); continue; }
            if (!atlas.TryGetProperty("pixels_per_meter", out var scaleValue) || !scaleValue.TryGetDouble(out double scale) || !double.IsFinite(scale) || scale <= 0)
            { issues.Add("STRUCTURE_ATLAS_SCALE_REQUIRED:" + group.Key); continue; }
            var selected = atlas.GetProperty("frames").EnumerateArray().Where(f => f.TryGetProperty("name", out var n) && n.GetString() == production.FrameName).ToArray();
            if (selected.Length != 1 || atlas.GetProperty("frames").GetArrayLength() != 1)
            { issues.Add("STRUCTURE_STATIC_FRAME_REQUIRED:" + group.Key); continue; }
            var frame = selected[0];
            if (art.Master is null || !frame.TryGetProperty("source_sha256", out var sourceHash) ||
                !string.Equals(sourceHash.GetString(), art.Master.Sha256, StringComparison.OrdinalIgnoreCase))
            { issues.Add("STRUCTURE_MASTER_ATLAS_LINEAGE_MISMATCH:" + group.Key); continue; }
            var pivot = frame.GetProperty("pivot"); var center = new PixelAnchor(pivot[0].GetInt32(), pivot[1].GetInt32());
            var rect = frame.GetProperty("rect"); var size = atlas.GetProperty("image_size");
            foreach (var item in group)
            {
                token.ThrowIfCancellationRequested(); var source = item.Placement.VisualSource!;
                string[] names = ["ground-nw","ground-ne","ground-se","ground-sw"];
                if (!frame.TryGetProperty("anchors", out var anchors) || source.Footprint!.Vertices.Count != 4 ||
                    names.Where((name,index) => !anchors.TryGetProperty(name, out var point) ||
                        Math.Abs((point[0].GetDouble()-center.X)/scale + source.PositionMeters.X-source.Footprint.Vertices[index].X)>1e-6 ||
                        Math.Abs((point[1].GetDouble()-center.Y)/scale + source.PositionMeters.Y-source.Footprint.Vertices[index].Y)>1e-6).Any())
                { issues.Add("STRUCTURE_GROUND_CALIBRATION_MISMATCH:" + item.Placement.Id); continue; }
                bindings.Add(new(item.Placement.Id, production.AssetId, production.ComponentId, production.Facing, 3, item.Placement.ParentObjectId));
                frames.Add(new(item.Placement.Id, production.AssetId, imageFile, sheet.ImageSha256, size[0].GetInt32(), size[1].GetInt32(),
                    rect[0].GetInt32(),rect[1].GetInt32(),rect[2].GetInt32(),rect[3].GetInt32(),scale,center,source.PositionMeters));
            }
        }
        if (issues.Count != 0) return new(draft, null, [], issues.Distinct(StringComparer.Ordinal).ToArray());
        var structurePlan = world.Design! with { Objects = draft.Assembly.Instances.Select(i => i.Placement).ToArray() };
        var approved = await new WorldArtAssembler(registry).AssembleAsync(structurePlan, bindings, token);
        issues.AddRange(approved.Issues.Select(i => i.Code + ":" + i.AssetId));
        return new(draft, approved.IsValid ? approved : null, approved.IsValid ? frames : [], issues);
    }
}
