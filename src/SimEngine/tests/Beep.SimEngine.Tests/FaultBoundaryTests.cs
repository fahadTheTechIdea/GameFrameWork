using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    // Fault injection only: normal commands, actual entity initialization, existing lifecycle owner.
    internal static class FaultBoundaryTests
    {
        internal static void Run(Check c)
        {
            Type type = typeof(WorldFault);
            c.Equal(0, type.GetFields().Length, "public fault diagnostics contain no mutable fields");
            c.Equal(0, type.GetConstructors().Length, "hosts cannot fabricate an authoritative fault record");
            foreach (string name in new[] { "Tick", "Phase", "Message", "Detail" })
                c.That(type.GetProperty(name).GetSetMethod(true) == null, "fault fact is immutable: " + name);

            JsonValue root = Json.Parse(StateGame.Defs);
            root.Get("entities").Add(Json.Parse("{\"id\":\"broken\",\"kind\":\"structure\",\"components\":{\"fault_initialization\":{}}}"));
            GameFactory factory = StateGame.Factory(root.ToString());
            SimGame isolated = factory.NewGame(new NewGameRequest { Scenario = "state-test", Seed = 71 });
            ulong isolatedHash = isolated.Hash();
            for (int mode = 0; mode < 3; mode++)
            {
                SimGame game = factory.NewGame(new NewGameRequest { Scenario = "state-test", Seed = 72 });
                game.Update();
                GameSnapshot healthy = game.Snapshot;
                long time = game.Clock.StepIndex;
                game.Clock.TicksPerFrame = 8;
                game.Seat(0).Submit(new FaultProbeCommand { Mode = 3 });
                game.Seat(0).Submit(new FaultProbeCommand { Mode = mode });
                game.Seat(0).Submit(new FaultProbeCommand { Mode = 3 });
                WorldFault observed = null;
                game.Updated += _ => observed = game.Fault;
                c.Equal(0, game.Update(), "failed command earns no completed game time: " + mode);
                WorldFault fault = game.Fault;
                c.That(fault != null && ReferenceEquals(fault, observed), "Update publishes the same immutable fault: " + mode);
                c.Equal("entity.initialize", fault.Phase, "outer command catch preserves original failing owner: " + mode);
                c.Equal("InvalidOperationException: initialization probe", fault.Message, "wrapper/swallowed exceptions do not replace original cause: " + mode);
                c.That(fault.Detail.Contains(nameof(FaultInitialization.Initialize)), "diagnostic retains original initialization stack: " + mode);
                c.Equal(time, fault.Tick, "failure coordinate derives from the only game clock: " + mode);
                c.Equal(time, game.Clock.StepIndex, "partial failure never advances the game clock: " + mode);
                c.That(game.Time.ElapsedGameTime.IsZero && game.Clock.PendingTicks == 0, "fault cancels pacing backlog, not game time: " + mode);
                c.Equal(1, game.World.LastTickResults.Count, "no success result for failed or later command: " + mode);
                c.That(game.World.LastTickResults[0].Accepted, "preceding actually completed command retains its result: " + mode);
                c.Equal(1, game.World.Entities.Count, "fault stops before pending entity flush: " + mode);
                c.That(game.World.Entities.Get(game.World.LastTickResults[0].Entity) != null, "earlier successful spawn keeps its actual pending identity: " + mode);
                c.Equal(1, game.World.Entities.All[0].Get<SignalA>().Reads, "entity state update cannot continue after partial command failure: " + mode);
                c.Equal(GameState.Faulted, game.Snapshot.State, "fault state is published before Updated: " + mode);
                c.Equal(GameState.Running, healthy.State, "retained healthy snapshot is not overwritten: " + mode);
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted partial operation cannot be saved: " + mode);
                game.Clock.Pause(); game.Clock.Resume();
                c.Equal(0, game.Update(), "resume cannot restart a faulted update: " + mode);
                c.Equal(0, game.RunTicks(8), "diagnostic batches cannot bypass terminal failure: " + mode);
                game.World.FaultReplacement(new InvalidOperationException("later failure"));
                c.That(ReferenceEquals(fault, game.Fault) && fault.Message.Contains("initialization probe"), "later fault paths retain the first record: " + mode);
                c.Equal(time, game.Clock.StepIndex, "repeated observations never change game time: " + mode);
                c.Equal(isolatedHash, isolated.Hash(), "failure does not change another session: " + mode);
                c.That(isolated.Fault == null && isolated.Clock.StepIndex == 0, "unrelated session remains healthy at its own one clock: " + mode);
            }
        }
    }

    public sealed class FaultInitializationDef : ComponentDef<FaultInitialization>
    {
        protected override void ReadCore(JsonReader reader) { }
    }

    public sealed class FaultInitialization : Component<FaultInitializationDef>, IEntityInitialization
    {
        public void Initialize(World world) => throw new InvalidOperationException("initialization probe");
    }

    public sealed class FaultProbeCommand : Command
    {
        public int Mode;
        public override void Sync(IStateSync state) => state.Int(ref Mode);
    }

    public sealed class FaultProbeHandler : CommandHandler<FaultProbeCommand>
    {
        protected override Code Check(World world, FaultProbeCommand command) => Code.None;
        protected override void Apply(World world, FaultProbeCommand command, CommandContext context)
        {
            if (command.Mode == 3)
            {
                context.Entity = world.Spawn(world.Content.Get<EntityDef>("machine"), command.Player).Id;
                return;
            }
            try { world.Spawn(world.Content.Get<EntityDef>("broken"), command.Player); }
            catch (InvalidOperationException error)
            {
                if (command.Mode == 0) throw;
                if (command.Mode == 1) throw new InvalidOperationException("outer wrapper", error);
                // Probe a dishonest provider swallowing failure; the engine's latch remains authoritative.
            }
        }
    }
}
