using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Target/selected-worker reference operations over the one existing activity argument parser.</summary>
    public static class PlanEntityReferenceContracts
    {
        public static IReadOnlyList<EntityId> Read(IEntityTargetPlanDefinition plan, JsonValue arguments)
        {
            var selection = ActivityWorkerSelection.Read(arguments, plan.Graph, plan.PlanKey + ".intent", out var target);
            var ids = new SortedSet<EntityId> { target };
            foreach (var binding in selection.Bindings) ids.Add(binding.Worker);
            return new List<EntityId>(ids).AsReadOnly();
        }
        public static Code Check(IEntityTargetPlanDefinition plan, World world, PlayerId company, JsonValue arguments, Entity source, EntityDef destination)
        {
            var selection = ActivityWorkerSelection.Read(arguments, plan.Graph, plan.PlanKey + ".intent", out var target);
            if (source.Owner != company) return Codes.NotOwned;
            if (target == source.Id)
            {
                try { plan.ValidateTargetDefinition(destination); }
                catch (ContentException) { return StandardCodes.WrongTarget; }
                if (world.Content.Find<IWorkEffect>(plan.OnComplete) is IEntityReplacementEffect replacement &&
                    !EntityObservation.ReadProvider(world, replacement, source, () => replacement.AcceptsSourceDefinition(destination),
                        space: world.Space, observeTargetState: true)) return StandardCodes.WrongTarget;
            }
            foreach (var binding in selection.Bindings)
                if (binding.Worker == source.Id)
                {
                    var worker = destination.Component<IWorkerDefinition>();
                    if (!destination.HasCapability<IWorker>() || worker == null) return WorkSystem.BlockerNoWorker;
                    bool compatible = false;
                    foreach (var role in plan.Graph.Find(binding.Node).Roles)
                        foreach (var supported in worker.Roles)
                            if (role == supported && (!binding.Role.IsValid || binding.Role == role)) compatible = true;
                    if (!compatible) return WorkSystem.BlockerNoWorker;
                }
            return Code.None;
        }
        public static JsonValue Replace(IEntityTargetPlanDefinition plan, JsonValue arguments, EntityId source, EntityId destination)
        {
            var selection = ActivityWorkerSelection.Read(arguments, plan.Graph, plan.PlanKey + ".intent", out var target);
            var bindings = new List<ActivityWorkerBinding>();
            foreach (var binding in selection.Bindings)
                bindings.Add(new ActivityWorkerBinding(binding.Node, binding.Worker == source ? destination : binding.Worker, binding.Role));
            return ActivityWorkerSelection.CreateArguments(target == source ? destination : target, new ActivityWorkerSelection(bindings));
        }
    }
}
