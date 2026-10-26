using System.Collections.Immutable;
using ScenarioEngine.Contracts;

namespace ScenarioEngine.Core;

/// <summary>Explicit trusted registration; no executable code is loaded from scenario data.</summary>
public sealed class GameLayerRegistry : IGameLayerRegistry
{
    private readonly ImmutableDictionary<LayerIdentity, IGameLayer> _layers;
    public GameLayerRegistry(IEnumerable<IGameLayer> layers)
    {
        var builder = ImmutableDictionary.CreateBuilder<LayerIdentity, IGameLayer>();
        foreach (var layer in layers)
        {
            if (string.IsNullOrWhiteSpace(layer.Id) || string.IsNullOrWhiteSpace(layer.Version))
                throw new ArgumentException("A layer requires an ID and exact version.", nameof(layers));
            if (layer.Capabilities.Facts.Keys.Concat(layer.Capabilities.Operations.Keys)
                .Concat(layer.Capabilities.Effects.Keys).Concat(layer.Capabilities.Observations.Keys)
                .Any(k => !k.StartsWith(layer.Id + ".", StringComparison.Ordinal)))
                throw new ArgumentException("A layer must namespace its registered capabilities.", nameof(layers));
            if (!builder.TryAdd(new(layer.Id, layer.Version), layer))
                throw new ArgumentException("Duplicate layer identity.", nameof(layers));
        }
        _layers = builder.ToImmutable();
    }
    public bool TryResolve(string id, string exactVersion, out IGameLayer layer) =>
        _layers.TryGetValue(new(id, exactVersion), out layer!);
    public ImmutableArray<LayerIdentity> ListInstalled() =>
        [.. _layers.Keys.OrderBy(k => k.Id, StringComparer.Ordinal).ThenBy(k => k.Version, StringComparer.Ordinal)];
}
