using System;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    /// <summary>Ordinary physical movement of owned resources; no instant delivery or purchase.</summary>
    public sealed class HaulResourcesCommand : Command, IEntityControlCommand
    {
        public EntityId Source, Destination, Worker;
        public Code SourcePort, DestinationPort;
        public Code Selection;
        public ResourceId Resource;
        public Fix Ceiling;
        public int Priority;
        public EntityId TargetEntity => Destination;
        public override void Sync(IStateSync sync)
        {
            sync.Entity(ref Source); sync.Entity(ref Destination); sync.Entity(ref Worker);
            string source = SourcePort.Id, destination = DestinationPort.Id;
            sync.String(ref source); sync.String(ref destination);
            string selection = Selection.Id; sync.String(ref selection); if (sync.Reading) Selection = selection == null ? Code.None : new Code(selection);
            if (sync.Reading) { SourcePort = source == null ? Code.None : new Code(source); DestinationPort = destination == null ? Code.None : new Code(destination); }
            sync.Resource(ref Resource); sync.Fix(ref Ceiling); sync.Int(ref Priority);
        }
    }
    internal sealed class HaulResourcesHandler : CommandHandler<HaulResourcesCommand>
    {
        protected override Code Check(World world, HaulResourcesCommand command)
        {
            HaulPreview result = world.Services.Get<IHaulPlayerControls>().PreviewPlayerHaul(command.Player,
                command.Source, command.Destination, command.Resource, command.Ceiling, worker: command.Worker,
                sourcePort: command.SourcePort, destinationPort: command.DestinationPort, priority: command.Priority, selection: command.Selection);
            if (result.Status == HaulStatus.Ready) return Code.None;
            if (!result.Blocker.IsNone) return result.Blocker;
            return result.Status == HaulStatus.NotFound ? Codes.NotFound : result.Status == HaulStatus.NotOwned ? Codes.NotOwned
                : result.Status == HaulStatus.InvalidRequest ? Codes.InvalidArgument : new Code("haul." + result.Status.ToString().ToLowerInvariant());
        }
        protected override void Apply(World world, HaulResourcesCommand command, CommandContext result)
        {
            int job = world.Services.Get<IHaulPlayerControls>().AcquirePlayerHaul(command.Player, command.Source,
                command.Destination, command.Resource, command.Ceiling, command.Priority, worker: command.Worker,
                sourcePort: command.SourcePort, destinationPort: command.DestinationPort, selection: command.Selection);
            if (job == 0) throw new InvalidOperationException("physical haul changed after command admission");
            result.Entity = command.Destination; result.Arg0 = job;
        }
    }
}
