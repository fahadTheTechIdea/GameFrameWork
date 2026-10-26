using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // New authored kind, same original physical runtime and declaration. No proxy or copied processor.
    public class IndependentProcessorDef : ComponentDef<Transformer>, ITransformerWipDefinition, IAutomationSignalDefinition
    {
        private TransformerDeclaration _declaration; private string _fault;
        public int OperationSchemaVersion => 1;
        public int SignalSchemaVersion => 1;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => TransformerDeclaration.Signals;
        public ResolvedContent Content => _fault == "content" ? null : _declaration.Content;
        public ICapacityPolicy WipCapacity => _fault == "capacity" ? null : _declaration.WipCapacity;
        public virtual IReadOnlyList<ITransformationDefinition> Transformations => _fault == "duplicate"
            ? Array.AsReadOnly(new[] { _declaration.Transformations[0], _declaration.Transformations[0] }) : _declaration.Transformations;
        public OperationSettings ControlSettings => _declaration.ControlSettings;
        public Code CapabilityId => ControlSettings.Id;
        public EntityStateBindings States => _fault == "states" ? null : _declaration.States;
        public bool AutomaticSupply => _declaration.AutomaticSupply;
        public ITransformationDefinition IdleTransformation => _fault == "unsupported-repeat" ? _declaration.Content.Get<ITransformationDefinition>("contract.unused-recipe")
            : _declaration.IdleTransformation;
        public int Resolutions { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _fault = reader.String("fault", "none"); _declaration = TransformerDeclaration.Read(reader);
        }
        protected override void ResolveCore(ContentResolver content) { _declaration.Resolve(content); Resolutions++; }
    }
    public sealed class ProcessorContractModule : GameModule
    {
        public override string Id => "proof.processor-contracts";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(TransformationModule.ModuleId, Version) };
        public bool ActualMetadata { get; private set; }
        public override void DeclareContent(ContentTypes types) => types.Component<IndependentProcessorDef>("independent_processor", 9);
        public override void Compose(WorldComposer composer)
        {
            var entity = composer.Content.Get<EntityDef>("contract.factory");
            var definition = entity.Component<ITransformerWipDefinition>(new Code("contract.process"));
            ActualMetadata = definition is IndependentProcessorDef independent && independent.Resolutions == 1
                && ReferenceEquals(definition.Content, composer.Content) && definition.ComponentType == typeof(Transformer)
                && ReferenceEquals(entity.Component<ITransformerDefinition>(definition.CapabilityId), definition)
                && entity.HasCapability<ITransformerTools>(definition.CapabilityId);
        }
    }
}
