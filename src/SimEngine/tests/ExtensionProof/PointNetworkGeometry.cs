using System;
using System.Collections.Generic;
using System.Globalization;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Original sparse spatial state only. Same framework topology/stock/flow owner as native worlds.
    public sealed partial class PointSpace : INetworkGraphGeometry
    {
        private static string VertexKey(Fix point)=>point.Raw.ToString("D20",CultureInfo.InvariantCulture);
        private bool TryVertex(string identity,out Fix point)
        {
            point=Fix.Zero;
            if(identity==null||!long.TryParse(identity,NumberStyles.None,CultureInfo.InvariantCulture,out long raw)||raw<0)return false;
            point=Fix.FromRaw(raw);return InLayerBounds(point)&&VertexKey(point)==identity;
        }
        public void ValidateNetwork(World world,INetworkDefinition network)
        {
            ValidateLine(world,network.Line);
            if(!_definition.NetworkStep.HasValue||!_definition.NetworkReach.HasValue)
                throw new ContentException(network.Id+": point graph requires explicit network_graph step/reach JSON");
        }
        public NetworkGeometryVersion ReadVersion(World world,ILineDefinition line)
        {
            if(!ReferenceEquals(world.Space,this)||line.Layer.Space.Id!=Id||Layer(line.Layer.Id)==null)
                throw new InvalidOperationException("point graph requires actual selected sparse layer");
            var version = new NetworkGeometryVersion(_observerVersion,line.Owned?_observerVersion:0);
            GeometryObservationFault?.Invoke("network.version", null);
            return version;
        }
        public void CopyVertices(World world,ILineDefinition line,IList<NetworkVertex> destination)
        {
            destination.Clear();var points=new List<Fix>();Layer(line.Layer.Id).CopyPositivePositions(points);
            foreach(var point in points)destination.Add(new NetworkVertex(VertexKey(point),ReadLineCompany(world,line,point)));
            if(points.Count>=2&&_definition.NetworkFault=="duplicate-vertex")destination.Add(destination[0]);
            if(points.Count>=2&&_definition.NetworkFault=="invalid-owner")destination[0]=new NetworkVertex(destination[0].Identity,new PlayerId(0));
            GeometryObservationFault?.Invoke("network.vertices", null);
        }
        public void CopyNeighbours(World world,ILineDefinition line,string identity,IList<string> destination)
        {
            destination.Clear();if(!TryVertex(identity,out var point)||!_definition.NetworkStep.HasValue)
                throw new InvalidOperationException("point graph requires actual identity and explicit adjacency");
            Fix step=_definition.NetworkStep.Value;
            if(point>=step)destination.Add(VertexKey(point-step));
            if(_definition.Extent-point>=step)destination.Add(VertexKey(point+step));
            var occupied=new List<Fix>();Layer(line.Layer.Id).CopyPositivePositions(occupied);
            if(occupied.Count<2||_definition.NetworkFault==null)return;
            if(_definition.NetworkFault=="backward-only") {
                for(int i=destination.Count-1;i>=0;i--)if(TryVertex(destination[i],out var next)&&next>point)destination.RemoveAt(i);
            }
            if(_definition.NetworkFault=="duplicate-neighbour")destination.Add(destination[0]);
            if(_definition.NetworkFault=="self")destination.Add(identity);
            if(_definition.NetworkFault=="invalid-neighbour")destination.Add("invalid.position");
            if(_definition.NetworkFault=="throw")throw new InvalidOperationException("proof.graph.callback.first");
            if(_definition.NetworkFault=="mutate-layer") {
                Code refusal=WriteLayer(world,PlayerId.None,line.Layer.Id,occupied[0],line.Value+Fix.One);
                if(!refusal.IsNone)throw new InvalidOperationException("proof graph mutation setup refused: "+refusal);
            }
        }
        public bool ValidateIdentity(World world,ILineDefinition line,string identity)
            =>ReferenceEquals(world.Space,this)&&line.Layer.Space.Id==Id&&TryVertex(identity,out _);
        public bool Touches(World world,ILineDefinition line,Entity anchor,EntityDef footprint,string identity)
            =>ValidateIdentity(world,line,identity)&&TryVertex(identity,out var point)&&anchor?.Get<PointPresence>() is PointPresence presence&&
                footprint?.Component<PointPresenceDef>()?.Space==Id&&
                presence.Space==Id&&_definition.NetworkReach.HasValue&&Fix.Abs(presence.Position-point)<=_definition.NetworkReach.Value;
        public string VertexIdentity(World world,ILineDefinition line,SpatialPlacement placement)
        {
            if(!ReferenceEquals(world.Space,this)||!(placement is PointPlacement point)||point.Space!=Id||!InLayerBounds(point.Position))
                throw new InvalidOperationException("point graph requires actual canonical placement");
            return VertexKey(point.Position);
        }
    }
}
