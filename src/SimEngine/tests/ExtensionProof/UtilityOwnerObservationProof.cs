using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Explicit file-selected negative instrumentation; no utility, inventory or clock owner.
    public sealed class UtilityOwnerFailureDef : Def
    {
        public string Mode { get; private set; }
        public string Phase { get; private set; }
        public GameDuration ReadAfter { get; private set; }
        protected override void ReadCore(JsonReader r)
        {
            Mode = r.String("mode"); Phase = r.String("phase"); ReadAfter = r.Child("read_after").RationalDuration();
            if (ReadAfter.IsZero || Mode != "getter" && Mode != "demand" && Mode != "port" && Mode != "clock-throw"
                && Mode != "spatial-layer" && Mode != "spatial-layer-throw"
                || Phase != "view" && Phase != "update") throw r.Error("explicit utility owner fault mode/phase/time required");
        }
    }
    public sealed class UtilityOwnerWitnessDef : ComponentDef<UtilityOwnerWitness>
    { protected override void ReadCore(JsonReader r) { } }
    public sealed class UtilityOwnerWitness : Component<UtilityOwnerWitnessDef>
    {
        private long _counter;
        internal void Corrupt() { _counter = checked(_counter + 1); } // Deliberately no Entity.Touch.
        public override void Sync(IStateSync sync) => sync.Long(ref _counter);
    }
    public sealed class UtilityOwnerProbeDef : ComponentDef<UtilityOwnerProbe>, INamedCapabilityDefinition
    {
        public Code CapabilityId { get; private set; }
        public Code Source { get; private set; }
        private string _failure;
        public UtilityOwnerFailureDef Failure { get; private set; }
        protected override void ReadCore(JsonReader r)
        { CapabilityId = new Code(r.String("id")); Source = new Code(r.String("source")); _failure = r.String("failure");
            if (CapabilityId.IsNone || Source.IsNone) throw r.Error("actual named utility identities required"); }
        protected override void ResolveCore(ContentResolver r) => Failure = r.Def<UtilityOwnerFailureDef>(_failure);
        protected override void ValidateCore(EntityDef owner)
        {
            var source = owner.Component<INamedCapabilityDefinition>(Source);
            if (Source == CapabilityId || source == null || !typeof(IUtilityConsumer).IsAssignableFrom(source.ComponentType)
                || owner.Component<UtilityOwnerWitnessDef>() == null) throw new ContentException("actual utility and sibling witness required");
        }
    }
    public sealed class UtilityOwnerProbe : Component<UtilityOwnerProbeDef>, IUtilityConsumer
    {
        private World _session; // Per-session negative instrumentation binding, never authored/global state.
        internal void Bind(World world) { _session = world; }
        private bool Due => _session != null && _session.Clock.StepIndex >= _session.Time.Ticks(Def.Failure.ReadAfter);
        public Code CapabilityId => Def.CapabilityId;
        Entity IEntityCapability.Entity { get { Fail("getter"); return Entity; } }
        private void Fail(string mode)
        {
            if (!Due || Def.Failure.Mode != mode) return;
            if (mode == "spatial-layer" || mode == "spatial-layer-throw")
            {
                var result = ((PointSpace)_session.Space).WriteLayer(_session, Entity.Owner,
                    "corridor.depth", Fix.FromInt(99), Fix.Parse("1.5"));
                if (!result.IsNone) throw new InvalidOperationException("proof.utility-spatial-write-refused: " + result);
                if (mode == "spatial-layer-throw") throw new InvalidOperationException("proof.utility-spatial-first-error");
                return;
            }
            if (mode == "clock-throw")
            { ((GameClock)_session.Clock).Speed = 2; throw new InvalidOperationException("proof.utility-owner-first-error"); }
            Entity.Get<UtilityOwnerWitness>().Corrupt();
        }
        public IResourcePort ResolveUtilityPort(World world, ResourceId resource)
        { var port = Entity.Get<IUtilityConsumer>(Def.Source).ResolveUtilityPort(world, resource); Fail("port"); return port; }
        public UtilityDemandSnapshot ReadUtilityDemand(World world)
        {
            var facts = UtilityContracts.Read(world, Entity.Get<IUtilityConsumer>(Def.Source));
            Fail("demand"); Fail("clock-throw"); Fail("spatial-layer"); Fail("spatial-layer-throw");
            return new UtilityDemandSnapshot(CapabilityId, facts.ElapsedTicks, facts.Ports, facts.Drawing,
                facts.Consumed, facts.NextInputs, facts.Available, facts.NextBlocker);
        }
    }
    public sealed class UtilityOwnerObservationModule : GameModule
    {
        public const string ModuleId = "proof.utility-owner-observation";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(TransformationModule.ModuleId, Version) };
        public bool Returned { get; private set; }
        public override void DeclareContent(ContentTypes types)
        { types.Section<UtilityOwnerFailureDef>("utility_owner_failures"); types.Component<UtilityOwnerProbeDef>("utility_owner_probe", 1);
            types.Component<UtilityOwnerWitnessDef>("utility_owner_witness", 1); }
        public override void Compose(WorldComposer composer) => composer.AddSystem(new NegativeRead(this), 8000);
        private sealed class NegativeRead : GameSystem
        {
            private readonly UtilityOwnerObservationModule _module;
            internal NegativeRead(UtilityOwnerObservationModule module) { _module = module; }
            public override string Id => ModuleId;
            public override int StateSchemaVersion => 1;
            public override void Update(World world, TickInfo time)
            {
                foreach (var entity in world.Entities.All) foreach (var component in entity.Components)
                    if (component is UtilityOwnerProbe source)
                    {
                        source.Bind(world);
                        if (source.Def.Failure.Phase == "update" && world.Clock.StepIndex >= world.Time.Ticks(source.Def.Failure.ReadAfter))
                        { _ = UtilityContracts.Read(world, source); _module.Returned = true; }
                    }
            }
        }
    }
}
