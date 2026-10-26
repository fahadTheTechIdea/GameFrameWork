using System;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    // Framework-only observation proof, not another gameplay executor or persistence owner.
    internal static class NestedSpatialObservationTests
    {
        internal const string Case = "Spatial observation baselines: nested full verification cleanup and session isolation";
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 6371 });
        private static int Read(World world, Func<int> read) => EntityObservation.ReadProvider(world, world.Space, null, read, world.Space);

        internal static void Run(Check c)
        {
            var factory = PointGame.Factory();
            foreach (bool recursive in new[] { false, true })
            {
                var game = New(factory); var world = game.World; var space = (PointSpace)world.Space;
                ulong before = game.Hash(); int calls = 0;
                space.ObserveSync = () => calls++;
                int result = Read(world, () => recursive ? Read(world, () => Read(world, () => 17))
                    : Read(world, () => 8) + Read(world, () => 9));
                space.ObserveSync = null;
                c.That(!world.ActiveSpatialObservation.HasValue, "root frame removed after successful nested interval");
                c.Equal(17, result, "actual nested provider result");
                c.Equal(7, calls, "one root capture and full getter/callback verification for every nested interval");
                c.Equal(before, game.Hash(), "nested observation adds no saved state or clock");
                c.Equal(before, factory.Load(game.Save()).Hash(), "no observation frame survives native persistence");

                // An ordinary change BETWEEN completed pure reads is a new legal baseline.
                space.SetAccess(false); ulong changed = game.Hash(); calls = 0; space.ObserveSync = () => calls++;
                c.Equal(21, Read(world, () => 21), "later read captures actual changed state, never an old positive result");
                space.ObserveSync = null;
                c.Equal(3, calls, "completed root frame has been removed");
                c.Equal(changed, game.Hash(), "fresh root remains pure after a legal external mutation");
                c.That(game.Fault == null, "healthy nested and later roots remain healthy");

                calls = 0; space.ObserveSync = () => calls++;
                var manual = EntityObservation.CaptureSpace(world, space);
                c.That(!world.ActiveSpatialObservation.HasValue, "manual commit/refusal scope does not activate a pure-read frame");
                c.Equal(29, Read(world, () => 29), "root inside manual scope still captures its own baseline");
                manual.Verify(); space.ObserveSync = null;
                c.Equal(6, calls, "manual capture/getter/verify and independent full root remain separate");
            }

            var first = New(factory); var second = New(factory);
            var firstSpace = (PointSpace)first.World.Space; var secondSpace = (PointSpace)second.World.Space;
            int firstCalls = 0, secondCalls = 0;
            firstSpace.ObserveSync = () => firstCalls++; secondSpace.ObserveSync = () => secondCalls++;
            c.Equal(23, Read(first.World, () => Read(second.World, () => 23)), "interleaved worlds use their actual separate observations");
            firstSpace.ObserveSync = null; secondSpace.ObserveSync = null;
            c.Equal(3, firstCalls, "first world owns one complete root interval");
            c.Equal(3, secondCalls, "second world cannot inherit first world baseline");
            c.That(first.Fault == null && second.Fault == null, "interleaved healthy reads stay independent");

            foreach (string mode in new[] { "parent", "parent-revision", "payload", "revision", "getter", "clock", "random", "throw", "swallowed" })
            {
                var game = New(factory); var world = game.World; var space = (PointSpace)world.Space;
                var idle = New(factory); ulong isolated = idle.Hash(); var original = new ApplicationException("proof.nested-original");
                int nestedBodies = 0, afterChild = 0; Exception caught = null;
                try
                {
                    Read(world, () =>
                    {
                        if (mode == "parent") space.SetAccess(false);
                        if (mode == "parent-revision") Invalidate(space);
                        if (mode == "getter") space.ObserveRevision = () => space.SetAccess(false);
                        try
                        {
                            Read(world, () =>
                            {
                                nestedBodies++;
                                if (mode == "payload" || mode == "swallowed") space.SetAccess(false);
                                if (mode == "revision") Invalidate(space);
                                if (mode == "clock") game.Clock.Pause();
                                if (mode == "random") world.Random.NextULong();
                                if (mode == "throw") throw original;
                                return 1;
                            });
                            afterChild++;
                        }
                        catch when (mode == "swallowed") { space.SetAccess(true); }
                        return 2;
                    });
                }
                catch (Exception error) { caught = error; }
                space.ObserveRevision = null;
                c.That(caught != null && game.Fault != null, "original shared observer refuses nested violation: " + mode);
                c.Equal(mode == "parent" || mode == "parent-revision" || mode == "getter" ? 0 : 1, nestedBodies, "corrupt prefix/getter refuses before nested body: " + mode);
                c.Equal(0, afterChild, "no successful prefix after bad child: " + mode);
                if (mode == "throw") c.That(ReferenceEquals(original, caught), "original nested callback exception retained");
                var fault = game.Fault; game.Update();
                c.That(ReferenceEquals(fault, game.Fault), "first fault remains authoritative: " + mode);
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted nested interval cannot save: " + mode);
                c.Equal(isolated, idle.Hash(), "broken observation never binds idle session: " + mode);
                c.Equal(31, Read(idle.World, () => 31), "another session still captures a fresh healthy root: " + mode);
                c.That(!world.ActiveSpatialObservation.HasValue && !idle.World.ActiveSpatialObservation.HasValue,
                    "all frames removed even after construction/verification/throw failures: " + mode);
            }
        }
        // Framework-only injection into the actual original invalidation counter,
        // including revision-only corruption with an unchanged persisted payload.
        private static void Invalidate(PointSpace space)
        {
            var field = typeof(PointSpace).GetField("_observerVersion", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("missing actual point-space invalidation owner");
            field.SetValue(space, checked((int)field.GetValue(space) + 1));
        }
    }
}
