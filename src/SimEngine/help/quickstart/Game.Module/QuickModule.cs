using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace QuickGame.Engine
{
    // Code availability and dependencies only. All startup intent is authored JSON.
    public sealed class QuickModule : GameModule
    {
        public override string Id => "quick";
        public override ModuleVersion Version => new ModuleVersion(1, 1, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {
            new ModuleRequirement(ContentAccessModule.ModuleId, new ModuleVersion(1, 0, 0)),
            new ModuleRequirement(WorldSetupModule.ModuleId, new ModuleVersion(1, 0, 0)) });
        public static GameDefinition CreateDefinition() => new GameDefinition("quick-game", new GameModule[] {
            new ContentAccessModule(), new WorldSetupModule(), new QuickModule() });
    }
}
