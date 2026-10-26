using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Admission and scoped observation of actual immutable policy aliases, never a policy/state registry.</summary>
    internal static class NetworkPolicyContracts
    {
        internal static TResult Read<TPolicy, TResult>(World world, TPolicy policy, Entity entity, Func<TResult> read, bool observeTargetState = false)
            where TPolicy : class, IContentDefinition
            => EntityObservation.ReadProvider(world, policy, entity, () =>
            {
                RequireOwned(world, policy);
                return read();
            }, observeTargetState: observeTargetState);

        private static void RequireOwned<TPolicy>(World world, TPolicy policy) where TPolicy : class, IContentDefinition
        {
            if (!(policy is Def definition) || !definition.IsFrozen || !world.Content.Owns(definition) ||
                policy.Id != definition.Id || policy.Module != definition.Module ||
                !ReferenceEquals(world.Content.Find<TPolicy>(definition.Id), policy))
                throw new ContentException("network policy requires its actual immutable catalog alias");
        }

        internal static void ValidateComposition(World world)
        {
            foreach (var policy in world.Content.All<INetworkFlowPolicy>())
                Read(world, policy, null, () => true);
            foreach (var policy in world.Content.All<INetworkDemandPolicy>())
                Read(world, policy, null, () => true);
            foreach (var solver in world.Content.All<INetworkSolver>())
                Read(world, solver, null, () => { solver.ValidateTime(world.Time); return true; });
            foreach (var network in world.Content.All<INetworkDefinition>())
            {
                Read(world, network.FlowPolicy, null, () => true);
                Read(world, network.DemandPolicy, null, () => true);
                Read(world, network.Solver, null, () => true);
            }
        }
    }
}
