using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Geometry only, compiled against public contracts. Original sparse layers/presence own all state.
    public sealed partial class PointSpace : ILineGeometryComposition, ILineRemovalGeometry
    {
        public string Space => Id;
        public void ComposeLineGeometry(WorldComposer composer)
        {
            if (!ReferenceEquals(composer.World.Space,this)) throw new ContentException("point corridor requires actual selected space");
            if (!ReferenceEquals(composer.Services.Get<ILineGeometry>(),this))
                throw new ContentException("point corridor geometry must retain its original provider/module owner");
        }
        public void ValidateLine(World world, ILineDefinition line)
        {
            if (!ReferenceEquals(world.Space,this) || line.Layer.Space.Id != Id || line.Layer.ValueType != SpatialLayerValueType.Fixed ||
                Layer(line.Layer.Id) == null || line.WorkMarker.Component<PointPresenceDef>()?.Space != Id)
                throw new ContentException(line.Id + ": point corridor requires actual sparse fixed layer and point cursor");
            if(line.Owned&&(line.OwnerLayer.Space.Id!=Id||Layer(line.OwnerLayer.Id)?.ValueType!=SpatialLayerValueType.Int32))
                throw new ContentException(line.Id+": point owned corridor requires actual integer owner layer");
        }
        public Code CheckPath(World world, ILineDefinition line, IReadOnlyList<SpatialPlacement> path)
        {
            if (!ReferenceEquals(world.Space,this) || line.Layer.Space.Id != Id) return LineCodes.WrongSpace;
            if (path == null || path.Count == 0) return Codes.InvalidArgument;
            var seen = new HashSet<long>();
            foreach (SpatialPlacement placement in path) {
                if (!(placement is PointPlacement point) || point.Space != Id) return Codes.InvalidArgument;
                if (!InLayerBounds(point.Position)) return Codes.OutOfBounds;
                if (!seen.Add(point.Position.Raw)) return LineCodes.NotContiguous;
            }
            return Code.None;
        }
        public Code CheckSegment(World world, PlayerId company, ILineDefinition line, SpatialPlacement placement, out bool alreadyLaid)
        {
            alreadyLaid = false;
            if (!ReferenceEquals(world.Space,this) || !(placement is PointPlacement point) || point.Space != Id) return LineCodes.WrongSpace;
            Code refusal = CheckWrite(world,company,line.Layer.Id,point.Position,line.Value);
            if (!refusal.IsNone) return refusal;
            if(line.Owned) {
                if(Layer(line.Layer.Id).Read(point.Position).IsPositive&&ReadLineCompany(world,line,point.Position)!=company)return LineCodes.ForeignLine;
                refusal=CheckWrite(world,company,line.OwnerLayer.Id,point.Position,Fix.FromInt(checked(company.Value+1)));
                if(!refusal.IsNone)return refusal;
            }
            alreadyLaid = Layer(line.Layer.Id).Read(point.Position) >= line.Value; return Code.None;
        }
        public bool IsCursorAt(World world, Entity cursor, SpatialPlacement placement)
            => ReferenceEquals(world.Space,this) && placement is PointPlacement point && point.Space == Id &&
                cursor?.Get<PointPresence>() is PointPresence presence && presence.Space == Id && presence.Position == point.Position;
        public void MoveCursor(World world, Entity cursor, SpatialPlacement placement)
        {
            if (!(placement is PointPlacement point) || point.Space != Id) throw new InvalidOperationException("point cursor placement differs");
            cursor.Get<PointPresence>().Move(world,point.Position);
        }
        public Code Stamp(World world, PlayerId company, ILineDefinition line, SpatialPlacement placement)
        {
            Code refusal = CheckSegment(world,company,line,placement,out bool laid);
            if(!refusal.IsNone||laid)return refusal;
            Fix point=((PointPlacement)placement).Position;
            return line.Owned?WriteLayers(world,company,new[]{
                (Layer:line.OwnerLayer.Id,Point:point,Value:Fix.FromInt(checked(company.Value+1))),
                (Layer:line.Layer.Id,Point:point,Value:line.Value)}):WriteLayer(world,company,line.Layer.Id,point,line.Value);
        }
        private PlayerId ReadLineCompany(World world,ILineDefinition line,Fix point)
        {
            if(!line.Owned)return PlayerId.None;
            Fix value=Layer(line.OwnerLayer.Id).Read(point);
            int encoded=(int)(value.Raw/Fix.One.Raw);
            PlayerId company=encoded>0?new PlayerId(encoded-1):PlayerId.None;
            if(!world.Players.IsSeat(company))throw new CorruptStateException("point corridor has invalid actual company owner");
            return company;
        }
        public Code ReadRemovalPath(World world, ILineDefinition line, Entity target, out IReadOnlyList<SpatialPlacement> path)
        {
            path = null;
            if (!ReferenceEquals(world.Space,this) || line.Layer.Space.Id != Id) return LineCodes.WrongSpace;
            PointPresence presence = target?.Get<PointPresence>();
            if (presence == null || target.Get<IMotionEstimate>() != null) return Codes.InvalidArgument;
            if (presence.Space != Id) return LineCodes.WrongSpace;
            path = Array.AsReadOnly(new SpatialPlacement[]{new PointPlacement(Id,presence.Position)}); return Code.None;
        }
        public Code CheckRemoval(World world, PlayerId company, ILineDefinition line, IReadOnlyList<SpatialPlacement> path)
            =>world.Services.Get<INetworkTopology>().CheckRemoval(company,line,path);
        public Code Remove(World world, PlayerId company, ILineDefinition line, IReadOnlyList<SpatialPlacement> path)
            =>world.Services.Get<INetworkTopology>().Remove(company,line,path);
        public Code CheckErase(World world, PlayerId company, ILineDefinition line, IReadOnlyList<SpatialPlacement> path)
        {
            Code refusal = CheckPath(world,line,path); if (!refusal.IsNone) return refusal;
            foreach (PointPlacement point in path) {
                refusal = CheckWrite(world,company,line.Layer.Id,point.Position,Fix.Zero);
                if (!refusal.IsNone) return refusal;
                if (!Layer(line.Layer.Id).Read(point.Position).IsPositive) return Codes.NotFound;
                if(line.Owned) {
                    if(ReadLineCompany(world,line,point.Position)!=company)return LineCodes.ForeignLine;
                    refusal=CheckWrite(world,company,line.OwnerLayer.Id,point.Position,Fix.Zero);if(!refusal.IsNone)return refusal;
                }
            }
            return Code.None;
        }
        public void Erase(World world, PlayerId company, ILineDefinition line, IReadOnlyList<SpatialPlacement> path)
        {
            Code refusal = CheckErase(world,company,line,path);
            if(!refusal.IsNone)throw new InvalidOperationException("point graph erase changed after complete admission: "+refusal);
            if(_definition.NetworkEraseFault=="noop")return;
            if(_definition.NetworkEraseFault=="throw")throw new InvalidOperationException("proof.original-physical-erase-error");
            var writes=new List<(string Layer,Fix Point,Fix Value)>();
            foreach(PointPlacement point in path) {
                writes.Add((line.Layer.Id,point.Position,Fix.Zero));
                if(line.Owned)writes.Add((line.OwnerLayer.Id,point.Position,Fix.Zero));
            }
            if(_definition.NetworkEraseFault=="extra") {
                Fix extra=((PointPlacement)path[0]).Position-_definition.NetworkStep.Value;
                writes.Add((line.Layer.Id,extra,Fix.Zero));
                if(line.Owned)writes.Add((line.OwnerLayer.Id,extra,Fix.Zero));
            }
            refusal=WriteLayers(world,company,writes.AsReadOnly());
            if(!refusal.IsNone)throw new InvalidOperationException("point removal changed after complete write-set admission: "+refusal);
            if(_definition.NetworkEraseFault=="restore") {
                var restore=new List<(string Layer,Fix Point,Fix Value)>();
                foreach(PointPlacement point in path) {
                    restore.Add((line.Layer.Id,point.Position,line.Value));
                    if(line.Owned)restore.Add((line.OwnerLayer.Id,point.Position,Fix.FromInt(checked(company.Value+1))));
                }
                refusal=WriteLayers(world,company,restore.AsReadOnly());
                if(!refusal.IsNone)throw new InvalidOperationException("physical erase restoration probe refused: "+refusal);
            }
            GeometryObservationFault?.Invoke("erase", null);
        }
    }

    public sealed class PointLinePlacementPolicyDef : Def, ILinePlacementPolicy
    {
        private string _space;
        public ISpatialSpaceDefinition Space { get; private set; }
        protected override void ReadCore(JsonReader reader) { _space = reader.String("space"); }
        protected override void ResolveCore(ContentResolver content) => Space = content.Def<ISpatialSpaceDefinition>(_space);
        public void ValidateBinding(World world)
        { if (!(world.Space is PointSpace point) || point.Id != Space.Id) throw new ContentException("point corridor policy requires selected point authority"); }
        public Code Check(World world, PlayerId company, SpatialPlacement placement)
            => world.Space?.Id == Space.Id && placement is PointPlacement point && point.Space == Space.Id ? Code.None : LineCodes.WrongSpace;
    }
    public sealed class PointLineGeometryProofModule : GameModule
    {
        public override string Id => "proof.point-lines";
        public override ModuleVersion Version => new ModuleVersion(1,0,0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InfrastructureModule.ModuleId,Version),new ModuleRequirement("points",Version) };
        public readonly InitializeProbe Initialization = new InitializeProbe();
        public bool SharedOwner { get; private set; }
        public bool NativeOwnerAbsent { get; private set; }
        public readonly PointIntegerAdmissionProbe IntegerAdmission=new PointIntegerAdmissionProbe();
        public override void DeclareContent(ContentTypes types) => types.Section<PointLinePlacementPolicyDef>("point_line_placement_policies");
        public override void Compose(WorldComposer composer)
        {
            SharedOwner = composer.Services.Get<ILineConstruction>() is LineConstructionService &&
                ReferenceEquals(composer.Services.Get<ILineConstruction>(),composer.Services.Get<ILineWorkEffects>()) &&
                ReferenceEquals(composer.Services.Get<ILineConstruction>(),composer.Services.Get<ILineRemoval>()) &&
                ReferenceEquals(composer.Services.Get<ILineGeometry>(),composer.Services.Get<ILineRemovalGeometry>());
            NativeOwnerAbsent = composer.Services.Get<INetworkTopology>() is NetworkService &&
                !composer.Services.Has<ITileLineRouting>() && !composer.Services.Has<NavService>();
            composer.AddSystem(Initialization,430);
            if(composer.Content.Find<ISpatialLayerDefinition>("corridor.company")!=null)composer.AddSystem(IntegerAdmission,431);
        }
    }
}
