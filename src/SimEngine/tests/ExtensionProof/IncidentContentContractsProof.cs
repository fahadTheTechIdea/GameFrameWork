using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentIncidentWorkflowDef : Def, IIncidentWorkflowDefinition
    {
        private IncidentWorkflowDeclaration _declaration;
        public Code Kind => _declaration.Kind;
        public IActivityDefinition Response => _declaration.Response;
        public int Priority => _declaration.Priority;
        public IReadOnlyList<IIncidentPhaseDefinition> Phases => _declaration.Phases;
        protected override void ReadCore(JsonReader reader) => _declaration = IncidentWorkflowDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content, this);
    }

    public sealed class IndependentIncidentCompletionDef : Def, IIncidentCompletionEffect
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        private IncidentCompletionDeclaration _declaration;
        public Code Source => _declaration.Source;
        public bool BoundResponse => _declaration.BoundResponse;
        protected override void ReadCore(JsonReader reader) => _declaration = IncidentCompletionDeclaration.Read(reader);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => ResponseWork.ApplyCompletion(world, this, order, node);
    }

    public class IncidentContractModule : ControlContractModule
    {
        public IncidentContractModule(InitializeProbe initialize) : base(initialize) { }
        public bool IndependentIncidentContent { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Section<IndependentIncidentWorkflowDef>("independent_incident_workflows");
            types.Section<IndependentIncidentCompletionDef>("independent_incident_completions");
        }
        public override void Compose(WorldComposer composer)
        {
            base.Compose(composer);
            var workflow = composer.Content.Get<IIncidentWorkflowDefinition>("pressure.leak");
            var effect = composer.Content.Get<IWorkEffect>("pressure.resolve");
            IndependentIncidentContent = !(workflow is IncidentWorkflowDef) && effect.GetType().BaseType == typeof(Def)
                && composer.Content.Owns(workflow) && composer.Content.Owns(effect)
                && workflow.Response.OnComplete == effect.Id;
        }
    }
}
