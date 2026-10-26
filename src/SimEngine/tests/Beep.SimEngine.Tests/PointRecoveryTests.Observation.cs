using System;
using System.Linq;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static partial class PointRecoveryTests
    {
        internal const string ObservationCase = "Recovery observation: actual geometry capture placement and container checks reject mutation and preserve first faults";
        internal const string OperationCase = "Recovery operation: whole stock packing and authored yield reject partial checkpoints and retain classified quantities";
        // Explicit hostile extension-protocol instrumentation, not public player acceptance.
        internal static void ObservationBoundary(Check c)
        {
            var factory = Factory();
            foreach (bool loaded in new[] { false, true })
            foreach (var spec in new[] {
                (Entry: "evacuate", Stage: "capture", Mode: "touch", Call: 1, Refuse: false),
                (Entry: "evacuate", Stage: "capture", Mode: "clock", Call: 2, Refuse: false),
                (Entry: "view", Stage: "placement", Mode: "touch", Call: 1, Refuse: false),
                (Entry: "check-yield", Stage: "placement", Mode: "clock", Call: 1, Refuse: false),
                (Entry: "check-yield", Stage: "placement", Mode: "clock", Call: 1, Refuse: true),
                (Entry: "yield", Stage: "placement", Mode: "throw", Call: 2, Refuse: false),
                (Entry: "evacuate", Stage: "capture", Mode: "throw", Call: 1, Refuse: false),
                (Entry: "yield", Stage: "capture", Mode: "touch-container", Call: 1, Refuse: false)
            }) {
                var game = New(factory); if (loaded) game = factory.Load(game.Save());
                var world = game.World; var source = Find(game, "source"); var space = (PointSpace)world.Space;
                var independent = factory.Load(game.Save()); ulong untouched = independent.Hash(); int calls = 0; bool fired = false;
                var original = new InvalidOperationException("proof.original-recovery-geometry-error"); Entity created = null;
                Action attack = () => {
                    calls++; if (calls != spec.Call) return; fired = true;
                    if (spec.Mode == "clock") game.Clock.Pause();
                    else if (spec.Mode == "touch") source.Touch();
                    else if (spec.Mode == "touch-container") created.Touch();
                    else throw original;
                };
                if (spec.Stage == "capture") space.PlacementCaptureFault = attack;
                space.GeometryObservationFault = (stage, entity) => {
                    if (stage == "configure") created = entity;
                    if (spec.Stage == "placement" && stage == "placement") attack();
                };
                var recovery = world.Services.Get<IResourceRecovery>(); Exception caught = null;
                var bill = new Bill(new[] { new ResourceAmount(Resource(game), Fix.FromInt(8)) });
                try {
                    if (spec.Entry == "view") game.Seat(0).View.Feature<Beep.SimEngine.Views.ConstructionView>().CheckRecovery(source.Id);
                    else if (spec.Entry == "evacuate") recovery.Evacuate(world, source);
                    else if (spec.Entry == "check-yield") recovery.CheckYield(world, new PlayerId(0), new PointPlacement(space.Id, Fix.FromInt(spec.Refuse ? 101 : 0)), bill, new ReservationOwner("proof.recovery-observation", 1), new Code("proof.yield"));
                    else recovery.ReleaseYield(world, new PlayerId(0), new PointPlacement(space.Id, Fix.Zero), bill, new ReservationOwner("proof.recovery-observation", 1), new Code("proof.yield"));
                } catch (Exception error) { caught = error; }
                string context = spec + "/loaded=" + loaded;
                c.That(fired, "actual selected recovery geometry callback runs: " + context);
                c.That(caught is InvalidOperationException && game.Fault != null, "impure/throwing recovery callback cannot return healthy admission or commit: " + context);
                if (spec.Mode == "throw") c.That(ReferenceEquals(original, caught), "original recovery callback exception survives: " + context);
                if (game.Fault != null) {
                    var fault = game.Fault; long at = game.Clock.StepIndex;
                    c.Throws<InvalidOperationException>(() => game.Save(), "faulted recovery cannot save: " + context);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "faulted recovery cannot hash healthy: " + context);
                    game.Update(); c.Equal(at, game.Clock.StepIndex, "faulted recovery advances no game time: " + context);
                    c.That(ReferenceEquals(fault, game.Fault), "first recovery fault remains authoritative: " + context);
                }
                c.Equal(untouched, independent.Hash(), "hostile recovery callback cannot contaminate isolated saved session: " + context);
            }
        }

        internal static void OperationBoundary(Check c)
        {
            var factory = Factory();
            foreach (bool yield in new[] { false, true }) {
                var game = New(factory); var world = game.World; var source = Find(game, "source"); var space = (PointSpace)world.Space;
                var recovery = world.Services.Get<IResourceRecovery>(); int probes = 0, saves = 0, hashes = 0; long cursor = Flows(game).Latest; Entity created = null;
                space.GeometryObservationFault = (stage, entity) => {
                    if (stage == "configure") created = entity;
                    if (stage != "placement" || created == null) return;
                    probes++;
                    try { game.Save(); saves++; } catch (InvalidOperationException) { }
                    try { game.Hash(); hashes++; } catch (InvalidOperationException) { }
                };
                var amount = Fix.FromInt(8); Code result = yield
                    ? recovery.ReleaseYield(world, new PlayerId(0), new PointPlacement(space.Id, Fix.Zero), new Bill(new[] { new ResourceAmount(Resource(game), amount) }), new ReservationOwner("proof.recovery-operation", 1), new Code("proof.structural-yield"))
                    : recovery.Evacuate(world, source);
                c.Equal(Code.None, result, "original whole recovery completes through its existing owner");
                c.That(probes > 0, "checkpoint probe runs between real packed containers, not only before work");
                c.Equal(0, saves, "partial packed recovery/yield cannot checkpoint a resumable prefix");
                c.Equal(0, hashes, "partial packed recovery/yield cannot advertise a completed hash");
                c.That(game.Fault == null, "denied nested checkpoint does not corrupt otherwise valid recovery");
                space.GeometryObservationFault = null; Tick(game);
                c.Equal(yield ? Fix.FromInt(28) : Fix.FromInt(20), Total(game), "authored yield creates eight while existing-stock packing conserves twenty");
                var facts = Flows(game, cursor).Entries.Select(row => row.Item).ToArray();
                c.That(facts.All(row => row.Kind == (yield ? ResourceFlowKind.Produced : ResourceFlowKind.StockTransfer)), "original recovery publishes only its declared semantic kind");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "fully completed original packing and yield checkpoint canonically");
            }
        }
    }
}
