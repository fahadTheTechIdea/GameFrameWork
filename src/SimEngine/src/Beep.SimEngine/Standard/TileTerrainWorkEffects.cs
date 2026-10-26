using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    public enum TileTerrainOperation : byte { Set=0, Add=1 }
    /// <summary>Immutable terrain intent. Resource stock changes use resource transactions, never terrain resets.</summary>
    public interface ITileTerrainWorkEffectDefinition : IPhysicalWorkEffect, IWorkEffectIntentAdmission, IIncidentExposureEffect
    {
        ISpatialLayerDefinition Layer { get; }
        Fix Value { get; }
        TileTerrainOperation Operation { get; }
    }

    public sealed class TileTerrainWorkDeclaration
    {
        private readonly string _layer;
        public ISpatialLayerDefinition Layer { get; private set; }
        public Fix Value { get; }
        public TileTerrainOperation Operation { get; }
        private TileTerrainWorkDeclaration(JsonReader reader)
        {
            _layer=reader.String("layer");Value=reader.Fix("value");
            string operation=reader.String("operation");
            Operation=operation=="set"?TileTerrainOperation.Set:operation=="add"?TileTerrainOperation.Add:
                throw reader.Error("terrain operation must be explicit set or add");
            try { ContractIdentity.ValidateId(_layer,"terrain work layer"); }
            catch(ArgumentException error) { throw reader.Error(error.Message); }
        }
        public static TileTerrainWorkDeclaration Read(JsonReader reader)
            => new TileTerrainWorkDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            Layer=content.Def<ISpatialLayerDefinition>(_layer);
            if(Layer.Space is not ITileSpaceDefinition || Layer.ResourceMeasure==SpatialResourceMeasure.Quantity)
                throw content.Fail("terrain work requires tile data, never a resource-quantity channel");
        }
    }

    /// <summary>One actual-layer/footprint rule. Existing physical work and spatial owners execute it.</summary>
    public static class TileTerrainWorkContracts
    {
        public static IReadOnlyList<IContentDefinition> References(ITileTerrainWorkEffectDefinition effect)
        {
            if(effect?.Layer?.Space==null)throw new ContentException("terrain work requires resolved layer/space references");
            return Array.AsReadOnly(new IContentDefinition[]{effect.Layer,effect.Layer.Space});
        }
        public static void RequireCapabilities(World world,ITileTerrainWorkEffectDefinition effect)
        {
            EntityObservation.ReadProvider(world,effect,null,()=>{
                if(effect is not Def authored || !authored.IsFrozen || !world.Content.Owns(effect) ||
                    !ReferenceEquals(world.Content.Find<ITileTerrainWorkEffectDefinition>(effect.Id),effect) ||
                    !ReferenceEquals(world.Content.Find<IWorkEffect>(effect.Id),effect) || effect.Layer==null ||
                    !ReferenceEquals(world.Content.Find<ISpatialLayerDefinition>(effect.Layer.Id),effect.Layer) ||
                    effect.Layer.ResourceMeasure==SpatialResourceMeasure.Quantity || world.Space is not ITileSampleMutationSpace space)
                    throw new ContentException("terrain work requires actual immutable aliases and selected atomic tile data");
                var layer=TileSpatialGeometry.RequireLayer<ITileWritableSampleLayer>(world,space,effect.Layer,false);
                if(effect.Operation!=TileTerrainOperation.Set&&effect.Operation!=TileTerrainOperation.Add ||
                    layer.Version<0 || !layer.CheckSample(new TileCoord(0,0),effect.Value).IsNone)
                    throw new ContentException(effect.Id+": invalid actual terrain sample value/revision");
                return true;
            },world.Space);
        }
        public static Code CheckIntent(World world,ITileTerrainWorkEffectDefinition effect,PlayerId company,Entity target)
            =>BuildChanges(world,effect,company,target,out _);
        private static Code BuildChanges(World world,ITileTerrainWorkEffectDefinition effect,PlayerId company,Entity target,out List<TileSampleMutation> writes)
        {
            writes=null;
            RequireCapabilities(world,effect);
            if(target==null || !target.Alive || !ReferenceEquals(world.Entities.Get(target.Id),target))return Codes.NotFound;
            if(target.Owner!=company)return Codes.NotOwned;
            var presence=target.Get<ITilePresence>();
            if(presence==null || !presence.Placed || presence.Space!=effect.Layer.Space.Id)return Codes.InvalidArgument;
            if(presence.Movable)return WorkEffectCodes.MovingSource;
            var space=(ITileSampleMutationSpace)world.Space;
            if(!space.InBounds(presence.Footprint))return Codes.InvalidArgument;
            var layer=TileSpatialGeometry.RequireLayer<ITileWritableSampleLayer>(world,space,effect.Layer,false);
            if(layer.Visibility==SpatialLayerVisibility.CompanyPrivate && layer.Owner!=company)return Codes.NotOwned;
            var area=presence.Footprint;
            var candidate=new List<TileSampleMutation>();
            for(int y=area.Y;y<area.Bottom;y++)for(int x=area.X;x<area.Right;x++){
                var tile=new TileCoord(x,y);Fix value;
                try{value=effect.Operation==TileTerrainOperation.Add?layer.GetSample(tile)+effect.Value:effect.Value;}
                catch(OverflowException){return Codes.InvalidArgument;}
                Code refusal=layer.CheckSample(tile,value);if(!refusal.IsNone)return refusal;
                candidate.Add(new TileSampleMutation(effect.Layer,tile,value));
            }
            writes=candidate;
            return Code.None;
        }
        public static IncidentExposurePreparation PrepareTerrain(World world,ITileTerrainWorkEffectDefinition effect,Entity target)
        {
            Code refusal=BuildChanges(world,effect,target?.Owner??PlayerId.None,target,out var writes);
            if(!refusal.IsNone)return new IncidentExposurePreparation(refusal);
            var space=(ITileSampleMutationSpace)world.Space;
            var layer=TileSpatialGeometry.RequireLayer<ITileWritableSampleLayer>(world,space,effect.Layer,false);
            int revision=layer.Version;PlayerId layerOwner=layer.Owner,company=target.Owner;
            var footprint=target.Get<ITilePresence>().Footprint;long step=world.Clock.StepIndex;
            return new IncidentExposurePreparation(()=>{
                world.RequireHealthyUpdate();
                if(layer.Version!=revision||layer.Owner!=layerOwner||world.Clock.StepIndex!=step||
                    !ReferenceEquals(world.Space,space)||!ReferenceEquals(world.Entities.Get(target.Id),target)||target.Owner!=company||
                    !target.Get<ITilePresence>().Footprint.Equals(footprint))
                    throw new InvalidOperationException("prepared terrain publication no longer names its admitted footprint/revision");
                space.ApplySamples(writes);
            });
        }
        public static Code CheckSubject(World world,ITileTerrainWorkEffectDefinition effect,Entity subject,Order order,IActivityNodeDefinition node)
        {
            if(node==null ? order.NodesDone!=order.Nodes.Count : !order.Node(node.Id).Progress.Complete(world.Time.Ticks(node.Duration)))
                return WorkEffectCodes.NotComplete;
            return CheckIntent(world,effect,order.Owner,subject);
        }
        public static Code CommitSubject(World world,ITileTerrainWorkEffectDefinition effect,Entity subject,Order order,IActivityNodeDefinition node)
        {
            Code refusal=CheckSubject(world,effect,subject,order,node);if(!refusal.IsNone)return refusal;
            var prepared=PrepareTerrain(world,effect,subject);if(!prepared.Ready)return prepared.Blocker;
            prepared.Commit();return Code.None;
        }
    }

    public sealed class TileTerrainWorkEffectDef : Def,ITileTerrainWorkEffectDefinition
    {
        private TileTerrainWorkDeclaration _declaration;
        public ISpatialLayerDefinition Layer=>_declaration.Layer;
        public Fix Value=>_declaration.Value;
        public TileTerrainOperation Operation=>_declaration.Operation;
        public IReadOnlyList<IContentDefinition> ContentDependencies=>TileTerrainWorkContracts.References(this);
        protected override void ReadCore(JsonReader reader)=>_declaration=TileTerrainWorkDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content)=>_declaration.Resolve(content);
        public void RequireCapabilities(World world)=>TileTerrainWorkContracts.RequireCapabilities(world,this);
        public void ValidatePlan(World world,IActivityDefinition plan,IActivityNodeDefinition node)
            =>PhysicalWorkEffectContracts.ValidatePlan(world,this,plan,node);
        public Code CheckIntent(World world,PlayerId company,Entity target)=>TileTerrainWorkContracts.CheckIntent(world,this,company,target);
        public Code CheckStart(World world,PlayerId company,Entity target)=>CheckIntent(world,company,target);
        public Entity ResolveSubject(World world,Order order,IActivityNodeDefinition node)=>world.Entities.Get(order.Target);
        public Code CheckSubject(World world,Entity subject,Order order,IActivityNodeDefinition node)=>TileTerrainWorkContracts.CheckSubject(world,this,subject,order,node);
        public Code CommitSubject(World world,Entity subject,Order order,IActivityNodeDefinition node)=>TileTerrainWorkContracts.CommitSubject(world,this,subject,order,node);
        public Code Check(World world,Order order,IActivityNodeDefinition node)=>PhysicalWorkEffectContracts.Check(world,this,order,node);
        public WorkEffectResult Apply(World world,Order order,IActivityNodeDefinition node)=>PhysicalWorkEffectContracts.Apply(world,this,order,node);
        public Code CheckExposure(IncidentPropagationContext context,Entity target)
            =>context.World==null||context.Source==null||!context.Incident.Active?IncidentPropagationCodes.SourceEnded:
                TileTerrainWorkContracts.CheckIntent(context.World,this,target?.Owner??PlayerId.None,target);
        public IncidentExposurePreparation PrepareExposure(IncidentPropagationContext context,Entity target)
        {
            Code refusal=CheckExposure(context,target);return !refusal.IsNone?new IncidentExposurePreparation(refusal):
                TileTerrainWorkContracts.PrepareTerrain(context.World,this,target);
        }
    }

    public sealed class TileTerrainWorkModule : GameModule
    {
        public const string ModuleId="sim.tile-terrain-work";
        public override string Id=>ModuleId;
        public override ModuleVersion Version=>new ModuleVersion(1,0,0);
        public override IReadOnlyList<ModuleRequirement> Requires=>new[]{new ModuleRequirement(WorkModule.ModuleId,Version)};
        public override void DeclareContent(ContentTypes types)=>types.Section<TileTerrainWorkEffectDef>("tile_terrain_work_effects");
        public override void Compose(WorldComposer composer)
        {
            foreach(var effect in composer.Content.All<ITileTerrainWorkEffectDefinition>())
                TileTerrainWorkContracts.RequireCapabilities(composer.World,effect);
        }
    }
}
