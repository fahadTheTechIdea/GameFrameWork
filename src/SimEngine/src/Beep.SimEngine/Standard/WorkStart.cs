using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{

    public sealed partial class WorkBoard
    {
        private static bool HasSupplyRequirement(IWorkRequirement root) => HasRequirement<IWorkSupplyRequirement>(root);
        private static bool HasCostRequirement(IWorkRequirement root) => HasRequirement<IWorkCostRequirement>(root);
        private static bool HasRequirement<T>(IWorkRequirement root, Func<T, bool> predicate = null) where T : IWorkRequirement
        {
            var pending = new Stack<IWorkRequirement>(); var seen = new HashSet<IWorkRequirement>();
            if (root != null) pending.Push(root);
            while (pending.Count > 0)
            {
                var current = pending.Pop(); if (!seen.Add(current)) continue;
                if (current is T selected && (predicate == null || predicate(selected))) return true;
                if (current is IWorkRequirementGroup group) foreach (var entry in group.Entries) pending.Push(entry.Requirement);
            }
            return false;
        }
        internal static Bill ScaledBill(IResourceWorkRequirement requirement, Fix multiplier, IResourceCatalog content)
        {
            var lines = new List<ResourceAmount>();
            foreach (var line in requirement.Quantities.Lines)
            {
                Fix amount = ScaleAmount(line.Amount, multiplier, requirement.Id);
                if (UnitConversion.ValidateAmount(content.Get(line.Resource).QuantityUnit, amount) != UnitConversionStatus.Converted)
                    throw new ContentException(requirement.Id + ": scaled requirement violates native quantum");
                lines.Add(new ResourceAmount(line.Resource, amount));
            }
            return new Bill(lines);
        }
        private static Fix ScaleAmount(Fix amount, Fix multiplier, string requirement)
        {
            BigInteger product = (BigInteger)amount.Raw * multiplier.Raw;
            BigInteger raw = BigInteger.DivRem(product, Fix.One.Raw, out BigInteger remainder);
            if (!remainder.IsZero || raw <= 0 || raw > long.MaxValue)
                throw new ContentException(requirement + ": scaled requirement is not an exact positive representable quantity");
            return Fix.FromRaw((long)raw);
        }
        private static IReadOnlyList<IResourceRate> ScaledRates(IResourceWorkRequirement requirement, Bill quantities)
        {
            var rates = new List<IResourceRate>();
            foreach (var rate in requirement.Rates)
                rates.Add(new ResourceRate(rate.Resource, quantities.AmountOf(rate.Resource.Resource), rate.PeriodNumerator, rate.PeriodDenominator, rate.PeriodUnit));
            return rates.AsReadOnly();
        }
        private void ValidateScaledRequirements(IWorkRequirement root, Fix multiplier)
        {
            var pending = new Stack<IWorkRequirement>(); var seen = new HashSet<IWorkRequirement>();
            if (root != null) pending.Push(root);
            while (pending.Count > 0)
            {
                var requirement = pending.Pop(); if (!seen.Add(requirement)) continue;
                if (requirement is IResourceWorkRequirement resource) ScaledBill(resource, multiplier, _world.Content);
                if (requirement is IWorkCostRequirement cost)
                {
                    if (!cost.Amount.IsPositive || requirement is IResourceWorkRequirement || requirement is IWorkRequirementGroup)
                        throw new ContentException(requirement.Id + ": monetary requirement needs a positive, distinct leaf protocol");
                    ScaleAmount(cost.Amount, multiplier, requirement.Id);
                    if (BusinessModeRules.EvaluateCost(_world, cost.CostCategory, cost.Amount).Applicability == MonetaryCostApplicability.UnavailableCapability)
                        throw new ContentException(requirement.Id + ": enabled monetary work cost requires IEconomy");
                    if (!cost.Credit && BusinessModeRules.EvaluateCost(_world, cost.CostCategory, cost.Amount).Applicability == MonetaryCostApplicability.Enabled && !_world.Services.Has<IMonetaryBudgets>())
                        throw new ContentException(requirement.Id + ": enabled ordinary work cost requires IMonetaryBudgets");
                }
                if (requirement is IWorkRequirementGroup group) foreach (var entry in group.Entries) pending.Push(entry.Requirement);
            }
        }
        private static IEnumerable<(IWorkRequirement Requirement, IReadOnlyList<string> Path)> SelectedOccurrences(
            IWorkRequirement root, ActivityNodeExecution node)
        {
            var pending = new Stack<(IWorkRequirement Requirement, IReadOnlyList<string> Path)>();
            if (root != null) pending.Push((root, Array.AsReadOnly(new[] { root.Id })));
            while (pending.Count > 0)
            {
                var current = pending.Pop(); yield return current;
                if (!(current.Requirement is IWorkRequirementGroup group)) continue;
                string selected = null;
                if (group.Mode == WorkRequirementMode.Any) foreach (var choice in node.Choices) if (choice.At(current.Path)) { selected = choice.Entry; break; }
                for (int i = group.Entries.Count - 1; i >= 0; i--)
                {
                    var entry = group.Entries[i]; if (group.Mode == WorkRequirementMode.Any && selected != entry.Id) continue;
                    var path = new List<string>(current.Path) { entry.Id }; pending.Push((entry.Requirement, path.AsReadOnly()));
                }
            }
        }
        private void ValidateCostBindings(Order order, ActivityNodeExecution node, IActivityNodeDefinition definition, Job job)
        {
            bool assigned = order.IsActive && (node.State == NodeState.Working || node.State == NodeState.Paused) &&
                (definition.Roles.Count == 0 || job?.Worker.IsValid == true);
            var found = new HashSet<WorkCostBinding>();
            foreach (var occurrence in SelectedOccurrences(definition.Requirement, node))
            {
                if (!(occurrence.Requirement is IWorkCostRequirement requirement)) continue;
                WorkCostBinding binding = CostBinding(node, occurrence.Path);
                MonetaryCostDecision decision = BusinessModeRules.EvaluateCost(_world, requirement.CostCategory, requirement.Amount);
                if (decision.Applicability != MonetaryCostApplicability.Enabled)
                {
                    if (binding != null) throw new CorruptStateException("disabled monetary work cannot carry a paid or reserved obligation");
                    continue;
                }
                if (binding == null) { if (assigned) throw new CorruptStateException("assigned activity is missing its monetary obligation"); continue; }
                if (!found.Add(binding) || binding.Choice.Requirement != requirement.Id || binding.Choice.Amount != ScaleAmount(requirement.Amount, definition.RequirementMultiplier, requirement.Id) ||
                    binding.Choice.Credit != requirement.Credit || binding.Choice.Category != requirement.CostCategory || binding.Spent && node.Progress.WholeTicks == 0 && node.Progress.Fraction == 0 ||
                    !binding.Spent && (!assigned || node.Progress.WholeTicks > 0 || node.Progress.Fraction > 0))
                    throw new CorruptStateException("saved monetary obligation differs from its authored occurrence/lifetime");
                if (!binding.Choice.Credit)
                {
                    MonetaryBudgetReceipt receipt = binding.Receipt.Value;
                    if (receipt.Company != order.Owner || !receipt.Owner.Equals(binding.Choice.BudgetOwner(order.Id, node.Id)))
                        throw new CorruptStateException("saved work budget differs from its actual order/node occurrence");
                    MonetaryBudgetStatus actual = MonetaryBudgetExecution.CheckReceipts(_world, _world.Services.Get<IMonetaryBudgets>(), order.Owner, new[] { receipt });
                    if (binding.Spent ? actual != MonetaryBudgetStatus.InvalidReceipt : actual != MonetaryBudgetStatus.Ready)
                        throw new CorruptStateException("saved work budget differs from its live/spent account receipt");
                }
            }
            if (found.Count != node.Costs.Count) throw new CorruptStateException("saved monetary obligations are duplicated or outside the selected workflow");
        }
        /// <summary>Conservative authored target-capacity projection, not branch selection or live stock.</summary>
        internal static Bill SupplyCapacity(IActivityGraph graph, IResourceCatalog content)
        {
            var lines = new List<ResourceAmount>();
            foreach (IActivityNodeDefinition node in graph.Nodes)
                lines.AddRange(SupplyCapacity(node.Requirement, node.RequirementMultiplier, content).Lines);
            return new Bill(lines);
        }
        internal static Bill SupplyCapacity(IWorkRequirement root, Fix multiplier, IResourceCatalog content)
        {
            if (root == null) return Bill.Empty;
            var stack = new Stack<(IWorkRequirement Requirement, bool Visited)>();
            var bills = new Dictionary<IWorkRequirement, Bill>(); var active = new HashSet<IWorkRequirement>();
            stack.Push((root, false));
            while (stack.Count > 0)
            {
                var frame = stack.Pop(); if (bills.ContainsKey(frame.Requirement)) continue;
                if (!frame.Visited)
                {
                    if (!active.Add(frame.Requirement)) throw new ContentException(root.Id + ": cyclic supply requirement");
                    stack.Push((frame.Requirement, true));
                    if (frame.Requirement is IWorkRequirementGroup children)
                        for (int i = children.Entries.Count - 1; i >= 0; i--) stack.Push((children.Entries[i].Requirement, false));
                    continue;
                }
                active.Remove(frame.Requirement); var lines = new List<ResourceAmount>();
                if (frame.Requirement is IWorkSupplyRequirement supply && supply.Port.IsNone) lines.AddRange(ScaledBill(supply, multiplier, content).Lines);
                if (frame.Requirement is IWorkRequirementGroup group)
                {
                    if (group.Mode == WorkRequirementMode.All)
                        foreach (var entry in group.Entries) lines.AddRange(bills[entry.Requirement].Lines);
                    else
                    {
                        var maximum = new Dictionary<ResourceId, Fix>();
                        foreach (var entry in group.Entries) foreach (var line in bills[entry.Requirement].Lines)
                            maximum[line.Resource] = maximum.TryGetValue(line.Resource, out var old) ? Fix.Max(old, line.Amount) : line.Amount;
                        foreach (var pair in maximum) lines.Add(new ResourceAmount(pair.Key, pair.Value));
                    }
                }
                bills.Add(frame.Requirement, new Bill(lines));
            }
            return bills[root];
        }
        internal void CopySupply(WorkRequirementResult result, ActivityNodeExecution node, Entity target, IList<LogisticsSupplyRequest> destination)
        {
            foreach (var choice in result.Resources)
            {
                if (!WorkResourceUses.Running(choice.Use) && Binding(node, choice.Path) != null ||
                    !(_world.Content.Find<IWorkRequirement>(choice.Requirement) is IWorkSupplyRequirement supply)) continue;
                IResourceOutlet store = supply.Port.IsNone ? target.Get<IOutletPort>() :
                    target.Get<IResourcePort>(supply.Port) is IResourcePort port
                        ? (IResourceOutlet)ResourcePortContracts.Store(_world, target, port, choice.Endpoint).Store : null;
                if (store?.Endpoint != choice.Endpoint) continue; // Never turn remote/custom offers into implicit target deliveries.
                var lines = new List<ResourceAmount>();
                if (WorkResourceUses.Running(choice.Use))
                {
                    foreach (var candidate in result.ResourceCandidates)
                        if (candidate.Choice.At(choice.Path) && candidate.Choice.Endpoint == choice.Endpoint)
                        { foreach (var line in candidate.Availability) if (line.Required.IsPositive) lines.Add(new ResourceAmount(line.Resource, line.Required)); break; }
                }
                else lines.AddRange(choice.Quantities.Lines);
                if (lines.Count == 0) continue;
                foreach (var line in lines)
                {
                    Content.IResourceLotSelectionDefinition selected = null;
                    foreach (var selection in choice.Selections) if (selection.Resource == line.Resource) selected = selection;
                    var selections = selected == null ? Array.Empty<Content.IResourceLotSelectionDefinition>() : new[] { selected };
                    bool merged = false;
                    for (int i = 0; i < destination.Count; i++)
                        if (destination[i].Endpoint == choice.Endpoint && destination[i].Quantities.Count == 1 &&
                            destination[i].Quantities[0].Resource == line.Resource && destination[i].SelectionFor(line.Resource)?.Id == selected?.Id)
                        {
                            var combined = new Bill(new[] { new ResourceAmount(line.Resource, destination[i].Quantities[0].Amount + line.Amount) });
                            destination[i] = new LogisticsSupplyRequest(supply.Port, choice.Endpoint, combined, selections); merged = true; break;
                        }
                    if (!merged) destination.Add(new LogisticsSupplyRequest(supply.Port, choice.Endpoint, new Bill(new[] { line }), selections));
                }
            }
        }
        private static WorkCostBinding CostBinding(ActivityNodeExecution node, IReadOnlyList<string> path)
        { if (node != null) foreach (var binding in node.Costs) if (binding.Choice.At(path)) return binding; return null; }
        private WorkRequirementResult EvaluateCost(IWorkCostRequirement requirement, IReadOnlyList<string> path,
            WorkRequirementContext context, WorkRequirementResult intrinsic)
        {
            var choice = CostBinding(context.Execution, path)?.Choice ?? new WorkCostChoice(path, requirement.Id,
                BusinessModeRules.EvaluateCost(_world, requirement.CostCategory, ScaleAmount(requirement.Amount, context.Multiplier, requirement.Id)), requirement.Credit);
            var result = intrinsic.WithCosts(Array.AsReadOnly(new[] { choice }));
            return CheckCostSet(result, context);
        }
        private WorkRequirementResult CheckCostSet(WorkRequirementResult result, WorkRequirementContext context)
        {
            WorkRequirementResult check = CheckCostSet(new[] { result }, new[] { context });
            return check.Status == WorkRequirementStatus.Satisfied ? result
                : check.WithResources(result.Resources, result.ResourceCandidates).WithCosts(result.Costs);
        }
        // Same cost admission for one node or its selected parallel frontier. No payment.
        private WorkRequirementResult CheckCostSet(IReadOnlyList<WorkRequirementResult> results, IReadOnlyList<WorkRequirementContext> contexts)
        {
            if (results == null || contexts == null || results.Count != contexts.Count)
                throw new ArgumentException("cost admission requires aligned actual results and contexts");
            if (contexts.Count == 0) return WorkRequirementResult.Satisfied();
            PlayerId company = contexts[0].Owner;
            BigInteger ordinary = BigInteger.Zero;
            var held = new List<MonetaryBudgetReceipt>();
            for (int i = 0; i < contexts.Count; i++)
            {
                var context = contexts[i];
                if (context.Owner != company || !ReferenceEquals(context.World, _world))
                    throw new ArgumentException("cost admission requires actual same-company session contexts");
                foreach (var choice in results[i].Costs)
                {
                    if (choice.Applicability == MonetaryCostApplicability.UnavailableCapability)
                        return WorkRequirementResult.Blocked(BusinessModeRules.UnavailableCostCapability);
                    WorkCostBinding binding = CostBinding(context.Execution, choice.Path);
                    if (choice.Applicability == MonetaryCostApplicability.Enabled && !choice.Credit && binding?.Spent != true)
                    { if (binding?.Receipt != null) held.Add(binding.Receipt.Value); else ordinary += choice.Amount.Raw; }
                }
            }
            if (held.Count > 0)
            {
                MonetaryBudgetStatus status = MonetaryBudgetExecution.CheckSpend(_world, _world.Services.Get<IMonetaryBudgets>(), company, held.AsReadOnly());
                if (status != MonetaryBudgetStatus.Ready)
                    return WorkRequirementResult.Pending(MonetaryBudgetExecution.Blocker(status));
            }
            if (ordinary > long.MaxValue) return WorkRequirementResult.Pending(MonetaryBudgetExecution.AdmissionRefused);
            if (ordinary > BigInteger.Zero && !BusinessModeRules.CanPay(_world.Services.Get<IEconomy>(), company, Fix.FromRaw((long)ordinary)))
                return WorkRequirementResult.Pending(WorkSystem.BlockerCash);
            return WorkRequirementResult.Satisfied();
        }
        // One composition of the actual native admission owners. Detached proposals only;
        // actual job acquisition still rechecks and commits through its original owners.
        internal bool CheckWorkSet(IReadOnlyList<WorkRequirementResult> results, IReadOnlyList<WorkRequirementContext> contexts,
            out IReadOnlyList<WorkRequirementResult> selected, out ResourceClaimCheck resources,
            out Code dockBlocker, out IReadOnlyList<IncidentPreparednessDock> docks, out Code costBlocker)
        {
            dockBlocker = CheckDockSet(contexts, out docks);
            resources = CheckResourceSet(results, contexts, out selected);
            costBlocker = CheckCostSet(selected, contexts).Blocker;
            if (dockBlocker.IsNone && resources.Ready && costBlocker.IsNone) return true;
            if (TryAlternativeWorkSet(results, contexts, out var alternatives, out var material, out var bays))
            {
                selected = alternatives; resources = material; docks = bays;
                dockBlocker = Code.None; costBlocker = Code.None; return true;
            }
            return false;
        }
// The same complete-set admission owner also selects authored role alternatives.
        // Candidates are pure proposals for already DISTINCT matched actors, never claims.
        internal bool CheckWorkSet(IReadOnlyList<WorkRequirementResult> results, IReadOnlyList<WorkRequirementContext> contexts,
            IReadOnlyList<IReadOnlyList<WorkWorkerCandidate>> alternatives,
            out IReadOnlyList<WorkWorkerCandidate?> chosen, out IReadOnlyList<WorkRequirementResult> selected,
            out ResourceClaimCheck resources, out Code dockBlocker, out IReadOnlyList<IncidentPreparednessDock> docks,
            out Code costBlocker)
        {
            if (results == null || contexts == null || alternatives == null || results.Count != contexts.Count ||
                results.Count != alternatives.Count) throw new ArgumentException("role admission requires aligned native rows");
            var positions = new int[contexts.Count];
            for (int i = 0; i < contexts.Count; i++)
            {
                var context = contexts[i]; var options = alternatives[i];
                if (!ReferenceEquals(context.World, _world) || options == null ||
                    (context.Worker == null ? options.Count != 0 : options.Count == 0))
                    throw new ArgumentException("role admission requires actual worker alternatives or an explicit timed row");
                var roles = new HashSet<WorkerRoleId>();
                foreach (var option in options)
                {
                    bool declared = false;
                    foreach (var role in context.Definition.Roles) if (option.Role == role) { declared = true; break; }
                    if (option.Worker != context.Worker.Id || !declared || !roles.Add(option.Role) ||
                        option.Result.Status != WorkRequirementStatus.Satisfied)
                        throw new ArgumentException("role alternatives must name distinct supported roles of the actual matched actor");
                }
            }
            chosen = Array.Empty<WorkWorkerCandidate?>(); selected = results; resources = default;
            dockBlocker = Code.None; docks = Array.Empty<IncidentPreparednessDock>(); costBlocker = Code.None;
            bool first = true;
            while (true)
            {
                var current = new List<WorkWorkerCandidate?>(); var inputs = new List<WorkRequirementResult>();
                var actual = new List<WorkRequirementContext>();
                for (int i = 0; i < contexts.Count; i++)
                {
                    var context = contexts[i];
                    if (context.Worker == null)
                    { current.Add(null); inputs.Add(results[i]); actual.Add(context); }
                    else
                    {
                        var option = alternatives[i][positions[i]];
                        current.Add(option); inputs.Add(option.Result);
                        actual.Add(new WorkRequirementContext(_world, context.Owner, context.Target, context.Definition,
                            context.Worker, context.Choices, context.Execution, option.Role));
                    }
                }
                bool ready = CheckWorkSet(inputs.AsReadOnly(), actual.AsReadOnly(), out var picked, out var material,
                    out var dock, out var bays, out var cost);
                if (first || ready)
                {
                    chosen = current.AsReadOnly(); selected = picked; resources = material;
                    dockBlocker = dock; docks = bays; costBlocker = cost; first = false;
                }
                if (ready) return true;
                // Canonical iterative Cartesian search. There is no recursion, persisted cursor,
                // arbitrary combination cap or speculative resource/worker reservation.
                int next = contexts.Count - 1;
                while (next >= 0)
                {
                    positions[next]++;
                    if (positions[next] < alternatives[next].Count) break;
                    positions[next] = 0; next--;
                }
                if (next < 0) return false;
            }
        }

        private void BindCosts(ActivityNodeExecution node, WorkRequirementResult result, IReadOnlyList<MonetaryBudgetReceipt> receipts)
        {
            var costs = new List<WorkCostBinding>(node.Costs);
            PlayerId company = Order(node.OrderId).Owner;
            int index = 0;
            foreach (var choice in result.Costs) if (choice.Applicability == MonetaryCostApplicability.Enabled && CostBinding(node, choice.Path) == null)
            {
                MonetaryBudgetReceipt? receipt = choice.Credit ? (MonetaryBudgetReceipt?)null : receipts[index++];
                if (receipt.HasValue && (receipt.Value.Company != company || !receipt.Value.Owner.Equals(choice.BudgetOwner(node.OrderId, node.Id))))
                    throw new InvalidOperationException("account provider returned another work budget owner");
                costs.Add(new WorkCostBinding(choice, false, receipt));
            }
            if (index != receipts.Count) throw new InvalidOperationException("account provider returned an extra work budget receipt");
            node.Costs = costs.AsReadOnly();
        }
        private static List<MonetaryBudgetRequest> BudgetRequests(ActivityNodeExecution node, WorkRequirementResult result)
        {
            var requests = new List<MonetaryBudgetRequest>();
            foreach (var choice in result.Costs)
                if (choice.Applicability == MonetaryCostApplicability.Enabled && !choice.Credit && CostBinding(node, choice.Path) == null)
                    requests.Add(new MonetaryBudgetRequest(choice.BudgetOwner(node.OrderId, node.Id), choice.Amount));
            return requests;
        }
        private void ValidateOwnedBudgets()
        {
            if (!_world.Services.TryGet<IMonetaryBudgets>(out var budgets)) return;
            foreach (var receipt in MonetaryBudgetExecution.Inspect(_world, budgets))
            {
                if (!receipt.Owner.Scope.StartsWith("sim.work.budgets.", StringComparison.Ordinal)) continue;
                Order order = receipt.Owner.Identity <= int.MaxValue ? Order((int)receipt.Owner.Identity) : null;
                int references = 0;
                if (order != null && order.Owner == receipt.Company) foreach (var node in order.Nodes) foreach (var binding in node.Costs)
                    if (!binding.Spent && binding.Receipt.HasValue && binding.Receipt.Value.Equals(receipt) &&
                        receipt.Owner.Equals(binding.Choice.BudgetOwner(order.Id, node.Id))) references++;
                if (references != 1) throw new CorruptStateException("orphan or mismatched account-owned work budget");
            }
        }
        private sealed class ProductiveAdmission
        {
            public Order Order;
            public ActivityNodeExecution Node;
            public IActivityNodeDefinition Definition;
            public Entity Worker;
            public WorkProgress Next;
            public long ProductiveNext;
            public Code Cause;
            public Fix Credit;
            public readonly List<ResourceReservation> Receipts = new List<ResourceReservation>();
            public readonly List<ResourceClaimIntent> Running = new List<ResourceClaimIntent>();
            public readonly List<MonetaryBudgetReceipt> Budgets = new List<MonetaryBudgetReceipt>();
        }
        // The same owner admits one actual machine quantum or a complete actual productive frontier.
        // Caller supplies identities only, never retained rates, definitions, receipts or causes.
        internal bool AdvanceWork(IReadOnlyList<(int Order, string Node)> intents, PlayerId company, out Code blocker)
        {
            if (intents == null || intents.Count == 0) throw new ArgumentException("productive work needs a nonempty actual set");
            blocker = Codes.InvalidArgument;
            var ids = new List<(int Order, string Node)>(intents);
            ids.Sort((a, b) => { int c = a.Order.CompareTo(b.Order); return c != 0 ? c : string.CompareOrdinal(a.Node, b.Node); });
            var seen = new HashSet<(int, string)>(); var prepared = new List<ProductiveAdmission>();
            // Complete shape and actual ownership before any extension callbacks.
            foreach (var intent in ids)
            {
                Order order = Order(intent.Order);
                ActivityNodeExecution node = intent.Node == null ? null : order?.Node(intent.Node);
                IActivityNodeDefinition definition = intent.Node == null ? null : order?.Plan.Graph.Find(intent.Node);
                Entity target = _world.Entities.Get(order?.Target ?? EntityId.None);
                if (!seen.Add(intent) || order?.IsActive != true || order.Owner != company || target?.Alive != true ||
                    target.Owner != company || node == null || definition == null || definition.Duration.IsZero ||
                    node.State != NodeState.Working && node.State != NodeState.Paused) return false;
                Entity worker = null;
                if (ActivityExecutorContracts.RequiresWorker(definition))
                {
                    Job job = Job(node.JobId); worker = _world.Entities.Get(job?.Worker ?? EntityId.None);
                    if (job == null || job.IsHaul || job.OrderId != order.Id || job.NodeId != node.Id ||
                        job.State != JobState.Assigned && job.State != JobState.Paused || worker?.Alive != true ||
                        worker.Owner != company || worker.Get<IWorker>()?.Job != job.Id || job.ToilIndex < 0 ||
                        job.ToilIndex >= job.Toils.Count ||
                        _world.Services.Get<IWorkPhaseCatalog>().Purpose(job.Toils[job.ToilIndex].Executor) != WorkPhasePurpose.Productive)
                        return false;
                }
                else if (node.JobId != 0) return false;
                prepared.Add(new ProductiveAdmission { Order = order, Node = node, Definition = definition, Worker = worker,
                    Cause = new Code(order.PlanKey + "." + node.Id) });
            }
            var transaction = _world.Services.Get<IResourceTransaction>();
            var running = new List<ResourceClaimIntent>(); var retained = new List<ResourceReservation>();
            var budgets = new List<MonetaryBudgetReceipt>();
            foreach (var entry in prepared)
            {
                Order order = entry.Order; ActivityNodeExecution node = entry.Node; var definition = entry.Definition;
                blocker = PlanOrderBlocker(order.Id);
                if (blocker.IsNone) blocker = ResponseWork.CheckActiveOrder(_world, _world.Entities.Get(order.Target), order);
                if (!blocker.IsNone) return false;
                if (!WorkSystem.DependenciesDone(order, definition)) { blocker = WorkSystem.BlockerDependencies; return false; }
                if (entry.Worker != null)
                {
                    blocker = _world.Services.Get<IWorkerDispatch>().CheckJob(_world, entry.Worker, Job(node.JobId));
                    if (!blocker.IsNone) return false;
                }
                var readiness = EvaluateRequirements(company, _world.Entities.Get(order.Target), definition, node, null);
                if (readiness.Status != WorkRequirementStatus.Satisfied) { blocker = readiness.Blocker; return false; }
                foreach (var choice in readiness.Resources) if (Binding(node, choice.Path) == null)
                { blocker = Codes.InvalidArgument; return false; }
                foreach (var choice in readiness.Costs)
                    if (choice.Applicability == MonetaryCostApplicability.Enabled && CostBinding(node, choice.Path) == null)
                    { blocker = Codes.InvalidArgument; return false; }
                Fix rate = ExecutorRate(order, definition, entry.Worker);
                if (!rate.IsPositive) { blocker = WorkSystem.BlockerWorkerUnavailable; return false; }
                entry.Next = node.Progress.Advance(rate, _world.Time.Ticks(definition.Duration));
                bool serviceDuty = RetainProductiveWork(order, definition);
                entry.ProductiveNext = entry.Next.Equals(node.Progress) && !serviceDuty ? node.ProductiveSteps : checked(node.ProductiveSteps + 1);
                if (entry.ProductiveNext == node.ProductiveSteps) continue;
                foreach (var binding in node.Resources)
                {
                    if (!binding.Spent) retained.AddRange(binding.Receipts);
                    if (!binding.Spent && binding.Choice.Use == WorkResourceUse.Consume) entry.Receipts.AddRange(binding.Receipts);
                    if (!WorkResourceUses.Running(binding.Choice.Use)) continue;
                    Bill delta = RunningDelta(binding.Choice, node.Progress, entry.Next, _world.Time.Ticks(definition.Duration),
                        node.ProductiveSteps, entry.ProductiveNext);
                    if (!delta.IsEmpty) entry.Running.Add(new ResourceClaimIntent(ReservationDirection.Outbound,
                        binding.Choice.Owner(node.OrderId, node.Id), binding.Choice.Endpoint, delta, DemandSelections(binding.Choice, delta)));
                }
                foreach (var binding in node.Costs) if (!binding.Spent)
                { if (binding.Choice.Credit) entry.Credit += binding.Choice.Amount; else entry.Budgets.Add(binding.Receipt.Value); }
                running.AddRange(entry.Running); budgets.AddRange(entry.Budgets);
            }
            if (!CheckProductiveCommitments(transaction, company, running, retained, budgets, out blocker)) return false;
            IReadOnlyList<ResourceReservation> acquired = Array.Empty<ResourceReservation>();
            if (running.Count > 0)
            {
                ResourceClaimResult result = transaction.AcquireClaims(company, running, retained);
                if (!result.Committed) throw new InvalidOperationException("complete productive resource acquisition changed after admission");
                acquired = result.Receipts;
            }
            // Actual transaction receipts must map exactly to native obligations before any progress binds.
            var used = new HashSet<ReservationToken>();
            foreach (var entry in prepared)
            {
                foreach (var request in entry.Running) foreach (var line in request.Quantities.Lines)
                {
                    int matches = 0;
                    foreach (var receipt in acquired)
                        if (receipt.Owner.Equals(request.Owner) && receipt.Token.Endpoint == request.Endpoint && receipt.Resource == line.Resource)
                        {
                            if (receipt.Direction != ReservationDirection.Outbound || receipt.Amount != line.Amount || !used.Add(receipt.Token))
                                throw new InvalidOperationException("productive acquisition returned mismatched or duplicate actual receipt");
                            matches++; entry.Receipts.Add(receipt);
                        }
                    if (matches != 1) throw new InvalidOperationException("productive acquisition omitted an actual running obligation");
                }
            }
            if (used.Count != acquired.Count) throw new InvalidOperationException("productive acquisition returned extra receipts");
            var allRetained = new List<ResourceReservation>(retained); allRetained.AddRange(acquired);
            if (!CheckProductiveCommitments(transaction, company, Array.Empty<ResourceClaimIntent>(), allRetained, budgets, out _))
                throw new InvalidOperationException("productive acquisition changed complete retained resource or budget commitments");
            foreach (var entry in prepared)
            {
                if (entry.ProductiveNext == entry.Node.ProductiveSteps) continue;
                if (entry.Budgets.Count > 0) MonetaryBudgetExecution.Spend(_world, _world.Services.Get<IMonetaryBudgets>(),
                    company, entry.Budgets.AsReadOnly(), entry.Cause);
                if (entry.Credit.IsPositive) _world.Services.Get<IEconomy>().Charge(company, entry.Credit, entry.Cause);
                if (entry.Receipts.Count > 0)
                {
                    if (!transaction.ConsumeClaims(company, entry.Receipts, entry.Cause, allRetained).Committed)
                        throw new InvalidOperationException("resource provider violated complete productive consumption preflight");
                    var consumed = new HashSet<ReservationToken>(); foreach (var receipt in entry.Receipts) consumed.Add(receipt.Token);
                    allRetained.RemoveAll(receipt => consumed.Contains(receipt.Token));
                }
            }
            // No node gets earned progress/spent flags until all exact mutations verify.
            foreach (var entry in prepared)
            {
                var node = entry.Node; if (entry.ProductiveNext == node.ProductiveSteps) continue;
                var resources = new List<WorkResourceBinding>();
                foreach (var binding in node.Resources) resources.Add(!binding.Spent && binding.Choice.Use == WorkResourceUse.Consume
                    ? new WorkResourceBinding(binding.Choice, binding.Receipts, true) : binding);
                node.Resources = resources.AsReadOnly();
                var costs = new List<WorkCostBinding>();
                foreach (var binding in node.Costs) costs.Add(binding.Spent ? binding : new WorkCostBinding(binding.Choice, true, binding.Receipt));
                node.Costs = costs.AsReadOnly(); node.Progress = entry.Next; node.ProductiveSteps = entry.ProductiveNext;
            }
            blocker = Code.None; return true;
        }
        private bool CheckProductiveCommitments(IResourceTransaction transaction, PlayerId company,
            IReadOnlyList<ResourceClaimIntent> running, IReadOnlyList<ResourceReservation> retained,
            IReadOnlyList<MonetaryBudgetReceipt> budgets, out Code blocker)
        {
            blocker = Code.None;
            if (running.Count > 0)
            {
                var check = transaction.EvaluateClaims(company, running, retained);
                if (!check.Ready) { blocker = ResourceRefusal(check.Status).Blocker; return false; }
            }
            if (retained.Count > 0)
            {
                var check = transaction.EvaluateClaimReceipts(company, retained);
                if (!check.Ready) { blocker = ResourceRefusal(check.Status).Blocker; return false; }
            }
            if (budgets.Count > 0)
            {
                var status = MonetaryBudgetExecution.CheckSpend(_world, _world.Services.Get<IMonetaryBudgets>(), company, budgets);
                if (status != MonetaryBudgetStatus.Ready) { blocker = MonetaryBudgetExecution.Blocker(status); return false; }
            }
            return true;
        }
        private void ReleaseCosts(ActivityNodeExecution node, PlayerId company)
        {
            var spent = new List<WorkCostBinding>(); var receipts = new List<MonetaryBudgetReceipt>();
            foreach (var binding in node.Costs) { if (binding.Spent) spent.Add(binding); else if (binding.Receipt.HasValue) receipts.Add(binding.Receipt.Value); }
            if (receipts.Count > 0) MonetaryBudgetExecution.Release(_world, _world.Services.Get<IMonetaryBudgets>(), company, receipts.AsReadOnly());
            node.Costs = spent.AsReadOnly();
        }
        private void SyncCosts(IStateSync sync, ActivityNodeExecution node)
        {
            int count = sync.Count(node.Costs.Count); var costs = sync.Reading ? new List<WorkCostBinding>() : null;
            for (int i = 0; i < count; i++)
            {
                WorkCostBinding binding = sync.Reading ? null : node.Costs[i]; var choice = binding?.Choice;
                int segments = sync.Count(choice?.Path.Count ?? 0); var path = sync.Reading ? new List<string>() : null;
                for (int j = 0; j < segments; j++) { string part = sync.Reading ? null : choice.Path[j]; sync.String(ref part); path?.Add(part); }
                string requirement = choice?.Requirement; Fix amount = choice?.Amount ?? Fix.Zero;
                bool credit = choice?.Credit ?? false, spent = binding?.Spent ?? false;
                MonetaryCostCategory category = choice?.Category ?? MonetaryCostCategory.Unavailable;
                sync.String(ref requirement); sync.Fix(ref amount); sync.Bool(ref credit); sync.Bool(ref spent);
                sync.Enum(ref category);
                bool hasReceipt = binding?.Receipt.HasValue ?? false; sync.Bool(ref hasReceipt);
                MonetaryBudgetReceipt? budget = null;
                if (hasReceipt)
                {
                    MonetaryBudgetReceipt receipt = binding?.Receipt ?? default;
                    long id = receipt.Id, identity = receipt.Owner.Identity; PlayerId company = receipt.Company;
                    string scope = receipt.Owner.Scope; Fix held = receipt.Amount;
                    sync.Long(ref id); sync.Player(ref company); sync.String(ref scope); sync.Long(ref identity); sync.Fix(ref held);
                    budget = new MonetaryBudgetReceipt(id, company, new ReservationOwner(scope, identity), held);
                }
                if (sync.Reading)
                {
                    try { costs.Add(new WorkCostBinding(new WorkCostChoice(path, requirement,
                        new MonetaryCostDecision(category, amount, MonetaryCostApplicability.Enabled), credit), spent, budget)); }
                    catch (ArgumentException) { throw new CorruptStateException("invalid saved work cost identity/amount"); }
                }
            }
            if (sync.Reading) node.Costs = costs.AsReadOnly();
        }
    }
}
