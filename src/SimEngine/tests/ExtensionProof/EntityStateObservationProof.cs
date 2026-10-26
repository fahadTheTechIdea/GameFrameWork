using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    public enum EntityStateProbeFault { Exception, HiddenState, PauseClock, SpeedClock, Touch, AllocateIdentity, RecursiveEntity, SwallowEntity, RecursiveGame, SwallowGame }
    // Reuses the original boundary provider and codec. This module adds diagnostic instrumentation only.
    public sealed class EntityStateObservationModule : GameModule
    {
        public override string Id => "proof.entity-state-observation";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types) => types.Component<EntityBoundaryProbeDef>("entity_probe", 2);
        public override void Compose(WorldComposer composer)
            => composer.AddViewFeature(Id, 1, (world, viewer) => new EntityStateProbeDiagnostics(world, viewer));
    }
    public sealed class EntityStateProbeDiagnostics
    {
        private readonly World _world; private readonly PlayerId _viewer;
        internal EntityStateProbeDiagnostics(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        private Entity Owned(EntityId target)
        {
            var entity = _world.Entities.Get(target);
            if (entity == null || _viewer != PlayerId.None && entity.Owner != _viewer)
                throw new ArgumentException("diagnostic probe requires actual owned entity");
            return entity;
        }
        private EntityBoundaryProbe Probe(EntityId target, Code source)
            => Owned(target).Get<EntityBoundaryProbe>(source) ?? throw new ArgumentException("actual named state probe required");
        public int Reads(EntityId target, Code source) => Probe(target, source).Reads;
        public EntityUpdateContext LastContext(EntityId target, Code source) => Probe(target, source).LastContext;
        public string Phase(EntityId target, Code source)
        {
            var entity = Owned(target);
            var part = (ComponentDef)entity.Def.Component<INamedCapabilityDefinition>(source);
            return "entity.state:" + entity.Id.Value + ":" + part.Key + "[" + part.Slot + "]";
        }
        public void ProbeUpdate(EntityId target, EntityUpdateContext context) => Owned(target).Update(context);
        public void ArmForeign(EntityId target, Code source, EntityStateProbeDiagnostics foreign)
            => Probe(target, source).Alternate = foreign._world.Content.Get<IEntityStateDefinition>("state.alpha");
        public void Arm(EntityId target, Code source, EntityStateProbeFault fault)
        {
            var probe = Probe(target, source);
            probe.OnRead = context =>
            {
                if (fault == EntityStateProbeFault.Exception) throw new InvalidOperationException("proof.entity.state.observation");
                if (fault == EntityStateProbeFault.HiddenState) probe.MutateHiddenProbe();
                if (fault == EntityStateProbeFault.PauseClock) ((GameClock)context.Clock).Pause();
                if (fault == EntityStateProbeFault.SpeedClock) ((GameClock)context.Clock).Speed = 2;
                if (fault == EntityStateProbeFault.Touch) probe.Entity.Touch();
                if (fault == EntityStateProbeFault.AllocateIdentity) context.World.Spawn(context.World.Content.Get<EntityDef>("source"), probe.Entity.Owner);
                if (fault == EntityStateProbeFault.RecursiveEntity) probe.Entity.Update(context);
                if (fault == EntityStateProbeFault.SwallowEntity) { try { probe.Entity.Update(context); } catch (InvalidOperationException) { } }
                if (fault == EntityStateProbeFault.RecursiveGame) context.World.Step();
                if (fault == EntityStateProbeFault.SwallowGame) { try { context.World.Step(); } catch (InvalidOperationException) { } }
            };
        }
    }
}
