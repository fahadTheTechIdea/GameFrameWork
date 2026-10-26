using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.ExtensionProof
{
    // A real public engine module. Fault delegates/call counters are transient test instrumentation.
    public sealed class ViewObservationPartDef : ComponentDef<ViewObservationPart>
    {
        public Fix Initial { get; private set; }
        public Fix PerUpdate { get; private set; }
        protected override void ReadCore(JsonReader reader)
        { Initial = reader.Fix("initial"); PerUpdate = reader.Fix("per_update"); }
    }
    public sealed class ViewObservationPart : Component<ViewObservationPartDef>, IInspectable
    {
        public static readonly Code ValueFacet = new Code("proof.view-value");
        public Fix Value;
        public int Reads;
        public Action<World, PlayerId, Entity> OnInspect;
        public override void OnCreate() => Value = Def.Initial;
        public override void Sync(IStateSync sync) => sync.Fix(ref Value);
        public void Inspect(World world, PlayerId viewer, IList<Facet> into)
        {
            Reads++;
            if (viewer != PlayerId.None && viewer != Entity.Owner) return;
            into.Add(new Facet(ValueFacet, Value));
            OnInspect?.Invoke(world, viewer, Entity);
        }
    }
    public interface IObservationAlias { }
    public sealed class ViewObservationVisibility : IVisibility, ISpatialSampleVisibility, IPersistentService, IObservationAlias
    {
        public string Id => "proof.view-visibility";
        public int StateSchemaVersion => 1;
        public int Value, EntityReads, ContentReads;
        public Action<World, PlayerId, Entity> OnEntity;
        public Action<World, PlayerId, IContentDefinition> OnContent;
        public Func<World, PlayerId, IContentDefinition, bool> ContentAllowed;
        public Func<World, PlayerId, Entity, bool> EntityAllowed;
        public Action<World, PlayerId, ISpatialLayerDefinition, SpatialSnapshot> OnSample;
        public Func<PlayerId, SpatialSnapshot, bool> SampleAllowed;
        public Action<World, PlayerId, ISpatialLayerDefinition, IList<string>> OnKeys;
        public bool CanSee(World world, PlayerId viewer, Entity entity)
        { EntityReads++; OnEntity?.Invoke(world, viewer, entity); return EntityAllowed?.Invoke(world, viewer, entity) ?? entity.Owner == viewer; }
        public bool CanSeeContent(World world, PlayerId viewer, IContentDefinition definition)
        { ContentReads++; OnContent?.Invoke(world, viewer, definition); return ContentAllowed?.Invoke(world, viewer, definition) ?? definition.Id != "secret"; }
        public bool CanSeeSample(World world, PlayerId viewer, ISpatialLayerDefinition layer, SpatialSnapshot sample)
        { OnSample?.Invoke(world, viewer, layer, sample); return SampleAllowed?.Invoke(viewer, sample) ?? true; }
        public void CopyObservationKeys(World world, PlayerId viewer, ISpatialLayerDefinition layer, IList<string> destination)
        { destination.Clear(); OnKeys?.Invoke(world, viewer, layer, destination); }
        public void Sync(IStateSync sync) => sync.Int(ref Value);
    }
    public sealed class ViewObservationReadModel
    {
        private readonly PlayerView _view;
        public ViewObservationReadModel(World world, PlayerId viewer) => _view = world.ViewFor(viewer);
        public bool Read(EntityId id, out Fix value)
        {
            var rows = new List<Facet>();
            if (_view.Inspect(id, rows))
                foreach (Facet row in rows) if (row.Key == ViewObservationPart.ValueFacet) { value = row.Value; return true; }
            value = default; return false;
        }
    }
    public sealed class ViewObservationSystem : GameSystem
    {
        public override string Id => "proof.view-update";
        public override int StateSchemaVersion => 1;
        public override void OnNewGame(World world)
        {
            var reader = world.Content.Get<ScenarioDef>(world.Settings.Scenario).SettingsFor("proof.views");
            string prototype = reader.String("prototype"); int x = reader.Int("x"), y = reader.Int("y"), spacing = reader.Int("spacing"); reader.End();
            for (int p = 0; p < world.Players.Count; p++)
                ((TileMap)world.Space).SpawnAt(world, world.Content.Get<EntityDef>(prototype), new PlayerId(p), new TileCoord(x + p * spacing, y));
        }
        public override void Update(World world, TickInfo tick)
        {
            foreach (Entity entity in world.Entities.All)
                if (entity.Alive && entity.Get<ViewObservationPart>() is ViewObservationPart part)
                { part.Value += part.Def.PerUpdate; entity.Touch(); }
        }
    }
    public sealed class ViewObservationModule : GameModule
    {
        private World _world;
        private ViewObservationVisibility _visibility;
        public int SpatialReadCalls { get; private set; }
        // Negative mod instrumentation stays here, never in the player-simulation driver.
        public void ArmSpatialRead(string surface, string mode)
        {
            if (mode != "write" && mode != "restore" && mode != "throw") throw new ArgumentException(nameof(mode));
            void Mutate(World world)
            {
                SpatialReadCalls++;
                var layer = ((TileMap)world.Space).Layers[0];
                var tile = new TileCoord(0, 0); Fix prior = layer.GetSample(tile);
                layer.SetSample(tile, prior == Fix.Zero ? Fix.One : Fix.Zero);
                if (mode == "restore") layer.SetSample(tile, prior);
                if (mode == "throw") throw new InvalidOperationException("proof.public-read-original-error");
            }
            switch (surface)
            {
                case "factory": OnFactory = (world, viewer) => Mutate(world); break;
                case "visibility": _visibility.OnEntity = (world, viewer, entity) => Mutate(world); break;
                case "inspect":
                    foreach (Entity entity in _world.Entities.All)
                        if (entity.Owner == new PlayerId(0) && entity.Get<ViewObservationPart>() is ViewObservationPart part)
                            part.OnInspect = (world, viewer, target) => Mutate(world);
                    break;
                default: throw new ArgumentException(nameof(surface));
            }
        }
        public Action<World, PlayerId> OnFactory;
        public Action<ViewObservationVisibility> ConfigureVisibility;
        public override string Id => "proof.views";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types) => types.Component<ViewObservationPartDef>("observation_probe", 1);
        public override void Compose(WorldComposer composer)
        {
            var visibility = new ViewObservationVisibility();
            _world = composer.World; _visibility = visibility;
            ConfigureVisibility?.Invoke(visibility);
            composer.Services.Bind<IVisibility>(visibility);
            composer.AddSystem(new ViewObservationSystem(), 100);
            composer.AddViewFeature("proof.observation", 1, (world, viewer) =>
            { OnFactory?.Invoke(world, viewer); return new ViewObservationReadModel(world, viewer); });
        }
    }
    public static class ViewObservationProof
    {
        public static GameDefinition Definition(ViewObservationModule module = null)
            => new GameDefinition("view-observation", new GameModule[] { module ?? new ViewObservationModule() });
        public static ContentSet Content(GameDefinition definition, string authored = null)
        {
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("view-observation-master.json"))
                .Add("view-observation.json", authored ?? FrameworkProofContent.Read("view-observation.json"));
            return ContentSet.Load(files, definition.ContentTypes);
        }
        public static GameFactory Factory(ViewObservationModule module = null, string authored = null)
        { var definition = Definition(module); return new GameFactory(definition, Content(definition, authored)); }
        public static SimGame New(GameFactory factory = null) => (factory ?? Factory()).NewGame(new NewGameRequest
            { Scenario = "views", Seed = 288, Seats = { Controller.Human, Controller.Human } });
    }
}
