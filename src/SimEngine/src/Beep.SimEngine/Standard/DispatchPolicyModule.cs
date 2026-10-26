using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional authored dispatch rules; no scheduler, system, mutable store or state codec.</summary>
    public sealed class DispatchPolicyModule : GameModule
    {
        public const string ModuleId = "sim.dispatch-policy";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Section<AuthoredDispatchProfileDef>("dispatch_profiles");
        public override void Compose(WorldComposer composer)
        {
            JsonReader settings = composer.Scenario.SettingsFor(ModuleId)
                ?? throw new ContentException("selected dispatch-policy needs an explicit profile");
            string profile = settings.String("profile"); settings.End();
            IWorkerDispatchPolicy selected = composer.Content.Get<IWorkerDispatchPolicy>(profile);
            composer.Services.Bind<IWorkerDispatchPolicy>(selected);
            if (selected is IWorkerDispatchPreemptionPolicy preemption)
                composer.Services.Bind<IWorkerDispatchPreemptionPolicy>(preemption);
        }
    }
}
