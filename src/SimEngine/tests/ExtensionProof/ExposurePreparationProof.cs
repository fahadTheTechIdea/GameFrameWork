using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Delegates all positive terrain behavior to the actual engine effect. Hooks are negative diagnostics only.
    public sealed class ExposurePreparationProbeDef : Def, IIncidentExposureEffect
    {
        private string _effectId, _fault;
        private IIncidentExposureEffect _effect;
        private World _faultWorld;
        public int PreparationCalls { get; private set; }
        public IReadOnlyList<IContentDefinition> ContentDependencies { get; private set; }
        protected override void ReadCore(JsonReader reader) => _effectId = reader.String("effect");
        protected override void ResolveCore(ContentResolver content)
        { _effect = content.Def<IIncidentExposureEffect>(_effectId); ContentDependencies = Array.AsReadOnly(new IContentDefinition[] { _effect }); }
        public void RequireCapabilities(World world) => _effect.RequireCapabilities(world);
        public Code CheckExposure(IncidentPropagationContext context, Entity target) => _effect.CheckExposure(context, target);
        public void Arm(World world, string fault)
        { _faultWorld = world; _fault = fault; PreparationCalls = 0; }
        public IncidentExposurePreparation PrepareExposure(IncidentPropagationContext context, Entity target)
        {
            PreparationCalls++;
            var prepared = _effect.PrepareExposure(context, target);
            if (!ReferenceEquals(_faultWorld, context.World)) return prepared;
            if (_fault == "target" || _fault == "target-throw" || _fault == "target-refuse") target.Get<ExposurePrivateProbe>().Corrupt();
            if (_fault == "source" || _fault == "source-throw") context.Source.Get<ExposurePrivateProbe>().Corrupt();
            if (_fault == "clock") ((GameClock)context.World.Clock).Pause();
            if (_fault == "throw" || _fault == "target-throw" || _fault == "source-throw") throw new InvalidOperationException("proof.exposure-preparation-first-error");
            if (_fault == "null") return null;
            if (_fault == "target-refuse") return new IncidentExposurePreparation(new Code("proof.exposure-refused"));
            return prepared;
        }
    }
}
