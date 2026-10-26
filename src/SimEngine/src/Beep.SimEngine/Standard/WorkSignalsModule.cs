using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional stateless bridge. Existing work and automation keep their sole owners.</summary>
    public sealed class WorkSignalsModule : GameModule
    {
        public const string ModuleId = "sim.work-signals";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[]
        {
            new ModuleRequirement(WorkModule.ModuleId, Version),
            new ModuleRequirement(OperationsModule.ModuleId, Version)
        });
        public override void DeclareContent(ContentTypes types) => types.Component<WorkLoadSignalDef>("work_load_signal", 1);
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Require<IWorkLoad>();
            WorkLoadSignalBindingContracts.ValidateComposition(composer.World);
        }
    }
}
