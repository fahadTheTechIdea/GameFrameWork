using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public interface IResourceConnection : IEntityCapability { IReadOnlyList<WorkResourceSource> Sources { get; } }
    public sealed class ResourceConnectionDef : ComponentDef<ResourceConnection>
    {
        private string[] _pools; private long[] _costs;
        public IReadOnlyList<WorkResourceSource> Sources { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            JsonValue sources = reader.Array("sources") ?? throw reader.Error("sources must be authored");
            if (sources.Count == 0) throw reader.Error("sources cannot be empty");
            _pools = new string[sources.Count]; _costs = new long[sources.Count];
            for (int i = 0; i < sources.Count; i++)
            {
                var source = new JsonReader(sources[i], reader.Where + ".sources[" + i + "]");
                if (source.Value("cost")?.Kind != JsonKind.Number) throw source.Error("cost must explicitly be authored");
                _pools[i] = source.String("pool"); _costs[i] = source.Long("cost"); source.End();
                if (_costs[i] < 0) throw reader.Error("cost cannot be negative");
            }
        }
        protected override void ResolveCore(ContentResolver content)
        {
            var sources = new List<WorkResourceSource>(); var unique = new HashSet<long>();
            for (int i = 0; i < _pools.Length; i++)
            {
                var pool = content.Def<EndpointPoolDef>(_pools[i]);
                if (!unique.Add(pool.Identity)) throw content.Fail("duplicate pool connection");
                sources.Add(new WorkResourceSource(new ResourceEndpointId("colony.pools", pool.Identity), _costs[i]));
            }
            Sources = sources.AsReadOnly(); _pools = null; _costs = null;
        }
    }
    public sealed class ResourceConnection : Component<ResourceConnectionDef>, IResourceConnection
    { public IReadOnlyList<WorkResourceSource> Sources => Def.Sources; }
    // An ordinary unrelated module leaf over the existing requirement family and scoped endpoints.
    // The physical connection is explicitly authored on the actual work target, not a remote fallback.
    public sealed class ConnectedResourceRequirementDef : ResourceWorkRequirementDef
    {
        public override Code CopySources(WorkRequirementContext context, IList<WorkResourceSource> destination)
        {
            destination.Clear(); IResourceConnection connection = context.Target.Get<IResourceConnection>();
            if (connection != null) foreach (var source in connection.Sources) destination.Add(source);
            return destination.Count == 0 ? WorkResourceCodes.MissingSource : Code.None;
        }
        public override WorkRequirementResult CheckSource(WorkRequirementContext context, ResourceEndpointId endpoint)
        {
            IResourceConnection connection = context.Target.Get<IResourceConnection>();
            if (connection != null) foreach (var source in connection.Sources) if (source.Endpoint == endpoint) return WorkRequirementResult.Satisfied();
            return WorkRequirementResult.Pending(WorkResourceCodes.MissingSource);
        }
    }
}
