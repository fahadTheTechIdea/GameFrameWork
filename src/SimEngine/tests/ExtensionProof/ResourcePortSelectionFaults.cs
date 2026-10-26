using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Deliberately violates engine contracts only in authored negative cases. No positive gameplay owner.
    public sealed class SelectionFaultProfileDef : Def
    {
        public string Mode { get; private set; }
        public ResourceId Resource { get; private set; }
        private string _resource;
        protected override void ReadCore(JsonReader reader)
        {
            Mode=reader.String("mode");_resource=reader.String("resource");
            if(Mode!="reserve"&&Mode!="throw"&&Mode!="clock"&&Mode!="duplicate"&&Mode!="scope"&&Mode!="initial-missing"&&Mode!="initial-undeclared")throw reader.Error("explicit selection fault mode required");
        }
        protected override void ResolveCore(ContentResolver content)=>Resource=content.Resource(_resource);
    }
    public sealed class SelectionFaultPortDef : ComponentDef<SelectionFaultPort>,IResourcePortSelectionDefinition
    {
        public bool BindsEntityInventory => Backing.BindsEntityInventory;
        public bool RetainsOriginalAccess => Backing.RetainsOriginalAccess;
        public ResourceEndpointId InitialEndpoint => Profile.Mode=="initial-missing"?default:Profile.Mode=="initial-undeclared"?
            new ResourceEndpointId(Backing.InitialEndpoint.Scope,long.MaxValue):Backing.InitialEndpoint;
        private string _profile;
        public SelectionFaultProfileDef Profile { get; private set; }
        public Code Source { get; private set; }
        public Code CapabilityId { get; private set; }
        private IResourcePortSelectionDefinition Backing=>Owner.Component<IResourcePortSelectionDefinition>(Source);
        public ICapacityPolicy CapacityPolicy=>Backing.CapacityPolicy;
        public bool Accepts(ResolvedContent content,ResourceId resource)=>Backing.Accepts(content,resource);
        public IReadOnlyList<ResourceEndpointId> Choices=>Profile.Mode=="duplicate"?
            Array.AsReadOnly(new[]{Backing.Choices[0],Backing.Choices[0]}):Profile.Mode=="scope"?
            Array.AsReadOnly(new[]{new ResourceEndpointId("missing.selection-provider",1)}):Backing.Choices;
        protected override void ReadCore(JsonReader reader)
        { CapabilityId=new Code(reader.String("id"));Source=new Code(reader.String("source"));_profile=reader.String("profile"); }
        protected override void ResolveCore(ContentResolver content)=>Profile=content.Def<SelectionFaultProfileDef>(_profile);
        protected override void ValidateCore(EntityDef owner)
        {
            if(Source==CapabilityId||Backing==null||Backing is SelectionFaultPortDef)throw new ContentException("fault selector needs its actual nonfaulting backing contract");
        }
    }
    public sealed class SelectionFaultPort : Component<SelectionFaultPortDef>,IResourcePortSelection
    {
        private IResourcePortSelection Backing=>Entity.Get<IResourcePortSelection>(Def.Source);
        public Code CapabilityId=>Def.CapabilityId;
        public ResourceEndpointId SelectedEndpoint=>Backing.SelectedEndpoint;
        public ResourcePortResolution ResolveStore(World world,ResourceEndpointId? original=null)=>Backing.ResolveStore(world,original);
        public IResourceOutlet ResolveChoice(World world,ResourceEndpointId endpoint)=>Backing.ResolveChoice(world,endpoint);
        public void Select(World world,ResourceEndpointId endpoint)
        {
            Backing.Select(world,endpoint);
            if(Def.Profile.Mode=="clock"){((Beep.SimEngine.Hosting.GameClock)world.Clock).Pause();return;}
            var store=Backing.ResolveChoice(world,endpoint);
            store.ReserveIn(new ReservationOwner("proof.selection-fault",1),Def.Profile.Resource,Fix.One);
            if(Def.Profile.Mode=="throw")throw new InvalidOperationException("authored selection callback failed after prefix");
        }
    }
}

