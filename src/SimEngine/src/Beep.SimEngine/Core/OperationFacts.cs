using System;

namespace Beep.SimEngine
{
    public enum OperationStatus : byte { Invalid = 0, Idle = 1, Running = 2, Blocked = 3, Disabled = 4 }

    /// <summary>Provider-owned operating facts, never an independent entity state machine.</summary>
    public readonly struct OperationSnapshot
    {
        public OperationStatus Status { get; }
        public Code Blocker { get; }
        public OperationSnapshot(OperationStatus status, Code blocker)
        {
            if (status < OperationStatus.Idle || status > OperationStatus.Disabled)
                throw new ArgumentOutOfRangeException(nameof(status));
            if ((status == OperationStatus.Blocked || status == OperationStatus.Disabled) == blocker.IsNone)
                throw new ArgumentException("blocked/disabled operations need a blocker; idle/running operations have none", nameof(blocker));
            Status = status; Blocker = blocker;
        }
        internal bool IsValid => Status >= OperationStatus.Idle && Status <= OperationStatus.Disabled
            && ((Status == OperationStatus.Blocked || Status == OperationStatus.Disabled) != Blocker.IsNone);
    }

    public readonly struct OperationControlSnapshot
    {
        public bool Enabled { get; }
        public Fix Target { get; }
        public Fix Minimum { get; }
        public Fix Maximum { get; }
        public Code Unit { get; }
        public OperationControlSnapshot(bool enabled, Fix target, Fix minimum, Fix maximum, Code unit)
        {
            if (minimum > maximum || target < minimum || target > maximum) throw new ArgumentOutOfRangeException(nameof(target));
            if (unit.IsNone) throw new ArgumentException("operation target needs an authored unit", nameof(unit));
            Enabled = enabled; Target = target; Minimum = minimum; Maximum = maximum; Unit = unit;
        }
    }

    public enum AutomationSignalKind : byte { Invalid = 0, Number = 1, Boolean = 2 }

    /// <summary>Typed, detached signal fact; unavailable is never a fabricated numeric zero.</summary>
    public readonly struct AutomationSignalSnapshot
    {
        public string Signal { get; }
        public AutomationSignalKind Kind { get; }
        public Code Unit { get; }
        public Fix? Value { get; }
        public Code Blocker { get; }
        public bool Available => Value.HasValue;
        public AutomationSignalSnapshot(string signal, AutomationSignalKind kind, Code unit, Fix? value, Code blocker)
        {
            if (string.IsNullOrEmpty(signal)) throw new ArgumentException("a signal needs an identity", nameof(signal));
            if (kind != AutomationSignalKind.Number && kind != AutomationSignalKind.Boolean) throw new ArgumentOutOfRangeException(nameof(kind));
            if (unit.IsNone || value.HasValue == !blocker.IsNone) throw new ArgumentException("signal requires a unit and either a value or blocker");
            if (kind == AutomationSignalKind.Boolean && value.HasValue && value != Fix.Zero && value != Fix.One)
                throw new ArgumentException("boolean signals must be zero or one", nameof(value));
            Signal = signal; Kind = kind; Unit = unit; Value = value; Blocker = blocker;
        }
    }
}
