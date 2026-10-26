using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    public interface IConstructionPlacementDefinition : IContentDefinition, IConstructionPlacementRule
    {
        string Space { get; }
    }
    public static class ConstructionPlacementContracts
    {
        public static void Validate(IConstructionPlacementDefinition rule, ContentResolver content)
        {
            if (rule == null || !ReferenceEquals(content.Def<IConstructionPlacementDefinition>(rule.Id), rule))
                throw content.Fail("construction placement requires its actual profile alias");
            content.Def<ISpatialSpaceDefinition>(rule.Space);
        }
        public static bool IsActual(IConstructionPlacementDefinition rule, World world)
            => rule is Def authored && authored.IsFrozen && world != null && world.Content.Owns(rule) &&
                ReferenceEquals(world.Content.Get<IConstructionPlacementDefinition>(rule.Id), rule) &&
                world.Space != null && rule.Space == world.Space.Id;
    }
    public sealed class TileConstructionPlacementEntry
    {
        public string Id { get; }
        public int Priority { get; }
        public Code Refusal { get; }
        private readonly TileConditionReferences _conditions;
        public IReadOnlyList<ITileConditionDefinition> Conditions => _conditions.Definitions;
        internal void Resolve(ContentResolver content) => _conditions.Resolve(content);
        internal void Validate(ContentResolver content, ITileSpaceDefinition space) => _conditions.Validate(content, space);
        internal TileConstructionPlacementEntry(JsonReader reader)
        {
            Id = reader.String("id"); Priority = reader.Int("priority"); string refusal = reader.String("refusal");
            try { ContractIdentity.ValidateId(Id, "placement rule"); ContractIdentity.ValidateId(refusal, "placement refusal"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            Refusal = new Code(refusal);
            _conditions = new TileConditionReferences(reader); reader.End();
        }
    }
    /// <summary>Authored stateless exclusions over native footprint samples; ConstructionService owns admission.</summary>
    public sealed class TileConstructionPlacementDef : Def, IConstructionPlacementDefinition
    {
        private readonly TileConstructionPlacementDeclaration _declaration = new TileConstructionPlacementDeclaration();
        public string Space => _declaration.Space.Id;
        public IReadOnlyList<TileConstructionPlacementEntry> Rules => _declaration.Rules;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.Validate(content);
        public Code CheckPlacement(World world, PlayerId company, EntityDef definition, SpatialPlacement placement)
            => ConstructionPlacementContracts.IsActual(this, world)
                ? _declaration.CheckPlacement(world, company, definition, placement) : Codes.InvalidArgument;
    }
    /// <summary>Sole native footprint exclusion authoring and interpretation, never placement state.</summary>
    public sealed class TileConstructionPlacementDeclaration
    {
        private string _space;
        private ITileSpaceDefinition _resolved;
        public ITileSpaceDefinition Space => _resolved;
        public IReadOnlyList<TileConstructionPlacementEntry> Rules { get; private set; }
        public void Read(JsonReader reader)
        {
            _space = reader.String("space"); var entries = reader.Array("rules") ?? throw reader.Error("explicit placement rules required, including []");
            var rules = new TileConstructionPlacementEntry[entries.Count]; var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                var rule = rules[i] = new TileConstructionPlacementEntry(new JsonReader(entries[i], reader.Where + ".rules[" + i + "]"));
                if (!ids.Add(rule.Id)) throw reader.Error("duplicate placement rule identity: " + rule.Id);
            }
            Array.Sort(rules, (a, b) => { int priority = b.Priority.CompareTo(a.Priority); return priority != 0 ? priority : StringComparer.Ordinal.Compare(a.Id, b.Id); });
            Rules = Array.AsReadOnly(rules);
        }
        public void Resolve(ContentResolver content)
        {
            _resolved = content.Def<ITileSpaceDefinition>(_space);
            foreach (var rule in Rules) rule.Resolve(content);
        }
        public void Validate(ContentResolver content)
        {
            foreach (var rule in Rules) rule.Validate(content, _resolved);
        }
        public Code CheckPlacement(World world, PlayerId company, EntityDef definition, SpatialPlacement placement)
        {
            if (world == null || !world.Content.Owns(definition) || Space == null || !world.Content.Owns(Space) ||
                !(world.Space is ITileSpatialSpace map) || map.Id != Space.Id || !(placement is TilePlacement tile) || tile.Space != Space.Id)
                return Codes.InvalidArgument;
            if (!world.Players.IsSeat(company)) return Codes.NotSeated;
            var presence = definition.Component<ITilePresenceDefinition>();
            if (presence == null || presence.Space != Space.Id) return Codes.NotAllowed;
            if (tile.Origin.X < 0 || tile.Origin.Y < 0 || (long)tile.Origin.X + presence.Width > map.Width || (long)tile.Origin.Y + presence.Height > map.Height)
                return Codes.OutOfBounds;
            foreach (var rule in Rules)
                for (int y = tile.Origin.Y; y < tile.Origin.Y + presence.Height; y++)
                    for (int x = tile.Origin.X; x < tile.Origin.X + presence.Width; x++)
                    {
                        bool matches = true;
                        foreach (var condition in rule.Conditions)
                            if (!TileConditionContracts.Matches(condition, world, map, new TileCoord(x, y))) { matches = false; break; }
                        if (matches) return rule.Refusal;
                    }
            return Code.None;
        }
    }
    /// <summary>Optional content selection only: no second placement coordinator, state, scheduler or clock.</summary>
    public sealed class TileConstructionRulesModule : GameModule
    {
        public const string ModuleId = "sim.tile-construction-rules";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(ConstructionModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types) => types.Section<TileConstructionPlacementDef>("tile_construction_rules");
        public override void Compose(WorldComposer composer)
        {
            if (!(composer.World.Space is ITileSpatialSpace)) throw new ContentException("selected tile construction rules require an actual tile space");
            var settings = composer.Scenario.SettingsFor(ModuleId) ?? throw new ContentException("selected tile construction rules require explicit profile settings");
            string id = settings.String("profile"); settings.End();
            var profile = composer.Content.Get<IConstructionPlacementDefinition>(id);
            if (!EntityObservation.ReadProvider(composer.World, profile, null, () => ConstructionPlacementContracts.IsActual(profile, composer.World), composer.World.Space))
                throw new ContentException("construction rule profile belongs to another actual space: " + id);
            composer.Services.Bind<IConstructionPlacementRule>(profile);
        }
    }
}
