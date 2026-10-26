using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Explicit opt-in on the actual semantic provider, not a universal grant of research.</summary>
    public interface IKnowledgeSharingProvider : IKnowledgeProvider
    {
        Code CheckSharingSubject(string subject);
        Code CheckShare(KnowledgeObservation outgoing, PlayerId recipient);
    }
    public readonly struct KnowledgeSubjectKey
    {
        public string Provider { get; }
        public string Subject { get; }
        public KnowledgeSubjectKey(string provider, string subject)
        { ContractIdentity.ValidateId(provider, "knowledge provider"); ContractIdentity.ValidateId(subject, "knowledge subject"); Provider = provider; Subject = subject; }
        internal static int Compare(KnowledgeSubjectKey a, KnowledgeSubjectKey b)
        { int c = string.CompareOrdinal(a.Provider, b.Provider); return c != 0 ? c : string.CompareOrdinal(a.Subject, b.Subject); }
    }
    public interface IKnowledgeSubjectPolicy : IContentDefinition
    {
        IReadOnlyList<KnowledgeSubjectKey> Subjects { get; }
        void ValidateBinding(World world);
    }
    public interface IKnowledgeSharingPolicy : IKnowledgeSubjectPolicy
    { bool Allows(World world, PlayerId sender, PlayerId recipient); }
    public static class KnowledgeSubjectKeys
    {
        public static IReadOnlyList<KnowledgeSubjectKey> Read(JsonReader reader)
        {
            var array = reader.Array("subjects") ?? throw reader.Error("explicit knowledge subjects required");
            var keys = new List<KnowledgeSubjectKey>();
            for (int i = 0; i < array.Count; i++)
            {
                var entry = new JsonReader(array[i], reader.Where + ".subjects[" + i + "]");
                try { keys.Add(new KnowledgeSubjectKey(entry.String("provider"), entry.String("subject"))); }
                catch (ArgumentException error) { throw entry.Error(error.Message); }
                entry.End();
            }
            keys.Sort(KnowledgeSubjectKey.Compare); Validate(keys, reader.Where); return keys.AsReadOnly();
        }
        public static void Validate(IReadOnlyList<KnowledgeSubjectKey> keys, string context)
        {
            if (keys == null || keys.Count == 0) throw new ContentException(context + ": knowledge subjects required");
            for (int i = 0; i < keys.Count; i++)
            {
                try { ContractIdentity.ValidateId(keys[i].Provider, "knowledge provider"); ContractIdentity.ValidateId(keys[i].Subject, "knowledge subject"); }
                catch (ArgumentException error) { throw new ContentException(error.Message); }
                if (i > 0 && KnowledgeSubjectKey.Compare(keys[i - 1], keys[i]) >= 0) throw new ContentException(context + ": knowledge subjects must be canonical and unique");
            }
        }
    }
    /// <summary>Immutable authored intent. It owns no learned data, relationships or live state.</summary>
    public sealed class KnowledgeSharingDeclaration
    {
        private KnowledgeSharingDeclaration() { }
        public static KnowledgeSharingDeclaration Read(JsonReader reader)
        { var declaration = new KnowledgeSharingDeclaration(); declaration.ReadCore(reader); return declaration; }
        private readonly struct Direction
        {
            internal readonly PlayerId Sender, Recipient;
            internal Direction(int sender, int recipient) { Sender = new PlayerId(sender); Recipient = new PlayerId(recipient); }
        }
        private IReadOnlyList<KnowledgeSubjectKey> _subjects;
        private IReadOnlyList<Direction> _directions;
        public IReadOnlyList<KnowledgeSubjectKey> Subjects => _subjects;
        private void ReadCore(JsonReader reader)
        {
            _subjects = KnowledgeSubjectKeys.Read(reader);
            var directions = reader.Array("directions") ?? throw reader.Error("explicit sharing directions required");
            if (directions.Count == 0) throw reader.Error("sharing directions cannot be empty");
            var pairs = new List<Direction>();
            for (int i = 0; i < directions.Count; i++)
            {
                var entry = new JsonReader(directions[i], reader.Where + ".directions[" + i + "]");
                int sender = entry.Int("sender"), recipient = entry.Int("recipient"); entry.End();
                if (sender < 0 || recipient < 0 || sender == recipient) throw reader.Error("sharing needs distinct nonnegative company identities");
                pairs.Add(new Direction(sender, recipient));
            }
            pairs.Sort((a, b) => { int c = a.Sender.Value.CompareTo(b.Sender.Value); return c != 0 ? c : a.Recipient.Value.CompareTo(b.Recipient.Value); });
            for (int i = 1; i < pairs.Count; i++) if (pairs[i - 1].Sender == pairs[i].Sender && pairs[i - 1].Recipient == pairs[i].Recipient) throw reader.Error("duplicate sharing direction");
            _directions = pairs.AsReadOnly();
        }
        public bool Allows(PlayerId sender, PlayerId recipient)
        {
            foreach (var pair in _directions) if (pair.Sender == sender && pair.Recipient == recipient) return true;
            return false;
        }
        public void ValidateBinding(World world, string context)
        {
            foreach (var pair in _directions) if (!world.Players.IsSeat(pair.Sender) || !world.Players.IsSeat(pair.Recipient)) throw new ContentException(context + ": sharing direction is not seated");
        }
    }
    /// <summary>Registered immutable metadata using the sole authored policy declaration.</summary>
    public sealed class AuthoredKnowledgeSharingDef : Def, IKnowledgeSharingPolicy
    {
        private KnowledgeSharingDeclaration _declaration;
        public IReadOnlyList<KnowledgeSubjectKey> Subjects => _declaration.Subjects;
        protected override void ReadCore(JsonReader reader) => _declaration = KnowledgeSharingDeclaration.Read(reader);
        public bool Allows(World world, PlayerId sender, PlayerId recipient) => world.Content.Owns(this) && _declaration.Allows(sender, recipient);
        public void ValidateBinding(World world)
        { KnowledgeContentContracts.ValidateAlias(world, this); _declaration.ValidateBinding(world, Id); }
    }
    public interface IKnowledgeSharing
    {
        Code PreviewShare(PlayerId sender, PlayerId recipient, string policy, out KnowledgeShareSnapshot snapshot);
        Code Share(PlayerId sender, PlayerId recipient, string policy);
    }
    /// <summary>Caller-owned historical observation and admission blocker; never foreign holdings.</summary>
    public readonly struct KnowledgeSubjectSnapshot
    {
        public KnowledgeSubjectKey Key { get; }
        public KnowledgeObservation? Observation { get; }
        public Code Blocker { get; }
        public KnowledgeSubjectSnapshot(KnowledgeSubjectKey key, KnowledgeObservation? observation, Code blocker)
        { Key = key; Observation = observation; Blocker = blocker; }
    }
    public sealed class KnowledgeShareSnapshot
    {
        public PlayerId Sender { get; }
        public PlayerId Recipient { get; }
        public string Policy { get; }
        public IReadOnlyList<KnowledgeSubjectSnapshot> Entries { get; }
        public KnowledgeShareSnapshot(PlayerId sender, PlayerId recipient, string policy, IReadOnlyList<KnowledgeSubjectSnapshot> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            Sender = sender; Recipient = recipient; Policy = policy;
            var copy = new List<KnowledgeSubjectSnapshot>(); foreach (var entry in entries) copy.Add(entry); Entries = copy.AsReadOnly();
        }
    }
    public static class KnowledgeSharingCodes
    { public static readonly Code Unsupported = new Code("knowledge.no-sharing-capability"); }
    internal sealed class ShareKnowledgeHandler : CommandHandler<ShareKnowledgeCommand>
    {
        protected override Code Check(World world, ShareKnowledgeCommand command)
            => world.Services.Get<IKnowledgeSharing>().PreviewShare(command.Player, command.Recipient, command.Policy, out _);
        protected override void Apply(World world, ShareKnowledgeCommand command, CommandContext context)
        {
            Code refusal = world.Services.Get<IKnowledgeSharing>().Share(command.Player, command.Recipient, command.Policy);
            if (!refusal.IsNone) throw new InvalidOperationException("admitted knowledge sharing refused: " + refusal);
        }
    }
}
