using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Negative-only provider: no jobs, resources, scheduling decisions or alternate gameplay owner.
    public sealed class LogisticsProjectionFaultModule : GameModule
    {
        public bool ThrowNextProjection;
        public override string Id => "proof.logistics-projection-fault";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public override void Compose(WorldComposer composer)
            => composer.Services.Bind(new Source(composer.World, this));
        private sealed class Source : IWorkerDispatchSource
        {
            private readonly LogisticsProjectionFaultModule _probe;
            public Source(World world, LogisticsProjectionFaultModule probe) { World = world; _probe = probe; }
            public World World { get; }
            public string DispatchScope => "proof.logistics-projection-fault";
            public Schedule GetDispatchSchedule(ISimulationTime time) => Schedule.EveryTick;
            public void PrepareDispatch() { }
            public void CopyOffers(PlayerId viewer, WorkCandidateMode mode, IList<WorkerDispatchOffer> destination)
            {
                if (!_probe.ThrowNextProjection) return;
                _probe.ThrowNextProjection = false;
                destination.Add(default); // A deliberately invalid private provider prefix must never reach the caller.
                throw new InvalidOperationException("proof.logistics.projection.first");
            }
            public void CopyCandidates(long identity, string entry, WorkCandidateMode mode, IList<WorkerDispatchOffer> destination) { }
            public bool TryAcquire(long identity, string entry, EntityId worker, WorkCandidateMode mode) => false;
        }
    }
}
