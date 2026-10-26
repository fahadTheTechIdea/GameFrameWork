using System;
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
    public static class TransformationQueueBoundaryTests
    {
        public const string Case = "Transformation queue boundary: external append parity, hidden mutation and fault cutoff";
        private static readonly Code Pulse = new Code("pulse");
        public static void Register() => TestRunner.Add(Case, Run);
        private static SimGame New(GameFactory factory) => TransformerTests.New(factory);
        private static Entity Emitter(SimGame game) => TransformerTests.Find(game, "emitter");
        private static TransformationSnapshot Read(SimGame game, Entity entity = null, Code capability = default)
        {
            entity = entity ?? Emitter(game); if (capability.IsNone) capability = Pulse;
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(entity.Id, capability, out var value))
                throw new InvalidOperationException("missing actual provider queue");
            return value;
        }
        private static void Enqueue(SimGame game, Entity entity = null, int count = 1)
        {
            game.Seat(0).Submit(new EnqueueTransformationCommand { Transformer = (entity ?? Emitter(game)).Id,
                Capability = Pulse, Transformation = "pulse", Count = count });
            game.Update();
        }
        private static void Cutoff(Check c, SimGame game, string name)
        {
            c.That(game.Fault != null, name + " latches actual fault"); long time = game.Tick;
            game.Update(); c.Equal(time, game.Tick, name + " cannot continue time");
            c.Throws<InvalidOperationException>(() => game.Save(), name + " cannot save partial provider state");
            c.Throws<InvalidOperationException>(() => game.Hash(), name + " cannot checkpoint hash");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "strict provider boundary identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "same actual queue/control/provider bytes");
            var factory = TransformerTests.Factory(); var game = New(factory);
            c.That(ReferenceEquals(game.World.Services.Get<ITransformationQueue>(), game.World.Services.Get<TransformerManager>()), "same coordinator alias for independent provider");
            c.That(Emitter(game).Get<Transformer>() == null, "valid external provider not native subclass");
            ulong hash = game.Hash(); var view = game.Seat(0).View.Feature<TransformationManagementView>();
            c.Equal(Code.None, view.PreviewEnqueue(Emitter(game).Id, Pulse, "pulse", 2, false, EntityId.None), "valid independent admission");
            c.Equal(hash, game.Hash(), "codec guards preserve healthy pure preview");
            Enqueue(game, count:2); c.That(game.Fault == null, "valid independent append accepted");
            c.Equal(2, Read(game).QueueCount, "actual external queue receives exact requested entries");
            c.That(Read(game).Progress.WholeTicks > 0 && Read(game).Progress.WholeTicks < Read(game).Required, "external manufacturing remains genuinely timed");
            var frozen = Read(game); var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "valid independent queue/progress saves");
            for (int i = 0; i < 8 && Read(game).QueueCount > 0; i++) { game.Update(); twin.Update(); }
            c.Equal(0, Read(game).QueueCount, "bounded real external manufacturing finishes");
            c.Equal(2, game.World.Entities.All.Count(e => e.Def.Id == "token"), "exact real engine outputs after earned time");
            c.Equal(game.Hash(), twin.Hash(), "interleaved saved external execution identical");
            c.Equal(2, frozen.QueueCount, "detached queue never becomes live state");

            game = New(factory); var native = TransformerTests.Find(game, "assembler");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = native.Id, Capability = new Code("transformation"), Transformation = "build", Count = 2 }).Accepted,
                "native inventory owner permits legitimate queue Touch");
            c.That(game.Fault == null && Read(game, native, new Code("transformation")).QueueCount == 2, "sibling inventory unchanged across native append");

            foreach (int mode in new[] { 8, 9 })
            {
                game = New(factory); Emitter(game).Get<PulseTransformer>().FaultMode = mode;
                if (mode == 8) c.Throws<InvalidOperationException>(() => game.Seat(0).View.Feature<TransformationManagementView>().PreviewEnqueue(Emitter(game).Id, Pulse, "pulse", 1, false, default), "hidden pure admission mutation refuses");
                else c.Throws<InvalidOperationException>(() => Read(game), "hidden persisted snapshot mutation refuses");
                Cutoff(c, game, "hidden mode" + mode);
            }
            foreach (int mode in new[] { 10, 11, 13, 14 })
            {
                game = New(factory); Emitter(game).Get<PulseTransformer>().FaultMode = mode; Enqueue(game);
                Cutoff(c, game, "bad append mode" + mode);
                c.Equal(0, game.World.Entities.All.Count(e => e.Def.Id == "token"), "bad append cannot create productive output");
            }
            game = New(factory); Emitter(game).Get<PulseTransformer>().FaultMode = 12; Enqueue(game, count:2);
            Cutoff(c, game, "rewritten queue prefix/request");
            var siblings = TransformerTests.Factory(root => root.Get("entities")[0].Get("components").Set("pulse_transformer",
                Json.Parse(FrameworkProofContent.Read("transformer-proof.json")).Get("entities")[0].Get("components").Get("pulse_transformer")));
            game = New(siblings); native = TransformerTests.Find(game, "assembler"); native.Get<PulseTransformer>().FaultMode = 15;
            Enqueue(game, native); Cutoff(c, game, "sibling component mutation");
            c.That(New(factory).Fault == null, "new independent session not poisoned by failed mod sessions");
        }
    }
}
