using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>An entity became another def (an upgrade): same place, same owner, new id.</summary>
    public struct EntityReplaced
    {
        public EntityId Old;
        public EntityId New;
        public PlayerId Owner;
    }

    /// <summary>
    /// Sole state-preserving replacement coordinator. Geometry belongs to the selected spatial
    /// provider; resource stock moves through verified receipts, never through component bytes.
    /// </summary>
    public static class Upgrades
    {
        public static readonly Code NoteUpgraded = new Code("note.upgraded");

        public static readonly Code IncompatibleState = new Code("core.replacement-state-incompatible");
        public static readonly Code StockWouldBeLost = new Code("core.replacement-stock-loss");
        public static readonly Code InventoryReserved = new Code("core.replacement-inventory-reserved");
        public static readonly Code WorkerBusy = new Code("core.replacement-worker-busy");

        public static Code Check(World world, Entity old, EntityDef def)
        {
            return Check(world, old, def, out _);
        }

        private static Code Check(World world, Entity old, EntityDef def, out SpatialPlacement placement)
        {
            placement = null;
            if (world == null || old == null || !ReferenceEquals(world.Entities.Get(old.Id), old)) return Codes.NotFound;
            if (world.Fault != null) return Codes.NotAllowed;
            // The version is not a codec: a guard can alter another component's
            // persisted payload, the clock or spatial state without touching it.
            // Keep the single preflight owner and withhold placement until every
            // original observation scope verifies, including typed refusals.
            var admission = EntityObservation.ReadProvider(world, old, old, () => {
                Code result = CheckCurrent(world, old, def, out SpatialPlacement captured);
                return (Result: result, Placement: captured);
            }, space: world.Space, observeTargetState: true);
            placement = admission.Placement;
            return admission.Result;
        }

        private static Code CheckCurrent(World world, Entity old, EntityDef def, out SpatialPlacement placement)
        {
            placement = null;
            if (def == null || !ReferenceEquals(world.Content.Find<EntityDef>(def.Id), def)) return Codes.UnknownDef;
            if (old.Get<IWorker>() is IWorker worker && worker.Job != 0) return WorkerBusy;
            if (old.Get<ISupplier>() is ISupplier supplier && supplier.Reservations.Count != 0) return InventoryReserved;
            IInletPort inlet = old.Get<IInletPort>();
            if (inlet != null && inlet.Reservations.Count != 0) return InventoryReserved;
            ISpatialPresence presence = old.Get<ISpatialPresence>();
            ISpatialPresenceDefinition destination = def.Component<ISpatialPresenceDefinition>();
            if ((presence == null) != (destination == null)) return Codes.NotAllowed;
            if (presence != null)
            {
                if (!(world.Space is ISpatialPlacement provider) || presence.Space != world.Space.Id || destination.Space != presence.Space)
                    return Codes.NotAllowed;
                placement = provider.CapturePlacement(old);
                if (placement == null || placement.Space != presence.Space) return Codes.InvalidArgument;
                Code refusal = provider.CheckPlacement(world, def, placement, old);
                if (!refusal.IsNone) return refusal;
            }
            foreach (Component component in old.Components)
            {
                if (component is IReplacementGuard guard)
                {
                    Code refusal = guard.CheckReplacement(world, def);
                    if (!refusal.IsNone) return refusal;
                }
                else if (component is IEntityRemovalGuard removal && !(component is IResourceEndpoint))
                {
                    Code refusal = removal.CheckRemoval(world);
                    if (!refusal.IsNone) return refusal;
                }
                if (component is ISpatialPresence) continue;
                ComponentDef target = Find(def, component.Def.Key);
                if (target != null && !(component is IResourceEndpoint) && !CanRetainState(component.Def, target)) return IncompatibleState;
            }
            List<ResourceAmount> stock = ReadStock(world, inlet);
            if (stock.Count > 0)
            {
                IInventoryDefinition capacity = def.Component<IInventoryDefinition>();
                if (!(inlet is IOutletPort) || capacity == null || !CapacityPolicyContracts.Check(capacity.CapacityPolicy, stock,
                    Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>()).Fits) return StockWouldBeLost;
                foreach (ResourceAmount line in stock)
                    if (!capacity.Accepts(world.Content, line.Resource) ||
                        CapacityPolicyContracts.Quantum(capacity.CapacityPolicy, line.Resource).Floor(line.Amount) != line.Amount)
                        return StockWouldBeLost;
            }
            foreach (IEntityReplacementReferenceOwner owner in ReferenceOwners(world))
            {
                Component component = owner as Component;
                Code refusal = EntityObservation.ReadProvider(world, owner, component?.Entity,
                    () => owner.CheckReferenceReplacement(world, old, def), space: world.Space, observeTargetState: true);
                if (!refusal.IsNone) return refusal;
            }
            return Code.None;
        }

        public static Code TryReplace(World world, Entity old, EntityDef def, out Entity made)
        {
            made = null;
            Code refusal = Check(world, old, def, out SpatialPlacement placement);
            if (!refusal.IsNone) return refusal;
            int sourceVersion = old.Version;
            var preparationFacts = EntityObservation.Capture(world, old);
            var preparationState = EntityObservation.CaptureProvider(world, old, old.Sync, "entity.replace");
            var preparationSpace = EntityObservation.CaptureSpace(world, world.Space);
            var preparationEntities = new List<(EntityObservation.Scope Facts, EntityObservation.ProviderScope State)>();
            foreach (Entity entity in world.Entities.CaptureRegistered())
                if (!ReferenceEquals(entity, old))
                    preparationEntities.Add((EntityObservation.Capture(world, entity),
                        EntityObservation.CaptureProvider(world, entity, entity.Sync, "entity.replace")));
            var preparationServices = new List<EntityObservation.ProviderScope>();
            foreach (var service in world.Services.Persistent())
                preparationServices.Add(EntityObservation.CaptureProvider(world, service.Provider, service.Provider.Sync, "entity.replace"));
            void CheckPreparation(bool latch)
            {
                preparationFacts.Verify();
                if (latch) preparationState.Verify(); else preparationState.CheckUnchanged();
                if (latch) preparationSpace.Verify(); else preparationSpace.CheckUnchanged();
                foreach (var entity in preparationEntities)
                {
                    entity.Facts.Verify();
                    if (latch) entity.State.Verify(); else entity.State.CheckUnchanged();
                }
                foreach (var service in preparationServices)
                    if (latch) service.Verify(); else service.CheckUnchanged();
            }
            void VerifyPreparation() => CheckPreparation(true);
            try
            {
                IReadOnlyList<ResourceAmount> quantities = ReadStock(world, old.Get<IInletPort>()).AsReadOnly();
                var carried = new List<KeyValuePair<string, byte[]>>();
                foreach (Component component in old.Components)
                {
                    if (component is ISpatialPresence || component is IResourceEndpoint || Find(def, component.Def.Key) == null) continue;
                    using (var ms = new MemoryStream())
                    {
                        var w = new StateWriter(ms);
                        component.Sync(w);
                        w.Flush();
                        carried.Add(new KeyValuePair<string, byte[]>(component.Def.Key, ms.ToArray()));
                    }
                }
                refusal = world.Entities.Replace(old, def, world.Tick, world.Space,
                    entity => { if (placement != null) ((ISpatialPlacement)world.Space).ConfigurePlacement(entity, placement); },
                    VerifyPreparation,
                    entity =>
                    {
                        // Resource owners are never byte-carried, including finite/unlimited supplier books.
                        foreach (Component component in entity.Components)
                        {
                            if (component is ISpatialPresence) continue;
                            if (component is IEntityReplacementState retained)
                            {
                                Component source = null;
                                foreach (Component part in old.Components) if (part.Def.Key == component.Def.Key) source = part;
                                if (source != null)
                                {
                                    retained.RestoreReplacementState(world, source);
                                    VerifyPreparation();
                                }
                                continue;
                            }
                            if (component is IResourceEndpoint) continue;
                            foreach (KeyValuePair<string, byte[]> state in carried)
                            if (state.Key == component.Def.Key)
                            {
                                var reader = new StateReader(state.Value, 0, state.Value.Length);
                                component.Sync(reader);
                                if (!reader.AtEnd) throw new CorruptStateException("replacement codec did not consume its state: " + component.Def.Key);
                                VerifyPreparation();
                            }
                        }
                    },
                    entity => {
                        // Candidate-local initialization/retention is allowed. It must
                        // not alter the original source or session before admission.
                        VerifyPreparation();
                        Code current = CheckPrepared(world, old, entity, quantities, sourceVersion);
                        VerifyPreparation();
                        return current;
                    },
                    entity => Relocate(world, old, entity, quantities), out made);
            }
            catch (Exception error)
            {
                if (world.Fault == null)
                {
                    if (world.Entities.Replacing) world.FaultReplacement(error);
                    else
                    {
                        // A purely local failed candidate may be discarded safely.
                        // Verify that claim, preserving the ORIGINAL callback exception.
                        // Observation already owns any first mutation fault it detects.
                        try { CheckPreparation(false); }
                        catch { world.FaultReplacement(error); }
                    }
                }
                throw;
            }
            if (!refusal.IsNone) return refusal;
            world.Events.Publish(new EntityReplaced { Old = old.Id, New = made.Id, Owner = old.Owner });
            world.Notify(NoteUpgraded, old.Owner, made.Id, def.Id);
            return Code.None;
        }

        private static Code CheckPrepared(World world, Entity old, Entity target, IReadOnlyList<ResourceAmount> quantities, int sourceVersion)
        {
            try
            {
                return EntityObservation.ReadProvider(world, old, old, () => {
                    // This private candidate has an original codec, but no world
                    // registration yet. Never assert a live entity binding for it.
                    if (target.Alive || world.Entities.Get(target.Id) != null)
                        throw new InvalidOperationException("replacement admission requires an unregistered candidate");
                    var candidate = EntityObservation.CaptureProvider(world, target, target.Sync, "entity.replace");
                    Code refusal = CheckPreparedCurrent(world, old, target, quantities, sourceVersion);
                    candidate.Verify();
                    if (target.Alive || world.Entities.Get(target.Id) != null)
                        throw new InvalidOperationException("replacement admission registered its private candidate");
                    return refusal;
                }, space: world.Space, observeTargetState: true);
            }
            catch (Exception error) { world.FaultReplacement(error); throw; }
        }

        private static Code CheckPreparedCurrent(World world, Entity old, Entity target, IReadOnlyList<ResourceAmount> quantities, int sourceVersion)
        {
            ResourcePortSelectionContracts.ValidateReplacement(world, old, target);
            IInletPort source = old.Get<IInletPort>(), destination = target.Get<IInletPort>();
            if (source != null && (source.Endpoint != ResourceEndpointId.ForEntity(old.Id) || source.Company != old.Owner) ||
                destination != null && (destination.Endpoint != ResourceEndpointId.ForEntity(target.Id) || destination.Company != target.Owner))
                throw new InvalidOperationException("replacement endpoint identity differs from its actual entity owner");
            // A local creation/retention callback is not permission to clone resource state.
            if (old.Version != sourceVersion || !SameStock(quantities, ReadStock(world, source)) ||
                source?.Reservations.Count > 0 || old.Get<ISupplier>()?.Reservations.Count > 0 ||
                ReadStock(world, destination).Count != 0 || destination?.Reservations.Count > 0 ||
                target.Get<ISupplier>()?.Reservations.Count > 0)
            {
                throw new InvalidOperationException("replacement preparation changed resource state or promises");
            }
            if (quantities.Count == 0) return Code.None;
            if (!(source is IOutletPort outlet) || destination == null || !world.Services.Has<IResourceTransaction>()) return StockWouldBeLost;
            var owner = new ReservationOwner("sim.replacement", old.Id.Value);
            int beforeSource = source.Version, beforeTarget = destination.Version;
            bool allowed = destination.CheckIncoming(quantities).Fits;
            foreach (ResourceAmount line in quantities)
            {
                CapacityQuantum common = CapacityQuantum.Combine(CapacityPolicyContracts.Quantum(source.CapacityPolicy, line.Resource), CapacityPolicyContracts.Quantum(destination.CapacityPolicy, line.Resource));
                allowed &= common.Status == CapacityStatus.Fits && common.Floor(line.Amount) == line.Amount &&
                    outlet.CanReserveOut(owner, line.Resource, line.Amount) && destination.CanReserveIn(owner, line.Resource, line.Amount);
            }
            if (source.Version != beforeSource || destination.Version != beforeTarget)
            {
                throw new InvalidOperationException("replacement provider mutated pure admission");
            }
            return allowed ? Code.None : StockWouldBeLost;
        }

        private static void Relocate(World world, Entity old, Entity target, IReadOnlyList<ResourceAmount> quantities)
        {
            var owner = new ReservationOwner("sim.replacement", old.Id.Value);
            IOutletPort source = old.Get<IOutletPort>(); IInletPort destination = target.Get<IInletPort>();
            if (quantities.Count != 0)
            {
                IResourceTransaction transactions = world.Services.Get<IResourceTransaction>();
                var movement = new ResourceMovementIntent(owner, old.Owner, source.Endpoint, destination.Endpoint,
                    new Bill(quantities), Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(),
                    Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
                if (!transactions.EvaluateMovement(movement).Ready)
                    throw new InvalidOperationException("replacement whole-vector admission changed");
                ResourceTransferBatchResult moved = transactions.CommitMovement(movement);
                if (!moved.Committed || moved.CommittedTransfers != quantities.Count)
                    throw new InvalidOperationException("replacement whole-vector transfer refused");
            }
            VerifyRelocatedStock(world, old, target, quantities);
            Code refusal = world.CheckRemoval(old, null, target.Def);
            if (!refusal.IsNone) throw new InvalidOperationException("replacement source retirement refused: " + refusal.Id);
            // References and their parent versions may change; identity, state,
            // physical placement, resources, clock and random stream may not.
            var sourceFacts = EntityObservation.Capture(world, old, allowEntityVersionChange: true);
            var targetFacts = EntityObservation.Capture(world, target, allowEntityVersionChange: true);
            var spaceFacts = EntityObservation.CaptureSpace(world, world.Space);
            SpatialSnapshot sourceShape = EntityObservation.ReadSpatial(world, old);
            SpatialSnapshot targetShape = EntityObservation.ReadSpatial(world, target);
            // Reference owners may legitimately change their own reference codecs, including
            // components on entities with stock. Observe actual endpoint-only state instead.
            var resources = new List<EntityObservation.ProviderScope>();
            foreach (Entity entity in world.Entities.CaptureRegistered())
                foreach (Component component in entity.Components)
                    if (component is IResourceEndpoint endpoint)
                        resources.Add(EntityObservation.CaptureProvider(world, component, writer => WriteReferenceResource(endpoint, writer),
                            "entity.replace", allowEntityVersionChange: true));
            foreach (IResourceEndpointProvider provider in world.Services.Providers<IResourceEndpointProvider>())
            {
                resources.Add(EntityObservation.CaptureProvider(world, provider,
                    writer => WriteReferenceResourceCatalog(provider, writer), "entity.replace", allowEntityVersionChange: true));
                EntityObservation.ReadProvider(world, provider, null, () => {
                    provider.InspectReservations((identity, records) => {
                        IResourceEndpoint endpoint = provider.Resolve(identity);
                        if (endpoint == null || endpoint.Endpoint != identity)
                            throw new InvalidOperationException("reference resource observation requires an actual existing endpoint");
                        resources.Add(EntityObservation.CaptureProvider(world, endpoint, writer => {
                            IResourceEndpoint current = provider.Resolve(identity);
                            if (current == null || current.Endpoint != identity)
                                throw new InvalidOperationException("reference resource endpoint identity changed");
                            WriteReferenceResource(current, writer);
                        }, "entity.replace", allowEntityVersionChange: true));
                    });
                    return true;
                }, space: world.Space);
            }
            foreach (IEntityReplacementReferenceOwner referenceOwner in ReferenceOwners(world, old, target))
            {
                referenceOwner.ReplaceEntityReferences(world, old, target);
                foreach (var resource in resources) resource.Verify();
                sourceFacts.Verify(); targetFacts.Verify(); spaceFacts.Verify();
                VerifyRelocatedStock(world, old, target, quantities);
                if (!EntityObservation.MatchesSpatial(world, old, sourceShape, EntityObservation.ReadSpatial(world, old)) ||
                    !EntityObservation.MatchesSpatial(world, target, targetShape, EntityObservation.ReadSpatial(world, target)))
                    throw new InvalidOperationException("reference rebinding changed physical replacement placement");
                sourceFacts.Verify(); targetFacts.Verify(); spaceFacts.Verify();
            }
        }

        private static void WriteReferenceResource(IResourceEndpoint endpoint, IStateSync writer)
        {
            ResourceStateEncoding.RequireWriter(writer);
            int version = endpoint.Version;
            ResourceEndpointId identity = endpoint.Endpoint; ResourceEndpointId.Sync(writer, ref identity);
            int company = endpoint.Company.Value; writer.Int(ref company);
            endpoint.WriteResourceState(writer);
            if (endpoint.Version != version || endpoint.Endpoint != identity || endpoint.Company.Value != company)
                throw new InvalidOperationException("resource-only encoding changed its actual endpoint");
        }
        private static void WriteReferenceResourceCatalog(IResourceEndpointProvider provider, IStateSync writer)
        {
            ResourceStateEncoding.RequireWriter(writer);
            var identities = new List<ResourceEndpointId>();
            provider.InspectReservations((identity, records) => identities.Add(identity));
            writer.Count(identities.Count);
            foreach (ResourceEndpointId entry in identities) { var identity = entry; ResourceEndpointId.Sync(writer, ref identity); }
        }
        private static void VerifyRelocatedStock(World world, Entity old, Entity target, IReadOnlyList<ResourceAmount> quantities)
        {
            void Verify(Entity entity, IReadOnlyList<ResourceAmount> expected)
                => EntityObservation.ReadProvider(world, entity, entity, () => {
                    IInletPort inlet = entity.Get<IInletPort>();
                    if (!SameStock(expected, ReadStock(world, inlet)) || inlet?.Reservations.Count > 0 ||
                        entity.Get<ISupplier>()?.Reservations.Count > 0 || inlet != null &&
                        (inlet.Endpoint != ResourceEndpointId.ForEntity(entity.Id) || inlet.Company != entity.Owner))
                        throw new InvalidOperationException("replacement violated complete stock/promise disposition");
                    return true;
                }, space: world.Space, observeTargetState: true);
            Verify(old, Array.Empty<ResourceAmount>());
            Verify(target, quantities);
        }

        private static IReadOnlyList<IEntityReplacementReferenceOwner> ReferenceOwners(World world, Entity retired = null, Entity prepared = null)
        {
            var owners = new List<IEntityReplacementReferenceOwner>();
            void Add(IEntityReplacementReferenceOwner candidate)
            {
                foreach (var existing in owners) if (ReferenceEquals(existing, candidate)) return;
                owners.Add(candidate);
            }
            foreach (var owner in world.Services.Providers<IEntityReplacementReferenceOwner>()) Add(owner);
            foreach (GameSystem system in world.Systems)
                if (system is IEntityReplacementReferenceOwner owner) Add(owner);
            foreach (Entity entity in world.Entities.All)
                if (entity.Alive && !ReferenceEquals(entity, retired))
                    foreach (Component component in entity.Components)
                        if (component is IEntityReplacementReferenceOwner owner) Add(owner);
            if (prepared != null)
                foreach (Component component in prepared.Components)
                    if (component is IEntityReplacementReferenceOwner owner) Add(owner);
            return owners.AsReadOnly();
        }

        // Temporary immutable-intent vectors, not a second stock authority. Catalog also covers non-inventory ports.
        private static List<ResourceAmount> ReadStock(World world, IInletPort port)
        {
            var result = new List<ResourceAmount>();
            if (port == null) return result;
            foreach (IResourceContentDefinition definition in world.Content.Resources)
            {
                Fix amount = port.Amount(definition.Resource);
                if (amount < Fix.Zero || UnitConversion.ValidateAmount(definition.QuantityUnit, amount) != UnitConversionStatus.Converted)
                    throw new InvalidOperationException("replacement endpoint contains invalid native stock");
                if (amount.IsPositive) result.Add(new ResourceAmount(definition.Resource, amount));
            }
            if (port is IInventory inventory && !SameStock(result, inventory.Stock))
                throw new InvalidOperationException("replacement inventory vector disagrees with its resource amounts");
            return result;
        }

        private static bool SameStock(IReadOnlyList<ResourceAmount> a, IReadOnlyList<ResourceAmount> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (a[i].Resource != b[i].Resource || a[i].Amount != b[i].Amount) return false;
            return true;
        }

        private static ComponentDef Find(EntityDef definition, string key)
        {
            foreach (ComponentDef component in definition.Components) if (component.Key == key) return component;
            return null;
        }

        internal static bool CanRetainState(ComponentDef a, ComponentDef b)
            => a.ComponentType == b.ComponentType && a.StateCodec.Id == b.StateCodec.Id
                && a.StateCodec.Owner == b.StateCodec.Owner && a.StateCodec.SchemaVersion == b.StateCodec.SchemaVersion;
    }
}
