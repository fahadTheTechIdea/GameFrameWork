using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    public static class ExtensionBoundaryTests
    {
        public static void Register() => TestRunner.Add("Modules: independent extension and scoped public views", Boundary);

        private static void Boundary(Check c)
        {
            Assembly framework = typeof(World).Assembly;
            Assembly extension = typeof(TestModule).Assembly;
            c.That(framework != extension, "module implementation is compiled in a separate assembly");
            bool privileged = false;
            foreach (InternalsVisibleToAttribute friend in framework.GetCustomAttributes<InternalsVisibleToAttribute>())
                privileged |= friend.AssemblyName == extension.GetName().Name || friend.AssemblyName == "OilCo.Game";
            c.That(!privileged, "neither proof module nor oil extension has friend access");
            bool oilReference = false;
            foreach (AssemblyName reference in framework.GetReferencedAssemblies())
                oilReference |= reference.Name.StartsWith("OilCo", StringComparison.Ordinal);
            foreach (AssemblyName reference in extension.GetReferencedAssemblies())
                oilReference |= reference.Name.StartsWith("OilCo", StringComparison.Ordinal);
            c.That(!oilReference, "framework and proof module have no oil assembly dependency");
            c.That(typeof(SimGame).GetProperty("World") == null, "hosting facade does not expose mutable world state");

            SimGame game = TestGame.New();
            game.RunTicks(20);
            var entities = new List<EntityView>();
            game.Observer.CopyEntities(new PlayerId(0), entities);
            EntityId own = entities[0].Id;
            c.That(game.Seat(0).View.Feature<CounterView>().TryRead(own, out Fix value) && value == Fix.FromInt(10),
                "public extension view reads its owner's settled state");
            c.That(!game.Seat(1).View.Feature<CounterView>().TryRead(own, out _), "extension view keeps the same private-state policy as a player");
            c.That(game.Observer.Feature<CounterView>().TryRead(own, out _), "explicit observer projection can inspect observer-authorized data");
            c.Throws<ArgumentOutOfRangeException>(() => game.World.ViewFor(new PlayerId(game.SeatCount)), "unknown viewer is refused");
            c.Throws<ArgumentOutOfRangeException>(() => game.World.ViewFor(new PlayerId(-2)), "invalid observer identity is refused");
        }
    }
}
