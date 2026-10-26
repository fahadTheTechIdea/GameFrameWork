using Beep.SimEngine.Maps;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{














    public static class StandardCodes
    {
        public static readonly Code NotOrderable = new Code("core.not-orderable");
        public static readonly Code WrongTarget = new Code("core.wrong-target");
        public static readonly Code NoStorage = new Code("core.no-storage");
        public static readonly Code NotAccepted = new Code("core.resource-not-accepted");
        public static readonly Code NotWorker = new Code("core.not-a-worker");
    }

    internal sealed class PostOrderHandler : CommandHandler<PostOrderCommand>
    {
        protected override Code Check(World world, PostOrderCommand c)
        {
            WorkBoard board = world.Services.Get<WorkBoard>(); string key = "activity:" + c.Plan;
            Code check = board.CheckPlayerActivity(c.Player, key, c.Target, c.Priority);
            if (!check.IsNone) return check;
            var plan = (IPlayerActivityDefinition)board.ResolvePlan(key);
            return PlanObservation.Activation(world, plan, c.Player, ActivityWorkerSelection.CreateArguments(c.Target, ActivityWorkerSelection.Unbound));
        }

        protected override void Apply(World world, PostOrderCommand c, CommandContext ctx)
        {
            var plan = (IPlayerActivityDefinition)world.Services.Get<WorkBoard>().ResolvePlan("activity:" + c.Plan);
            Order o = plan.Activate(world, c.Player, ActivityWorkerSelection.CreateArguments(c.Target, ActivityWorkerSelection.Unbound),
                c.Priority == 0 ? plan.Priority : c.Priority);
            ctx.Entity = c.Target;
            ctx.Arg0 = o.Id;
        }
    }

    internal sealed class CancelOrderHandler : CommandHandler<CancelOrderCommand>
    {
        protected override Code Check(World world, CancelOrderCommand c)
        {
            Order o = world.Services.Get<WorkBoard>().Order(c.Order);
            if (o == null || !o.IsActive) return Codes.NotFound;
            return o.Owner != c.Player ? Codes.NotOwned : Code.None;
        }

        protected override void Apply(World world, CancelOrderCommand c, CommandContext ctx)
        {
            Order o = world.Services.Get<WorkBoard>().Order(c.Order);
            world.System<WorkSystem>().Cancel(world, o);
            ctx.Entity = o.Target;
        }
    }

    internal sealed class SetOrderPriorityHandler : CommandHandler<SetOrderPriorityCommand>
    {
        protected override Code Check(World world, SetOrderPriorityCommand c)
        {
            Order o = world.Services.Get<WorkBoard>().Order(c.Order);
            if (o == null || !o.IsActive) return Codes.NotFound;
            if (o.Owner != c.Player) return Codes.NotOwned;
            return c.Priority < 0 ? Codes.InvalidArgument : Code.None;
        }

        protected override void Apply(World world, SetOrderPriorityCommand c, CommandContext ctx)
        {
            WorkBoard board = world.Services.Get<WorkBoard>();
            Order o = board.Order(c.Order);
            o.Priority = c.Priority;
            board.Dirty = true;
        }
    }

    internal sealed class AddRouteHandler : CommandHandler<AddRouteCommand>
    {
        protected override Code Check(World world, AddRouteCommand c)
            => world.Services.Get<ILogisticsRoutes>().CheckRoute(c.Player, c.From, c.To, c.Resource, c.Settings, c.SourcePort, c.DestinationPort);

        protected override void Apply(World world, AddRouteCommand c, CommandContext ctx)
            => ctx.Arg0 = world.Services.Get<ILogisticsRoutes>().AddRoute(c.Player, c.From, c.To, c.Resource, c.Settings, c.SourcePort, c.DestinationPort);
    }

    internal sealed class ConfigureRouteHandler : CommandHandler<ConfigureRouteCommand>
    {
        protected override Code Check(World world, ConfigureRouteCommand command)
            => world.Services.Get<ILogisticsRoutes>().CheckRouteSettings(command.Player, command.Route, command.Settings);
        protected override void Apply(World world, ConfigureRouteCommand command, CommandContext context)
        {
            Code refusal = world.Services.Get<ILogisticsRoutes>().ConfigureRoute(command.Player, command.Route, command.Settings);
            if (!refusal.IsNone) throw new System.InvalidOperationException("route edit changed after admission: " + refusal);
            context.Arg0 = command.Route;
        }
    }

    internal sealed class RemoveRouteHandler : CommandHandler<RemoveRouteCommand>
    {
        protected override Code Check(World world, RemoveRouteCommand c)
        {
            return world.Services.Get<ILogisticsRoutes>().CheckRemoveRoute(c.Player, c.Route);
        }

        protected override void Apply(World world, RemoveRouteCommand c, CommandContext ctx) => world.Services.Get<ILogisticsRoutes>().RemoveRoute(c.Route);
    }

    internal sealed class MoveVehicleHandler : CommandHandler<MoveVehicleCommand>
    {
        public static readonly Code NotMovable = new Code("core.not-movable");
        public static readonly Code Unreachable = new Code("core.unreachable");
        public static readonly Code SearchBudgetExceeded = new Code("core.path-search-budget-exceeded");

        protected override Code Check(World world, MoveVehicleCommand c)
        {
            Entity v = world.Entities.Get(c.Vehicle);
            if (v == null) return Codes.NotFound;
            if (v.Owner != c.Player) return Codes.NotOwned;
            if (!v.TryGet(out IMobility m)) return NotMovable;
            if (v.Get<IWorker>() != null)
            { Code refusal = world.Services.Get<IWorkerControl>().CheckHeld(world, c.Player, v.Id, true); if (!refusal.IsNone) return refusal; }
            if (world.Space is not ITileNavigationSpace map || !map.InBounds(c.To)) return Codes.OutOfBounds;
            PathSearchResult result = m.CanReach(world, new TileRect(c.To, 1, 1));
            return result == PathSearchResult.Found ? Code.None
                : result == PathSearchResult.BudgetExceeded ? SearchBudgetExceeded : Unreachable;
        }

        protected override void Apply(World world, MoveVehicleCommand c, CommandContext ctx)
        {
            Entity v = world.Entities.Get(c.Vehicle);
            if (v.Get<IWorker>() != null) world.Services.Get<IWorkerControl>().SetHeld(world, c.Player, v.Id, true);
            v.Get<IMobility>().GoTo(new TileRect(c.To, 1, 1));
            ctx.Entity = v.Id;
        }
    }

    internal sealed class SetHomeHandler : CommandHandler<SetHomeCommand>
    {
        protected override Code Check(World world, SetHomeCommand c)
            => WorkerHomeContracts.CheckHome(world, c.Player, c.Worker, c.Home);

        protected override void Apply(World world, SetHomeCommand c, CommandContext ctx)
        { WorkerHomeContracts.Configure(world, c.Player, c.Worker, c.Home); ctx.Entity = c.Worker; }
    }

    // ======================================================================== view feature








}
