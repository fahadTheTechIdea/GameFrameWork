using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IContentAccess
    {
        Code Check(World world, PlayerId player, IContentDefinition definition);
    }
    /// <summary>Actual policy/rule eligibility of an already admitted operation, independent of metadata projection.</summary>
    public interface IContentOperationAccess
    {
        Code CheckOperation(World world, PlayerId company, IContentDefinition definition);
    }

    public interface IContentAccessRule
    {
        string Id { get; }
        World World { get; }
        Code Check(PlayerId player, IContentDefinition definition);
    }

    /// <summary>One authored policy selected through the existing definition catalog.</summary>
    public interface IContentAccessDefinition : IContentDefinition, IContentAccess { }

    /// <summary>Explicit open policy; absence of a policy is never treated as open.</summary>
    public sealed class OpenContentAccessDef : Def, IContentAccessDefinition
    {
        protected override void ReadCore(JsonReader reader) { }
        public Code Check(World world, PlayerId player, IContentDefinition definition) => Code.None;
    }

    /// <summary>Engine-side modules add access restrictions here, never to construction or a host.</summary>
    public sealed class ContentAccessPolicy : IContentAccess, IContentOperationAccess, ICompositionValidator
    {
        private readonly World _world;
        private readonly IContentAccessDefinition _basis;
        private IReadOnlyList<RuleBinding> _rules;
        private bool _checking;
        private readonly struct RuleBinding
        {
            internal readonly string Id;
            internal readonly IContentAccessRule Rule;
            internal RuleBinding(string id, IContentAccessRule rule) { Id = id; Rule = rule; }
        }
        public ContentAccessPolicy(World world, IContentAccessDefinition basis)
        { _world = world ?? throw new ArgumentNullException(nameof(world)); _basis = basis ?? throw new ArgumentNullException(nameof(basis)); }
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ContentException("content access belongs to another world");
            EntityObservation.ReadProvider(world, _basis, null, () => {
                if (!(_basis is Def actual) || !actual.IsFrozen || _basis is IPersistentService ||
                    !world.Content.Owns(_basis) || _basis.Id != actual.Id ||
                    !ReferenceEquals(world.Content.Find<IContentAccessDefinition>(_basis.Id), _basis))
                    throw new ContentException("content access requires its actual immutable authored definition; mutable rules are declared services");
                return true;
            }, world.Space);
            var rules = new List<RuleBinding>();
            foreach (IContentAccessRule rule in world.Services.Providers<IContentAccessRule>())
                rules.Add(EntityObservation.ReadProvider(world, rule, null, () => {
                    string id = rule.Id;
                    try { ContractIdentity.ValidateId(id, "content access rule"); } catch (ArgumentException e) { throw new ContentException(e.Message); }
                    if (!ReferenceEquals(rule.World, world)) throw new ContentException("content access rule belongs to another session");
                    return new RuleBinding(id, rule);
                }, world.Space));
            rules.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            for (int i = 0; i < rules.Count; i++)
            {
                if (i > 0 && rules[i - 1].Id == rules[i].Id)
                    throw new ContentException("content access rules need unique identities and the current session");
            }
            _rules = rules.AsReadOnly();
        }
        public Code Check(World world, PlayerId player, IContentDefinition definition)
            => CheckCore(world, player, definition, true);
        public Code CheckOperation(World world, PlayerId company, IContentDefinition definition)
            => CheckCore(world, company, definition, false);
        private Code CheckCore(World world, PlayerId player, IContentDefinition definition, bool metadata)
        {
            if (!ReferenceEquals(world, _world) || !world.Content.Owns(definition)) return Codes.UnknownDef;
            world.RequireHealthyUpdate();
            if (player != PlayerId.None && !world.Players.IsSeat(player)) return Codes.NotSeated;
            if (metadata && !world.ViewFor(player).CanSeeContent(definition)) return Codes.UnknownDef;
            if (_rules == null) throw new InvalidOperationException("content access is not composed");
            if (_checking)
            {
                var error = new InvalidOperationException("recursive content access observation");
                world.LatchFault("observation.provider", error); throw error;
            }
            _checking = true;
            try
            {
                Code initial = EntityObservation.ReadProvider(world, _basis, null,
                    () => _basis.Check(world, player, definition), world.Space);
                if (!initial.IsNone) return initial;
                foreach (RuleBinding binding in _rules)
                {
                    Code refusal = EntityObservation.ReadProvider(world, binding.Rule, null, () => {
                        RequireBinding(binding);
                        Code result = binding.Rule.Check(player, definition);
                        RequireBinding(binding);
                        return result;
                    }, world.Space);
                    if (!refusal.IsNone) return refusal;
                }
                return Code.None;
            }
            finally { _checking = false; }
        }
        private void RequireBinding(RuleBinding binding)
        {
            if (binding.Rule.Id != binding.Id || !ReferenceEquals(binding.Rule.World, _world))
                throw new InvalidOperationException("content access rule changed its composed identity or session");
        }
    }

    public sealed class ContentAccessModule : GameModule
    {
        public const string ModuleId = "sim.access";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types) => types.Section<OpenContentAccessDef>("open_content_access");
        public override void Compose(WorldComposer composer)
        {
            IReadOnlyList<IContentAccessDefinition> policies = composer.Content.All<IContentAccessDefinition>();
            if (policies.Count != 1) throw new ContentException("selected content must declare exactly one content-access policy");
            var policy = new ContentAccessPolicy(composer.World, policies[0]);
            composer.Services.Bind<IContentAccess>(policy);
            composer.Services.Bind<IContentOperationAccess>(policy);
        }
    }
}
