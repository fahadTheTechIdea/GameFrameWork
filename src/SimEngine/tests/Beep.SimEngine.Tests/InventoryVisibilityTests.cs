using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Player commands, views and sole clock only; the selected module owns the actual sight rule/state.
    internal static class InventoryVisibilityTests
    {
        internal const string Case = "Inventory visibility: cached hidden entities refuse facts during physical saved work";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half, DirectResourceContentModule resources)
            => ResourceContentContractTests.Factory(half, resources, worldEdit: root =>
            {
                var scenario = root.Get("scenarios")[0]; scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                if (scenario.Get("settings") == null) scenario.Set("settings", JsonValue.NewObject());
                scenario.Get("settings").Set(InventorySightModule.ModuleId, Json.Parse("{\"target_kind\":\"entity\",\"target\":\"destination\",\"initial_hidden\":false}"));
            }, extraModules: new GameModule[] { new InventorySightModule() },
                extraDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
        private static void Hidden(Check c, SimGame game, EntityId target, ResourceId resource)
        {
            ulong before = game.Hash(); long time = game.Clock.StepIndex; var view = game.Seat(0).View; var stock = view.Feature<InventoryView>();
            c.That(!view.TryGetEntity(target, out _), "same actual visibility hides target from entity projection");
            c.That(!stock.TryGetAmount(target, resource, out var hiddenAmount) && hiddenAmount.IsZero, "cached ID cannot disclose a hidden local quantity");
            c.Equal(Codes.NotFound, stock.ReadResourcePort(target, new Code("stock"), resource, out var port), "cached ID cannot bypass sight for port stock");
            c.That(port.Stock == null && !port.Endpoint.IsValid, "hidden port query outputs only default facts");
            c.That(!stock.TryGetCapacity(target, Code.None, out var capacity) && capacity.Usage.Count == 0, "cached ID cannot disclose hidden capacity");
            c.That(!stock.TryGetReservations(target, Code.None, out var reservations) && reservations.Count == 0, "cached ID cannot disclose hidden tool/haul receipts");
            var choices = new List<ResourcePortChoiceSnapshot> { default };
            c.Equal(Codes.NotFound, stock.ReadResourcePortChoices(target, new Code("stock"), choices), "hidden choices use the same original visibility authority");
            c.Equal(0, choices.Count, "hidden choices clear caller seed without provider output");
            var inspect = new List<Facet>(); c.That(!view.Inspect(target, inspect) && inspect.Count == 0, "generic inspect and inventory panels agree");
            var foreign = game.Seat(1).View.Feature<InventoryView>();
            c.Equal(Codes.NotFound, foreign.ReadResourcePort(target, new Code("stock"), resource, out _), "foreign hidden refusal leaks no ownership-specific fact");
            c.That(!foreign.TryGetCapacity(target, Code.None, out _) && !foreign.TryGetReservations(target, Code.None, out _), "foreign protected facts remain absent");
            var observer = game.Observer.Feature<InventoryView>();
            c.Equal(Code.None, observer.ReadResourcePort(target, new Code("stock"), resource, out var actual), "explicit observer remains authorized to actual physical stock");
            c.That(actual.Ready && actual.Stock.AmountOf(resource) == Fix.FromInt(6), "hiding does not move or duplicate material");
            c.That(observer.TryGetReservations(target, Code.None, out var actualReservations) && actualReservations.Count > 0, "actual reusable tool receipt still exists while player facts are withheld");
            c.Equal(before, game.Hash(), "private/observer/missing observations are pure");
            c.Equal(time, game.Clock.StepIndex, "inventory visibility never advances game time");
        }
        private static void Checkpoint(Check c, GameFactory factory, SimGame game, EntityId target, ResourceId resource)
        {
            var stock = game.Seat(0).View.Feature<InventoryView>();
            c.Equal(Code.None, stock.ReadResourcePort(target, new Code("stock"), resource, out var retained), "current visible physical port is observable");
            c.That(stock.TryGetCapacity(target, Code.None, out var retainedCapacity), "visible capacity is observable");
            c.That(stock.TryGetAmount(target, resource, out var retainedAmount) && retainedAmount == retained.Stock.AmountOf(resource), "local quantity equals the same actual visible named store");
            c.That(stock.TryGetReservations(target, Code.None, out var retainedReservations) && retainedReservations.Count > 0, "visible reusable receipts are observable");
            var visible = game.Seat(0).Snapshot;
            PermanentRemovalTests.Submit(c, game, new SetInventorySightCommand { Hidden = true });
            c.That(game.Seat(0).Snapshot.Removed.Contains(target) && !game.Seat(0).Snapshot.Entities.Any(x => x.Id == target),
                "normal engine command publishes loss-of-sight delta, not hidden activity");
            game.Clock.Pause(); Hidden(c, game, target, resource);
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual hidden visibility and original inventory/receipts save together");
            // Existing one-clock design: local pause/speed are pacing controls, not saved simulation age.
            twin.Clock.Pause();
            Hidden(c, twin, target, resource); long stopped = game.Clock.StepIndex;
            game.Update(); twin.Update(); c.That(stopped == game.Clock.StepIndex && game.Hash() == twin.Hash(),
                "hidden paused twins: at=" + stopped + "/" + game.Clock.StepIndex + "/" + twin.Clock.StepIndex +
                " paused=" + game.Clock.Paused + "/" + twin.Clock.Paused + " hash=" + game.Hash() + "/" + twin.Hash());
            Hidden(c, game, target, resource);
            c.That(visible.Entities.Any(x => x.Id == target) && retained.Stock.AmountOf(resource) == Fix.FromInt(6) &&
                retainedCapacity.Usage.Count > 0 && retainedReservations.Count > 0, "retained authorized facts stay immutable after visibility change");
            game.Clock.Resume();
            PermanentRemovalTests.Submit(c, game, new SetInventorySightCommand { Hidden = false });
            c.That(game.Seat(0).Snapshot.Spawned.Contains(target) && game.Seat(0).Snapshot.Entities.Any(x => x.Id == target),
                "reveal publishes same surviving identity through original snapshot owner");
            c.Equal(Code.None, stock.ReadResourcePort(target, new Code("stock"), resource, out var revealed), "previously obtained view follows current visibility dynamically");
            c.That(revealed.Ready && revealed.Stock.AmountOf(resource) == retained.Stock.AmountOf(resource), "reveal restores actual stock observation without regrant");
            c.That(stock.TryGetAmount(target, resource, out var revealedAmount) && revealedAmount == retainedAmount, "retained quantity stays detached and reveal reads the surviving stock owner");
            c.That(stock.TryGetCapacity(target, Code.None, out _) && stock.TryGetReservations(target, Code.None, out _), "revealed panels resume through existing public API");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, Beep.SimEngine.Kernel.EngineInfo.Build, "current visibility contract identity");
            foreach (bool half in new[] { false, true }) foreach (bool cancel in new[] { false, true })
            {
                var resources = new DirectResourceContentModule(); var factory = Factory(half, resources);
                ResourceContentContractTests.Healthy(c, half, cancel, factory, resources, partialCheckpoint: Checkpoint);
            }
        }
    }
}
