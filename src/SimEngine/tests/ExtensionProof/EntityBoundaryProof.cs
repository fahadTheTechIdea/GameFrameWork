using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    // Public-contract lifecycle fault instrumentation, not another gameplay/state scheduler.
    public sealed class EntityBoundaryProbeDef : ComponentDef<EntityBoundaryProbe>, INamedCapabilityDefinition
    {
        private string _state;
        public Code CapabilityId { get; private set; }
        public string Mode { get; private set; }
        public bool Active { get; private set; }
        public IEntityStateDefinition State { get; private set; }
        protected override void ReadCore(JsonReader reader)
        { CapabilityId = new Code(reader.String("id")); Mode = reader.String("mode"); Active = reader.Bool("active"); _state = reader.String("state"); }
        protected override void ResolveCore(ContentResolver content) => State = content.Def<IEntityStateDefinition>(_state);
    }
    public sealed class EntityBoundaryProbe : Component<EntityBoundaryProbeDef>, INamedEntityCapability, IEntityStateSource
    {
        public Code CapabilityId => Def.CapabilityId;
        private bool _active;
        public override void OnCreate() => _active = Def.Active;
        internal void MutateHiddenProbe() => _active = !_active;
        public override void Sync(IStateSync sync) => sync.Bool(ref _active);
        // Instrumentation is intentionally not authoritative state and is excluded from Sync.
        public int Reads;
        public EntityUpdateContext LastContext;
        public IEntityStateDefinition Alternate;
        public Action<EntityUpdateContext> OnRead;
        public IEntityStateDefinition GetState(EntityUpdateContext context)
        {
            Reads++; LastContext = context; OnRead?.Invoke(context);
            switch (Def.Mode)
            {
                case "throw": throw new InvalidOperationException("entity source probe");
                case "recursive": Entity.Update(context); break;
                case "swallow-recursive":
                    try { Entity.Update(context); } catch (InvalidOperationException) { }
                    break;
                case "swallow-step":
                    try { context.World.Step(); } catch (InvalidOperationException) { }
                    break;
                case "swallow-initialize":
                case "wrap-initialize":
                    try { context.World.Spawn(context.World.Content.Get<EntityDef>("broken"), Entity.Owner); }
                    catch (InvalidOperationException error)
                    { if (Def.Mode == "wrap-initialize") throw new InvalidOperationException("state source wrapper", error); }
                    break;
                case "cross-entity":
                    foreach (Entity other in context.World.Entities.All)
                        if (other != Entity) { other.Update(context); break; }
                    break;
                case "repeat-prior":
                    foreach (Entity other in context.World.Entities.All)
                        if (other.Id.Value < Entity.Id.Value)
                        {
                            var probe = other.Get<EntityBoundaryProbe>(new Code("a"));
                            if (probe == null) continue;
                            other.Update(probe.LastContext); break;
                        }
                    break;
                case "inactive": return null;
                case "healthy": break;
                default: throw new InvalidOperationException("unknown explicit entity probe mode");
            }
            return _active ? Alternate ?? Def.State : null;
        }
    }
    public sealed class EntityBoundaryBrokenDef : ComponentDef<EntityBoundaryBroken>
    { protected override void ReadCore(JsonReader reader) { } }
    public sealed class EntityBoundaryBroken : Component<EntityBoundaryBrokenDef>, IEntityInitialization
    { public void Initialize(World world) => throw new InvalidOperationException("entity boundary initialization probe"); }
    public sealed class EntityBoundarySystem : GameSystem
    {
        public override string Id => "probe.entity-boundaries";
        public override int StateSchemaVersion => 1;
        public int Updates, Completions;
        public override void OnNewGame(World world)
        {
            world.Spawn(world.Content.Get<EntityDef>("actor"), new PlayerId(0));
            world.Spawn(world.Content.Get<EntityDef>("actor"), new PlayerId(1));
            world.Spawn(world.Content.Get<EntityDef>("later"), new PlayerId(0));
        }
        public override void Update(World world, TickInfo update) => Updates++;
        public override void CompleteUpdate(World world) => Completions++;
    }
}
