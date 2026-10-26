using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional authored requirement validation after actual selected services/providers compose.</summary>
    public interface IWorkRequirementBindingValidator
    {
        void ValidateBinding(World world);
    }
    public enum ActivityCompletionScope : byte { Target = 1, Company = 2 }
    /// <summary>A leaf requiring earlier full completion. Conditional alternatives use all/any groups.</summary>
    public interface IPriorActivityWorkRequirement : IWorkRequirement
    {
        string Plan { get; }
    }
    public sealed class PriorActivityRequirementDef : WorkRequirementDef, IPriorActivityWorkRequirement
    {
        public override System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        public string Plan { get; private set; }
        public ActivityCompletionScope Scope { get; private set; }
        public Code Blocker { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Plan = reader.String("plan"); string scope = reader.String("scope"), blocker = reader.String("blocker");
            if (string.IsNullOrWhiteSpace(Plan)) throw reader.Error("an explicit composed activity plan key is required");
            if (scope == "target") Scope = ActivityCompletionScope.Target;
            else if (scope == "company") Scope = ActivityCompletionScope.Company;
            else throw reader.Error("prior activity scope must explicitly be target or company");
            try { ContractIdentity.ValidateId(blocker, "prior activity blocker"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            Blocker = new Code(blocker);
        }
        public override WorkRequirementResult Evaluate(WorkRequirementContext context)
        {
            if (context.Target == null || !context.Target.Alive || !ReferenceEquals(context.World.Entities.Get(context.Target.Id), context.Target))
                return WorkRequirementResult.Invalid(Codes.NotFound);
            if (context.Target.Owner != context.Owner) return WorkRequirementResult.Blocked(Codes.NotOwned);
            bool found = context.World.Services.Get<IActivityCompletionHistory>().TryReadCompletion(context.Owner, Plan,
                Scope == ActivityCompletionScope.Target ? context.Target.Id : EntityId.None, out _);
            return found ? WorkRequirementResult.Satisfied() : WorkRequirementResult.Pending(Blocker);
        }
    }
}
