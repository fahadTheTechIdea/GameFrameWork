using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IInventoryWorkEffectDefinition : IPhysicalWorkEffect, IWorkEffectIntentAdmission
    {
        Bill Quantities { get; }
        IReadOnlyList<ResourcePortRoute> Ports { get; }
        ResourceFlowKind Kind { get; }
    }

    /// <summary>Sole authored disposition metadata; the original transaction owns every material change.</summary>
    public sealed class InventoryWorkEffectDeclaration
    {
        private readonly BillSpec _quantities;
        private readonly List<KeyValuePair<string, string>> _ports = new List<KeyValuePair<string, string>>();
        public Bill Quantities { get; private set; }
        public IReadOnlyList<ResourcePortRoute> Ports { get; private set; }
        public IReadOnlyList<IContentDefinition> ContentDependencies { get; private set; }
        public ResourceFlowKind Kind { get; }
        private InventoryWorkEffectDeclaration(JsonReader reader)
        {
            string kind = reader.String("kind");
            Kind = kind == "consumed" ? ResourceFlowKind.Consumed : kind == "destroyed" ? ResourceFlowKind.Destroyed :
                kind == "lost" ? ResourceFlowKind.Lost : throw reader.Error("inventory effect kind must be consumed, destroyed or lost");
            if (!reader.Has("quantities")) throw reader.Error("inventory effect requires explicit quantities");
            _quantities = BillSpec.Read(reader, "quantities");
            if (_quantities.IsEmpty) throw reader.Error("inventory disposition bill must not be empty");
            ResourcePortContracts.ReadRoutes(reader, "ports", _ports);
        }
        public static InventoryWorkEffectDeclaration Read(JsonReader reader)
            => new InventoryWorkEffectDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (Quantities != null) throw new InvalidOperationException("inventory disposition resolves once");
            Quantities = _quantities.Resolve(content);
            Ports = ResourcePortContracts.ResolveRoutes(content, _ports);
            ResourcePortContracts.ValidateRoutes(Ports, Quantities, Array.Empty<IResourceRate>(), "disposition");
            ContentDependencies = Quantities.ContentReferences(content.Content);
        }
    }

    /// <summary>One original lifecycle admission/commit path for native and independent physical effects.</summary>
    public static class InventoryWorkEffectContracts
    {
        public static void RequireCapabilities(World world, IInventoryWorkEffectDefinition effect)
        {
            if (effect is not Def actual || !actual.IsFrozen || !world.Content.Owns(effect) ||
                !ReferenceEquals(world.Content.Find<IInventoryWorkEffectDefinition>(effect.Id), effect) ||
                !ReferenceEquals(world.Content.Find<IWorkEffect>(effect.Id), effect) || effect.Quantities == null || effect.Quantities.IsEmpty || effect.Ports == null ||
                !ResourceConversionInput.IsDispositionKind(effect.Kind))
                throw new ContentException("inventory effect requires its actual immutable bill/kind contract alias");
            if (!world.Services.Has<IResourceTransaction>()) throw new ContentException(effect.Id + ": inventory effect requires IResourceTransaction");
            ResourcePortContracts.ValidateRoutes(effect.Ports, effect.Quantities, Array.Empty<IResourceRate>(), "disposition");
            foreach (var line in effect.Quantities.Lines)
            {
                var resource = world.Content.Resource(line.Resource);
                if (resource == null || !line.Amount.IsPositive || UnitConversion.ValidateAmount(resource.QuantityUnit, line.Amount) != UnitConversionStatus.Converted)
                    throw new ContentException(effect.Id + ": inventory disposition requires exact positive native quantities");
            }
        }
        public static Code CheckIntent(World world, IInventoryWorkEffectDefinition effect, PlayerId company, Entity target)
        {
            RequireCapabilities(world, effect);
            if (target == null || !target.Alive || !ReferenceEquals(world.Entities.Get(target.Id), target)) return Codes.NotFound;
            if (target.Owner != company) return Codes.NotOwned;
            return EntityObservation.ReadProvider(world, effect, target, () =>
                ResourcePortContracts.CheckDefinitionBindings(world.Content, target.Def, effect.Ports, effect.Quantities, Array.Empty<IResourceRate>(), true));
        }
        private static Code Prepare(World world, IInventoryWorkEffectDefinition effect, PlayerId company, Entity target, ReservationOwner owner,
            out IReadOnlyList<ResourcePortBinding> bindings, out IReadOnlyList<ResourceClaimIntent> claims)
        {
            bindings = null; claims = null;
            Code refusal = CheckIntent(world, effect, company, target);
            if (!refusal.IsNone) return refusal;
            bindings = ResourcePortContracts.Bindings(world, target, effect.Ports);
            refusal = ResourcePortContracts.Blocker(bindings);
            if (!refusal.IsNone) return refusal;
            claims = ResourcePortContracts.OutboundClaims(owner, ResourcePortContracts.QuantitiesByEndpoint(bindings, effect.Quantities));
            ResourcePortContracts.Verify(bindings);
            return Refusal(world.Services.Get<IResourceTransaction>().EvaluateClaims(company, claims, Array.Empty<ResourceReservation>()).Status);
        }
        public static Code Check(World world, IInventoryWorkEffectDefinition effect, PlayerId company, Entity target, ReservationOwner owner)
            => Prepare(world, effect, company, target, owner, out _, out _);
        public static Code CheckStart(World world, IInventoryWorkEffectDefinition effect, PlayerId company, Entity target)
            => Check(world, effect, company, target, new ReservationOwner("sim.inventory-effect-preview", target?.Id.Value ?? 0));
        public static Code Commit(World world, IInventoryWorkEffectDefinition effect, PlayerId company, Entity target, ReservationOwner owner)
        {
            Code refusal = Prepare(world, effect, company, target, owner, out var bindings, out var claims);
            if (!refusal.IsNone) return refusal;
            IResourceTransaction transactions = world.Services.Get<IResourceTransaction>();
            var acquired = transactions.AcquireClaims(company, claims, Array.Empty<ResourceReservation>());
            if (!acquired.Committed) return Refusal(acquired.Status);
            // Books may advance during acquisition, but a callback may not redirect the named port.
            // Resolve through the SAME port owner; never consume an alternative or fabricate rollback.
            var current = ResourcePortContracts.Bindings(world, target, effect.Ports);
            for (int i = 0; i < bindings.Count; i++)
                if (!current[i].Ready || current[i].Endpoint != bindings[i].Endpoint ||
                    current[i].Company != bindings[i].Company || !ReferenceEquals(current[i].Policy, bindings[i].Policy))
                    throw new InvalidOperationException("disposition port changed after complete claim acquisition");
            var inputs = new List<ResourceConversionInput>();
            foreach (var receipt in acquired.Receipts) inputs.Add(new ResourceConversionInput(effect.Kind, receipt, receipt.Amount, false));
            var result = transactions.CommitConversion(new ResourceConversionIntent(owner, company, inputs,
                Array.Empty<ResourceProductionIntent>(), new Code(effect.Id)), null);
            if (!result.Committed) throw new InvalidOperationException("disposition changed after complete claim acquisition: " + result.Status);
            return Code.None;
        }
        private static Code Refusal(ResourceTransferStatus status)
        {
            if (status == ResourceTransferStatus.Ready || status == ResourceTransferStatus.Committed) return Code.None;
            if (status == ResourceTransferStatus.MissingEndpoint) return Codes.NotFound;
            if (status == ResourceTransferStatus.NoStock) return WorkEffectCodes.NoStock;
            if (status == ResourceTransferStatus.Incompatible || status == ResourceTransferStatus.NoCapacity) return WorkEffectCodes.IncompatibleInventory;
            return Codes.InvalidArgument;
        }
    }
}
