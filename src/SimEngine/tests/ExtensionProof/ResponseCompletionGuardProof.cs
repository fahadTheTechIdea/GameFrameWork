using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Stateless contract instrumentation executed by the engine's authored DAG, never by the driver.
    public sealed class ResponseCompletionGuardDef : Def, IWorkEffect
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        protected override void ReadCore(JsonReader reader) { }
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
        {
            var response = world.Entities.Get(order.Target)?.Get<IIncidentResponse>(order.ResponseSource);
            if (response == null) return WorkEffectResult.Failed(IncidentCodes.NotResponse);
            var before = response.ReadIncident();
            if (response.CompleteResponse(world, order) != IncidentCodes.StaleResponse)
                return WorkEffectResult.Failed(new Code("proof.unfinished-response-completed"));
            if (before.Episode > 1) {
                var old = world.Services.Get<WorkBoard>().ResponseOn(order.Target, order.ResponseSource, before.Episode - 1);
                if (old == null) return WorkEffectResult.Failed(new Code("proof.old-response-missing"));
                if (old.State != OrderState.Done || !old.CompletionEffectApplied)
                    return WorkEffectResult.Failed(new Code("proof.old-response-incomplete"));
                if (response.CompleteResponse(world, old) != IncidentCodes.StaleResponse)
                    return WorkEffectResult.Failed(new Code("proof.stale-response-completed"));
            }
            var after = response.ReadIncident();
            return after.Active && after.Episode == before.Episode && after.RaisedTick == before.RaisedTick
                ? WorkEffectResult.Applied : WorkEffectResult.Failed(new Code("proof.response-episode-changed"));
        }
    }
    public sealed class ResponseCompletionGuardModule : GameModule
    {
        public const string ModuleId = "proof.response-completion-guard";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(WorkModule.ModuleId, Version),
            new ModuleRequirement(IncidentResponseModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
            => types.Section<ResponseCompletionGuardDef>("response_guard_effects");
    }
}
