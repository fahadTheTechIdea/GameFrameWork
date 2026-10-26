using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class ProductionStaffingTests
    {
        internal const string Case = "Production staffing: authored distinct physical machines, retained process holds and saved player control";
        internal const string IndependentCase = "Production staffing: independent JSON metadata shares physical facility teams and replay";
        internal static void Register()
        {
            TestRunner.Add(Case, c => Run(c, false));
            TestRunner.Add(IndependentCase, c => Run(c, true));
        }
        private static readonly Code Process = new Code("process");
        internal static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "native-survey", Seed = 753, Seats = { Controller.Human, Controller.Human } });
        internal static EntityId Find(SimGame game, string definition)
            => game.Seat(0).Snapshot.Entities.Single(e => e.Def == definition).Id;
        private static TransformationManagementView View(SimGame game) => game.Seat(0).View.Feature<TransformationManagementView>();
        internal static TransformationSnapshot State(SimGame game)
        {
            if (!View(game).TryGetTransformation(Find(game, "staff.factory"), Process, out var value)) throw new InvalidOperationException("missing native process");
            return value;
        }
        internal static TransformationStaffingSnapshot Staffing(SimGame game, string recipe = "staff.batch")
        {
            Code read = View(game).ReadStaffing(Find(game, "staff.factory"), Process, recipe, out var facts);
            if (!read.IsNone || facts.Count != 1) throw new InvalidOperationException("missing native staffing " + read);
            return facts[0];
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("staffing fault at " + game.Tick + ": " + game.Fault);
            if (twin == null) return;
            twin.Update(); if (twin.Fault != null || twin.Hash() != game.Hash()) throw new InvalidOperationException("staffing twin divergence at " + game.Tick);
        }
        internal static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 200 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + "/time=" + game.Tick + "/process=" + State(game).Phase +
                "/blocker=" + State(game).Blocker + "/staff=" + string.Join(";", Staffing(game).Requirements.Select(n => n.Id + ":" + n.Assignments.Count)));
        }
        internal static CommandResult Order(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary player intent preview");
            c.Equal(hash, game.Hash(), "preview is pure"); var result = TestKit.Order(game, 0, command);
            c.That(result.Accepted, "ordinary player intent accepted: " + result.Refusal);
            if (!result.Accepted) throw new InvalidOperationException("refused staffing intent " + result.Refusal); return result;
        }
        internal static void Enqueue(Check c, SimGame game, string recipe) => Order(c, game, new EnqueueTransformationCommand {
            Transformer = Find(game, "staff.factory"), Capability = Process, Transformation = recipe, Count = 1 });
        private static void Hold(Check c, SimGame game, EntityId worker, bool held) => Order(c, game, new SetWorkerHeldCommand { Worker = worker, Held = held });
        internal static Fix Stock(SimGame game)
        {
            var facets = new List<Facet>(); if (!game.Seat(0).View.Inspect(Find(game, "staff.factory"), facets)) throw new InvalidOperationException("missing stock");
            return facets.Where(f => f.Key == Storage.FacetStock && f.Subject == "steel").Aggregate(Fix.Zero, (sum, row) => sum + row.Value);
        }
        private static JsonValue Gate(JsonValue content) => content.Get("entities")[0].Get("components").Get("staffed_transformation_gate");
        private static void Run(Check c, bool independent)
        {
            GameFactory Factory(Action<JsonValue> edit = null, Action<JsonValue> editWorld = null)
                => StaffedTransformationGame.Factory(edit, editWorld, independent);
            void Invalid(Check check, Action<JsonValue> edit, string message)
                => check.Throws<ContentException>(() => New(Factory(edit)), message);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build, "actual staffing engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat, "gate introduces no independent runtime state");
            var factory = Factory(); var game = New(factory);
            var limits = new ReplayLimits(4 * 1024 * 1024, 800); var recorder = game.RecordReplay(limits);
            var target = Find(game, "staff.factory"); ulong hash = game.Hash(); var empty = Staffing(game);
            var metadata = game.World.Entities.Get(target).Def.Component<IStaffedTransformationGateDefinition>(new Code("staffing"));
            c.Equal(independent, metadata is ProofStaffingGateDef, "selected JSON metadata is the actual independent contract implementation");
            c.Equal(StaffedTransformationGate.Shortage, empty.Blocker, "idle role-compatible machines do not authorize production");
            c.Equal(2, empty.Requirements.Count, "two explicit authored staffing demands");
            c.Equal(2, empty.Requirements[0].Roles.Count, "primary accepts authored alternative roles");
            c.That(empty.Requirements.All(n => n.Assignments.Count == 0 && n.Count == 1), "idle is no actual assignment");
            c.Equal(hash, game.Hash(), "staffing diagnostics are pure");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<TransformationManagementView>().ReadStaffing(target, Process,
                "staff.batch", out var hidden), "staffing is company private"); c.Equal(0, hidden.Count, "foreign staffing exposes no actor");
            c.Equal(Codes.UnknownDef, View(game).ReadStaffing(target, Process, "missing.recipe", out hidden), "unknown recipe cannot inherit staffing");
            Enqueue(c, game, "staff.batch");
            c.Equal(StaffedTransformationGate.Shortage, State(game).Blocker, "unassigned production waits");
            c.That(State(game).Wip.Stock.IsEmpty && State(game).Tools.Receipts.Count == 0, "staffing failure promises no resource prefix");
            int order = (int)Order(c, game, new PostOrderCommand { Plan = "staff.operate", Target = target, Priority = 60 }).Arg0;
            c.That(!Staffing(game).Blocker.IsNone, "just posting a work intent does not teleport or staff a process");
            Until(game, () => Staffing(game).Blocker.IsNone, "both physical machine nodes earn actual work");
            var staffed = Staffing(game); var actors = staffed.Requirements.SelectMany(n => n.Assignments).ToList();
            c.Equal(2, actors.Count, "complete team uses two native assignments");
            c.Equal(2, actors.Select(a => a.Worker).Distinct().Count(), "one machine cannot count twice");
            c.Equal(new WorkerRoleId("staff.technician"), actors.Single(a => a.Node == "primary").Role,
                "alternative-only machine takes primary so survey-only machine can fill secondary");
            c.That(actors.All(a => a.Order == order && a.Target == target && a.PhaseTarget == target &&
                a.PhasePurpose == WorkPhasePurpose.Productive && a.State == JobState.Assigned && !a.Progress.Equals(WorkProgress.Zero)),
                "exact productive native jobs, not tags or state labels");
            Until(game, () => State(game).Progress.WholeTicks > 0, "physical staffing admits native resource startup");
            var started = State(game); var token = started.Tools.Receipts.Single().Token;
            c.Equal(Fix.FromInt(18), Stock(game), "only process feed moved to original WIP");
            c.Equal(1, started.Tools.Receipts.Count, "original stock owner holds one actual tool token");
            EntityId primary = actors.Single(a => a.Node == "primary").Worker;
            Hold(c, game, primary, true); var paused = State(game); var preserved = paused.Progress;
            c.Equal(StaffedTransformationGate.Shortage, paused.Blocker, "ordinary machine control removes actual staffing immediately");
            var partial = Staffing(game);
            c.Equal(1, partial.Requirements.Sum(n => n.Assignments.Count), "only remaining actual actor is counted");
            c.Equal(token, paused.Tools.Receipts.Single().Token, "staffing loss retains original material tool commitment");
            var twin = factory.Load(game.Save()); Step(game, twin); Step(game, twin);
            c.Equal(preserved, State(game).Progress, "lost staffing earns no process progress");
            c.Equal(Fix.FromInt(18), Stock(game), "lost staffing consumes no further feed");
            c.Equal(game.Hash(), twin.Hash(), "saved shortage retains exact jobs/WIP/tools");
            c.That(staffed.Requirements.All(n => n.Ready), "retained earlier diagnostics stay detached");
            Hold(c, game, primary, false); Hold(c, twin, primary, false);
            Until(game, () => State(game).QueueCount == 0, "same work owner reacquires and completes timed batch", twin);
            c.Equal(Fix.FromInt(19), Stock(game), "one batch consumes two and returns one authored product");
            c.Equal(0, State(game).Tools.Receipts.Count, "whole completion releases actual tools");
            c.That(Staffing(game).Requirements.All(n => n.Ready), "staffing activity retains its own authored interval, not another processor ledger");
            c.Equal(game.Hash(), twin.Hash(), "saved machine replacement and batch completion identical");

            Enqueue(c, game, "staff.flow");
            c.That(State(game).Progress.WholeTicks > 0, "continuous production uses the same actual staffing gate");
            var flowing = State(game); var flowToken = flowing.Tools.Receipts.Single().Token;
            Hold(c, game, primary, true); var flowProgress = State(game).Progress; Fix flowStock = Stock(game);
            twin = factory.Load(game.Save()); Step(game, twin); Step(game, twin);
            c.Equal(flowProgress, State(game).Progress, "unstaffed continuous flow earns nothing");
            c.Equal(flowStock, Stock(game), "unstaffed continuous flow burns nothing");
            c.Equal(flowToken, State(game).Tools.Receipts.Single().Token, "continuous shortage preserves the original tool hold");
            Order(c, game, new CancelTransformationCommand { Transformer = target, Capability = Process, Index = 0 });
            c.Equal(0, State(game).QueueCount, "continuous cancellation needs no fabricated staffing");
            c.Equal(flowStock, Stock(game), "consumed continuous feed is not refunded");
            c.Equal(0, State(game).Tools.Receipts.Count, "continuous stop releases its actual tool");
            Hold(c, game, primary, false);
            Until(game, () => Staffing(game).Blocker.IsNone, "existing physical work resumes without a new scheduler");
            Enqueue(c, game, "staff.batch");
            var batchStock = Stock(game); var working = State(game);
            c.That(working.Progress.WholeTicks > 0, "second real batch starts only with actual staff");
            Order(c, game, new CancelOrderCommand { Order = order });
            c.Equal(StaffedTransformationGate.Shortage, State(game).Blocker, "cancelled staffing order cannot act as an entity tool");
            game.Seat(0).Submit(new CancelTransformationCommand { Transformer = target, Capability = Process, Index = 0 });
            twin = factory.Load(game.Save()); Step(game, twin);
            c.Equal(0, State(game).QueueCount, "saved process cancellation returns material without machines");
            c.Equal(batchStock + Fix.FromInt(2), Stock(game), "only real unconsumed batch WIP returns");
            c.Equal(0, State(game).Tools.Receipts.Count, "cancelled batch releases only its real tool receipt");
            c.Equal(game.Hash(), twin.Hash(), "pending process cancellation restores with native owners");
            var verified = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(verified.Success, "ordinary staffing/production commands replay deterministically");
            c.Equal(game.Hash(), verified.ActualHash, "staffing replay reaches the actual final live state");

            Invalid(c, json => Gate(json).Remove("requirements"), "missing staffing requirements refuse");
            Invalid(c, json => Gate(json).Get("requirements")[0].Set("count", JsonValue.Number("0")), "nonpositive count refuses");
            Invalid(c, json => Gate(json).Get("requirements")[1].Set("id", JsonValue.Of("primary")), "duplicate stable requirement ID refuses");
            Invalid(c, json => Gate(json).Get("requirements")[0].Set("node", JsonValue.Of("missing")), "missing activity node refuses");
            Invalid(c, json => Gate(json).Get("requirements")[0].Set("activity", JsonValue.Of("unknown")), "unknown activity refuses");
            Invalid(c, json => Gate(json).Get("requirements")[1].Set("roles", Json.Parse("[\"staff.technician\"]")), "node-incompatible role refuses");
            Invalid(c, json => Gate(json).Get("requirements")[0].Set("roles", Json.Parse("[\"survey\",\"survey\"]")), "duplicate alternative role refuses");
            Invalid(c, json => Gate(json).Get("requirements")[0].Set("roles", Json.Parse("[]")), "empty alternatives refuse");

            var overlapping = New(Factory(json => {
                var need = Gate(json).Get("requirements")[1]; need.Set("node", JsonValue.Of("primary"));
                need.Set("roles", Json.Parse("[\"staff.technician\"]"));
            }));
            Enqueue(c, overlapping, "staff.batch");
            Order(c, overlapping, new PostOrderCommand { Plan = "staff.operate", Target = Find(overlapping, "staff.factory"), Priority = 60 });
            Until(overlapping, () => Staffing(overlapping).Requirements.Sum(n => n.Assignments.Count) == 1, "overlapping actual staffing requirements");
            Step(overlapping); Step(overlapping);
            c.Equal(StaffedTransformationGate.Shortage, State(overlapping).Blocker, "same native productive actor cannot satisfy two needs");
            c.Equal(1, Staffing(overlapping).Requirements.Sum(n => n.Assignments.Count), "maximum matching counts contested actor only once");
            c.Equal(Fix.FromInt(20), Stock(overlapping), "contested staffing spends no input prefix");
            c.That(State(overlapping).Wip.Stock.IsEmpty && State(overlapping).Tools.Receipts.Count == 0, "contested staffing acquires no material tools");

            var large = New(Factory(json =>
                Gate(json).Get("requirements")[0].Set("count", JsonValue.Number(int.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture)))));
            c.Equal(int.MaxValue, Staffing(large).Requirements[0].Count, "no artificial platform-style staffing count ceiling");
            c.Equal(StaffedTransformationGate.Shortage, Staffing(large).Blocker, "large demand uses actual actors, not allocated ghost slots");
            c.Throws<ContentException>(() => New(Factory(editWorld: json => {
                var modules = json.Get("scenarios")[0].Get("modules"); var selected = JsonValue.NewArray();
                for (int i = 0; i < modules.Count; i++) if (modules[i].Text != WorkModule.ModuleId) selected.Add(modules[i]);
                json.Get("scenarios")[0].Set("modules", selected);
            })), "selected staffing without work authoring/services refuses before Update");
        }
    }
}
