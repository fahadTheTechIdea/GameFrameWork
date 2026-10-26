using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent authoring only; original engine runtime owns every staffing decision.</summary>
    public sealed class ProofStaffingGateDef : ComponentDef<StaffedTransformationGate>, IStaffedTransformationGateDefinition
    {
        private StaffedTransformationGateDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public bool ProcessBound => _declaration.ProcessBound;
        public int OrdersPerUpdate => _declaration.OrdersPerUpdate;
        public IReadOnlyList<Code> Processes => _declaration.Processes;
        public IReadOnlyList<ITransformationDefinition> Transformations => _declaration.Transformations;
        public IReadOnlyList<ITransformationStaffingRequirement> Requirements => _declaration.Requirements;
        protected override void ReadCore(JsonReader reader) => _declaration = StaffedTransformationGateDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        // No local validation override: engine composition must validate even unused metadata.
    }
    public sealed class StaffingMetadataProofModule : GameModule
    {
        public const string ModuleId = "proof.staffing-metadata";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(TransformationModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Component<ProofStaffingGateDef>("proof_staffing_gate", 2);
    }

    // Engine-owned content composition only; all activity, motion, resource and process rules are native.
    public static class StaffedTransformationGame
    {
        public static GameFactory Factory(Action<JsonValue> edit = null, Action<JsonValue> editWorld = null, bool independent = false)
            => Compose("staffed-transformations-master.json", "staffed-transformations.json", "staff.factory", edit, editWorld, independent);
        internal static GameFactory Compose(string masterFile, string contentFile, string factoryDefinition,
            Action<JsonValue> edit, Action<JsonValue> editWorld, bool independent = false)
        {
            var modules = new List<GameModule> {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new TileNavigationModule(),
                new WorkModule(), new CompanyKnowledgeModule(), new OperationsModule(), new ResourceFlowsModule(),
                new TransformationModule(), new WorldSetupModule() };
            if (independent) modules.Add(new StaffingMetadataProofModule());
            var definition = new GameDefinition("staffed-transformations", modules);
            var content = Json.Parse(FrameworkProofContent.Read(contentFile)); edit?.Invoke(content);
            if (independent)
                for (int i = 0; i < content.Get("entities").Count; i++)
                {
                    var components = content.Get("entities")[i].Get("components");
                    var gate = components.Get("staffed_transformation_gate");
                    if (gate == null) continue;
                    components.Remove("staffed_transformation_gate"); components.Set("proof_staffing_gate", gate);
                }
            var manifest = Json.Parse(FrameworkProofContent.Read(masterFile));
            if (independent)
            {
                var declarations = manifest.Get("modules");
                for (int i = 0; i < declarations.Count; i++)
                    if (declarations[i].Get("id").Text == TransformationModule.ModuleId)
                    {
                        var documents = declarations[i].Get("documents"); var retained = JsonValue.NewArray();
                        for (int j = 0; j < documents.Count; j++) if (documents[j].Text != contentFile) retained.Add(documents[j]);
                        declarations[i].Set("documents", retained);
                    }
                declarations.Add(Json.Parse("{\"id\":\"proof.staffing-metadata\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"}],\"documents\":[\"" + contentFile + "\"]}"));
            }
            var world = Json.Parse(FrameworkProofContent.Read("native-survey-world.json"));
            var scenario = world.Get("scenarios")[0];
            scenario.Get("modules").Add(JsonValue.Of(OperationsModule.ModuleId));
            scenario.Get("modules").Add(JsonValue.Of(TransformationModule.ModuleId));
            scenario.Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
            if (independent) scenario.Get("modules").Add(JsonValue.Of(StaffingMetadataProofModule.ModuleId));
            editWorld?.Invoke(world);
            var startup = Json.Parse(FrameworkProofContent.Read("native-survey-startup.json"));
            JsonValue entries = startup.Get("entity_setups");
            for (int i = 0; i < entries.Count; i++)
            {
                JsonValue entry = entries[i];
                if (entry.Get("id").Text == "start.site") entry.Set("entity", JsonValue.Of(factoryDefinition));
            }
            startup.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("start.assistant"));
            startup.Get("entity_setups").Add(Json.Parse("{\"id\":\"start.assistant\",\"after\":[\"start.site\"],\"entity\":\"staff.assistant\",\"company\":0,\"placement\":{\"space\":\"survey.tiles\",\"x\":3,\"y\":11},\"home\":null}"));
            var files = new PreloadedFileSource()
                .Add("manifest.json", manifest.ToString())
                .Add(contentFile, content.ToString())
                .Add("native-survey-world.json", world.ToString()).Add("native-survey-startup.json", startup.ToString())
                .Add("native-survey-navigation.json", FrameworkProofContent.Read("native-survey-navigation.json"))
                .Add("native-survey-work.json", FrameworkProofContent.Read("native-survey-work.json"))
                .Add("native-survey-knowledge.json", FrameworkProofContent.Read("native-survey-knowledge.json"))
                .Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"))
                .Add("endpoints-flows.json", FrameworkProofContent.Read("endpoints-flows.json"))
                .Add("open-access.json", FrameworkProofContent.Access);
            if (contentFile != "staffed-transformations.json")
                files.Add("staffed-transformations.json", FrameworkProofContent.Read("staffed-transformations.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
    }
    public static class ProcessStaffingGame
    {
        public static GameFactory Factory(Action<JsonValue> edit = null, bool independent = false)
            => StaffedTransformationGame.Compose("process-staffing-master.json", "process-staffing.json", "staff.process-factory", edit, null, independent);
    }
}
