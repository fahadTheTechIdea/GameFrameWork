using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>One JSON rule declaration and evaluator, shared by native and independent content definitions.</summary>
    public sealed class ControlRuleDeclaration
    {
        private IReadOnlyList<ControlSignalInput> _inputs;
        private IReadOnlyDictionary<string, bool> _initial;
        private IReadOnlyDictionary<string, IControlThresholdDefinition> _thresholds;
        public IReadOnlyDictionary<string, IControlThresholdDefinition> Thresholds => _thresholds;
        private bool _initialLatch;
        private GameDuration _dwell, _cooldown;
        private ControlAction _true, _false;
        private string[] _inputIds, _signalIds;
        private string _root;
        private string _inputRecovery;
        private string _actionPolicy;
        private IControlActionPolicy _actions;
        public IControlActionPolicy ActionPolicy => _actions;
        private IControlInputRecoveryPolicy _recovery;
        public IControlInputRecoveryPolicy InputRecovery => _recovery;
        private DependencyGraph<Condition> _graph;
        public IReadOnlyList<ControlSignalInput> Inputs => _inputs;
        public IReadOnlyDictionary<string, bool> InitialState => _initial;
        public bool InitialLatch => _initialLatch;
        public GameDuration Dwell => _dwell;
        public GameDuration Cooldown => _cooldown;
        public ControlAction WhenTrue => _true;
        public ControlAction WhenFalse => _false;
        public ControlRuleArbitration Arbitration => ControlRuleArbitration.Exclusive;

        public void Read(JsonReader reader)
        {
            _inputRecovery = reader.String("input_recovery"); ValidateId(reader, _inputRecovery);
            _actionPolicy = reader.String("action_policy"); ValidateId(reader, _actionPolicy);
            if (reader.String("arbitration") != "exclusive") throw reader.Error("authored arbitration must be exclusive");
            _initialLatch = Boolean(reader, "initial_latch");
            _dwell = Duration(reader, "dwell"); _cooldown = Duration(reader, "cooldown");
            _true = Action(reader, "when_true"); _false = Action(reader, "when_false");
            JsonValue inputs = reader.Array("inputs");
            if (inputs == null || inputs.Count == 0) throw reader.Error("inputs must be explicitly authored and nonempty");
            _inputIds = new string[inputs.Count]; _signalIds = new string[inputs.Count]; var names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < inputs.Count; i++)
            {
                var row = new JsonReader(inputs[i], reader.Where + ".inputs[" + i + "]");
                _inputIds[i] = row.String("id"); _signalIds[i] = row.String("signal"); row.End();
                ValidateId(row, _inputIds[i]); ValidateId(row, _signalIds[i]);
                if (!names.Add(_inputIds[i])) throw row.Error("duplicate input identity");
            }
            JsonValue conditions = reader.Array("conditions");
            if (conditions == null || conditions.Count == 0) throw reader.Error("conditions must be explicitly authored and nonempty");
            var nodes = new List<Condition>();
            for (int i = 0; i < conditions.Count; i++) nodes.Add(new Condition(new JsonReader(conditions[i], reader.Where + ".conditions[" + i + "]")));
            _graph = new DependencyGraph<Condition>(nodes, reader.Where);
            _root = reader.String("root"); ValidateId(reader, _root);
            if (_graph.Find(_root) == null) throw reader.Error("missing root condition");
            var reached = new HashSet<string>(StringComparer.Ordinal); var pending = new Stack<string>(); pending.Push(_root);
            while (pending.Count > 0)
            {
                string id = pending.Pop(); if (!reached.Add(id)) continue;
                foreach (string child in _graph.Dependencies(id)) pending.Push(child);
            }
            if (reached.Count != nodes.Count) throw reader.Error("every condition must contribute to the root");
            var used = new HashSet<string>(StringComparer.Ordinal); var initial = new SortedDictionary<string, bool>(StringComparer.Ordinal);
            var thresholds = new SortedDictionary<string, IControlThresholdDefinition>(StringComparer.Ordinal);
            foreach (Condition condition in _graph.Nodes)
            {
                initial.Add(condition.Id, condition.Initial);
                if (condition.Kind == "above" || condition.Kind == "below")
                    thresholds.Add(condition.Id, new ControlThresholdDefinition(condition.Input, condition.Kind == "above",
                        new ControlThresholdBand(condition.Unit, condition.On, condition.Off)));
                if (condition.Input != null)
                {
                    if (!names.Contains(condition.Input)) throw reader.Error("unknown input '" + condition.Input + "'");
                    used.Add(condition.Input);
                }
            }
            if (used.Count != names.Count) throw reader.Error("every input must contribute to a condition");
            _initial = AutomationFacts.Copy(initial);
            _thresholds = AutomationFacts.Copy(thresholds);
        }

        public void Resolve(ContentResolver content)
        {
            _recovery = content.Def<IControlInputRecoveryPolicy>(_inputRecovery);
            _actions = content.Def<IControlActionPolicy>(_actionPolicy);
            var inputs = new List<ControlSignalInput>(); var signals = new Dictionary<string, IAutomationSignalContent>(StringComparer.Ordinal);
            for (int i = 0; i < _inputIds.Length; i++)
            {
                IAutomationSignalContent signal = content.Def<IAutomationSignalContent>(_signalIds[i]);
                inputs.Add(new ControlSignalInput(_inputIds[i], signal)); signals.Add(_inputIds[i], signal);
            }
            foreach (Condition condition in _graph.Nodes)
            {
                if (condition.Input == null) continue;
                IAutomationSignalContent signal = signals[condition.Input];
                if (condition.Kind == "boolean" ? signal.Kind != AutomationSignalKind.Boolean
                    : signal.Kind != AutomationSignalKind.Number || signal.Unit != condition.Unit)
                    throw content.Fail("condition '" + condition.Id + "' disagrees with its signal kind/unit");
            }
            inputs.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); _inputs = inputs.AsReadOnly();
        }

        public ControlEvaluation Evaluate(ControlEvaluationContext context)
        {
            var state = new SortedDictionary<string, bool>(StringComparer.Ordinal);
            foreach (Condition node in _graph.Nodes)
            {
                bool value;
                if (node.Input != null)
                {
                    AutomationSignalSnapshot signal = context.Signals[node.Input];
                    if (!signal.Available) return new ControlEvaluation(false, context.PreviousState, signal.Blocker);
                    if (node.Kind == "boolean") value = signal.Value.Value == (node.Expected ? Fix.One : Fix.Zero);
                    else
                    {
                        ControlThresholdBand band = context.Thresholds[node.Id];
                        Fix threshold = context.PreviousState[node.Id] ? band.Off : band.On;
                        value = node.Kind == "above" ? signal.Value.Value > threshold : signal.Value.Value < threshold;
                    }
                }
                else
                {
                    value = node.Kind == "all";
                    foreach (string child in _graph.Dependencies(node.Id))
                        value = node.Kind == "all" ? value & state[child] : value | state[child];
                }
                state.Add(node.Id, value);
            }
            return new ControlEvaluation(state[_root], state, Code.None);
        }

        private sealed class Condition : IDependencyNode
        {
            public Condition(JsonReader reader)
            {
                Id = reader.String("id"); ValidateId(reader, Id); Kind = reader.String("kind"); Initial = Boolean(reader, "initial");
                if (Kind == "all" || Kind == "any")
                {
                    JsonValue children = reader.Array("conditions");
                    if (children == null || children.Count == 0) throw reader.Error("logical conditions must be nonempty");
                    Dependencies = Array.AsReadOnly(reader.Strings("conditions"));
                }
                else if (Kind == "boolean" || Kind == "above" || Kind == "below")
                {
                    Dependencies = Array.Empty<string>(); Input = reader.String("input"); ValidateId(reader, Input);
                    if (Kind == "boolean") Expected = Boolean(reader, "expected");
                    else
                    {
                        Unit = new Code(reader.String("unit")); ValidateId(reader, Unit.Id);
                        if (!reader.Has("on") || !reader.Has("off")) throw reader.Error("on/off thresholds must be explicit");
                        On = reader.Fix("on"); Off = reader.Fix("off");
                        if (Kind == "above" ? On < Off : On > Off) throw reader.Error("hysteresis band is reversed");
                    }
                }
                else throw reader.Error("condition kind must be boolean, above, below, all or any");
                reader.End();
            }
            public string Id { get; }
            public string Kind { get; }
            public string Input { get; }
            public Code Unit { get; }
            public bool Initial { get; }
            public bool Expected { get; }
            public Fix On { get; }
            public Fix Off { get; }
            public IReadOnlyList<string> Dependencies { get; }
        }

        internal static bool Boolean(JsonReader reader, string key)
        {
            JsonValue value = reader.Value(key);
            if (value == null || value.Kind != JsonKind.Bool) throw reader.Error(key + " must be an explicit boolean");
            return value.Bool;
        }
        private static void ValidateId(JsonReader reader, string id)
        { try { ContractIdentity.ValidateId(id, "control identity"); } catch (ArgumentException ex) { throw reader.Error(ex.Message); } }
        private static GameDuration Duration(JsonReader reader, string key)
            => (reader.Child(key) ?? throw reader.Error("missing duration " + key)).RationalDuration();
        private static ControlAction Action(JsonReader reader, string key)
        {
            JsonReader row = reader.Child(key) ?? throw reader.Error("missing action " + key);
            return ControlAction.Read(row);
        }
    }

    /// <summary>Native registration of the shared rule declaration, not a required mod base.</summary>
    public sealed class AuthoredControlRuleDef : Def, IControlRule
    {
        private readonly ControlRuleDeclaration _declaration = new ControlRuleDeclaration();
        public IControlInputRecoveryPolicy InputRecovery => _declaration.InputRecovery;
        public IControlActionPolicy ActionPolicy => _declaration.ActionPolicy;
        public IReadOnlyList<ControlSignalInput> Inputs => _declaration.Inputs;
        public IReadOnlyDictionary<string, IControlThresholdDefinition> Thresholds => _declaration.Thresholds;
        public IReadOnlyDictionary<string, bool> InitialState => _declaration.InitialState;
        public bool InitialLatch => _declaration.InitialLatch;
        public GameDuration Dwell => _declaration.Dwell;
        public GameDuration Cooldown => _declaration.Cooldown;
        public ControlAction WhenTrue => _declaration.WhenTrue;
        public ControlAction WhenFalse => _declaration.WhenFalse;
        public ControlRuleArbitration Arbitration => _declaration.Arbitration;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public ControlEvaluation Evaluate(ControlEvaluationContext context) => _declaration.Evaluate(context);
    }
}
