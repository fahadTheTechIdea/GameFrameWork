using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Continuous execution capability; selected stores are explicit named resource ports.</summary>
    public interface ITransformerFlow : ITransformer { }

    /// <summary>Detached native-port/rate facts derived from earned progress; no remainder or stock ledger.</summary>
    public readonly struct TransformationFlowSnapshot
    {
        public static TransformationFlowSnapshot None => new TransformationFlowSnapshot(Array.Empty<ResourcePortSnapshot>(),
            Array.Empty<ResourcePortSnapshot>(), Array.Empty<ResourcePortSnapshot>(), Bill.Empty, Bill.Empty,
            Bill.Empty, Bill.Empty, Bill.Empty, Bill.Empty, Code.None);
        public IReadOnlyList<ResourcePortSnapshot> InputPorts { get; }
        public IReadOnlyList<ResourcePortSnapshot> OutputPorts { get; }
        public IReadOnlyList<ResourcePortSnapshot> ToolPorts { get; }
        public Bill ToolConsumed { get; }
        public Bill NextTools { get; }
        public Bill Consumed { get; }
        public Bill Produced { get; }
        public Bill NextInputs { get; }
        public Bill NextOutputs { get; }
        public Code NextBlocker { get; }
        public TransformationFlowSnapshot(IReadOnlyList<ResourcePortSnapshot> inputPorts, IReadOnlyList<ResourcePortSnapshot> outputPorts,
            IReadOnlyList<ResourcePortSnapshot> toolPorts, Bill consumed, Bill produced, Bill nextInputs, Bill nextOutputs,
            Bill toolConsumed, Bill nextTools, Code nextBlocker)
        {
            if (inputPorts == null || outputPorts == null || toolPorts == null) throw new ArgumentNullException();
            InputPorts = new List<ResourcePortSnapshot>(inputPorts).AsReadOnly();
            OutputPorts = new List<ResourcePortSnapshot>(outputPorts).AsReadOnly();
            ToolPorts = new List<ResourcePortSnapshot>(toolPorts).AsReadOnly(); ToolConsumed = toolConsumed; NextTools = nextTools;
            Consumed = consumed; Produced = produced; NextInputs = nextInputs; NextOutputs = nextOutputs; NextBlocker = nextBlocker;
        }
    }
    public sealed partial class Transformer : ITransformerFlow
    {
        private ReservationOwner FlowOwner => OwnerFor(_execution > 0 ? _execution : Queue[0].Execution);
        private Code CheckFlow(World world, ITransformationDefinition definition, out WorkProgress next, out Bill draw, out Bill outputs,
            out IReadOnlyList<ResourceClaimIntent> claims)
        {
            next = Progress; draw = outputs = Bill.Empty; claims = Array.Empty<ResourceClaimIntent>();
            var inputs = TransformationContracts.InputBindings(world, Entity, definition);
            var ports = TransformationContracts.OutputBindings(world, Entity, definition);
            var operatingPorts = TransformationContracts.OperatingBindings(world, Entity, definition);
            var tools = TransformationContracts.ToolBindings(world, Entity, definition, _execution > 0 ? _toolSources : null);
            Code unavailable=ResourcePortContracts.Blocker(inputs,ports,operatingPorts,tools);
            if(!unavailable.IsNone)return unavailable;
            int version = Entity.Version;
            try
            {
                next = Progress.AdvanceUnbounded(TargetRate);
                var flow = TransformationContracts.FlowFacts(definition, world.Time, inputs, ports, tools, Progress, TargetRate, Phase);
                if (!flow.NextBlocker.IsNone) return flow.NextBlocker;
                Bill utilities = TransformationContracts.OperatingRates(definition, world.Time, operatingPorts,
                    new WorkProgress(_runningTicks), new WorkProgress(checked(_runningTicks + 1)), false);
                var grouped = ResourcePortContracts.OutboundClaims(FlowOwner,
                    ResourcePortContracts.QuantitiesByEndpoint(inputs, flow.NextInputs),
                    ResourcePortContracts.QuantitiesByEndpoint(operatingPorts, utilities),
                    ResourcePortContracts.QuantitiesByEndpoint(tools, flow.NextTools));
                var original = ToolClaims(world); var requests = new List<ResourceClaimIntent>();
                foreach (var vector in grouped)
                    foreach (var line in vector.Quantities.Lines)
                    {
                        var quantity = new Bill(new[] { line }); ResourceReservation held = default;
                        foreach (var record in original)
                            if (record.Token.Endpoint == vector.Endpoint && record.Resource == line.Resource) { held = record; break; }
                        requests.Add(held.Token.IsValid ? new ResourceClaimIntent(held, quantity)
                            : new ResourceClaimIntent(ReservationDirection.Outbound, FlowOwner, vector.Endpoint, quantity));
                    }
                claims = requests.AsReadOnly();
                var amounts = new List<ResourceAmount>(flow.NextInputs.Lines); amounts.AddRange(utilities.Lines);
                amounts.AddRange(flow.NextTools.Lines);
                draw = new Bill(amounts); outputs = flow.NextOutputs;
            }
            catch (OverflowException) { return TransformerManager.BlockerFlowRange; }
            IResourceTransaction transactions = world.Services.Get<IResourceTransaction>();
            var admission = transactions.EvaluateConversionClaims(Entity.Owner, FlowOwner, claims,
                ResourcePortContracts.QuantitiesByEndpoint(ports, outputs, definition.OutputQualities), new Code(definition.Id));
            ResourcePortContracts.Verify(ports); ResourcePortContracts.Verify(inputs);
            ResourcePortContracts.Verify(operatingPorts); ResourcePortContracts.Verify(tools);
            if (version != Entity.Version) throw new InvalidOperationException("net flow admission mutated actual participants");
            if (!admission.Ready)
            {
                if (admission.Status == ResourceTransferStatus.SettlementRefused) return admission.Blocker;
                if (admission.Status == ResourceTransferStatus.NoCapacity)
                {
                    if (Def.AutomaticSupply) world.Services.Get<ITransformationSupplyRequests>().WithdrawInputs(this);
                    return TransformerManager.BlockerOutput;
                }
                if (admission.Status != ResourceTransferStatus.NoStock)
                    return admission.Status == ResourceTransferStatus.Incompatible ? TransformerManager.BlockerInputCompatibility
                        : TransformerManager.BlockerInputEndpoint;
                RequestInputSupply(world, claims, inputs, tools, operatingPorts);
                return TransformerManager.BlockerFlowInputs;
            }
            return Code.None;
        }
        private Code AdvanceFlow(World world, ITransformationDefinition definition)
        {
            Code check = CheckFlow(world, definition, out WorkProgress next, out Bill draw, out Bill outputs, out var claims);
            if (!check.IsNone) return check;
            IResourceTransaction transactions = world.Services.Get<IResourceTransaction>();
            IReadOnlyList<ResourceReservation> consumed = Array.Empty<ResourceReservation>();
            if (claims.Count != 0)
            {
                var acquired = transactions.AcquireClaims(Entity.Owner, claims, System.Array.Empty<ResourceReservation>());
                if (!acquired.Committed) throw new InvalidOperationException("continuous complete selected input admission changed before acquisition");
                consumed = acquired.Receipts;
            }
            if (!draw.IsEmpty || !outputs.IsEmpty)
            {
                var ports = TransformationContracts.OutputBindings(world, Entity, definition);
                var conversionInputs = new List<ResourceConversionInput>();
                foreach (var receipt in consumed)
                {
                    Fix requested = Fix.Zero;
                    foreach (var claim in claims)
                        if (claim.Endpoint == receipt.Token.Endpoint && claim.Owner.Equals(receipt.Owner))
                            requested += claim.Quantities.AmountOf(receipt.Resource);
                    if (!requested.IsPositive) throw new InvalidOperationException("continuous draw lost its actual requested receipt delta");
                    conversionInputs.Add(new ResourceConversionInput(ResourceFlowKind.Consumed, receipt, requested, false));
                }
                var conversion = new ResourceConversionIntent(FlowOwner, Entity.Owner, conversionInputs,
                    ResourcePortContracts.QuantitiesByEndpoint(ports, outputs, definition.OutputQualities), new Code(definition.Id));
                if (!transactions.CommitConversion(conversion, null).Committed)
                    throw new InvalidOperationException("continuous complete input/output admission changed after acquisition");
            }
            Progress = next; _runningTicks = checked(_runningTicks + 1);
            if (Def.AutomaticSupply) world.Services.Get<ITransformationSupplyRequests>().WithdrawInputs(this);
            return Code.None;
        }
        private TransformationFlowSnapshot ReadFlow(World world)
        {
            var head = Queue.Count == 0 ? null : world.Content.Get<ITransformationDefinition>(Queue[0].Transformation);
            return TransformationContracts.FlowFacts(head, world.Time, TransformationContracts.InputBindings(world, Entity, head),
                TransformationContracts.OutputBindings(world, Entity, head), TransformationContracts.FlowToolBindings(world, this, head),
                Progress, TargetRate, Phase);
        }
    }
    public static partial class TransformationContracts
    {
        internal static TransformationFlowSnapshot FlowFacts(ITransformationDefinition head, ISimulationTime time, IReadOnlyList<ResourcePortBinding> inputs,
            IReadOnlyList<ResourcePortBinding> outputs, IReadOnlyList<ResourcePortBinding> tools, WorkProgress progress, Fix target, TransformationPhase phase)
        {
            Bill consumed = Bill.Empty, produced = Bill.Empty, nextInputs = Bill.Empty, nextOutputs = Bill.Empty; Code blocker = ResourcePortContracts.Blocker(inputs,outputs,tools);
            Bill toolConsumed = Bill.Empty, nextTools = Bill.Empty;
            if (head?.Mode == TransformationMode.Continuous)
            {
                consumed = InputRates(head, time, inputs, WorkProgress.Zero, progress, true);
                produced = OutputRates(head, time, outputs, WorkProgress.Zero, progress, true);
                toolConsumed = ToolRates(head, time, tools, WorkProgress.Zero, progress, true);
                if (phase != TransformationPhase.Returning)
                {
                    try
                    {
                        WorkProgress next = progress.AdvanceUnbounded(target);
                        nextInputs = InputRates(head, time, inputs, progress, next, false);
                        nextOutputs = OutputRates(head, time, outputs, progress, next, false);
                        nextTools = ToolRates(head, time, tools, progress, next, false);
                    }
                    catch (OverflowException) { nextInputs = nextOutputs = nextTools = Bill.Empty; blocker = TransformerManager.BlockerFlowRange; }
                }
            }
            return new TransformationFlowSnapshot(ResourcePortContracts.Facts(inputs), ResourcePortContracts.Facts(outputs),
                ResourcePortContracts.Facts(tools), consumed, produced, nextInputs, nextOutputs, toolConsumed, nextTools, blocker);
        }
        internal static IReadOnlyList<ResourcePortBinding> FlowToolBindings(World world, ITransformer provider, ITransformationDefinition head)
            => head?.Mode != TransformationMode.Continuous ? Array.Empty<ResourcePortBinding>() : ToolBindings(world, provider.Entity, head,
                provider is ITransformerTools tools && tools.WorkInProgressOwner.IsValid ? tools.ToolSources : null);

        private static void ValidateFlow(World world, ITransformer provider, TransformationSnapshot state, ITransformationDefinition head)
        {
            var facts = state.Flow;
            if (facts.Consumed == null || facts.Produced == null || facts.NextInputs == null || facts.NextOutputs == null ||
                facts.ToolConsumed == null || facts.NextTools == null || facts.ToolPorts == null ||
                facts.InputPorts == null || facts.OutputPorts == null) throw new CorruptStateException("missing explicit flow facts");
            if (!(provider is ITransformerFlow))
            {
                if (head?.Mode == TransformationMode.Continuous || facts.InputPorts.Count != 0 || facts.OutputPorts.Count != 0 ||
                    facts.ToolPorts.Count != 0 || !facts.ToolConsumed.IsEmpty || !facts.NextTools.IsEmpty ||
                    !facts.Consumed.IsEmpty || !facts.Produced.IsEmpty || !facts.NextInputs.IsEmpty || !facts.NextOutputs.IsEmpty || !facts.NextBlocker.IsNone)
                    throw new CorruptStateException("provider without flow reports continuous state");
                return;
            }
            int version = provider.Entity.Version;
            var inputs = InputBindings(world, provider.Entity, head); var outputs = OutputBindings(world, provider.Entity, head);
            var tools = FlowToolBindings(world, provider, head);
            TransformationFlowSnapshot expected;
            try { expected = FlowFacts(head, world.Time, inputs, outputs, tools, state.Progress, OperationContracts.ReadControl(world, provider).Target, state.Phase); }
            catch (OverflowException) { throw new CorruptStateException("saved continuous totals exceed native representation"); }
            if (!SameBill(facts.Consumed, expected.Consumed) || !SameBill(facts.Produced, expected.Produced) || !SameBill(facts.NextInputs, expected.NextInputs) ||
                !SameBill(facts.NextOutputs, expected.NextOutputs) || !SameBill(facts.ToolConsumed, expected.ToolConsumed) ||
                !SameBill(facts.NextTools, expected.NextTools) || facts.NextBlocker != expected.NextBlocker)
                throw new CorruptStateException("continuous facts differ from actual authored progress/rates");
            ResourcePortContracts.ValidateFacts(facts.InputPorts, expected.InputPorts); ResourcePortContracts.ValidateFacts(facts.OutputPorts, expected.OutputPorts);
            ResourcePortContracts.ValidateFacts(facts.ToolPorts, expected.ToolPorts);
            ResourcePortContracts.Verify(inputs); ResourcePortContracts.Verify(outputs); ResourcePortContracts.Verify(tools);
            if (provider.Entity.Version != version) throw new InvalidOperationException("continuous provider mutated a pure read");
        }

    }
    public sealed partial class TransformerManager
    {
        public static readonly Code BlockerFlowRange = new Code("blocker.transformation-flow-range");
        public static readonly Code BlockerFlowInputs = new Code("blocker.transformation-flow-inputs");
    }
}
