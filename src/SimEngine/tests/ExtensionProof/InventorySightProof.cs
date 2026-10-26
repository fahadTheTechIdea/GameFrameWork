using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.ExtensionProof
{
    // Ordinary selected game module: one real visibility service, JSON settings, command and existing codec.
    // No inventory, job, production, timing or driver-side gameplay owner.
    public interface IInventorySightControl { bool Hidden { get; set; } }
    public sealed class SetInventorySightCommand : Command
    {
        public bool Hidden;
        public override void Sync(IStateSync sync) => sync.Bool(ref Hidden);
    }
    // Read-only framework diagnostic at the real command boundary, before automatic assignment.
    // It never releases a worker, creates work, reserves stock or owns a diagnostic/gameplay cache.
    public sealed class ProbeReadyCargoDisclosureCommand : Command
    {
        public EntityId Worker, Destination;
        public int Request;
        public bool Diagnostics;
        public override void Sync(IStateSync sync)
        { sync.Entity(ref Worker); sync.Entity(ref Destination); sync.Int(ref Request); sync.Bool(ref Diagnostics); }
    }
    // Pure observation before native dispatch. Both actual offer selection and permission decisions stay in the engine.
    public sealed class ProbeAutomaticServiceDisclosureCommand : Command
    {
        public EntityId Worker, WaitingWorker, Source, Destination;
        public LogisticsDispatchKind Kind;
        public bool Diagnostics;
        public override void Sync(IStateSync sync)
        { sync.Entity(ref Worker); sync.Entity(ref WaitingWorker); sync.Entity(ref Source); sync.Entity(ref Destination); sync.Enum(ref Kind); sync.Bool(ref Diagnostics); }
    }
    public sealed class InventorySightModule : GameModule
    {
        public const string ModuleId = "proof.inventory-sight";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InventoryModule.ModuleId, Version) };
        public override void Compose(WorldComposer composer)
        {
            var settings = composer.Scenario.SettingsFor(Id) ?? throw new ContentException("inventory sight settings are required");
            string kind = settings.String("target_kind"), target = settings.String("target");
            bool hidden = settings.Bool("initial_hidden"); settings.End();
            IContentDefinition definition;
            if (kind == "entity") definition = composer.Content.Get<EntityDef>(target);
            else if (kind == "resource") definition = composer.Content.Get<IResourceContentDefinition>(target);
            else if (kind == "activity") definition = composer.Content.Get<IAuthoredActivityDefinition>(target);
            else if (kind == "trip-profile") definition = composer.Content.Get<IHaulTripProfile>(target);
            else if (kind == "load-profile") definition = composer.Content.Get<IHaulLoadProfile>(target);
            else throw new ContentException("inventory sight target_kind must be entity, resource, activity, trip-profile or load-profile");
            var state = new Sight(definition, kind != "entity", hidden);
            composer.Services.Bind<IVisibility>(state); composer.Services.Bind<IInventorySightControl>(state);
            composer.RequireViewFeature<InventoryView>("sim.inventory", 12);
            composer.Commands.Register("proof.inventory-sight.set", new SetSightHandler());
            composer.Commands.Register("proof.inventory-sight.ready-cargo", new ProbeCargoHandler());
            composer.Commands.Register("proof.inventory-sight.automatic-service", new ProbeAutomaticServiceHandler());
        }
        private sealed class Sight : IVisibility, IInventorySightControl, IPersistentService
        {
            private readonly IContentDefinition _target;
            private readonly bool _content;
            public Sight(IContentDefinition target, bool content, bool hidden) { _target = target; _content = content; Hidden = hidden; }
            public string Id => "proof.inventory-sight.state";
            public int StateSchemaVersion => 1;
            public bool Hidden { get; set; }
            public bool CanSee(World world, PlayerId viewer, Entity entity)
                => entity.Owner == viewer && !(!_content && Hidden && ReferenceEquals(entity.Def, _target));
            public bool CanSeeContent(World world, PlayerId viewer, IContentDefinition definition)
                => !(_content && Hidden && ReferenceEquals(definition, _target));
            public void Sync(IStateSync sync) { bool hidden = Hidden; sync.Bool(ref hidden); if (sync.Reading) Hidden = hidden; }
        }
        private sealed class SetSightHandler : CommandHandler<SetInventorySightCommand>
        {
            protected override Code Check(World world, SetInventorySightCommand command)
                => command.Player == new PlayerId(0) ? Code.None : Codes.NotOwned;
            protected override void Apply(World world, SetInventorySightCommand command, CommandContext context)
                => world.Services.Get<IInventorySightControl>().Hidden = command.Hidden;
        }
        private sealed class ProbeCargoHandler : CommandHandler<ProbeReadyCargoDisclosureCommand>
        {
            protected override Code Check(World world, ProbeReadyCargoDisclosureCommand command)
            {
                var viewer = world.ViewFor(command.Player);
                if (!viewer.TryGetEntity(command.Worker, out var worker) || worker.Owner != command.Player) return Codes.NotFound;
                var rows = new List<CargoReturnView>();
                Code observed = world.ViewFor(PlayerId.None).Feature<LogisticsView>().CopyCargoReturns(command.Worker, rows);
                if (!observed.IsNone || rows.Count != 1 || rows[0].Return.Status != HaulStatus.Ready
                    || rows[0].Return.Carrier != command.Worker || rows[0].Return.Destination != command.Destination
                    || rows[0].Return.HaulDemand.Scope != WorkModule.ModuleId || rows[0].Return.HaulDemand.Identity != command.Request
                    || !viewer.TryFindResource("supply", out var resource) || resource != rows[0].Resource)
                    throw new InvalidOperationException("cargo disclosure probe requires the actual ready owned work-request recovery before assignment");
                Code result;
                if (command.Diagnostics)
                {
                    result = viewer.Feature<LogisticsView>().ReadDiagnostics(command.Worker, resource, out var facts);
                    if (result == Codes.NotFound && facts != null) throw new InvalidOperationException("private cargo diagnosis published a prefix");
                }
                else
                {
                    result = viewer.Feature<LogisticsView>().CopyCargoReturns(command.Worker, rows);
                    if (result == Codes.NotFound && rows.Count != 0) throw new InvalidOperationException("private cargo return published a prefix");
                }
                if (!result.IsNone && result != Codes.NotFound) throw new InvalidOperationException("unexpected cargo disclosure result/" + result);
                return result;
            }
            protected override void Apply(World world, ProbeReadyCargoDisclosureCommand command, CommandContext context) { }
        }
        private sealed class ProbeAutomaticServiceHandler : CommandHandler<ProbeAutomaticServiceDisclosureCommand>
        {
            protected override Code Check(World world, ProbeAutomaticServiceDisclosureCommand command)
            {
                var viewer = world.ViewFor(command.Player);
                if (!viewer.TryGetEntity(command.Worker, out var worker) || worker.Owner != command.Player
                    || !viewer.TryFindResource("supply", out var resource)) return Codes.NotFound;
                var actual = new List<LogisticsDispatchOffer>();
                world.ViewFor(PlayerId.None).Feature<LogisticsView>().CopyQueue(actual);
                bool returning = command.Kind == LogisticsDispatchKind.CargoReturn;
                bool recovery = returning || command.Kind == LogisticsDispatchKind.Evacuation;
                if (!recovery && command.Kind != LogisticsDispatchKind.Demand) return Codes.InvalidArgument;
                int routes = 0, demands = 0;
                foreach (var offer in actual) if (offer.Resource == resource && (recovery
                    ? offer.Kind == command.Kind && offer.RecoveryOrigin == command.Source : offer.Destination == command.Destination))
                {
                    if (offer.Haul.Status != HaulStatus.Ready || offer.Haul.Carrier != command.Worker
                        || offer.Haul.Destination != command.Destination || !returning && offer.Haul.Source != command.Source)
                        throw new InvalidOperationException("automatic service probe requires actual ready serviced-carrier offers: actual="
                            + offer.Kind + "/" + offer.Haul.Status + "/" + offer.Haul.Blocker + "/from=" + offer.Haul.Source + "/to=" + offer.Haul.Destination
                            + "/worker=" + offer.Haul.Carrier + "; expected from=" + command.Source + "/to=" + command.Destination + "/worker=" + command.Worker);
                    if (offer.Route == 0) demands++; else routes++;
                }
                if (demands != 1 || routes != (recovery ? 0 : 1)) throw new InvalidOperationException("automatic service probe requires the actual selected recovery or route and source demand");
                var waiting = HaulTripPolicyContracts.ReadServiceWindow(world, world.Entities.Get(command.WaitingWorker));
                if (waiting == null || waiting.Unrestricted || waiting.Blocker.IsNone)
                    throw new InvalidOperationException("automatic service probe requires a real service-blocked discarded carrier");
                var view = viewer.Feature<LogisticsView>();
                if (returning)
                {
                    var rows = new List<CargoReturnView>();
                    if (!view.CopyCargoReturns(command.Worker, rows).IsNone || rows.Count != 1 || rows[0].Return.Status != HaulStatus.Ready
                        || rows[0].Return.Carrier != command.Worker || rows[0].Return.Destination != command.Destination)
                        throw new InvalidOperationException("unrelated explicit shared carrier must stay admissible");
                }
                else if (view.PreviewHaul(command.Source, command.Destination, resource, Fix.FromInt(2), worker: command.Worker).Status != HaulStatus.Ready)
                    throw new InvalidOperationException("unrelated explicit serviced carrier must stay admissible");
                if (command.Diagnostics)
                {
                    Code result = view.ReadDiagnostics(command.Destination, resource, out var facts);
                    if (result == Codes.NotFound && facts != null) throw new InvalidOperationException("automatic service diagnosis leaked a prefix");
                    if (!result.IsNone && result != Codes.NotFound) throw new InvalidOperationException("unexpected automatic service diagnosis/" + result);
                    return result;
                }
                var published = new List<LogisticsDispatchOffer>(); view.CopyQueue(published); int count = 0;
                foreach (var offer in published) if (offer.Resource == resource && (recovery
                    ? offer.Kind == command.Kind && offer.RecoveryOrigin == command.Source : offer.Destination == command.Destination)) count++;
                if (count != 0 && count != (recovery ? 1 : 2)) throw new InvalidOperationException("automatic service offer disclosure published a partial decision set");
                return count == 0 ? Codes.NotFound : Code.None;
            }
            protected override void Apply(World world, ProbeAutomaticServiceDisclosureCommand command, CommandContext context) { }
        }
    }
}
