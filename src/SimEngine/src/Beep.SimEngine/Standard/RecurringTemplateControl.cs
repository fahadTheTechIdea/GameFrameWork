using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Future template-cycle intent. Admitted work never reads changing settings as history.</summary>
    public sealed class RecurringTemplateSettings
    {
        public PlanTemplateSelection Selection { get; }
        public JsonValue Anchor { get; }
        public string Group { get; }
        public RecurringTemplateSettings(PlanTemplateSelection selection, JsonValue anchor, string group)
        {
            Selection = selection ?? throw new ArgumentNullException(nameof(selection));
            if (anchor == null || !anchor.IsObject) throw new ArgumentException("explicit template anchor object required");
            ContractIdentity.ValidateId(group, "recurring template group");
            Anchor = anchor.Clone(); Anchor.Freeze(); Group = group;
        }
        internal bool Matches(RecurringTemplateSettings other) => other != null && Selection.Matches(other.Selection)
            && Group == other.Group && Anchor.ToString() == other.Anchor.ToString();
        public static RecurringTemplateSettings Sync(IStateSync sync, RecurringTemplateSettings current)
        {
            bool present = current != null; sync.Bool(ref present); if (!present) return null;
            PlanTemplateSelection selection = current?.Selection; PlanTemplateSelection.Sync(sync, ref selection);
            string anchor = current?.Anchor.ToString(), group = current?.Group; sync.String(ref anchor); sync.String(ref group);
            if (!sync.Reading) return current;
            try { return new RecurringTemplateSettings(selection, Json.Parse(anchor), group); }
            catch (Exception error) when (error is ContentException || error is ArgumentException || error is FormatException)
            { throw new CorruptStateException("invalid recurring template settings: " + error.Message); }
        }
    }
    public interface IRecurringTemplateSource : IRecurringPlanSource
    {
        RecurringTemplateSettings ReadTemplateSettings();
        void ConfigureTemplate(World world, RecurringTemplateSettings settings);
    }

    public static partial class RecurringPlanContracts
    {
        public static RecurringTemplateSettings ReadTemplateSettings(World world, IRecurringPlanSource source)
        {
            Entity entity = Actual(world, source);
            bool template = entity.Def.Component<IRecurringPlanDefinition>(source.CapabilityId)?.Template != null;
            if (!(source is IRecurringTemplateSource provider))
            { if (template) throw new InvalidOperationException("template source requires future-settings contract"); return null; }
            var settings = EntityObservation.Read(world, entity, provider.ReadTemplateSettings);
            if (template != (settings != null)) throw new CorruptStateException("recurring template settings differ from selected binding kind");
            return settings;
        }
        internal static bool SameTemplateSettings(RecurringTemplateSettings a, RecurringTemplateSettings b)
            => a == null ? b == null : a.Matches(b);
        internal static Code CheckTemplatePriority(PlanTemplate template, int priority, int criticality)
        {
            long maximum = (long)priority + criticality;
            if (priority < 0 || criticality < 0 || maximum > int.MaxValue) return OperationPolicyCodes.PriorityRange;
            if (template != null) foreach (var entry in template.Entries)
                if (maximum + entry.Priority > int.MaxValue) return OperationPolicyCodes.PriorityRange;
            return Code.None;
        }
        internal static Code CheckSourcePriority(World world, IRecurringPlanSource source, int priority, int criticality)
        {
            var settings = ReadTemplateSettings(world, source); PlanTemplate template = null;
            if (settings != null) world.Services.Get<IPlanBoard>().ResolvePlanTemplate(source.Entity.Owner, settings.Selection, out template);
            // A broken future reference blocks work, not the player's ability to disable or repair its policy.
            return CheckTemplatePriority(template, priority, criticality);
        }
        internal static PlanGroupCheck CheckPlayerConfigureTemplate(World world, PlayerId player, EntityId entity, Code source, RecurringTemplateSettings settings)
        {
            Code refusal = CheckPlayerSource(world, player, entity, source, out var provider);
            // Validate the requested replacement, not disclosure of the old template: repairing private/broken intent is allowed.
            return refusal.IsNone ? CheckConfigureTemplate(world, provider, settings) : new PlanGroupCheck(refusal);
        }
        public static PlanGroupCheck CheckConfigureTemplate(World world, IRecurringPlanSource source, RecurringTemplateSettings settings)
        {
            if (source == null) return new PlanGroupCheck(RecurringPlanCodes.NotSource);
            Entity entity = Actual(world, source);
            if (ReadTemplateSettings(world, source) == null) return new PlanGroupCheck(RecurringPlanCodes.NotTemplate);
            if (settings == null) return new PlanGroupCheck(Codes.InvalidArgument);
            IPlanBoard board = world.Services.Get<IPlanBoard>();
            Code resolved = board.ResolvePlanTemplate(entity.Owner, settings.Selection, out PlanTemplate template);
            if (!resolved.IsNone) return new PlanGroupCheck(resolved);
            PlanGroupCheck check = board.PreviewPlanTemplateStructure(template, settings.Anchor); if (!check.Ready) return check;
            var schedule = ReadSchedule(world, source);
            Code priority = CheckTemplatePriority(template, schedule.Priority, EffectivePriority(world, source, schedule.Priority) - schedule.Priority);
            return priority.IsNone ? check : new PlanGroupCheck(priority, check.Entries);
        }
        public static void ConfigureTemplate(World world, IRecurringPlanSource source, RecurringTemplateSettings settings)
        {
            Code check = CheckConfigureTemplate(world, source, settings).Blocker;
            if (!check.IsNone) throw new InvalidOperationException("recurring template control refused: " + check);
            var before = ReadSchedule(world, source); Entity entity = source.Entity; int version = entity.Version;
            PlayerId owner = entity.Owner; long time = world.Clock.StepIndex; EntityId next = world.Entities.NextIdentity;
            ((IRecurringTemplateSource)source).ConfigureTemplate(world, settings);
            if (!settings.Matches(ReadTemplateSettings(world, source)) || !Same(before, ReadSchedule(world, source))
                || entity.Version == version || entity.Owner != owner || world.Clock.StepIndex != time || world.Entities.NextIdentity != next)
                throw new InvalidOperationException("template source did not commit exact future settings without changing chronology");
        }
    }
}
