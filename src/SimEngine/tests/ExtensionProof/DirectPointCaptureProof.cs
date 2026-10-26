using System;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    // Engine-side negative injection only. The public driver never obtains a World or a writer.
    public sealed class DirectPointCaptureCommand : Command
    {
        public override void Sync(IStateSync sync) { }
    }
    public sealed class DirectPointCaptureHandler : CommandHandler<DirectPointCaptureCommand>
    {
        protected override Code Check(World world, DirectPointCaptureCommand command)
            => world.Space is PointSpace ? Code.None : Codes.InvalidArgument;
        protected override void Apply(World world, DirectPointCaptureCommand command, CommandContext context)
        {
            ((PointSpace)world.Space).SetAccess(false);
            // Read inside the command before the next publication can mask an unvalidated live read.
            _ = world.ViewFor(command.Player).Space;
            throw new InvalidOperationException("proof.direct-space-read-returned");
        }
    }
}
