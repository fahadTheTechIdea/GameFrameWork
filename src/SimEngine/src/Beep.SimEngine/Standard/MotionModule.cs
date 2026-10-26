using Beep.SimEngine.Kernel;
using Beep.SimEngine.Content;
using Beep.SimEngine.Views;
using Beep.SimEngine.Commands;

namespace Beep.SimEngine.Standard
{
    /// <summary>Coordinate-neutral physical updates through the existing IMotion contract.</summary>
    public sealed class MotionModule : GameModule
    {
        public const string ModuleId = "sim.motion";
        public const int SystemOrder = 410;
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<MotionExecutionDef>("motion_execution", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            composer.AddSystem(new MovementSystem(), SystemOrder);
            composer.AddViewFeature(Id, 14, (world, viewer) => new MotionView(world, viewer));
            composer.Commands.Register("sim.configure-motion-navigation", new ConfigureMotionNavigationHandler());
            composer.Commands.Register("sim.configure-motion-navigation-group", new ConfigureMotionNavigationGroupHandler());
        }
    }
}
