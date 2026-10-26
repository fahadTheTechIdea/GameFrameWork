using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Metadata only; creates the same native TilePresence and codec1, no second geometry runtime.
    public sealed class IndependentTilePresenceDef : ComponentDef<TilePresence>, ITilePresenceDefinition
    {
        private TilePresenceDeclaration _declaration;
        private string _fault;
        internal Action ObserveWidth;
        public string Space=>_fault=="space"?"presence.missing":_declaration.Space;
        public int Width { get { ObserveWidth?.Invoke();return _fault=="width"?0:_fault=="oversized"?int.MaxValue:_declaration.Width; } }
        public int Height=>_fault=="height"?0:_declaration.Height;
        public bool Blocks=>_declaration.Blocks;
        public bool Movable=>_declaration.Movable;
        protected override void ReadCore(JsonReader reader) { _declaration=TilePresenceDeclaration.Read(reader);_fault=reader.String("proof_fault",null); }
        protected override void ResolveCore(ContentResolver content)=>_declaration.Resolve(content);
        protected override void ValidateCore(EntityDef entity)=>_declaration.ValidateBinding(entity);
        public void ValidateComposition(World world,EntityDef entity)=>TilePresenceDeclaration.ValidateComposition(world,entity,this);
    }
    public sealed class TilePresenceBindingModule : GameModule
    {
        public override string Id=>"proof.tile-presence-bindings";
        public override ModuleVersion Version=>new ModuleVersion(1,0,0);
        public override IReadOnlyList<ModuleRequirement> Requires=>new[]{new ModuleRequirement(TileNavigationModule.ModuleId,Version)};
        public readonly InitializeProbe Initialize=new InitializeProbe();
        private World _world;
        public int RegionProbeReads { get; private set; }
        public override void DeclareContent(ContentTypes types)=>types.Component<IndependentTilePresenceDef>("independent_tile_presence",1);
        public override void Compose(WorldComposer composer)
        {
            _world=composer.World;composer.AddSystem(Initialize,305);
            var settings=composer.Scenario.SettingsFor(Id);
            bool owned=settings?.Bool("owner_visibility",false)??false;settings?.End();
            if(owned)composer.Services.Bind<IVisibility>(new OwnerVisibility());
        }
        private sealed class OwnerVisibility:IVisibility
        {
            public bool CanSee(World world,PlayerId viewer,Entity entity)=>entity.Owner==viewer;
            public bool CanSeeContent(World world,PlayerId viewer,IContentDefinition definition)=>true;
        }
        public void ArmRegion(string entity,string mode)
        {
            var definition=(IndependentTilePresenceDef)_world.Content.Get<EntityDef>(entity).Component<ITilePresenceDefinition>();
            definition.ObserveWidth=()=>
            {
                if(RegionProbeReads!=0)return;RegionProbeReads++;
                var map=(TileMap)_world.Space;MapLayer layer=null;
                foreach(var candidate in map.Layers)if(candidate.ResourceMeasure==SpatialResourceMeasure.None){layer=candidate;break;}
                if(layer==null)throw new InvalidOperationException("missing actual scalar probe layer");
                var tile=new TileCoord(0,0);Fix prior=layer.GetSample(tile);
                layer.SetSample(tile,prior==Fix.Zero?Fix.One:Fix.Zero);
                if(mode=="space-restore")layer.SetSample(tile,prior);
                if(mode=="throw-after-space")throw new InvalidOperationException("proof.regional-query-original-error");
            };
        }
        // Deliberately invalid metadata callback, armed only after a healthy session starts.
        public void Arm(string entity,string mode)
        {
            World world=_world;
            var definition=(IndependentTilePresenceDef)world.Content.Get<EntityDef>(entity).Component<ITilePresenceDefinition>();
            definition.ObserveWidth=()=>{
                if(mode=="clock"||mode=="throw-after-clock")((GameClock)world.Clock).Speed=2;
                if(mode=="throw"||mode=="throw-after-clock")throw new InvalidOperationException("proof.tile-presence-first-error");
            };
        }
    }
}
