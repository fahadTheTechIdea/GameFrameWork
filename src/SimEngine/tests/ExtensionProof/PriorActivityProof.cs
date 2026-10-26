using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent engine-side content: no custom binding validator or completion authority.
    public sealed class PriorActivityProofDef : WorkRequirementDef, IPriorActivityWorkRequirement
    {
        public override System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        public string Plan { get; private set; }
        private bool _company;
        private Code _blocker;
        protected override void ReadCore(JsonReader reader)
        {
            Plan = reader.String("plan"); _company = reader.Bool("company_scope");
            string blocker = reader.String("blocker");
            if (string.IsNullOrWhiteSpace(blocker)) throw reader.Error("explicit blocker required");
            _blocker = new Code(blocker);
        }
        public override WorkRequirementResult Evaluate(WorkRequirementContext context)
        {
            if (context.Target == null || !context.Target.Alive || !ReferenceEquals(context.World.Entities.Get(context.Target.Id), context.Target))
                return WorkRequirementResult.Invalid(Codes.NotFound);
            if (context.Target.Owner != context.Owner) return WorkRequirementResult.Blocked(Codes.NotOwned);
            return context.World.Services.Get<IActivityCompletionHistory>().TryReadCompletion(context.Owner, Plan,
                _company ? EntityId.None : context.Target.Id, out _) ? WorkRequirementResult.Satisfied() : WorkRequirementResult.Pending(_blocker);
        }
    }
    public sealed class PriorActivityProofModule : GameModule
    {
        public int Initializations;
        public bool FaultNextUpdate;
        // Negative observation only; assigned by the exact faulting system, never positive gameplay.
        public IActivityCompletionHistory FaultedHistory { get; private set; }
        public override string Id => "proof.prior-activity";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Section<PriorActivityProofDef>("proof_prior_activity_requirements");
        public override void Compose(WorldComposer composer)
        {
            composer.AddSystem(new InitializationCounter(this), 300);
        }
        private sealed class InitializationCounter : GameSystem
        {
            private readonly PriorActivityProofModule _module;
            public InitializationCounter(PriorActivityProofModule module) { _module = module; }
            public override string Id => "proof.prior-initialization";
            public override int StateSchemaVersion => 1;
            public override void Initialize(World world) { _module.Initializations++; }
            public override void Update(World world, TickInfo tick)
            {
                if (_module.FaultNextUpdate)
                {
                    _module.FaultNextUpdate = false;
                    _module.FaultedHistory = world.Services.Get<IActivityCompletionHistory>();
                    throw new InvalidOperationException("proof.prior.first");
                }
            }
        }
    }
}
