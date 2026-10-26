using System;
using System.Collections.Generic;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Detached declared provider identity, never a registration or executable provider.</summary>
    public readonly struct AutomationProviderSnapshot
    {
        internal AutomationProviderSnapshot(AutomationSignalProviderSchema provider)
        { Component = provider.StateCodec.Id; SchemaVersion = provider.Version; }
        public string Component { get; }
        public int SchemaVersion { get; }
    }

    public sealed class AutomationInputSnapshot
    {
        internal AutomationInputSnapshot(ControlSignalInput input)
        {
            Id = input.Id; Signal = input.Signal.Id; Kind = input.Signal.Kind;
            Metric = input.Signal.Metric; Unit = input.Signal.Unit;
            var providers = new List<AutomationProviderSnapshot>();
            foreach (var provider in input.Signal.Providers) providers.Add(new AutomationProviderSnapshot(provider));
            providers.Sort((a, b) => string.CompareOrdinal(a.Component, b.Component));
            Providers = providers.AsReadOnly();
        }
        public string Id { get; }
        public string Signal { get; }
        public AutomationSignalKind Kind { get; }
        public Code Metric { get; }
        public Code Unit { get; }
        public IReadOnlyList<AutomationProviderSnapshot> Providers { get; }
    }

    /// <summary>One immutable authored profile projection; no runtime settings or content handles.</summary>
    public sealed class AutomationProfileSnapshot
    {
        internal AutomationProfileSnapshot(IControlRule rule)
        {
            Id = rule.Id; InputRecovery = rule.InputRecovery.Id;
            ActionPolicy = rule.ActionPolicy.Id; ActionCommand = rule.ActionPolicy.CommandId; ActionCommandOwner = rule.ActionPolicy.CommandOwner;
            TargetKind = rule.ActionPolicy.TargetKind;
            InitialLatch = rule.InitialLatch; InitialState = AutomationFacts.Copy(rule.InitialState);
            Dwell = rule.Dwell; Cooldown = rule.Cooldown;
            WhenTrue = rule.WhenTrue; WhenFalse = rule.WhenFalse; Arbitration = rule.Arbitration;
            var inputs = new List<AutomationInputSnapshot>();
            foreach (var input in rule.Inputs) inputs.Add(new AutomationInputSnapshot(input));
            inputs.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); Inputs = inputs.AsReadOnly();
            var thresholds = new SortedDictionary<string, ControlThresholdDefinition>(StringComparer.Ordinal);
            foreach (var row in rule.Thresholds)
                thresholds.Add(row.Key, new ControlThresholdDefinition(row.Value.Input, row.Value.Above, row.Value.Initial));
            Thresholds = AutomationFacts.Copy(thresholds);
        }
        public string Id { get; }
        public string InputRecovery { get; }
        public string ActionPolicy { get; }
        public string ActionCommand { get; }
        public string ActionCommandOwner { get; }
        public ControlTargetKind TargetKind { get; }
        public IReadOnlyList<AutomationInputSnapshot> Inputs { get; }
        public IReadOnlyDictionary<string, ControlThresholdDefinition> Thresholds { get; }
        public IReadOnlyDictionary<string, bool> InitialState { get; }
        public bool InitialLatch { get; }
        public GameDuration Dwell { get; }
        public GameDuration Cooldown { get; }
        public ControlAction WhenTrue { get; }
        public ControlAction WhenFalse { get; }
        public ControlRuleArbitration Arbitration { get; }

        // The same complete dependency disclosure gate serves discovery and configure admission.
        // Saved existing history still uses structural validation, not a new discovery permission.
        internal static bool VisibleTo(World world, PlayerId viewer, IControlRule rule)
        {
            world.RequireHealthyUpdate();
            if (viewer != PlayerId.None && !world.Players.IsSeat(viewer)) return false;
            if (rule == null) return false;
            return EntityObservation.ReadProvider(world, rule, null, () => VisibleCore(world, viewer, rule));
        }
        private static bool VisibleCore(World world, PlayerId viewer, IControlRule rule)
        {
            PlayerView view = world.ViewFor(viewer);
            if (rule == null || !view.CanSeeContent(rule)
                || !view.CanSeeContent(rule.InputRecovery)
                || !view.CanSeeContent(rule.ActionPolicy)) return false;
            foreach (var input in rule.Inputs) if (!view.CanSeeContent(input.Signal)) return false;
            foreach (var dependency in rule.ActionPolicy.ContentDependencies) if (!view.CanSeeContent(dependency)) return false;
            return true;
        }
    }
}
