using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Shares actual authoring and native guarded reader. No independent values/reading rules.
    public class IndependentSignalContentDef : Def, IAutomationSignalContent
    {
        private AutomationSignalDeclaration _declaration;
        public AutomationSignalKind Kind => _declaration.Kind;
        public Code Metric => _declaration.Metric;
        public Code Unit => _declaration.Unit;
        public IReadOnlyList<AutomationSignalProviderSchema> Providers => _declaration.Providers;
        protected override void ReadCore(JsonReader reader) => _declaration=AutomationSignalDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content,this);
    }
    public sealed class IndependentResourceSignalContentDef : IndependentSignalContentDef, IResourceAutomationSignalDefinition
    {
        private string _resource;
        public IResourceDefinition Resource { get; private set; }
        public IUnitDefinition QuantityUnit { get; private set; }
        protected override void ReadCore(JsonReader reader)
        { base.ReadCore(reader);_resource=reader.String("resource");if(Kind!=AutomationSignalKind.Number)throw reader.Error("resource signals must be numeric"); }
        protected override void ResolveCore(ContentResolver content)
        {
            Resource=content.Def<IResourceContentDefinition>(_resource);QuantityUnit=content.Def<IUnitContentDefinition>(Unit.Id);
            if(Resource.QuantityUnit.Dimension!=QuantityUnit.Dimension)throw content.Fail("resource signal dimension mismatch");
            base.ResolveCore(content);
        }
    }
    public sealed class InjectSignalContractFaultCommand : Command
    {
        public EntityId Entity;
        public int Mode;
        public override void Sync(IStateSync sync){sync.Entity(ref Entity);sync.Int(ref Mode);}
    }
    public sealed class InjectSignalContractFaultHandler : CommandHandler<InjectSignalContractFaultCommand>
    {
        protected override Code Check(World world,InjectSignalContractFaultCommand command)
        {
            var entity=world.Entities.Get(command.Entity);
            return entity==null?Codes.NotFound:entity.Owner!=command.Player?Codes.NotOwned:
                entity.Get<AutomationProbe>()==null || (command.Mode!=1&&command.Mode!=5&&command.Mode!=6&&command.Mode!=7)?Codes.InvalidArgument:Code.None;
        }
        protected override void Apply(World world,InjectSignalContractFaultCommand command,CommandContext result)
            =>world.Entities.Get(command.Entity).Get<AutomationProbe>().FaultMode=command.Mode;
    }
    public sealed class SignalContractModule : ControlContractModule
    {
        public SignalContractModule(InitializeProbe initialize):base(initialize){}
        public bool IndependentDescriptors { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Section<IndependentSignalContentDef>("independent_signals");
            types.Section<IndependentResourceSignalContentDef>("independent_resource_signals");
        }
        public override void Compose(WorldComposer composer)
        {
            base.Compose(composer);
            IndependentDescriptors=true;
            foreach(var id in new[]{"contract.incident-active","contract.sensor-level","contract.plant-stock"})
            {
                var signal=composer.Content.Get<IAutomationSignalContent>(id);
                IndependentDescriptors &= !(signal is AutomationSignalDef)&&composer.Content.Owns(signal);
            }
            composer.Commands.Register("proof.inject-signal-contract-fault",new InjectSignalContractFaultHandler());
        }
    }
}
