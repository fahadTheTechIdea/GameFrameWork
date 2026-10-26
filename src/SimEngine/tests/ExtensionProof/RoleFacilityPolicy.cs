using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Immutable independent content preference; native engine owns safety and physical execution.
    public sealed class RoleFacilityPolicyDef : Def, IActivityFacilityPolicy
    {
        private string _node;
        private string[] _roles;
        private long[] _ranks;
        private IReadOnlyList<(WorkerRoleId Role, long Rank)> _resolved;
        public string Node => _node;
        protected override void ReadCore(JsonReader reader)
        {
            _node = reader.String("node");
            if (string.IsNullOrWhiteSpace(_node)) throw reader.Error("explicit facility probe node required");
            var rows = reader.Array("roles") ?? throw reader.Error("explicit facility role ranks required");
            if (rows.Count == 0) throw reader.Error("facility role ranks must be nonempty");
            _roles = new string[rows.Count]; _ranks = new long[rows.Count];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = new JsonReader(rows[i], reader.Where + ".roles[" + i + "]");
                _roles[i] = row.String("role"); _ranks[i] = row.Int("rank"); row.End();
                if (!seen.Add(_roles[i]) || _ranks[i] < 0) throw row.Error("unique roles and nonnegative ranks required");
            }
        }
        protected override void ResolveCore(ContentResolver content)
        {
            var rows = new List<(WorkerRoleId Role, long Rank)>();
            for (int i = 0; i < _roles.Length; i++) rows.Add((content.Def<IWorkerRoleDefinition>(_roles[i]).Role, _ranks[i]));
            rows.Sort((a,b) => a.Role.CompareTo(b.Role)); _resolved = rows.AsReadOnly();
            _roles = null; _ranks = null;
        }
        public long? Rank(ActivityFacilityFacts facts)
        {
            foreach (var row in _resolved) if (row.Role == facts.Role) return row.Rank;
            return null;
        }
    }
}

