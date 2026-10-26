using System;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Content
{
    /// <summary>Immutable authored state metadata; no entity state, transition or update owner.</summary>
    public interface IEntityStateDefinition : IContentDefinition
    {
        Code State { get; }
        int Priority { get; }
        bool Default { get; }
    }

    /// <summary>Sole JSON declaration shared by native and independent state definitions.</summary>
    public sealed class EntityStateDeclaration
    {
        public Code State { get; private set; }
        public int Priority { get; private set; }
        public bool Default { get; private set; }

        public void Read(JsonReader r)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            string state = r.String("state");
            if (string.IsNullOrWhiteSpace(state)) throw r.Error("a published state code is required");
            State = new Code(state);
            Priority = r.Int("priority");
            Default = r.Bool("default", false);
        }

        public static void Validate(IEntityStateDefinition state, ContentResolver content)
        {
            if (state == null || content == null) throw new ArgumentNullException();
            try
            {
                if (!content.Content.Owns(state) || !ReferenceEquals(content.Content.Find<IEntityStateDefinition>(state.Id), state)
                    || string.IsNullOrWhiteSpace(state.State.Id))
                    throw content.Fail("entity state requires its actual owned identity and a published state code");
                // Reading every field admits actual provider metadata before entities resolve or Initialize.
                _ = state.Priority; _ = state.Default;
            }
            catch (Exception error) when (!(error is ContentException))
            { throw content.Fail("invalid entity state metadata: " + error.Message); }
        }
    }

    /// <summary>Normal native JSON registration, not a required mod authoring base.</summary>
    public sealed class EntityStateDef : Def, IEntityStateDefinition
    {
        private readonly EntityStateDeclaration _declaration = new EntityStateDeclaration();
        public Code State => _declaration.State;
        public int Priority => _declaration.Priority;
        public bool Default => _declaration.Default;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
    }
}
