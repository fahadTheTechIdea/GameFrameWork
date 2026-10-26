using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Engine extension behavior; the player proof never implements admission or work rules.
    public sealed class RestrictedAccessDef : Def, IContentAccessDefinition
    {
        private string[] _ids;
        private IReadOnlyList<EntityDef> _denied;
        private int _company;
        protected override void ReadCore(JsonReader reader)
        {
            _ids = reader.Strings("denied_entities"); _company = reader.Int("company");
            if (_company < 0 || reader.Raw.Get("denied_entities")?.Kind != JsonKind.Array)
                throw reader.Error("explicit entity array and company required");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            var rows = new List<EntityDef>(); var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in _ids)
            { if (!ids.Add(id)) throw content.Fail("repeated denied entity"); rows.Add(content.Def<EntityDef>(id)); }
            _denied = rows.AsReadOnly(); _ids = null;
        }
        public Code Check(World world, PlayerId player, IContentDefinition definition)
        {
            world.Services.Get<ContentAccessContractModule>().Observe(AccessProbeTarget.Basis, world, player, definition);
            if (player.Value == _company)
                foreach (EntityDef denied in _denied)
                    if (ReferenceEquals(denied, definition)) return new Code("proof.access.basis");
            return Code.None;
        }
    }
    public sealed class AccessRuleDef : Def
    {
        private string _target;
        public EntityDef Target { get; private set; }
        public bool Enabled { get; private set; }
        public string MetadataFault { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _target = reader.String("entity"); Enabled = reader.Bool("enabled");
            MetadataFault = reader.String("metadata_fault", null);
        }
        protected override void ResolveCore(ContentResolver content) => Target = content.Def<EntityDef>(_target);
    }
    // Invalid declaration probe: a frozen shared definition must never own session state.
    public sealed class StatefulAccessDef : Def, IContentAccessDefinition, IPersistentService
    {
        private bool _state;
        public int StateSchemaVersion => 1;
        protected override void ReadCore(JsonReader reader) { }
        public Code Check(World world, PlayerId player, IContentDefinition definition) => Code.None;
        public void Sync(IStateSync sync) => sync.Bool(ref _state);
    }
    public interface IAccessRuleA : IContentAccessRule { }
    public interface IAccessRuleB : IContentAccessRule { }
    public enum AccessProbeTarget { Basis, RuleCheck, RuleMetadata }
    public enum AccessProbeFault { Exception, PauseClock, AllocateIdentity, LayerWrite, RuleState, ChangeId, ChangeWorld, Recurse, SwallowedRecurse }
    public enum SpatialCallbackFault { Layer, LayerThrow, Revision, RevisionThrow }
    // Shared negative callback instrumentation only; actual providers retain all data and revisions.
    internal static class SpatialMutationProbe
    {
        internal static (bool PayloadRestored, bool RevisionChanged) Change(World world, bool restore)
        {
            ISpatialSpace space=world.Space;var before=new StateHasher();space.Sync(before);int version=space.ObservationVersion;
            if(space is PointSpace point)
            {
                PointLayer layer=point.Layer("guard.sample");
                Fix position=Fix.One,old=layer.Read(position);
                Code changed=point.WriteLayer(world,new PlayerId(0),"guard.sample",position,old==Fix.One?Fix.FromInt(2):Fix.One);
                if(!changed.IsNone)throw new InvalidOperationException("proof.spatial-mutation-refused: "+changed);
                // Negative injection only: a second authorized write would re-enter the
                // outer observer before restoration. Retain the genuine first-write
                // revision and restore through the original sample owner instead.
                if(restore&&!layer.Write(position,old))
                    throw new InvalidOperationException("proof.spatial-restoration-refused");
            }
            else if(space is TileMap map)
            {
                var layer=map.Layer<ByteLayer>("water");var position=new TileCoord(1,1);byte old=layer[position];
                layer.Set(position,old==1?(byte)2:(byte)1);if(restore)layer.Set(position,old);
            }
            else throw new InvalidOperationException("negative spatial mutation requires the actual declared proof space");
            var after=new StateHasher();space.Sync(after);
            return(before.Value==after.Value,version!=space.ObservationVersion);
        }
    }
    public class ProofAccessRule : IContentAccessRule, IPersistentService
    {
        private readonly World _world;
        private readonly AccessRuleDef _definition;
        private readonly ContentAccessContractModule _probe;
        private bool _enabled;
        private string _ruleId;
        private bool _foreign;
        public ProofAccessRule(World world, AccessRuleDef definition, ContentAccessContractModule probe)
        { _world = world; _definition = definition; _probe = probe; _enabled = definition.Enabled; _ruleId = definition.Id; }
        public string Id => _definition.Id;
        public int StateSchemaVersion => 1;
        string IContentAccessRule.Id
        {
            get
            {
                _probe.Observe(AccessProbeTarget.RuleMetadata, _world, new PlayerId(0), _definition.Target, this);
                return _definition.MetadataFault == "duplicate" ? "same.rule" :
                    _definition.MetadataFault == "invalid" ? "invalid id" : _ruleId;
            }
        }
        public World World => _foreign || _definition.MetadataFault == "foreign" ? null : _world;
        public Code Check(PlayerId player, IContentDefinition definition)
        {
            _probe.Trace.Add(_definition.Id);
            _probe.Observe(AccessProbeTarget.RuleCheck, _world, player, definition, this);
            return _enabled && player == new PlayerId(0) && ReferenceEquals(definition, _definition.Target)
                ? new Code(_definition.Id + ".denied") : Code.None;
        }
        public void Configure(bool enabled) => _enabled = enabled;
        public void Mutate(AccessProbeFault fault)
        {
            if (fault == AccessProbeFault.RuleState) _enabled = !_enabled;
            if (fault == AccessProbeFault.ChangeId) _ruleId = "changed.rule";
            if (fault == AccessProbeFault.ChangeWorld) _foreign = true;
        }
        public void Sync(IStateSync sync) => sync.Bool(ref _enabled);
    }
    public sealed class ProofAccessRuleA : ProofAccessRule, IAccessRuleA
    { public ProofAccessRuleA(World world, AccessRuleDef definition, ContentAccessContractModule probe) : base(world, definition, probe) { } }
    public sealed class ProofAccessRuleB : ProofAccessRule, IAccessRuleB
    { public ProofAccessRuleB(World world, AccessRuleDef definition, ContentAccessContractModule probe) : base(world, definition, probe) { } }
    public sealed class ConfigureAccessRuleCommand : Command
    {
        public string Rule;
        public bool Enabled;
        public override void Sync(IStateSync sync) { sync.String(ref Rule); sync.Bool(ref Enabled); }
    }
    public sealed class ConfigureAccessRuleHandler : CommandHandler<ConfigureAccessRuleCommand>
    {
        protected override Code Check(World world, ConfigureAccessRuleCommand command)
        {
            if (command.Player != new PlayerId(0)) return Codes.NotAllowed;
            foreach (ProofAccessRule rule in world.Services.Providers<ProofAccessRule>())
                if (rule.Id == command.Rule) return Code.None;
            return Codes.NotFound;
        }
        protected override void Apply(World world, ConfigureAccessRuleCommand command, CommandContext context)
        {
            foreach (ProofAccessRule rule in world.Services.Providers<ProofAccessRule>())
                if (rule.Id == command.Rule) { rule.Configure(command.Enabled); return; }
            throw new InvalidOperationException("accepted rule disappeared");
        }
    }
    public sealed class ContentAccessContractModule : GameModule
    {
        public const string ModuleId = "proof.access-contracts";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(ContentAccessModule.ModuleId, Version) };
        public bool ActualAlias, DirectDefinition, RuleAliases;
        public InitializeProbe Initialize { get; private set; }
        public readonly List<string> Trace = new List<string>();
        private AccessProbeTarget _target;
        private AccessProbeFault? _fault;
        private SpatialCallbackFault? _spatialFault;
        public bool SpatialPayloadRestored { get; private set; }
        public bool SpatialRevisionChanged { get; private set; }
        public int Reads;
        public bool MutationCompleted;
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<RestrictedAccessDef>("restricted_access");
            types.Section<AccessRuleDef>("proof_access_rules");
            types.Section<StatefulAccessDef>("stateful_access");
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Bind(this);
            var definition = composer.Content.All<IContentAccessDefinition>();
            ActualAlias = definition.Count == 1 && ReferenceEquals(definition[0], composer.Content.Find<IContentAccessDefinition>(definition[0].Id));
            DirectDefinition = typeof(OpenContentAccessDef).BaseType == typeof(Def);
            var a = new ProofAccessRuleA(composer.World, composer.Content.Get<AccessRuleDef>("proof.access.a"), this);
            var z = new ProofAccessRuleB(composer.World, composer.Content.Get<AccessRuleDef>("proof.access.b"), this);
            // Deliberately declare B before A; ContentAccessPolicy owns deterministic ID ordering.
            composer.Services.Bind<IAccessRuleB>(z); composer.Services.Bind<ProofAccessRuleB>(z);
            composer.Services.Bind<IAccessRuleA>(a); composer.Services.Bind<ProofAccessRuleA>(a);
            int count = 0;
            foreach (IPersistentService state in composer.Services.Providers<IPersistentService>())
                if (ReferenceEquals(state, a) || ReferenceEquals(state, z)) count++;
            RuleAliases = count == 2 && ReferenceEquals(composer.Services.Get<IAccessRuleA>(), composer.Services.Get<ProofAccessRuleA>())
                && ReferenceEquals(composer.Services.Get<IAccessRuleB>(), composer.Services.Get<ProofAccessRuleB>());
            composer.Commands.Register("proof.configure-access", new ConfigureAccessRuleHandler());
            Initialize = new InitializeProbe(ModuleId + ".initialize"); composer.AddSystem(Initialize, 12347);
        }
        public void Arm(AccessProbeTarget target, AccessProbeFault fault)
        { _target = target; _fault = fault; _spatialFault=null; Reads = 0; MutationCompleted = false; }
        public void ArmSpatial(AccessProbeTarget target,SpatialCallbackFault fault)
        { _target=target;_fault=null;_spatialFault=fault;Reads=0;MutationCompleted=false;SpatialPayloadRestored=SpatialRevisionChanged=false; }
        internal void Observe(AccessProbeTarget target, World world, PlayerId player, IContentDefinition definition, ProofAccessRule rule = null)
        {
            if(_spatialFault.HasValue&&target==_target&&Reads==0)
            {
                Reads++;var spatialFault=_spatialFault.Value;
                var changed=SpatialMutationProbe.Change(world,spatialFault==SpatialCallbackFault.Revision||spatialFault==SpatialCallbackFault.RevisionThrow);
                SpatialPayloadRestored=changed.PayloadRestored;SpatialRevisionChanged=changed.RevisionChanged;MutationCompleted=true;
                if(spatialFault==SpatialCallbackFault.LayerThrow||spatialFault==SpatialCallbackFault.RevisionThrow)
                    throw new InvalidOperationException("proof.spatial-access-first-error");
                return;
            }
            if (!_fault.HasValue || target != _target || Reads != 0) return;
            Reads++;
            AccessProbeFault fault = _fault.Value;
            if (fault == AccessProbeFault.Exception) throw new InvalidOperationException("proof.access-callback");
            if (fault == AccessProbeFault.PauseClock) ((GameClock)world.Clock).Pause();
            if (fault == AccessProbeFault.AllocateIdentity) world.Spawn(world.Content.Get<EntityDef>("tower"), new PlayerId(0),
                entity => entity.Get<ITilePresence>().Place(new TileCoord(44, 28), FixVec2.Centre(new TileCoord(44, 28))));
            if (fault == AccessProbeFault.LayerWrite) ((TileMap)world.Space).Layer<ByteLayer>("water").Set(new TileCoord(1, 1), 1);
            if (fault == AccessProbeFault.RuleState || fault == AccessProbeFault.ChangeId || fault == AccessProbeFault.ChangeWorld)
                rule.Mutate(fault);
            if (fault == AccessProbeFault.Recurse || fault == AccessProbeFault.SwallowedRecurse)
            {
                try { world.Services.Get<IContentAccess>().Check(world, player, definition); }
                catch (InvalidOperationException) when (fault == AccessProbeFault.SwallowedRecurse) { }
            }
            MutationCompleted = true;
        }
    }
}
