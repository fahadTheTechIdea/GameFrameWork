using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    /// <summary>Sole authored distance/footprint policy; original map/exposure/work owners keep all state.</summary>
    public sealed class TileIncidentPropagationDeclaration
    {
        private readonly IncidentPropagationPolicyDeclaration _policy;
        private readonly string _space;
        private readonly string _exposureEffect;
        public IIncidentExposureEffect ExposureEffect { get; private set; }
        public ITileSpaceDefinition Space { get; private set; }
        public long Radius { get; }
        public TileFootprintConditionDeclaration SourceTiles { get; }
        public TileFootprintConditionDeclaration TargetTiles { get; }
        public GameDuration Interval => _policy.Interval;
        public GameDuration Delay => _policy.Delay;
        public Code TargetSource => _policy.TargetSource;
        public Code TargetKind => _policy.TargetKind;
        public bool CrossCompany => _policy.CrossCompany;
        public System.Collections.Generic.IReadOnlyList<SignalGateCondition> SourceConditions => _policy.SourceConditions;
        public System.Collections.Generic.IReadOnlyList<SignalGateCondition> TargetConditions => _policy.TargetConditions;
        private TileIncidentPropagationDeclaration(JsonReader reader)
        {
            _policy = IncidentPropagationPolicyDeclaration.Read(reader);
            _space = reader.String("space"); Radius = reader.Long("radius");
            _exposureEffect=reader.String("exposure_effect",null);
            if(_exposureEffect!=null)try{ContractIdentity.ValidateId(_exposureEffect,"exposure effect");}catch(ArgumentException error){throw reader.Error(error.Message);}
            try { ContractIdentity.ValidateId(_space, "tile propagation space"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            if (Radius < 0) throw reader.Error("tile propagation radius cannot be negative");
            SourceTiles = TileFootprintConditionDeclaration.Read(reader.Child("source_tiles")
                ?? throw reader.Error("explicit source_tiles required"));
            TargetTiles = TileFootprintConditionDeclaration.Read(reader.Child("target_tiles")
                ?? throw reader.Error("explicit target_tiles required"));
        }
        public static TileIncidentPropagationDeclaration Read(JsonReader reader)
            => new TileIncidentPropagationDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (Space != null) throw new InvalidOperationException("tile propagation resolves once");
            Space = content.Def<ITileSpaceDefinition>(_space); _policy.Resolve(content);
            if(_exposureEffect!=null)ExposureEffect=content.Def<IIncidentExposureEffect>(_exposureEffect);
            SourceTiles.Resolve(content); TargetTiles.Resolve(content);
        }
        public void Validate(ContentResolver content)
        { SourceTiles.Validate(content, Space); TargetTiles.Validate(content, Space); }
        public void ValidateSource(EntityDef source)
        {
            _policy.ValidateSource(source);
            if (source.Component<ITilePresenceDefinition>()?.Space != Space.Id)
                throw new ContentException(source.Id + ": propagation requires its actual tile space");
        }
        public void ValidateBinding(World world, string id)
        {
            _policy.ValidateBinding(world, id);
            if (Space == null || !world.Content.Owns(Space) || !ReferenceEquals(world.Content.Find<ITileSpaceDefinition>(Space.Id), Space) ||
                !(world.Space is ITileSpatialSpace) || world.Space.Id != Space.Id)
                throw new ContentException(id + ": required propagation tile space is not selected");
            SourceTiles.ValidateBinding(world, Space); TargetTiles.ValidateBinding(world, Space);
        }
        public IncidentExposureObservation Observe(IncidentPropagationContext context, Entity target, string id)
        {
            Code blocker = _policy.CheckConditions(context, target, id);
            if (!blocker.IsNone) return IncidentExposureObservation.Refused(blocker);
            ITilePresence a = context.Source.Get<ITilePresence>(), b = target.Get<ITilePresence>();
            if (a == null || b == null || !a.Placed || !b.Placed || a.Space != Space.Id || b.Space != Space.Id)
                return IncidentExposureObservation.Refused(IncidentPropagationCodes.Presence);
            if (!(context.World.Space is ITileSpatialSpace map) || map.Id != Space.Id)
                throw new InvalidOperationException("tile propagation requires the actual selected tile geometry");
            TileRect from = a.Footprint, to = b.Footprint;
            long dx = Math.Max(0, Math.Max((long)from.X - to.X - to.W + 1, (long)to.X - from.X - from.W + 1));
            long dy = Math.Max(0, Math.Max((long)from.Y - to.Y - to.H + 1, (long)to.Y - from.Y - from.H + 1));
            if (Math.Max(dx, dy) > Radius) return IncidentExposureObservation.Refused(IncidentPropagationCodes.Distance);
            if (!SourceTiles.Matches(context.World, map, from)) return IncidentExposureObservation.Refused(IncidentPropagationCodes.SourceTerrain);
            if (!TargetTiles.Matches(context.World, map, to)) return IncidentExposureObservation.Refused(IncidentPropagationCodes.TargetTerrain);
            return IncidentExposureObservation.Ready;
        }
    }
}
