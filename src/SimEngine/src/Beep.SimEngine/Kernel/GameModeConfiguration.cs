using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Kernel
{
    public enum GameModeValueKind : byte { Unavailable = 0, Boolean = 1, Number = 2, Text = 3 }
    public enum GameModeValueSource : byte { Unavailable = 0, PropertyDefault = 1, Profile = 2, Scenario = 3, Request = 4 }

    /// <summary>An immutable typed setting. Unavailable is not false, zero or an authored empty text.</summary>
    public readonly struct GameModeValue : IEquatable<GameModeValue>
    {
        public GameModeValueKind Kind { get; }
        private readonly bool _boolean;
        private readonly Fix _number;
        private readonly string _text;
        private GameModeValue(GameModeValueKind kind, bool boolean, Fix number, string text)
        { Kind = kind; _boolean = boolean; _number = number; _text = text; }
        public static GameModeValue FromBoolean(bool value) => new GameModeValue(GameModeValueKind.Boolean, value, Fix.Zero, null);
        public static GameModeValue FromNumber(Fix value) => new GameModeValue(GameModeValueKind.Number, false, value, null);
        public static GameModeValue FromText(string value)
            => new GameModeValue(GameModeValueKind.Text, false, Fix.Zero, value ?? throw new ArgumentNullException(nameof(value)));
        public bool Boolean => Kind == GameModeValueKind.Boolean ? _boolean : throw new InvalidOperationException("mode value is not Boolean");
        public Fix Number => Kind == GameModeValueKind.Number ? _number : throw new InvalidOperationException("mode value is not numeric");
        public string Text => Kind == GameModeValueKind.Text ? _text : throw new InvalidOperationException("mode value is not text");
        public bool Equals(GameModeValue other) => Kind == other.Kind && _boolean == other._boolean && _number == other._number && _text == other._text;
        public override bool Equals(object obj) => obj is GameModeValue value && Equals(value);
        public override int GetHashCode() => ((int)Kind * 397 ^ _boolean.GetHashCode()) * 397 ^ _number.GetHashCode() ^ (_text?.GetHashCode() ?? 0);

        // Tagged values have one wire representation for request intent and derived identity.
        internal static void Sync(IStateSync sync, ref GameModeValue value)
        {
            GameModeValueKind kind = value.Kind; sync.Enum(ref kind);
            switch (kind)
            {
                case GameModeValueKind.Boolean:
                    bool boolean = sync.Reading ? false : value.Boolean; sync.Bool(ref boolean);
                    if (sync.Reading) value = FromBoolean(boolean);
                    break;
                case GameModeValueKind.Number:
                    Fix number = sync.Reading ? Fix.Zero : value.Number; sync.Fix(ref number);
                    if (sync.Reading) value = FromNumber(number);
                    break;
                case GameModeValueKind.Text:
                    string text = sync.Reading ? null : value.Text; sync.String(ref text);
                    if (text == null) throw new CorruptStateException("mode text value must not be null");
                    if (sync.Reading) value = FromText(text);
                    break;
                default: throw new CorruptStateException("unavailable or unknown mode value cannot checkpoint");
            }
        }

        internal static GameModeValue Read(JsonValue value, string where)
        {
            if (value == null) throw new ContentException(where + ": an explicit value is required");
            switch (value.Kind)
            {
                case JsonKind.Bool: return FromBoolean(value.Bool);
                case JsonKind.Number:
                    if (!Fix.TryParse(value.Text, out Fix number)) throw new ContentException(where + ": value exceeds fixed-point representation");
                    return FromNumber(number);
                case JsonKind.String: return FromText(value.Text);
                default: throw new ContentException(where + ": mode values must be Boolean, fixed-point number or text");
            }
        }
    }

    public readonly struct GameModeAssignment
    {
        public Code Property { get; }
        public GameModeValue Value { get; }
        public GameModeAssignment(Code property, GameModeValue value)
        {
            ContractIdentity.ValidateId(property.Id, "mode property");
            if (value.Kind == GameModeValueKind.Unavailable) throw new ArgumentException("an explicit mode value is required", nameof(value));
            Property = property; Value = value;
        }
    }

    public readonly struct GameModePropertyView
    {
        public Code Property { get; }
        public Code Capability { get; }
        public GameModeValue Value { get; }
        public GameModeValueSource Source { get; }
        public bool Overridable { get; }
        internal GameModePropertyView(IGameModePropertyDefinition definition, GameModeValue value, GameModeValueSource source)
        { Property = new Code(definition.Id); Capability = definition.Capability; Overridable = definition.Overridable; Value = value; Source = source; }
    }

    public interface IGameModeConfiguration
    {
        string ModeId { get; }
        IReadOnlyList<GameModePropertyView> Properties { get; }
        bool TryGet(Code property, out GameModeValue value);
    }

    /// <summary>One immutable resolver over existing content. No gameplay state, clock, loop or second definition catalog.</summary>
    public sealed class GameModeConfiguration : IGameModeConfiguration
    {
        public string ModeId { get; }
        public IReadOnlyList<GameModePropertyView> Properties { get; }
        private GameModeConfiguration(string modeId, GameModePropertyView[] properties)
        { ModeId = modeId; Properties = Array.AsReadOnly(properties); }

        // Derived identity is checked against current content on restore, never installed as a
        // second mutable setting store. The same protocol participates in saves and state hashes.
        internal static void SyncIdentity(IStateSync sync, IGameModeConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            sync.Section("game-mode");
            string mode = configuration.ModeId; sync.String(ref mode);
            if (mode != configuration.ModeId) throw new CorruptStateException("saved game mode differs from selected scenario");
            int expected = configuration.Properties.Count;
            if (sync.Count(expected) != expected) throw new CorruptStateException("saved game mode property count differs");
            for (int i = 0; i < expected; i++)
            {
                GameModePropertyView property = configuration.Properties[i];
                string id = property.Property.Id, capability = property.Capability.Id;
                GameModeValue value = property.Value; GameModeValueSource source = property.Source;
                bool overridable = property.Overridable;
                sync.String(ref id); sync.String(ref capability); GameModeValue.Sync(sync, ref value); sync.Enum(ref source); sync.Bool(ref overridable);
                if (id != property.Property.Id || capability != property.Capability.Id || !value.Equals(property.Value) ||
                    source != property.Source || overridable != property.Overridable)
                    throw new CorruptStateException("saved game mode property declaration differs");
            }
        }

        internal static IReadOnlyList<GameModeAssignment> CaptureAssignments(IEnumerable<GameModeAssignment> assignments)
        {
            if (assignments == null) throw new ArgumentNullException(nameof(assignments));
            var captured = new List<GameModeAssignment>(assignments);
            captured.Sort((a, b) => string.CompareOrdinal(a.Property.Id, b.Property.Id));
            ValidateAssignments(captured);
            return Array.AsReadOnly(captured.ToArray());
        }

        internal static void ValidateSelectedCapabilities(ResolvedContent content, IGameModeConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            foreach (GameModePropertyView property in configuration.Properties)
                if (property.Value.Kind == GameModeValueKind.Boolean && property.Value.Boolean && !content.HasModule(property.Capability.Id))
                    throw new ContentException("enabled mode property '" + property.Property.Id + "' requires explicitly selected capability '" + property.Capability.Id + "'");
        }

        private static void ValidateAssignments(IReadOnlyList<GameModeAssignment> assignments)
        {
            for (int i = 0; i < assignments.Count; i++)
            {
                GameModeAssignment assignment = assignments[i];
                ContractIdentity.ValidateId(assignment.Property.Id, "mode property");
                if (assignment.Value.Kind == GameModeValueKind.Unavailable)
                    throw new ArgumentException("an explicit mode value is required");
                if (i > 0 && string.CompareOrdinal(assignments[i - 1].Property.Id, assignment.Property.Id) >= 0)
                    throw new ArgumentException("mode override identities must be distinct and canonical");
            }
        }

        internal static void SyncAssignments(IStateSync sync, ref IReadOnlyList<GameModeAssignment> assignments)
        {
            int count = sync.Count(assignments.Count);
            var restored = sync.Reading ? new GameModeAssignment[count] : null;
            for (int i = 0; i < count; i++)
            {
                string id = sync.Reading ? null : assignments[i].Property.Id;
                GameModeValue value = sync.Reading ? default : assignments[i].Value;
                sync.String(ref id); GameModeValue.Sync(sync, ref value);
                if (sync.Reading)
                {
                    try { restored[i] = new GameModeAssignment(new Code(id), value); }
                    catch (ArgumentException error) { throw new CorruptStateException("invalid saved mode override: " + error.Message); }
                }
            }
            if (!sync.Reading) return;
            try { ValidateAssignments(restored); }
            catch (ArgumentException error) { throw new CorruptStateException("invalid saved mode overrides: " + error.Message); }
            assignments = Array.AsReadOnly(restored);
        }

        public bool TryGet(Code property, out GameModeValue value)
        {
            int left = 0, right = Properties.Count - 1;
            while (left <= right)
            {
                int middle = left + (right - left) / 2;
                int order = string.CompareOrdinal(Properties[middle].Property.Id, property.Id);
                if (order == 0) { value = Properties[middle].Value; return true; }
                if (order < 0) left = middle + 1; else right = middle - 1;
            }
            value = default; return false;
        }

        internal static IReadOnlyList<GameModeAssignment> ReadAssignments(JsonValue assignments, string where)
        {
            if (assignments == null || !assignments.IsObject) throw new ContentException(where + ": properties must be an object");
            var result = new GameModeAssignment[assignments.Count];
            for (int i = 0; i < result.Length; i++)
            {
                string id = assignments.KeyAt(i);
                try { ContractIdentity.ValidateId(id, "mode property"); }
                catch (ArgumentException error) { throw new ContentException(where + ": " + error.Message); }
                result[i] = new GameModeAssignment(new Code(id), GameModeValue.Read(assignments.ValueAt(i), where + "." + id));
            }
            Array.Sort(result, (a, b) => string.CompareOrdinal(a.Property.Id, b.Property.Id));
            return Array.AsReadOnly(result);
        }

        internal static GameModeConfiguration Resolve(ResolvedContent content, IGameModeDefinition mode,
            IReadOnlyList<GameModeAssignment> overrides, string where, IReadOnlyList<GameModeAssignment> requestOverrides = null)
        {
            var definitions = new List<IGameModePropertyDefinition>(content.All<GameModePropertyDef>());
            definitions.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            var indices = new Dictionary<string, int>(StringComparer.Ordinal);
            var properties = new GameModePropertyView[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
            {
                IGameModePropertyDefinition definition = definitions[i];
                indices.Add(definition.Id, i);
                properties[i] = new GameModePropertyView(definition, definition.Default, GameModeValueSource.PropertyDefault);
            }
            Apply(mode.Assignments, false, GameModeValueSource.Profile);
            if (overrides != null) Apply(overrides, true, GameModeValueSource.Scenario);
            if (requestOverrides != null) Apply(requestOverrides, true, GameModeValueSource.Request);
            for (int i = 0; i < definitions.Count; i++)
            {
                IGameModePropertyDefinition definition = definitions[i];
                definition.ValidateValue(properties[i].Value, where);
                if (properties[i].Value.Kind != GameModeValueKind.Boolean || !properties[i].Value.Boolean) continue;
                foreach (Code required in definition.RequiresWhenTrue)
                {
                    if (!indices.TryGetValue(required.Id, out int prerequisite) ||
                        properties[prerequisite].Value.Kind != GameModeValueKind.Boolean || !properties[prerequisite].Value.Boolean)
                        throw new ContentException(where + ": enabled '" + definition.Id + "' requires enabled '" + required.Id + "'");
                }
            }
            return new GameModeConfiguration(mode.Id, properties);

            void Apply(IReadOnlyList<GameModeAssignment> assignments, bool isOverride, GameModeValueSource source)
            {
                foreach (GameModeAssignment assignment in assignments)
                {
                    if (!indices.TryGetValue(assignment.Property.Id, out int index))
                        throw new ContentException(where + ": unknown mode property '" + assignment.Property.Id + "'");
                    IGameModePropertyDefinition definition = definitions[index];
                    if (isOverride && !definition.Overridable)
                        throw new ContentException(where + ": mode property '" + definition.Id + "' does not permit overrides");
                    definition.ValidateValue(assignment.Value, where);
                    properties[index] = new GameModePropertyView(definition, assignment.Value, source);
                }
            }
        }
    }
}
