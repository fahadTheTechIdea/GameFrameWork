using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Another file of the same owner. No separate stock, packing ledger, scheduler or codec.
    public sealed partial class ConstructionService
    {
        public static readonly Code RecoveryIncompatible = new Code("sim.resource.recovery-incompatible");

        // Geometry reads use the same observation owner as other engine previews. The actual
        // source/container codec and selected space remain authoritative, including on refusal.
        private T ReadRecoveryGeometry<T>(World world, Entity target, Func<T> read)
            => EntityObservation.ReadProvider(world, world.Space, target, read,
                space: world.Space, observeTargetState: true);

        private void ValidateRecoveryDefinition(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ContentException("recovery belongs to another session");
            foreach (IResourceRecoveryDefinition profile in world.Content.All<IResourceRecoveryDefinition>())
                EntityObservation.ReadProvider(world, profile, null, () => {
                    if (!(profile is Def actual) || !actual.IsFrozen || !world.Content.Owns(profile) ||
                        !ReferenceEquals(world.Content.Find<IResourceRecoveryDefinition>(profile.Id), profile))
                        throw new ContentException("recovery requires its actual frozen catalog profile");
                    ResourceRecoveryDeclaration.ValidateMetadata(world.Content, profile.Space, profile.Container);
                    return true;
                });
            if (Definition == null) return; // Explicit null selection, never an inferred container.
            if (!ReferenceEquals(world.Content.Find<IResourceRecoveryDefinition>(Definition.Id), Definition) ||
                world.Space == null || Definition.Space != world.Space.Id ||
                !ReferenceEquals(world.Content.Find<EntityDef>(Definition.Container?.Id), Definition.Container) ||
                !(world.Space is ISpatialRecoveryPlacement provider) ||
                Definition.Container.Component<ISpatialPresenceDefinition>()?.Space != world.Space.Id ||
                !EntityObservation.ReadProvider(world, provider, null, () => provider.CheckRecoveryDefinition(Definition.Container)).IsNone)
                throw new ContentException("recovery requires exact authored profile/container and selected compatible geometry");
        }

        public Code CheckEvacuation(World world, Entity source)
        {
            if (!ReferenceEquals(world, _world)) return Codes.InvalidArgument;
            world.RequireHealthyUpdate();
            if (source == null || !ReferenceEquals(world.Entities.Get(source.Id), source)) return Codes.NotFound;
            IInventory stock = source.Get<IInventory>();
            if (stock == null) return world.CheckDespawn(source);
            if (stock.Reservations.Count != 0) return ResourceTransactions.ActivePromises;
            Code refusal = world.CheckRemoval(source, stock, null);
            if (!refusal.IsNone || stock.Stock.Count == 0) return refusal;
            if (!(world.Space is ISpatialPlacement provider) || source.Get<ISpatialPresence>()?.Space != world.Space.Id) return Codes.InvalidArgument;
            return CheckPacking(world, ReadRecoveryGeometry(world, source, () => provider.CapturePlacement(source)), new Bill(stock.Stock), stock);
        }

        public Code Evacuate(World world, Entity source)
        {
            Code refusal = CheckEvacuation(world, source); if (!refusal.IsNone) return refusal;
            IInventory stock = source.Get<IInventory>(); if (stock == null || stock.Stock.Count == 0) return Code.None;
            try
            {
                SpatialPlacement placement = ReadRecoveryGeometry(world, source, () => ((ISpatialPlacement)world.Space).CapturePlacement(source));
                Pack(world, source.Owner, placement, new Bill(stock.Stock), source,
                    new ReservationOwner("sim.resource.recovery", source.Id.Value), Code.None);
                if (stock.Stock.Count != 0) throw new InvalidOperationException("resource recovery left undisposed source stock");
                return Code.None;
            }
            catch (Exception error) { world.LatchFault("construction.recovery", error); throw; }
        }

        public Code CheckYield(World world, PlayerId company, SpatialPlacement placement, Bill resources, ReservationOwner requester, Code cause)
        {
            if (!ReferenceEquals(world, _world)) return Codes.InvalidArgument;
            world.RequireHealthyUpdate();
            if (resources == null || !requester.IsValid || cause.IsNone ||
                company != PlayerId.None && !world.Players.IsSeat(company)) return Codes.InvalidArgument;
            return CheckPacking(world, placement, resources, null);
        }
        /// <summary>Explicit authored structural yield, distinct from moving existing stock.</summary>
        public Code ReleaseYield(World world, PlayerId company, SpatialPlacement placement, Bill resources, ReservationOwner requester, Code cause)
        {
            Code refusal = CheckYield(world, company, placement, resources, requester, cause); if (!refusal.IsNone) return refusal;
            try { Pack(world, company, placement, resources, null, requester, cause); return Code.None; }
            catch (Exception error) { world.LatchFault("construction.recovery", error); throw; }
        }

        private Code CheckPacking(World world, SpatialPlacement placement, Bill resources, IInventory source)
        {
            if (!ReferenceEquals(world, _world)) return Codes.InvalidArgument;
            if (resources.IsEmpty) return Code.None;
            if (Definition == null) return ResourceTransactions.MissingRecovery;
            if (placement == null || placement.Space != Definition.Space || !(world.Space is ISpatialRecoveryPlacement provider)) return Codes.InvalidArgument;
            Code geometry = ReadRecoveryGeometry(world, source?.Entity, () => provider.CheckPlacement(world, Definition.Container, placement, null));
            if (!geometry.IsNone) return geometry;
            IInventoryDefinition receiving = Definition.Container.Component<IInventoryDefinition>();
            foreach (ResourceAmount line in resources.Lines)
            {
                IResourceContentDefinition resource = world.Content.Resource(line.Resource);
                if (resource == null || UnitConversion.ValidateAmount(resource.QuantityUnit, line.Amount) != UnitConversionStatus.Converted ||
                    !receiving.Accepts(world.Content, line.Resource)) return RecoveryIncompatible;
                CapacityQuantum quantum = source == null ? CapacityPolicyContracts.Quantum(receiving.CapacityPolicy, line.Resource)
                    : CapacityQuantum.Combine(CapacityPolicyContracts.Quantum(source.CapacityPolicy, line.Resource), CapacityPolicyContracts.Quantum(receiving.CapacityPolicy, line.Resource));
                if (quantum.Floor(line.Amount) != line.Amount ||
                    !quantum.Floor(CapacityPolicyContracts.Offer(receiving.CapacityPolicy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), line.Resource, line.Amount).Amount).IsPositive)
                    return RecoveryIncompatible;
            }
            return Code.None;
        }

        private void Pack(World world, PlayerId company, SpatialPlacement placement, Bill resources, Entity source,
            ReservationOwner requester, Code cause)
        {
            if (resources.IsEmpty) return;
            var provider = (ISpatialRecoveryPlacement)world.Space; EntityDef container = Definition.Container;
            IInventory original = source?.Get<IInventory>(), store = null;
            IResourceTransaction transactions = world.Services.Get<IResourceTransaction>();
            foreach (ResourceAmount resource in resources.Lines)
            {
                Fix remaining = resource.Amount;
                CapacityQuantum quantum = CapacityPolicyContracts.Quantum(container.Component<IInventoryDefinition>().CapacityPolicy, resource.Resource);
                if (original != null) quantum = CapacityQuantum.Combine(CapacityPolicyContracts.Quantum(original.CapacityPolicy, resource.Resource), quantum);
                while (remaining.IsPositive)
                {
                    if (store == null || !quantum.Floor(store.Offer(resource.Resource, remaining)).IsPositive)
                    {
                        if (!ReadRecoveryGeometry(world, source, () => provider.CheckPlacement(world, container, placement, null)).IsNone)
                            throw new InvalidOperationException("recovery geometry changed after successful preflight");
                        Entity entity = world.Spawn(container, company, e => provider.ConfigurePlacement(e, placement)); store = entity.Get<IInventory>();
                        SpatialPlacement actual = ReadRecoveryGeometry(world, entity, () => provider.CapturePlacement(entity));
                        string actualIdentity = ReadRecoveryGeometry(world, entity, () => provider.PlacementIdentity(actual));
                        string intendedIdentity = ReadRecoveryGeometry(world, entity, () => provider.PlacementIdentity(placement));
                        if (store == null || store.Stock.Count != 0 || store.Reservations.Count != 0 || actualIdentity != intendedIdentity)
                            throw new InvalidOperationException("recovery provider violated empty creation or source placement");
                    }
                    Fix amount = quantum.Floor(store.Offer(resource.Resource, remaining));
                    if (!amount.IsPositive || amount > remaining) throw new InvalidOperationException("recovery provider violated exact bounded packing preflight");
                    if (original != null)
                    {
                        var movement = new ResourceMovementIntent(requester, company, original.Endpoint, store.Endpoint,
                            new Bill(new[] { new ResourceAmount(resource.Resource, amount) }), Array.Empty<ResourceReservation>(),
                            Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
                        if (!transactions.CommitMovement(movement).Committed)
                            throw new InvalidOperationException("resource recovery provider refused exact transfer");
                    }
                    else if (!transactions.CommitInventory(new ResourceInventoryIntent(requester, company, store.Entity.Id,
                        new Bill(new[] { new ResourceAmount(resource.Resource, amount) }), ResourceFlowKind.Produced, cause)).Committed)
                        throw new InvalidOperationException("recovery provider refused exact lifecycle yield");
                    remaining -= amount;
                }
            }
        }
    }
}
