using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Commands
{
    public sealed class ForgetKnowledgeCommand : Command
    {
        public string Policy;
        public override void Sync(IStateSync sync) => sync.String(ref Policy);
    }
    /// <summary>Shares only an authored outgoing subject set. No caller-supplied facts or timestamps.</summary>
    public sealed class ShareKnowledgeCommand : Command
    {
        public PlayerId Recipient;
        public string Policy;
        public override void Sync(IStateSync sync) { sync.Player(ref Recipient); sync.String(ref Policy); }
    }
}
