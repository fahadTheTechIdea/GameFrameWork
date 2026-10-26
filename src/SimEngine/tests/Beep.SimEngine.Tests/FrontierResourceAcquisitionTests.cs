using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    internal static class FrontierResourceAcquisitionTests
    {
        internal const string Case = "Work source selection: joint backtracking, fresh refusal and saved acquisition";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ResourceEndpointId First = new ResourceEndpointId("colony.pools", 1),
            Alternate = new ResourceEndpointId("colony.pools", 5000000000);
        private static WorkWorkerCandidate Offer(SimGame game, Entity target, Entity worker)
        {
            var offers = new List<WorkWorkerCandidate>();
            Code result = game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("requirements.frontier", "work", target.Id, offers);
            if (!result.IsNone) throw new InvalidOperationException("source preview: " + result);
            return offers.Single(x => x.Worker == worker.Id);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin)
        {
            for (int i = 0; i < 400 && !done(); i++) { Tick(game); Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + ": " + string.Join(";", game.World.Services.Get<WorkBoard>().Orders
                .SelectMany(o => o.Nodes.Select(n => o.Id + "/" + n.Id + "/" + n.State + "/" + n.Progress.WholeTicks + "/" + n.Blocker))));
        }
        private static WorkResourceChoice Copy(WorkResourceChoice source, ResourceEndpointId endpoint, string requirement = null)
            => new WorkResourceChoice(source.Path, requirement ?? source.Requirement, endpoint, source.Quantities, source.Use, source.Rates);
        private static void Run(Check c)
        {
            // Explicit physical-only authored composition: no Tycoon provider or cost obligations.
            // Reuse the same master-selected documents and engine-side colony module.
            var physical = Content.Json.Parse(FrameworkProofContent.Read("resource-recovery.json"));
            var entities = physical.Get("entities");
            for (int i = 0; i < entities.Count; i++) entities[i].Get("components").Remove("supplier");
            var work = Content.Json.Parse(FrameworkProofContent.Read("colony-work.json"));
            work.Remove("work_cost_requirements"); work.Remove("work_requirement_groups");
            var physicalText = new System.Text.StringBuilder(); physical.WriteCanonical(physicalText);
            var workText = new System.Text.StringBuilder(); work.WriteCanonical(workText);
            var factory = Colony.Factory(physicalText.ToString(), FrameworkProofContent.Read("endpoints-flows.json"),
                additionalDefinitions: WorkerExecutionTestContent.Author(FrameworkProofContent.Read("resource-requirements.json")),
                moduleDocuments: new Dictionary<string, string> { ["colony-work.json"] = workText.ToString() });
            var game = New(factory); var board = game.World.Services.Get<WorkBoard>();
            Entity flexible = Spawn(game, "requirements.target", 20, 20), restricted = Spawn(game, "requirements.restricted", 30, 20);
            Entity a = Spawn(game, "requirements.builder", 16, 20), b = Spawn(game, "requirements.builder", 26, 20);
            ResourceId water = game.World.Content.ResourceId("water"); var pools = game.World.Services.Get<EndpointPools>();
            var transaction = game.World.Services.Get<IResourceTransaction>();
            foreach (long identity in new[] { First.Identity, Alternate.Identity })
            {
                var destination = pools.Get(identity); var owner = new ReservationOwner("proof.frontier-fill", identity);
                var source = flexible.Get<IInventory>();
                c.That(transaction.Commit(new ResourceTransferIntent(owner, Company, source.ReserveOut(owner, water, Fix.One),
                    destination.ReserveIn(owner, water, Fix.One), water, Fix.One)).Committed, "fixture fills actual native pools through the existing transfer owner");
            }
            Tick(game);
            var definition = game.World.Content.Get<ActivityDef>("requirements.frontier").Graph.Find("work");
            var first = Offer(game, flexible, a); var second = Offer(game, restricted, b);
            var contexts = new[] {
                new WorkRequirementContext(game.World, Company, flexible, definition, a, role: first.Role),
                new WorkRequirementContext(game.World, Company, restricted, definition, b, role: second.Role) };
            ulong hash = game.Hash();
            c.Equal(First, first.Result.Resources[0].Endpoint, "individual flexible admission picks the canonical first pool");
            c.Equal(First, second.Result.Resources[0].Endpoint, "restricted work can use only the same scarce first pool");
            c.That(board.CheckWorkSet(new[] { first.Result, second.Result }, contexts, out var selected, out _, out _, out _, out _),
                "the existing joint evaluator finds a feasible source set");
            c.Equal(Alternate, selected[0].Resources[0].Endpoint, "backtracking moves earlier flexible work to the alternate actual pool");
            c.Equal(First, selected[1].Resources[0].Endpoint, "joint admission leaves the sole eligible pool for restricted work");
            c.Equal(WorkRequirementStatus.Satisfied, board.BindResourceSelection(first.Result, selected[0].Resources, contexts[0]).Status,
                "selection rebinds to the current eligible native offer");
            c.Equal(hash, game.Hash(), "joint selection and rebinding reserve nothing and change no game state");
            c.Throws<NotSupportedException>(() => ((IList<WorkResourceChoice>)selected[0].Resources).Clear(), "selected identities are detached immutable facts");
            foreach (var endpoint in new[] { new ResourceEndpointId("colony.pools", 2), new ResourceEndpointId("colony.pools", 999) })
                c.That(board.BindResourceSelection(first.Result, new[] { Copy(first.Result.Resources[0], endpoint) }, contexts[0]).Status != WorkRequirementStatus.Satisfied,
                    "foreign or missing selected sources cannot substitute a current allowed endpoint");
            c.Equal(WorkRequirementStatus.Invalid, board.BindResourceSelection(first.Result,
                new[] { Copy(first.Result.Resources[0], First, "requirements.fluid") }, contexts[0]).Status, "altered authored occurrence metadata is refused");
            c.Equal(WorkRequirementStatus.Invalid, board.BindResourceSelection(first.Result, Array.Empty<WorkResourceChoice>(), contexts[0]).Status,
                "incomplete source intention cannot discard an authored obligation");
            c.Equal(hash, game.Hash(), "refused source intentions acquire no stock or machine");
            var claimOwner = new ReservationOwner("proof.frontier-obstruction", 1);
            var claim = transaction.AcquireClaims(Company, new[] { new ResourceClaimIntent(ReservationDirection.Outbound, claimOwner, Alternate,
                selected[0].Resources[0].Quantities) }, System.Array.Empty<ResourceReservation>());
            c.That(claim.Committed, "real native promise changes current selected-source availability");
            var fresh = Offer(game, flexible, a); hash = game.Hash();
            c.That(board.BindResourceSelection(fresh.Result, selected[0].Resources, contexts[0]).Status != WorkRequirementStatus.Satisfied,
                "retained joint source refuses fresh shortage instead of silently using the first pool");
            c.Equal(hash, game.Hash(), "fresh shortage refusal is pure");
            c.That(transaction.ReleaseClaims(new ResourceClaimReleaseIntent(Company, claim.Receipts, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed, "same native receipt owner releases the obstruction");
            game.Seat(0).Submit(new PostOrderCommand { Target = flexible.Id, Plan = "requirements.frontier" });
            game.Seat(0).Submit(new PostOrderCommand { Target = restricted.Id, Plan = "requirements.frontier" });
            Tick(game);
            var orders = board.Orders.Where(o => o.PlanKey == "activity:requirements.frontier").OrderBy(o => o.Target.Value).ToArray();
            c.Equal(2, orders.Length, "ordinary player commands create both engine-owned work orders");
            c.That(orders.All(o => board.Job(o.Node("work").JobId)?.Worker.IsValid == true), "normal dispatch acquires both real machines without starving restricted work");
            c.Equal(Alternate, orders[0].Node("work").Resources[0].Choice.Endpoint, "actual first job preserves jointly selected alternate source");
            c.Equal(First, orders[1].Node("work").Resources[0].Choice.Endpoint, "actual second job acquires its sole eligible source");
            c.Equal(Fix.One, pools.Get(First.Identity).Amount(water), "assignment does not consume distant material");
            c.Equal(Fix.One, pools.Get(Alternate.Identity).Amount(water), "selected alternate stock remains physical during travel");
            c.Equal(Fix.FromRatio(1, 4), pools.Get(First.Identity).Available(water), "restricted job holds its native three-quarter promise");
            c.Equal(Fix.FromRatio(1, 4), pools.Get(Alternate.Identity).Available(water), "flexible job holds the alternate native promise");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "travelling selected sources persist in the existing node and endpoint codecs");
            game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); Tick(game); Tick(twin);
            c.Equal(hash, game.Hash(), "one paused game clock freezes physical execution"); game.Clock.Resume(); twin.Clock.Resume();
            Until(game, () => orders.All(o => o.Node("work").Progress.WholeTicks > 0), "physical source work", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved source selections continue identical physical progress");
            twin = factory.Load(game.Save()); Until(game, () => orders.All(o => o.State == OrderState.Done), "physical completion", twin);
            c.Equal(game.Hash(), twin.Hash(), "partial physical work completes identically after save/load");
            c.Equal(Fix.FromRatio(1, 4), pools.Get(First.Identity).Amount(water), "restricted source spends exactly once on actual productive work");
            c.Equal(Fix.FromRatio(1, 4), pools.Get(Alternate.Identity).Amount(water), "alternate source spends exactly once on actual productive work");
            c.That(orders.All(o => o.Node("work").Resources.All(r => r.Spent)), "completion retains earned source history, not live material promises");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed source histories round-trip without another ledger");
        }
    }
}
