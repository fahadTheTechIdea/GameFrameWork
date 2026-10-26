using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class Transformer : IUtilityConsumer
    {
        public IResourcePort ResolveUtilityPort(World world, ResourceId resource)
        {
            if (Queue.Count == 0) return null;
            var head = world.Content.Get<ITransformationDefinition>(Queue[0].Transformation);
            foreach (var route in head.OperatingPorts) if (route.Resource == resource) return Entity.Get<IResourcePort>(route.Port);
            return null;
        }
        public UtilityDemandSnapshot ReadUtilityDemand(World world) => ReadOperatingInputs(world);
        private ReservationOwner OperatingOwner => new ReservationOwner(
            TransformerManager.WipOwnerScope(WorkInProgress.Endpoint) + ".operating", _execution > 0 ? _execution : Queue[0].Execution);
        private Code CheckStartupInputs(World world, ITransformationDefinition definition)
        {
            var ports = TransformationContracts.InputBindings(world, Entity, definition);
            var tools = TransformationContracts.ToolBindings(world, Entity, definition);
            var operatingPorts = TransformationContracts.OperatingBindings(world, Entity, definition);
            Code unavailable=ResourcePortContracts.Blocker(ports,tools,operatingPorts);if(!unavailable.IsNone)return unavailable;
            Bill inputs = definition.Mode == TransformationMode.Batch ? definition.Inputs
                : TransformationContracts.InputRates(definition, world.Time, ports, WorkProgress.Zero,
                    WorkProgress.Zero.AdvanceUnbounded(TargetRate), false);
            Bill operating = TransformationContracts.OperatingRates(definition, world.Time, operatingPorts,
                WorkProgress.Zero, new WorkProgress(1), false);
            Bill wear = definition.Mode == TransformationMode.Continuous ? TransformationContracts.ToolRates(definition, world.Time, tools,
                WorkProgress.Zero, WorkProgress.Zero.AdvanceUnbounded(TargetRate), false) : Bill.Empty;
            var claims = ResourcePortContracts.OutboundClaims(OwnerFor(Queue[0].Execution),
                ResourcePortContracts.QuantitiesByEndpoint(ports, inputs),
                ResourcePortContracts.QuantitiesByEndpoint(tools, definition.Tools),
                ResourcePortContracts.QuantitiesByEndpoint(operatingPorts, operating),
                ResourcePortContracts.QuantitiesByEndpoint(tools, wear));
            if (claims.Count == 0) return Code.None;
            var transactions = world.Services.Get<IResourceTransaction>();
            ResourceClaimCheck admission = transactions.EvaluateClaims(Entity.Owner, claims, System.Array.Empty<ResourceReservation>());
            ResourcePortContracts.Verify(ports); ResourcePortContracts.Verify(tools); ResourcePortContracts.Verify(operatingPorts);
            if (admission.Ready) return Code.None;
            if (admission.Status != ResourceTransferStatus.NoStock)
                return admission.Status == ResourceTransferStatus.Incompatible ? TransformerManager.BlockerInputCompatibility : TransformerManager.BlockerInputEndpoint;
            RequestInputSupply(world, claims, ports, tools, operatingPorts);
            var toolClaims = ResourcePortContracts.OutboundClaims(OwnerFor(Queue[0].Execution),
                ResourcePortContracts.QuantitiesByEndpoint(tools, definition.Tools));
            if (toolClaims.Count != 0 && !transactions.EvaluateClaims(Entity.Owner, toolClaims, System.Array.Empty<ResourceReservation>()).Ready) return TransformerManager.BlockerTools;
            var feedClaims = ResourcePortContracts.OutboundClaims(OwnerFor(Queue[0].Execution), ResourcePortContracts.QuantitiesByEndpoint(ports, inputs));
            if (feedClaims.Count == 0 || transactions.EvaluateClaims(Entity.Owner, feedClaims, System.Array.Empty<ResourceReservation>()).Ready)
            {
                var running = ResourcePortContracts.OutboundClaims(OwnerFor(Queue[0].Execution),
                    ResourcePortContracts.QuantitiesByEndpoint(operatingPorts, operating));
                if (running.Count != 0 && !transactions.EvaluateClaims(Entity.Owner, running, System.Array.Empty<ResourceReservation>()).Ready) return TransformerManager.BlockerOperatingInputs;
            }
            return definition.Mode == TransformationMode.Batch ? WorkSystem.BlockerMaterials : TransformerManager.BlockerFlowInputs;
        }
        private void RequestInputSupply(World world, IReadOnlyList<ResourceClaimIntent> claims,
            params IReadOnlyList<ResourcePortBinding>[] bindings)
        {
            if (!Def.AutomaticSupply) return;
            var groups = new Dictionary<(Code, ResourceEndpointId), List<ResourceAmount>>();
            foreach (ResourceClaimIntent claim in claims) foreach (ResourceAmount line in claim.Quantities.Lines)
            {
                Code port = Code.None; bool found = false;
                foreach (var set in bindings)
                {
                    foreach (var binding in set)
                        if (binding.Endpoint == claim.Endpoint && binding.Route.Resource == line.Resource)
                        { port = binding.Route.Port; found = true; break; }
                    if (found) break;
                }
                if (!found) throw new InvalidOperationException("supply claim has no exact authored physical port");
                var key = (port, claim.Endpoint);
                if (!groups.TryGetValue(key, out var amounts)) groups.Add(key, amounts = new List<ResourceAmount>());
                amounts.Add(line);
            }
            var requests = new List<LogisticsSupplyRequest>();
            foreach (var group in groups) requests.Add(new LogisticsSupplyRequest(group.Key.Item1, group.Key.Item2, new Bill(group.Value)));
            world.Services.Get<ITransformationSupplyRequests>().RequestInputs(this, requests.AsReadOnly(), Tuning.TransformationSupplyPriority);
            foreach (var set in bindings) ResourcePortContracts.Verify(set);
        }
        private Code ConsumeOperatingInputs(World world, ITransformationDefinition definition)
        {
            var ports = TransformationContracts.OperatingBindings(world, Entity, definition);
            Code unavailable=ResourcePortContracts.Blocker(ports);if(!unavailable.IsNone)return unavailable;
            Bill draw;
            try { draw = TransformationContracts.OperatingRates(definition, world.Time, ports,
                new WorkProgress(_runningTicks), new WorkProgress(checked(_runningTicks + 1)), false); }
            catch (OverflowException) { return TransformerManager.BlockerOperatingRange; }
            var claims = ResourcePortContracts.OutboundClaims(OperatingOwner, ResourcePortContracts.QuantitiesByEndpoint(ports, draw));
            if (claims.Count == 0)
            {
                if (Def.AutomaticSupply) world.Services.Get<ITransformationSupplyRequests>().WithdrawInputs(this);
                return Code.None;
            }
            var transactions = world.Services.Get<IResourceTransaction>(); var check = transactions.EvaluateClaims(Entity.Owner, claims, System.Array.Empty<ResourceReservation>());
            ResourcePortContracts.Verify(ports);
            if (!check.Ready)
            {
                if (check.Status != ResourceTransferStatus.NoStock) return check.Status == ResourceTransferStatus.Incompatible
                    ? TransformerManager.BlockerInputCompatibility : TransformerManager.BlockerInputEndpoint;
                RequestInputSupply(world, claims, ports);
                return TransformerManager.BlockerOperatingInputs;
            }
            var acquired = transactions.AcquireClaims(Entity.Owner, claims, System.Array.Empty<ResourceReservation>());
            if (!acquired.Committed || !transactions.ConsumeClaims(Entity.Owner, acquired.Receipts, TransformerManager.CauseOperatingInputs, System.Array.Empty<ResourceReservation>()).Committed)
                throw new InvalidOperationException("whole actual operating draw changed after admission");
            if (Def.AutomaticSupply) world.Services.Get<ITransformationSupplyRequests>().WithdrawInputs(this);
            return Code.None;
        }
        private UtilityDemandSnapshot ReadOperatingInputs(World world)
        {
            var head = Queue.Count == 0 ? null : world.Content.Get<ITransformationDefinition>(Queue[0].Transformation);
            var ports = TransformationContracts.OperatingBindings(world, Entity, head);
            Bill consumed = head == null ? Bill.Empty : TransformationContracts.OperatingRates(head, world.Time, ports,
                WorkProgress.Zero, new WorkProgress(_runningTicks), true);
            Bill next = Bill.Empty; Code blocker = ResourcePortContracts.Blocker(ports);
            if (head != null && Phase != TransformationPhase.Blocked && Phase != TransformationPhase.Returning)
            {
                try { next = TransformationContracts.OperatingRates(head, world.Time, ports, new WorkProgress(_runningTicks),
                    new WorkProgress(checked(_runningTicks + 1)), false); }
                catch (OverflowException) { blocker = TransformerManager.BlockerOperatingRange; }
            }
            var available = new List<ResourceAmount>();
            foreach (var port in ports)
            {
                if(!port.Ready)continue;
                ResourceId resource = port.Route.Resource;
                if (!ResourceQuantitySignals.Capture(world, port.Store, resource, out var quantity).IsNone)
                    throw new InvalidOperationException("operating draw lost its actual selected stock compatibility");
                Fix contribution = Fix.Min(next.AmountOf(resource), quantity.Available);
                if (contribution.IsPositive) available.Add(new ResourceAmount(resource, contribution));
                ResourcePortContracts.Verify(ports);
            }
            bool drawing = _enabled && ports.Count > 0 && Phase != TransformationPhase.Blocked && Phase != TransformationPhase.Returning;
            return new UtilityDemandSnapshot(CapabilityId, _runningTicks, ResourcePortContracts.Facts(ports), drawing,
                consumed, next, new Bill(available), blocker);
        }
    }
    public sealed partial class TransformerManager
    {
        public static readonly Code BlockerOperatingInputs = new Code("blocker.transformation-operating-inputs");
        public static readonly Code BlockerOperatingRange = new Code("blocker.transformation-operating-range");
        public static readonly Code CauseOperatingInputs = new Code("transformation.operating-inputs");
    }
    public static partial class TransformationContracts
    {
        private static void ValidateOperatingInputs(World world, ITransformer provider, TransformationSnapshot state, ITransformationDefinition head)
        {
            var facts = state.Operating;
            if (facts.Consumed == null || facts.NextInputs == null || facts.Available == null || facts.Ports == null)
                throw new CorruptStateException("missing explicit operating facts");
            if (!(provider is IUtilityConsumer inputs))
            {
                if (!facts.Consumer.IsNone || facts.Drawing || facts.Resources == null || facts.Resources.Count != 0 ||
                    facts.Ports.Count != 0 || !facts.Available.IsEmpty || facts.ElapsedTicks != 0 || !facts.Consumed.IsEmpty ||
                    !facts.NextInputs.IsEmpty || !facts.NextBlocker.IsNone || head != null && head.OperatingInputs.Count != 0)
                    throw new CorruptStateException("provider without operating inputs reports running resource state");
                return;
            }
            int version = provider.Entity.Version;
            if (facts.ElapsedTicks < 0 || facts.ElapsedTicks > world.Tick && facts.ElapsedTicks - world.Tick > 1 ||
                (state.Phase == TransformationPhase.Idle || state.Phase == TransformationPhase.Supplying) && facts.ElapsedTicks != 0 ||
                !state.Progress.Equals(WorkProgress.Zero) && facts.ElapsedTicks == 0)
                throw new CorruptStateException("invalid operating exposure/phase");
            UtilityContracts.Validate(world, inputs, facts);
            var ports = OperatingBindings(world, provider.Entity, head);
            ResourcePortContracts.ValidateFacts(facts.Ports, ResourcePortContracts.Facts(ports));
            Bill consumed;
            try { consumed = head == null ? Bill.Empty : OperatingRates(head, world.Time, ports, WorkProgress.Zero, new WorkProgress(facts.ElapsedTicks), true); }
            catch (OverflowException) { throw new CorruptStateException("saved operating consumption exceeds native representation"); }
            Bill next = Bill.Empty; Code blocker = ResourcePortContracts.Blocker(ports);
            if (head != null && state.Phase != TransformationPhase.Blocked && state.Phase != TransformationPhase.Returning)
            {
                try { next = OperatingRates(head, world.Time, ports, new WorkProgress(facts.ElapsedTicks), new WorkProgress(checked(facts.ElapsedTicks + 1)), false); }
                catch (OverflowException) { blocker = TransformerManager.BlockerOperatingRange; }
            }
            bool drawing = OperationContracts.ReadControl(world, provider).Enabled && ports.Count > 0 && state.Phase != TransformationPhase.Blocked && state.Phase != TransformationPhase.Returning;
            if (!SameBill(facts.Consumed, consumed) || !SameBill(facts.NextInputs, next) || facts.NextBlocker != blocker || facts.Drawing != drawing)
                throw new CorruptStateException("operating facts differ from actual elapsed time/rates/selected quantum");
            ResourcePortContracts.Verify(ports);
            if (provider.Entity.Version != version) throw new InvalidOperationException("operating provider mutated a pure read");
        }
        private static bool SameBill(Bill left, Bill right)
        {
            if (left.Count != right.Count) return false;
            foreach (ResourceAmount line in left.Lines) if (right.AmountOf(line.Resource) != line.Amount) return false;
            return true;
        }
    }
}
