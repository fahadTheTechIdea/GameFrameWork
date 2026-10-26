using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ControlContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Public ordinary player inputs and detached observations only; no World/state writer.
    internal static class EnvironmentContentContractTests
    {
        internal const string Case = "Environment content contracts: physical incident accounting and private objectives";
        internal const string RetainedFaultCase = "Environmental views: retained terminal faults reject private unknown metrics and preserve physical accounting";
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly Code Source = new Code("pressure.leak");
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add(RetainedFaultCase, c => {
                c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current environmental terminal-boundary image");
                c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current original persistent owner layouts");
                Physical(c); Faults(c, retained: true);
            });
        }
        private static GameFactory Build(InitializeProbe initialize, out EnvironmentContentModule proof, Action<JsonValue> edit = null)
        {
            var doc = Json.Parse(FrameworkProofContent.Read("environment-content-contracts.json")); edit?.Invoke(doc);
            proof = new EnvironmentContentModule(initialize);
            return Factory(initialize, out _, extension: proof,
                contentDocuments: new Dictionary<string,string>{{"environment-content-contracts.json",doc.ToString()}},
                additionalModules: new GameModule[]{new EnvironmentalMetricsModule(),new ObjectiveModule()},
                additionalDeclarations: Json.Parse("[{\"id\":\"sim.environment-metrics\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"sim.objectives\",\"version\":\"1.0.0\",\"documents\":[]}]"),
                scenarioEdit: scenario => {
                    foreach (string id in new[]{ResourceFlowsModule.ModuleId,EnvironmentalMetricsModule.ModuleId,ObjectiveModule.ModuleId})
                    { var modules=scenario.Get("modules"); if(!Enumerable.Range(0,modules.Count).Any(i=>modules[i].Text==id))modules.Add(JsonValue.Of(id)); }
                    scenario.Get("settings").Set(ObjectiveModule.ModuleId,Json.Parse("{\"companies\":[{\"company\":0,\"objectives\":[\"environment.cleanup-goal\"]}]}"));
                },
                responseEdit: response => {
                    var activity=response.Get("activities")[0]; activity.Get("nodes")[1].Set("effect",JsonValue.Of("environment.spill"));
                    activity.Get("nodes").Add(Json.Parse("{\"id\":\"cleanup\",\"executor\":\"work.staffed\",\"after\":[\"repair\"],\"roles\":[\"inspect\"],\"duration\":{\"numerator\":3,\"denominator\":1,\"unit\":\"minute\"},\"effect\":\"environment.cleanup\"}"));
                    response.Get("incident_workflows")[0].Get("phases").Add(Json.Parse("{\"id\":\"cleanup\",\"nodes\":[\"cleanup\"]}"));
                });
        }
        private static EnvironmentalObservation Read(SimGame game,string id="contract.environment-spilled",int company=0)
            =>game.Seat(company).View.Feature<EnvironmentalMetricsView>().Read(new PlayerId(company),id);
        private static ObjectiveSnapshot Goal(SimGame game)
        { var rows=new List<ObjectiveSnapshot>();game.Seat(0).View.Feature<ObjectiveView>().CopyObjectives(rows);return rows.Single(r=>r.Objective=="environment.cleanup-goal"); }
        private static int Response(SimGame game)
        {
            if(!game.Seat(0).View.Feature<IncidentsView>().TryGetResponseOrder(Entity(game,"field.plant").Id,Source,out var order))
                throw new InvalidOperationException("missing real response");return order;
        }
        private static void Both(Check c,SimGame game,SimGame twin,Command command)
        { Order(c,game,command);Order(c,twin,command);c.Equal(game.Hash(),twin.Hash(),"same ordinary command in saved twin"); }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"environment contract protocol");c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"current original persistent owner layouts");
            c.Equal(typeof(Def),typeof(ResourceEnvironmentalMetricDef).BaseType,"native definition has no environmental dependency base");
            c.Equal(typeof(Def),typeof(ProofEnvironmentalMetricDef).BaseType,"existing external definition no longer needs old base");
            Physical(c);Invalid(c);Faults(c);
        }
        private static void Physical(Check c)
        {
            var init=new InitializeProbe();var factory=Build(init,out var proof);var game=New(factory);var isolated=New(factory);ulong untouched=isolated.Hash();
            c.That(proof.ActualMetricAliases&&proof.OriginalJournal&&init.Initializations>0,"actual contract metric/unit/objective aliases use sole journal");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId),"resource-only game requires no business module");
            var empty=Read(game);c.That(empty.Available&&empty.AmountRaw==BigInteger.Zero&&empty.Facts==0,"complete empty history is actual zero");
            c.That(!empty.Rate.HasValue&&empty.RateBlocker==EnvironmentalCodes.NoElapsedTime,"zero elapsed clock has no fabricated rate");
            var view=game.Seat(0).View.Feature<EnvironmentalMetricsView>();ulong hash=game.Hash();
            var foreign=view.Read(new PlayerId(1),"contract.environment-spilled");
            c.That(!foreign.Available&&foreign.Blocker==Codes.NotOwned&&!foreign.Unit.IsValid&&foreign.ThroughTick==0,"foreign metric withholds private amounts/units/time");
            c.Equal(Codes.UnknownDef,Read(game,"missing").Blocker,"missing metric has no default");
            c.Equal(Codes.NotSeated,game.Observer.Feature<EnvironmentalMetricsView>().Read(new PlayerId(99),"contract.environment-spilled").Blocker,"unknown beneficiary refuses");
            c.Equal(hash,game.Hash(),"private and unknown observations are pure");
            c.That(!game.Seat(0).View.Feature<ObjectiveView>().Observe("environment.cleanup-goal").Satisfied,"objective waits on real history");
            var limits=new ReplayLimits(4*1024*1024,3000);
            using(var recorder=game.RecordReplay(limits))
            {
                Order(c,game,new ConfigureWorkPolicyCommand{Worker=Entity(game,"machine").Id,Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("haul"),new WorkerRoleId("inspect")},null,true,true,WorkAssignmentMode.All)});
                Order(c,game,new RaiseIncidentCommand{Target=Entity(game,"field.plant").Id,Source=Source,Kind=Source});
                c.That(Active(game)&&Stock(game)==Fix.FromInt(20)&&Read(game).AmountRaw.Value.IsZero,"raising incident grants no free physical effect");
                var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"raised episode/empty metric journal checkpoint");
                Until(game,twin,()=>((PointSnapshot)Entity(game,"machine").Spatial).Position>Fix.FromInt(4),"actual travel starts");
                c.That(((PointSnapshot)Entity(game,"machine").Spatial).Position<Fix.FromInt(20)&&Read(game).AmountRaw.Value.IsZero,"unreached physical target has no fabricated spill");
                twin=factory.Load(game.Save());Until(game,twin,()=>Stock(game)==Fix.FromInt(12),"actual startup material consumption");
                c.That(Active(game)&&Read(game).AmountRaw.Value.IsZero&&((PointSnapshot)Entity(game,"machine").Spatial).Position==Fix.FromInt(20),"startup eight units consumed only after reach, not environmental loss");
                Until(game,twin,()=>Read(game).AmountRaw.Value>0,"positive physical repair then once-only spill");
                var spilled=Read(game);c.Equal((BigInteger)Fix.FromInt(2).Raw,spilled.AmountRaw.Value,"real two-unit loss");
                c.Equal(1L,spilled.Facts,"one physical loss event");c.Equal(Fix.FromInt(10),Stock(game),"twenty minus eight repair minus two spill");
                c.Equal(spilled.AmountRaw,Read(game,"environment.native-spilled").AmountRaw,"native/independent share same exact classification algorithm");
                c.Equal((BigInteger)2,Read(game,"contract.environment-recent").Rate.Value.Numerator,"complete one-minute window uses actual two units");
                c.Equal(BigInteger.Zero,Read(game,"contract.environment-cleaned").AmountRaw.Value,"later cleanup needs its own timed machine work");
                int order=Response(game);
                Order(c,game,new SetWorkerHeldCommand{Worker=Entity(game,"machine").Id,Held=true});twin=factory.Load(game.Save());
                for(int i=0;i<4;i++)Step(game,twin);
                c.That(Stock(game)==Fix.FromInt(10)&&Read(game,"contract.environment-cleaned").AmountRaw.Value.IsZero&&Active(game),"held worker blocks cleanup without inventory/accounting fallback");
                c.That(!Goal(game).Observation.Satisfied&&Goal(game).State!=ObjectiveState.Completed,"private objective still waits");
                Both(c,game,twin,new CancelOrderCommand{Order=order});
                c.Equal(order,Response(game),"cancellation resumes same episode/work/history owner");
                c.Equal(1L,Read(game).Facts,"resumption cannot repeat spill");
                long at=game.Clock.StepIndex;game.Clock.Pause();twin.Clock.Pause();hash=game.Hash();Step(game,twin);
                c.That(game.Clock.StepIndex==at&&game.Hash()==hash,"one paused clock freezes work/history/objective");game.Clock.Resume();twin.Clock.Resume();
                Both(c,game,twin,new SetWorkerHeldCommand{Worker=Entity(game,"machine").Id,Held=false});
                Until(game,twin,()=>Read(game,"contract.environment-cleaned").AmountRaw.Value>0,"physical cleanup completion");
                var cleaned=Read(game,"contract.environment-cleaned");
                c.Equal((BigInteger)Fix.FromInt(3).Raw,cleaned.AmountRaw.Value,"real three-unit cleanup disposal");
                c.Equal(cleaned.AmountRaw,Read(game,"environment.native-cleaned").AmountRaw,"native and direct cleanup aliases agree");
                c.Equal(Fix.FromInt(7),Stock(game),"whole physical stock balance twenty minus eight minus two minus three");
                c.That(Goal(game).State!=ObjectiveState.Completed,"event publication does not recursively complete same-update objective");
                Until(game,twin,()=>Goal(game).State==ObjectiveState.Completed&&!Active(game),"earned objective and real resolved response");
                for(int i=0;i<5;i++)Step(game,twin);
                c.That(Read(game).Facts==1&&Read(game,"contract.environment-cleaned").Facts==1&&Stock(game)==Fix.FromInt(7),"settled updates cannot duplicate effects/history");
                c.Equal(EnvironmentalCodes.HistoryExpired,Read(game,"contract.environment-coverage").Blocker,"evicted overlapping history refuses instead of truncating amounts");
                var quiet=Read(game,"contract.environment-recent");c.That(quiet.Available&&quiet.AmountRaw.Value.IsZero,"new complete quiet window is real zero");
                c.Equal((BigInteger)2*60*Read(game).Rate.Value.Denominator,Read(game).Rate.Value.Numerator*Read(game).ThroughTick,"exact cumulative rate follows original minute clock");
                c.Equal(spilled.AmountRaw,Read(game).AmountRaw,"cumulative loss survives short retained feed");
                c.Equal(BigInteger.Zero,empty.AmountRaw.Value,"held early facts stay detached");
                var rows=new List<ObjectiveSnapshot>();game.Seat(1).View.Feature<ObjectiveView>().CopyObjectives(rows);c.Equal(0,rows.Count,"foreign objective activity not leaked");
                c.That(Read(game,company:1).AmountRaw.Value.IsZero,"other company has no invented environmental loss");
                c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"resolved objective/work/journal saves together");
                c.Equal(untouched,isolated.Hash(),"interleaved session isolation");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
                c.That(replay.Success && replay.ActualHash == game.Hash(), "complete initial incident/hold/cancel/clock stream replays to final live hash");
                c.Equal(game.Tick - recorder.InitialStep, (long)recorder.CheckpointCount, "every advancing physical accounting update is recorded");
                Console.WriteLine("environment content replay: initial=" + recorder.InitialStep + ";final=" + game.Tick
                    + ";commands=" + recorder.CommandCount + ";checkpoints=" + recorder.CheckpointCount
                    + ";expected-hash=" + game.Hash().ToString("X16") + ";actual-hash=" + replay.ActualHash.ToString("X16"));
            }
        }
        private static void Invalid(Check c)
        {
            foreach(string fault in new[]{"unit-metadata","rate-metadata","window-metadata","validation-throw","validation-pause","validation-reenter"})
            {
                var init=new InitializeProbe();Action run=()=>New(Build(init,out _,doc=>{
                    var bad=doc.Get("independent_environment_metrics")[0].Clone();bad.Set("id",JsonValue.Of("unused.environment-invalid"));bad.Set("fault",JsonValue.Of(fault));doc.Get("independent_environment_metrics").Add(bad);
                }));
                if(fault.EndsWith("metadata",StringComparison.Ordinal))c.Throws<ContentException>(run,"all-loaded immutable metadata refusal "+fault);
                else c.Throws<InvalidOperationException>(run,"actual guarded validation callback "+fault);
                c.Equal(0,init.Initializations,"invalid unused metric cannot initialize "+fault);
            }
            foreach(string fault in new[]{"unit","resource","kind","cause","window-missing","window-zero","window-quantum","unit-mismatch","duplicate","unknown","objective-metric","objective-unit"})
            {
                var init=new InitializeProbe();
                c.Throws<ContentException>(()=>New(Build(init,out _,doc=>{
                    var bad=doc.Get("independent_environment_metrics")[1];
                    if(fault=="unit")bad.Set("unit",JsonValue.Of("missing"));
                    if(fault=="resource")bad.Set("resources",Json.Parse("[\"missing\"]"));
                    if(fault=="kind")bad.Set("kinds",Json.Parse("[\"stock-transfer\"]"));
                    if(fault=="cause")bad.Set("causes",Json.Parse("[]"));
                    if(fault=="window-missing")bad.Remove("window");
                    if(fault=="window-zero")bad.Set("window",Json.Parse("{\"numerator\":0,\"denominator\":1,\"unit\":\"minute\"}"));
                    if(fault=="window-quantum")bad.Set("window",Json.Parse("{\"numerator\":1,\"denominator\":2,\"unit\":\"minute\"}"));
                    if(fault=="unit-mismatch")bad.Set("unit",JsonValue.Of("unit.work-rate"));
                    if(fault=="duplicate")bad.Get("causes").Add(JsonValue.Of("environment.spill"));
                    if(fault=="unknown")bad.Set("legacy",JsonValue.Of(true));
                    if(fault=="objective-metric")doc.Get("environmental_objective_predicates")[0].Set("metric",JsonValue.Of("missing"));
                    if(fault=="objective-unit")doc.Get("environmental_objective_predicates")[0].Set("unit",JsonValue.Of("unit.work-rate"));
                })),"early direct environmental authoring refusal "+fault);
                c.Equal(0,init.Initializations,"invalid explicit content before Initialize "+fault);
            }
        }
        private static void Faults(Check c, bool retained = false)
        {
            foreach(string fault in new[]{"observe-null","observe-throw","observe-pause","observe-touch","observe-reenter","observe-identity"})
            foreach(bool restored in retained ? new[]{false,true} : new[]{false})
            {
                var init=new InitializeProbe();var factory=Build(init,out _,doc=>{
                    var bad=doc.Get("independent_environment_metrics")[0].Clone();bad.Set("id",JsonValue.Of("unused.environment-fault"));bad.Set("fault",JsonValue.Of(fault));doc.Get("independent_environment_metrics").Add(bad);
                });var game=New(factory);
                if (restored) game = factory.Load(game.Save());
                var idle = retained ? New(factory) : null; ulong untouched = idle?.Hash() ?? 0;
                var own = game.Seat(0).View.Feature<EnvironmentalMetricsView>();
                var foreign = game.Seat(1).View.Feature<EnvironmentalMetricsView>();
                var observer = game.Observer.Feature<EnvironmentalMetricsView>();
                // Original metric source access below is read-only engine contract diagnostics, not a host escape.
                var source = retained ? game.World.Services.Get<IEnvironmentalMetricSource>() : null;
                var prior = retained ? own.Read(Company, "contract.environment-spilled") : null;
                if (retained)
                {
                    ulong before = game.Hash();
                    c.That(prior.Available && prior.AmountRaw == BigInteger.Zero && prior.Facts == 0,
                        "actual empty cumulative history is a healthy measured zero before the negative probe");
                    c.Equal(Codes.NotOwned, foreign.Read(Company, "contract.environment-spilled").Blocker, "healthy foreign metric rule unchanged");
                    c.Equal(Codes.NotSeated, observer.Read(new PlayerId(99), "contract.environment-spilled").Blocker, "healthy unknown company remains typed refusal");
                    c.Equal(Codes.UnknownDef, own.Read(Company, "missing").Blocker, "healthy unknown definition remains typed refusal");
                    c.Equal(Codes.UnknownDef, own.Read(Company, null).Blocker, "healthy null definition has no invented default");
                    c.Equal(game.Hash(), before, "healthy public/source environmental observations remain pure");
                }
                long at = game.Tick;
                c.Throws<InvalidOperationException>(()=>Read(game,"unused.environment-fault"),"actual direct provider observation "+fault);
                c.That(game.Fault?.Phase=="observation.provider","fault belongs to original observation guard "+fault);
                if (retained)
                {
                    var first = game.Fault;
                    foreach (var view in new[] { own, foreign, observer, new EnvironmentalMetricsView(game.World, new PlayerId(-2)),
                        new EnvironmentalMetricsView(game.World, new PlayerId(99)) })
                    foreach (var company in new[] { Company, new PlayerId(1), new PlayerId(-2), new PlayerId(99), PlayerId.None })
                    foreach (string metric in new[] { "contract.environment-spilled", "environment.native-spilled", "unused.environment-fault", "missing", null })
                        c.Throws<InvalidOperationException>(() => view.Read(company, metric),
                            "cached view cutoff precedes owned, foreign, observer, invalid and unknown metric paths");
                    foreach (var company in new[] { Company, new PlayerId(1), new PlayerId(-2), new PlayerId(99), PlayerId.None })
                    foreach (string metric in new[] { "contract.environment-spilled", "environment.native-spilled", "unused.environment-fault", "missing", null })
                        c.Throws<InvalidOperationException>(() => source.Read(new EnvironmentalQuery(company, metric)),
                            "original source contract cannot bypass terminal state on missing/invalid shortcuts");
                    c.Equal(0, game.Update(), "terminal environmental observations cannot earn work or game time");
                    c.That(game.Tick == at && ReferenceEquals(first, game.Fault), "all cached/direct reads preserve original fault and clock position");
                    c.That(prior.Available && prior.AmountRaw == BigInteger.Zero && prior.Facts == 0 && prior.ThroughTick == at,
                        "previous detached healthy facts never mutate into a new partial metric result");
                    c.Equal(untouched, idle.Hash(), "faulted environmental queries leave an independent session untouched");
                    c.That(Read(idle).Available && idle.Fault == null, "independent native environment source remains live and healthy");
                }
                c.Throws<InvalidOperationException>(()=>game.Save(),"fault cannot checkpoint "+fault);
                c.Throws<InvalidOperationException>(()=>game.Hash(),"fault cannot report healthy hash "+fault);
            }
        }
    }
}
