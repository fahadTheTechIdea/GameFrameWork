using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    /// <summary>Configures an owned provider within its authored target bounds.</summary>
    public sealed class ConfigureOperationCommand : Command, IEntityControlCommand
    {
        public EntityId TargetEntity => Entity;
        public EntityId Entity;
        public Code Operation;
        public bool Enabled;
        public Fix Target;
        public override void Sync(IStateSync sync)
        {
            sync.Entity(ref Entity);
            string id = Operation.Id; sync.String(ref id); Operation = id == null ? Code.None : new Code(id);
            sync.Bool(ref Enabled); sync.Fix(ref Target);
        }
    }
}
