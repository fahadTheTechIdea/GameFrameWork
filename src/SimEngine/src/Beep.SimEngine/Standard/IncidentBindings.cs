using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable binding for the reusable episode owner, not a custom incident's state model.</summary>
    public interface IIncidentEpisodeDefinition : IPhasedIncidentDefinition
    {
        EntityStateBindings States { get; }
        void ValidateBinding(World world);
    }

    public interface IIncidentSignalDefinition : IAutomationSignalDefinition
    {
        Code Source { get; }
        IIncidentWorkflowDefinition Workflow { get; }
        IIncidentPhaseDefinition Phase { get; }
        void ValidateBinding(World world);
    }

    /// <summary>One strict reader/resolver for episode metadata; contains no live episode facts.</summary>
    public sealed class IncidentEpisodeDeclaration
    {
        private readonly string _workflow;
        public Code CapabilityId { get; }
        public IIncidentWorkflowDefinition Workflow { get; private set; }
        public EntityStateBindings States { get; private set; }
        private IncidentEpisodeDeclaration(Code id, string workflow, EntityStateBindings states)
        { CapabilityId = id; _workflow = workflow; States = states; }
        public static IncidentEpisodeDeclaration Read(JsonReader reader)
        {
            string id = reader.String("id"), workflow = reader.String("workflow");
            try
            {
                ContractIdentity.ValidateId(id, "incident capability");
                ContractIdentity.ValidateId(workflow, "incident workflow");
            }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            return new IncidentEpisodeDeclaration(new Code(id), workflow, EntityStateBindings.Read(reader, "active"));
        }
        public void Resolve(ContentResolver content)
        {
            if (Workflow != null) throw new InvalidOperationException("incident declaration already resolved");
            Workflow = content.Def<IIncidentWorkflowDefinition>(_workflow); States = States.Resolve(content);
        }
    }

    /// <summary>One strict named-source and phase declaration, shared by native and independent JSON.</summary>
    public sealed class IncidentSignalDeclaration
    {
        private readonly string _workflow, _phase;
        public Code CapabilityId { get; }
        public Code Source { get; }
        public IIncidentWorkflowDefinition Workflow { get; private set; }
        public IIncidentPhaseDefinition Phase { get; private set; }
        public static IReadOnlyList<AutomationMetricSchema> Metrics { get; } = Array.AsReadOnly(new[]
        {
            new AutomationMetricSchema(new Code("incident.active"), AutomationSignalKind.Boolean, new Code("unit.boolean")),
            new AutomationMetricSchema(new Code("incident.phase-hold"), AutomationSignalKind.Boolean, new Code("unit.boolean"))
        });
        private IncidentSignalDeclaration(Code id, Code source, string workflow, string phase)
        { CapabilityId = id; Source = source; _workflow = workflow; _phase = phase; }
        public static IncidentSignalDeclaration Read(JsonReader reader)
        {
            string id = reader.String("id"), source = reader.String("source"), workflow = reader.String("workflow"), phase = reader.String("phase");
            try
            { foreach (string value in new[] { id, source, workflow, phase }) ContractIdentity.ValidateId(value, "incident signal binding"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            return new IncidentSignalDeclaration(new Code(id), new Code(source), workflow, phase);
        }
        public void Resolve(ContentResolver content)
        {
            if (Workflow != null) throw new InvalidOperationException("incident signal declaration already resolved");
            Workflow = content.Def<IIncidentWorkflowDefinition>(_workflow);
            Phase = IncidentPhases.Find(Workflow, _phase) ?? throw content.Fail("incident signal names an absent authored phase '" + _phase + "'");
        }
        public void ValidateOwner(EntityDef owner)
        {
            var source = owner.Component<IPhasedIncidentDefinition>(Source);
            if (!(source is ComponentDef part) || !typeof(IPhasedIncidentResponse).IsAssignableFrom(part.ComponentType) ||
                !ReferenceEquals(source.Workflow, Workflow))
                throw new ContentException(owner.Id + ": incident signal requires its exact named phased source/workflow");
        }
    }

    /// <summary>Actual immutable aliases only. Existing entity/provider observations own purity checks.</summary>
    public static class IncidentBindingContracts
    {
        private static T Observe<T>(World world, object provider, Entity entity, Func<T> read)
            => EntityObservation.ReadProvider(world, provider, entity, () => {
                if (entity == null) return read();
                var scope = EntityObservation.CaptureProvider(world, entity, entity.Sync, "incident.metadata");
                try { return read(); } finally { scope.Verify(); }
            });
        internal static ComponentDef Actual(World world, INamedCapabilityDefinition definition, Type runtime, Entity entity)
        {
            if (!(definition is ComponentDef part) || !part.IsFrozen || !world.Content.Owns(part.Owner) ||
                !runtime.IsAssignableFrom(part.ComponentType) || part.Owner.NamedSlot(definition.CapabilityId) != part.Slot ||
                !ReferenceEquals(part.Owner.Components[part.Slot], part) || entity != null &&
                (!ReferenceEquals(entity.Def, part.Owner) || entity.Components[part.Slot].GetType() != part.ComponentType))
                throw new ContentException("incident binding must alias its actual immutable owner and reusable runtime family");
            return part;
        }
        internal static void Workflow(World world, IIncidentWorkflowDefinition workflow)
        {
            if (!(workflow is Def actual) || !actual.IsFrozen || !world.Content.Owns(workflow) ||
                !ReferenceEquals(world.Content.Find<IIncidentWorkflowDefinition>(workflow.Id), workflow))
                throw new ContentException("incident binding requires its actual immutable workflow alias");
        }
        public static void ValidateEpisode(World world, IIncidentEpisodeDefinition definition)
            => ReadEpisode(world, definition, null);
        internal static IIncidentEpisodeDefinition ReadEpisode(World world, IIncidentEpisodeDefinition definition, Entity entity)
            => Observe(world, definition, entity, () => {
                var part = Actual(world, definition, typeof(Incident), entity); Workflow(world, definition.Workflow);
                if (part.StateCodec.SchemaVersion != 2) throw new ContentException("incident binding requires original episode codec2");
                var states = definition.States;
                if (states == null || states.Definitions.Count != 1 || !states.Definitions.TryGetValue("active", out var active) ||
                    !world.Content.Owns(active) || !ReferenceEquals(world.Content.Find<IEntityStateDefinition>(active.Id), active) ||
                    !definition.Supports(definition.Workflow.Kind) || definition.Supports(Code.None))
                    throw new ContentException("incident binding requires explicit actual active state and workflow kind");
                return definition;
            });
        public static void ValidateSignal(World world, IIncidentSignalDefinition definition)
            => ReadSignal(world, definition, null);
        internal static IIncidentSignalDefinition ReadSignal(World world, IIncidentSignalDefinition definition, Entity entity)
            => Observe(world, definition, entity, () => {
                var part = AutomationSignalContracts.ValidateReusable(world, definition, typeof(IncidentSignal), 1, 1,
                    IncidentSignalDeclaration.Metrics, entity); Workflow(world, definition.Workflow);
                if (definition.Phase == null || !ReferenceEquals(IncidentPhases.Find(definition.Workflow, definition.Phase.Id), definition.Phase))
                    throw new ContentException("incident signal requires its actual registered phase alias");
                var source = part.Owner.Component<IPhasedIncidentDefinition>(definition.Source);
                if (!(source is ComponentDef sourcePart) || !typeof(IPhasedIncidentResponse).IsAssignableFrom(sourcePart.ComponentType) ||
                    !ReferenceEquals(source.Workflow, definition.Workflow))
                    throw new ContentException(part.Owner.Id + ": incident signal requires its exact named phased source/workflow");
                return definition;
            });
        internal static void ValidateComposition(World world)
        {
            foreach (var owner in world.Content.All<EntityDef>())
                foreach (var part in owner.Components)
                {
                    if (typeof(Incident).IsAssignableFrom(part.ComponentType) && !(part is IIncidentEpisodeDefinition) ||
                        part.ComponentType == typeof(IncidentSignal) && !(part is IIncidentSignalDefinition))
                        throw new ContentException(owner.Id + ": reusable incident runtime requires matching definition contract");
                    if (part is IIncidentEpisodeDefinition episode)
                        Observe(world, part, null, () => { ValidateEpisode(world, episode); episode.ValidateBinding(world); ValidateEpisode(world, episode); return true; });
                    if (part is IIncidentSignalDefinition signal)
                        Observe(world, part, null, () => { ValidateSignal(world, signal); signal.ValidateBinding(world); ValidateSignal(world, signal); return true; });
                }
        }
    }
}
