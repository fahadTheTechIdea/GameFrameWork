using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Authored composition and player commands/views/clock/Update only; no account or gameplay writes.
    internal static class BusinessViewFaultCutoffTests
    {
        internal const string Case = "Public account views: retained fault cutoff private outputs and healthy saved production";
        private static readonly Code Pulse = new Code("pulse");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest
        { Scenario = "process", Seed = 306, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Actor(SimGame g) => g.Seat(0).Snapshot.Entities.Single(x => x.Def == "emitter").Id;
        private static void Input(Check c, SimGame g, Command command)
        {
            ulong hash = g.Hash(); c.Equal(Code.None, g.Seat(0).Preview(command), "ordinary engine player preview");
            c.Equal(hash, g.Hash(), "player preview mutates no accounts/time"); int sequence = g.Seat(0).Submit(command); g.Update();
            if (g.Fault != null) throw new InvalidOperationException("view proof clock=" + g.Clock.StepIndex + " " + g.Fault.Message);
            long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(x => x.Sequence == sequence).Accepted, "normal input applied in Update");
        }
        private static IReadOnlyList<Action> Properties(BusinessView view) => new Action[]
        {
            () => _ = view.HasAccounts, () => _ = view.HasValuation, () => _ = view.HasBudgets,
            () => _ = view.HasGenerationIncome, () => _ = view.HasPurchasing, () => _ = view.Rules,
            () => _ = view.Currency, () => _ = view.Policy, () => _ = view.Mode,
            () => _ = view.Quote(ResourceId.None, Fix.Zero)
        };
        private static void Session(Check c, string mode, bool half)
        {
            var f = CancellationCostDurabilityTests.Independent(mode, half); var g = New(f); var idle = New(f); ulong untouched = idle.Hash();
            var own = g.Seat(0).View.Feature<BusinessView>(); var foreign = g.Seat(1).View.Feature<BusinessView>();
            ulong hash = g.Hash();
            c.That(own.HasAccounts && own.HasBudgets && !own.HasValuation && !own.HasGenerationIncome && !own.HasPurchasing,
                "actual optional account capabilities, no fabricated price/income/trading providers");
            c.That(own.Mode == BusinessMode.Unavailable && own.Policy == null && own.Currency.Id == "unit.cancellation.money"
                && own.Rules.MoneyEnabled && !own.Rules.TycoonEnabled, "existing optional mode/currency meanings retained");
            c.Equal(Code.None, own.ReadAccount(new PlayerId(0), out var retained), "owned immutable account captured");
            c.Equal(Fix.FromInt(20), retained.Balance, "actual authored opening balance");
            c.Equal(Codes.NotOwned, foreign.ReadAccount(new PlayerId(0), out var hidden), "healthy foreign account refusal unchanged");
            c.That(hidden.Policy == null, "healthy foreign read has no output prefix");
            var rows = new List<MonetaryBudgetReceipt> { default };
            c.Equal(Code.None, own.CopyBudgets(new PlayerId(0), rows), "healthy original budget collection"); c.Equal(0, rows.Count, "copy replaces stale caller rows");
            rows.Add(default); c.Equal(Codes.NotOwned, foreign.CopyBudgets(new PlayerId(0), rows), "healthy foreign budget refusal");
            c.Equal(0, rows.Count, "refused budget copy clears stale private prefix");
            c.Equal(hash, g.Hash(), "all healthy optional view reads pure");
            Input(c, g, new EnqueueTransformationCommand { Transformer = Actor(g), Capability = Pulse, Transformation = "pulse", Count = 3 });
            Input(c, g, new ConfigureOperationCommand { Entity = Actor(g), Operation = Pulse, Enabled = false, Target = Fix.One });
            Command input = mode.StartsWith("append", StringComparison.Ordinal)
                ? new EnqueueTransformationCommand { Transformer = Actor(g), Capability = Pulse, Transformation = "pulse" }
                : new CancelTransformationCommand { Transformer = Actor(g), Capability = Pulse, Index = 1 };
            if (mode.Contains("admission"))
            {
                g.Clock.Pause(); c.Throws<InvalidOperationException>(() => g.Seat(0).Preview(input), "actual monetary admission fault");
            }
            else
            {
                c.Equal(Code.None, g.Seat(0).Preview(input), "legitimate intent before bad callback");
                g.Seat(0).Submit(input); g.Update();
            }
            c.Equal("transformations.queue-account", g.Fault?.Phase, "real first account fault owns cutoff");
            var first = g.Fault; long at = g.Clock.StepIndex;
            foreach (var view in new[] { own, foreign })
            {
                foreach (var query in Properties(view)) c.Throws<InvalidOperationException>(query, "retained metadata/absence/quote cannot bypass engine fault");
                foreach (var player in new[] { new PlayerId(0), new PlayerId(1), new PlayerId(99), PlayerId.None })
                {
                    BusinessAccountSnapshot leaked = retained;
                    c.Throws<InvalidOperationException>(() => view.ReadAccount(player, out leaked), "fault precedes private/missing account fallback");
                    c.That(leaked.Policy == null && leaked.Balance == Fix.Zero, "failed account read clears prior output before refusing");
                    rows.Add(default);
                    c.Throws<InvalidOperationException>(() => view.CopyBudgets(player, rows), "fault precedes private/missing budget fallback");
                    c.Equal(0, rows.Count, "failed retained budget view publishes no stale/prefix row");
                    var spending = new List<BusinessExpenseSnapshot> { default };
                    c.Throws<InvalidOperationException>(() => view.CopySpending(player, spending), "fault precedes private/missing spending fallback");
                    c.Equal(0, spending.Count, "failed spending read clears retained rows before fault");
                }
            }
            c.That(retained.Balance == Fix.FromInt(20) && retained.Policy == "proof.cancellation.accounts", "previous detached facts never change with bad charge/refund");
            g.Update(); c.That(ReferenceEquals(first, g.Fault) && g.Clock.StepIndex == at, "retained queries preserve first fault and cannot drive time");
            c.Throws<InvalidOperationException>(() => g.Save(), "faulted account view cannot become a healthy save");
            c.Equal(untouched, idle.Hash(), "retained faulty view cannot affect interleaved healthy session");
            c.Equal(Code.None, idle.Seat(0).View.Feature<BusinessView>().ReadAccount(new PlayerId(0), out var fresh), "other view stays usable");
            c.Equal(Fix.FromInt(20), fresh.Balance, "other session account untouched");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual view fault boundary protocol");
            foreach (string mode in new[] { "append-charge", "cancel-charge", "cancel-credit", "append-admission-charge", "cancel-admission-charge" })
                foreach (bool half in new[] { false, true }) Session(c, mode, half);
            CancellationCostDurabilityTests.Native(c, true, true);
        }
    }
}
