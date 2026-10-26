using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Deliberately dishonest providers for boundary diagnostics, not gameplay fallback behavior.
    public enum ReplayProbeFailure
    {
        None, Compose, Restore, Loaded, InitialHash, CheckpointHash, FinalHash, Update,
        Constructor, Decode, CopyWrite, CopyRead, CorruptPayload, InvalidPayload
    }
    public sealed class ReplayProbeDef : Def
    {
        protected override void ReadCore(JsonReader reader) { }
    }
    public class ReplayProbeCommand : Command
    {
        public int Value;
        public override void Sync(IStateSync state) => state.Int(ref Value);
    }
    public sealed class ReplayConstructorFailureCommand : ReplayProbeCommand
    {
        public ReplayConstructorFailureCommand() => throw new InvalidOperationException("replay constructor probe");
    }
    public sealed class ReplayDecodeFailureCommand : ReplayProbeCommand
    {
        public override void Sync(IStateSync state)
        { base.Sync(state); if (state.Reading) throw new InvalidOperationException("replay decode probe"); }
    }
    public sealed class ReplayCopyWriteFailureCommand : ReplayProbeCommand
    {
        private bool _decoded;
        public override void Sync(IStateSync state)
        {
            if (_decoded && state.Mode == SyncMode.Write) throw new InvalidOperationException("replay copy write probe");
            base.Sync(state); if (state.Reading) _decoded = true;
        }
    }
    public sealed class ReplayCopyReadFailureCommand : ReplayProbeCommand
    {
        public override void Sync(IStateSync state)
        {
            base.Sync(state);
            if (!state.Reading) return;
            if (Value == 1) throw new InvalidOperationException("replay copy read probe");
            Value = 1; // Deliberately rewrite decode so the original inbox clone exposes the violation.
        }
    }
    public sealed class ReplayCorruptPayloadCommand : ReplayProbeCommand
    {
        public override void Sync(IStateSync state)
        { base.Sync(state); if (state.Reading) throw new CorruptStateException("replay corrupt payload probe"); }
    }
    public sealed class ReplayInvalidPayloadCommand : ReplayProbeCommand
    {
        public override void Sync(IStateSync state)
        { base.Sync(state); if (state.Reading) throw new ArgumentException("replay invalid payload probe"); }
    }
    public sealed class ReplayProbeHandler<T> : CommandHandler<T> where T : ReplayProbeCommand
    {
        protected override Code Check(World world, T command) => Code.None;
        protected override void Apply(World world, T command, CommandContext result) { }
    }
    public sealed class ReplayFailureProofModule : GameModule
    {
        private readonly ReplayProbeFailure _failure;
        public ReplayFailureProofModule(ReplayProbeFailure failure = ReplayProbeFailure.None) { _failure = failure; }
        public override string Id => "proof.replay-failure";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(TransformationModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Section<ReplayProbeDef>("replay_probes");
        public override void Compose(WorldComposer composer)
        {
            if (_failure == ReplayProbeFailure.Compose) throw new InvalidOperationException("replay composition probe");
            composer.AddSystem(new FailureSystem(_failure), 500);
            const string command = "proof.replay-input";
            switch (_failure)
            {
                case ReplayProbeFailure.Constructor: composer.Commands.Register(command, new ReplayProbeHandler<ReplayConstructorFailureCommand>()); break;
                case ReplayProbeFailure.Decode: composer.Commands.Register(command, new ReplayProbeHandler<ReplayDecodeFailureCommand>()); break;
                case ReplayProbeFailure.CopyWrite: composer.Commands.Register(command, new ReplayProbeHandler<ReplayCopyWriteFailureCommand>()); break;
                case ReplayProbeFailure.CopyRead: composer.Commands.Register(command, new ReplayProbeHandler<ReplayCopyReadFailureCommand>()); break;
                case ReplayProbeFailure.CorruptPayload: composer.Commands.Register(command, new ReplayProbeHandler<ReplayCorruptPayloadCommand>()); break;
                case ReplayProbeFailure.InvalidPayload: composer.Commands.Register(command, new ReplayProbeHandler<ReplayInvalidPayloadCommand>()); break;
                default: composer.Commands.Register(command, new ReplayProbeHandler<ReplayProbeCommand>()); break;
            }
        }
        private sealed class FailureSystem : GameSystem
        {
            private readonly ReplayProbeFailure _failure;
            private int _steps, _finalHashCalls;
            public FailureSystem(ReplayProbeFailure failure) { _failure = failure; }
            public override string Id => "proof.replay-failure";
            public override int StateSchemaVersion => 1;
            public override void Update(World world, TickInfo tick)
            {
                if (_failure == ReplayProbeFailure.Update) throw new InvalidOperationException("replay update probe");
                _steps++;
            }
            public override void OnLoaded(World world)
            { if (_failure == ReplayProbeFailure.Loaded) throw new InvalidOperationException("replay loaded probe"); }
            public override void Sync(IStateSync state)
            {
                if (state.Reading && _failure == ReplayProbeFailure.Restore) throw new InvalidOperationException("replay restore probe");
                if (state.Mode == SyncMode.Hash &&
                    (_failure == ReplayProbeFailure.InitialHash && _steps == 0 ||
                     _failure == ReplayProbeFailure.CheckpointHash && _steps == 1 ||
                     _failure == ReplayProbeFailure.FinalHash && _steps == 2 && _finalHashCalls++ > 0))
                    throw new InvalidOperationException("replay hash probe");
                state.Int(ref _steps);
            }
        }
    }
}
