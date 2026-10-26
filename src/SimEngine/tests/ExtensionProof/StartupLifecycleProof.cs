using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class StartupLifecycleProfileDef : Def
    {
        public string Phase { get; private set; }
        public string Failure { get; private set; }
        private string _entity;
        public EntityDef Entity { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Phase = reader.NullableString("phase"); Failure = reader.String("failure"); _entity = reader.String("entity");
            if (Phase != null && Phase != "initialize" && Phase != "generate" && Phase != "new-game" ||
                Failure != "none" && Failure != "direct" && Failure != "caught-create" || (Phase == null) != (Failure == "none"))
                throw reader.Error("explicit coherent startup phase/failure required");
        }
        protected override void ResolveCore(ContentResolver content) => Entity = content.Def<EntityDef>(_entity);
    }

    // Per-session diagnostics only: not a clock, gameplay owner, saved counter or alternate lifecycle.
    public sealed class StartupLifecycleSession
    {
        private readonly StartupLifecycleProfileDef _profile;
        internal StartupLifecycleSession(World world, StartupLifecycleProfileDef profile) { World = world; _profile = profile; }
        public World World { get; }
        public int FirstInitialize, LaterInitialize, FirstGeneration, LaterGeneration, FirstNewGame, LaterNewGame, Caught;
        internal void Callback(string phase)
        {
            if (_profile.Phase != phase) return;
            if (_profile.Failure == "direct") throw new InvalidOperationException("proof.startup-original-error");
            try { World.Spawn(_profile.Entity, new PlayerId(0)); }
            catch (InvalidOperationException) { Caught++; }
        }
    }
    internal sealed class StartupLifecycleSystem : GameSystem
    {
        private readonly StartupLifecycleSession _session; private readonly bool _later;
        internal StartupLifecycleSystem(StartupLifecycleSession session, bool later) { _session = session; _later = later; }
        public override string Id => _later ? "proof.startup-later" : "proof.startup-first";
        public override int StateSchemaVersion => 1;
        public override void Initialize(World world)
        {
            if (_later) _session.LaterInitialize++;
            else { _session.FirstInitialize++; _session.Callback("initialize"); }
        }
        public override void OnNewGame(World world)
        {
            if (_later) _session.LaterNewGame++;
            else { _session.FirstNewGame++; _session.Callback("new-game"); }
        }
        public override void Update(World world, TickInfo tick) { }
    }
    internal sealed class StartupLifecycleStage : WorldGenStage
    {
        private readonly StartupLifecycleSession _session; private readonly bool _later;
        internal StartupLifecycleStage(StartupLifecycleSession session, bool later) { _session = session; _later = later; }
        public override string Id => _later ? "proof.startup-later-stage" : "proof.startup-first-stage";
        public override void Generate(World world, ScenarioDef scenario)
        {
            if (_later) _session.LaterGeneration++;
            else { _session.FirstGeneration++; _session.Callback("generate"); }
        }
    }
    public sealed class StartupLifecycleProofModule : GameModule
    {
        public override string Id => "proof.startup-lifecycle";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorldSetupModule.ModuleId, Version) };
        public StartupLifecycleSession Last { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<StartupLifecycleProfileDef>("startup_lifecycle_profiles");
            types.Component<CreationProbeDef>("startup_creation_probe", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            Last = new StartupLifecycleSession(composer.World, composer.Content.Get<StartupLifecycleProfileDef>("startup.lifecycle"));
            composer.AddSystem(new StartupLifecycleSystem(Last, false), -1000000000);
            composer.AddSystem(new StartupLifecycleSystem(Last, true), -999999999);
            composer.AddWorldGen(new StartupLifecycleStage(Last, false), -1000000000);
            composer.AddWorldGen(new StartupLifecycleStage(Last, true), -999999999);
        }
    }
}
