using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static partial class ContinuousTransformationTests
    {
        private static readonly Code Capability = new Code("process");
        private const string Declaration = "{\"id\":\"proof.transformation-supply\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"},{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"continuous-transformations.json\"]}";
        public static void Register()
        {
            TestRunner.Add("Continuous transformations: authored modes, quantization and private contracts", Contracts);
            TestRunner.Add("Continuous transformations: physical flow, waits, cancellation and saved throughput", Physical);
            TestRunner.Add(PhysicalWearMinuteCase, c => PhysicalWear(c, false));
            TestRunner.Add(PhysicalWearHalfCase, c => PhysicalWear(c, true));
        }
        private static string Text(JsonValue json) { var result = new StringBuilder(); json.WriteCanonical(result); return result.ToString(); }
        private static JsonValue Definition(JsonValue json) => json.Get("transformations")[0];
        private static JsonValue Part(JsonValue json, string key) => json.Get("entities")[0].Get("components").Get(key);
        internal static GameFactory Factory(Action<JsonValue> edit = null, Action<JsonValue> basisEdit = null, string flowDefinitions = null)
        {
            JsonValue content = Json.Parse(FrameworkProofContent.Read("continuous-transformations.json")); edit?.Invoke(content);
            JsonValue basis = Json.Parse(PointGame.Definitions);
            var scenario = basis.Get("scenarios")[0]; scenario.Set("seats", JsonValue.Number("2")); scenario.Set("default_seats", JsonValue.Number("2"));
            scenario.Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId)); basisEdit?.Invoke(basis);
            return PointGame.Factory(Text(basis), additionalModule: new TransformationSupplyProofModule(), moduleDeclaration: Declaration,
                moduleDocument: Text(content), flowDefinitions: flowDefinitions ?? FrameworkProofContent.Read("running-input-flows.json"), startupProfiles: new[] { "continuous-transformations.start" });
        }
        private static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "points", Seed = 814 }; request.Mods.Add("proof.transformation-supply"); return factory.NewGame(request); }
        private static Entity FactoryEntity(SimGame game)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == "flow.factory") return entity; throw new InvalidOperationException("missing factory"); }
        private static IInventory Stock(SimGame game) => FactoryEntity(game).Get<IInventory>();
        private static ResourceId Resource(SimGame game, string id) => game.World.Content.ResourceId(id);
        private static TransformationSnapshot State(SimGame game)
        { if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(FactoryEntity(game).Id, Capability, out var state)) throw new InvalidOperationException("missing flow facts"); return state; }
        private static void Tick(SimGame game) { if (game.RunTicks(1) != 1) throw new InvalidOperationException("Update fault: " + game.Fault); }
        private static void Enqueue(Check c, SimGame game)
            => c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = FactoryEntity(game).Id, Capability = Capability, Transformation = "flow.refine" }).Accepted, "ordinary enqueue accepted");
        private static void Control(Check c, SimGame game, bool enabled, string target)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = FactoryEntity(game).Id, Operation = Capability, Enabled = enabled, Target = Fix.Parse(target) }).Accepted, "ordinary control accepted");
        private static void Cancel(Check c, SimGame game)
            => c.That(TestKit.Order(game, 0, new CancelTransformationCommand { Transformer = FactoryEntity(game).Id, Capability = Capability, Index = 0 }).Accepted, "ordinary stop accepted");
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 800 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done())
            {
                var jobs = new List<string>(); foreach (Job job in game.World.Services.Get<WorkBoard>().Jobs) jobs.Add(job.Id + "/" + job.State + "/" + job.Blocker);
                throw new InvalidOperationException(stage + " timeout tick=" + game.Tick + " progress=" + State(game).Progress + " blocker=" + State(game).Blocker + " jobs=" + string.Join(";", jobs));
            }
        }
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current clean engine semantics"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "breaking save identity");
            ITransformerFlow flow = FactoryEntity(game).Get<ITransformer>(Capability) as ITransformerFlow;
            c.That(flow != null && ReferenceEquals(FactoryEntity(game).Get<IResourcePort>(new Code("material.input")).ResolveStore(game.World).Store, Stock(game)) && ReferenceEquals(FactoryEntity(game).Get<IResourcePort>(new Code("material.output")).ResolveStore(game.World).Store, Stock(game)), "explicit local port bindings alias actual stock without a second store");
            c.That(State(game).Mode == null && State(game).Flow.Consumed.IsEmpty, "idle has no invented stream");
            foreach (string invalid in new[] { "mode", "unknown-mode", "shape", "duration", "missing-duration", "entity", "cancel", "empty-flow", "unknown-resource", "duplicate", "native", "period", "missing-idle", "missing-operating" })
            {
                c.Throws<ContentException>(() => New(Factory(json =>
                {
                    var row = Definition(json);
                    switch (invalid)
                    {
                        case "mode": row.Remove("mode"); break;
                        case "unknown-mode": row.Set("mode", JsonValue.Of("legacy")); break;
                        case "shape": row.Set("inputs", Json.Parse("{}")); break;
                        case "duration": row.Set("duration", Json.Parse("{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"}")); break;
                        case "missing-duration": row.Remove("duration"); break;
                        case "entity": row.Set("entity_output", JsonValue.Of("source")); break;
                        case "cancel": row.Set("cancel_policy", JsonValue.Of("return_inputs")); break;
                        case "empty-flow": row.Set("outputs", Json.Parse("[]")); row.Set("inputs", Json.Parse("[]")); break;
                        case "unknown-resource": row.Get("inputs")[0].Set("resource", JsonValue.Of("missing")); break;
                        case "duplicate": row.Get("inputs").Add(row.Get("inputs")[0].Clone()); break;
                        case "native": row.Get("inputs")[0].Set("quantity", JsonValue.Number("0.5")); break;
                        case "period": row.Get("inputs")[0].Get("period").Set("denominator", JsonValue.Number("0")); break;
                        case "missing-idle": Part(json, "transformer").Set("idle_transformation", JsonValue.Of("missing")); break;
                        case "missing-operating": row.Remove("operating_inputs"); break;
                    }
                })), "strict authored refusal: " + invalid);
            }
            var definition = game.World.Content.Get<TransformationDef>("flow.refine");
            c.That(definition.Mode == TransformationMode.Continuous && definition.Duration.IsZero && definition.Inputs.IsEmpty && definition.Outputs.IsEmpty, "rate metadata never fabricates batch material");
            IResourceRate rate = definition.OutputRates[0];
            var half = WorkProgress.Zero.AdvanceUnbounded(Fix.Parse("0.5"));
            c.That(half.WholeTicks == 0 && half.Fraction == 2147483648U, "unbounded progress retains exact fraction");
            c.That(ResourceRates.Accumulated(new[] { rate }, game.SimulationTime, Stock(game).CapacityPolicy, half, ResourceRateRounding.Floor).IsEmpty, "fractional discrete product remains prospective");
            c.Equal(Fix.One, ResourceRates.Accumulated(new[] { rate }, game.SimulationTime, Stock(game).CapacityPolicy, half, ResourceRateRounding.Ceiling).AmountOf(rate.Resource.Resource), "input cumulative quantization prepays one native quantum");
            c.Equal(Fix.One, ResourceRates.Between(new[] { rate }, game.SimulationTime, Stock(game).CapacityPolicy, half, new WorkProgress(1), ResourceRateRounding.Floor).AmountOf(rate.Resource.Resource), "next interval earns one actual product");
            c.Throws<ArgumentException>(() => ResourceRates.Between(new[] { rate }, game.SimulationTime, Stock(game).CapacityPolicy, new WorkProgress(1), half, ResourceRateRounding.Floor), "backwards interval refuses");
            c.Throws<OverflowException>(() => new WorkProgress(long.MaxValue, uint.MaxValue).AdvanceUnbounded(Fix.One), "wide progress does not silently complete or clip");
            var wide = new ResourceRate(rate.Resource, Fix.One, long.MaxValue, 1, DurationUnit.Second);
            c.Equal(Fix.FromInt(60), ResourceRates.Accumulated(new[] { wide }, game.SimulationTime, Stock(game).CapacityPolicy, new WorkProgress(long.MaxValue), ResourceRateRounding.Floor).AmountOf(rate.Resource.Resource), "wide cumulative math never narrows time");
            c.Throws<ArgumentException>(() => ResourceRates.Accumulated(new[] {rate,rate},game.SimulationTime,Stock(game).CapacityPolicy,half,ResourceRateRounding.Floor),"duplicate authored contract rates refuse");
            c.Throws<ArgumentException>(() => ResourceRates.Accumulated(new[] {rate},game.SimulationTime,Stock(game).CapacityPolicy,half,(ResourceRateRounding)99),"unknown rounding mode has no default");
            Enqueue(c, game); var saved = State(game); ulong hash = game.Hash();
            c.That(saved.Mode == TransformationMode.Continuous && saved.Required == 0 && saved.Phase == TransformationPhase.Working && saved.Progress.Equals(half), "stream remains working with no fake completion duration");
            c.That(saved.Wip.Execution > 0 && saved.Wip.Stock.IsEmpty && saved.Tools.Held.AmountOf(Resource(game,"supply")) == Fix.One, "same execution owns tools without raw WIP copy");
            c.That(saved.Flow.Consumed.AmountOf(Resource(game,"supply")) == Fix.One && saved.Flow.Produced.IsEmpty && saved.Operating.Consumed.AmountOf(Resource(game,"supply")) == Fix.One, "process inputs and elapsed fuel derive distinct earned facts");
            c.Equal(Fix.FromInt(18), Stock(game).Amount(Resource(game,"supply")), "first flow consumes actual input and fuel atomically");
            for (int i = 0; i < 3; i++) State(game);
            c.Equal(hash, game.Hash(), "private rate views are pure");
            c.That(!game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(FactoryEntity(game).Id, Capability, out _), "foreign company cannot inspect flow");
            SimGame twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "same sole codec saves fractional stream");
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "isolated interleaved saved stream is identical");
            c.That(saved.Flow.Produced.IsEmpty && saved.Progress.Equals(half), "retained facts remain detached after output");
            c.That(!TestKit.Order(game,0,new EnqueueTransformationCommand { Transformer=FactoryEntity(game).Id,Capability=Capability,Transformation="flow.refine",Repeat=true }).Accepted, "repeat command rejects an indefinite stream");
            c.Equal(Codes.InvalidArgument,game.Seat(0).View.Feature<TransformationManagementView>().PreviewEnqueue(FactoryEntity(game).Id,Capability,"flow.refine",1,true,default),"repeat preview shares engine command refusal");
            SimGame corrupt = factory.Load(twin.Save()); FactoryEntity(corrupt).Get<Transformer>().Phase = TransformationPhase.Blocked;
            c.Throws<CorruptStateException>(() => corrupt.Save(), "continuous fake completion refuses checkpoint");
            c.That(corrupt.RunTicks(1) == 0 && corrupt.Fault != null, "invalid continuous phase faults normal Update");
            c.Throws<InvalidOperationException>(() => corrupt.Hash(), "faulted stream cannot checkpoint");
            Batch(c);
        }
        private static void Batch(Check c)
        {
            SimGame game = New(Factory(json =>
            {
                var row = Definition(json); row.Set("mode",JsonValue.Of("batch")); row.Set("cancel_policy",JsonValue.Of("return_inputs"));
                row.Set("inputs",Json.Parse("{\"supply\":3}")); row.Set("outputs",Json.Parse("{\"flow.product\":1}"));
                row.Set("duration",Json.Parse("{\"numerator\":2,\"denominator\":1,\"unit\":\"minute\"}"));
            }));
            Enqueue(c,game); Reach(game,()=>State(game).QueueCount==0,"finite batch after explicit mode migration");
            c.Equal(Fix.One,Stock(game).Amount(Resource(game,"flow.product")),"same native executor still completes finite batches");
            c.Equal(Fix.FromInt(15),Stock(game).Amount(Resource(game,"supply")),"batch consumes raw three and elapsed fuel two, not continuous rates");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Enqueue(c,game);
            for(int i=0;i<3;i++) Tick(game);
            c.That(State(game).Progress.Equals(new WorkProgress(2)) && State(game).QueueCount==1 && State(game).Phase==TransformationPhase.Working,"continuous stream never turns into a completed batch");
            c.Equal(Fix.FromInt(2),Stock(game).Amount(Resource(game,"flow.product")),"two earned product quanta actually exist");
            c.Equal(Fix.One,Stock(game).Amount(Resource(game,"flow.residue")),"simultaneous slower coproduct exists only when earned");
            c.Equal(Fix.FromInt(14),Stock(game).Amount(Resource(game,"supply")),"four updates use four process inputs plus two elapsed fuel");
            var history=game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0);
            c.That(history.Totals(Resource(game,"supply")).ConsumedRaw==Fix.One.Raw*6L,"existing journal records complete feed/fuel consumption once");
            c.That(history.Totals(Resource(game,"flow.product")).CreatedRaw==Fix.One.Raw*2L&&history.Totals(Resource(game,"flow.residue")).CreatedRaw==Fix.One.Raw,"existing journal records all actual coproducts without another balance");
            c.That(game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0).Status==ResourceFlowReadStatus.NotOwned,"continuous resource history stays company-private");
            Control(c,game,false,"0.5"); var frozen=State(game); Fix stock=Stock(game).Amount(Resource(game,"supply"));
            SimGame twin=factory.Load(game.Save()); for(int i=0;i<8;i++){Tick(game);Tick(twin);}
            c.That(State(game).Progress.Equals(frozen.Progress)&&State(game).Operating.ElapsedTicks==frozen.Operating.ElapsedTicks,"disabled time earns neither throughput nor fuel");
            c.Equal(stock,Stock(game).Amount(Resource(game,"supply")),"pause spends no input prefix"); c.Equal(game.Hash(),twin.Hash(),"saved pause is deterministic");
            Control(c,game,true,"2"); Control(c,twin,true,"2");
            c.Equal(game.Hash(),twin.Hash(),"target change resumes at actual saved cumulative progress");
            c.Equal(Fix.FromInt(4),State(game).Flow.Consumed.AmountOf(Resource(game,"supply"))-frozen.Flow.Consumed.AmountOf(Resource(game,"supply")),"target scales process feed, not elapsed fuel");
            c.Equal(1L,State(game).Operating.ElapsedTicks-frozen.Operating.ElapsedTicks,"fast update earns one elapsed fuel quantum");
            stock=Stock(game).Amount(Resource(game,"supply")); Fix product=Stock(game).Amount(Resource(game,"flow.product"));
            Cancel(c,game);Cancel(c,twin);
            c.That(State(game).QueueCount==0&&Stock(game).Reservations.Count==0&&State(game).Operating.ElapsedTicks==0,"stop releases actual tools and sole execution");
            c.Equal(stock,Stock(game).Amount(Resource(game,"supply")),"stop never refunds processed inputs or fuel"); c.Equal(product,Stock(game).Amount(Resource(game,"flow.product")),"stop retains actual earned products"); c.Equal(game.Hash(),twin.Hash(),"saved stopped sessions remain identical");
            Shortage(c); Backpressure(c); Supply(c); Fluid(c); Generator(c); Gates(c);
        }
        private static void Shortage(Check c)
        {
            GameFactory factory=Factory(json=>Part(json,"storage").Set("initial_stock",Json.Parse("{\"supply\":4}")));
            SimGame game=New(factory);Enqueue(c,game);Tick(game);var before=State(game);Tick(game);
            c.That(State(game).Blocker==TransformerManager.BlockerFlowInputs&&State(game).Progress.Equals(before.Progress)&&State(game).Operating.ElapsedTicks==before.Operating.ElapsedTicks,"input shortage freezes entire flow step");
            c.That(Stock(game).Amount(Resource(game,"supply"))==Fix.One&&Stock(game).Amount(Resource(game,"flow.product"))==Fix.One,"protected tool is not feed and shortage creates no product");
            SimGame twin=factory.Load(game.Save());for(int i=0;i<5;i++){Tick(game);Tick(twin);}c.Equal(game.Hash(),twin.Hash(),"saved shortage has no repeated burns");
            Cancel(c,game);c.Equal(Fix.One,Stock(game).Amount(Resource(game,"supply")),"shortage stop releases but never consumes tool");
        }
        private static void Backpressure(Check c)
        {
            SimGame game=New(Factory(json=>
            {
                Part(json,"storage").Set("initial_stock",Json.Parse("{\"supply\":4,\"flow.product\":36}"));
                Definition(json).Get("outputs")[0].Set("quantity", JsonValue.Number("6"));
                Part(json,"transformer").Get("operation").Set("target",JsonValue.Number("1"));
            }));Enqueue(c,game);
            c.That(State(game).Blocker==TransformerManager.BlockerOutput&&State(game).Wip.Execution==0,"startup output space admission precedes tools/payment");
            c.That(Stock(game).Amount(Resource(game,"supply"))==Fix.FromInt(4)&&Stock(game).Reservations.Count==0,"full destination never burns available feed/fuel");
            // Authored net growth fills a real local store while enough feed remains for the refused step.
            game=New(Factory(json=>
            {
                Definition(json).Set("outputs",Json.Parse("[{\"resource\":\"flow.product\",\"quantity\":6,\"period\":{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"}}]"));
                Definition(json).Set("output_ports",Json.Parse("{\"flow.product\":\"material.output\"}"));
                Part(json,"transformer").Get("operation").Set("target",JsonValue.Number("1"));
            }));Enqueue(c,game);Reach(game,()=>State(game).Blocker==TransformerManager.BlockerOutput,"running physical output backpressure");
            var before=State(game);Fix feed=Stock(game).Amount(Resource(game,"supply"));Tick(game);
            c.That(State(game).Progress.Equals(before.Progress)&&State(game).Operating.ElapsedTicks==before.Operating.ElapsedTicks,"running output wait preserves both progress clocks");
            c.Equal(feed,Stock(game).Amount(Resource(game,"supply")),"running backpressure consumes no feed/fuel");
            c.That(State(game).Tools.Held.AmountOf(Resource(game,"supply"))==Fix.One,"full output retains real tool claim");Cancel(c,game);
        }
        private static void Supply(Check c)
        {
            GameFactory factory=Factory(json=>
            {Part(json,"storage").Set("initial_stock",Json.Parse("{}"));Part(json,"transformer").Set("automatic_supply",JsonValue.Of(true));});
            SimGame game=New(factory);Enqueue(c,game);
            c.That(State(game).Phase==TransformationPhase.Supplying&&State(game).Progress.Equals(WorkProgress.Zero),"empty processor waits for real delivery");
            Reach(game,()=>{foreach(Job job in game.World.Services.Get<WorkBoard>().Jobs) if(job.IsHaul&&job.Loaded.IsPositive)return true;return false;},"loaded physical flow startup");
            c.That(State(game).Progress.Equals(WorkProgress.Zero),"cargo in transit cannot become feed");
            SimGame twin=factory.Load(game.Save());Reach(game,()=>State(game).Progress.WholeTicks>=2,"physical running refill",twin);
            c.Equal(game.Hash(),twin.Hash(),"saved loaded cargo and continuing refills remain identical");
            c.That(Stock(game).Amount(Resource(game,"flow.product"))>=Fix.FromInt(2),"real supplied flow produces products");
            c.That(State(game).Operating.ElapsedTicks<game.Tick,"travel/handling wait earns no process time");
            Cancel(c,game);Cancel(c,twin);for(int i=0;i<20;i++){Tick(game);Tick(twin);}c.Equal(game.Hash(),twin.Hash(),"stop preserves acquired physical deliveries without duplicated outputs");
        }
        private static void Fluid(Check c)
        {
            GameFactory factory=Factory(json=>
            {
                json.Get("resources").Add(Json.Parse("{\"id\":\"flow.fluid\",\"unit\":\"unit.capacity.volume\",\"category\":\"fluid\",\"phase\":\"liquid\",\"traits\":[],\"capacity\":{\"unit.capacity.mass\":1,\"unit.capacity.volume\":1}}"));
                for(int i=0;i<2;i++)json.Get("resources")[i].Get("capacity").Set("unit.capacity.mass",JsonValue.Number("1"));
                json.Set("capacity_policies",Json.Parse("[{\"id\":\"flow.mixed\",\"limits\":{\"unit.capacity.mass\":40}}]"));
                Part(json,"storage").Set("capacity_policy",JsonValue.Of("flow.mixed"));
                Definition(json).Set("outputs",Json.Parse("[{\"resource\":\"flow.fluid\",\"quantity\":1,\"period\":{\"numerator\":7,\"denominator\":2,\"unit\":\"minute\"}}]"));
                Definition(json).Set("output_ports",Json.Parse("{\"flow.fluid\":\"material.output\"}"));
            },basis=>basis.Get("resources")[0].Get("capacity").Set("unit.capacity.mass",JsonValue.Number("1")));
            SimGame game=New(factory);Enqueue(c,game);SimGame twin=factory.Load(game.Save());for(int i=0;i<6;i++){Tick(game);Tick(twin);}
            c.Equal(Fix.One,Stock(game).Amount(Resource(game,"flow.fluid")),"seven fractional updates earn exact rational continuous fluid output");
            c.Equal(game.Hash(),twin.Hash(),"fluid cumulative native precision survives save without remainder ledger");
        }
        private static void Generator(Check c)
        {
            GameFactory factory=Factory(json=>
            {
                var row=Definition(json);row.Set("inputs",Json.Parse("[]"));row.Set("tools",Json.Parse("{}"));row.Set("operating_inputs",Json.Parse("[]"));
                row.Set("input_ports",Json.Parse("{}"));row.Set("tool_ports",Json.Parse("{}"));row.Set("operating_ports",Json.Parse("{}"));
                row.Set("outputs",Json.Parse("[{\"resource\":\"flow.product\",\"quantity\":1,\"period\":{\"numerator\":3,\"denominator\":1,\"unit\":\"minute\"}}]"));
                row.Set("output_ports",Json.Parse("{\"flow.product\":\"material.output\"}"));
                Part(json,"transformer").Get("operation").Set("target",JsonValue.Number("0.25"));
            });SimGame game=New(factory);Enqueue(c,game);
            for(int i=0;i<4;i++)Tick(game);SimGame twin=factory.Load(game.Save());
            c.That(State(game).Flow.Consumed.IsEmpty&&State(game).Flow.Produced.IsEmpty&&Stock(game).Reservations.Count==0,"explicit generator earns prospective throughput without dummy receipts");
            for(int i=0;i<7;i++){Tick(game);Tick(twin);}
            c.Equal(Fix.One,Stock(game).Amount(Resource(game,"flow.product")),"cumulative floor across twelve tiny updates produces one actual quantum");
            c.Equal(Fix.FromInt(20),Stock(game).Amount(Resource(game,"supply")),"authored empty feed does not invent a material cost");
            c.Equal(game.Hash(),twin.Hash(),"empty-delta updates survive save without per-update rounding loss");
            c.Equal(12L,State(game).Operating.ElapsedTicks,"resource-free update still earns actual successful running time");
            Cancel(c,game);c.Equal(Fix.One,Stock(game).Amount(Resource(game,"flow.product")),"stopping a generator preserves only actually earned product");
        }
        private static void Gates(Check c)
        {
            GameFactory factory=Factory(json=>
            {
                json.Set("signals",Json.Parse("[{\"id\":\"flow.target\",\"kind\":\"number\",\"metric\":\"operation.target\",\"unit\":\"unit.work-rate\",\"providers\":[{\"component\":\"transformer\",\"schema\":1}]}]"));
                json.Get("entities")[0].Get("components").Set("signal_transformation_gate",Json.Parse("{\"id\":\"safe-rate\",\"transformations\":[\"flow.refine\"],\"conditions\":[{\"id\":\"rate\",\"source\":\"process\",\"signal\":\"flow.target\",\"minimum\":1,\"maximum\":2,\"unit\":\"unit.work-rate\",\"blocker\":\"flow.rate-gated\"}]}"));
            });SimGame game=New(factory);Enqueue(c,game);
            c.That(State(game).Wip.Execution==0&&State(game).Blocker==new Code("flow.rate-gated"),"signal gate blocks continuous startup before material commitments");
            Control(c,game,true,"1");var running=State(game);Fix stock=Stock(game).Amount(Resource(game,"supply"));
            Control(c,game,true,"0.5");SimGame twin=factory.Load(game.Save());for(int i=0;i<4;i++){Tick(game);Tick(twin);}
            c.That(State(game).Progress.Equals(running.Progress)&&State(game).Operating.ElapsedTicks==running.Operating.ElapsedTicks,"mid-stream gate earns no work or elapsed fuel");
            c.Equal(stock,Stock(game).Amount(Resource(game,"supply")),"gated flow has no consumption prefix");c.Equal(game.Hash(),twin.Hash(),"saved signal wait remains deterministic");
            Cancel(c,game);c.That(State(game).QueueCount==0&&Stock(game).Reservations.Count==0,"gated stop still releases real tools through existing return");
        }
    }
}
