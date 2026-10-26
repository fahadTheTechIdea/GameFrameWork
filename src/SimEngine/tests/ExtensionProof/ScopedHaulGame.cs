using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Composition only. The master-listed JSON and native engine own all gameplay.
    public static class ScopedHaulGame
    {
        public static GameFactory Factory(bool automaticSupply = false, bool scopedSources = false, bool scopedPolicies = false, bool scopedActivities = false, bool scopedRecovery = false, bool scopedReroutes = false, bool scopedEvacuation = false, bool scopedAccess = false, bool originalPortLoss = false, bool originalDockRecovery = false, bool restoredDockDelivery = false, bool pointDockRecovery = false,
            System.Action<JsonValue> editCommon=null,System.Action<JsonValue> editBindings=null,string selectionFaultDefinitions=null,string availabilityDefinitions=null,
            bool alternateDockAccess=false, System.Action<JsonValue> editPools=null)
        {
            if(alternateDockAccess&&pointDockRecovery)throw new System.ArgumentException("alternate dock content requires its authored tile space");
            if(alternateDockAccess)restoredDockDelivery=true;
            if (pointDockRecovery) restoredDockDelivery = true;
            if (restoredDockDelivery) originalDockRecovery = true;
            if (originalDockRecovery) originalPortLoss = true;
            var modules = new System.Collections.Generic.List<GameModule> {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new TileNavigationModule(),
                new WorkModule(), new LogisticsModule(), new ResourceFlowsModule(), new OperationsModule(),
                new TransformationModule(), new WorldSetupModule(), new EndpointProofModule(), new OutputPortProofModule() };
            if (scopedRecovery || scopedReroutes || scopedEvacuation || scopedAccess || originalPortLoss) modules.Add(new LogisticsRecoveryPolicyModule());
            if (pointDockRecovery) modules.Add(new PointModule());
            if (scopedReroutes) modules.Add(new ConstructionModule());
            if (scopedPolicies) { modules.Add(new AutomationModule()); modules.Add(new InventorySignalsModule()); }
            var definition = new GameDefinition("scoped-haul", modules.ToArray());
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read(pointDockRecovery ? "scoped-point-dock-restoration-master.json" : restoredDockDelivery ? "scoped-dock-restoration-master.json" : originalDockRecovery ? "scoped-dock-original-loss-master.json" : originalPortLoss ? "scoped-original-port-loss-master.json" : scopedAccess ? "scoped-recovery-access-master.json" : scopedEvacuation ? "scoped-evacuation-origins-master.json" : scopedReroutes ? "scoped-reroute-master.json" : scopedRecovery ? "scoped-recovery-master.json" : scopedActivities ? "scoped-activity-master.json" : scopedPolicies ? "scoped-policies-master.json" : scopedSources ? "scoped-sources-master.json" : automaticSupply ? "scoped-supply-master.json" : "scoped-haul-master.json"));
            foreach (string file in new[] { "scoped-haul.json", "scoped-haul-common.json", "scoped-source-stores.json", "scoped-recovery-policies.json", "scoped-original-binding-policy.json", "scoped-dock-work-policies.json", "work-retry.json", "endpoints-flows.json", "open-access.json" })
                files.Add(file, file == "open-access.json" ? FrameworkProofContent.Access : FrameworkProofContent.Read(file));
            if (scopedAccess) files.Add("scoped-recovery-access.json", FrameworkProofContent.Read("scoped-recovery-access.json"));
            if (originalPortLoss) files.Add("scoped-original-port-loss.json", FrameworkProofContent.Read("scoped-original-port-loss.json"));
            if (originalDockRecovery) files.Add("scoped-dock-original-loss.json", FrameworkProofContent.Read("scoped-dock-original-loss.json"));
            if (restoredDockDelivery) files.Add("scoped-dock-restoration.json", FrameworkProofContent.Read("scoped-dock-restoration.json"));
            if (pointDockRecovery) files.Add("scoped-point-dock-restoration.json", FrameworkProofContent.Read("scoped-point-dock-restoration.json"));
            if (scopedEvacuation || scopedAccess) files.Add("scoped-evacuation-origins.json", FrameworkProofContent.Read("scoped-evacuation-origins.json"));
            if (scopedReroutes) files.Add("scoped-reroute.json", FrameworkProofContent.Read("scoped-reroute.json"));
            if (scopedRecovery || scopedReroutes || scopedEvacuation || scopedAccess || originalPortLoss) { files.Add("scoped-sources.json", FrameworkProofContent.Read("scoped-sources.json")); files.Add("scoped-recovery.json", FrameworkProofContent.Read("scoped-recovery.json")); }
            if (scopedActivities) files.Add("scoped-activity.json", FrameworkProofContent.Read("scoped-activity.json"));
            if (scopedPolicies) files.Add("scoped-policies.json", FrameworkProofContent.Read("scoped-policies.json"));
            if(editCommon!=null){var root=Json.Parse(files.ReadText("scoped-haul-common.json"));editCommon(root);files.Add("scoped-haul-common.json",root.ToString());}
            if(editPools!=null){var root=Json.Parse(files.ReadText("scoped-source-stores.json"));editPools(root);files.Add("scoped-source-stores.json",root.ToString());}
            if(editBindings!=null){string path=pointDockRecovery?"scoped-point-dock-restoration.json":restoredDockDelivery?"scoped-dock-restoration.json":"scoped-original-port-loss.json";
                var root=Json.Parse(files.ReadText(path));editBindings(root);files.Add(path,root.ToString());}
            if(selectionFaultDefinitions!=null){var master=Json.Parse(files.ReadText("manifest.json"));var rows=master.Get("modules");for(int i=0;i<rows.Count;i++)
                if(rows[i].Get("id").Text=="proof.output-ports")rows[i].Get("documents").Add(JsonValue.Of("resource-port-selection-faults.json"));
                files.Add("manifest.json",master.ToString()).Add("resource-port-selection-faults.json",selectionFaultDefinitions);}
            if(availabilityDefinitions!=null){var master=Json.Parse(files.ReadText("manifest.json"));var rows=master.Get("modules");for(int i=0;i<rows.Count;i++)
                if(rows[i].Get("id").Text=="proof.output-ports")rows[i].Get("documents").Add(JsonValue.Of("resource-port-availability.json"));
                files.Add("manifest.json",master.ToString()).Add("resource-port-availability.json",availabilityDefinitions);}
            if (scopedSources) files.Add("scoped-sources.json", FrameworkProofContent.Read("scoped-sources.json"));
            if(alternateDockAccess){
                var master=Json.Parse(files.ReadText("manifest.json"));var declared=master.Get("modules");
                for(int i=0;i<declared.Count;i++)if(declared[i].Get("id").Text=="proof.output-ports")
                    declared[i].Get("documents").Add(JsonValue.Of("scoped-dock-alternate-access.json"));
                var restored=Json.Parse(files.ReadText("scoped-dock-restoration.json"));
                restored.Get("scenarios")[0].Get("settings").Get("sim.world-setup").Set("profile",JsonValue.Of("alternate.start"));
                files.Add("manifest.json",master.ToString()).Add("scoped-dock-restoration.json",restored.ToString())
                    .Add("scoped-dock-alternate-access.json",FrameworkProofContent.Read("scoped-dock-alternate-access.json"));
            }
            if (automaticSupply) files.Add("scoped-supply.json", FrameworkProofContent.Read("scoped-supply.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
    }
}
