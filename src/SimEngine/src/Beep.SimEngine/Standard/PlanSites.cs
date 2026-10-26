using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Pure company-private placement intent, never a reservation or execution grant.</summary>
    public interface IPlanSiteSource : IContentDefinition
    {
        void ValidateBinding(World world, PlayerId company, PlanTemplate template, string group);
        PlanSiteObservation Observe(PlayerView view, Func<IGameCondition, Code> observe);
    }
    public abstract class PlanSiteSourceDef : Def, IPlanSiteSource
    {
        public abstract void ValidateBinding(World world, PlayerId company, PlanTemplate template, string group);
        public abstract PlanSiteObservation Observe(PlayerView view, Func<IGameCondition, Code> observe);
    }
    public sealed class PlanSiteCandidate
    {
        public string Id { get; }
        public int Priority { get; }
        public JsonValue Anchor { get; }
        public Code Blocker { get; }
        public PlanSiteCandidate(string id, int priority, JsonValue anchor, Code blocker)
        {
            ContractIdentity.ValidateId(id, "plan site");
            if (priority < 0 || anchor == null || !anchor.IsObject) throw new ArgumentException("site needs explicit priority and object anchor");
            if (!blocker.IsNone) ContractIdentity.ValidateId(blocker.Id, "site policy blocker");
            Id = id; Priority = priority; Anchor = anchor.Clone(); Anchor.Freeze(); Blocker = blocker;
        }
    }
    public sealed class PlanSiteObservation
    {
        public Code Blocker { get; }
        public IReadOnlyList<PlanSiteCandidate> Sites { get; }
        public PlanSiteObservation(Code blocker, IEnumerable<PlanSiteCandidate> sites)
        {
            if (!blocker.IsNone) ContractIdentity.ValidateId(blocker.Id, "site source blocker");
            var copy = new List<PlanSiteCandidate>(sites ?? throw new ArgumentNullException(nameof(sites)));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var site in copy)
                if (site == null || !ids.Add(site.Id)) throw new ArgumentException("site source needs distinct nonnull candidates");
            if (!blocker.IsNone && copy.Count > 0) throw new ArgumentException("blocked source cannot publish candidates");
            copy.Sort((a, b) => { int priority = b.Priority.CompareTo(a.Priority); return priority != 0 ? priority : string.CompareOrdinal(a.Id, b.Id); });
            Blocker = blocker; Sites = copy.AsReadOnly();
        }
    }
    public sealed class AuthoredPlanSitesDef : PlanSiteSourceDef
    {
        private PlanSiteObservation _sites;
        private readonly Dictionary<string, string[]> _conditionIds = new Dictionary<string, string[]>(StringComparer.Ordinal);
        private readonly Dictionary<string, IReadOnlyList<IGameCondition>> _conditions = new Dictionary<string, IReadOnlyList<IGameCondition>>(StringComparer.Ordinal);
        protected override void ReadCore(JsonReader reader)
        {
            JsonValue rows = reader.Array("sites") ?? throw reader.Error("explicit sites required");
            if (rows.Count == 0) throw reader.Error("authored sites must be nonempty");
            var sites = new List<PlanSiteCandidate>();
            try
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = new JsonReader(rows[i], reader.Where + ".sites[" + i + "]");
                    string id = row.String("id");
                    if (row.Array("conditions") == null) throw row.Error("explicit site conditions array required");
                    string[] conditions = row.Strings("conditions");
                    Array.Sort(conditions, StringComparer.Ordinal);
                    for (int j = 0; j < conditions.Length; j++)
                    {
                        ContractIdentity.ValidateId(conditions[j], "site condition");
                        if (j > 0 && conditions[j] == conditions[j - 1]) throw row.Error("duplicate site condition");
                    }
                    if (_conditionIds.ContainsKey(id)) throw row.Error("duplicate site identity");
                    _conditionIds.Add(id, conditions);
                    sites.Add(new PlanSiteCandidate(id, row.Int("priority"),
                        (row.Child("anchor") ?? throw row.Error("explicit site anchor required")).Raw, Code.None));
                    row.End();
                }
                _sites = new PlanSiteObservation(Code.None, sites);
            }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
        }
        protected override void ResolveCore(ContentResolver resolver)
        {
            foreach (var site in _sites.Sites)
            {
                var conditions = new List<IGameCondition>();
                foreach (string id in _conditionIds[site.Id]) conditions.Add(resolver.Def<IGameCondition>(id));
                _conditions.Add(site.Id, conditions.AsReadOnly());
            }
            _conditionIds.Clear();
        }
        public override void ValidateBinding(World world, PlayerId company, PlanTemplate template, string group)
        {
            if (!world.Players.IsSeat(company) || !world.Services.TryGet<IPlanBoard>(out var board))
                throw new ContentException(Id + ": sites require actual company and native plan board");
            foreach (var site in _sites.Sites)
            {
                foreach (var condition in _conditions[site.Id])
                {
                    GameConditionContracts.ValidateBinding(world, company, condition);
                }
                var check = board.PreviewPlanTemplateStructure(template, site.Anchor);
                if (!check.Ready) throw new ContentException(Id + "/" + site.Id + ": invalid authored site/template " + check.Blocker);
                Code admission = board.PreviewPlanTemplate(company, template, site.Anchor, group).Blocker;
                if (admission == Codes.InvalidArgument || admission == Codes.OutOfBounds || admission == Codes.UnknownDef || admission == PlanCodes.TemplateUnsupported)
                    throw new ContentException(Id + "/" + site.Id + ": invalid authored site admission " + admission);
            }
        }
        public override PlanSiteObservation Observe(PlayerView view, Func<IGameCondition, Code> observe)
        {
            if (view == null || view.IsObserver || !view.Viewer.IsValid)
                return new PlanSiteObservation(Codes.NotSeated, Array.Empty<PlanSiteCandidate>());
            if (observe == null) throw new ArgumentNullException(nameof(observe));
            var sites = new List<PlanSiteCandidate>();
            foreach (var site in _sites.Sites)
            {
                Code blocker = Code.None;
                foreach (var condition in _conditions[site.Id])
                { blocker = observe(condition); if (!blocker.IsNone) break; }
                sites.Add(new PlanSiteCandidate(site.Id, site.Priority, site.Anchor, blocker));
            }
            return new PlanSiteObservation(Code.None, sites);
        }
    }
    internal static class PlanSiteContracts
    {
        internal static PlanSiteObservation Read(World world, PlayerView view, IPlanSiteSource source)
        {
            if (source == null || !world.Content.Owns(source) || !ReferenceEquals(world.Content.Find<IPlanSiteSource>(source.Id), source))
                throw new ArgumentException("site source must be an unambiguous actual session content alias");
            if (view == null || view.IsObserver || !world.Players.IsSeat(view.Viewer))
                return new PlanSiteObservation(Codes.NotSeated, Array.Empty<PlanSiteCandidate>());
            if (!view.CanSeeContent(source)) return new PlanSiteObservation(Codes.UnknownDef, Array.Empty<PlanSiteCandidate>());
            return EntityObservation.ReadProvider(world, source, null, () =>
                source.Observe(view, condition => GameConditionContracts.Read(world, view, condition))
                ?? throw new InvalidOperationException("site source returned null observation"));
        }
    }
    public sealed class PlaybookSiteSnapshot
    {
        public PlanSiteCandidate Site { get; }
        public PlanGroupCheck Admission { get; }
        public Code Blocker => Site.Blocker.IsNone ? Admission.Blocker : Site.Blocker;
        public bool Ready => Blocker.IsNone && Admission.Ready;
        public PlaybookSiteSnapshot(PlanSiteCandidate site, PlanGroupCheck admission)
        { Site = site ?? throw new ArgumentNullException(nameof(site)); Admission = admission ?? throw new ArgumentNullException(nameof(admission)); }
    }
}

