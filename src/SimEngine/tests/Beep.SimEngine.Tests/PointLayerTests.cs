using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;

namespace Beep.SimEngine.Tests
{
    public static class PointLayerTests
    {
        private static string Layers => FrameworkProofContent.Read("point-layers.json");
        private static string Definitions
        {
            get
            {
                JsonValue json = Json.Parse(PointGame.Definitions);
                json.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
                json.Get("scenarios")[0].Set("default_seats", JsonValue.Number("2"));
                return json.ToString();
            }
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 43 });
        public static void Register()
            => TestRunner.Add("Spatial providers: non-tile layers, strict admission and saved continuation", Run);

        private static void Write(Check c, SimGame game, int player, string layer, Fix point, Fix value)
        {
            var command = new WritePointLayerCommand { Layer = layer, Position = point, Value = value };
            ulong hash = game.Hash();
            c.Equal(Code.None, game.Seat(player).Preview(command), "engine previews actual sample write: " + layer);
            c.Equal(hash, game.Hash(), "sample preview is pure");
            c.That(TestKit.Order(game, player, command).Accepted, "ordinary player command writes sample through its provider");
        }
        private static void Run(Check c)
        {
            GameFactory factory = PointGame.Factory(Definitions, Layers); SimGame game = New(factory);
            var space = (PointSpace)game.World.Space;
            PointLayerView mine = game.Seat(0).View.Feature<PointLayerView>(), other = game.Seat(1).View.Feature<PointLayerView>();
            PointLayerView observer = game.Observer.Feature<PointLayerView>();
            ISpatialLayerQuery<PointRegion, Fix> query = mine;
            var values = new List<Fix> { Fix.One };
            var region = new PointRegion(Fix.FromRatio(5, 2), Fix.FromRatio(1, 2), 2);
            c.That(!game.World.Services.TryGet<TileMap>(out _) && !game.World.Services.TryGet<NavService>(out _), "non-tile layers compose without TileMap or NavService");
            c.That(game.World.Space is ISpatialLayerProvider && space.SpatialLayers.Count == 3, "selected non-tile provider registers the ordinary authored contract");
            c.Equal(2, mine.Layers.Count, "unassigned private layer hides its metadata");
            c.Equal(3, observer.Layers.Count, "authorized observer sees registered metadata");
            c.Equal(SpatialLayerReadStatus.Unavailable, query.ReadLayer("point.deposit", region, values), "unassigned private samples are unavailable");
            c.Equal(0, values.Count, "hidden read clears prior output");
            c.That(TestKit.Order(game, 0, new AssignPointLayerCommand { Layer = "point.deposit", Owner = new PlayerId(0) }).Accepted, "engine command claims an unassigned private layer");
            SpatialLayerSnapshot retained = mine.Layers[0];
            c.That(retained.Id == "point.deposit" && retained.Resource.Resource == game.World.Content.ResourceId("supply")
                && retained.Unit.Dimension == "count" && retained.VerticalUnit.Dimension == "length", "same catalog contracts describe count-valued subsurface samples and metre offsets");
            int otherCursor = other.Version;
            Write(c, game, 0, "point.deposit", region.Start, Fix.FromInt(3));
            c.Equal(otherCursor, other.Version, "private sample write does not leak through another company's cursor");
            Write(c, game, 0, "point.terrain", region.Start, Fix.FromRatio(-7, 4));
            Write(c, game, 0, "point.water", region.Start, Fix.FromRatio(9, 8));
            c.Equal(SpatialLayerReadStatus.Success, query.ReadLayer("point.deposit", region, values), "provider-owned region query reads private exact samples");
            c.That(values.Count == 2 && values[0] == Fix.FromInt(3) && values[1] == Fix.Zero, "unallocated authorized sample has the declared sparse zero default");
            c.Equal(SpatialLayerReadStatus.Success, query.ReadLayer("point.terrain", region, values), "terrain remains an independent channel");
            c.Equal(Fix.FromRatio(-7, 4), values[0], "terrain may be below its explicit datum");
            c.Equal(SpatialLayerReadStatus.Success, query.ReadLayer("point.water", region, values), "water height associated with a volume resource retains its own unit");
            c.Equal(Fix.FromRatio(9, 8), values[0], "water value did not overwrite terrain");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(new WritePointLayerCommand { Layer = "point.deposit", Position = region.Start, Value = Fix.One }), "private writes reveal no hidden layer existence");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(new WritePointLayerCommand { Layer = "missing", Position = region.Start, Value = Fix.One }), "missing and hidden writes share refusal semantics");
            c.Equal(Codes.NotOwned, game.Seat(1).Preview(new AssignPointLayerCommand { Layer = "point.deposit", Owner = new PlayerId(1) }), "another player cannot seize a private layer");
            c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(new WritePointLayerCommand { Layer = "point.deposit", Position = region.Start, Value = Fix.FromRatio(3, 2) }), "discrete resource samples refuse fractional native counts");
            c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(new WritePointLayerCommand { Layer = "point.deposit", Position = region.Start, Value = -Fix.One }), "quantity samples refuse negative material");
            c.Equal(Codes.OutOfBounds, game.Seat(0).Preview(new WritePointLayerCommand { Layer = "point.terrain", Position = Fix.FromInt(101), Value = Fix.One }), "provider extent is a scenario boundary, not a platform limit");
            c.Equal(SpatialLayerReadStatus.InvalidRegion, query.ReadLayer("point.terrain", new PointRegion(Fix.Zero, Fix.One, -1), values), "negative query count refuses");
            c.Equal(SpatialLayerReadStatus.InvalidRegion, query.ReadLayer("point.terrain", new PointRegion(Fix.Zero, Fix.Zero, 2), values), "zero-spacing query refuses");
            c.Equal(SpatialLayerReadStatus.InvalidRegion, query.ReadLayer("point.terrain", new PointRegion(Fix.Zero, Fix.FromRaw(long.MaxValue), 3), values), "overflowing query refuses before arithmetic or allocation");
            c.Equal(SpatialLayerReadStatus.Success, query.ReadLayer("point.terrain", new PointRegion(Fix.Zero, Fix.One, 0), values), "empty region succeeds without fabricated values");
            c.Equal(0, values.Count, "empty query returns no samples");
            c.Throws<NotSupportedException>(() => ((IList<ISpatialLayer>)space.SpatialLayers).Clear(), "provider registration collection cannot be replaced by a caller");
            c.Throws<NotSupportedException>(() => ((IList<SpatialLayerSnapshot>)mine.Layers).Clear(), "view metadata list is detached and read-only");
            c.Throws<InvalidOperationException>(() => mine.Layers[1].Metadata.Get("generator").Set("id", JsonValue.Of("changed")), "nested authored metadata is immutable");
            c.Throws<InvalidOperationException>(() => space.RegisterLayer(game.World.Content.Get<ISpatialLayerDefinition>("point.water")), "registration closes before the game loop");
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "one non-tile space codec saves samples/ownership with existing motion and work");
            var loaded = twin.Seat(0).View.Feature<PointLayerView>();
            c.Equal(SpatialLayerReadStatus.Success, loaded.ReadLayer("point.deposit", region, values), "restored private sample remains authorized");
            c.Equal(Fix.FromInt(3), values[0], "restored sparse sample retains its exact native value");
            c.That(TestKit.Order(game, 0, new AssignPointLayerCommand { Layer = "point.deposit", Owner = new PlayerId(1) }).Accepted, "owner transfers layer through engine command");
            c.That(TestKit.Order(twin, 0, new AssignPointLayerCommand { Layer = "point.deposit", Owner = new PlayerId(1) }).Accepted, "saved twin executes the same transfer");
            c.Equal(new PlayerId(0), retained.Owner, "retained metadata does not follow live ownership");
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("point.deposit", region, values), "old owner loses access");
            c.Equal(SpatialLayerReadStatus.Success, other.ReadLayer("point.deposit", region, values), "new owner gains access through provider visibility");
            Write(c, game, 1, "point.deposit", region.Start, Fix.FromInt(4));
            Write(c, twin, 1, "point.deposit", region.Start, Fix.FromInt(4));
            game.Update(); twin.Update();
            c.Equal(game.Hash(), twin.Hash(), "saved twins continue identically through real Update and commands");
            SimGame independent = New(factory);
            var independentView = independent.Seat(0).View.Feature<PointLayerView>();
            c.Equal(SpatialLayerReadStatus.Success, independentView.ReadLayer("point.terrain", region, values), "another session uses the same immutable definitions");
            c.Equal(Fix.Zero, values[0], "interleaved sessions share no live layer samples");
            c.Equal(SpatialLayerReadStatus.Unavailable, independentView.ReadLayer("point.deposit", region, values), "another session shares no private layer owner");
            c.Throws<ArgumentException>(() => space.AssignOwner(independent.World, "point.deposit", new PlayerId(0)), "cross-session ownership refuses");
            c.Equal(Codes.InvalidArgument, space.WriteLayer(independent.World, new PlayerId(0), "point.terrain", Fix.Zero, Fix.One), "cross-session sample mutation refuses");
            foreach (Entity entity in game.World.Entities.All)
                if (entity.Def.Id == "source") c.Equal(Fix.FromInt(20), entity.Get<IInletPort>().Amount(game.World.Content.ResourceId("supply")), "spatial deposit samples do not manufacture inventory");
            Corruption(c, factory);
            Admission(c);
        }

        private static void Corruption(Check c, GameFactory factory)
        {
            foreach (string fault in new[] { "count", "identity", "owner", "negative", "outside", "zero", "fractional", "order" })
            {
                SimGame game = New(factory);
                using (var bytes = new MemoryStream())
                {
                    var writer = new StateWriter(bytes);
                    bool open = true; writer.Bool(ref open); writer.Count(fault == "count" ? 2 : 3);
                    string[] ids = { "point.deposit", "point.terrain", "point.water" };
                    for (int i = 0; i < ids.Length; i++)
                    {
                        string id = fault == "identity" && i == 0 ? "different" : ids[i]; writer.String(ref id);
                        PlayerId owner = fault == "owner" && i == 0 ? new PlayerId(7) : PlayerId.None; writer.Player(ref owner);
                        writer.Count(i == 0 ? (fault == "order" ? 2 : 1) : 0);
                        if (i != 0) continue;
                        long coordinate = fault == "negative" ? -1 : fault == "outside" ? Fix.FromInt(101).Raw : Fix.One.Raw;
                        Fix value = fault == "zero" ? Fix.Zero : fault == "fractional" ? Fix.FromRatio(3, 2) : Fix.One;
                        writer.Long(ref coordinate); writer.Fix(ref value);
                        if (fault == "order") { writer.Long(ref coordinate); writer.Fix(ref value); }
                    }
                    writer.Flush();
                    byte[] body = bytes.ToArray();
                    c.Throws<CorruptStateException>(() => game.World.Space.Sync(new StateReader(body, 0, body.Length)), "non-tile layer codec rejects invalid " + fault + " before continuation");
                }
            }
        }

        private static void Admission(Check c)
        {
            foreach (string fault in new[] { "missing", "null", "duplicate", "extra", "space", "type", "shape", "visibility", "unit", "resource", "measure", "domain", "vertical-unit", "reference", "offset", "metadata", "mutable-metadata", "owner", "public-owner", "access", "observer" })
            {
                var module = new AdmissionModule();
                GameFactory factory = AdmissionFactory(module, fault);
                c.Throws<ContentException>(() => factory.NewGame(new NewGameRequest { Scenario = "admission" }), "invalid provider " + fault + " fails composition");
                c.Equal(0, module.Initialized, "invalid provider never reaches Initialize");
            }
            var good = new AdmissionModule();
            AdmissionFactory(good, "valid").NewGame(new NewGameRequest { Scenario = "admission" });
            c.Equal(1, good.Initialized, "correct independent provider contracts initialize normally");
            JsonValue unsupported = Json.Parse(Layers);
            unsupported.Get("spatial_layers")[0].Set("value_type", JsonValue.Of("byte"));
            c.Throws<ContentException>(() => New(PointGame.Factory(Definitions, unsupported.ToString())), "selected point provider explicitly refuses unsupported storage instead of substituting fixed samples");
        }
        private static GameFactory AdmissionFactory(AdmissionModule module, string fault)
        {
            JsonValue layers = Json.Parse(Layers), source = Json.Parse(PointGame.Definitions);
            for (int i = 0; i < layers.Get("units").Count; i++) source.Get("units").Add(layers.Get("units")[i].Clone());
            for (int i = 0; i < layers.Get("resources").Count; i++) source.Get("resources").Add(layers.Get("resources")[i].Clone());
            string json = "{" + TestGame.TimeDefinitions + "\"units\":" + source.Get("units") + ",\"resources\":" + source.Get("resources")
                + ",\"spatial_layers\":" + layers.Get("spatial_layers")
                + ",\"admission_spaces\":[{\"id\":\"field.points\",\"fault\":\"" + fault + "\"}],"
                + "\"game_modes\":[{\"id\":\"point-layer.resource-only\",\"name\":\"Physical diagnostic game\",\"properties\":{}}],\"scenarios\":[{\"id\":\"admission\",\"game_mode\":\"point-layer.resource-only\",\"mode_overrides\":{},\"time\":\"test.minute\",\"space\":\"field.points\",\"modules\":[\"admission\"],\"seats\":2,\"default_seats\":2}]}";
            var definition = new GameDefinition("admission", new GameModule[] { module });
            var files = new PreloadedFileSource().Add("manifest.json", "{\"schema\":1,\"modules\":[{\"id\":\"admission\",\"version\":\"1.0.0\",\"documents\":[\"layers.json\"]}]}").Add("layers.json", json);
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }

        private sealed class AdmissionModule : GameModule
        {
            public int Initialized;
            public override string Id => "admission";
            public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
            public override void DeclareContent(ContentTypes types) => types.Section<AdmissionSpaceDef>("admission_spaces");
            public override void Compose(WorldComposer composer) => composer.AddSystem(new InitializationProbe(this), 10);
        }
        private sealed class InitializationProbe : GameSystem
        {
            private readonly AdmissionModule _owner;
            public InitializationProbe(AdmissionModule owner) { _owner = owner; }
            public override string Id => "admission.initialize";
            public override int StateSchemaVersion => 1;
            public override void Initialize(World world) { _owner.Initialized++; }
            public override void Update(World world, TickInfo tick) { }
        }
        private sealed class AdmissionSpaceDef : Def, ISpatialSpaceDefinition
        {
            public AdmissionSpaceDef() { }
            private string _fault;
            protected override void ReadCore(JsonReader reader) => _fault = reader.String("fault");
            public void Compose(WorldComposer composer) => composer.Services.Bind<ISpatialSpace>(new AdmissionSpace(this, composer.Content, _fault));
        }
        private sealed class AdmissionSpace : ISpatialSpace, ISpatialLayerProvider
        {
            private readonly List<ISpatialLayer> _layers = new List<ISpatialLayer>();
            public AdmissionSpace(AdmissionSpaceDef definition, ResolvedContent content, string fault)
            {
                Id = definition.Id;
                foreach (ISpatialLayerDefinition layer in content.All<ISpatialLayerDefinition>())
                    _layers.Add(new FaultLayer(new LayerBinding(layer), fault, content));
                if (fault == "missing") _layers.RemoveAt(0);
                if (fault == "null") _layers[0] = null;
                if (fault == "duplicate") _layers.Add(_layers[0]);
            }
            public string Id { get; }
            public int StateSchemaVersion => 1;
            public int ObservationVersion => 0; // Immutable negative metadata fixture.
            public IReadOnlyList<ISpatialLayer> SpatialLayers => _layers.AsReadOnly();
            public void RegisterLayer(ISpatialLayerDefinition definition) => throw new NotSupportedException();
            public void AssignOwner(World world, string id, PlayerId owner) => throw new NotSupportedException();
            public void ValidateComposition(World world) { }
            public void OnSpawn(Entity entity) { }
            public void OnDespawn(Entity entity) { }
            public void OnLoaded(World world) { }
            public SpatialWorldSnapshot Capture(PlayerId viewer) => new PointWorldSnapshot(Id, 0, Fix.One, true);
            public void Sync(IStateSync sync) { }
        }
        private sealed class LayerBinding : SpatialLayer { public LayerBinding(ISpatialLayerDefinition definition) : base(definition) { } }
        private sealed class FaultLayer : ISpatialLayer
        {
            private readonly ISpatialLayer _source;
            private readonly string _fault;
            private readonly ResolvedContent _content;
            public FaultLayer(ISpatialLayer source, string fault, ResolvedContent content) { _source = source; _fault = fault; _content = content; }
            public string Id => _fault == "extra" ? "not-authored" : _source.Id;
            public string Space => _fault == "space" ? "another-space" : _source.Space;
            public Code Type => _fault == "type" ? new Code("wrong") : _source.Type;
            public SpatialLayerValueType ValueType => _fault == "shape" ? SpatialLayerValueType.Byte : _source.ValueType;
            public SpatialLayerVisibility Visibility => _fault == "visibility" ? (_source.Visibility == SpatialLayerVisibility.Public ? SpatialLayerVisibility.CompanyPrivate : SpatialLayerVisibility.Public) : _source.Visibility;
            public PlayerId Owner => _fault == "owner" ? new PlayerId(7) : _fault == "public-owner" ? new PlayerId(0) : _source.Owner;
            public bool CanRead(PlayerId viewer) => _fault != "observer" && (_fault == "access" || _source.CanRead(viewer));
            public IUnitDefinition Unit => _fault == "unit" ? _content.Get<UnitDef>("point.litre") : _source.Unit;
            public IResourceDefinition Resource => _fault == "resource" ? _content.Get<ResourceDef>("point.water") : _source.Resource;
            public SpatialResourceMeasure ResourceMeasure => _fault == "measure" ? SpatialResourceMeasure.Associated : _source.ResourceMeasure;
            public Code VerticalDomain => _fault == "domain" ? new Code("wrong") : _source.VerticalDomain;
            public IUnitDefinition VerticalUnit => _fault == "vertical-unit" ? _content.Get<UnitDef>("point.litre") : _source.VerticalUnit;
            public string VerticalReference => _fault == "reference" ? "wrong" : _source.VerticalReference;
            public Fix VerticalOffset => _fault == "offset" ? Fix.One : _source.VerticalOffset;
            public JsonValue Metadata => _fault == "metadata" ? Json.Parse("{\"wrong\":true}") : _fault == "mutable-metadata" ? _source.Metadata.Clone() : _source.Metadata;
        }
    }
}
