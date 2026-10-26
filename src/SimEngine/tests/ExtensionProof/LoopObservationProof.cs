using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Non-authoritative lifecycle instrumentation; no gameplay state or positive execution behavior.
    public sealed class LoopObservationDef : Def
    {
        public string SnapshotFault { get; private set; }
        public long? SnapshotAt { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            SnapshotFault = reader.String("snapshot_fault");
            if (SnapshotFault != "none" && SnapshotFault != "throw" && SnapshotFault != "mutate") throw reader.Error("explicit snapshot probe mode required");
            JsonValue at = reader.Value("snapshot_at") ?? throw reader.Error("explicit snapshot_at or null required");
            if (at.Kind != JsonKind.Null) { SnapshotAt = reader.Long("snapshot_at"); if (SnapshotAt < 0) throw reader.Error("negative probe coordinate"); }
            if ((SnapshotFault == "none") != !SnapshotAt.HasValue) throw reader.Error("snapshot fault and coordinate must agree");
        }
    }
    public sealed class LoopStateProbeDef : ComponentDef<LoopStateProbe>
    {
        public string Mode { get; private set; }
        public long? At { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Mode = reader.String("mode");
            if (Mode != "healthy" && Mode != "throw" && Mode != "recursive" && Mode != "swallow-recursive" && Mode != "step" && Mode != "swallow-step") throw reader.Error("explicit source probe mode required");
            JsonValue at = reader.Value("at") ?? throw reader.Error("explicit source at or null required");
            if (at.Kind != JsonKind.Null) { At = reader.Long("at"); if (At < 0) throw reader.Error("negative source probe coordinate"); }
            if ((Mode == "healthy") != !At.HasValue) throw reader.Error("source fault and coordinate must agree");
        }
    }
    public readonly struct LoopRead
    {
        public readonly EntityId Entity;
        public readonly long At;
        public LoopRead(EntityId entity, long at) { Entity = entity; At = at; }
    }
    public sealed class LoopObservation
    {
        private readonly List<LoopRead> _reads = new List<LoopRead>();
        public IReadOnlyList<LoopRead> Reads => _reads.AsReadOnly();
        internal void Record(EntityId entity, long at) => _reads.Add(new LoopRead(entity, at));
        public readonly ViewObservationVisibility Visibility = new ViewObservationVisibility();
    }
    public sealed class LoopStateProbe : Component<LoopStateProbeDef>, IEntityStateSource
    {
        public IEntityStateDefinition GetState(EntityUpdateContext context)
        {
            context.World.Services.Get<LoopObservation>().Record(Entity.Id, context.Clock.StepIndex);
            if (!Def.At.HasValue || context.Tick != Def.At.Value) return null;
            if (Def.Mode == "throw") throw new InvalidOperationException("loop.probe.source");
            if (Def.Mode == "recursive" || Def.Mode == "swallow-recursive")
            {
                try { Entity.Update(context); } catch (InvalidOperationException) { if (Def.Mode != "swallow-recursive") throw; }
            }
            if (Def.Mode == "step" || Def.Mode == "swallow-step")
            {
                try { context.World.Step(); } catch (InvalidOperationException) { if (Def.Mode != "swallow-step") throw; }
            }
            return null;
        }
    }
    public sealed class LoopObservationModule : GameModule
    {
        public override string Id => "proof.loop-observation";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(WorkModule.ModuleId, Version) });
        public readonly InitializeProbe Initialize = new InitializeProbe("loop.probe.initialize");
        private readonly List<LoopObservation> _sessions = new List<LoopObservation>();
        public IReadOnlyList<LoopObservation> Sessions => _sessions.AsReadOnly();
        public override void DeclareContent(ContentTypes types)
        { types.Section<LoopObservationDef>("loop_observations"); types.Component<LoopStateProbeDef>("loop_probe", 1); }
        public override void Compose(WorldComposer composer)
        {
            var definition = composer.Content.Get<LoopObservationDef>("loop.session");
            var observation = new LoopObservation(); _sessions.Add(observation);
            observation.Visibility.OnEntity = (world, viewer, entity) => {
                if (viewer != new PlayerId(1) || !definition.SnapshotAt.HasValue || world.Tick < definition.SnapshotAt.Value) return;
                if (definition.SnapshotFault == "throw") throw new InvalidOperationException("loop.probe.snapshot");
                if (definition.SnapshotFault == "mutate") observation.Visibility.Value++;
            };
            composer.Services.Bind(observation); composer.Services.Bind<IVisibility>(observation.Visibility);
            composer.AddSystem(Initialize, 306);
        }
    }
}
