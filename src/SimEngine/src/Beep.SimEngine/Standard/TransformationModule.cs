using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional request interface; the existing logistics coordinator owns actual demands.</summary>
    public interface ITransformationSupplyRequests
    {
        void RequestInputs(ITransformer transformer, IReadOnlyList<LogisticsSupplyRequest> inputs, int priority);
        void WithdrawInputs(ITransformer transformer);
    }

    public sealed class TransformationModule : GameModule
    {
        public const string ModuleId = "sim.transformations";
        public const int SystemOrder = 350;
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[]
        {
            new ModuleRequirement(ContentAccessModule.ModuleId, Version),
            new ModuleRequirement(InventoryModule.ModuleId, Version),
            new ModuleRequirement(OperationsModule.ModuleId, Version)
        });
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<TransformerDef>("transformer", 10);
            types.Component<SignalTransformationGateDef>("signal_transformation_gate", 1);
            types.Component<StaffedTransformationGateDef>("staffed_transformation_gate", 2);
            types.Section<TransformationDef>("transformations");
            types.Section<TransformationDispatchEffectDef>("transformation_dispatch_effects");
        }
        public override void Compose(WorldComposer composer)
        {
            TransformationGateBindingContracts.ValidateComposition(composer.World);
            composer.Services.Require<IContentOperationAccess>();
            composer.Services.Require<IContentAccess>();
            composer.Events.Register<EntityTransformed>("sim.entity-transformed", 1);
            var transformation = new TransformerManager(composer.World);
            composer.Services.Bind(transformation);
            composer.Services.Bind<ITransformationQueue>(transformation);
            composer.Services.Bind<ITransformationOperationQueue>(transformation);
            // Scoped endpoint providers are discovered from declared owners, not one replaceable interface binding.
            composer.AddSystem(transformation, SystemOrder);
            composer.Commands.Register("sim.enqueue-transformation", new EnqueueTransformationHandler());
            composer.Commands.Register("sim.cancel-transformation", new CancelTransformationHandler());
            composer.AddViewFeature("sim.transformations", 24, (world, viewer) => new TransformationManagementView(world, viewer));
        }
    }
}
