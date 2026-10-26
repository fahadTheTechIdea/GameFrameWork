using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public readonly struct NetworkVertex
    {
        public string Identity { get; }
        public PlayerId Company { get; }
        public NetworkVertex(string identity, PlayerId company)
        {
            if (string.IsNullOrWhiteSpace(identity)) throw new ArgumentException("actual network vertex identity required",nameof(identity));
            Identity=identity;Company=company;
        }
    }
    public readonly struct NetworkGeometryVersion : IEquatable<NetworkGeometryVersion>
    {
        public int Geometry { get; }
        public int Ownership { get; }
        public NetworkGeometryVersion(int geometry,int ownership)
        {
            if(geometry<0||ownership<0)throw new ArgumentOutOfRangeException(nameof(geometry));
            Geometry=geometry;Ownership=ownership;
        }
        public bool Equals(NetworkGeometryVersion other)=>Geometry==other.Geometry&&Ownership==other.Ownership;
        public override bool Equals(object other)=>other is NetworkGeometryVersion value&&Equals(value);
        public override int GetHashCode()=>unchecked(Geometry*397^Ownership);
    }
    /// <summary>Actual spatial graph only; original topology owner owns pools, promises and flow.</summary>
    public interface INetworkGraphGeometry
    {
        string Space { get; }
        void ValidateNetwork(World world,INetworkDefinition network);
        NetworkGeometryVersion ReadVersion(World world,ILineDefinition line);
        void CopyVertices(World world,ILineDefinition line,IList<NetworkVertex> destination);
        // Potential neighbours independent of occupancy, allowing dirty settled checkpoints to validate.
        // Every occupied edge must be reciprocal, including edges into already visited vertices.
        void CopyNeighbours(World world,ILineDefinition line,string identity,IList<string> destination);
        bool ValidateIdentity(World world,ILineDefinition line,string identity);
        /// <summary>Contact of the given definition at the actual anchor's retained placement; does not create or move an entity.</summary>
        bool Touches(World world,ILineDefinition line,Entity anchor,EntityDef footprint,string identity);
        string VertexIdentity(World world,ILineDefinition line,SpatialPlacement placement);
        Code CheckErase(World world,PlayerId company,ILineDefinition line,IReadOnlyList<SpatialPlacement> path);
        void Erase(World world,PlayerId company,ILineDefinition line,IReadOnlyList<SpatialPlacement> path);
    }
}
