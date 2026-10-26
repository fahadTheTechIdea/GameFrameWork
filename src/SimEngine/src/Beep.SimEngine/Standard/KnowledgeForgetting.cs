using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IKnowledgeForgettingProvider : IKnowledgeProvider
    {
        Code CheckForgettingSubject(string subject);
        /// <summary>Pure whole-set dependency admission; both sets are actual own-company/provider facts.</summary>
        Code CheckForget(PlayerId company, IReadOnlyList<KnowledgeObservation> removed, IReadOnlyList<KnowledgeObservation> retained);
    }
    public interface IKnowledgeForgettingPolicy : IKnowledgeSubjectPolicy
    { bool Allows(World world, PlayerId company); }
    public sealed class KnowledgeForgettingDeclaration
    {
        private KnowledgeForgettingDeclaration() { }
        public static KnowledgeForgettingDeclaration Read(JsonReader reader)
        { var declaration = new KnowledgeForgettingDeclaration(); declaration.ReadCore(reader); return declaration; }
        private IReadOnlyList<KnowledgeSubjectKey> _subjects;
        private IReadOnlyList<PlayerId> _companies;
        public IReadOnlyList<KnowledgeSubjectKey> Subjects => _subjects;
        private void ReadCore(JsonReader reader)
        {
            _subjects = KnowledgeSubjectKeys.Read(reader);
            var array = reader.Array("companies") ?? throw reader.Error("explicit forgetting companies required");
            if (array.Count == 0) throw reader.Error("forgetting companies cannot be empty");
            var companies = new List<PlayerId>();
            for (int i = 0; i < array.Count; i++)
            {
                // Use the original strict integer reader rather than guessing/coercing identities.
                var value = JsonValue.NewObject(); value.Set("company", array[i]);
                int company = new JsonReader(value, reader.Where + ".companies[" + i + "]").Int("company");
                if (company < 0) throw reader.Error("forgetting company must be nonnegative"); companies.Add(new PlayerId(company));
            }
            companies.Sort((a, b) => a.Value.CompareTo(b.Value));
            for (int i = 1; i < companies.Count; i++) if (companies[i] == companies[i - 1]) throw reader.Error("duplicate forgetting company");
            _companies = companies.AsReadOnly();
        }
        public bool Allows(PlayerId company)
        { foreach (var permitted in _companies) if (permitted == company) return true; return false; }
        public void ValidateBinding(World world, string context)
        {
            foreach (var company in _companies) if (!world.Players.IsSeat(company)) throw new ContentException(context + ": forgetting company is not seated");
        }
    }
    /// <summary>Registered immutable metadata using the sole authored policy declaration.</summary>
    public sealed class AuthoredKnowledgeForgettingDef : Def, IKnowledgeForgettingPolicy
    {
        private KnowledgeForgettingDeclaration _declaration;
        public IReadOnlyList<KnowledgeSubjectKey> Subjects => _declaration.Subjects;
        protected override void ReadCore(JsonReader reader) => _declaration = KnowledgeForgettingDeclaration.Read(reader);
        public bool Allows(World world, PlayerId company) => world.Content.Owns(this) && _declaration.Allows(company);
        public void ValidateBinding(World world)
        { KnowledgeContentContracts.ValidateAlias(world, this); _declaration.ValidateBinding(world, Id); }
    }
    public interface IKnowledgeForgetting
    {
        Code PreviewForget(PlayerId company, string policy, out KnowledgeForgetSnapshot snapshot);
        Code Forget(PlayerId company, string policy);
    }
    public sealed class KnowledgeForgetSnapshot
    {
        public PlayerId Company { get; }
        public string Policy { get; }
        public IReadOnlyList<KnowledgeSubjectSnapshot> Entries { get; }
        public KnowledgeForgetSnapshot(PlayerId company, string policy, IReadOnlyList<KnowledgeSubjectSnapshot> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries)); Company = company; Policy = policy;
            var copy = new List<KnowledgeSubjectSnapshot>(); foreach (var entry in entries) copy.Add(entry); Entries = copy.AsReadOnly();
        }
    }
    public static class KnowledgeForgettingCodes
    { public static readonly Code Unsupported = new Code("knowledge.no-forgetting-capability"); }
    internal sealed class ForgetKnowledgeHandler : CommandHandler<ForgetKnowledgeCommand>
    {
        protected override Code Check(World world, ForgetKnowledgeCommand command)
            => world.Services.Get<IKnowledgeForgetting>().PreviewForget(command.Player, command.Policy, out _);
        protected override void Apply(World world, ForgetKnowledgeCommand command, CommandContext context)
        {
            Code refusal = world.Services.Get<IKnowledgeForgetting>().Forget(command.Player, command.Policy);
            if (!refusal.IsNone) throw new InvalidOperationException("admitted knowledge forgetting refused: " + refusal);
        }
    }
}
