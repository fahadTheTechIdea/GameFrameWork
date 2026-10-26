using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum ControlInputRecoveryAction : byte { Suspend = 1, Wait = 2 }

    /// <summary>Detached facts for an unavailable still-bound signal, never mutable session handles or time.</summary>
    public readonly struct ControlInputFailure
    {
        public string Input { get; }
        public Code Signal { get; }
        public Code Blocker { get; }
        public ControlInputFailure(string input, Code signal, Code blocker)
        {
            ContractIdentity.ValidateId(input, "recovery input");
            if (signal.IsNone || blocker.IsNone) throw new ArgumentException("recovery needs an explicit signal and blocker");
            ContractIdentity.ValidateId(signal.Id, "recovery signal"); ContractIdentity.ValidateId(blocker.Id, "recovery blocker");
            Input = input; Signal = signal; Blocker = blocker;
        }
    }
    public interface IControlInputRecoveryPolicy : IContentDefinition
    {

        ControlInputRecoveryAction Decide(ControlInputFailure failure);
    }
    /// <summary>Exact authored retry classifications. An empty list explicitly selects permanent suspension.</summary>
    public sealed class ControlInputRecoveryDeclaration
    {
        private readonly HashSet<Code> _retry = new HashSet<Code>();
        public IReadOnlyList<Code> RetryBlockers { get; private set; }
        public void Read(JsonReader reader)
        {
            JsonValue values = reader.Array("retry_blockers") ?? throw reader.Error("retry_blockers must be an explicit array");
            var ordered = new List<Code>();
            for (int i = 0; i < values.Count; i++)
            {
                JsonValue value = values[i];
                if (value.Kind != JsonKind.String) throw reader.Error("retry blocker must be an explicit code");
                Code code;
                try { ContractIdentity.ValidateId(value.Text, "retry blocker"); code = new Code(value.Text); }
                catch (ArgumentException ex) { throw reader.Error(ex.Message); }
                if (!_retry.Add(code)) throw reader.Error("duplicate retry blocker"); ordered.Add(code);
            }
            ordered.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); RetryBlockers = ordered.AsReadOnly();
        }
        public ControlInputRecoveryAction Decide(ControlInputFailure failure)
            => _retry.Contains(failure.Blocker) ? ControlInputRecoveryAction.Wait : ControlInputRecoveryAction.Suspend;
    }

    public sealed class AuthoredControlInputRecoveryPolicyDef : Def, IControlInputRecoveryPolicy
    {
        private readonly ControlInputRecoveryDeclaration _declaration = new ControlInputRecoveryDeclaration();
        public IReadOnlyList<Code> RetryBlockers => _declaration.RetryBlockers;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        public ControlInputRecoveryAction Decide(ControlInputFailure failure) => _declaration.Decide(failure);
    }
}
