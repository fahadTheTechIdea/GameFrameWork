using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    public enum SensorObservationStage { Coverage, Sight }
    public enum SensorObservationFault { Sibling, Throw, Clock, SiblingThrow }
    public enum SensorTargetCaptureFault { Null, Foreign, Blank, Throw, TouchThrow, SiblingThrow, Sibling }

    // Transient negative instrumentation of the actual target provider, not another spatial implementation.
    public sealed partial class PointPresence
    {
        private SensorTargetCaptureFault? _visibilityCaptureFault;
        public int VisibilityCaptureCalls { get; private set; }
        public void ArmVisibilityCapture(SensorTargetCaptureFault fault)
        { _visibilityCaptureFault = fault; VisibilityCaptureCalls = 0; }
        private bool ProbeVisibilityCapture(out SpatialSnapshot value)
        {
            value = null;
            if (!_visibilityCaptureFault.HasValue) return false;
            VisibilityCaptureCalls++;
            var fault = _visibilityCaptureFault.Value;
            if (fault == SensorTargetCaptureFault.Null) return true;
            if (fault == SensorTargetCaptureFault.Foreign || fault == SensorTargetCaptureFault.Blank)
            { value = new PointSnapshot(fault == SensorTargetCaptureFault.Foreign ? "foreign.space" : "", _position, _previous); return true; }
            if (fault == SensorTargetCaptureFault.TouchThrow) Entity.Touch();
            if (fault == SensorTargetCaptureFault.Sibling || fault == SensorTargetCaptureFault.SiblingThrow)
                Entity.Get<CompletionProbe>(new Code("sensor.observation-probe")).MutatePrivate();
            if (fault != SensorTargetCaptureFault.Sibling)
                throw new InvalidOperationException("proof.sensor-target-capture-first-error");
            return false;
        }
    }

    // Negative instrumentation only. All positive sight/control/state remain in the original sensor.
    public sealed partial class PointSensor
    {
        private SensorObservationStage? _observationStage;
        private SensorObservationFault _observationFault;
        public int ObservationCalls { get; private set; }
        public void ArmObservation(SensorObservationStage stage, SensorObservationFault fault)
        { _observationStage = stage; _observationFault = fault; ObservationCalls = 0; }
        private void ProbeObservation(World world, SensorObservationStage stage)
        {
            if (_observationStage != stage) return;
            ObservationCalls++;
            if (_observationFault == SensorObservationFault.Sibling || _observationFault == SensorObservationFault.SiblingThrow)
                Entity.Get<CompletionProbe>(new Code("sensor.observation-probe")).MutatePrivate();
            if (_observationFault == SensorObservationFault.Clock) ((GameClock)world.Clock).Pause();
            if (_observationFault == SensorObservationFault.Throw || _observationFault == SensorObservationFault.SiblingThrow)
                throw new InvalidOperationException("proof.sensor-first-error");
        }
    }

    public sealed class SensorObservationProofModule : GameModule
    {
        public override string Id => "proof.sensor-observation";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(PointSensorModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Component<CompletionProbeDef>("sensor_sibling_probe", 1);
    }
}
