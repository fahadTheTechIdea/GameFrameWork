using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent role-aware requirement. Existing actual ports own every resource/claim.</summary>
    public sealed class RoleInputRequirementDef : EntityResourceRequirementDef, IWorkerWorkRequirement
    {
        private string[] _roles, _sources;
        private IReadOnlyList<(WorkerRoleId Role, bool Worker)> _resolved;
        protected override void ReadCore(JsonReader reader)
        {
            base.ReadCore(reader);
            var rows = reader.Array("role_sources") ?? throw reader.Error("role_sources must be explicitly authored");
            if (rows.Count == 0) throw reader.Error("role_sources must be nonempty");
            _roles = new string[rows.Count]; _sources = new string[rows.Count];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = new JsonReader(rows[i], reader.Where + ".role_sources[" + i + "]");
                _roles[i] = row.String("role"); _sources[i] = row.String("source"); row.End();
                if (!seen.Add(_roles[i]) || _sources[i] != "target" && _sources[i] != "worker")
                    throw row.Error("role sources require unique roles and explicit target or worker endpoints");
            }
        }
        protected override void ResolveCore(ContentResolver content)
        {
            base.ResolveCore(content);
            var rows = new List<(WorkerRoleId Role, bool Worker)>();
            for (int i = 0; i < _roles.Length; i++)
                rows.Add((content.Def<IWorkerRoleDefinition>(_roles[i]).Role, _sources[i] == "worker"));
            rows.Sort((a, b) => string.CompareOrdinal(a.Role.Value, b.Role.Value));
            _resolved = rows.AsReadOnly(); _roles = _sources = null;
        }
        protected override Entity Source(WorkRequirementContext context)
        {
            foreach (var row in _resolved) if (row.Role == context.Role) return row.Worker ? context.Worker : context.Target;
            return null;
        }
        public override WorkRequirementResult Evaluate(WorkRequirementContext context)
        {
            foreach (var row in _resolved) if (row.Role == context.Role) return WorkRequirementResult.Satisfied();
            return WorkRequirementResult.Pending(new Code("proof.role-source-missing"));
        }
    }
}

