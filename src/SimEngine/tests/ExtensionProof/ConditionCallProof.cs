using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent non-friend contract-call fault probes; no engine scheduler edits.</summary>
    public class ConditionCallProofModule : GameConditionProofModule
    {
        public override void DeclareContent(ContentTypes types)
        { base.DeclareContent(types); types.Section<ConditionCallProbeDef>("condition_call_probes"); }
    }
    public sealed class ConditionCallProbeDef : GameConditionDef
    {
        private string _targetId, _mode;
        private IGameCondition _target;
        // Negative lifetime instrumentation only; never an input to gameplay or serialized state.
        public GameConditionContext Last { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _targetId = reader.String("target"); _mode = reader.String("mode");
            if (_mode != "forward" && _mode != "repeat" && _mode != "alias" &&
                _mode != "swallow" && _mode != "throw" && _mode != "bad-code")
                throw reader.Error("explicit probe mode required");
        }
        protected override void ResolveCore(ContentResolver resolver) => _target = resolver.Def<IGameCondition>(_targetId);
        public override void ValidateBinding(World world, PlayerId company) { }
        public override Code Check(GameConditionContext context)
        {
            Last = context;
            if (_mode == "throw") throw new InvalidOperationException("proof.condition-call");
            if (_mode == "bad-code") return new Code("!bad");
            if (_mode == "alias") return context.View.Feature<GameConditionsView>().CheckCondition(_target.Id);
            if (_mode == "swallow")
            { try { context.Check(_target); } catch (InvalidOperationException) { } return Code.None; }
            Code result = context.Check(_target);
            if (_mode == "repeat")
            {
                Code again = context.Check(_target);
                if (again != result) throw new InvalidOperationException("same pure child changed");
            }
            return result;
        }
    }
}
