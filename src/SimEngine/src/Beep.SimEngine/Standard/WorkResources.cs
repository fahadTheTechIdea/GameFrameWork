using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Same WorkBoard owner/evaluator/codec. No service, selector state or second reservation ledger.
    public sealed partial class WorkBoard
    {
        internal static bool HasSpent(ActivityNodeExecution execution)
        {
            if (execution == null) return false;
            if (execution.Progress.WholeTicks > 0 || execution.Progress.Fraction > 0) return true;
            foreach (var binding in execution.Resources) if (binding.Spent) return true;
            foreach (var binding in execution.Costs) if (binding.Spent) return true;
            return false;
        }
        private static bool HasResourceRequirement(IWorkRequirement root)
            => HasRequirement<IResourceWorkRequirement>(root);
        private void ValidateResourceDefinition(IResourceWorkRequirement resource)
        {
            if (resource.Quantities == null || resource.Quantities.IsEmpty || resource.Use > WorkResourceUse.Elapsed)
                throw new ContentException(resource.Id + ": resource requirement needs an explicit bill/use");
            try { WorkResourceChoice.CopyRates(resource.Rates, resource.Quantities, resource.Use); }
            catch (ArgumentException error) { throw new ContentException(resource.Id + ": " + error.Message); }
            if (resource.Selections == null) throw new ContentException(resource.Id + ": resource selections must be explicit, possibly empty");
            var selectedResources = new HashSet<ResourceId>();
            foreach (var selection in resource.Selections)
                if (selection == null || !_world.Content.Owns(selection) || !ReferenceEquals(_world.Content.Find<IResourceLotSelectionDefinition>(selection.Id), selection) ||
                    !resource.Quantities.AmountOf(selection.Resource).IsPositive || !selectedResources.Add(selection.Resource))
                    throw new ContentException(resource.Id + ": selections require unique actual resource aliases in the bill");
            foreach (var line in resource.Quantities.Lines)
            {
                IResourceContentDefinition definition = _world.Content.Resource(line.Resource);
                if (definition == null || UnitConversion.ValidateAmount(definition.QuantityUnit, line.Amount) != UnitConversionStatus.Converted)
                    throw new ContentException(resource.Id + ": illegal native requirement amount");
            }
        }
        private static WorkResourceBinding Binding(ActivityNodeExecution node, IReadOnlyList<string> path)
        {
            if (node != null) foreach (var binding in node.Resources) if (binding.Choice.At(path)) return binding;
            return null;
        }
        private static WorkRequirementResult ResourceRefusal(ResourceTransferStatus status)
        {
            switch (status)
            {
                case ResourceTransferStatus.Ready: return WorkRequirementResult.Satisfied();
                case ResourceTransferStatus.NoStock: return WorkRequirementResult.Pending(WorkResourceCodes.Stock);
                case ResourceTransferStatus.MissingEndpoint: return WorkRequirementResult.Pending(WorkResourceCodes.MissingSource);
                case ResourceTransferStatus.Incompatible: return WorkRequirementResult.Blocked(WorkResourceCodes.Incompatible);
                case ResourceTransferStatus.InvalidRequest: return WorkRequirementResult.Blocked(Codes.NotOwned);
                case ResourceTransferStatus.InvalidReceipt: return WorkRequirementResult.Pending(WorkResourceCodes.Lost);
                default: throw new InvalidOperationException("unexpected work resource preflight status: " + status);
            }
        }
        private WorkRequirementResult EvaluateResource(IResourceWorkRequirement requirement, IReadOnlyList<string> path,
            WorkRequirementContext context, WorkRequirementResult intrinsic)
        {
            WorkResourceBinding bound = Binding(context.Execution, path);
            if (bound?.Spent == true) return intrinsic.WithResources(Array.AsReadOnly(new[] { bound.Choice }));
            var sources = new List<WorkResourceSource>(); Code sourceBlocker = requirement.CopySources(context, sources);
            if (!sourceBlocker.IsNone)
            {
                if (sources.Count != 0) throw new InvalidOperationException(requirement.Id + ": unavailable resource source published candidates");
                return WorkRequirementResult.Pending(sourceBlocker)
                    .WithResources(bound == null ? null : Array.AsReadOnly(new[] { bound.Choice }));
            }
            var identities = new HashSet<ResourceEndpointId>();
            foreach (var source in sources)
                if (!source.Endpoint.IsValid || source.Cost < 0 || !identities.Add(source.Endpoint))
                    throw new InvalidOperationException(requirement.Id + ": invalid/duplicate resource source identity or cost");
            sources.Sort((a, b) =>
            {
                int order = a.Cost.CompareTo(b.Cost); if (order != 0) return order;
                order = string.CompareOrdinal(a.Endpoint.Scope, b.Endpoint.Scope);
                return order != 0 ? order : a.Endpoint.Identity.CompareTo(b.Endpoint.Identity);
            });
            var offers = new List<WorkResourceCandidate>(); WorkResourceChoice selected = null;
            WorkRequirementResult? invalid = null;
            IResourceTransaction transaction = _world.Services.Get<IResourceTransaction>();
            foreach (var source in sources)
            {
                // A committed occurrence has one actual source. Alternatives are not live authority.
                if (bound != null && source.Endpoint != bound.Choice.Endpoint) continue;
                Bill quantities = ScaledBill(requirement, context.Multiplier, _world.Content);
                var choice = new WorkResourceChoice(path, requirement.Id, source.Endpoint, quantities, requirement.Use, ScaledRates(requirement, quantities), requirement.Selections);
                var owner = choice.Owner(context.Execution?.OrderId ?? 1, context.Node);
                ResourceClaimCheck stock = transaction.EvaluateClaims(context.Owner, new[] { new ResourceClaimIntent(ReservationDirection.Outbound, 
                    new ReservationOwner(owner.Scope + ".inspect", owner.Identity), source.Endpoint, ResourceDemand(choice, context), DemandSelections(choice, ResourceDemand(choice, context))) }, System.Array.Empty<ResourceReservation>());
                if (stock.Status == ResourceTransferStatus.InvalidRequest) continue; // Never disclose private foreign endpoints, even blocked ones.
                WorkRequirementResult check = requirement.CheckSource(context, source.Endpoint);
                if (!check.IsWellFormed || check.SelectedEntry != null || check.Choices.Count != 0 || check.Resources.Count != 0 || check.ResourceCandidates.Count != 0 || check.Costs.Count != 0)
                    throw new InvalidOperationException(requirement.Id + ": malformed source check");
                IReadOnlyList<ResourceClaimAvailability> observations = stock.Availability;
                if (check.Status == WorkRequirementStatus.Invalid && !invalid.HasValue) invalid = check;
                if (check.Status == WorkRequirementStatus.Satisfied)
                {
                    if (bound != null)
                        check = bound.Choice.Endpoint != source.Endpoint ? WorkRequirementResult.Pending(WorkResourceCodes.Lost)
                            : WorkResourceUses.Running(requirement.Use) ? ResourceRefusal(stock.Status)
                            : ResourceRefusal(transaction.EvaluateClaimReceipts(context.Owner, bound.Receipts).Status);
                    else check = ResourceRefusal(stock.Status);
                }
                offers.Add(new WorkResourceCandidate(choice, source, check, observations));
                if (selected == null && check.Status == WorkRequirementStatus.Satisfied) selected = bound?.Choice ?? choice;
            }
            if (invalid.HasValue) return invalid.Value.WithResources(null, offers.AsReadOnly());
            if (selected != null) return intrinsic.WithResources(Array.AsReadOnly(new[] { selected }), offers.AsReadOnly());
            WorkRequirementResult refusal = offers.Count == 0 ? WorkRequirementResult.Pending(WorkResourceCodes.MissingSource) : offers[0].Result;
            WorkResourceChoice intent = offers.Count == 0 ? null : offers[0].Choice;
            if (bound != null)
            {
                // Source omission is unavailable access, not proof that the original receipt was lost.
                intent = bound.Choice; refusal = WorkRequirementResult.Pending(WorkResourceCodes.MissingSource);
                foreach (var offer in offers)
                    if (offer.Choice.Endpoint == bound.Choice.Endpoint)
                    { refusal = offer.Result; break; }
            }
            return refusal.WithResources(intent == null ? null : Array.AsReadOnly(new[] { intent }), offers.AsReadOnly());
        }
        private List<ResourceClaimIntent> ResourceIntents(WorkRequirementResult result, WorkRequirementContext context, bool assignment = false)
        {
            var intents = new List<ResourceClaimIntent>();
            foreach (var choice in result.Resources)
                if (WorkResourceUses.Running(choice.Use) ? !assignment : Binding(context.Execution, choice.Path) == null)
                {
                    Bill quantities = ResourceDemand(choice, context); if (quantities.IsEmpty) continue;
                    ReservationOwner owner = choice.Owner(context.Execution?.OrderId ?? 1, context.Node);
                    if (context.Execution == null) owner = new ReservationOwner(owner.Scope + ".preview", owner.Identity);
                    intents.Add(new ResourceClaimIntent(ReservationDirection.Outbound, owner, choice.Endpoint, quantities, DemandSelections(choice, quantities)));
                }
            return intents;
        }
        private static IReadOnlyList<IResourceLotSelectionDefinition> DemandSelections(WorkResourceChoice choice, Bill demand)
        {
            var result = new List<IResourceLotSelectionDefinition>();
            foreach (var selection in choice.Selections) if (demand.AmountOf(selection.Resource).IsPositive) result.Add(selection);
            return result.AsReadOnly();
        }
        private static bool SelectionsEqual(IReadOnlyList<IResourceLotSelectionDefinition> left, IReadOnlyList<IResourceLotSelectionDefinition> right)
        {
            if (left.Count != right.Count) return false;
            foreach (var selection in left)
            {
                bool found = false; foreach (var other in right) if (ReferenceEquals(selection, other)) { found = true; break; }
                if (!found) return false;
            }
            return true;
        }
        private WorkRequirementResult CheckResourceSet(WorkRequirementResult result, WorkRequirementContext context)
        {
            ResourceClaimCheck check = CheckResourceSet(new[] { result }, new[] { context }, out var selected);
            return check.Ready ? selected[0]
                : ResourceRefusal(check.Status).WithResources(result.Resources, result.ResourceCandidates).WithCosts(result.Costs);
        }
        // The same pure source search for one node or a covered parallel frontier. No acquisition.
        private ResourceClaimCheck CheckResourceSet(IReadOnlyList<WorkRequirementResult> results,
            IReadOnlyList<WorkRequirementContext> contexts, out IReadOnlyList<WorkRequirementResult> selected)
        {
            if (results == null || contexts == null || results.Count != contexts.Count)
                throw new ArgumentException("resource selection requires aligned actual results and contexts");
            selected = new List<WorkRequirementResult>(results).AsReadOnly();
            if (contexts.Count == 0) return new ResourceClaimCheck(ResourceTransferStatus.Ready);
            PlayerId company = contexts[0].Owner;
            var intents = new List<ResourceClaimIntent>();
            for (int i = 0; i < results.Count; i++)
            {
                if (contexts[i].Owner != company || !ReferenceEquals(contexts[i].World, _world))
                    throw new ArgumentException("resource selection requires actual same-company session contexts");
                intents.AddRange(ResourceIntents(results[i], contexts[i]));
            }
            if (intents.Count == 0) return new ResourceClaimCheck(ResourceTransferStatus.Ready);
            IResourceTransaction transaction = _world.Services.Get<IResourceTransaction>();
            ResourceClaimCheck check = transaction.EvaluateClaims(company, intents, System.Array.Empty<ResourceReservation>());
            if (check.Ready) return check;
            // One iterative prefix-pruned joint selection. Never reserve, change a committed recipe,
            // or cap content combinations. Occurrences retain their actual node/context identity.
            var options = new List<List<WorkResourceChoice>>();
            var occurrences = new List<int>();
            for (int row = 0; row < results.Count; row++) foreach (var choice in results[row].Resources)
            {
                var alternatives = new List<WorkResourceChoice>();
                if (Binding(contexts[row].Execution, choice.Path) != null) alternatives.Add(choice);
                else foreach (var candidate in results[row].ResourceCandidates)
                    if (candidate.Result.Status == WorkRequirementStatus.Satisfied && candidate.Choice.At(choice.Path))
                        alternatives.Add(candidate.Choice);
                if (alternatives.Count == 0) alternatives.Add(choice);
                options.Add(alternatives); occurrences.Add(row);
            }
            var indices = new int[options.Count]; var chosen = new List<WorkResourceChoice>(); int level = 0;
            while (level >= 0)
            {
                if (indices[level] >= options[level].Count)
                {
                    indices[level] = 0; level--;
                    if (level >= 0) { chosen.RemoveAt(chosen.Count - 1); indices[level]++; }
                    continue;
                }
                chosen.Add(options[level][indices[level]]);
                var proposed = new List<WorkRequirementResult>(); var prefix = new List<ResourceClaimIntent>();
                for (int row = 0; row < results.Count; row++)
                {
                    var resources = new List<WorkResourceChoice>();
                    for (int occurrence = 0; occurrence < chosen.Count; occurrence++)
                        if (occurrences[occurrence] == row) resources.Add(chosen[occurrence]);
                    WorkRequirementResult current = results[row].WithResources(resources.AsReadOnly(), results[row].ResourceCandidates);
                    proposed.Add(current); prefix.AddRange(ResourceIntents(current, contexts[row]));
                }
                ResourceClaimCheck proposal = prefix.Count == 0 ? new ResourceClaimCheck(ResourceTransferStatus.Ready)
                    : transaction.EvaluateClaims(company, prefix, System.Array.Empty<ResourceReservation>());
                bool fits = proposal.Ready;
                if (!fits) { chosen.RemoveAt(chosen.Count - 1); indices[level]++; continue; }
                if (level == options.Count - 1) { selected = proposed.AsReadOnly(); return proposal; }
                level++;
            }
            return check;
        }
        // Selection is identity intent, never authority: bind only to fresh checked offers and
        // recheck the same native set with no alternate source left for silent substitution.
        internal WorkRequirementResult BindResourceSelection(WorkRequirementResult fresh,
            IReadOnlyList<WorkResourceChoice> selected, WorkRequirementContext context)
        {
            if (selected.Count != fresh.Resources.Count) return WorkRequirementResult.Invalid(Codes.InvalidArgument);
            var resources = new List<WorkResourceChoice>(); var offers = new List<WorkResourceCandidate>();
            var used = new bool[fresh.Resources.Count];
            foreach (var intent in selected)
            {
                int occurrence = -1;
                for (int i = 0; i < fresh.Resources.Count; i++)
                    if (!used[i] && intent != null && fresh.Resources[i].At(intent.Path)) { occurrence = i; break; }
                if (occurrence < 0) return WorkRequirementResult.Invalid(Codes.InvalidArgument);
                used[occurrence] = true; WorkResourceChoice current = fresh.Resources[occurrence];
                if (current.Requirement != intent.Requirement || current.Use != intent.Use ||
                    !BillsEqual(current.Quantities, intent.Quantities) || !RatesEqual(current.Rates, intent.Rates) || !SelectionsEqual(current.Selections, intent.Selections))
                    return WorkRequirementResult.Invalid(Codes.InvalidArgument);
                WorkResourceChoice actual = current.Endpoint == intent.Endpoint ? current : null;
                foreach (var offer in fresh.ResourceCandidates)
                    if (offer.Choice.At(intent.Path) && offer.Choice.Endpoint == intent.Endpoint &&
                        offer.Result.Status == WorkRequirementStatus.Satisfied)
                    { actual = offer.Choice; offers.Add(offer); break; }
                if (actual == null) return WorkRequirementResult.Pending(WorkResourceCodes.MissingSource);
                resources.Add(actual);
            }
            return CheckResourceSet(fresh.WithResources(resources.AsReadOnly(), offers.AsReadOnly()), context);
        }
        // One resource/budget acquisition for the actual node set. No separate team ledger.
        private bool AcquireResources(IReadOnlyList<WorkRequirementResult> results,
            IReadOnlyList<WorkRequirementContext> contexts, out Code blocker)
        {
            if (results.Count != contexts.Count) throw new ArgumentException("unaligned work acquisition");
            var indices = new List<int>(); for (int i = 0; i < contexts.Count; i++) indices.Add(i);
            indices.Sort((a, b) => {
                int c = (contexts[a].Execution?.OrderId ?? 0).CompareTo(contexts[b].Execution?.OrderId ?? 0);
                return c != 0 ? c : string.CompareOrdinal(contexts[a].Node, contexts[b].Node);
            });
            var orderedResults = new List<WorkRequirementResult>(); var orderedContexts = new List<WorkRequirementContext>();
            foreach (int i in indices) { orderedResults.Add(results[i]); orderedContexts.Add(contexts[i]); }
            results = orderedResults.AsReadOnly(); contexts = orderedContexts.AsReadOnly();
            if (!PrepareResourceAcquisition(results, contexts, out var transaction, out var intents, out var budgets, out blocker)) return false;
            if (contexts.Count == 0) return true;
            PlayerId company = contexts[0].Owner;
            return CommitResourceAcquisition(results, contexts, company, transaction, intents, budgets);
        }
        private bool PrepareResourceAcquisition(IReadOnlyList<WorkRequirementResult> results,
            IReadOnlyList<WorkRequirementContext> contexts, out IResourceTransaction transaction,
            out List<ResourceClaimIntent> intents, out List<MonetaryBudgetRequest> budgets, out Code blocker)
        {
            blocker = Code.None;
            transaction = null; intents = new List<ResourceClaimIntent>(); budgets = new List<MonetaryBudgetRequest>();
            if (results.Count != contexts.Count) throw new ArgumentException("unaligned work acquisition");
            if (contexts.Count == 0) return true;
            PlayerId company = contexts[0].Owner;
            var nodes = new HashSet<ActivityNodeExecution>();
            for (int i = 0; i < contexts.Count; i++)
            {
                if (contexts[i].Execution == null || !nodes.Add(contexts[i].Execution) || contexts[i].Owner != company ||
                    !ReferenceEquals(contexts[i].World, _world) || Order(contexts[i].Execution.OrderId)?.Owner != company ||
                    Order(contexts[i].Execution.OrderId)?.Node(contexts[i].Node) != contexts[i].Execution)
                    throw new ArgumentException("acquisition requires distinct actual same-company nodes");
                intents.AddRange(ResourceIntents(results[i], contexts[i], true));
                budgets.AddRange(BudgetRequests(contexts[i].Execution, results[i]));
            }
            var costCheck = CheckCostSet(results, contexts);
            if (costCheck.Status != WorkRequirementStatus.Satisfied) { blocker = costCheck.Blocker; return false; }
            transaction = intents.Count == 0 ? null : _world.Services.Get<IResourceTransaction>();
            if (transaction != null)
            {
                var check = transaction.EvaluateClaims(company, intents, System.Array.Empty<ResourceReservation>());
                if (!check.Ready) { blocker = ResourceRefusal(check.Status).Blocker; return false; }
            }
            if (budgets.Count > 0)
            {
                var status = MonetaryBudgetExecution.CheckReservations(_world, _world.Services.Get<IMonetaryBudgets>(), company, budgets.AsReadOnly());
                if (status != MonetaryBudgetStatus.Ready) { blocker = MonetaryBudgetExecution.Blocker(status); return false; }
            }
            return true;
        }
        private bool CommitResourceAcquisition(IReadOnlyList<WorkRequirementResult> results,
            IReadOnlyList<WorkRequirementContext> contexts, PlayerId company, IResourceTransaction transaction,
            List<ResourceClaimIntent> intents, List<MonetaryBudgetRequest> budgets)
        {
            IReadOnlyList<ResourceReservation> acquiredReceipts = Array.Empty<ResourceReservation>();
            if (transaction != null)
            {
                ResourceClaimResult acquired = transaction.AcquireClaims(company, intents, System.Array.Empty<ResourceReservation>());
                if (!acquired.Committed) throw new InvalidOperationException("complete native resource acquisition changed after admission");
                acquiredReceipts = acquired.Receipts;
            }
            IReadOnlyList<MonetaryBudgetReceipt> budgetReceipts = budgets.Count == 0 ? Array.Empty<MonetaryBudgetReceipt>()
                : MonetaryBudgetExecution.Reserve(_world, _world.Services.Get<IMonetaryBudgets>(), company, budgets.AsReadOnly());
            if (acquiredReceipts.Count > 0 && !transaction.EvaluateClaimReceipts(company, acquiredReceipts).Ready)
                throw new InvalidOperationException("optional budget acquisition changed real native work receipts");
            var used = new HashSet<ReservationToken>(); int budgetIndex = 0;
            for (int i = 0; i < contexts.Count; i++)
            {
                var node = contexts[i].Execution; var result = results[i]; var bindings = new List<WorkResourceBinding>(node.Resources);
                foreach (var choice in result.Resources)
                {
                    if (Binding(node, choice.Path) != null) continue;
                    ReservationOwner owner = choice.Owner(node.OrderId, node.Id); var receipts = new List<ResourceReservation>();
                    foreach (var receipt in acquiredReceipts) if (receipt.Owner.Equals(owner) && receipt.Token.Endpoint == choice.Endpoint)
                    {
                        if (!used.Add(receipt.Token)) throw new InvalidOperationException("native work set duplicated an actual receipt binding");
                        receipts.Add(receipt);
                    }
                    if (receipts.Count != (WorkResourceUses.Running(choice.Use) ? 0 : choice.Quantities.Count))
                        throw new InvalidOperationException("activity acquisition did not return its exact native receipt set");
                    bindings.Add(new WorkResourceBinding(choice, receipts, false));
                }
                node.Resources = bindings.AsReadOnly();
                var actualBudgets = new List<MonetaryBudgetReceipt>();
                int count = BudgetRequests(node, result).Count;
                for (int j = 0; j < count; j++) actualBudgets.Add(budgetReceipts[budgetIndex++]);
                BindCosts(node, result, actualBudgets.AsReadOnly());
            }
            if (used.Count != acquiredReceipts.Count || budgetIndex != budgetReceipts.Count)
                throw new InvalidOperationException("native work set returned an extra resource or budget receipt");
            return true;
        }
        internal void ReleaseResources(ActivityNodeExecution node, PlayerId company)
        {
            var spent = new List<WorkResourceBinding>();
            var receipts = new List<ResourceReservation>();
            foreach (var binding in node.Resources)
            {
                if (binding.Spent) { spent.Add(binding); continue; }
                receipts.AddRange(binding.Receipts);
            }
            if (receipts.Count != 0)
            {
                var participants = new List<ResourceReservation>(); var endpoints = new HashSet<ResourceEndpointId>();
                var order = Order(node.OrderId);
                IReadOnlyList<ActivityNodeExecution> nodes = order == null ? new[] { node } : order.Nodes;
                foreach (var actual in nodes) foreach (var binding in actual.Resources)
                {
                    endpoints.Add(binding.Choice.Endpoint);
                    if (!binding.Spent) participants.AddRange(binding.Receipts);
                }
                var intent = new ResourceClaimReleaseIntent(company, receipts, participants, new List<ResourceEndpointId>(endpoints), ResourceClaimReleasePolicy.Surviving);
                ResourceClaimResult result = _world.Services.Get<IResourceTransaction>().ReleaseClaims(intent);
                if (!result.Committed) throw new InvalidOperationException("activity failed to release its surviving resource claim set: " + result.Status);
            }
            node.Resources = spent.AsReadOnly();
            ReleaseCosts(node, company);
        }
        private IResourceWorkRequirement ResourceAt(IWorkRequirement root, IReadOnlyList<string> path)
        {
            if (root == null || path.Count == 0 || path[0] != root.Id) return null;
            IWorkRequirement current = root;
            for (int i = 1; i < path.Count; i++)
            {
                if (!(current is IWorkRequirementGroup group)) return null;
                IWorkRequirement next = null;
                foreach (var entry in group.Entries) if (entry.Id == path[i]) { next = entry.Requirement; break; }
                if (next == null) return null; current = next;
            }
            return current as IResourceWorkRequirement;
        }
        private void ValidateResourceBindings(Order order, ActivityNodeExecution node, IActivityNodeDefinition definition, Job job)
        {
            var paths = new List<IReadOnlyList<string>>(); var tokens = new HashSet<ReservationToken>();
            foreach (var binding in node.Resources)
            {
                WorkResourceChoice choice = binding.Choice; IResourceWorkRequirement requirement = ResourceAt(definition.Requirement, choice.Path);
                if (requirement == null || requirement.Id != choice.Requirement || requirement.Use != choice.Use || binding.Spent && choice.Use != WorkResourceUse.Consume ||
                    !BillsEqual(ScaledBill(requirement, definition.RequirementMultiplier, _world.Content), choice.Quantities) ||
                    !RatesEqual(ScaledRates(requirement, choice.Quantities), choice.Rates) || !SelectionsEqual(requirement.Selections, choice.Selections) ||
                    binding.Receipts.Count != (WorkResourceUses.Running(choice.Use) ? 0 : choice.Quantities.Count))
                    throw new CorruptStateException("saved resource binding differs from its authored occurrence/native bill/use");
                foreach (var path in paths) if (choice.At(path)) throw new CorruptStateException("duplicate saved resource occurrence");
                paths.Add(choice.Path); ReservationOwner owner = choice.Owner(order.Id, node.Id);
                for (int i = 0; i < binding.Receipts.Count; i++)
                {
                    ResourceReservation receipt = binding.Receipts[i]; ResourceAmount line = choice.Quantities[i];
                    if (!receipt.Token.IsValid || receipt.Token.Endpoint != choice.Endpoint || !tokens.Add(receipt.Token) || !receipt.Owner.Equals(owner) ||
                        receipt.Direction != ReservationDirection.Outbound || receipt.Resource != line.Resource || receipt.Amount != line.Amount)
                        throw new CorruptStateException("saved resource receipt differs from its actual node owner/native quantity");
                }
                ResourceClaimCheck actual = WorkResourceUses.Running(choice.Use)
                    ? _world.Services.Get<IResourceTransaction>().EvaluateClaims(order.Owner, new[] { new ResourceClaimIntent(ReservationDirection.Outbound, owner, choice.Endpoint, Bill.Empty) }, System.Array.Empty<ResourceReservation>())
                    : _world.Services.Get<IResourceTransaction>().EvaluateClaimReceipts(order.Owner, binding.Receipts);
                if (binding.Spent ? actual.Ready || node.Progress.WholeTicks == 0 && node.Progress.Fraction == 0
                    : actual.Status != ResourceTransferStatus.Ready && actual.Status != ResourceTransferStatus.MissingEndpoint)
                    throw new CorruptStateException("saved resource binding is not its actual live/spent endpoint promise");
                if (!binding.Spent && (!order.IsActive || node.State != NodeState.Working && node.State != NodeState.Paused ||
                    definition.Roles.Count > 0 && job?.Worker.IsValid != true))
                    throw new CorruptStateException("unassigned or terminal activity retains live resource claims");
            }
            bool assigned = order.IsActive && (node.State == NodeState.Working || node.State == NodeState.Paused) &&
                (definition.Roles.Count == 0 || job?.Worker.IsValid == true);
            if (!assigned && node.Resources.Count == 0) return;
            var found = new HashSet<WorkResourceBinding>();
            foreach (var current in SelectedOccurrences(definition.Requirement, node))
            {
                if (current.Requirement is IResourceWorkRequirement)
                {
                    WorkResourceBinding binding = Binding(node, current.Path);
                    if (binding != null) found.Add(binding);
                    else if (assigned) throw new CorruptStateException("assigned activity is missing a selected resource occurrence");
                }
            }
            if (found.Count != node.Resources.Count) throw new CorruptStateException("resource binding is outside the selected workflow branch");
        }
        private static bool BillsEqual(Bill left, Bill right)
        {
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++) if (!left[i].Equals(right[i])) return false;
            return true;
        }
        private static bool RatesEqual(IReadOnlyList<IResourceRate> left, IReadOnlyList<IResourceRate> right)
        {
            if (left.Count != right.Count) return false;
            foreach (var rate in left)
            {
                bool found = false;
                foreach (var other in right)
                    if (rate.Resource.Resource == other.Resource.Resource && rate.Quantity == other.Quantity &&
                        rate.PeriodNumerator == other.PeriodNumerator && rate.PeriodDenominator == other.PeriodDenominator && rate.PeriodUnit == other.PeriodUnit)
                    { found = true; break; }
                if (!found) return false;
            }
            return true;
        }
        private void SyncResources(IStateSync sync, ActivityNodeExecution node, IActivityNodeDefinition authored)
        {
            int count = sync.Count(node.Resources.Count); var bindings = sync.Reading ? new List<WorkResourceBinding>() : null;
            for (int i = 0; i < count; i++)
            {
                WorkResourceBinding binding = sync.Reading ? null : node.Resources[i]; WorkResourceChoice choice = binding?.Choice;
                int segments = sync.Count(choice?.Path.Count ?? 0); var path = sync.Reading ? new List<string>() : null;
                for (int j = 0; j < segments; j++) { string part = sync.Reading ? null : choice.Path[j]; sync.String(ref part); path?.Add(part); }
                string requirement = choice?.Requirement; sync.String(ref requirement);
                ResourceEndpointId endpoint = choice?.Endpoint ?? default; ResourceEndpointId.Sync(sync, ref endpoint);
                byte use = (byte)(choice?.Use ?? default); sync.Byte(ref use); bool spent = binding?.Spent ?? false; sync.Bool(ref spent);
                int records = sync.Count(binding?.Receipts.Count ?? 0); var receipts = sync.Reading ? new List<ResourceReservation>() : null;
                for (int j = 0; j < records; j++)
                {
                    ResourceReservation receipt = sync.Reading ? default : binding.Receipts[j]; ReservationToken token = receipt.Token;
                    string scope = receipt.Owner.Scope; long identity = receipt.Owner.Identity; ResourceId resource = receipt.Resource; Fix amount = receipt.Amount;
                    byte direction = (byte)receipt.Direction;
                    ReservationToken.Sync(sync, ref token); sync.String(ref scope); sync.Long(ref identity); sync.Resource(ref resource); sync.Fix(ref amount); sync.Byte(ref direction);
                    receipts?.Add(new ResourceReservation(token, new ReservationOwner(scope, identity), resource, amount, (ReservationDirection)direction));
                }
                if (sync.Reading)
                {
                    var definition = _world.Content.Find<IWorkRequirement>(requirement) as IResourceWorkRequirement;
                    if (definition == null) throw new CorruptStateException("unknown saved resource requirement");
                    try
                    {
                        Bill quantities = ScaledBill(definition, authored.RequirementMultiplier, _world.Content);
                        bindings.Add(new WorkResourceBinding(new WorkResourceChoice(path, requirement, endpoint,
                            quantities, (WorkResourceUse)use, ScaledRates(definition, quantities), definition.Selections), receipts, spent));
                    }
                    catch (ArgumentException) { throw new CorruptStateException("invalid saved resource binding identity"); }
                }
            }
            if (sync.Reading) node.Resources = bindings.AsReadOnly();
        }
    }
}
