using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public static class WorkResourceCodes
    {
        public static readonly Code MissingSource = new Code("work.resource.missing-source");
        public static readonly Code Stock = new Code("work.resource.stock");
        public static readonly Code Incompatible = new Code("work.resource.incompatible");
        public static readonly Code Lost = new Code("work.resource.lost-claim");
    }
    /// <summary>Common strict native authoring, not another execution or stock owner.</summary>
    public sealed class ResourceRequirementDeclaration
    {
        private BillSpec _bill;
        private ResourceRateSpec _rates;
        private readonly List<string> _selections = new List<string>();
        public IReadOnlyList<IResourceLotSelectionDefinition> Selections { get; private set; }
        public Bill Quantities { get; private set; }
        public IReadOnlyList<IContentDefinition> ContentDependencies { get; private set; }
        public WorkResourceUse Use { get; private set; }
        public IReadOnlyList<IResourceRate> Rates { get; private set; } = Array.Empty<IResourceRate>();
        private ResourceRequirementDeclaration(JsonReader reader)
        {
            _selections.AddRange(reader.Strings("lot_selections"));
            string use = reader.String("use");
            if (use == "consume") Use = WorkResourceUse.Consume;
            else if (use == "hold") Use = WorkResourceUse.Hold;
            else if (use == "continuous") Use = WorkResourceUse.Continuous;
            else if (use == "elapsed") Use = WorkResourceUse.Elapsed;
            else throw reader.Error("use must explicitly be consume, hold, continuous or elapsed");
            if (Use == WorkResourceUse.Elapsed) _rates = ResourceRateSpec.Read(reader, "rates");
            else _bill = BillSpec.Read(reader, "quantities");
        }
        public static ResourceRequirementDeclaration Read(JsonReader reader)
            => new ResourceRequirementDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (Quantities != null) throw new InvalidOperationException("resource requirement resolves once");
            if (Use == WorkResourceUse.Elapsed)
            {
                Rates = _rates.Resolve(content); _rates = null;
                var lines = new List<ResourceAmount>();
                foreach (var rate in Rates) lines.Add(new ResourceAmount(rate.Resource.Resource, rate.Quantity));
                Quantities = new Bill(lines);
            }
            else { Quantities = _bill.Resolve(content); _bill = null; }
            if (Quantities.IsEmpty) throw content.Fail("resource requirement needs a nonempty native bill");
            var dependencies = new List<IContentDefinition>(Quantities.ContentReferences(content.Content));
            var selections = new List<IResourceLotSelectionDefinition>(); var resources = new HashSet<ResourceId>();
            foreach (string id in _selections)
            {
                var selection = content.Def<IResourceLotSelectionDefinition>(id);
                if (!Quantities.AmountOf(selection.Resource).IsPositive || !resources.Add(selection.Resource)) throw content.Fail("lot selection must target one unique resource in the requirement bill");
                selections.Add(selection); dependencies.Add(selection);
            }
            Selections = selections.AsReadOnly(); ContentDependencies = dependencies.AsReadOnly();
            foreach (var line in Quantities.Lines)
                if (UnitConversion.ValidateAmount(content.Content.Resource(line.Resource).QuantityUnit, line.Amount) != UnitConversionStatus.Converted)
                    throw content.Fail("resource requirement violates native quantity unit");
        }
    }
    public abstract class ResourceWorkRequirementDef : WorkRequirementDef, IResourceWorkRequirement
    {
        public override IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        private ResourceRequirementDeclaration _declaration;
        public Bill Quantities => _declaration.Quantities;
        public IReadOnlyList<IResourceLotSelectionDefinition> Selections => _declaration.Selections;
        public WorkResourceUse Use => _declaration.Use;
        public IReadOnlyList<IResourceRate> Rates => _declaration.Rates;
        protected override void ReadCore(JsonReader reader) => _declaration = ResourceRequirementDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public override WorkRequirementResult Evaluate(WorkRequirementContext context) => WorkRequirementResult.Satisfied();
        public abstract Code CopySources(WorkRequirementContext context, IList<WorkResourceSource> destination);
        public abstract WorkRequirementResult CheckSource(WorkRequirementContext context, ResourceEndpointId endpoint);
    }
    /// <summary>One native entity/named-store source algorithm; stock and claims stay with actual endpoints.</summary>
    public sealed class EntityResourceSourceDeclaration
    {
        public Code Port { get; private set; }
        private EntityResourceSourceDeclaration(JsonReader reader)
        {
            JsonValue port = reader.Value("port");
            if (port == null || port.Kind != JsonKind.Null && port.Kind != JsonKind.String)
                throw reader.Error("resource port must be explicit null for local stock or a named port");
            if (port.Kind == JsonKind.String)
            {
                try { ContractIdentity.ValidateId(port.Text, "activity resource port"); }
                catch (ArgumentException error) { throw reader.Error(error.Message); }
                Port = new Code(port.Text);
            }
        }
        public static EntityResourceSourceDeclaration Read(JsonReader reader)
            => new EntityResourceSourceDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void ValidateBinding(World world, IResourceWorkRequirement requirement)
        {
            if (Port.IsNone) return;
            foreach (EntityDef entity in world.Content.All<EntityDef>())
            {
                IResourcePortDefinition port = entity.Component<IResourcePortDefinition>(Port);
                if (port == null) continue;
                bool accepted = true;
                foreach (var line in requirement.Quantities.Lines) accepted &= port.Accepts(world.Content, line.Resource);
                if (accepted) return;
            }
            throw new ContentException(requirement.Id + ": no selected compatible resource port definition " + Port);
        }
        private ResourcePortResolution Store(WorkRequirementContext context, Entity entity, IResourceWorkRequirement requirement, ResourceEndpointId? original)
        {
            if (entity?.Alive != true) return ResourcePortResolution.Unavailable(WorkResourceCodes.MissingSource);
            if (Port.IsNone)
            {
                IOutletPort local = entity.Get<IOutletPort>();
                if (local == null) return ResourcePortResolution.Unavailable(WorkResourceCodes.MissingSource);
                return original.HasValue && local.Endpoint != original.Value
                    ? ResourcePortResolution.Unavailable(ResourcePortCodes.OriginalUnavailable)
                    : ResourcePortResolution.ReadyStore(local);
            }
            IResourcePort port = entity.Get<IResourcePort>(Port);
            IResourcePortDefinition definition = entity.Def.Component<IResourcePortDefinition>(Port);
            if (port == null || definition == null) return ResourcePortResolution.Unavailable(WorkResourceCodes.MissingSource);
            foreach (var line in requirement.Quantities.Lines)
                if (!definition.Accepts(context.World.Content, line.Resource)) return ResourcePortResolution.Unavailable(WorkResourceCodes.Incompatible);
            return ResourcePortContracts.Store(context.World, entity, port, original);
        }
        public Code CopySources(WorkRequirementContext context, Entity entity, IResourceWorkRequirement requirement, IList<WorkResourceSource> destination)
        {
            destination.Clear(); ResourcePortResolution resolved = Store(context, entity, requirement, context.CommittedSource);
            if (resolved.Ready) destination.Add(new WorkResourceSource(resolved.Store.Endpoint, 0));
            return resolved.Blocker;
        }
        public WorkRequirementResult CheckSource(WorkRequirementContext context, Entity entity, IResourceWorkRequirement requirement, ResourceEndpointId endpoint)
        {
            ResourcePortResolution resolved = Store(context, entity, requirement, endpoint);
            return resolved.Ready ? WorkRequirementResult.Satisfied() : WorkRequirementResult.Pending(resolved.Blocker);
        }
    }
    public abstract class EntityResourceRequirementDef : ResourceWorkRequirementDef, IWorkRequirementBindingValidator
    {
        private EntityResourceSourceDeclaration _source;
        public Code Port => _source.Port;
        protected override void ReadCore(JsonReader reader) { base.ReadCore(reader); _source = EntityResourceSourceDeclaration.Read(reader); }
        protected abstract Entity Source(WorkRequirementContext context);
        public void ValidateBinding(World world) => _source.ValidateBinding(world,this);
        public override Code CopySources(WorkRequirementContext context, IList<WorkResourceSource> destination)
            => _source.CopySources(context,Source(context),this,destination);
        public override WorkRequirementResult CheckSource(WorkRequirementContext context, ResourceEndpointId endpoint)
            => _source.CheckSource(context,Source(context),this,endpoint);
    }
    public sealed class TargetResourceRequirementDef : EntityResourceRequirementDef, IWorkSupplyRequirement
    { protected override Entity Source(WorkRequirementContext context) => context.Target; }
    public sealed class WorkerResourceRequirementDef : EntityResourceRequirementDef, IWorkerWorkRequirement
    { protected override Entity Source(WorkRequirementContext context) => context.Worker; }
}
