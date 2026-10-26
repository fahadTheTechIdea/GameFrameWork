using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Authored regional visibility over the existing knowledge ledger and region contracts.</summary>
    public sealed class LayerRegionKnowledgeVisibilityDef : Def, IVisibilitySampleTarget
    {
        private sealed class Entry
        {
            public string RegionId;
            public string[] FactIds;
            public bool All;
            public ISpatialRegionDefinition Region;
            public readonly List<IKnowledgeSubjectDefinition> Facts = new List<IKnowledgeSubjectDefinition>();
            public bool Known(World world, PlayerId company)
                => KnowledgeVisibilityContracts.Known(world, company, Facts, All);
        }
        private string _layer;
        private readonly List<Entry> _entries = new List<Entry>();
        public ISpatialLayerDefinition Target { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _layer = reader.String("definition");
            JsonValue regions = reader.Array("regions") ?? throw reader.Error("explicit visibility regions required");
            if (regions.Count == 0) throw reader.Error("visibility regions cannot be empty");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < regions.Count; i++)
            {
                var item = new JsonReader(regions[i], reader.Where + ".regions[" + i + "]");
                var entry = new Entry { RegionId = item.String("region") };
                if (!ids.Add(entry.RegionId)) throw item.Error("duplicate visibility region");
                string mode = item.String("mode");
                if (mode != "all" && mode != "any") throw item.Error("visibility mode must be all or any");
                entry.All = mode == "all";
                if (!item.Has("facts")) throw item.Error("explicit visibility facts required");
                entry.FactIds = item.Strings("facts");
                if (entry.FactIds.Length == 0) throw item.Error("visibility facts cannot be empty");
                Array.Sort(entry.FactIds, StringComparer.Ordinal);
                for (int f = 1; f < entry.FactIds.Length; f++)
                    if (entry.FactIds[f] == entry.FactIds[f - 1]) throw item.Error("duplicate visibility fact");
                item.End(); _entries.Add(entry);
            }
            _entries.Sort((a, b) => string.CompareOrdinal(a.RegionId, b.RegionId));
        }
        protected override void ResolveCore(ContentResolver content)
        {
            Target = content.Def<ISpatialLayerDefinition>(_layer);
            foreach (Entry entry in _entries)
            {
                entry.Region = content.Def<ISpatialRegionDefinition>(entry.RegionId);
                foreach (string id in entry.FactIds) entry.Facts.Add(content.Def<IKnowledgeSubjectDefinition>(id));
                entry.FactIds = null;
            }
        }
        protected override void ValidateResolvedCore(ContentResolver content)
        {
            foreach (Entry entry in _entries)
                if (entry.Region.Space != Target.Space.Id) throw content.Fail("visibility region belongs to another space");
        }
        public IReadOnlyList<IContentDefinition> Targets => KnowledgeVisibilityContracts.SingleTarget(Target);
        public void ValidateBinding(World world)
        {
            if (!world.Services.Has<ICompanyKnowledge>() || !world.Content.Owns(Target) || world.Space == null || Target.Space.Id != world.Space.Id)
                throw new ContentException(Id + ": regional visibility requires actual knowledge/content/space owners");
            foreach (Entry entry in _entries)
                SpatialRegionContracts.ValidateAlias(world, entry.Region);
        }
        public bool Allows(World world, PlayerId company)
        {
            foreach (Entry entry in _entries) if (entry.Known(world, company)) return true;
            return false;
        }
        public bool AllowsSample(World world, PlayerId company, SpatialSnapshot sample)
        {
            foreach (Entry entry in _entries)
                if (entry.Known(world, company))
                {
                    SpatialRegionStatus status = EntityObservation.ReadProvider(world, entry.Region, null,
                        () => entry.Region.Check(sample), space: world.Space);
                    if (status == SpatialRegionStatus.Inside) return true;
                }
            return false;
        }
        public void CopyObservationKeys(World world, PlayerId company, IList<string> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            foreach (Entry entry in _entries) if (entry.Known(world, company)) destination.Add(entry.Region.Id);
        }
    }
}
