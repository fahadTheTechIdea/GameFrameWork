using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Pure admission through the actual work owner; never allocates a slot or claim.</summary>
    public interface IWorkDockAdmission
    {
        Code CheckDock(WorkRequirementContext context);
        /// <summary>Existing live job claims hold the endpoint still through service and departure.</summary>
        Code CheckEndpointMotion(Entity endpoint);
    }

    /// <summary>Requires actual service/queue admission, not generic proximity to an undocked target.</summary>
    public sealed class DockWorkRequirementDef : WorkRequirementDef, IWorkerWorkRequirement
    {
        public override System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        // Identity alone declares this capability guard; Def rejects any extra fields.
        protected override void ReadCore(JsonReader reader) { }
        public override WorkRequirementResult Evaluate(WorkRequirementContext context)
        {
            Code refusal = context.World.Services.Get<IWorkDockAdmission>().CheckDock(context);
            return refusal.IsNone ? WorkRequirementResult.Satisfied() : WorkRequirementResult.Blocked(refusal);
        }
    }
}
