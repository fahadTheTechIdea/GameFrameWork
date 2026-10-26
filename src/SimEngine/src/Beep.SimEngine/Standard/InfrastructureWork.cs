using System.Collections.Generic;
using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    /// <summary>Authored capability for a physical corridor's one worksite cursor.</summary>
    public interface ILineWorkDefinition : IComponentDefinition
    {
        void ValidateBinding(EntityDef owner);
    }

    /// <summary>Single line/path/order binding; earned progress remains in the original graph.</summary>
    public interface ILineWork : IEntityCapability, IWorkOrderRetention
    {
        string Line { get; }
        JsonValue Path { get; }
        int Order { get; }
        void BindPath(World world, ILineDefinition line, IReadOnlyList<SpatialPlacement> path);
        void BindOrder(World world, Order order);
        ILineExecutionPlan ExecutionPlan(World world);
    }

    /// <summary>Original segment graph compiler, not a second progress or execution owner.</summary>
    public interface ILineExecutionPlan : IActivityDefinition, IActivityWorkflowBinding
    {
        int CompletedSegments(Order order);
        void ValidateOrder(Order order);
    }

    public static class LineWorkContracts
    {
        public static void ValidateBinding(EntityDef owner)
        {
            if (owner == null || !owner.HasCapability<ISpatialPresence>() || !owner.HasCapability<IInventory>() ||
                owner.Component<ISpatialPresenceDefinition>() == null || owner.Component<IInventoryDefinition>() == null)
                throw new ContentException("line worksite requires actual spatial presence and inventory contracts");
        }

        public static ILineExecutionPlan ReadPlan(World world, ILineWork work)
        {
            if (world == null || work?.Entity == null || !work.Entity.Alive ||
                !ReferenceEquals(world.Entities.Get(work.Entity.Id), work.Entity) ||
                !ReferenceEquals(work.Entity.Get<ILineWork>(), work))
                throw new CorruptStateException("line cursor requires its actual live capability binding");
            return EntityObservation.ReadProvider(world, work, work.Entity, () => {
                ILineExecutionPlan plan = work.ExecutionPlan(world);
                if (plan == null) throw new CorruptStateException("line cursor has no actual execution plan");
                return plan;
            });
        }
    }

    /// <summary>Session-owned physical corridor admission and execution binding.</summary>
    public interface ILineConstruction
    {
        Code CheckIntent(PlayerId player, ILineDefinition line, IReadOnlyList<SpatialPlacement> path);
        Code Check(PlayerId player, ILineDefinition line, IReadOnlyList<SpatialPlacement> path);
        Order Begin(PlayerId player, ILineDefinition line, IReadOnlyList<SpatialPlacement> path, int priority);
        IActivityDefinition BindExecution(ILineDefinition line, EntityId target);
        string ExecutionIdentity(ILineDefinition line, Order order);
        void Advance();
        void ValidateMarkers();
    }

    /// <summary>Selected provider composition; no implicit tile routing or substitute spatial authority.</summary>
    public interface ILineGeometryComposition
    {
        void ComposeLineGeometry(WorldComposer composer);
    }

    /// <summary>Spatial layer/cursor rules only. Work, supplies, orders and progress stay in the shared owner.</summary>
    public interface ILineGeometry
    {
        string Space { get; }
        void ValidateLine(World world, ILineDefinition line);
        Code CheckPath(World world, ILineDefinition line, IReadOnlyList<SpatialPlacement> path);
        Code CheckSegment(World world, PlayerId company, ILineDefinition line, SpatialPlacement placement, out bool alreadyLaid);
        bool IsCursorAt(World world, Entity cursor, SpatialPlacement placement);
        void MoveCursor(World world, Entity cursor, SpatialPlacement placement);
        Code Stamp(World world, PlayerId company, ILineDefinition line, SpatialPlacement placement);
    }

    /// <summary>Optional removal geometry and disposition, never another work/progress owner.</summary>
    public interface ILineRemovalGeometry : ILineGeometry
    {
        Code ReadRemovalPath(World world, ILineDefinition line, Entity target, out IReadOnlyList<SpatialPlacement> path);
        Code CheckRemoval(World world, PlayerId company, ILineDefinition line, IReadOnlyList<SpatialPlacement> path);
        Code Remove(World world, PlayerId company, ILineDefinition line, IReadOnlyList<SpatialPlacement> path);
    }

    /// <summary>Shared actual subject admission and earned removal; same corridor execution owner.</summary>
    public interface ILineRemoval
    {
        Code CheckIntent(PlayerId company, ILineDefinition line, Entity target);
        Code Check(PlayerId company, ILineDefinition line, Entity target);
        Code Remove(PlayerId company, ILineDefinition line, Entity target);
    }

    /// <summary>Optional native tile route search; not required by shared corridor authoring or cursors.</summary>
    public interface ITileLineRouting
    {
        PathSearchResult PlanPath(PlayerId player, ILineDefinition line, TileCoord from, TileCoord to,
            List<TileCoord> path, int maxExpanded = NavGrid.DefaultExpansionBudget);
    }

    /// <summary>Earned cursor effects; original work/stock/topology owners apply them.</summary>
    public interface ILineWorkEffects
    {
        Code CheckSegment(Entity marker, Order order, IActivityNodeDefinition step);
        Code CompleteSegment(Entity marker, Order order, IActivityNodeDefinition step);
        Code CheckFinish(Entity marker, Order order, IActivityNodeDefinition step);
        Code Finish(Order order);
    }

    /// <summary>Existing infrastructure lifecycle, invoked by the sole game update pipeline.</summary>
    public interface ILineSimulation
    {
        void RebuildDirty();
        void Flow();
    }
}
