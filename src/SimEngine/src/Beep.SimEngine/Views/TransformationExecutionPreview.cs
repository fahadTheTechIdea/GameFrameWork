using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    /// <summary>Read-only projection. The original named processor remains the execution authority.</summary>
    public interface ITransformationExecutionView
    {
        Code ReadExecutionPreview(EntityId entity, Code capability, string transformation, out TransformationExecutionPreview facts);
    }
    public enum ProductiveEstimateStatus : byte
    {
        NotHead, Continuous, Waiting, Blocked, Paused, ZeroRate, OutOfRange, Conditional, Returning
    }
    /// <summary>Detached yields and current execution. Productive ETA excludes supply/settlement and assumes unchanged rate/gates.</summary>
    public sealed class TransformationExecutionPreview
    {
        internal TransformationExecutionPreview(ITransformationDefinition definition, OperationControlSnapshot control,
            TransformationSnapshot current, ExecutionGateEvaluation gates, MonetaryCostDecision cost,
            long observedStep, bool clockPaused, GameDuration? nominalCycle, GameDuration? remainingWork,
            long? completionStep, ProductiveEstimateStatus status)
        {
            Transformation = definition.Id; Mode = definition.Mode; Inputs = definition.Inputs; Outputs = definition.Outputs;
            EntityOutput = definition.EntityOutput?.Id;
            OutputHome = definition.OutputHome;
            Tools = definition.Tools; ToolConsumption = definition.ToolConsumption; AuthoredDuration = definition.Duration;
            InputRates = CopyRates(definition.InputRates); OutputRates = CopyRates(definition.OutputRates);
            OperatingRates = CopyRates(definition.OperatingInputs); ToolConsumptionRates = CopyRates(definition.ToolConsumptionRates);
            Control = control; Current = current; Gates = gates; MonetaryCost = cost;
            ObservedStep = observedStep; ClockPaused = clockPaused; NominalCycle = nominalCycle;
            RemainingProductiveTime = remainingWork; ProductiveCompletionStep = completionStep; EstimateStatus = status;
        }
        private static IReadOnlyList<ResourceRateInfo> CopyRates(IReadOnlyList<IResourceRate> rates)
        {
            var copy = new ResourceRateInfo[rates.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = new ResourceRateInfo(rates[i]);
            return Array.AsReadOnly(copy);
        }
        public string Transformation { get; }
        public TransformationMode Mode { get; }
        public Bill Inputs { get; }
        public Bill Outputs { get; }
        /// <summary>Authored single-entity batch yield, not a placed result or the current queue head.</summary>
        public string EntityOutput { get; }
        public TransformationOutputHome? OutputHome { get; }
        public Bill Tools { get; }
        public Bill ToolConsumption { get; }
        public IReadOnlyList<ResourceRateInfo> InputRates { get; }
        public IReadOnlyList<ResourceRateInfo> OutputRates { get; }
        public IReadOnlyList<ResourceRateInfo> OperatingRates { get; }
        public IReadOnlyList<ResourceRateInfo> ToolConsumptionRates { get; }
        public GameDuration AuthoredDuration { get; }
        public OperationControlSnapshot Control { get; }
        public TransformationSnapshot Current { get; }
        public ExecutionGateEvaluation Gates { get; }
        public MonetaryCostDecision MonetaryCost { get; }
        public long ObservedStep { get; }
        public bool ClockPaused { get; }
        public bool IsHead => Current.Transformation == Transformation;
        // Batch throughput: Outputs per NominalCycle. Continuous: OutputRates times Control.Target.
        // Native packing and blockers can reduce delivered throughput; no extra remainder is calculated here.
        public GameDuration? NominalCycle { get; }
        public GameDuration? RemainingProductiveTime { get; }
        public long? ProductiveCompletionStep { get; }
        public ProductiveEstimateStatus EstimateStatus { get; }
        public Code? CurrentBlocker => !IsHead ? (Code?)null
            : Current.Phase == TransformationPhase.Returning ? Current.Blocker
            : !Control.Enabled ? OperationCodes.Disabled
            : !Gates.Blocker.IsNone ? Gates.Blocker : Current.Blocker;
    }
}

