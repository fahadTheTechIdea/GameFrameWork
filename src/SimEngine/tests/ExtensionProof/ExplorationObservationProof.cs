using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Explicit negative instrumentation only. No worker, discovery, progress or stock owner.
    public sealed class ExplorationObservationDef : ComponentDef<ExplorationObservationSource>, IExplorationDefinition
    {
        public Code CapabilityId { get; private set; }
        public Code Contact { get; private set; }
        public string Mode { get; private set; }
        public GameDuration ReadAfter { get; private set; }
        protected override void ReadCore(JsonReader r)
        {
            CapabilityId = new Code(r.String("id")); Contact = new Code(r.String("contact")); Mode = r.String("mode");
            ReadAfter = r.Child("read_after").RationalDuration();
            if (CapabilityId.IsNone || Contact.IsNone) throw r.Error("explicit negative source/contact identity required");
            if (ReadAfter.IsZero || Mode != "throw" && Mode != "clock" && Mode != "clock-throw" && Mode != "malformed"
                && Mode != "composition-clock-throw" && Mode != "load-clock-throw")
                throw r.Error("explicit negative exploration mode and positive read_after required");
        }
        protected override void ValidateCore(EntityDef owner)
        {
            if (owner.Component<IAuthoredExplorationDefinition>(Contact) == null) throw new ContentException("actual contact required");
        }
        public void ValidateBinding(World world)
        {
            AuthoredExplorationContracts.ValidateBinding(world, Owner.Component<IAuthoredExplorationDefinition>(Contact));
            _ = world.Time.Ticks(ReadAfter);
            if (Mode == "composition-clock-throw") Fail(world, true, true);
        }
        internal static void Fail(World world, bool clock, bool throws)
        {
            if (clock) ((GameClock)world.Clock).Speed = 2;
            if (throws) throw new InvalidOperationException("proof.exploration-first-error");
        }
    }
    public sealed class ExplorationObservationSource : Component, IExplorationSource
    {
        public new ExplorationObservationDef Def => (ExplorationObservationDef)base.Def;
        public Code CapabilityId => Def.CapabilityId;
        private IExplorationSource Contact => Entity.Get<IExplorationSource>(Def.Contact);
        public ExplorationSnapshot ReadExploration(World world)
        {
            var actual = Contact.ReadExploration(world);
            if (world.Clock.StepIndex >= world.Time.Ticks(Def.ReadAfter))
            {
                if (Def.Mode == "malformed") return new ExplorationSnapshot(Entity.Id, new Code("wrong.source"), actual.Entries);
                if (Def.Mode == "throw" || Def.Mode == "clock" || Def.Mode == "clock-throw")
                    ExplorationObservationDef.Fail(world, Def.Mode != "throw", Def.Mode != "clock");
            }
            return new ExplorationSnapshot(Entity.Id, CapabilityId, actual.Entries);
        }
        public void ValidateExploration(World world)
        {
            Contact.ValidateExploration(world);
            if (Def.Mode == "load-clock-throw") ExplorationObservationDef.Fail(world, true, true);
        }
        public void AdvanceExploration(ExplorationUpdateContext context)
        { if (!context.IsCurrent(this)) throw new InvalidOperationException("actual negative update permission required"); }
    }
    public sealed class ExplorationObservationModule : GameModule
    {
        public override string Id => "proof.exploration-observation";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(ExplorationModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Component<ExplorationObservationDef>("exploration_observation_probe", 1);
    }
}
