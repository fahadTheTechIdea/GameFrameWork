using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Standard
{
    /// <summary>Selected-module rules attached to an existing visibility policy; not another policy owner.</summary>
    public interface IKnowledgeVisibilityRuleBinding : IContentDependencyDefinition
    {
        IKnowledgeVisibilityDefinition Policy { get; }
        IReadOnlyList<IVisibilityTarget> Rules { get; }
    }

    public sealed class KnowledgeVisibilityRuleBindingDef : Def, IKnowledgeVisibilityRuleBinding
    {
        private string _policy;
        private string[] _rules;
        public IKnowledgeVisibilityDefinition Policy { get; private set; }
        public IReadOnlyList<IVisibilityTarget> Rules { get; private set; }
        public IReadOnlyList<IContentDefinition> ContentDependencies { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _policy = reader.String("policy"); _rules = reader.Strings("rules");
            if (_rules.Length == 0) throw reader.Error("visibility binding requires explicit rules");
            Array.Sort(_rules, StringComparer.Ordinal);
            for (int i = 1; i < _rules.Length; i++)
                if (_rules[i] == _rules[i - 1]) throw reader.Error("duplicate visibility binding rule");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            Policy = content.Def<IKnowledgeVisibilityDefinition>(_policy);
            var rules = new List<IVisibilityTarget>();
            var dependencies = new List<IContentDefinition> { Policy };
            foreach (string id in _rules)
            {
                IVisibilityTarget rule = content.Def<IVisibilityTarget>(id);
                rules.Add(rule); dependencies.Add(rule);
            }
            Rules = rules.AsReadOnly(); ContentDependencies = dependencies.AsReadOnly();
        }
    }
}
