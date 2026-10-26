using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Authored negative instrumentation only; no learned table, progress, clock or host rule.
    public sealed class LedgerFailureDef : Def
    {
        public string Mode { get; private set; }
        public string Phase { get; private set; }
        public GameDuration ReadAfter { get; private set; }
        protected override void ReadCore(JsonReader r)
        {
            Mode = r.String("mode"); Phase = r.String("phase"); ReadAfter = r.Child("read_after").RationalDuration();
            if (ReadAfter.IsZero || Mode != "throw" && Mode != "clock" && Mode != "clock-throw"
                || Phase != "share" && Phase != "forget" && Phase != "observation" && Phase != "composition")
                throw r.Error("explicit negative ledger phase/mode/duration required");
        }
        public void Check(World world, string phase)
        {
            if (Phase != phase || phase != "composition" && world.Clock.StepIndex < world.Time.Ticks(ReadAfter)) return;
            if (Mode != "throw") ((GameClock)world.Clock).Speed = 2;
            if (Mode != "clock") throw new InvalidOperationException("proof.knowledge-ledger-first-error");
        }
    }
    public sealed class LedgerSharingProbeDef : Def, IKnowledgeSharingPolicy
    {
        private string _failure;
        private LedgerFailureDef _config;
        private KnowledgeSharingDeclaration _declaration;
        public IReadOnlyList<KnowledgeSubjectKey> Subjects => _declaration.Subjects;
        protected override void ReadCore(JsonReader r) { _failure = r.String("failure"); _declaration = KnowledgeSharingDeclaration.Read(r); }
        protected override void ResolveCore(ContentResolver c) => _config = c.Def<LedgerFailureDef>(_failure);
        public void ValidateBinding(World world)
        { _declaration.ValidateBinding(world, Id); _config.Check(world, "composition"); }
        public bool Allows(World world, PlayerId sender, PlayerId recipient)
        { _config.Check(world, "share"); return _declaration.Allows(sender, recipient); }
    }
    public sealed class LedgerForgettingProbeDef : Def, IKnowledgeForgettingPolicy
    {
        private string _failure;
        private LedgerFailureDef _config;
        private KnowledgeForgettingDeclaration _declaration;
        public IReadOnlyList<KnowledgeSubjectKey> Subjects => _declaration.Subjects;
        protected override void ReadCore(JsonReader r) { _failure = r.String("failure"); _declaration = KnowledgeForgettingDeclaration.Read(r); }
        protected override void ResolveCore(ContentResolver c) => _config = c.Def<LedgerFailureDef>(_failure);
        public void ValidateBinding(World world) => _declaration.ValidateBinding(world, Id);
        public bool Allows(World world, PlayerId company)
        { _config.Check(world, "forget"); return _declaration.Allows(company); }
    }
    public sealed class LedgerSubjectProbeDef : Def, IKnowledgeSubjectDefinition
    {
        private string _failure;
        private LedgerFailureDef _config;
        private string _value;
        public KnowledgeRetention Retention => KnowledgeRetention.FirstLearned;
        public GameDuration? FreshFor => null;
        protected override void ReadCore(JsonReader r)
        { _failure = r.String("failure"); _value = r.Child("value").Raw.ToString(); }
        protected override void ResolveCore(ContentResolver c) => _config = c.Def<LedgerFailureDef>(_failure);
        public Code CheckTarget(World world, Entity target) => Code.None;
        public JsonValue Observe(World world, Entity target) => Json.Parse(_value);
        public Code CheckObservation(World world, JsonValue data)
        { _config.Check(world, "observation"); return data.ToString() == _value ? Code.None : Codes.InvalidArgument; }
    }
    public sealed class KnowledgeLedgerDurabilityModule : GameModule
    {
        public override string Id => "proof.knowledge-ledger-durability";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(CompanyKnowledgeModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<LedgerFailureDef>("ledger_failures"); types.Section<LedgerSharingProbeDef>("ledger_sharing_probes");
            types.Section<LedgerForgettingProbeDef>("ledger_forgetting_probes"); types.Section<LedgerSubjectProbeDef>("ledger_subject_probes");
        }
    }
}
