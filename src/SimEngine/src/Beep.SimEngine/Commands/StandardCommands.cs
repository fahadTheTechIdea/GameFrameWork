using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Commands
{
    /// <summary>Place a structure: a site appears (free), and its construction order starts.</summary>
    public sealed class ConstructCommand : Command
    {
        public string Def;
        public JsonValue Placement;
        public int Priority;
        public override void Sync(IStateSync s)
        {
            s.String(ref Def); string args = null;
            if (!s.Reading && Placement != null) { var text = new System.Text.StringBuilder(); Placement.WriteCanonical(text); args = text.ToString(); }
            s.String(ref args); s.Int(ref Priority);
            if (s.Reading) { Placement = args == null ? null : Json.Parse(args); Placement?.Freeze(); }
        }
    }

    /// <summary>Stop a site's order; recover delivered stock and remove it when active claims permit.</summary>
    public sealed class CancelConstructionCommand : Command
    {
        public EntityId Site;
        public override void Sync(IStateSync s) => s.Entity(ref Site);
    }

    /// <summary>Order a structure demolished: a machine takes it down and leaves salvage.</summary>
    public sealed class DemolishCommand : Command
    {
        public EntityId Target;
        public override void Sync(IStateSync s) => s.Entity(ref Target);
    }

    /// <summary>Queue a transformation at a transformer (Count copies, or one repeating entry).</summary>
    public sealed class EnqueueTransformationCommand : Command
    {
        public EntityId Transformer;
        public Code Capability;
        public string Transformation;
        public int Count = 1;
        public bool Repeat;
        public EntityId Home;

        public override void Sync(IStateSync s)
        {
            s.Entity(ref Transformer); string capability = Capability.Id; s.String(ref capability);
            if (s.Reading) Capability = capability == null ? Code.None : new Code(capability);
            s.String(ref Transformation); s.Int(ref Count); s.Bool(ref Repeat); s.Entity(ref Home);
        }
    }

    /// <summary>Remove a queue entry (the head entry's consumed inputs are not returned).</summary>
    public sealed class CancelTransformationCommand : Command
    {
        public EntityId Transformer;
        public Code Capability;
        public int Index;
        public override void Sync(IStateSync s)
        {
            s.Entity(ref Transformer); string capability = Capability.Id; s.String(ref capability);
            if (s.Reading) Capability = capability == null ? Code.None : new Code(capability);
            s.Int(ref Index);
        }
    }

    /// <summary>Lay a line (road, pipe, power line) along a contiguous path; a machine lays it tile by tile.</summary>
    public sealed class LayLineCommand : Command
    {
        public string Line;
        public List<TileCoord> Tiles = new List<TileCoord>();

        public override void Sync(IStateSync s)
        {
            s.String(ref Line);
            int n = s.Count(Tiles.Count);
            if (s.Reading) Tiles.Clear();
            for (int i = 0; i < n; i++) { TileCoord t = s.Reading ? default : Tiles[i]; s.Tile(ref t); if (s.Reading) Tiles.Add(t); }
        }
    }

    /// <summary>Place an order (from the "activities" content marked player_orderable) on one of your entities.</summary>
    public sealed class PostOrderCommand : Command
    {
        public string Plan;
        public EntityId Target;
        /// <summary>0: the order's own priority.</summary>
        public int Priority;
        public override void Sync(IStateSync s) { s.String(ref Plan); s.Entity(ref Target); s.Int(ref Priority); }
    }

    public sealed class CancelOrderCommand : Command
    {
        public int Order;
        public override void Sync(IStateSync s) => s.Int(ref Order);
    }

    public sealed class SetOrderPriorityCommand : Command
    {
        public int Order;
        public int Priority;
        public override void Sync(IStateSync s) { s.Int(ref Order); s.Int(ref Priority); }
    }

    /// <summary>Standing physical transfer intent with explicit native batch and dispatch policy.</summary>
    public sealed class AddRouteCommand : Command
    {
        public EntityId From;
        public EntityId To;
        public ResourceId Resource;
        public Code SourcePort;
        public Code DestinationPort;
        public Beep.SimEngine.Standard.LogisticsRouteSettings Settings;
        public override void Sync(IStateSync s)
        { s.Entity(ref From); s.Entity(ref To);
            string sourceName = SourcePort.Id, destinationName = DestinationPort.Id; s.String(ref sourceName); s.String(ref destinationName);
            if (s.Reading) { SourcePort = sourceName == null ? Code.None : new Code(sourceName); DestinationPort = destinationName == null ? Code.None : new Code(destinationName); }
            s.Resource(ref Resource); bool present = Settings != null; s.Bool(ref present);
            Settings = present ? Beep.SimEngine.Standard.LogisticsRouteSettings.Sync(s, Settings) : null; }
    }

    public sealed class ConfigureRouteCommand : Command
    {
        public int Route;
        public Beep.SimEngine.Standard.LogisticsRouteSettings Settings;
        public override void Sync(IStateSync s)
        { s.Int(ref Route); bool present = Settings != null; s.Bool(ref present);
            Settings = present ? Beep.SimEngine.Standard.LogisticsRouteSettings.Sync(s, Settings) : null; }
    }

    public sealed class RemoveRouteCommand : Command
    {
        public int Route;
        public override void Sync(IStateSync s) => s.Int(ref Route);
    }

    /// <summary>Base a machine at a depot: it returns there when idle.</summary>
    public sealed class SetHomeCommand : Command
    {
        public EntityId Worker;
        public EntityId Home;
        public override void Sync(IStateSync s) { s.Entity(ref Worker); s.Entity(ref Home); }
    }
}

namespace Beep.SimEngine.Commands
{
    /// <summary>Drive a vehicle to a tile. It leaves any job it had (another machine takes it) and waits there under your orders.</summary>
    public sealed class MoveVehicleCommand : Command
    {
        public EntityId Vehicle;
        public TileCoord To;
        public override void Sync(IStateSync s) { s.Entity(ref Vehicle); s.Tile(ref To); }
    }

}
