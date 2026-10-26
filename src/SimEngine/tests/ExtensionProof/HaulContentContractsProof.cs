using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentHaulLoadProfileDef : Def, IHaulLoadProfile
    {
        private HaulLoadProfileDeclaration _declaration;
        public HaulLoadSettings Initial => _declaration.Initial;
        protected override void ReadCore(JsonReader reader) => _declaration=HaulLoadProfileDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content,Id);
        public Code CheckLoad(HaulLoadContext context,HaulLoadSettings settings) => _declaration.CheckLoad(context,settings);
    }
    public sealed class IndependentHaulTripProfileDef : Def, IHaulTripProfile
    {
        private Action _observationProbe;
        private bool _probing, _observingSources;
        private int _sourceReads;
        public int RuleFault { get; private set; }
        public int SourcesFault { get; private set; }
        internal int Observe(Action probe, Action query, bool sources = false)
        {
            if (_observationProbe != null) throw new InvalidOperationException("trip rule probe already active");
            _observationProbe = probe; _observingSources = sources; _sourceReads = 0;
            try { query(); return _sourceReads; } finally { _observationProbe = null; _observingSources = false; }
        }
        private HaulTripProfileDeclaration _declaration;
        public HaulTripSettings Initial => _declaration.Initial;
        public IReadOnlyList<Code> ServiceSources
        {
            get
            {
                if (_observingSources) { _sourceReads++; Probe(); }
                return _declaration.ServiceSources;
            }
        }
        private void Probe()
        {
            if (_observationProbe == null || _probing) return;
            _probing = true; try { _observationProbe(); } finally { _probing = false; }
        }
        protected override void ReadCore(JsonReader reader)
        {
            RuleFault = reader.Int("rule_fault", 0); if (RuleFault < 0 || RuleFault > 6) throw reader.Error("unknown trip rule probe");
            SourcesFault = reader.Int("sources_fault", 0); if (SourcesFault < 0 || SourcesFault > 6) throw reader.Error("unknown trip sources probe");
            _declaration=HaulTripProfileDeclaration.Read(reader,Id);
        }
        public Code CheckTrip(HaulTripEvaluation context)
        {
            if (!_observingSources) Probe();
            return _declaration.CheckTrip(context);
        }
    }
    // Content registration and diagnostic Initialize only; native loading/trip decisions keep their sole owners.
    public sealed class HaulContractModule : GameModule
    {
        public const string ModuleId="proof.haul-contracts";
        private readonly InitializeProbe _initialize;
        public HaulContractModule(InitializeProbe initialize) { _initialize=initialize; }
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1,0,0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[]{new ModuleRequirement(LogisticsModule.ModuleId,Version)};
        public bool IndependentProfiles { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {types.Section<IndependentHaulLoadProfileDef>("independent_haul_load_profiles");types.Section<IndependentHaulTripProfileDef>("independent_haul_trip_profiles");}
        public override void Compose(WorldComposer composer)
        {
            var load=composer.Content.Get<IHaulLoadProfile>("contract.load");var trip=composer.Content.Get<IHaulTripProfile>("contract.trip");
            IndependentProfiles=!(load is AuthoredHaulLoadProfileDef)&&!(trip is AuthoredHaulTripProfileDef)&&composer.Content.Owns(load)&&composer.Content.Owns(trip);
            composer.AddSystem(_initialize,301);
        }
    }
}
