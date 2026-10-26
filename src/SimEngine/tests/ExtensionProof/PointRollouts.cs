using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed partial class PointSpace
    {
        private Code CheckMobileRollout(World world, EntityRolloutRequest request, PointPlacement placement)
        {
            if (!_accessOpen) return Codes.Blocked;
            if (request.Origin is not PointPlacement origin || request.Producer.Component<IPointDockDefinition>() is not IPointDockDefinition layout)
                return Codes.InvalidArgument;
            if (!MobileVacancy(world, null, placement.Position, placement.Position).IsNone) return Codes.Blocked;
            foreach (var lane in WorkDockRules.RolloutsFor(request.Producer, request.Output))
            {
                bool selected = false;
                foreach (Code start in lane.Starts) if (origin.Position + layout.Offset(start) == placement.Position) selected = true;
                if (!selected) continue;
                foreach (Code exit in lane.Exits)
                {
                    Fix goal = origin.Position + layout.Offset(exit);
                    if (goal < Fix.Zero || goal > _definition.Extent) continue;
                    if (goal == placement.Position) throw new System.InvalidOperationException("launch and exit cannot alias physical geometry");
                    if (!MobileVacancy(world, null, placement.Position, goal).IsNone) continue;
                    Fix minimum = Fix.Min(placement.Position, goal), maximum = Fix.Max(placement.Position, goal); bool blocked = false;
                    foreach (var occupied in request.Unavailable)
                    {
                        if (occupied.Placement is not PointPlacement other || other.Space != Id) return Codes.InvalidArgument;
                        blocked |= other.Position >= minimum && other.Position <= maximum;
                    }
                    if (!blocked) return Code.None;
                }
            }
            return Codes.Blocked;
        }
    }
}
