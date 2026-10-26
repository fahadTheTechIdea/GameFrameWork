using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    /// <summary>Private pure game readiness, not a command/plan/work executor.</summary>
    public interface IGameCondition : IContentDefinition
    {
        void ValidateBinding(World world, PlayerId company);
        Code Check(GameConditionContext context);
    }
    public abstract class GameConditionDef : Def, IGameCondition
    {
        public abstract void ValidateBinding(World world, PlayerId company);
        public abstract Code Check(GameConditionContext context);
    }
    /// <summary>One expiring private read invocation, not saved state or a provider grant.</summary>
    public sealed class GameConditionContext
    {
        private readonly GameConditionContracts _owner;
        private readonly PlayerView _view;
        private readonly HashSet<string> _active = new HashSet<string>(StringComparer.Ordinal);
        private bool _open = true;
        internal GameConditionContext(GameConditionContracts owner, PlayerView view) { _owner = owner; _view = view; }
        public PlayerView View { get { RequireOpen(); return _view; } }
        public Code Check(IGameCondition condition) { RequireOpen(); return _owner.ReadChild(this, condition); }
        internal void Enter(string id)
        { RequireOpen(); if (!_active.Add(id)) throw new InvalidOperationException("recursive condition observation: " + id); }
        internal void Leave(string id) => _active.Remove(id);
        internal void Close() { _open = false; _active.Clear(); }
        private void RequireOpen()
        { if (!_open) throw new InvalidOperationException("condition invocation has expired"); }
    }
    /// <summary>Authored dependency metadata; leaves do not declare this capability.</summary>
    public interface IGameConditionDependencies
    {
        IReadOnlyList<string> ConditionIds { get; }
    }
    /// <summary>Pure logical composition over actual registered conditions, never cached readiness.</summary>
    public sealed class CompositeGameConditionDef : GameConditionDef, IGameConditionDependencies
    {
        private bool _all;
        private IReadOnlyList<IGameCondition> _conditions;
        public IReadOnlyList<string> ConditionIds { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            string mode = reader.String("mode");
            if (mode != "all" && mode != "any") throw reader.Error("explicit all/any condition mode required");
            _all = mode == "all";
            if (reader.Array("conditions") == null) throw reader.Error("explicit conditions array required");
            string[] ids = reader.Strings("conditions"); Array.Sort(ids, StringComparer.Ordinal);
            if (ids.Length == 0) throw reader.Error("condition group must be nonempty");
            try
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    ContractIdentity.ValidateId(ids[i], "condition dependency");
                    if (i > 0 && ids[i] == ids[i - 1]) throw reader.Error("duplicate condition dependency");
                }
            }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            ConditionIds = Array.AsReadOnly(ids);
        }
        protected override void ResolveCore(ContentResolver resolver)
        {
            var conditions = new List<IGameCondition>();
            foreach (string id in ConditionIds) conditions.Add(resolver.Def<IGameCondition>(id));
            _conditions = conditions.AsReadOnly();
        }
        public override void ValidateBinding(World world, PlayerId company)
        {
            if (!world.Players.IsSeat(company) || !world.ViewFeatures.ContainsKey(typeof(GameConditionsView)))
                throw new ContentException(Id + ": composite condition requires actual company and condition view");
            foreach (var condition in _conditions)
            {
                GameConditionContracts.ValidateBinding(world, company, condition);
            }
        }
        public override Code Check(GameConditionContext context)
        {
            Code first = Code.None;
            foreach (var condition in _conditions)
            {
                Code blocker = context.Check(condition);
                if (_all && !blocker.IsNone || !_all && blocker.IsNone) return blocker;
                if (first.IsNone) first = blocker;
            }
            return _all ? Code.None : first;
        }
    }
    public sealed class SignalGameConditionDef : GameConditionDef
    {
        private string _entity;
        private EntityDef _definition;
        private bool _all;
        public IReadOnlyList<SignalGateCondition> Conditions { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _entity = reader.String("entity"); string mode = reader.String("mode");
            if (mode != "all" && mode != "any") throw reader.Error("condition target mode must be all or any");
            _all = mode == "all"; Conditions = SignalGateCondition.ReadAll(reader);
        }
        protected override void ResolveCore(ContentResolver content)
        {
            _definition = content.Def<EntityDef>(_entity);
            foreach (var condition in Conditions) condition.Resolve(content);
        }
        public override void ValidateBinding(World world, PlayerId company)
        {
            if (!world.Content.Owns(_definition) || world.ViewFor(company).Feature<OperationsView>() == null)
                throw new ContentException(Id + ": game signal condition needs actual target and selected operation view");
            foreach (var condition in Conditions) condition.Validate(_definition);
        }
        public override Code Check(GameConditionContext context)
        {
            var view = context.View;
            var entities = new List<EntityView>(); view.CopyEntities(view.Viewer, entities);
            entities.Sort((a, b) => a.Id.CompareTo(b.Id));
            bool found = false; Code first = Code.None;
            foreach (var entity in entities)
                if (entity.Def == _definition.Id)
                {
                    found = true; Code blocker = Code.None;
                    foreach (var fact in SignalGateCondition.ReadFromView(view.Feature<OperationsView>(), entity.Id, Conditions))
                        if (blocker.IsNone && !fact.Ready) blocker = fact.Blocker;
                    if (_all && !blocker.IsNone) return blocker;
                    if (!_all && blocker.IsNone) return Code.None;
                    if (first.IsNone) first = blocker;
                }
            return !found ? new Code("condition.no-target") : _all ? Code.None : first;
        }
    }
    internal sealed class GameConditionContracts
    {
        private readonly World _world;
        private GameConditionContext _active;
        internal GameConditionContracts(World world) { _world = world; }
        internal static void ValidateBinding(World world, PlayerId company, IGameCondition condition)
        {
            if (!world.Services.Has<GameConditionContracts>() || condition == null || !world.Content.Owns(condition) ||
                !ReferenceEquals(world.Content.Find<IGameCondition>(condition.Id), condition) || !world.Players.IsSeat(company))
                throw new ContentException("condition binding requires selected module, actual content and company");
            EntityObservation.ReadProvider(world, condition, null, () => {
                condition.ValidateBinding(world, company); return true;
            }, faultPhase: "condition.observation");
        }
        internal static Code Read(World world, PlayerView view, IGameCondition condition)
        {
            if (view == null || view.IsObserver || !world.Players.IsSeat(view.Viewer)) return Codes.NotSeated;
            return world.Services.Get<GameConditionContracts>().Observe(view, condition);
        }
        private Code Observe(PlayerView view, IGameCondition condition)
        {
            if (_active != null)
            {
                if (view.Viewer != _active.View.Viewer)
                {
                    var error = new InvalidOperationException("condition invocation cannot change private company");
                    _world.LatchFault("condition.observation", error); throw error;
                }
                return _active.Check(condition);
            }
            var context = new GameConditionContext(this, view); _active = context;
            try
            {
                Code result = context.Check(condition); _world.RequireHealthyUpdate(); return result;
            }
            catch (Exception error) { _world.LatchFault("condition.observation", error); throw; }
            finally { context.Close(); _active = null; }
        }
        internal Code ReadChild(GameConditionContext context, IGameCondition condition)
        {
            try
            {
                if (!ReferenceEquals(context, _active)) throw new InvalidOperationException("foreign condition invocation");
                if (condition == null || !_world.Content.Owns(condition) ||
                    !ReferenceEquals(_world.Content.Find<IGameCondition>(condition.Id), condition))
                    throw new ArgumentException("game condition must be an unambiguous actual session content alias");
                if (!context.View.CanSeeContent(condition)) return Codes.UnknownDef;
                context.Enter(condition.Id);
                try
                {
                    return EntityObservation.ReadProvider(_world, condition, null, () => {
                        Code result = condition.Check(context);
                        if (!result.IsNone) ContractIdentity.ValidateId(result.Id, "game condition blocker");
                        _world.RequireHealthyUpdate(); return result;
                    }, faultPhase: "condition.observation");
                }
                finally { context.Leave(condition.Id); }
            }
            catch (Exception error) { _world.LatchFault("condition.observation", error); throw; }
        }
    }
}

