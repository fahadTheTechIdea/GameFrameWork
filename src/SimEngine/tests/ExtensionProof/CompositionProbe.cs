using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>External mutable metadata probe. Only the definition's captured registration is authoritative.</summary>
    public sealed class MutableModule : GameModule
    {
        public string SuppliedId;
        public ModuleVersion SuppliedVersion;
        public IReadOnlyList<ModuleRequirement> SuppliedRequirements = Array.Empty<ModuleRequirement>();
        public Action<ContentTypes> Declare;
        public Action<WorldComposer> Build;
        public int IdReads, VersionReads, RequirementReads;
        public override string Id { get { IdReads++; return SuppliedId; } }
        public override ModuleVersion Version { get { VersionReads++; return SuppliedVersion; } }
        public override IReadOnlyList<ModuleRequirement> Requires { get { RequirementReads++; return SuppliedRequirements; } }
        public override void DeclareContent(ContentTypes types) => Declare?.Invoke(types);
        public override void Compose(WorldComposer composer) => Build?.Invoke(composer);
    }

    /// <summary>Real external module registrations configured by composition tests, never host gameplay.</summary>
    public sealed class ProbeModule : GameModule
    {
        private readonly string _id;
        private readonly ModuleVersion _version;
        private readonly IReadOnlyList<ModuleRequirement> _requires;
        private readonly Action<ContentTypes> _content;
        private readonly Action<WorldComposer> _compose;
        public ProbeModule(string id, ModuleVersion version, ModuleRequirement[] requires = null,
            Action<ContentTypes> content = null, Action<WorldComposer> compose = null)
        {
            _id = id; _version = version;
            _requires = Array.AsReadOnly(requires == null ? Array.Empty<ModuleRequirement>() : (ModuleRequirement[])requires.Clone());
            _content = content; _compose = compose;
        }
        public override string Id => _id;
        public override ModuleVersion Version => _version;
        public override IReadOnlyList<ModuleRequirement> Requires => _requires;
        public override void DeclareContent(ContentTypes types) => _content?.Invoke(types);
        public override void Compose(WorldComposer composer) => _compose?.Invoke(composer);
    }

    public sealed class InitializeProbe : GameSystem
    {
        private readonly string _id;
        public InitializeProbe(string id = "probe.initialize") { _id = id; }
        public override int StateSchemaVersion => 1;
        public int Initializations;
        public override string Id => _id;
        public override void Initialize(World world) => Initializations++;
        public override void Update(World world, TickInfo tick) { }
    }

    public interface IProbeFirst { }
    public interface IProbeSecond { }
    public sealed class ProbeView
    {
        public ProbeView(PlayerId viewer) { Viewer = viewer; }
        public PlayerId Viewer { get; }
    }
    public sealed class OtherProbeView { }
    public sealed class ProbeState : IProbeFirst, IProbeSecond, IPersistentService
    {
        public int StateSchemaVersion => 1;
        public ProbeState(string id) { Id = id; }
        public string Id { get; }
        public Fix Value;
        // Test instrumentation, not simulation state. It measures calls to the sole codec.
        public int SyncCalls;
        public void Sync(IStateSync s) { SyncCalls++; s.Fix(ref Value); }
        // Deliberately unusual equality proves aliases must use instance identity, not Equals.
        public override bool Equals(object obj) => obj is ProbeState;
        public override int GetHashCode() => 0;
    }

    public static class CompositionProbe
    {
        public static string Definitions => FrameworkProofContent.Read("probe.json");
        public static GameFactory Factory(ProbeModule module, string definitions = null)
        {
            var definition = new GameDefinition("probe", new[] { module });
            string manifest = "{ \"schema\": 1, \"modules\": [{ \"id\": \"probe\", \"version\": \"" + module.Version + "\", \"documents\": [\"probe.json\"] }] }";
            var files = new PreloadedFileSource().Add("manifest.json", manifest).Add("probe.json", definitions ?? Definitions);
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        public static SimGame New(ProbeModule module) => Factory(module).NewGame(new NewGameRequest { Scenario = "probe", Seed = 1 });
    }
}
