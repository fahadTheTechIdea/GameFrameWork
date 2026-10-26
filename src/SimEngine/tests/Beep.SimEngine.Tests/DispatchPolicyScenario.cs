using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;

namespace Beep.SimEngine.Tests
{
    internal enum PolicyDispatchKind { Demand, Route, Evacuation, CargoReturn, Independent, Manual, WorkSupply, NativeWork, EmergencyResponse, EmergencySupply }
    // Sole pure scenario authoring for dispatch-policy acceptance; no admission or execution rules.
    internal static class DispatchPolicyScenario
    {
        internal static bool IsLocal(PolicyDispatchKind kind) => kind == PolicyDispatchKind.NativeWork || kind == PolicyDispatchKind.EmergencyResponse;
        internal static bool IsSuppliedWork(PolicyDispatchKind kind) => kind == PolicyDispatchKind.WorkSupply || kind == PolicyDispatchKind.EmergencySupply;
        internal static bool IsEmergency(PolicyDispatchKind kind) => kind == PolicyDispatchKind.EmergencyResponse || kind == PolicyDispatchKind.EmergencySupply;
        internal static GameFactory Create(PolicyDispatchKind kind, bool direct, bool external)
        {
            bool cargo = kind == PolicyDispatchKind.CargoReturn, local = IsLocal(kind);
            return LogisticsPolicyContentTests.Factory(new LogisticsPolicyContentModule(), direct, external, edit: doc => {
                var entries = doc.Get(direct ? "independent_logistics_policies" : "logistics_policies");
                var stock = entries[1].Get("stock").Get("supply");
                stock.Set("minimum", JsonValue.Number(kind == PolicyDispatchKind.Demand ? "1" : "0"));
                stock.Set("desired", JsonValue.Number(kind == PolicyDispatchKind.Demand ? "8" : "0"));
                stock.Set("maximum", JsonValue.Number(cargo ? "2" : "8"));
                // A stock-demand profile needs desired <= maximum; player command narrows it before enabling workers.
                if (kind != PolicyDispatchKind.Demand && !local) stock.Set("maximum", JsonValue.Number(cargo ? "2" : "4"));
                if (local) entries[1].Get("outlet").Get("reserve").Set("supply", JsonValue.Number("8"));
                if (cargo) {
                    var profile = entries[0].Clone(); profile.Set("id", JsonValue.Of("policy.cargo"));
                    profile.Get("outlet").Get("reserve").Set("supply", JsonValue.Number("2")); entries.Add(profile);
                }
            }, editRoot: root => {
                var source = root.Get("entities")[0].Get("components");
                if (kind == PolicyDispatchKind.Evacuation) {
                    source.Remove("point_stock");
                    source.Set("storage", Json.Parse("{\"capacity_policy\":\"fixture.count.40\",\"public\":true,\"evacuate\":true,\"accepts\":[\"supply\"],\"phases\":[],\"initial_stock\":{\"supply\":20}}"));
                }
                if (cargo) {
                    source.Get("point_stock").Set("initial", JsonValue.Number("16"));
                    var carrier = root.Get("entities")[2].Get("components");
                    carrier.Get("storage").Set("initial_stock", Json.Parse("{\"supply\":4}"));
                    if (external) carrier.Set("contract_outlet_policy", Json.Parse("{\"profile\":\"policy.cargo\",\"fault\":0}"));
                    else carrier.Set("logistics_policy", Json.Parse("{\"profiles\":[{\"port\":null,\"profile\":\"policy.cargo\"}]}"));
                }
                if (local) {
                    source.Get("point_stock").Set("initial", JsonValue.Number("12"));
                    root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number("8"));
                }
                if (IsEmergency(kind)) root.Get("entities")[1].Get("components").Get("contract_incident").Set("response", JsonValue.Of(local ? "policy.local-response" : "policy.supplied-response"));
            }, recovery: kind == PolicyDispatchKind.Evacuation || cargo, shipment: kind == PolicyDispatchKind.Independent, editWork: work => {
                var activities = Json.Parse(FrameworkProofContent.Read("dispatch-policy-activities.json")).Get("activities");
                for (int i = 0; i < activities.Count; i++) work.Get("activities").Add(activities[i]);
            });
        }
    }
}
