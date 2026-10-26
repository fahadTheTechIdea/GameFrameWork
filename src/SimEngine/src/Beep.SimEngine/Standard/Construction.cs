using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>
    /// "construction": { "site": "tower-site", "activity": "tower.build-work",
    ///                   "demolish": { "activity": "tower.removal-work", "salvage": { "steel": 10 } },
    ///                   "states": { "site": "building" } }
    ///
    /// How a structure is built and taken down. The plan's key is "build:&lt;entity id&gt;"; it runs on the
    /// explicitly authored site prototype named by "site".
    /// </summary>
    /// <summary>Native JSON registration of the shared construction declaration, not an extension base.</summary>
    public sealed class ConstructibleDef : ComponentDef<Constructible>, IConstructionDefinition, IPlanTemplateDefinition
    {
        private ConstructionDeclaration _declaration;
        public bool AutomaticSupply => _declaration.AutomaticSupply;
        public IReadOnlyList<IContentDefinition> DisclosureContent => _declaration.DisclosureContent;
        public IActivityDefinition Workflow => _declaration.Workflow;
        public EntityDef TargetDefinition => _declaration.TargetDefinition;
        public EntityDef SiteDefinition => _declaration.SiteDefinition;
        public IEntityStateDefinition SiteState => _declaration.SiteState;
        public IReadOnlyList<IActivityDefinition> ActivityDefinitions => _declaration.ActivityDefinitions;
        public string PlanKey => _declaration.PlanKey;
        public IActivityGraph Graph => _declaration.Graph;
        public string OnComplete => _declaration.OnComplete;
        public IRemovalPlanDefinition Demolition => _declaration.Demolition;
        public EntityStateBindings States => _declaration.States;
        public Bill SupplyCapacity => _declaration.SupplyCapacity;
        public Bill Salvage => _declaration.Salvage;
        protected override void ReadCore(JsonReader reader) => _declaration = ConstructionDeclaration.Read(reader, this);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
        public IActivityDefinition BindExecution(World world, EntityId target) => _declaration.BindExecution(world, target);
        public string IntentIdentity(World world, JsonValue arguments) => _declaration.IntentIdentity(world, arguments);
        public System.Collections.Generic.IReadOnlyList<EntityId> ReadEntityReferences(World world, JsonValue arguments) => _declaration.ReadEntityReferences(world, arguments);
        public Code CheckEntityReplacement(World world, PlayerId company, JsonValue arguments, Entity source, EntityDef destination) => _declaration.CheckEntityReplacement(world, company, arguments, source, destination);
        public JsonValue ReplaceEntityReference(World world, JsonValue arguments, EntityId source, EntityId destination) => _declaration.ReplaceEntityReference(world, arguments, source, destination);
        public string ExecutionIdentity(World world, Order order) => _declaration.ExecutionIdentity(world, order);
        public Code CheckSubmission(World world, PlayerId company, JsonValue arguments) => _declaration.CheckSubmission(world, company, arguments);
        public Code CheckActivation(World world, PlayerId company, JsonValue arguments) => _declaration.CheckActivation(world, company, arguments);
        public Order Activate(World world, PlayerId company, JsonValue arguments, int priority) => _declaration.Activate(world, company, arguments, priority);
        public void ValidateTemplate(World world, JsonValue arguments) => _declaration.ValidateTemplate(world, arguments);
        public void ValidateTemplateAnchor(World world, JsonValue arguments, JsonValue anchor) => _declaration.ValidateTemplateAnchor(world, arguments, anchor);
        public Code CaptureTemplate(World world, PlayerId company, JsonValue arguments, JsonValue origin, out JsonValue template)
            => _declaration.CaptureTemplate(world, company, arguments, origin, out template);
        public Code PlaceTemplate(World world, PlayerId company, JsonValue arguments, JsonValue anchor, out JsonValue intent)
            => _declaration.PlaceTemplate(world, company, arguments, anchor, out intent);
    }

    /// <summary>Marks a structure as one that is built by a construction order (no state).</summary>
    public sealed class Constructible : Component { }

    /// <summary>The site of a structure under construction: which def it becomes.</summary>
    public sealed class SiteDef : ComponentDef<Site>, IConstructionSiteDefinition
    {
        private ConstructionSiteDeclaration _declaration;
        public EntityDef Target => _declaration.Target;
        public IEntityStateDefinition ConstructionState => _declaration.ConstructionState;
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ReadCore(JsonReader reader) => _declaration = ConstructionSiteDeclaration.Read(reader);
    }

    public sealed class Site : Component, IConstructionSite, IInspectable, IEntityStateSource, IWorkCancellationTarget, IWorkOrderRetention
    {
        public new IConstructionSiteDefinition Def => (IConstructionSiteDefinition)base.Def;
        public bool RetainsOrder(Order order) => Entity.Alive && order != null && order.Target == Entity.Id && order.Id == Order;
        public void OnWorkCancelled(World world, Order order)
        { if (Order == order.Id) world.Services.Get<IConstructionProjects>().Abandon(world, Entity); }
        public EntityDef Target => Def.Target;
        public int Order { get; set; }
        public IEntityStateDefinition GetState(EntityUpdateContext context)
            => ReadMetadata(context.World, definition => definition.ConstructionState);

        public override void Sync(IStateSync s) { int order = Order; s.Int(ref order); Order = order; }

        public static readonly Code FacetProgress = new Code("facet.construction-progress");
        public static readonly Code FacetBlocker = new Code("facet.blocker");

        public void Inspect(World world, PlayerId viewer, IList<Facet> into)
        {
            Order o = world.Services.Get<WorkBoard>().Order(Order);
            if (o == null) return;
            string target = ReadMetadata(world, definition => definition.Target.Id);
            into.Add(new Facet(FacetProgress, Fix.FromInt(o.NodesDone), target, Fix.FromInt(o.Nodes.Count)));
            if (!o.Blocker.IsNone && (viewer == Entity.Owner || !viewer.IsValid)) into.Add(new Facet(FacetBlocker, Fix.Zero, o.Blocker.Id));
        }
        private bool _readingMetadata;
        private T ReadMetadata<T>(World world, Func<IConstructionSiteDefinition, T> read)
        {
            if (_readingMetadata)
            {
                var error = new InvalidOperationException("construction site metadata cannot recursively observe itself");
                world.LatchFault("construction.site-metadata", error); throw error;
            }
            _readingMetadata = true;
            try { return ConstructionBindingContracts.ReadSite(world, Def, Entity, read); }
            finally { _readingMetadata = false; }
        }
    }

    /// <summary>A structure finished (published by the construction effect).</summary>
    public struct StructureBuilt
    {
        public EntityId Entity;
        public EntityId Site;
        public PlayerId Owner;
    }

    /// <summary>A structure was taken down or a site cancelled.</summary>
    public struct StructureRemoved
    {
        public EntityId Entity;
        public PlayerId Owner;
        public string Def;
    }

    /// <summary>
    /// Construction rules shared by the commands, the effects and anyone previewing a placement. The game
    /// adds its own placement and permission rules (terrain, water, distance to base, technology).
    /// </summary>
    public sealed partial class ConstructionService : IResourceRecovery, IConstructionProjects, ICompositionValidator
    {
        public const string FinishEffect = "sim.construction.finish";
        public const string DemolishEffect = "sim.construction.demolish";
        private readonly World _world;
        public IResourceRecoveryDefinition Definition { get; }
        public ConstructionService(World world, IResourceRecoveryDefinition definition)
        { _world = world ?? throw new ArgumentNullException(nameof(world)); Definition = definition; }

        public static readonly Code NotConstructible = new Code("core.not-constructible");
        public static readonly Code NoteBuilt = new Code("note.structure-built");
        public static readonly Code NoteDemolished = new Code("note.structure-demolished");

        public Code CheckIntent(World world, PlayerId player, EntityDef def, JsonValue arguments)
        {
            if (!ReferenceEquals(world, _world)) return Codes.InvalidArgument;
            if (def == null) return Codes.UnknownDef;
            if (!ReferenceEquals(world.Content.Find<EntityDef>(def.Id), def)) return Codes.UnknownDef;
            if (!world.Players.IsSeat(player)) return Codes.NotSeated;
            if (!world.ViewFor(player).CanSeeContent(def)) return Codes.UnknownDef;
            IConstructionDefinition construction = def.Component<IConstructionDefinition>();
            if (construction == null) return NotConstructible;
            if (!(world.Space is ISpatialConstructionPlacement provider)) return Codes.NotAllowed;
            try
            {
                SpatialPlacement at = provider.ReadPlacement(arguments);
                if (at == null || at.Space != world.Space.Id) return Codes.InvalidArgument;
                Code bounds = provider.CheckPlacement(world, def, at, null);
                if (!bounds.IsNone && bounds != Codes.Blocked) return bounds;
            }
            catch (ContentException) { return Codes.InvalidArgument; }
            return provider.CheckConstructionDefinition(def, construction.SiteDefinition);
        }
        public Code CheckPlacement(World world, PlayerId player, EntityDef def, JsonValue arguments)
        {
            Code check = CheckIntent(world, player, def, arguments); if (!check.IsNone) return check;
            Code access = world.Services.Get<IContentAccess>().Check(world, player, def); if (!access.IsNone) return access;
            var provider = (ISpatialConstructionPlacement)world.Space; SpatialPlacement at = provider.ReadPlacement(arguments);
            check = provider.CheckPlacement(world, def, at, null); if (!check.IsNone) return check;
            check = provider.CheckPlacement(world, def.Component<IConstructionDefinition>().SiteDefinition, at, null);
            if (!check.IsNone) return check;
            foreach (IConstructionPlacementRule rule in world.Services.Providers<IConstructionPlacementRule>())
            { check = EntityObservation.ReadProvider(world, rule, null, () => rule.CheckPlacement(world, player, def, at), world.Space);
                if (!check.IsNone) return check; }
            return Code.None;
        }

        /// <summary>Places a site (free: materials and cash are taken as its steps start) and posts its order.</summary>
        public Entity PlaceSite(World world, PlayerId player, EntityDef def, JsonValue arguments, int priority)
        {
            Code check = priority < 0 ? Codes.InvalidArgument : CheckPlacement(world, player, def, arguments);
            if (!check.IsNone) throw new InvalidOperationException("construction admission changed: " + check);
            IConstructionDefinition construction = def.Component<IConstructionDefinition>();
            var provider = (ISpatialConstructionPlacement)world.Space; SpatialPlacement at = provider.ReadPlacement(arguments);
            Entity site = world.Spawn(construction.SiteDefinition, player, e => provider.ConfigurePlacement(e, at));
            Order o = world.Services.Get<WorkBoard>().Post(construction, player, site.Id, priority);
            site.Get<IConstructionSite>().Order = o.Id;
            return site;
        }

        public void ValidateComposition(World world)
        {
            ConstructionBindingContracts.ValidateComposition(world);
            ValidateRecoveryDefinition(world);
            if (!(world.Space is ISpatialConstructionPlacement provider)) throw new ContentException("construction requires a compatible placement provider");
            foreach (EntityDef target in world.Content.All<EntityDef>())
            {
                IConstructionDefinition construction = target.Component<IConstructionDefinition>(); if (construction == null) continue;
                EntityDef site = construction.SiteDefinition;
                if (site == null || !ReferenceEquals(world.Content.Find<EntityDef>(site.Id), site) ||
                    !ReferenceEquals(construction.TargetDefinition, target) || !site.HasCapability<IConstructionSite>() ||
                    !ReferenceEquals(site.Component<IConstructionSiteDefinition>()?.Target, target))
                    throw new ContentException(target.Id + ": construction requires its exact authored site/target binding");
                IInventoryDefinition inventory = site.Component<IInventoryDefinition>();
                if (inventory == null || !inventory.InitialStock.IsEmpty) throw new ContentException(site.Id + ": construction sites require empty authored inventory");
                if (target.Component<ISpatialPresenceDefinition>()?.Space == world.Space.Id &&
                    !EntityObservation.ReadProvider(world, provider, null, () => provider.CheckConstructionDefinition(target, site)).IsNone)
                    throw new ContentException(target.Id + ": incompatible site geometry");
                if (!CapacityPolicyContracts.Check(inventory.CapacityPolicy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), construction.SupplyCapacity.Lines).Fits)
                    throw new ContentException(site.Id + ": site capacity cannot hold its authored input envelope");
                foreach (ResourceAmount input in construction.SupplyCapacity.Lines)
                    if (!inventory.Accepts(world.Content, input.Resource) || CapacityPolicyContracts.Quantum(inventory.CapacityPolicy, input.Resource).Floor(input.Amount) != input.Amount)
                        throw new ContentException(site.Id + ": site filters or capacity quantum cannot receive its authored inputs");
            }
        }

        public Code Abandon(World world, Entity site)
        {
            if (site == null || !site.Alive || site.Get<IConstructionSite>() == null) return Codes.NotFound;
            Code refusal = world.Services.Get<IResourceRecovery>().Evacuate(world, site);
            if (!refusal.IsNone)
            {
                Order order = world.Services.Get<WorkBoard>().Order(site.Get<IConstructionSite>().Order);
                if (order != null && order.Blocker != refusal)
                { order.Blocker = refusal; site.Touch(); world.Notify(refusal, site.Owner, site.Id, site.Def.Id); }
                return refusal;
            }
            world.Despawn(site);
            return Code.None;
        }

        public Code Finish(World world, Order order)
        {
            Entity site = world.Entities.Get(order.Target);
            if (site == null) return Codes.NotFound;
            EntityDef target = site.Get<IConstructionSite>()?.Target;
            if (target == null || !(world.Space is ISpatialConstructionPlacement provider)) return Codes.InvalidArgument;
            SpatialPlacement at = provider.CapturePlacement(site);
            Code placement = provider.CheckPlacement(world, target, at, site); if (!placement.IsNone) return placement;
            Code refusal = world.Services.Get<IResourceRecovery>().Evacuate(world, site);
            if (!refusal.IsNone) return refusal;
            world.Despawn(site);
            Entity built = world.Spawn(target, order.Owner, e => provider.ConfigurePlacement(e, at));
            world.Events.Publish(new StructureBuilt { Entity = built.Id, Site = site.Id, Owner = order.Owner });
            world.Notify(NoteBuilt, order.Owner, built.Id, target.Id);
            return Code.None;
        }

        public Code PreviewDemolition(World world, PlayerId company, EntityId target, out PlanIntent intent)
        {
            intent = null; Entity entity = world.Entities.Get(target);
            if (entity == null) return Codes.NotFound;
            if (entity.Owner != company) return Codes.NotOwned;
            IRemovalPlanDefinition definition = entity.Def.Component<IConstructionDefinition>()?.Demolition;
            if (definition == null || definition.Graph.Nodes.Count == 0) return NotConstructible;
            intent = definition.CreateTargetIntent(target, ActivityWorkerSelection.Unbound, null, definition.Priority);
            Code check = world.Services.Get<IPlanBoard>().PreviewPlan(company, intent);
            return check.IsNone ? PlanObservation.Activation(world, definition, company, intent.Arguments) : check;
        }
        public Code CheckDemolition(World world, Entity entity)
        {
            if (entity == null || !ReferenceEquals(world.Entities.Get(entity.Id), entity)) return Codes.NotFound;
            IConstructionDefinition construction = entity.Def.Component<IConstructionDefinition>();
            if (construction?.Demolition == null) return NotConstructible;
            if (!(world.Space is ISpatialPlacement provider) || entity.Get<ISpatialPresence>()?.Space != world.Space.Id) return Codes.InvalidArgument;
            Code refusal = CheckPacking(world, provider.CapturePlacement(entity), construction.Salvage, null);
            return refusal.IsNone ? world.Services.Get<IResourceRecovery>().CheckEvacuation(world, entity) : refusal;
        }


        public Code Demolish(World world, Order order)
        {
            Entity entity = world.Entities.Get(order.Target);
            Code refusal = CheckDemolition(world, entity);
            if (!refusal.IsNone) return refusal;
            SpatialPlacement at = ((ISpatialPlacement)world.Space).CapturePlacement(entity);
            Bill yield = entity.Def.Component<IConstructionDefinition>().Salvage;
            refusal = world.Services.Get<IResourceRecovery>().Evacuate(world, entity);
            if (!refusal.IsNone) return refusal;
            refusal = ReleaseYield(world, order.Owner, at, yield, new ReservationOwner("sim.construction.salvage", order.Id), new Code(order.PlanKey));
            if (!refusal.IsNone) throw new InvalidOperationException("demolition yield changed after successful preflight");
            string definition = entity.Def.Id; world.Despawn(entity);
            world.Events.Publish(new StructureRemoved { Entity = entity.Id, Owner = order.Owner, Def = definition });
            world.Notify(NoteDemolished, order.Owner, entity.Id, definition);
            return Code.None;
        }

    }

    // ======================================================================== commands







    internal sealed class ConstructHandler : CommandHandler<ConstructCommand>
    {
        protected override Code Check(World world, ConstructCommand c)
            => c.Priority < 0 ? Codes.InvalidArgument : world.Services.Get<IConstructionProjects>().CheckPlacement(world, c.Player, world.Content.Find<EntityDef>(c.Def), c.Placement);

        protected override void Apply(World world, ConstructCommand c, CommandContext ctx)
            => ctx.Entity = world.Services.Get<IConstructionProjects>().PlaceSite(world, c.Player, world.Content.Get<EntityDef>(c.Def), c.Placement, c.Priority).Id;
    }

    internal sealed class CancelConstructionHandler : CommandHandler<CancelConstructionCommand>
    {
        protected override Code Check(World world, CancelConstructionCommand c)
        {
            Entity site = world.Entities.Get(c.Site);
            if (site == null || site.Get<IConstructionSite>() == null) return Codes.NotFound;
            return site.Owner != c.Player ? Codes.NotOwned : Code.None;
        }

        protected override void Apply(World world, CancelConstructionCommand c, CommandContext ctx)
        {
            Entity site = world.Entities.Get(c.Site);
            WorkBoard board = world.Services.Get<WorkBoard>();
            Order o = board.Order(site.Get<IConstructionSite>().Order);
            if (o != null && o.IsActive) world.System<WorkSystem>().Cancel(world, o); // ending the order abandons the site
            else world.Services.Get<IConstructionProjects>().Abandon(world, site);
            ctx.Entity = site.Id;
        }
    }

    internal sealed class DemolishHandler : CommandHandler<DemolishCommand>
    {
        protected override Code Check(World world, DemolishCommand c)
            => world.Services.Get<IConstructionProjects>().PreviewDemolition(world, c.Player, c.Target, out _);

        protected override void Apply(World world, DemolishCommand c, CommandContext ctx)
        {
            Code check = world.Services.Get<IConstructionProjects>().PreviewDemolition(world, c.Player, c.Target, out PlanIntent intent);
            if (!check.IsNone) throw new InvalidOperationException("removal admission changed: " + check);
            ctx.Arg0 = world.Services.Get<IPlanBoard>().SubmitPlan(c.Player, intent);
            ctx.Entity = c.Target;
        }
    }
}
