using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Private design records belong to the existing board, not a separate catalog or execution model.
    public sealed partial class WorkBoard
    {
        private sealed class StoredPlanTemplate
        {
            internal long Id, Revision;
            internal PlayerId Company;
            internal string Name;
            internal PlanTemplate Template;
            internal StoredPlanTemplateView View() => new StoredPlanTemplateView(Id, Name, Revision, Template);
        }
        private readonly List<StoredPlanTemplate> _storedPlanTemplates = new List<StoredPlanTemplate>();
        private long _nextTemplate = 1;

        private StoredPlanTemplate OwnedTemplate(PlayerId company, long id)
        {
            foreach (StoredPlanTemplate entry in _storedPlanTemplates)
                if (entry.Id == id && entry.Company == company) return entry;
            return null;
        }
        private PlanGroupCheck CheckStoredTemplateValue(PlanTemplate template)
        {
            if (template == null || template.Space != _world.Settings.Space) return new PlanGroupCheck(Codes.InvalidArgument);
            var checks = new List<PlanGroupEntryCheck>();
            foreach (PlanTemplateEntry entry in template.Entries)
                checks.Add(new PlanGroupEntryCheck(0, ValidateTemplateEntry(entry, out _)));
            return GroupResult(checks);
        }
        public PlanGroupCheck PreviewStorePlanTemplate(PlayerId company, long id, long revision, string name, PlanTemplate template)
        {
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company)) return new PlanGroupCheck(Codes.NotSeated);
            if (id < 0 || revision < 0 || id == 0 && revision != 0) return new PlanGroupCheck(Codes.InvalidArgument);
            try { ContractIdentity.ValidateId(name, "stored template name"); }
            catch (ArgumentException) { return new PlanGroupCheck(Codes.InvalidArgument); }
            StoredPlanTemplate existing = null;
            if (id != 0)
            {
                existing = OwnedTemplate(company, id); if (existing == null) return new PlanGroupCheck(Codes.NotFound);
                if (revision != existing.Revision) return new PlanGroupCheck(PlanCodes.TemplateRevisionConflict);
                if (revision == long.MaxValue) return new PlanGroupCheck(PlanCodes.IdentityRange);
            }
            else if (_nextTemplate == long.MaxValue) return new PlanGroupCheck(PlanCodes.IdentityRange);
            foreach (StoredPlanTemplate entry in _storedPlanTemplates)
                if (entry.Company == company && entry.Id != id && entry.Name == name) return new PlanGroupCheck(PlanCodes.TemplateNameConflict);
            if (template == null || template.Space != _world.Settings.Space) return new PlanGroupCheck(Codes.InvalidArgument);
            return CanDisclosePlanTemplate(company, template) ? CheckStoredTemplateValue(template) : new PlanGroupCheck(Codes.UnknownDef);
        }
        public StoredPlanTemplateView StorePlanTemplate(PlayerId company, long id, long revision, string name, PlanTemplate template)
        {
            RequirePlanCheck(PreviewStorePlanTemplate(company, id, revision, name, template).Blocker);
            StoredPlanTemplate entry = id == 0 ? new StoredPlanTemplate { Id = _nextTemplate, Company = company } : OwnedTemplate(company, id);
            entry.Name = name; entry.Revision = checked(revision + 1); entry.Template = template;
            if (id == 0) { _nextTemplate++; _storedPlanTemplates.Add(entry); }
            return entry.View();
        }
        public Code PreviewRemovePlanTemplate(PlayerId company, long id, long revision)
        {
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            if (id <= 0 || revision <= 0) return Codes.InvalidArgument;
            StoredPlanTemplate entry = OwnedTemplate(company, id);
            if (entry == null) return Codes.NotFound;
            return entry.Revision == revision ? Code.None : PlanCodes.TemplateRevisionConflict;
        }
        public void RemovePlanTemplate(PlayerId company, long id, long revision)
        {
            RequirePlanCheck(PreviewRemovePlanTemplate(company, id, revision));
            _storedPlanTemplates.Remove(OwnedTemplate(company, id));
        }
        public Code ReadStoredPlanTemplate(PlayerId company, long id, out StoredPlanTemplateView value)
        {
            value = null; _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            if (id <= 0) return Codes.InvalidArgument;
            StoredPlanTemplate entry = OwnedTemplate(company, id); if (entry == null) return Codes.NotFound;
            if (!CanDisclosePlanTemplate(company, entry.Template)) return Codes.NotFound;
            value = entry.View(); return Code.None;
        }
        public void CopyStoredPlanTemplates(PlayerId company, IList<StoredPlanTemplateView> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            _world.RequireHealthyUpdate();
            var facts = EntityObservation.ReadProvider(_world, this, null, () => {
                var rows = new List<StoredPlanTemplateView>();
                if (_world.Players.IsSeat(company)) foreach (StoredPlanTemplate entry in _storedPlanTemplates)
                    if (entry.Company == company && CanDisclosePlanTemplate(company, entry.Template)) rows.Add(entry.View());
                return rows;
            });
            _world.RequireHealthyUpdate(); destination.Clear(); foreach (var fact in facts) destination.Add(fact);
        }
        public Code ResolvePlanTemplate(PlayerId company, PlanTemplateSelection selection, out PlanTemplate template)
        {
            template = null; _world.RequireHealthyUpdate(); if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            if (selection == null) return Codes.InvalidArgument;
            if (selection.Kind == PlanTemplateSelectionKind.Snapshot)
            {
                if (!CanDisclosePlanTemplate(company, selection.Template)) return Codes.UnknownDef;
                template = selection.Template; return Code.None;
            }
            Code check = ReadStoredPlanTemplate(company, selection.Id, out StoredPlanTemplateView value);
            if (!check.IsNone) return check;
            if (selection.Revision != value.Revision) return PlanCodes.TemplateRevisionConflict;
            template = value.Template; return Code.None;
        }
        private void ValidateStoredPlanTemplates()
        {
            if (_nextTemplate < 1) throw new CorruptStateException("invalid stored template identity counter");
            long previous = 0; var names = new HashSet<(PlayerId, string)>();
            foreach (StoredPlanTemplate entry in _storedPlanTemplates)
            {
                if (entry.Id <= previous || entry.Id >= _nextTemplate || entry.Revision <= 0 || !_world.Players.IsSeat(entry.Company)
                    || !names.Add((entry.Company, entry.Name))) throw new CorruptStateException("invalid private template identity/revision/company/name");
                previous = entry.Id;
                try { ContractIdentity.ValidateId(entry.Name, "stored template name"); }
                catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
                if (!CheckStoredTemplateValue(entry.Template).Ready) throw new CorruptStateException("saved template differs from selected space/provider content");
            }
        }
        private void SyncStoredPlanTemplates(IStateSync sync)
        {
            if (!sync.Reading) ValidateStoredPlanTemplates();
            sync.Long(ref _nextTemplate); int count = sync.Count(_storedPlanTemplates.Count);
            if (sync.Reading) _storedPlanTemplates.Clear();
            for (int i = 0; i < count; i++)
            {
                StoredPlanTemplate entry = sync.Reading ? new StoredPlanTemplate() : _storedPlanTemplates[i];
                sync.Long(ref entry.Id); sync.Player(ref entry.Company); sync.String(ref entry.Name); sync.Long(ref entry.Revision);
                try { PlanTemplate.Sync(sync, ref entry.Template); }
                catch (Exception error) when (error is Content.ContentException || error is ArgumentException || error is FormatException)
                { throw new CorruptStateException("invalid stored template: " + error.Message); }
                if (sync.Reading) _storedPlanTemplates.Add(entry);
            }
            ValidateStoredPlanTemplates();
        }
    }
}
