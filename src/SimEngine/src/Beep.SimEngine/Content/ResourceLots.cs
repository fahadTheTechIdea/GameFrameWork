using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Content
{
    /// <summary>Typed authored quality axis for one actual resource.</summary>
    public interface IResourceQualityDefinition : IContentDefinition
    {
        ResourceId Resource { get; }
        IUnitDefinition Unit { get; }
        Fix Minimum { get; }
        Fix Maximum { get; }
    }
    public interface IResourceLotDefinition : IContentDefinition
    {
        ResourceId Resource { get; }
        IReadOnlyDictionary<string, Fix> Qualities { get; }
    }
    public readonly struct ResourceQualityRange
    {
        public IResourceQualityDefinition Quality { get; }
        public Fix Minimum { get; }
        public Fix Maximum { get; }
        public ResourceQualityRange(IResourceQualityDefinition quality, Fix minimum, Fix maximum)
        { Quality = quality; Minimum = minimum; Maximum = maximum; }
    }
    /// <summary>Authored eligibility, not stock or a reservation ledger.</summary>
    public interface IResourceLotSelectionDefinition : IContentDefinition
    {
        ResourceId Resource { get; }
        Code Lot { get; }
        IReadOnlyList<ResourceQualityRange> Qualities { get; }
    }
    public sealed class ResourceLotSelectionDeclaration
    {
        private readonly string _resource, _lot;
        private readonly SortedDictionary<string, (Fix Minimum, Fix Maximum)> _ranges = new SortedDictionary<string, (Fix, Fix)>(StringComparer.Ordinal);
        public ResourceId Resource { get; private set; }
        public Code Lot { get; private set; }
        public IReadOnlyList<ResourceQualityRange> Qualities { get; private set; }
        private ResourceLotSelectionDeclaration(JsonReader reader)
        {
            _resource = reader.String("resource");
            if (reader.Has("lot")) { var lot = reader.Value("lot"); if (lot.Kind != JsonKind.Null) _lot = reader.String("lot"); }
            var qualities = reader.Child("qualities");
            if (qualities != null)
            {
                for (int i = 0; i < qualities.Raw.Count; i++)
                {
                    string id = qualities.Raw.KeyAt(i); var range = qualities.Child(id);
                    if (!range.Has("minimum") || !range.Has("maximum")) throw range.Error("quality selection requires explicit bounds");
                    _ranges.Add(id, (range.Fix("minimum"), range.Fix("maximum"))); range.End();
                }
                qualities.End();
            }
            if (_lot == null && _ranges.Count == 0) throw reader.Error("lot selection requires an exact lot or quality ranges");
        }
        public static ResourceLotSelectionDeclaration Read(JsonReader reader) => new ResourceLotSelectionDeclaration(reader);
        public void Resolve(ContentResolver content)
        {
            Resource = content.Resource(_resource); Lot = _lot == null ? Code.None : new Code(content.Def<IResourceLotDefinition>(_lot).Id);
            var ranges = new List<ResourceQualityRange>();
            foreach (var pair in _ranges) ranges.Add(new ResourceQualityRange(content.Def<IResourceQualityDefinition>(pair.Key), pair.Value.Minimum, pair.Value.Maximum));
            Qualities = ranges.AsReadOnly();
            if (!ResourceLotContentContracts.ValidSelection(content.Content, Resource, Lot, Qualities)) throw content.Fail("invalid resource lot/quality selection");
        }
    }
    public sealed class ResourceLotSelectionDef : Def, IResourceLotSelectionDefinition
    {
        private ResourceLotSelectionDeclaration _declaration;
        public ResourceId Resource => _declaration.Resource;
        public Code Lot => _declaration.Lot;
        public IReadOnlyList<ResourceQualityRange> Qualities => _declaration.Qualities;
        protected override void ReadCore(JsonReader reader) => _declaration = ResourceLotSelectionDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class ResourceQualityDeclaration
    {
        private readonly string _resource, _unit;
        public ResourceId Resource { get; private set; }
        public IUnitDefinition Unit { get; private set; }
        public Fix Minimum { get; }
        public Fix Maximum { get; }
        private ResourceQualityDeclaration(JsonReader reader)
        {
            _resource = reader.String("resource"); _unit = reader.String("unit");
            Minimum = reader.Fix("minimum"); Maximum = reader.Fix("maximum");
            if (!reader.Has("minimum") || !reader.Has("maximum") || Minimum > Maximum)
                throw reader.Error("quality requires an explicit ordered range");
        }
        public static ResourceQualityDeclaration Read(JsonReader reader) => new ResourceQualityDeclaration(reader);
        public void Resolve(ContentResolver content)
        {
            Resource = content.Resource(_resource); Unit = content.Def<IUnitContentDefinition>(_unit);
            if (UnitConversion.ValidateAmount(Unit, Minimum) != UnitConversionStatus.Converted ||
                UnitConversion.ValidateAmount(Unit, Maximum) != UnitConversionStatus.Converted)
                throw content.Fail("quality bounds must respect the authored unit");
        }
    }
    public sealed class ResourceQualityDef : Def, IResourceQualityDefinition
    {
        private ResourceQualityDeclaration _declaration;
        public ResourceId Resource => _declaration.Resource;
        public IUnitDefinition Unit => _declaration.Unit;
        public Fix Minimum => _declaration.Minimum;
        public Fix Maximum => _declaration.Maximum;
        protected override void ReadCore(JsonReader reader) => _declaration = ResourceQualityDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class ResourceLotDeclaration
    {
        private readonly string _resource;
        public ResourceId Resource { get; private set; }
        public IReadOnlyDictionary<string, Fix> Qualities { get; }
        private ResourceLotDeclaration(JsonReader reader)
        {
            _resource = reader.String("resource"); var rows = reader.Child("qualities");
            if (rows == null || rows.Raw.Count == 0) throw reader.Error("lot qualities must be explicit and nonempty");
            var values = new SortedDictionary<string, Fix>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Raw.Count; i++) { string id = rows.Raw.KeyAt(i); values.Add(id, rows.Fix(id)); }
            rows.End(); Qualities = new System.Collections.ObjectModel.ReadOnlyDictionary<string, Fix>(values);
        }
        public static ResourceLotDeclaration Read(JsonReader reader) => new ResourceLotDeclaration(reader);
        public void Resolve(ContentResolver content)
        {
            Resource = content.Resource(_resource);
            foreach (var pair in Qualities)
            {
                var quality = content.Def<IResourceQualityDefinition>(pair.Key);
                if (quality.Resource != Resource || quality.Unit == null || pair.Value < quality.Minimum || pair.Value > quality.Maximum ||
                    UnitConversion.ValidateAmount(quality.Unit, pair.Value) != UnitConversionStatus.Converted)
                    throw content.Fail("lot quality disagrees with its resource, unit or bounds");
            }
        }
    }
    public sealed class ResourceLotDef : Def, IResourceLotDefinition
    {
        private ResourceLotDeclaration _declaration;
        public ResourceId Resource => _declaration.Resource;
        public IReadOnlyDictionary<string, Fix> Qualities => _declaration.Qualities;
        protected override void ReadCore(JsonReader reader) => _declaration = ResourceLotDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public static class ResourceLotContentContracts
    {
        public static bool ValidSelection(ResolvedContent content, ResourceId resource, Code lot, IReadOnlyList<ResourceQualityRange> ranges)
        {
            if (content.Resource(resource) == null || ranges == null || lot.IsNone && ranges.Count == 0) return false;
            if (!lot.IsNone && content.Find<IResourceLotDefinition>(lot.Id)?.Resource != resource) return false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var range in ranges)
            {
                var axis = range.Quality;
                if (axis == null || !ReferenceEquals(content.Find<IResourceQualityDefinition>(axis.Id), axis) || !seen.Add(axis.Id) ||
                    axis.Resource != resource || range.Minimum > range.Maximum || range.Minimum < axis.Minimum || range.Maximum > axis.Maximum ||
                    UnitConversion.ValidateAmount(axis.Unit, range.Minimum) != UnitConversionStatus.Converted ||
                    UnitConversion.ValidateAmount(axis.Unit, range.Maximum) != UnitConversionStatus.Converted) return false;
                if (!lot.IsNone && (!content.Find<IResourceLotDefinition>(lot.Id).Qualities.TryGetValue(axis.Id, out Fix value) || value < range.Minimum || value > range.Maximum)) return false;
            }
            return true;
        }
        public static void Validate(World world)
        {
            foreach (var selection in world.Content.All<IResourceLotSelectionDefinition>())
                EntityObservation.ReadProvider(world, selection, null, () => {
                    if (!world.Content.Owns(selection) || !ReferenceEquals(world.Content.Find<IResourceLotSelectionDefinition>(selection.Id), selection) ||
                        !ValidSelection(world.Content, selection.Resource, selection.Lot, selection.Qualities))
                        throw new ContentException(selection.Id + ": selection requires actual resource/lot/quality references and valid bounds");
                    return true;
                });
            foreach (var axis in world.Content.All<IResourceQualityDefinition>())
                EntityObservation.ReadProvider(world, axis, null, () => {
                    if (!world.Content.Owns(axis) || !ReferenceEquals(world.Content.Find<IResourceQualityDefinition>(axis.Id), axis) ||
                        world.Content.Resource(axis.Resource) == null || !(axis.Unit is IUnitContentDefinition unit) ||
                        !world.Content.Owns(unit) || !ReferenceEquals(world.Content.Find<IUnitContentDefinition>(unit.Id), unit) ||
                        axis.Minimum > axis.Maximum || UnitConversion.ValidateAmount(unit, axis.Minimum) != UnitConversionStatus.Converted ||
                        UnitConversion.ValidateAmount(unit, axis.Maximum) != UnitConversionStatus.Converted)
                        throw new ContentException(axis.Id + ": quality requires actual resource/unit aliases and valid bounds");
                    return true;
                });
            foreach (var lot in world.Content.All<IResourceLotDefinition>())
                EntityObservation.ReadProvider(world, lot, null, () => {
                    if (!world.Content.Owns(lot) || !ReferenceEquals(world.Content.Find<IResourceLotDefinition>(lot.Id), lot) ||
                        world.Content.Resource(lot.Resource) == null || lot.Qualities == null || lot.Qualities.Count == 0)
                        throw new ContentException(lot.Id + ": lot requires actual resource and explicit qualities");
                    foreach (var value in lot.Qualities)
                    {
                        var axis = world.Content.Find<IResourceQualityDefinition>(value.Key);
                        if (axis == null || axis.Resource != lot.Resource || value.Value < axis.Minimum || value.Value > axis.Maximum ||
                            UnitConversion.ValidateAmount(axis.Unit, value.Value) != UnitConversionStatus.Converted)
                            throw new ContentException(lot.Id + ": lot quality violates its actual resource/unit/bounds");
                    }
                    return true;
                });
        }
    }
}
