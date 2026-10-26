using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Engine-side content extension, private contracts only; no scheduler, state table or fixture/player rule.</summary>
    public sealed class WorkerPolicyPressureDef : Def, IPressureSource
    {
        private PressureSourceDeclaration _declaration;
        public Fix Cost => _declaration.Cost;
        public int Priority => _declaration.Priority;
        private string _worker;
        private EntityDef _definition;
        private GameDuration _after;
        private WorkPolicySettings _settings;
        protected override void ReadCore(JsonReader reader)
        {
            _declaration = PressureSourceDeclaration.Read(reader);
            _worker = reader.String("worker"); _after = (reader.Child("after") ?? throw reader.Error("explicit elapsed duration required")).RationalDuration();
            _settings = WorkPolicySettings.Read(reader.Child("settings") ?? throw reader.Error("explicit worker policy required"));
        }
        protected override void ResolveCore(ContentResolver content) => _definition = content.Def<EntityDef>(_worker);
        public void ValidateBinding(World world, PlayerId company)
        {
            if (!_definition.HasCapability<IWorkPolicy>() || !world.Commands.TryGet(typeof(ConfigureWorkPolicyCommand), out _))
                throw new ContentException("pressure worker source needs actual policy/command");
            foreach (WorkerRoleId role in _settings.AllowedRoles) world.Content.Get<IWorkerRoleDefinition>(role.Value);
            _ = world.Time.Ticks(_after);
        }
        public PressureObservation Evaluate(PressureContext context)
        {
            if (context.Tick < context.Time.Ticks(_after)) return PressureObservation.Wait(new Code("proof.pressure.grace"));
            var entities = new List<Beep.SimEngine.Hosting.EntityView>(); context.View.CopyEntities(context.Company, entities);
            entities.Sort((a, b) => a.Id.CompareTo(b.Id));
            foreach (var entity in entities)
                if (entity.Def == _definition.Id && context.View.Feature<WorkView>().TryReadPolicy(entity.Id, out WorkerPolicyView policy).IsNone && !policy.Settings.Enabled)
                    return PressureObservation.Propose(new ConfigureWorkPolicyCommand { Worker = entity.Id, Settings = _settings });
            return PressureObservation.Wait(PressureCodes.NoTarget);
        }
    }
    public sealed class ScenarioPressureProofModule : ScenarioPressureModule
    {
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types); types.Section<WorkerPolicyPressureDef>("worker_policy_pressure_sources");
            types.Component<DialOperationDef>("pressure_dial", 1);
        }
    }
}
