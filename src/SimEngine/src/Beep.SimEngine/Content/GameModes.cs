using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Content
{
    public interface IGameModePropertyDefinition : IContentDefinition
    {
        GameModeValueKind Kind { get; }
        GameModeValue Default { get; }
        Code Capability { get; }
        bool Overridable { get; }
        IReadOnlyList<Code> RequiresWhenTrue { get; }
        void ValidateValue(GameModeValue value, string where);
    }
    public interface IGameModeDefinition : IContentDefinition
    {
        string Name { get; }
        IReadOnlyList<GameModeAssignment> Assignments { get; }
    }

    /// <summary>Authored schema, not live gameplay state. Every default and permitted value is explicit JSON.</summary>
    public sealed class GameModePropertyDef : Def, IGameModePropertyDefinition
    {
        public GameModeValueKind Kind { get; private set; }
        public GameModeValue Default { get; private set; }
        public Code Capability { get; private set; }
        public bool Overridable { get; private set; }
        public IReadOnlyList<Code> RequiresWhenTrue { get; private set; } = Array.AsReadOnly(Array.Empty<Code>());
        public Fix Minimum { get; private set; }
        public Fix Maximum { get; private set; }
        public IReadOnlyList<string> AllowedText { get; private set; } = Array.AsReadOnly(Array.Empty<string>());

        protected override void ReadCore(JsonReader r)
        {
            ValidateId(Id, "mode property", r);
            string capability = r.String("capability"); ValidateId(capability, "mode capability", r);
            Capability = new Code(capability); Overridable = r.Bool("overridable");
            switch (r.String("kind"))
            {
                case "boolean":
                    Kind = GameModeValueKind.Boolean;
                    if (r.Array("requires_when_true") == null) throw r.Error("requires_when_true must be explicitly authored");
                    string[] ids = r.Strings("requires_when_true");
                    Array.Sort(ids, StringComparer.Ordinal);
                    var prerequisites = new Code[ids.Length];
                    for (int i = 0; i < ids.Length; i++)
                    {
                        ValidateId(ids[i], "prerequisite property", r);
                        if (ids[i] == Id || i > 0 && ids[i] == ids[i - 1]) throw r.Error("prerequisites must be distinct and cannot reference self");
                        prerequisites[i] = new Code(ids[i]);
                    }
                    RequiresWhenTrue = Array.AsReadOnly(prerequisites);
                    break;
                case "number":
                    Kind = GameModeValueKind.Number; Minimum = r.Fix("minimum"); Maximum = r.Fix("maximum");
                    if (Minimum > Maximum) throw r.Error("minimum exceeds maximum");
                    break;
                case "text":
                    Kind = GameModeValueKind.Text;
                    if (r.Array("allowed") == null) throw r.Error("allowed text values must be explicitly authored");
                    string[] allowed = r.Strings("allowed"); Array.Sort(allowed, StringComparer.Ordinal);
                    if (allowed.Length == 0) throw r.Error("text properties require a nonempty allowed domain");
                    for (int i = 0; i < allowed.Length; i++)
                    {
                        ValidateId(allowed[i], "mode text value", r);
                        if (i > 0 && allowed[i] == allowed[i - 1]) throw r.Error("allowed text values must be distinct");
                    }
                    AllowedText = Array.AsReadOnly(allowed);
                    break;
                default: throw r.Error("kind must be boolean, number or text");
            }
            Default = GameModeValue.Read(r.Value("default"), r.Where + ".default");
            ValidateValue(Default, r.Where);
        }

        protected override void ResolveCore(ContentResolver c)
        {
            foreach (Code requirement in RequiresWhenTrue)
                if (c.Def<GameModePropertyDef>(requirement.Id).Kind != GameModeValueKind.Boolean)
                    throw c.Fail("prerequisite '" + requirement.Id + "' must be Boolean");
        }

        public void ValidateValue(GameModeValue value, string where)
        {
            if (Kind == GameModeValueKind.Unavailable || value.Kind != Kind)
                throw new ContentException(where + ": mode property '" + Id + "' requires " + Kind);
            if (Kind == GameModeValueKind.Number && (value.Number < Minimum || value.Number > Maximum))
                throw new ContentException(where + ": mode property '" + Id + "' is outside its authored range");
            if (Kind == GameModeValueKind.Text)
            {
                bool known = false;
                foreach (string allowed in AllowedText) if (allowed == value.Text) { known = true; break; }
                if (!known) throw new ContentException(where + ": mode property '" + Id + "' has unknown text value '" + value.Text + "'");
            }
        }

        internal static void ValidateId(string id, string kind, JsonReader r)
        {
            try { ContractIdentity.ValidateId(id, kind); }
            catch (ArgumentException error) { throw r.Error(error.Message); }
        }
    }

    /// <summary>A flat authored preset. Modes are open content identities, never a genre enum or another game.</summary>
    public sealed class GameModeDef : Def, IGameModeDefinition
    {
        public string Name { get; private set; }
        public IReadOnlyList<GameModeAssignment> Assignments { get; private set; }
        protected override void ReadCore(JsonReader r)
        {
            GameModePropertyDef.ValidateId(Id, "game mode", r);
            Name = r.String("name");
            if (string.IsNullOrWhiteSpace(Name)) throw r.Error("mode name must not be empty");
            Assignments = GameModeConfiguration.ReadAssignments(r.Object("properties"), r.Where + ".properties");
        }
        protected override void ValidateResolvedCore(ContentResolver c)
        {
            // Validate the complete preset even when no current scenario chooses it. The derived
            // configuration is temporary here; scenarios own their single effective projection.
            GameModeConfiguration.Resolve(c.Content, this, null, AuthoringPath);
        }
    }
}
