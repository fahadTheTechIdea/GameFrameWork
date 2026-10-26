using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Sole authored metadata parser/resolver; no live signal, gameplay state or codec.</summary>
    public sealed class AutomationSignalDeclaration
    {
        public AutomationSignalKind Kind { get; }
        public Code Metric { get; }
        public Code Unit { get; }
        public IReadOnlyList<AutomationSignalProviderSchema> Providers { get; private set; }
        private readonly string[] _providerIds;
        private readonly int[] _providerVersions;
        private AutomationSignalDeclaration(JsonReader reader)
        {
            string kind = reader.String("kind");
            Kind = kind == "number" ? AutomationSignalKind.Number : kind == "boolean" ? AutomationSignalKind.Boolean
                : throw reader.Error("signal kind must be number or boolean");
            Metric = new Code(reader.String("metric")); Unit = new Code(reader.String("unit"));
            try { ContractIdentity.ValidateId(Metric.Id,"signal metric"); ContractIdentity.ValidateId(Unit.Id,"signal unit"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            JsonValue providers = reader.Array("providers");
            if (providers == null || providers.Count == 0) throw reader.Error("signal providers must be authored");
            _providerIds = new string[providers.Count]; _providerVersions = new int[providers.Count];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i=0;i<providers.Count;i++)
            {
                var row = new JsonReader(providers[i],reader.Where+".providers["+i+"]");
                _providerIds[i]=row.String("component");_providerVersions[i]=row.Int("schema");row.End();
                if (string.IsNullOrEmpty(_providerIds[i]) || !seen.Add(_providerIds[i]) || _providerVersions[i]<1)
                    throw reader.Error("provider component identities are unique and schema versions positive");
            }
        }
        public static AutomationSignalDeclaration Read(JsonReader reader)
            => new AutomationSignalDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content, IAutomationSignalContent signal)
        {
            if (Providers != null) throw new InvalidOperationException("signal metadata resolves once");
            if (signal == null || signal.Kind != Kind || signal.Metric != Metric || signal.Unit != Unit)
                throw content.Fail("signal metadata does not belong to its declaration");
            var providers = new List<AutomationSignalProviderSchema>();
            for (int i=0;i<_providerIds.Length;i++)
            {
                var schema = content.Content.ComponentDeclaration(_providerIds[i])?.SignalSchema;
                if (schema == null || schema.Version != _providerVersions[i])
                    throw content.Fail("signal provider '"+_providerIds[i]+"' is missing, inactive or has a different schema");
                AutomationSignalContracts.ValidateProvider(content.Content,signal,schema);
                providers.Add(schema);
            }
            providers.Sort((a,b)=>string.CompareOrdinal(a.StateCodec.Id,b.StateCodec.Id));Providers=providers.AsReadOnly();
        }
    }

    /// <summary>Sole typed signal reader/admission owner for native and independent descriptors.</summary>
    public static class AutomationSignalContracts
    {
        /// <summary>Sole actual registered-metric admission for reusable stateless signal bridges.</summary>
        internal static ComponentDef ValidateReusable(World world, IAutomationSignalDefinition definition, Type runtime,
            int codecVersion, int signalVersion, IReadOnlyList<AutomationMetricSchema> required, Entity entity)
        {
            if (!(definition is ComponentDef part) || !part.IsFrozen || !world.Content.Owns(part.Owner) ||
                part.ComponentType != runtime || part.StateCodec.SchemaVersion != codecVersion ||
                !ReferenceEquals(part.Owner.Component<IAutomationSignalDefinition>(definition.CapabilityId), definition) ||
                entity != null && !ReferenceEquals(entity.Def, part.Owner) || definition.SignalSchemaVersion != signalVersion ||
                definition.SignalSchema == null || !ReferenceEquals(part.SignalSchema, definition.SignalSchema) ||
                definition.SignalSchema.Version != signalVersion || !ReferenceEquals(definition.SignalSchema.StateCodec, part.StateCodec))
                throw new ContentException("signal binding requires actual immutable owner, original runtime/codec and registered schema");
            var bindings = definition.SignalBindings;
            if (bindings == null || bindings.Count != required.Count || definition.SignalSchema.Bindings.Count != required.Count)
                throw new ContentException(part.Owner.Id + ": signal requires original typed metric declarations");
            foreach (var expected in required)
            {
                if (!definition.SignalSchema.TryGet(expected.Metric, out var registered) || registered.Kind != expected.Kind ||
                    registered.Unit != expected.Unit || registered.UsesAuthoredUnit != expected.UsesAuthoredUnit)
                    throw new ContentException(part.Owner.Id + ": signal registered metrics differ from original facts");
                int matches = 0;
                foreach (var declared in bindings)
                    if (declared.Metric == expected.Metric && declared.Kind == expected.Kind && declared.Unit == expected.Unit &&
                        declared.UsesAuthoredUnit == expected.UsesAuthoredUnit) matches++;
                if (matches != 1) throw new ContentException(part.Owner.Id + ": signal metadata differs from registered metrics");
            }
            return part;
        }
        internal static void ValidateProvider(ResolvedContent content, IAutomationSignalContent signal, AutomationSignalProviderSchema schema)
        {
            var declared = content.ComponentDeclaration(schema.StateCodec.Id)?.SignalSchema;
            if (!ReferenceEquals(declared,schema) || !schema.TryGet(signal.Metric,out var binding) || binding.Kind!=signal.Kind ||
                !binding.UsesAuthoredUnit && binding.Unit!=signal.Unit)
                throw new ContentException("signal '"+signal.Id+"' disagrees with actual provider metric/kind/unit");
            if (binding.UsesAuthoredUnit)
            {
                var quantity=signal as IAutomationQuantitySignalDefinition;
                if (quantity?.QuantityUnit==null ||
                    !ReferenceEquals(content.Find<IUnitContentDefinition>(signal.Unit.Id),quantity.QuantityUnit))
                    throw new ContentException("signal '"+signal.Id+"' requires its actual resolved catalog quantity unit");
            }
        }
        internal static void ValidateComposition(World world)
        {
            foreach (var signal in world.Content.All<IAutomationSignalContent>())
            {
                if (!world.Content.Owns(signal) || !ReferenceEquals(world.Content.Find<IAutomationSignalContent>(signal.Id),signal))
                    throw new ContentException("actual unambiguous signal descriptor required");
                var guard=EntityObservation.CaptureProvider(world,signal);
                try
                {
                    ContractIdentity.ValidateId(signal.Metric.Id,"signal metric");ContractIdentity.ValidateId(signal.Unit.Id,"signal unit");
                    if ((signal.Kind!=AutomationSignalKind.Number && signal.Kind!=AutomationSignalKind.Boolean) || signal.Providers==null || signal.Providers.Count==0)
                        throw new ContentException(signal.Id+": explicit typed providers required");
                    var seen=new HashSet<string>(StringComparer.Ordinal);
                    foreach (var provider in signal.Providers)
                    {
                        if (provider==null || !seen.Add(provider.StateCodec.Id)) throw new ContentException(signal.Id+": duplicate/missing provider");
                        ValidateProvider(world.Content,signal,provider);
                    }
                    if (signal is IResourceAutomationSignalDefinition resource && (resource.Resource==null ||
                        !ReferenceEquals(world.Content.Resource(resource.Resource.Resource),resource.Resource) || resource.QuantityUnit==null ||
                        resource.Resource.QuantityUnit.Dimension!=resource.QuantityUnit.Dimension))
                        throw new ContentException(signal.Id+": actual compatible resource/unit required");
                }
                catch (ArgumentException error) { throw new ContentException(signal.Id+": "+error.Message); }
                finally { guard.Verify(); }
            }
        }
        internal static bool Accepts(this IAutomationSignalContent signal, AutomationSignalProviderSchema source)
        {
            foreach (var provider in signal.Providers) if (ReferenceEquals(provider,source)) return true;
            return false;
        }
        // Schema belongs to the actual named authored metadata, never a runtime class/cast.
        internal static AutomationSignalProviderSchema Schema(EntityDef owner, Code identity)
            => owner != null && !identity.IsNone && owner.HasCapability<IAutomationSignalSource>(identity)
                ? owner.Component<IAutomationSignalDefinition>(identity)?.SignalSchema : null;
        internal static bool Accepts(World world, IAutomationSignalContent signal, IAutomationSignalSource source)
        {
            if (source == null) return false;
            return EntityObservation.ReadProvider(world, source, null, () =>
                EntityObservation.ReadProvider(world, signal, null, () =>
                {
                    Entity entity = ActualSource(world, source);
                    var schema = Schema(entity.Def, source.CapabilityId);
                    return schema != null && signal.Accepts(schema);
                }));
        }
        private static Entity ActualSource(World world, IAutomationSignalSource source)
        {
            Entity entity = source.Entity;
            if (entity == null || !ReferenceEquals(world.Entities.Get(entity.Id), entity) || source.CapabilityId.IsNone ||
                !ReferenceEquals(entity.Get<IAutomationSignalSource>(source.CapabilityId), source))
                throw new ArgumentException("the signal source must be the actual live named capability of the executing world", nameof(source));
            return entity;
        }
        public static AutomationSignalSnapshot ReadFrom(this IAutomationSignalContent signal, World world, IAutomationSignalSource source)
        {
            if (world==null) throw new ArgumentNullException(nameof(world));
            if (signal==null || !world.Content.Owns(signal) || !ReferenceEquals(world.Content.Find<IAutomationSignalContent>(signal.Id),signal))
                throw new ArgumentException("the signal must belong to the executing world's content");
            world.RequireHealthyUpdate();
            return EntityObservation.ReadProvider(world, signal, null, () =>
            {
                if (source == null) return signal.Unavailable(OperationCodes.SignalUnavailable);
                return EntityObservation.ReadProvider(world, source, null, () =>
                {
                    Entity entity = ActualSource(world, source);
                    var schema = Schema(entity.Def, source.CapabilityId);
                    if (schema == null || !signal.Accepts(schema)) return signal.Unavailable(OperationCodes.SignalUnavailable);
                    try { var value = source.ReadSignal(world, signal); signal.Validate(value); return value; }
                    catch (Exception error) { world.LatchFault("operation.signal", error); throw; }
                });
            });
        }
        public static AutomationSignalSnapshot Number(this IAutomationSignalContent signal, Fix value, Code unit)
        {
            if (signal.Kind!=AutomationSignalKind.Number || signal.Unit!=unit)
                throw new InvalidOperationException("signal '"+signal.Id+"' disagrees with its numeric provider binding");
            return new AutomationSignalSnapshot(signal.Id,signal.Kind,signal.Unit,value,Code.None);
        }
        public static AutomationSignalSnapshot Boolean(this IAutomationSignalContent signal, bool value)
        {
            if (signal.Kind!=AutomationSignalKind.Boolean || signal.Unit!=new Code("unit.boolean"))
                throw new InvalidOperationException("signal '"+signal.Id+"' disagrees with its boolean provider binding");
            return new AutomationSignalSnapshot(signal.Id,signal.Kind,signal.Unit,value?Fix.One:Fix.Zero,Code.None);
        }
        public static AutomationSignalSnapshot Unavailable(this IAutomationSignalContent signal, Code blocker)
            =>new AutomationSignalSnapshot(signal.Id,signal.Kind,signal.Unit,null,blocker);
        internal static void Validate(this IAutomationSignalContent signal, AutomationSignalSnapshot value)
        {
            if (value.Signal!=signal.Id || value.Kind!=signal.Kind || value.Unit!=signal.Unit || value.Available==!value.Blocker.IsNone)
                throw new InvalidOperationException("signal '"+signal.Id+"' violated its authored identity/kind/unit contract");
            if (value.Available && signal is IAutomationQuantitySignalDefinition quantity &&
                UnitConversion.ValidateAmount(quantity.QuantityUnit,value.Value.Value)!=UnitConversionStatus.Converted)
                throw new InvalidOperationException("signal '"+signal.Id+"' returned an invalid exact quantity");
        }
    }
}
