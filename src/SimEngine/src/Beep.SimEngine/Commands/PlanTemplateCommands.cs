using System.Text;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    public sealed class PlacePlanTemplateCommand : Command
    {
        public PlanTemplateSelection Selection;
        public JsonValue Anchor;
        public string Group;
        public override void Sync(IStateSync sync)
        {
            PlanTemplateSelection.Sync(sync, ref Selection); sync.String(ref Group);
            string text = null; if (!sync.Reading && Anchor != null) { var encoded = new StringBuilder(); Anchor.WriteCanonical(encoded); text = encoded.ToString(); }
            sync.String(ref text); if (sync.Reading) Anchor = text == null ? null : Json.Parse(text);
        }
    }
    internal sealed class PlacePlanTemplateHandler : CommandHandler<PlacePlanTemplateCommand>
    {
        protected override Code Check(World world, PlacePlanTemplateCommand command)
        {
            IPlanBoard board = world.Services.Get<IPlanBoard>();
            Code check = board.ResolvePlanTemplate(command.Player, command.Selection, out PlanTemplate template);
            return check.IsNone ? board.PreviewPlanTemplate(command.Player, template, command.Anchor, command.Group).Blocker : check;
        }
        protected override void Apply(World world, PlacePlanTemplateCommand command, CommandContext result)
        {
            IPlanBoard board = world.Services.Get<IPlanBoard>();
            Code check = board.ResolvePlanTemplate(command.Player, command.Selection, out PlanTemplate template);
            if (!check.IsNone) throw new System.InvalidOperationException("admitted template selection changed: " + check);
            var ids = board.PlacePlanTemplate(command.Player, template, command.Anchor, command.Group);
            if (ids == null || ids.Count != template.Entries.Count || ids.Count == 0 || ids[0] <= 0
                || ids[0] > long.MaxValue - (ids.Count - 1))
                throw new System.InvalidOperationException("template placement returned invalid stable plan identities");
            for (int i = 1; i < ids.Count; i++)
                if (ids[i] != ids[0] + i) throw new System.InvalidOperationException("template plan identities must follow authored contiguous allocation");
            result.Arg0 = ids[0]; result.Arg1 = ids.Count;
        }
    }
    public sealed class StorePlanTemplateCommand : Command
    {
        public long Template, ExpectedRevision;
        public string Name;
        public PlanTemplate Value;
        public override void Sync(IStateSync sync)
        { sync.Long(ref Template); sync.Long(ref ExpectedRevision); sync.String(ref Name); PlanTemplate.Sync(sync, ref Value); }
    }
    internal sealed class StorePlanTemplateHandler : CommandHandler<StorePlanTemplateCommand>
    {
        protected override Code Check(World world, StorePlanTemplateCommand command)
            => world.Services.Get<IPlanBoard>().PreviewStorePlanTemplate(command.Player, command.Template, command.ExpectedRevision, command.Name, command.Value).Blocker;
        protected override void Apply(World world, StorePlanTemplateCommand command, CommandContext result)
        {
            StoredPlanTemplateView stored = world.Services.Get<IPlanBoard>().StorePlanTemplate(command.Player, command.Template, command.ExpectedRevision, command.Name, command.Value);
            result.Arg0 = stored.Id; result.Arg1 = stored.Revision;
        }
    }
    public sealed class RemovePlanTemplateCommand : Command
    {
        public long Template, ExpectedRevision;
        public override void Sync(IStateSync sync) { sync.Long(ref Template); sync.Long(ref ExpectedRevision); }
    }
    internal sealed class RemovePlanTemplateHandler : CommandHandler<RemovePlanTemplateCommand>
    {
        protected override Code Check(World world, RemovePlanTemplateCommand command)
            => world.Services.Get<IPlanBoard>().PreviewRemovePlanTemplate(command.Player, command.Template, command.ExpectedRevision);
        protected override void Apply(World world, RemovePlanTemplateCommand command, CommandContext result)
            => world.Services.Get<IPlanBoard>().RemovePlanTemplate(command.Player, command.Template, command.ExpectedRevision);
    }
}
