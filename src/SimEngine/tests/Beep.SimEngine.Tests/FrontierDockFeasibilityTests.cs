using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;

namespace Beep.SimEngine.Tests
{
    public static class FrontierDockFeasibilityTests
    {
        public const string Case = "Response frontier docks: shared service capacity, real waiting and saved completion";
        public static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Source = new Code("damage");
        internal static GameFactory Fixture(bool two = false, bool constrained = false, bool docked = true, bool single = false, int stock = 8, Action<JsonValue> edit = null, int[] fleet = null)
            => IncidentPreparednessTests.Fixture(fleet: fleet ?? (single ? new[] { 3 } : new[] { 1, 2 }),
                documentName: "prepared-frontier-resources.json", edit: root => {
                    var parts = root.Get("entities")[1].Get("components");
                    parts.Get("point_stock").Set("initial", JsonValue.Number(stock.ToString()));
                    if (docked) {
                        var dock = Json.Parse(FrameworkProofContent.Read("point-docks.json")).Get("entities")[1].Get("components").Get("point_dock").Clone();
                        dock.Set("work_roles", Json.Parse("[\"haul\",\"inspect\"]"));
                        if (two) dock.Get("slots").Add(Json.Parse("{\"id\":\"unload.bay.b\",\"kind\":\"service\",\"offset\":3}"));
                        parts.Set("point_dock", dock);
                    }
                    if (constrained) {
                        root.Get("point_interval_regions").Add(Json.Parse("{\"id\":\"frontier.inspector\",\"space\":\"field.points\",\"minimum\":0,\"maximum\":12}"));
                        var policy = Json.Parse(FrameworkProofContent.Read("worker-standby.json")).Get("work_policies")[0].Clone();
                        policy.Set("id", JsonValue.Of("frontier.inspector")); policy.Set("area", JsonValue.Of("frontier.inspector"));
                        var policies = Json.Parse("[]"); policies.Add(policy); root.Set("work_policies", policies);
                        root.Get("entities")[3].Get("components").Get("work_policy").Set("profile", JsonValue.Of("frontier.inspector"));
                    }
                    edit?.Invoke(root);
                });
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 613 }; request.Mods.Add("proof.shipments");
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static IncidentPreparednessSnapshot Read(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<IncidentsView>().ReadPreparedness(Find(game, "destination").Id, Source, out var result);
            if (!code.IsNone) throw new InvalidOperationException("frontier docks refused: " + code); return result;
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("dock time=" + game.Tick + " " + game.Fault);
            if (twin != null) Step(twin);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " nodes=" +
                string.Join(";", Board(game).Orders.SelectMany(o => o.Nodes.Select(n => n.Id + "/" + n.State + "/" + n.Progress + "/" + n.Blocker))) +
                " jobs=" + string.Join(";", Board(game).Jobs.Select(j => j.Id + "/" + j.State + "/" + j.DestinationDockSlot + "/" + j.Blocker)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "joint service semantics identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no persistent preparedness/dock owner");
            var factory = Fixture(); var game = New(factory); ulong hash = game.Hash(); var one = Read(game);
            c.That(game.World.Space is PointSpace && !game.World.Services.Has<IEconomy>(), "actual independent geometry, no platform/business requirement");
            c.That(one.Prospective && Board(game).Orders.Count == 0, "read creates no incident/order/job");
            c.Equal(2, one.FrontierWorkersCovered, "actual distinct machines individually covered");
            c.That(one.Nodes.Where(n => n.Frontier).All(n => n.Criteria.Status == WorkRequirementStatus.Satisfied), "each candidate separately admits to existing bay");
            c.That(one.FrontierResources.Value.Ready, "actual combined material check still ready");
            c.Equal(WorkDockCodes.Full, one.FrontierDockBlocker.Value, "one service bay cannot serve both at once");
            c.That(!one.ReadyForFrontier && one.FrontierDocks.Count == 0, "waiting capacity is not productive capacity or misleading partial assignment");
            c.Equal(hash, game.Hash(), "joint dock search acquires no live claims or motion");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "prospective shortage adds no saved state");

            var readyGame = New(Fixture(two: true)); hash = readyGame.Hash(); var ready = Read(readyGame);
            c.That(ready.ReadyForFrontier && ready.FrontierDockBlocker.Value.IsNone, "two real disjoint service bays support canonical frontier");
            c.Equal(2, ready.FrontierDocks.Count, "detached complete selected service set");
            c.Equal(2, ready.FrontierDocks.Select(d => d.Slot).Distinct().Count(), "same slot not counted twice");
            c.That(ready.FrontierDocks.All(d => d.Endpoint == Find(readyGame, "destination").Id && d.Worker.IsValid &&
                Find(readyGame, "destination").Get<IWorkDock>().Slots.Any(s => s.Id == d.Slot && s.Kind == WorkDockSlotKind.Service)), "only actual authored service IDs and covered workers");
            c.Equal(hash, readyGame.Hash(), "successful read still claims nothing");
            c.Throws<NotSupportedException>(() => ((IList<IncidentPreparednessDock>)ready.FrontierDocks).Clear(), "slot facts immutable");
            c.Equal(Codes.NotFound, readyGame.Seat(1).View.Feature<IncidentsView>().ReadPreparedness(Find(readyGame, "destination").Id, Source, out var hidden), "foreign company denied private service facts");
            c.That(hidden == null && hash == readyGame.Hash(), "private refusal leaks no partial facts or mutation");
            ((PointSpace)readyGame.World.Space).OverlapFault = SpatialOverlapStatus.Overlapping;
            var overlap = Read(readyGame);
            c.That(!overlap.ReadyForFrontier && overlap.FrontierDockBlocker == WorkDockCodes.Full, "different slot IDs do not override actual provider overlap");
            ((PointSpace)readyGame.World.Space).OverlapFault = null;
            c.That(ready.ReadyForFrontier && ready.FrontierDocks.Count == 2 && Read(readyGame).ReadyForFrontier, "retained facts frozen and current geometry freshly rechecked");

            var restricted = New(Fixture(two: true, constrained: true)); hash = restricted.Hash(); var alternate = Read(restricted);
            c.That(alternate.ReadyForFrontier, "prefix search revisits first slot for area-constrained second worker");
            c.Equal(new Code("unload.bay.b"), alternate.FrontierDocks.Single(d => d.Node == "a.flex").Slot, "unrestricted worker offers later service bay");
            c.Equal(new Code("unload.bay"), alternate.FrontierDocks.Single(d => d.Node == "b.inspect").Slot, "actual inspector area keeps sole allowed service bay");
            c.Equal(hash, restricted.Hash(), "alternative search preserves all actual jobs/geometry/stock");
            c.That(!Read(New(Fixture(single: true))).FrontierDockBlocker.HasValue, "worker shortage leaves joint docks explicitly unevaluated");
            c.That(!Read(New(Fixture(stock: 0))).FrontierDockBlocker.HasValue, "missing prerequisite does not fabricate dock readiness");
            var undocked = Read(New(Fixture(docked: false)));
            c.That(undocked.ReadyForFrontier && undocked.FrontierDockBlocker.Value.IsNone && undocked.FrontierDocks.Count == 0, "ordinary undocked work creates no imaginary berth");

            foreach (bool two in new[] { false, true })
            {
                factory = Fixture(two: two); game = New(factory); Order order = Raise(c, game);
                Until(game, () => order.Nodes.Take(2).All(n => Board(game).Job(n.JobId)?.Worker.IsValid == true), "real dock jobs acquired");
                Job first = Board(game).Job(order.Node("a.flex").JobId), second = Board(game).Job(order.Node("b.inspect").JobId);
                c.That(first.Worker != second.Worker && first.DestinationDockSlot != second.DestinationDockSlot, "sole existing jobs own distinct machine and slot intentions");
                if (!two) {
                    c.That(Board(game).IsDockWaiting(second), "actual second job owns waiting slot rather than service overlap");
                    var queued = Read(game);
                    c.That(!queued.ReadyForFrontier && queued.FrontierDockBlocker == WorkDockCodes.Full, "retained waiting job cannot count incumbent's live service claim");
                    c.Equal(0, queued.FrontierDocks.Count, "failed live joint check exposes no fabricated promotion");
                }
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual travelling/waiting intentions save through existing work owner");
                game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); long time = game.Tick; Step(game, twin); Read(game);
                c.That(game.Hash() == hash && game.Tick == time, "sole paused clock freezes physical dock work");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, () => order.Node("a.flex").Progress.WholeTicks > 0 && (!two || order.Node("b.inspect").Progress.WholeTicks > 0), "physical service reached", twin);
                if (two) c.That(Read(game).ReadyForFrontier, "actual two retained service claims exclude only their own jobs");
                else c.That(order.Node("b.inspect").Progress.WholeTicks == 0, "physical wait earns no overlapping productive work");
                c.Equal(game.Hash(), twin.Hash(), "saved actual movement and service continue identically");
                twin = factory.Load(game.Save());
                Until(game, () => order.State == OrderState.Done, "real work, departure, promotion and restart complete", twin);
                c.Equal(game.Hash(), twin.Hash(), "saved partial physical response finishes identically");
                c.That(!Find(game, "destination").Get<IIncidentResponse>(Source).ReadIncident().Active, "only actual completed response resolves episode");
                c.Equal(Fix.Zero, Find(game, "destination").Get<IOutletPort>().Amount(Supply(game)), "real service consumes eight materials once");
                c.That(first.DestinationDockSlot.IsNone && second.DestinationDockSlot.IsNone, "physical departure releases actual native intentions");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed existing state saves with no preparedness cache");
            }
        }
    }
}
