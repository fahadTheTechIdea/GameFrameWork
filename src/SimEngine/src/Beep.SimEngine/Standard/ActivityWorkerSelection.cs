using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Player selection intent, never a worker claim or completion receipt.</summary>
    public readonly struct ActivityWorkerBinding
    {
        public string Node { get; }
        public EntityId Worker { get; }
        public WorkerRoleId Role { get; }
        public ActivityWorkerBinding(string node, EntityId worker, WorkerRoleId role = default)
        {
            ContractIdentity.ValidateId(node, "selected activity node");
            if (!worker.IsValid) throw new ArgumentException("selected worker must be a positive entity identity");
            Node = node; Worker = worker; Role = role;
        }
    }
    public interface IActivityWorkerSelection
    {
        IReadOnlyList<ActivityWorkerBinding> Bindings { get; }
        /// <summary>None means deliberately unbound, not permission to replace an unavailable selected machine.</summary>
        EntityId WorkerFor(string node);
        /// <summary>None deliberately allows any compatible node role; a selected role is exact intent.</summary>
        WorkerRoleId RoleFor(string node);
    }
    /// <summary>One immutable owner for canonical selection, argument parsing, admission and embedded Sync.</summary>
    public sealed class ActivityWorkerSelection : IActivityWorkerSelection
    {
        public static readonly Code Mismatch = new Code("work.selected-worker-mismatch");
        public static ActivityWorkerSelection Unbound { get; } = new ActivityWorkerSelection(Array.Empty<ActivityWorkerBinding>());
        public IReadOnlyList<ActivityWorkerBinding> Bindings { get; }
        public ActivityWorkerSelection(IEnumerable<ActivityWorkerBinding> bindings)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            var values = new List<ActivityWorkerBinding>(bindings);
            values.Sort((a, b) => string.CompareOrdinal(a.Node, b.Node));
            for (int i = 0; i < values.Count; i++)
            {
                ContractIdentity.ValidateId(values[i].Node, "selected activity node");
                if (!values[i].Worker.IsValid || i > 0 && values[i - 1].Node == values[i].Node)
                    throw new ArgumentException("selected workers require positive identities and unique node bindings");
            }
            Bindings = values.AsReadOnly();
        }
        public EntityId WorkerFor(string node)
        {
            foreach (var value in Bindings) if (value.Node == node) return value.Worker;
            return EntityId.None;
        }
        public void ValidateGraph(IActivityGraph graph)
        {
            foreach (var value in Bindings)
            {
                ValidateMachineNode(graph, value.Node);
                if (value.Role.IsValid && !ContainsRole(graph.Find(value.Node).Roles, value.Role))
                    throw new ContentException("selected role is not declared by machine node: " + value.Node);
            }
        }
        public WorkerRoleId RoleFor(string node)
        {
            foreach (var value in Bindings) if (value.Node == node) return value.Role;
            return WorkerRoleId.None;
        }
        private static bool ContainsRole(IReadOnlyList<WorkerRoleId> roles, WorkerRoleId role)
        { foreach (var candidate in roles) if (candidate == role) return true; return false; }
        public static void ValidateMachineNode(IActivityGraph graph, string identity)
        {
            var node = graph?.Find(identity);
            if (node == null || node.Roles.Count == 0 || node.Duration.IsZero)
                throw new ContentException("selected worker must name a positive-time machine node: " + identity);
        }
        public Code Check(World world, PlayerId company, IActivityGraph graph)
        {
            ValidateGraph(graph);
            foreach (var value in Bindings)
            {
                Entity worker = world.Entities.Get(value.Worker);
                if (worker == null || !worker.Alive) return Codes.NotFound;
                if (worker.Owner != company) return Codes.NotOwned;
                if (worker.Get<IWorker>() == null) return WorkSystem.BlockerNoWorker;
                bool compatible = false;
                foreach (var role in graph.Find(value.Node).Roles)
                    if ((!value.Role.IsValid || value.Role == role) && WorkerExecutionContracts.Can(world, worker, role)) { compatible = true; break; }
                if (!compatible) return WorkSystem.BlockerNoWorker;
            }
            return Code.None;
        }
        public string Identity(EntityId target)
        {
            var text = new StringBuilder(target.Value.ToString(CultureInfo.InvariantCulture));
            foreach (var value in Bindings)
                text.Append('|').Append(value.Node.Length.ToString(CultureInfo.InvariantCulture)).Append(':')
                    .Append(value.Node).Append('=').Append(value.Worker.Value.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append((value.Role.Value?.Length ?? 0).ToString(CultureInfo.InvariantCulture)).Append(':').Append(value.Role.Value);
            return text.ToString();
        }
        public static JsonValue CreateArguments(EntityId target, IActivityWorkerSelection selection)
        {
            if (!target.IsValid || selection == null) throw new ArgumentException("target arguments require positive target and explicit selection policy");
            var canonical = new ActivityWorkerSelection(selection.Bindings);
            var arguments = JsonValue.NewObject(); arguments.Set("target", JsonValue.Number(target.Value.ToString(CultureInfo.InvariantCulture)));
            if (canonical.Bindings.Count != 0)
            {
                var workers = JsonValue.NewArray(); foreach (var binding in canonical.Bindings)
                {
                    var entry = JsonValue.NewObject(); entry.Set("node", JsonValue.Of(binding.Node));
                    entry.Set("worker", JsonValue.Number(binding.Worker.Value.ToString(CultureInfo.InvariantCulture)));
                    entry.Set("role", binding.Role.IsValid ? JsonValue.Of(binding.Role.Value) : JsonValue.Null()); workers.Add(entry);
                }
                arguments.Set("workers", workers);
            }
            return arguments;
        }
        public static ActivityWorkerSelection Read(JsonValue arguments, IActivityGraph graph, string where, out EntityId target)
        {
            var reader = new JsonReader(arguments, where); int identity = reader.Int("target");
            if (identity <= 0) throw reader.Error("target must be a positive entity identity");
            target = new EntityId(identity); JsonValue workers = reader.Array("workers"); reader.End();
            if (workers == null) return Unbound;
            if (workers.Count == 0) throw reader.Error("an explicit worker selection must be nonempty; omit it for unbound nodes");
            var bindings = new List<ActivityWorkerBinding>();
            for (int i = 0; i < workers.Count; i++)
            {
                var entry = new JsonReader(workers[i], where + ".workers[" + i + "]");
                string node = entry.String("node"); int worker = entry.Int("worker"); var role = entry.Value("role"); entry.End();
                if (role == null || role.Kind != JsonKind.Null && role.Kind != JsonKind.String)
                    throw entry.Error("explicit selected role identity or null required");
                try { bindings.Add(new ActivityWorkerBinding(node, new EntityId(worker),
                    role.Kind == JsonKind.Null ? WorkerRoleId.None : new WorkerRoleId(role.Text))); }
                catch (ArgumentException error) { throw entry.Error(error.Message); }
            }
            try { var result = new ActivityWorkerSelection(bindings); result.ValidateGraph(graph); return result; }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
        }
        internal static ActivityWorkerSelection Sync(IStateSync sync, ActivityWorkerSelection current)
        {
            if (!sync.Reading && current == null) throw new CorruptStateException("activity worker selection is missing");
            int count = sync.Count(current?.Bindings.Count ?? 0);
            var values = sync.Reading ? new List<ActivityWorkerBinding>() : null; string previous = null;
            for (int i = 0; i < count; i++)
            {
                string node = sync.Reading ? null : current.Bindings[i].Node;
                EntityId worker = sync.Reading ? EntityId.None : current.Bindings[i].Worker;
                string role = sync.Reading ? null : current.Bindings[i].Role.Value;
                sync.String(ref node); sync.Entity(ref worker); sync.String(ref role);
                if (previous != null && string.CompareOrdinal(previous, node) >= 0)
                    throw new CorruptStateException("activity worker selection is not canonical");
                previous = node;
                if (sync.Reading)
                {
                    try { values.Add(new ActivityWorkerBinding(node, worker, role == null ? WorkerRoleId.None : new WorkerRoleId(role))); }
                    catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
                }
            }
            return sync.Reading ? new ActivityWorkerSelection(values) : current;
        }
    }
}
