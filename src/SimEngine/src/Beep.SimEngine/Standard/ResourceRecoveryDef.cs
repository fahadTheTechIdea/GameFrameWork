using Beep.SimEngine.Content;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable selected content only; stock/packing stays with the existing recovery owner.</summary>
    public class ResourceRecoveryDef : Def, IResourceRecoveryDefinition
    {
        private ResourceRecoveryDeclaration _declaration;
        public string Space => _declaration.Space;
        public EntityDef Container => _declaration.Container;
        protected override void ReadCore(JsonReader reader)
            => _declaration = ResourceRecoveryDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver resolver)
            => _declaration.Resolve(resolver);
        protected override void ValidateResolvedCore(ContentResolver resolver)
            => _declaration.Validate(resolver);
    }
}
