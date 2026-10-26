using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>One authored readiness declaration; the actual consumer owns demand and physical consumption.</summary>
    public sealed class UtilityWorkRequirementDeclaration
    {
        public static readonly Code MissingConsumer = new Code("utility.missing-consumer");
        public Code Consumer { get; }
        private UtilityWorkRequirementDeclaration(JsonReader reader, string property)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (string.IsNullOrWhiteSpace(property)) throw new ArgumentException("consumer property is required", nameof(property));
            try { Consumer = new Code(reader.String(property)); ContractIdentity.ValidateId(Consumer.Id, "utility requirement consumer"); }
            catch (ArgumentException ex) { throw reader.Error(ex.Message); }
        }
        public static UtilityWorkRequirementDeclaration Read(JsonReader reader, string property)
            => new UtilityWorkRequirementDeclaration(reader, property);
        public WorkRequirementResult Evaluate(WorkRequirementContext context, Entity subject)
        {
            if (subject == null) return WorkRequirementResult.Pending(MissingConsumer);
            if (subject.Owner != context.Owner) return WorkRequirementResult.Blocked(Codes.NotOwned);
            IUtilityConsumer provider = subject.Get<IUtilityConsumer>(Consumer);
            if (provider == null) return WorkRequirementResult.Pending(MissingConsumer);
            UtilityDemandSnapshot demand = UtilityContracts.Read(context.World, provider);
            return demand.Satisfied ? WorkRequirementResult.Satisfied() : WorkRequirementResult.Pending(demand.Blocker);
        }
    }
    public sealed class TargetUtilityRequirementDef : WorkRequirementDef
    {
        public override System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        private UtilityWorkRequirementDeclaration _declaration;
        public Code Consumer => _declaration.Consumer;
        protected override void ReadCore(JsonReader reader) => _declaration = UtilityWorkRequirementDeclaration.Read(reader, "consumer");
        public override WorkRequirementResult Evaluate(WorkRequirementContext context) => _declaration.Evaluate(context, context.Target);
    }
    public sealed class WorkerUtilityRequirementDef : WorkRequirementDef, IWorkerWorkRequirement
    {
        public override System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        private UtilityWorkRequirementDeclaration _declaration;
        public Code Consumer => _declaration.Consumer;
        protected override void ReadCore(JsonReader reader) => _declaration = UtilityWorkRequirementDeclaration.Read(reader, "consumer");
        public override WorkRequirementResult Evaluate(WorkRequirementContext context) => _declaration.Evaluate(context, context.Worker);
    }
}
