using GameFrameWork.Core.Planning;

namespace GameFrameWork.Core;

/// <summary>Style/material ID is independent from an approved GameArts production asset ID.</summary>
public sealed record TerrainMaterialBinding(string MaterialId, string AssetId,
    string ComponentId, string Facing);
public sealed record BoundTerrainPiece(StyledTerrainPiece Terrain, TerrainMaterialBinding Material,
    ApprovedArtManifest Manifest);
public sealed record TerrainArtAssembly(IReadOnlyList<BoundTerrainPiece> Pieces,
    IReadOnlyList<string> Issues)
{
    public bool IsValid => Issues.Count == 0;
}
/// <summary>Resolves each purposefully designed terrain fill to an approved art asset.
/// Does not randomly choose textures or change polygon geometry.</summary>
public sealed class TerrainArtAssembler(IApprovedArtRegistry registry)
{
    public async Task<TerrainArtAssembly> AssembleAsync(
        StyledTerrainComposition terrain, IReadOnlyList<TerrainMaterialBinding> materials,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(materials);
        var errors = new List<string>(terrain.Diagnostics);
        var materialIndex = new Dictionary<string, TerrainMaterialBinding>(StringComparer.Ordinal);
        foreach (var material in materials)
            if (string.IsNullOrWhiteSpace(material.MaterialId) ||
                !materialIndex.TryAdd(material.MaterialId, material))
                errors.Add("MATERIAL_BINDING_INVALID_OR_DUPLICATE:" + material.MaterialId);
        var approved = new Dictionary<string, ApprovedArtManifest?>(StringComparer.Ordinal);
        var output = new List<BoundTerrainPiece>();
        foreach (var piece in terrain.Pieces)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!materialIndex.TryGetValue(piece.MaterialId, out var binding))
            {
                errors.Add("TERRAIN_MATERIAL_UNBOUND:" + piece.Id);
                continue;
            }
            if (string.IsNullOrWhiteSpace(binding.AssetId) ||
                string.IsNullOrWhiteSpace(binding.ComponentId) ||
                string.IsNullOrWhiteSpace(binding.Facing))
            {
                errors.Add("TERRAIN_ART_BINDING_INCOMPLETE:" + piece.Id);
                continue;
            }
            if (!approved.TryGetValue(binding.AssetId, out var manifest))
            {
                manifest = await registry.ResolveAsync(binding.AssetId, cancellationToken);
                approved[binding.AssetId] = manifest;
            }
            if (manifest is null || manifest.ApprovalState != ArtApprovalState.Approved ||
                manifest.AssetId != binding.AssetId)
            {
                errors.Add("TERRAIN_ART_NOT_APPROVED:" + piece.Id);
                continue;
            }
            if (!manifest.Sheets.Any(sheet => sheet.ComponentId == binding.ComponentId &&
                sheet.Directions.Contains(binding.Facing, StringComparer.Ordinal)))
            {
                errors.Add("TERRAIN_ART_VIEW_MISSING:" + piece.Id);
                continue;
            }
            output.Add(new(piece, binding, manifest));
        }
        return errors.Count == 0 ? new(output, []) : new([], errors);
    }
}
