using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;

namespace Beep.SimEngine.Tests
{
    internal static partial class OilManifestContentTests
    {
        internal const string DockCase = "Oil construction docks: all authored sites preserve roles materials and finite physical journeys";
        private static string Sorted(IEnumerable<string> values) => string.Join(",", values.OrderBy(value => value, StringComparer.Ordinal));
        private static void SiteDockAuthoring(Check c)
        {
            var authored = AuthoredDocuments(); var content = ResolveOil(new Files(authored));
            var sites = Rows(Json.Parse(authored["oil/construction-sites.json"]).Get("entities")).ToArray();
            c.Equal(18, sites.Length, "all original authored construction sites, not only the blocked example");
            foreach (var site in sites)
            {
                string id = site.Get("id").Text; var parts = site.Get("components"); var declaration = parts.Get("tile_work_dock");
                var entity = content.Get<EntityDef>(id); var dock = entity.Component<IWorkDockDefinition>();
                c.That(declaration != null && dock != null && entity.HasCapability<IWorkDock>(), "actual loaded dock contract on " + id);
                if (declaration == null || dock == null) throw new InvalidOperationException("missing authored site dock " + id);
                string target = parts.Get("site").Get("target").Text;
                var construction = content.Get<EntityDef>(target).Component<IConstructionDefinition>();
                var roles = construction.Workflow.Graph.Nodes.SelectMany(node => node.Roles).Select(role => role.Value).Distinct().ToArray();
                c.Equal(Sorted(roles), Sorted(Rows(declaration.Get("work_roles")).Select(role => role.Text)), "dock supports exactly original construction roles: " + id);
                var resources = Rows(parts.Get("storage").Get("accepts")).Select(resource => resource.Text).ToArray();
                c.Equal(Sorted(resources), Sorted(Rows(declaration.Get("resources")).Select(resource => resource.Text)), "dock policy matches original accepted materials: " + id);
                c.Equal(8, dock.Slots.Count, "explicit finite service and departure capacity: " + id);
                c.Equal(4, dock.Slots.Count(slot => slot.Kind == WorkDockSlotKind.Service), "four physical service alternatives: " + id);
                c.Equal(4, dock.Slots.Count(slot => slot.Kind == WorkDockSlotKind.Departure), "four physical departure alternatives: " + id);
                foreach (var request in roles.Select(role => WorkDockRequest.Work(new WorkerRoleId(role))).Concat(resources.Select(resource => WorkDockRequest.Haul(content.ResourceId(resource), ResourceHandlingPhase.Unloading))))
                {
                    c.That(dock.Slots.Any(slot => slot.Kind == WorkDockSlotKind.Service && slot.Matches(request)), "typed compatible service request: " + id);
                    c.That(dock.Slots.Any(slot => slot.Kind == WorkDockSlotKind.Departure && slot.Matches(request)), "typed compatible departure request: " + id);
                }
                int width = int.Parse(parts.Get("tile_presence").Get("width").Text, CultureInfo.InvariantCulture);
                int height = int.Parse(parts.Get("tile_presence").Get("height").Text, CultureInfo.InvariantCulture);
                var locations = new HashSet<string>();
                foreach (var slot in Rows(declaration.Get("slots")))
                {
                    int x = int.Parse(slot.Get("x").Text, CultureInfo.InvariantCulture), y = int.Parse(slot.Get("y").Text, CultureInfo.InvariantCulture);
                    c.That(x < 0 || y < 0 || x >= width || y >= height, "physical slot is outside unchanged blocking footprint: " + id);
                    c.That(locations.Add(x + ":" + y), "service/departure geometry is distinct: " + id);
                }
            }
            foreach (string fault in new[] { "duplicate", "interior", "role", "departure" })
            {
                var changed = new Files(authored);
                changed.Edit("oil/construction-sites.json", root => {
                    var dock = root.Get("entities")[0].Get("components").Get("tile_work_dock");
                    if (fault == "duplicate") dock.Get("slots")[1].Set("id", dock.Get("slots")[0].Get("id").Clone());
                    if (fault == "interior") { dock.Get("slots")[0].Set("x", JsonValue.Number("0")); dock.Get("slots")[0].Set("y", JsonValue.Number("0")); }
                    if (fault == "role") dock.Set("work_roles", Json.Parse("[\"unknown.construction.role\"]"));
                    if (fault == "departure") { var slots = JsonValue.NewArray(); foreach (var slot in Rows(dock.Get("slots"))) if (slot.Get("kind").Text != "departure") slots.Add(slot.Clone()); dock.Set("slots", slots); }
                });
                c.Throws<ContentException>(() => ResolveOil(changed), "native loader refuses invalid site dock: " + fault);
            }
            Console.WriteLine("authored site docks:18 existing sites,144 finite slots, original roles/materials,4 intended loader refusals; no generated world or Update");
        }
    }
}
