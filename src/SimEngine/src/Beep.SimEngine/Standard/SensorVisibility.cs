using System;
using System.Collections.Generic;
using System.Globalization;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Live owned sensor union within one rule; other visibility and layer ownership gates still apply.</summary>
    public abstract class SensorVisibilityDef<T> : Def, IVisibilityTarget where T : class, IContentDefinition
    {
        private string _target;
        private string[] _sensors;
        public T Target { get; private set; }
        protected abstract string TargetSpace { get; }
        protected override void ReadCore(JsonReader reader)
        {
            _target = reader.String("definition");
            if (!reader.Has("sensors")) throw reader.Error("explicit sensor identities required");
            _sensors = reader.Strings("sensors");
            if (_sensors.Length == 0) throw reader.Error("sensor identities cannot be empty");
            Array.Sort(_sensors, StringComparer.Ordinal);
            for (int i = 0; i < _sensors.Length; i++)
            {
                try { ContractIdentity.ValidateId(_sensors[i], "sensor"); }
                catch (ArgumentException error) { throw reader.Error(error.Message); }
                if (i > 0 && _sensors[i] == _sensors[i - 1]) throw reader.Error("duplicate sensor identity");
            }
        }
        protected override void ResolveCore(ContentResolver content) => Target = content.Def<T>(_target);
        public IReadOnlyList<IContentDefinition> Targets => KnowledgeVisibilityContracts.SingleTarget(Target);
        public void ValidateBinding(World world)
        {
            if (!world.Content.Owns(Target) || world.Space == null || TargetSpace != world.Space.Id)
                throw new ContentException(Id + ": sensor visibility requires actual target and selected space");
            foreach (string id in _sensors)
            {
                bool found = false;
                foreach (EntityDef entity in world.Content.All<EntityDef>()) foreach (ComponentDef part in entity.Components)
                    if (part is ISpatialSensorDefinition sensor && sensor.CapabilityId.Id == id && sensor.Space == TargetSpace &&
                        typeof(ISpatialSensor).IsAssignableFrom(part.ComponentType)) found = true;
                if (!found) throw new ContentException(Id + ": no declared compatible sensor: " + id);
            }
        }
        private IEnumerable<(ISpatialSensor Sensor, SensorCoverage Coverage, EntityId Owner, Code Capability)> Current(World world, PlayerId company)
        {
            if (!world.Players.IsSeat(company)) yield break;
            foreach (Entity entity in world.Entities.All)
                if (entity.Alive && entity.Owner == company && ReferenceEquals(world.Entities.Get(entity.Id), entity))
                    foreach (Component part in entity.Components)
                        if (part is ISpatialSensor sensor)
                        {
                            // The queried target can be another entity. Observe the sensor's actual whole
                            // owner as well, so a provider cannot mutate a sibling's private saved state.
                            var current = EntityObservation.ReadProvider(world, part, entity, () => {
                                Code id = sensor.CapabilityId;
                                if (Array.BinarySearch(_sensors, id.Id, StringComparer.Ordinal) < 0) return (Id: Code.None, Coverage: (SensorCoverage)null);
                                if (!(part.Def is ISpatialSensorDefinition declared) || declared.CapabilityId != id || declared.Space != TargetSpace)
                                    throw new InvalidOperationException("sensor runtime/definition space or identity mismatch");
                                SensorCoverage value = sensor.ReadCoverage(world);
                                if (value != null && value.Space != declared.Space) throw new InvalidOperationException("sensor returned foreign coverage");
                                return (Id: id, Coverage: value);
                            }, space: world.Space, observeTargetState: true);
                            if (current.Coverage == null) continue;
                            yield return (sensor, current.Coverage, entity.Id, current.Id);
                        }
        }
        public bool Allows(World world, PlayerId company)
        { foreach (var current in Current(world, company)) return true; return false; }
        protected bool Covers(World world, PlayerId company, SpatialSnapshot sample)
        {
            if (sample == null || sample.Space != TargetSpace) return false;
            foreach (var current in Current(world, company))
            {
                bool inside = EntityObservation.ReadProvider(world, current.Sensor, null,
                    () => current.Sensor.CanObserve(world, sample), space: world.Space, observeTargetState: true);
                if (inside) return true;
            }
            return false;
        }
        public void CopyObservationKeys(World world, PlayerId company, IList<string> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear(); var keys = new List<string>();
            foreach (var current in Current(world, company))
            {
                string id = current.Capability.Id;
                keys.Add(current.Owner.Value.ToString(CultureInfo.InvariantCulture) + ":" +
                    id.Length.ToString(CultureInfo.InvariantCulture) + ":" + id + current.Coverage.Key);
            }
            keys.Sort(StringComparer.Ordinal); foreach (string key in keys) destination.Add(key);
        }
    }
    public sealed class LayerSensorVisibilityDef : SensorVisibilityDef<ISpatialLayerDefinition>, IVisibilitySampleTarget
    {
        protected override string TargetSpace => Target.Space.Id;
        public bool AllowsSample(World world, PlayerId company, SpatialSnapshot sample) => Covers(world, company, sample);
    }
}

