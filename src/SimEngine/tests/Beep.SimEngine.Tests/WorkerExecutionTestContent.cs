using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    // Authoring/player input only. No availability, rate, timing or scheduler rules live here.
    internal static class WorkerExecutionTestContent
    {
        internal static void Add(JsonValue root)
        {
            var entities = root.Get("entities"); if (entities == null) return;
            for (int i = 0; i < entities.Count; i++)
            {
                var parts = entities[i].Get("components");
                var profile = parts?.Get("worker") ?? parts?.Get("contract_worker");
                if (profile != null && parts.Get("worker_execution") == null)
                {
                    var authored = Json.Parse("{\"operation\":{\"id\":\"availability\",\"enabled\":true,\"target\":1,\"minimum\":0,\"maximum\":2,\"unit\":\"unit.work-rate\"},\"conditions\":[],\"states\":{}}");
                    authored.Get("states").Set("disabled", profile.Get("states").Get("stopped").Clone()); parts.Set("worker_execution", authored);
                }
            }
        }
        internal static string Author(string text)
        { var root = Json.Parse(text); Add(root); var output = new StringBuilder(); root.WriteCanonical(output); return output.ToString(); }
        internal static CommandResult Control(SimGame game, EntityId id, bool enabled, Fix multiplier)
            => TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = id, Operation = new Code("availability"), Enabled = enabled, Target = multiplier });
        internal static void AddJobGate(JsonValue root)
        {
            Add(root); var entities = root.Get("entities");
            for (int i = 0; i < entities.Count; i++)
            {
                var parts = entities[i].Get("components");
                if (parts?.Get("worker_execution") != null)
                    parts.Set("proof_worker_execution", Json.Parse("{\"id\":\"job-gate\",\"operation\":\"availability\",\"job_required_at\":2,\"multiplier\":1,\"fault\":0}"));
            }
        }
    }
}
