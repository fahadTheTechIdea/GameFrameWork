using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Pure eligibility, not operation state, a balance or a billing timer.</summary>
    public interface IOperatingCostCondition : IContentDependencyDefinition
    {
        void ValidateTarget(EntityDef target);
        Code Check(World world, Entity target);
    }
    public sealed class RunningOperationCostConditionDef : Def, IOperatingCostCondition
    {
        public Code Operation { get; private set; }
        public static readonly Code NotRunning = new Code("business.operation-not-running");
        public IReadOnlyList<IContentDefinition> ContentDependencies => Array.Empty<IContentDefinition>();
        protected override void ReadCore(JsonReader reader)
        {
            string operation = reader.String("operation");
            try { ContractIdentity.ValidateId(operation, "operation"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            Operation = new Code(operation);
        }
        public void ValidateTarget(EntityDef target)
        {
            if (target == null || !target.HasCapability<IOperation>(Operation))
                throw new ContentException(Id + ": target requires its actual named operation '" + Operation + "'");
        }
        public Code Check(World world, Entity target)
        {
            var operation = target.Get<IOperation>(Operation);
            if (operation == null) return OperationCodes.NotOperation;
            var state = EntityObservation.ReadProvider(world, operation, target, () => operation.ReadOperation(world), observeTargetState: true);
            return state.Status == OperationStatus.Running ? Code.None : NotRunning;
        }
    }
}
