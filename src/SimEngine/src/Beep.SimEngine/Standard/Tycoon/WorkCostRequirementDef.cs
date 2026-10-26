using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed class WorkCostRequirementDef : WorkRequirementDef, IWorkCostRequirement
    {
        public override System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        public Fix Amount { get; private set; }
        public bool Credit { get; private set; }
        public MonetaryCostCategory CostCategory { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Amount = reader.Fix("amount"); Credit = reader.Bool("credit");
            string category = reader.String("category");
            CostCategory = category == "construction" ? MonetaryCostCategory.Construction
                : category == "manufacturing" ? MonetaryCostCategory.Manufacturing
                : category == "running" ? MonetaryCostCategory.Running
                : throw reader.Error("work cost category must be construction, manufacturing or running");
            if (!Amount.IsPositive) throw reader.Error("work cost requires a positive amount");
        }
        public override WorkRequirementResult Evaluate(WorkRequirementContext context) => WorkRequirementResult.Satisfied();
    }

}
