using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public readonly struct ControlThresholdBand
    {
        public ControlThresholdBand(Code unit, Fix on, Fix off)
        {
            ContractIdentity.ValidateId(unit.Id, "threshold unit");
            Unit = unit; On = on; Off = off;
        }
        public Code Unit { get; }
        public Fix On { get; }
        public Fix Off { get; }
        internal static ControlThresholdBand Sync(IStateSync sync, ControlThresholdBand current)
        {
            string unit = current.Unit.Id; Fix on = current.On, off = current.Off;
            sync.String(ref unit); sync.Fix(ref on); sync.Fix(ref off);
            try { return sync.Reading ? new ControlThresholdBand(new Code(unit), on, off) : current; }
            catch (ArgumentException ex) { throw new CorruptStateException("invalid threshold band: " + ex.Message); }
        }
    }

    public interface IControlThresholdDefinition
    {
        string Input { get; }
        bool Above { get; }
        ControlThresholdBand Initial { get; }
    }

    public sealed class ControlThresholdDefinition : IControlThresholdDefinition
    {
        public ControlThresholdDefinition(string input, bool above, ControlThresholdBand initial)
        {
            ContractIdentity.ValidateId(input, "threshold input");
            if (initial.Unit.IsNone || (above ? initial.On < initial.Off : initial.On > initial.Off))
                throw new ArgumentException("threshold must declare its unit and ordered hysteresis band");
            Input = input; Above = above; Initial = initial;
        }
        public string Input { get; }
        public bool Above { get; }
        public ControlThresholdBand Initial { get; }
    }

    /// <summary>Stateless validation/projection of actual rule declarations, never a parameter store.</summary>
    public static class ControlThresholds
    {
        public static IReadOnlyDictionary<string, ControlThresholdBand> Initial(IControlRule rule)
        {
            if (rule == null || rule.Thresholds == null) throw new ArgumentException("declared thresholds required");
            var values = new SortedDictionary<string, ControlThresholdBand>(StringComparer.Ordinal);
            foreach (var entry in rule.Thresholds) values.Add(entry.Key, entry.Value.Initial);
            return AutomationFacts.Copy(values);
        }

        public static Code Check(IControlRule rule, IReadOnlyDictionary<string, ControlThresholdBand> values)
        {
            if (rule == null || rule.Thresholds == null || values == null || values.Count != rule.Thresholds.Count)
                return AutomationCodes.InvalidSettings;
            foreach (var entry in rule.Thresholds)
            {
                if (entry.Value == null || !values.TryGetValue(entry.Key, out var band)) return AutomationCodes.InvalidSettings;
                if (band.Unit != entry.Value.Initial.Unit) return AutomationCodes.UnitMismatch;
                if (entry.Value.Above ? band.On < band.Off : band.On > band.Off) return AutomationCodes.InvalidSettings;
            }
            return Code.None;
        }
    }
}
