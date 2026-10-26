using System;
using System.Collections.Generic;
using System.Globalization;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Exact fresh survey coverage through the original knowledge and visibility owners.</summary>
    public sealed class LayerFreshKnowledgeVisibilityDef : Def, IVisibilitySampleTarget
    {
        private string _layer;
        private string[] _readingIds;
        private IReadOnlyList<ISpatialKnowledgeSubjectDefinition> _readings;
        public ISpatialLayerDefinition Target { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _layer = reader.String("definition");
            if (!reader.Has("readings")) throw reader.Error("explicit spatial readings required");
            _readingIds = reader.Strings("readings");
            if (_readingIds.Length == 0) throw reader.Error("spatial readings cannot be empty");
            Array.Sort(_readingIds, StringComparer.Ordinal);
            for (int i = 0; i < _readingIds.Length; i++)
            {
                try { ContractIdentity.ValidateId(_readingIds[i], "spatial reading"); }
                catch (ArgumentException error) { throw reader.Error(error.Message); }
                if (i > 0 && _readingIds[i] == _readingIds[i - 1]) throw reader.Error("duplicate spatial reading");
            }
        }
        protected override void ResolveCore(ContentResolver content)
        {
            Target = content.Def<ISpatialLayerDefinition>(_layer);
            var readings = new List<ISpatialKnowledgeSubjectDefinition>();
            foreach (string id in _readingIds)
            {
                if (!(content.Def<IKnowledgeSubjectDefinition>(id) is ISpatialKnowledgeSubjectDefinition reading))
                    throw content.Fail("visibility reading must declare exact spatial coverage: " + id);
                readings.Add(reading);
            }
            _readings = readings.AsReadOnly(); _readingIds = null;
        }
        protected override void ValidateResolvedCore(ContentResolver content)
        {
            foreach (var reading in _readings)
                if (!ReferenceEquals(reading.Layer, Target)) throw content.Fail("visibility reading belongs to another layer: " + reading.Id);
        }
        public IReadOnlyList<IContentDefinition> Targets => KnowledgeVisibilityContracts.SingleTarget(Target);
        public void ValidateBinding(World world)
        {
            if (!world.Services.Has<IKnowledgeFreshness>() || !world.Content.Owns(Target) || world.Space?.Id != Target.Space.Id)
                throw new ContentException(Id + ": fresh visibility requires actual knowledge/content/space owners");
            foreach (var reading in _readings)
                if (!world.Content.Owns(reading)) throw new ContentException(Id + ": foreign reading");
        }
        private static KnowledgeFreshnessSnapshot Fresh(World world, PlayerId company, ISpatialKnowledgeSubjectDefinition reading)
            => world.Services.Get<IKnowledgeFreshness>().ReadFreshness(company, company, AuthoredKnowledgeProvider.ProviderId, reading.Id, out var snapshot).IsNone
                && snapshot.IsFresh ? snapshot : null;
        public bool Allows(World world, PlayerId company)
        {
            foreach (var reading in _readings) if (Fresh(world, company, reading) != null) return true;
            return false;
        }
        public bool AllowsSample(World world, PlayerId company, SpatialSnapshot sample)
        {
            foreach (var reading in _readings)
                if (Fresh(world, company, reading) != null)
                {
                    bool covers = EntityObservation.ReadProvider(world, reading, null,
                        () => reading.CoversSample(sample), space: world.Space);
                    if (covers) return true;
                }
            return false;
        }
        public void CopyObservationKeys(World world, PlayerId company, IList<string> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination)); destination.Clear();
            foreach (var reading in _readings)
            {
                var snapshot = Fresh(world, company, reading); if (snapshot == null) continue;
                string key = reading.Id.Length.ToString(CultureInfo.InvariantCulture) + ":" + reading.Id + ":"
                    + snapshot.Observation.SchemaVersion.ToString(CultureInfo.InvariantCulture) + ":"
                    + snapshot.Observation.ObservedTick.ToString(CultureInfo.InvariantCulture);
                // Existing refresh runs before clock completion. Declaring the clock dependency while
                // finite/fresh conservatively invalidates even that update's terminal expiry boundary.
                if (snapshot.FreshSteps.HasValue) key += ":clock:" + world.Clock.StepIndex.ToString(CultureInfo.InvariantCulture);
                destination.Add(key);
            }
        }
    }
}
