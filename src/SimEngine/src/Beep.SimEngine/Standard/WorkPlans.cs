using System;
using System.Collections.Generic;
using System.Globalization;
using Beep.SimEngine.Content;
using System.Text;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class WorkBoard : IPlanBoard
    {
        private sealed class PlanEntry
        {
            internal long Id, CreatedTick;
            internal long EndedTick = -1;
            internal string Definition, Group;
            internal JsonValue Arguments;
            internal IReadOnlyList<long> Dependencies = Array.Empty<long>();
            // Only terminal history stores the former execution target. Active target is native Order.Target.
            internal EntityId Target;
            internal PlayerId Company;
            // While active priority belongs ONLY to the native order, and is omitted from this codec.
            internal int Priority, Order;
            internal bool Paused;
            internal PlanState State;
            internal Code TerminalBlocker;
            internal PlanOrigin Origin;
            // Current binding, once on the head. Origin remains immutable historical posting provenance.
            internal EntityId SourceOwner;
            // Immutable authored admission shape, once on the actual first template-cycle member.
            internal PlanTemplate CycleTemplate;
            internal bool Live => State == PlanState.Pending || State == PlanState.Active;
        }
        private readonly List<PlanEntry> _plans = new List<PlanEntry>();
        // Lookup aliases only: records, order and codec still belong to _plans/WorkBoard.
        private readonly Dictionary<long, PlanEntry> _planById = new Dictionary<long, PlanEntry>();
        private readonly Dictionary<int, PlanEntry> _activePlanByOrder = new Dictionary<int, PlanEntry>();
        private long _nextPlan = 1;
        // Call-local alias of an existing record, not another plan/state owner. Start
        // admission must distinguish the plan being activated from foreign intents.
        private PlanEntry _activatingPlan;
        private T WithPlanActivation<T>(PlanEntry entry, Func<T> action)
        {
            PlanEntry previous = _activatingPlan; _activatingPlan = entry;
            try { return action(); }
            finally { _activatingPlan = previous; }
        }
        private PlanEntry PlanEntryById(long id) => _planById.TryGetValue(id, out PlanEntry entry) ? entry : null;
        private int PlanPriority(PlanEntry entry) => entry.State == PlanState.Active
            ? Order(entry.Order).Priority : entry.Priority;
        private PlanIntent Intent(PlanEntry entry) => new PlanIntent(entry.Definition,
            entry.Arguments, entry.Group, PlanPriority(entry), entry.Paused, entry.Dependencies);
        private IPlanDefinition Definition(PlanEntry entry) => (IPlanDefinition)ResolvePlan(entry.Definition);
        private void RequirePlanCheck(Code check)
        {
            _world.RequireStateMutation();
            if (!check.IsNone) throw new InvalidOperationException("plan admission refused: " + check);
        }

        /// <summary>The one player activity admission used by direct commands, plan promotion and previews.</summary>
        public Code CheckPlayerActivity(PlayerId company, string activity, EntityId target, int priority)
        {
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            if (string.IsNullOrEmpty(activity)) return Codes.UnknownDef;
            IActivityDefinition definition = ResolvePlan(activity);
            if (definition == null) return Codes.UnknownDef;
            if (!CanDiscloseActivity(company, activity)) return Codes.UnknownDef;
            if (definition.PlanKey != activity) throw new InvalidOperationException("activity resolver returned a different identity");
            if (!(definition is IPlayerActivityDefinition player)) return StandardCodes.NotOrderable;
            Entity entity = _world.Entities.Get(target);
            if (entity == null) return Codes.NotFound;
            if (entity.Owner != company) return Codes.NotOwned;
            if (priority < 0) return Codes.InvalidArgument;
            Code check = PlanObservation.Intent(player, _world, company, entity);
            if (!check.IsNone) return check;
            check = ActivityStartRules.Check(_world, definition, company, entity);
            if (!check.IsNone) return check;
            return ActiveOn(target, definition.PlanKey) == null ? Code.None : Codes.AlreadyDone;
        }
        public Code PreviewPlan(PlayerId company, PlanIntent intent)
        {
            _world.RequireHealthyUpdate();
            if (intent == null) return Codes.InvalidArgument;
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            if (_nextPlan == long.MaxValue) return PlanCodes.IdentityRange;
            foreach (long dependency in intent.Dependencies)
            { Code dependencyCheck = CheckPlanOwner(company, dependency, out _); if (!dependencyCheck.IsNone) return dependencyCheck; }
            IActivityDefinition resolved = ResolvePlan(intent.Definition);
            if (resolved == null) return Codes.UnknownDef;
            if (!CanDiscloseActivity(company, intent.Definition)) return Codes.UnknownDef;
            if (!(resolved is IPlanDefinition provider)) return StandardCodes.NotOrderable;
            if (provider.PlanKey != intent.Definition) throw new InvalidOperationException("plan resolver returned a different identity");
            string identity;
            Code check;
            try { identity = PlanObservation.Identity(_world, provider, intent.Arguments); check = PlanObservation.Submission(_world, provider, company, intent.Arguments); }
            catch (ContentException) { return Codes.InvalidArgument; }
            if (string.IsNullOrWhiteSpace(identity)) throw new InvalidOperationException("plan provider returned an empty intent identity");
            if (!check.IsNone) return check;
            foreach (PlanEntry entry in _plans)
                if (entry.Live && entry.Company == company && entry.Definition == intent.Definition && PlanObservation.Identity(_world, Definition(entry), entry.Arguments) == identity)
                    return PlanCodes.Duplicate;
            return Code.None;
        }
        private Code CheckPlanOwner(PlayerId company, long id, out PlanEntry entry)
        {
            entry = null; _world.RequireHealthyUpdate(); entry = PlanEntryById(id);
            if (entry == null) return Codes.NotFound;
            return entry.Company != company ? Codes.NotOwned : Code.None;
        }
        public Code PreviewPlanEdit(PlayerId company, long plan, string group, int priority, bool paused)
        {
            Code check = CheckPlanOwner(company, plan, out PlanEntry entry); if (!check.IsNone) return check;
            if (!entry.Live) return Codes.AlreadyDone;
            try { _ = new PlanIntent(entry.Definition, entry.Arguments, group, priority, paused); }
            catch (ArgumentException) { return Codes.InvalidArgument; }
            return Code.None;
        }
        public Code PreviewPlanMove(PlayerId company, long plan, long before)
        {
            Code check = CheckPlanOwner(company, plan, out PlanEntry entry); if (!check.IsNone) return check;
            if (entry.State != PlanState.Pending) return PlanCodes.NotPending;
            if (before == 0) return Code.None;
            check = CheckPlanOwner(company, before, out PlanEntry other); if (!check.IsNone) return check;
            return other.State == PlanState.Pending ? Code.None : PlanCodes.NotPending;
        }
        public PlanGroupCheck PreviewPlanDependencies(PlayerId company, PlanDependencyChanges changes)
        {
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company)) return new PlanGroupCheck(Codes.NotSeated);
            if (changes == null) return new PlanGroupCheck(Codes.InvalidArgument);
            var checks = new List<PlanGroupEntryCheck>();
            var proposed = new Dictionary<long, IReadOnlyList<long>>();
            foreach (PlanDependencyChange change in changes.Entries)
            {
                Code check = CheckPlanOwner(company, change.Plan, out PlanEntry entry);
                if (check.IsNone && !entry.Live) check = PlanCodes.NotPending;
                if (check.IsNone && !entry.Origin.IsManual) check = PlanCodes.SourceOwnedDependencies;
                if (check.IsNone)
                    foreach (long id in change.Dependencies)
                    { check = CheckPlanOwner(company, id, out _); if (!check.IsNone) break; }
                if (check.IsNone && entry.State == PlanState.Active && !CheckPlanDependencies(company, change.Dependencies).IsNone)
                    check = _world.System<WorkSystem>().CheckPlanDependencyInterruption(_world, Order(entry.Order));
                checks.Add(new PlanGroupEntryCheck(change.Plan, check));
                proposed.Add(change.Plan, change.Dependencies);
            }
            PlanGroupCheck result = GroupResult(checks); if (!result.Ready) return result;
            var graph = new List<PlanGraphNode>();
            foreach (PlanEntry entry in _plans)
                if (entry.Company == company)
                    graph.Add(new PlanGraphNode(entry, proposed.TryGetValue(entry.Id, out var links) ? links : entry.Dependencies));
            try { _ = new DependencyGraph<PlanGraphNode>(graph, "proposed plan prerequisites"); }
            catch (ContentException)
            {
                checks.Clear();
                foreach (PlanDependencyChange change in changes.Entries)
                    checks.Add(new PlanGroupEntryCheck(change.Plan, PlanCodes.DependencyCycle));
                return GroupResult(checks);
            }
            return result;
        }
        public PlanGroupCheck PreviewPlanCancel(PlayerId company, PlanCancellationSelection selection)
        {
            _world.RequireHealthyUpdate();
            if (selection == null) return new PlanGroupCheck(Codes.InvalidArgument);
            var checks = new List<PlanGroupEntryCheck>();
            foreach (long id in selection.Plans)
            {
                Code check = CheckPlanOwner(company, id, out PlanEntry entry);
                if (check.IsNone)
                    check = selection.PendingOnly && entry.State != PlanState.Pending ? PlanCodes.NotPending :
                        selection.PendingOnly && entry.Paused ? PlanCodes.Paused : entry.Live ? Code.None : Codes.AlreadyDone;
                checks.Add(new PlanGroupEntryCheck(id, check));
            }
            return GroupResult(checks);
        }
        public Code PreviewPlanRepeat(PlayerId company, long plan)
        {
            Code check = CheckPlanOwner(company, plan, out PlanEntry entry); if (!check.IsNone) return check;
            return entry.Live ? PlanCodes.NotEnded : PreviewPlan(company, Intent(entry));
        }
        public long SubmitPlan(PlayerId company, PlanIntent intent)
        {
            RequirePlanCheck(PreviewPlan(company, intent));
            return AddPlan(company, intent);
        }
        private long AddPlan(PlayerId company, PlanIntent intent)
        {
            long id = _nextPlan; _nextPlan = checked(_nextPlan + 1);
            var entry = new PlanEntry { Id = id, Definition = intent.Definition, Arguments = intent.Arguments,
                Company = company, Group = intent.Group, Priority = intent.Priority, Paused = intent.Paused, CreatedTick = _world.Tick,
                Dependencies = intent.Dependencies };
            _plans.Add(entry); _planById.Add(id, entry);
            return id;
        }
        public void ConfigurePlan(PlayerId company, long plan, string group, int priority, bool paused)
        {
            RequirePlanCheck(PreviewPlanEdit(company, plan, group, priority, paused));
            PlanEntry entry = PlanEntryById(plan); entry.Group = group; entry.Paused = paused;
            if (entry.State == PlanState.Active)
            {
                Order order = Order(entry.Order); order.Priority = priority;
                Code blocker = PlanOrderBlocker(order.Id);
                if (!blocker.IsNone) _world.System<WorkSystem>().PausePlannedOrder(_world, order, blocker);
            }
            else entry.Priority = priority;
            Dirty = true;
        }
        public void MovePlan(PlayerId company, long plan, long before)
        {
            RequirePlanCheck(PreviewPlanMove(company, plan, before));
            if (plan == before) return;
            PlanEntry entry = PlanEntryById(plan); _plans.Remove(entry);
            if (before == 0) _plans.Add(entry); else _plans.Insert(_plans.IndexOf(PlanEntryById(before)), entry);
        }
        public void ConfigurePlanDependencies(PlayerId company, PlanDependencyChanges changes)
        {
            RequirePlanCheck(PreviewPlanDependencies(company, changes).Blocker);
            foreach (PlanDependencyChange change in changes.Entries)
                PlanEntryById(change.Plan).Dependencies = change.Dependencies;
            foreach (PlanDependencyChange change in changes.Entries)
            {
                PlanEntry entry = PlanEntryById(change.Plan);
                if (entry.State != PlanState.Active) continue;
                // Release dependency-blocked workers even if the player also explicitly paused.
                Code dependency = CheckPlanDependencies(entry);
                if (!dependency.IsNone) _world.System<WorkSystem>().PausePlannedOrder(_world, Order(entry.Order), dependency);
            }
            Dirty = true;
        }
        public void CancelPlan(PlayerId company, PlanCancellationSelection selection)
        {
            RequirePlanCheck(PreviewPlanCancel(company, selection).Blocker);
            foreach (long id in selection.Plans)
            {
                PlanEntry entry = PlanEntryById(id);
                if (entry.State == PlanState.Active)
                { _world.System<WorkSystem>().Cancel(_world, Order(entry.Order)); SettlePlan(entry); }
                else { entry.State = PlanState.Cancelled; entry.Paused = false; entry.EndedTick = _world.Clock.StepIndex; }
            }
        }
        public long RepeatPlan(PlayerId company, long plan)
        { RequirePlanCheck(PreviewPlanRepeat(company, plan)); return SubmitPlan(company, Intent(PlanEntryById(plan))); }
        public void CopyPlans(PlayerId viewer, IList<PlanView> destination)
        {
            _world.RequireHealthyUpdate();
            var facts = EntityObservation.ReadProvider(_world, this, null, () => {
                var rows = new List<PlanView>();
                foreach (PlanEntry entry in _plans) if (CanDisclosePlan(viewer, entry)) rows.Add(ProjectPlan(entry));
                return rows;
            });
            _world.RequireHealthyUpdate(); destination.Clear();
            foreach (PlanView fact in facts) destination.Add(fact);
        }
        private bool CanDisclosePlan(PlayerId viewer, PlanEntry root)
        {
            if (viewer != PlayerId.None && (!_world.Players.IsSeat(viewer) || viewer != root.Company)) return false;
            var pending = new Stack<PlanEntry>(); var visited = new HashSet<long>(); pending.Push(root);
            while (pending.Count > 0)
            {
                PlanEntry entry = pending.Pop(); if (!visited.Add(entry.Id)) continue;
                if (!CanDiscloseActivity(viewer, entry.Definition)) return false;
                EntityId target = entry.State == PlanState.Active ? Order(entry.Order).Target : entry.Target;
                // A finished corridor's temporary marker is deliberately retired. Its owning company's
                // existing terminal intent/result remains history, not a live-entity visibility query.
                Entity actualTarget = target.IsValid ? _world.Entities.Get(target) : null;
                if (target.IsValid && (entry.Live || actualTarget?.Alive == true) && !_world.ViewFor(viewer).CanSee(actualTarget)) return false;
                foreach (var reference in ReadPlanReferences(Definition(entry), entry.Arguments))
                    if (!_world.ViewFor(viewer).CanSee(_world.Entities.Get(reference))) return false;
                EntityId currentSource = PlanSourceOwner(entry);
                if (currentSource.IsValid && !_world.ViewFor(viewer).CanSee(_world.Entities.Get(currentSource))) return false;
                foreach (long id in entry.Dependencies)
                    pending.Push(PlanEntryById(id) ?? throw new InvalidOperationException("admitted plan lost its prerequisite"));
            }
            return true;
        }
        private PlanView ProjectPlan(PlanEntry entry)
        {
            Code gate = entry.Live ? entry.Paused ? PlanCodes.Paused : CheckPlanDependencies(entry) : Code.None;
            Code blocker = !gate.IsNone ? gate : entry.State == PlanState.Pending ? CheckPlanActivation(entry)
                : entry.State == PlanState.Active ? Order(entry.Order).Blocker : entry.TerminalBlocker;
            EntityId target = entry.State == PlanState.Active ? Order(entry.Order).Target : entry.Target;
            return new PlanView(entry.Id, Intent(entry), target, entry.State, entry.Order, entry.CreatedTick, entry.EndedTick, blocker, entry.Origin, PlanSourceOwner(entry));
        }
        internal Code PlanOrderBlocker(int order) => order != 0 && _activePlanByOrder.TryGetValue(order, out PlanEntry entry)
            ? entry.Paused ? PlanCodes.Paused : CheckPlanDependencies(entry) : Code.None;
        private Code CheckPlanDependencies(PlanEntry entry)
            => CheckPlanDependencies(entry.Company, entry.Dependencies);
        private Code CheckPlanDependencies(PlayerId company, IReadOnlyList<long> dependencies)
        {
            bool pending = false;
            foreach (long id in dependencies)
            {
                PlanEntry prerequisite = PlanEntryById(id);
                if (prerequisite == null || prerequisite.Company != company)
                    throw new InvalidOperationException("admitted plan lost its actual owned prerequisite");
                if (prerequisite.State == PlanState.Cancelled) return PlanCodes.DependencyCancelled;
                if (prerequisite.State == PlanState.Failed) return PlanCodes.DependencyFailed;
                pending |= prerequisite.State != PlanState.Done;
            }
            return pending ? PlanCodes.DependencyPending : Code.None;
        }
        private Code CheckPlanActivation(PlanEntry entry)
        { Code check = CheckPlanDependencies(entry); return check.IsNone
            ? WithPlanActivation(entry, () => PlanObservation.Activation(_world, Definition(entry), entry.Company, entry.Arguments)) : check; }
        internal void PromotePlans()
        {
            // This is intent admission only. The existing WorkSystem alone schedules and executes jobs.
            var pending = new List<PlanEntry>(); foreach (PlanEntry entry in _plans)
                if (entry.State == PlanState.Pending && !entry.Paused) pending.Add(entry);
            var positions = new Dictionary<PlanEntry, int>();
            for (int i = 0; i < pending.Count; i++) positions.Add(pending[i], i);
            pending.Sort((a, b) => a.Priority != b.Priority ? b.Priority.CompareTo(a.Priority) : positions[a].CompareTo(positions[b]));
            foreach (PlanEntry entry in pending)
            {
                IPlanDefinition provider = Definition(entry);
                if (!CheckPlanActivation(entry).IsNone) continue;
                int next = _nextOrder;
                Order order = WithPlanActivation(entry, () => provider.Activate(_world, entry.Company, entry.Arguments, entry.Priority));
                if (order == null || !ReferenceEquals(Order(order.Id), order) || order.Id != next || _nextOrder != checked(next + 1) ||
                    !order.IsActive || !ReferenceEquals(order.Plan, PlanObservation.Binding(_world, provider, order.Target)) || order.PlanKey != entry.Definition || order.Owner != entry.Company || order.Priority != entry.Priority ||
                    PlanObservation.ExecutionIdentity(_world, provider, order) != PlanObservation.Identity(_world, provider, entry.Arguments) || order.CreatedTick != _world.Tick ||
                    _world.Entities.Get(order.Target)?.Owner != entry.Company)
                    throw new InvalidOperationException("plan activation must create exactly one matching native execution");
                entry.Order = order.Id; entry.Priority = 0; entry.State = PlanState.Active;
                _activePlanByOrder.Add(order.Id, entry);
            }
        }
        private void SettlePlan(PlanEntry entry)
        {
            Order order = Order(entry.Order) ?? throw new InvalidOperationException("active plan lost its native order");
            if (order.IsActive) return;
            _activePlanByOrder.Remove(entry.Order);
            entry.Priority = order.Priority; entry.Paused = false;
            entry.Target = order.Target;
            entry.TerminalBlocker = order.Blocker;
            entry.EndedTick = _world.Clock.StepIndex;
            entry.State = order.State == OrderState.Done ? PlanState.Done : order.State == OrderState.Failed
                ? PlanState.Failed : PlanState.Cancelled;
        }
        internal void SettlePlans()
        { foreach (PlanEntry entry in _plans) if (entry.State == PlanState.Active) SettlePlan(entry); }
        internal void SettlePlanExecution(Order order)
        {
            if (_activePlanByOrder.TryGetValue(order.Id, out var entry)) SettlePlan(entry);
        }

        private void SyncPlans(IStateSync sync)
        {
            if (!sync.Reading) { ValidatePlanEntries(); ValidatePlanExecutions(); } sync.Long(ref _nextPlan);
            SyncPlanHistoryPolicy(sync);
            int count = sync.Count(_plans.Count); if (sync.Reading) { _plans.Clear(); _planById.Clear(); _activePlanByOrder.Clear(); }
            for (int i = 0; i < count; i++)
            {
                PlanEntry entry = sync.Reading ? new PlanEntry() : _plans[i];
                sync.Long(ref entry.Id); sync.String(ref entry.Definition); sync.Player(ref entry.Company);
                string arguments = null;
                if (!sync.Reading) { var text = new StringBuilder(); entry.Arguments.WriteCanonical(text); arguments = text.ToString(); }
                sync.String(ref arguments);
                if (sync.Reading)
                {
                    try { entry.Arguments = Json.Parse(arguments); entry.Arguments.Freeze(); }
                    catch (Exception ex) when (ex is ContentException || ex is ArgumentException || ex is FormatException)
                    { throw new CorruptStateException("invalid saved plan arguments"); }
                }
                sync.String(ref entry.Group); sync.Bool(ref entry.Paused);
                PlanIntent.SyncDependencies(sync, ref entry.Dependencies);
                sync.Enum(ref entry.State); sync.Int(ref entry.Order); sync.Long(ref entry.CreatedTick);
                entry.Origin = PlanOrigin.Sync(sync, entry.Origin);
                if (!entry.Origin.IsManual && entry.Origin.CycleHead == entry.Id) sync.Entity(ref entry.SourceOwner);
                try { PlanTemplate.Sync(sync, ref entry.CycleTemplate); }
                catch (Exception error) when (error is ContentException || error is ArgumentException || error is FormatException)
                { throw new CorruptStateException("invalid recurring cycle template: " + error.Message); }
                if (entry.State != PlanState.Active) sync.Int(ref entry.Priority);
                if (!entry.Live)
                { sync.Long(ref entry.EndedTick); sync.Entity(ref entry.Target); string blocker = entry.TerminalBlocker.Id; sync.String(ref blocker); if (sync.Reading) entry.TerminalBlocker = blocker == null ? Code.None : new Code(blocker); }
                if (sync.Reading)
                {
                    if (_planById.ContainsKey(entry.Id) || entry.State == PlanState.Active && _activePlanByOrder.ContainsKey(entry.Order))
                        throw new CorruptStateException("duplicate plan identity or active execution");
                    _plans.Add(entry); _planById.Add(entry.Id, entry);
                    if (entry.State == PlanState.Active) _activePlanByOrder.Add(entry.Order, entry);
                }
            }
            ValidatePlanEntries();
        }
        private void ValidatePlanEntries()
        {
            if (_nextPlan < 1 || _planById.Count != _plans.Count) throw new CorruptStateException("invalid plan counter or lookup alias");
            var ids = new HashSet<long>(); var orders = new HashSet<int>();
            var live = new HashSet<(PlayerId Company, string Definition, string Identity)>();
            foreach (PlanEntry entry in _plans)
            {
                if (entry.Id <= 0 || entry.Id >= _nextPlan || !ids.Add(entry.Id) || !ReferenceEquals(PlanEntryById(entry.Id), entry) || !_world.Players.IsSeat(entry.Company) ||
                    entry.CreatedTick < 0 || entry.CreatedTick > _world.Tick || entry.Priority < 0 ||
                    entry.Live && entry.EndedTick != -1 || !entry.Live && (entry.EndedTick < entry.CreatedTick || entry.EndedTick > _world.Clock.StepIndex) ||
                    entry.State < PlanState.Pending || entry.State > PlanState.Cancelled || entry.Order < 0 ||
                    entry.State == PlanState.Pending && entry.Order != 0 || entry.State == PlanState.Active && entry.Order == 0 ||
                    (entry.State == PlanState.Done || entry.State == PlanState.Failed) && entry.Order == 0 ||
                    !entry.Live && entry.Paused || entry.Live && !entry.TerminalBlocker.IsNone ||
                    entry.Live && entry.Target.IsValid || entry.Order != 0 && !entry.Live && !entry.Target.IsValid ||
                    entry.Arguments == null || !entry.Arguments.IsObject || !entry.Arguments.Frozen ||
                    !(ResolvePlan(entry.Definition) is IPlanDefinition definition) || definition.PlanKey != entry.Definition)
                    throw new CorruptStateException("invalid saved plan intent");
                string identity;
                if (!entry.Origin.IsManual && (!entry.Origin.Entity.IsValid || entry.Origin.Source.IsNone ||
                    entry.Origin.Entity.Value >= _world.Entities.NextIdentity.Value || entry.Origin.CycleHead <= 0 || entry.Origin.CycleHead > entry.Id ||
                    PlanEntryById(entry.Origin.CycleHead) == null))
                    throw new CorruptStateException("invalid plan source provenance");
                if (!entry.Origin.IsManual)
                {
                    PlanEntry cycle = PlanEntryById(entry.Origin.CycleHead);
                    if (cycle.Origin.IsManual || cycle.Origin.CycleHead != cycle.Id || cycle.Company != entry.Company ||
                        cycle.CreatedTick != entry.CreatedTick || cycle.Origin.Entity != entry.Origin.Entity ||
                        cycle.Origin.Source != entry.Origin.Source || cycle.Origin.TemplateCycle != entry.Origin.TemplateCycle)
                        throw new CorruptStateException("recurring member does not alias its actual admitted cycle head");
                }
                try { identity = PlanObservation.Identity(_world, Definition(entry), entry.Arguments); }
                catch (ContentException) { throw new CorruptStateException("invalid saved plan argument schema"); }
                if (string.IsNullOrWhiteSpace(identity)) throw new CorruptStateException("invalid saved plan identity");
                if (entry.Live && !live.Add((entry.Company, entry.Definition, identity)))
                    throw new CorruptStateException("duplicate live plan intent");
                if (entry.Order != 0 && (!orders.Add(entry.Order) || entry.Order >= _nextOrder))
                    throw new CorruptStateException("duplicate or future plan execution link");
                if (entry.State == PlanState.Active)
                {
                    Order order = Order(entry.Order);
                    if (order == null || !order.IsActive || order.PlanKey != entry.Definition || order.Owner != entry.Company || entry.Priority != 0 ||
                        !_activePlanByOrder.TryGetValue(entry.Order, out PlanEntry alias) || !ReferenceEquals(alias, entry))
                        throw new CorruptStateException("plan does not alias its native active execution");
                }
                else if (entry.Order != 0 && Order(entry.Order) is Order ended)
                {
                    PlanState actual = ended.State == OrderState.Done ? PlanState.Done :
                        ended.State == OrderState.Failed ? PlanState.Failed : PlanState.Cancelled;
                    if (ended.IsActive || ended.Owner != entry.Company || ended.Target != entry.Target ||
                        ended.PlanKey != entry.Definition || actual != entry.State || ended.Priority != entry.Priority || ended.Blocker != entry.TerminalBlocker)
                        throw new CorruptStateException("plan history differs from retained native disposition");
                }
                try { _ = Intent(entry); } catch (ArgumentException) { throw new CorruptStateException("invalid plan settings"); }
                bool head = !entry.Origin.IsManual && entry.Origin.CycleHead == entry.Id;
                if (head ? !entry.SourceOwner.IsValid || entry.SourceOwner.Value >= _world.Entities.NextIdentity.Value : entry.SourceOwner != EntityId.None)
                    throw new CorruptStateException("current recurring source binding must exist exactly once on its actual cycle head");
                if ((entry.CycleTemplate != null) != (head && entry.Origin.TemplateCycle))
                    throw new CorruptStateException("recurring template shape must exist exactly once on its actual cycle head");
                if (head)
                {
                    if (entry.CycleTemplate != null && !CheckStoredTemplateValue(entry.CycleTemplate).Ready)
                        throw new CorruptStateException("template cycle shape requires actual source provenance and selected providers");
                    var members = new List<PlanEntry>();
                    foreach (PlanEntry member in _plans)
                        if (!member.Origin.IsManual && member.Origin.CycleHead == entry.Id) members.Add(member);
                    members.Sort((a, b) => a.Id.CompareTo(b.Id));
                    ValidateRecurringCycle(members, entry.Id, entry.CycleTemplate, entry.Definition, entry.Company);
                }
            }
            int active = 0; foreach (PlanEntry entry in _plans) if (entry.State == PlanState.Active) active++;
            if (active != _activePlanByOrder.Count) throw new CorruptStateException("extra active plan lookup alias");
            var graph = new List<PlanGraphNode>();
            foreach (PlanEntry entry in _plans)
            {
                foreach (long id in entry.Dependencies)
                {
                    PlanEntry prerequisite = PlanEntryById(id);
                    if (prerequisite == null || prerequisite.Company != entry.Company ||
                        (entry.State == PlanState.Done || !entry.Origin.IsManual && entry.Order != 0) && prerequisite.State != PlanState.Done)
                        throw new CorruptStateException("plan prerequisite is missing, foreign or incompatible with earned/source-owned completion");
                }
                graph.Add(new PlanGraphNode(entry, entry.Dependencies));
            }
            try { _ = new DependencyGraph<PlanGraphNode>(graph, "saved plans"); }
            catch (ContentException error) { throw new CorruptStateException(error.Message); }
        }
        private sealed class PlanGraphNode : IDependencyNode
        {
            internal PlanGraphNode(PlanEntry entry, IReadOnlyList<long> dependencies)
            {
                Id = entry.Id.ToString(CultureInfo.InvariantCulture); var links = new List<string>();
                foreach (long id in dependencies) links.Add(id.ToString(CultureInfo.InvariantCulture));
                Dependencies = links.AsReadOnly();
            }
            public string Id { get; }
            public IReadOnlyList<string> Dependencies { get; }
        }
        private void ValidatePlanExecutions()
        {
            foreach (PlanEntry entry in _plans)
                if (entry.State == PlanState.Active)
                {
                    IPlanDefinition definition = Definition(entry); Order order = Order(entry.Order);
                    if (order == null || !ReferenceEquals(order.Plan, PlanObservation.Binding(_world, definition, order.Target)) ||
                        PlanObservation.ExecutionIdentity(_world, definition, order) != PlanObservation.Identity(_world, definition, entry.Arguments))
                        throw new CorruptStateException("plan execution target/placement differs from its actual intent");
                }
        }
    }
}
