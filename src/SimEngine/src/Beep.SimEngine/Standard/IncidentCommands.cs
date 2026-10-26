using System;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Ordinary owned incident request; environment/pacing uses the same admission and inbox.</summary>
    public sealed class RaiseIncidentCommand : Command
    {
        public EntityId Target;
        public Code Source, Kind;
        public override void Sync(IStateSync sync)
        {
            sync.Entity(ref Target);
            string source = Source.Id, kind = Kind.Id; sync.String(ref source); sync.String(ref kind);
            if (sync.Reading) { Source = source == null ? Code.None : new Code(source); Kind = kind == null ? Code.None : new Code(kind); }
        }
    }
    internal sealed class RaiseIncidentHandler : CommandHandler<RaiseIncidentCommand>
    {
        protected override Code Check(World world, RaiseIncidentCommand command)
        {
            Entity target = world.Entities.Get(command.Target);
            if (target == null || !world.ViewFor(command.Player).TryGetEntity(command.Target, out _)) return Codes.NotFound;
            if (target.Owner != command.Player) return Codes.NotOwned;
            if (command.Source.IsNone || command.Kind.IsNone) return Codes.InvalidArgument;
            IIncident incident = target.Get<IIncident>(command.Source);
            return incident == null ? Codes.InvalidArgument : EntityObservation.Read(world, target,
                () => !incident.Supports(command.Kind) || ResponseWork.Read(incident).Active ? Codes.InvalidArgument : Code.None);
        }
        protected override void Apply(World world, RaiseIncidentCommand command, CommandContext result)
        {
            if (!world.Entities.Get(command.Target).Get<IIncident>(command.Source).Raise(world, command.Kind))
                throw new InvalidOperationException("incident command check/apply disagreed");
            result.Entity = command.Target;
        }
    }
}
