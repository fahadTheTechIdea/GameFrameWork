using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed class OperatingAreaSnapshot
    {
        public PlayerId Company { get; }
        public string Name { get; }
        public string Template { get; }
        public string Space { get; }
        public string ShapeJson { get; }
        public long Revision { get; }
        internal OperatingAreaSnapshot(PlayerId company, string name, string template, ISpatialRegionGeometry geometry, long revision)
        { Company = company; Name = name; Template = template; Space = geometry.Space; ShapeJson = geometry.ShapeJson; Revision = revision; }
    }
    /// <summary>Engine execution contract. Hosts use detached WorkView facts and ordinary commands.</summary>
    public interface IOperatingAreas
    {
        ISpatialRegion Resolve(PlayerId company, string name);
        OperatingAreaSnapshot Read(PlayerId company, string name);
        void Copy(PlayerId company, IList<OperatingAreaSnapshot> destination);
        Code PreviewSet(PlayerId company, string name, string template, string shapeJson, long expectedRevision);
        void Set(PlayerId company, string name, string template, string shapeJson, long expectedRevision);
        Code PreviewRemove(PlayerId company, string name, long expectedRevision);
        void Remove(PlayerId company, string name, long expectedRevision);
    }
    public static class OperatingAreaCodes
    {
        public static readonly Code Invalid = new Code("work.area-invalid"), InvalidShape = new Code("work.area-invalid-shape"),
            UnknownTemplate = new Code("work.area-unknown-template"), Incompatible = new Code("work.area-incompatible-space"),
            Stale = new Code("work.area-stale-revision"), InUse = new Code("work.area-in-use");
    }
    /// <summary>The sole company-area state owner; geometry is an immutable decoded projection.</summary>
    internal sealed class OperatingAreas : IOperatingAreas, IPersistentService
    {
        private sealed class Record
        {
            internal OperatingAreaSnapshot Snapshot;
            internal ISpatialRegionGeometry Geometry;
        }
        private readonly World _world;
        private SortedDictionary<string, Record>[] _companies;
        private long[] _revisions;
        public string Id => "sim.operating-areas";
        public int StateSchemaVersion => 1;
        internal OperatingAreas(World world)
        {
            _world = world;
            foreach (var template in world.Content.All<ISpatialRegionDefinition>()) SpatialRegionContracts.ValidateAlias(world, template);
            _companies = new SortedDictionary<string, Record>[world.Players.Count]; _revisions = new long[_companies.Length];
            for (int company = 0; company < _companies.Length; company++)
            {
                _companies[company] = new SortedDictionary<string, Record>(StringComparer.Ordinal);
                foreach (ISpatialRegionDefinition template in world.Content.All<ISpatialRegionDefinition>())
                    if (template.Space == world.Settings.Space)
                    {
                        if (!ValidName(template.Id)) throw new ContentException("operating-area prototype needs a stable engine identity");
                        ISpatialRegionGeometry geometry = Build(template, template.Id, template.ShapeJson);
                        _companies[company].Add(template.Id, Make(new PlayerId(company), template.Id, template.Id, geometry, 0));
                    }
            }
        }
        private bool ValidCompany(PlayerId company) => _world.Players.IsSeat(company) && company.Value < _companies.Length;
        private Record Find(PlayerId company, string name)
        {
            if (!ValidCompany(company) || name == null || !_companies[company.Value].TryGetValue(name, out var area)) return null;
            Validate(area); return area;
        }
        private static void Validate(Record area)
        {
            if (area.Geometry.Id != area.Snapshot.Name || area.Geometry.Space != area.Snapshot.Space || area.Geometry.ShapeJson != area.Snapshot.ShapeJson)
                throw new InvalidOperationException("operating-area provider changed immutable version geometry");
        }
        public ISpatialRegion Resolve(PlayerId company, string name) => Find(company, name)?.Geometry;
        public OperatingAreaSnapshot Read(PlayerId company, string name) => Find(company, name)?.Snapshot;
        public void Copy(PlayerId company, IList<OperatingAreaSnapshot> destination)
        {
            destination.Clear(); if (!ValidCompany(company)) return;
            foreach (Record record in _companies[company.Value].Values) { Validate(record); destination.Add(record.Snapshot); }
        }
        private static Record Make(PlayerId company, string name, string template, ISpatialRegionGeometry geometry, long revision)
            => new Record { Snapshot = new OperatingAreaSnapshot(company, name, template, geometry, revision), Geometry = geometry };
        private static bool ValidName(string name)
        { try { ContractIdentity.ValidateId(name, "operating area"); return true; } catch (ArgumentException) { return false; } }
        private ISpatialRegionGeometry Build(ISpatialRegionDefinition template, string name, string shapeJson)
        {
            if (shapeJson == null) throw new ContentException("operating area needs explicit shape JSON");
            var value = Json.Parse(shapeJson); value.Freeze(); var reader = new JsonReader(value, "operating-area." + name);
            SpatialRegionContracts.ValidateAlias(_world, template);
            var guard = EntityObservation.CaptureProvider(_world, template, "region.factory");
            ISpatialRegionGeometry geometry;
            try
            {
                geometry = template.CreateGeometry(name, reader); reader.End();
                if (geometry == null || ReferenceEquals(geometry, template) || geometry.Id != name || geometry.Space != template.Space || geometry.ShapeJson == null)
                    throw new InvalidOperationException("region factory returned contradictory geometry");
                // Normalization belongs to the provider, but output must remain explicit shape intent.
                JsonValue intent;
                try { intent = Json.Parse(geometry.ShapeJson); }
                catch (Exception error) when (error is ContentException || error is FormatException)
                { throw new InvalidOperationException("region factory returned malformed shape intent", error); }
                if (!intent.IsObject || intent.Get("id") != null || intent.Get("space") != null)
                    throw new InvalidOperationException("region factory returned invalid shape intent");
            }
            catch (Exception error) when (!(error is ContentException) && !(error is FormatException))
            { _world.LatchFault("region.factory", error); throw; }
            finally { guard.Verify(); }
            return geometry;
        }
        private Code CheckSet(PlayerId company, string name, string templateId, string shapeJson, long expectedRevision, out ISpatialRegionGeometry geometry)
        {
            geometry = null;
            if (!ValidCompany(company) || !ValidName(name) || expectedRevision < -1) return OperatingAreaCodes.Invalid;
            Record current = Find(company, name);
            if (current == null ? expectedRevision != -1 : current.Snapshot.Revision != expectedRevision) return OperatingAreaCodes.Stale;
            if (_revisions[company.Value] == long.MaxValue) return OperatingAreaCodes.Invalid;
            ISpatialRegionDefinition template = templateId == null ? null : _world.Content.Find<ISpatialRegionDefinition>(templateId);
            if (template == null) return OperatingAreaCodes.UnknownTemplate;
            if (template.Space != _world.Settings.Space) return OperatingAreaCodes.Incompatible;
            try { geometry = Build(template, name, shapeJson); }
            catch (ContentException) { return OperatingAreaCodes.InvalidShape; }
            catch (FormatException) { return OperatingAreaCodes.InvalidShape; }
            return Code.None;
        }
        public Code PreviewSet(PlayerId company, string name, string template, string shapeJson, long expectedRevision)
            => CheckSet(company, name, template, shapeJson, expectedRevision, out _);
        public void Set(PlayerId company, string name, string template, string shapeJson, long expectedRevision)
        {
            Code refusal = CheckSet(company, name, template, shapeJson, expectedRevision, out var geometry);
            if (!refusal.IsNone) throw new InvalidOperationException("area edit refused: " + refusal);
            long revision = checked(_revisions[company.Value] + 1);
            _companies[company.Value][name] = Make(company, name, template, geometry, revision); _revisions[company.Value] = revision;
            WorkBoard board = _world.Services.Get<WorkBoard>(); board.Dirty = true;
            foreach (Entity entity in _world.Entities.All)
                if (entity.Alive && entity.Owner == company && EntityObservation.Read(_world, entity, () => WorkPolicyRules.Read(entity))?.Area == name)
                    _world.System<WorkSystem>().RefreshPolicy(_world, entity);
        }
        public Code PreviewRemove(PlayerId company, string name, long expectedRevision)
        {
            if (!ValidCompany(company) || !ValidName(name) || expectedRevision < 0) return OperatingAreaCodes.Invalid;
            Record current = Find(company, name); if (current == null) return Codes.NotFound;
            if (current.Snapshot.Revision != expectedRevision) return OperatingAreaCodes.Stale;
            if (_revisions[company.Value] == long.MaxValue) return OperatingAreaCodes.Invalid;
            // A future authored entity must not initialize with a dangling initial policy reference.
            foreach (IWorkPolicyDefinition policy in _world.Content.All<IWorkPolicyDefinition>())
                if (policy.Initial.Area == name) return OperatingAreaCodes.InUse;
            foreach (Entity entity in _world.Entities.All)
                if (entity.Alive && entity.Owner == company && EntityObservation.Read(_world, entity, () => WorkPolicyRules.Read(entity))?.Area == name)
                    return OperatingAreaCodes.InUse;
            return Code.None;
        }
        public void Remove(PlayerId company, string name, long expectedRevision)
        {
            Code refusal = PreviewRemove(company, name, expectedRevision); if (!refusal.IsNone) throw new InvalidOperationException("area removal refused: " + refusal);
            _companies[company.Value].Remove(name); _revisions[company.Value] = checked(_revisions[company.Value] + 1);
            _world.Services.Get<WorkBoard>().Dirty = true;
        }
        public void Sync(IStateSync sync)
        {
            int count = sync.Count(_companies.Length); if (count != _world.Players.Count) throw new CorruptStateException("area company count differs");
            var companies = sync.Reading ? new SortedDictionary<string, Record>[count] : _companies;
            var revisions = sync.Reading ? new long[count] : _revisions;
            for (int company = 0; company < count; company++)
            {
                long sequence = sync.Reading ? 0 : revisions[company]; sync.Long(ref sequence);
                if (sequence < 0) throw new CorruptStateException("negative area revision"); revisions[company] = sequence;
                int areas = sync.Count(sync.Reading ? 0 : companies[company].Count);
                if (sync.Reading) companies[company] = new SortedDictionary<string, Record>(StringComparer.Ordinal);
                var existing = sync.Reading ? null : new List<Record>(companies[company].Values); string previous = null;
                var usedRevisions = new HashSet<long>();
                for (int i = 0; i < areas; i++)
                {
                    OperatingAreaSnapshot snapshot = sync.Reading ? null : existing[i].Snapshot;
                    if (!sync.Reading) Validate(existing[i]);
                    string name = snapshot?.Name, templateId = snapshot?.Template, shape = snapshot?.ShapeJson;
                    long revision = snapshot?.Revision ?? 0;
                    sync.String(ref name); sync.String(ref templateId); sync.String(ref shape); sync.Long(ref revision);
                    if (!ValidName(name) || previous != null && string.CompareOrdinal(previous, name) >= 0 || revision < 0 || revision > sequence || revision > 0 && !usedRevisions.Add(revision))
                        throw new CorruptStateException("noncanonical area identity or revision"); previous = name;
                    if (!sync.Reading) continue;
                    ISpatialRegionDefinition template = templateId == null ? null : _world.Content.Find<ISpatialRegionDefinition>(templateId);
                    if (template == null || template.Space != _world.Settings.Space) throw new CorruptStateException("unknown or incompatible area prototype");
                    if (revision == 0 && (name != templateId || shape != template.ShapeJson)) throw new CorruptStateException("invalid initial area identity or geometry");
                    ISpatialRegionGeometry geometry;
                    try { geometry = Build(template, name, shape); }
                    catch (Exception error) when (error is ContentException || error is FormatException || error is InvalidOperationException)
                    { throw new CorruptStateException("invalid saved area geometry: " + error.Message); }
                    if (geometry.ShapeJson != shape) throw new CorruptStateException("noncanonical saved area shape");
                    companies[company].Add(name, Make(new PlayerId(company), name, templateId, geometry, revision));
                }
            }
            if (sync.Reading)
                foreach (IWorkPolicyDefinition policy in _world.Content.All<IWorkPolicyDefinition>())
                    if (policy.Initial.Area != null && _world.Content.Get<ISpatialRegionDefinition>(policy.Initial.Area).Space == _world.Settings.Space)
                        for (int company = 0; company < count; company++)
                            if (!companies[company].ContainsKey(policy.Initial.Area)) throw new CorruptStateException("missing authored initial policy area");
            if (sync.Reading) { _companies = companies; _revisions = revisions; }
        }
    }
}
