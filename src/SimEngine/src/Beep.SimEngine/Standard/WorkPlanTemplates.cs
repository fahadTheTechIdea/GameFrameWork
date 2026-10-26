using System;
using System.Collections.Generic;
using System.Globalization;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Pure authoring/expansion in the existing plan owner; no template execution store or codec.
    public sealed partial class WorkBoard
    {
        private bool _readingTemplate; // Transient callback admission only; never saved game state.
        public bool CanDisclosePlanTemplate(PlayerId viewer, PlanTemplate template)
        {
            _world.RequireHealthyUpdate();
            if (template == null || viewer != PlayerId.None && !_world.Players.IsSeat(viewer)) return false;
            foreach (PlanTemplateEntry entry in template.Entries)
                if (!CanDiscloseActivity(viewer, entry.Definition)) return false;
            return true;
        }
        private static JsonValue TemplateLocation(JsonValue location)
        {
            if (location == null || !location.IsObject) throw new ContentException("explicit provider location required");
            JsonValue copy = location.Clone(); copy.Freeze(); return copy;
        }
        private IPlanTemplateDefinition TemplateProvider(string key)
        {
            IActivityDefinition definition = ResolvePlan(key);
            if (definition == null) return null;
            if (definition.PlanKey != key) throw new InvalidOperationException("template resolver returned another identity");
            return definition as IPlanTemplateDefinition;
        }
        private T ReadTemplateProvider<T>(Func<T> read)
        {
            _world.RequireHealthyUpdate();
            if (_readingTemplate)
            {
                var error = new InvalidOperationException("template observation cannot reenter its plan owner");
                _world.LatchFault("plan.template-observation", error); throw error;
            }
            long plan = _nextPlan, template = _nextTemplate; int order = _nextOrder, job = _nextJob;
            var observation = EntityObservation.Capture(_world);
            var spatial = EntityObservation.CaptureSpace(_world, _world.Space);
            void Verify()
            {
                observation.Verify();
                if (_nextPlan != plan || _nextTemplate != template || _nextOrder != order || _nextJob != job)
                    throw new InvalidOperationException("pure template callback mutated execution identity or game-clock facts");
                spatial.Verify();
                _world.RequireHealthyUpdate();
            }
            _readingTemplate = true;
            try
            {
                T result;
                // Expected authoring refusals remain typed only if observation is still healthy.
                try { result = read(); }
                catch (ContentException) { Verify(); throw; }
                catch (OverflowException) { Verify(); throw; }
                Verify(); return result;
            }
            catch (Exception error) when (!(error is ContentException) && !(error is OverflowException))
            { _world.LatchFault("plan.template-observation", error); throw; }
            finally { _readingTemplate = false; }
        }
        private Code ValidateTemplateEntry(PlanTemplateEntry entry, out IPlanTemplateDefinition provider)
        {
            provider = TemplateProvider(entry.Definition);
            if (provider == null) return ResolvePlan(entry.Definition) == null ? Codes.UnknownDef : PlanCodes.TemplateUnsupported;
            IPlanTemplateDefinition selected = provider;
            try { return ReadTemplateProvider(() => { selected.ValidateTemplate(_world, entry.Arguments); return Code.None; }); }
            catch (ContentException) { return Codes.InvalidArgument; }
            catch (OverflowException) { return Codes.InvalidArgument; }
        }
        private void ValidateTemplates()
        {
            foreach (IPlanTemplateContent definition in _world.Content.All<IPlanTemplateContent>())
            {
                if (!_world.Content.Owns(definition) || !ReferenceEquals(_world.Content.Find<IPlanTemplateContent>(definition.Id), definition) || definition.Template == null)
                    throw new ContentException("authored template needs an actual unambiguous content alias and immutable template");
                if (definition.Template.Space != _world.Settings.Space) continue;
                foreach (PlanTemplateEntry entry in definition.Template.Entries)
                {
                    Code check = ValidateTemplateEntry(entry, out _);
                    if (!check.IsNone) throw new ContentException(definition.Id + ": template entry '" + entry.Id + "': " + check);
                }
            }
        }
        private void ValidateTemplateAnchor(PlanTemplate template, JsonValue anchor)
        {
            PlanGroupCheck check = PreviewPlanTemplateStructure(template, anchor);
            if (!check.Ready) throw new ContentException("invalid template anchor: " + check.Blocker);
        }
        public PlanGroupCheck PreviewPlanTemplateStructure(PlanTemplate template, JsonValue anchor)
        {
            _world.RequireHealthyUpdate();
            if (template == null || template.Space != _world.Settings.Space) return new PlanGroupCheck(Codes.InvalidArgument);
            JsonValue location;
            try { location = TemplateLocation(anchor); } catch (ContentException) { return new PlanGroupCheck(Codes.InvalidArgument); }
            var checks = new List<PlanGroupEntryCheck>();
            foreach (PlanTemplateEntry entry in template.Entries)
            {
                Code check = ValidateTemplateEntry(entry, out IPlanTemplateDefinition provider);
                if (check.IsNone)
                    try { ReadTemplateProvider(() => { provider.ValidateTemplateAnchor(_world, entry.Arguments, location); return true; }); }
                    catch (ContentException) { check = Codes.InvalidArgument; }
                    catch (OverflowException) { check = Codes.InvalidArgument; }
                checks.Add(new PlanGroupEntryCheck(0, check));
            }
            return GroupResult(checks);
        }
        public PlanGroupCheck CapturePlanTemplate(PlayerId company, string group, JsonValue origin, out PlanTemplate template)
        {
            template = null; _world.RequireHealthyUpdate(); Code admission = CheckGroup(company, group, out List<PlanEntry> members);
            if (!admission.IsNone) return new PlanGroupCheck(admission);
            foreach (PlanEntry member in members)
                if (!CanDisclosePlan(company, member)) return new PlanGroupCheck(Codes.NotFound);
            JsonValue location; try { location = TemplateLocation(origin); } catch (ContentException) { return new PlanGroupCheck(Codes.InvalidArgument); }
            var checks = new List<PlanGroupEntryCheck>(); var entries = new List<PlanTemplateEntry>();
            var identities = new Dictionary<long, string>();
            for (int i = 0; i < members.Count; i++) identities.Add(members[i].Id, "entry." + (i + 1).ToString(CultureInfo.InvariantCulture));
            int index = 0;
            foreach (PlanEntry member in members)
            {
                index++;
                IPlanTemplateDefinition provider = TemplateProvider(member.Definition); Code check = provider == null ? PlanCodes.TemplateUnsupported : Code.None;
                var dependencies = new List<string>();
                foreach (long prerequisite in member.Dependencies)
                    if (identities.TryGetValue(prerequisite, out string id)) dependencies.Add(id); else check = PlanCodes.ExternalDependencies;
                if (check.IsNone)
                {
                    try
                    {
                        check = ReadTemplateProvider(() =>
                        {
                            Code result = provider.CaptureTemplate(_world, company, member.Arguments, location, out JsonValue arguments);
                            ValidateTemplateOutput(result, arguments);
                            if (!result.IsNone) return result;
                            var captured = new PlanTemplateEntry("entry." + index.ToString(CultureInfo.InvariantCulture), member.Definition,
                                arguments, PlanPriority(member), member.Paused, dependencies);
                            provider.ValidateTemplate(_world, captured.Arguments); entries.Add(captured); return Code.None;
                        });
                    }
                    catch (ContentException) { check = Codes.InvalidArgument; }
                    catch (OverflowException) { check = Codes.InvalidArgument; }
                }
                checks.Add(new PlanGroupEntryCheck(member.Id, check));
            }
            PlanGroupCheck result = GroupResult(checks); if (result.Ready) template = new PlanTemplate(_world.Settings.Space, entries); return result;
        }
        private static void ValidateTemplateOutput(Code result, JsonValue arguments)
        {
            if (result.IsNone ? arguments == null || !arguments.IsObject : arguments != null)
                throw new InvalidOperationException("template provider must return object arguments only on successful admission");
        }
        private PlanGroupCheck ExpandTemplateContent(PlayerId company, PlanTemplate template, JsonValue anchor, string group, out PlanGroupIntent intent)
        {
            intent = null;
            if (template == null || template.Space != _world.Settings.Space || !ValidGroup(group)) return new PlanGroupCheck(Codes.InvalidArgument);
            if (!CanDisclosePlanTemplate(company, template)) return new PlanGroupCheck(Codes.UnknownDef);
            JsonValue location; try { location = TemplateLocation(anchor); } catch (ContentException) { return new PlanGroupCheck(Codes.InvalidArgument); }
            var checks = new List<PlanGroupEntryCheck>(); var entries = new List<PlanIntent>();
            foreach (PlanTemplateEntry entry in template.Entries)
            {
                Code check = ValidateTemplateEntry(entry, out IPlanTemplateDefinition provider);
                if (check.IsNone)
                {
                    try
                    {
                        check = ReadTemplateProvider(() =>
                        {
                            Code result = provider.PlaceTemplate(_world, company, entry.Arguments, location, out JsonValue arguments);
                            ValidateTemplateOutput(result, arguments);
                            if (result.IsNone) entries.Add(new PlanIntent(entry.Definition, arguments, group, entry.Priority, entry.Paused));
                            return result;
                        });
                    }
                    catch (ContentException) { check = Codes.InvalidArgument; }
                    catch (OverflowException) { check = Codes.InvalidArgument; }
                }
                checks.Add(new PlanGroupEntryCheck(0, check));
            }
            PlanGroupCheck expansion = GroupResult(checks); if (!expansion.Ready) return expansion;
            var positions = new Dictionary<string, int>(StringComparer.Ordinal); var links = new List<PlanGroupDependency>();
            for (int i = 0; i < template.Entries.Count; i++) positions.Add(template.Entries[i].Id, i);
            for (int i = 0; i < template.Entries.Count; i++)
                foreach (string prerequisite in template.Entries[i].Dependencies) links.Add(new PlanGroupDependency(i, positions[prerequisite]));
            intent = new PlanGroupIntent(group, entries, links); return expansion;
        }
        public PlanGroupCheck ExpandPlanTemplate(PlayerId company, PlanTemplate template, JsonValue anchor, string group, out PlanGroupIntent intent)
        {
            intent = null; _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company)) return new PlanGroupCheck(Codes.NotSeated);
            var expansion = ExpandTemplateContent(company, template, anchor, group, out var candidate);
            if (!expansion.Ready) return expansion;
            PlanGroupCheck admission = PreviewPlanGroup(company, candidate);
            _world.RequireHealthyUpdate();
            if (admission.Ready) intent = candidate;
            return admission;
        }
        public PlanGroupCheck PreviewPlanTemplate(PlayerId company, PlanTemplate template, JsonValue anchor, string group)
            => ExpandPlanTemplate(company, template, anchor, group, out _);
        public PlanGroupCheck PreviewPlanTemplateActivation(PlayerId company, PlanTemplate template, JsonValue anchor, string group)
        {
            PlanGroupCheck expansion = ExpandPlanTemplate(company, template, anchor, group, out PlanGroupIntent intent);
            if (!expansion.Ready) return expansion;
            var checks = new List<PlanGroupEntryCheck>();
            foreach (PlanIntent entry in intent.Entries)
            {
                var provider = (IPlanDefinition)ResolvePlan(entry.Definition);
                Code blocker = ReadTemplateProvider(() => PlanObservation.Activation(_world, provider, company, entry.Arguments));
                checks.Add(new PlanGroupEntryCheck(0, blocker));
            }
            return GroupResult(checks);
        }
        public IReadOnlyList<long> PlacePlanTemplate(PlayerId company, PlanTemplate template, JsonValue anchor, string group)
        {
            PlanGroupCheck check = ExpandPlanTemplate(company, template, anchor, group, out PlanGroupIntent intent); RequirePlanCheck(check.Blocker);
            return SubmitPlanGroup(company, intent);
        }
    }
}
