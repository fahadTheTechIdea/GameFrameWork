using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public class ObjectiveModule : GameModule
    {
        public const string ModuleId = "sim.objectives";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<ObjectiveDef>("objectives");
            types.Section<LogicalObjectivePredicateDef>("logical_objective_predicates");
            types.Section<TechnologyObjectivePredicateDef>("technology_objective_predicates");
            types.Section<EntityObjectivePredicateDef>("entity_objective_predicates");
            types.Section<ConditionObjectivePredicateDef>("condition_objective_predicates");
            types.Section<OutcomeObjectiveEffectDef>("outcome_objective_effects");
        }
        public override void Compose(WorldComposer composer)
        {
            JsonReader settings = composer.Scenario.SettingsFor(Id) ?? throw new ContentException("selected objectives need explicit company enrollment");
            JsonValue companies = settings.Array("companies") ?? throw settings.Error("explicit companies array required"); settings.End();
            var bindings = new SortedDictionary<int, IReadOnlyList<string>>();
            for (int i = 0; i < companies.Count; i++)
            {
                var row = new JsonReader(companies[i], settings.Where + ".companies[" + i + "]");
                int company = row.Int("company"); if (row.Array("objectives") == null) throw row.Error("explicit objectives required");
                string[] objectives = row.Strings("objectives"); row.End();
                if (company < 0 || company >= composer.Scenario.Seats || bindings.ContainsKey(company)) throw row.Error("unknown/duplicate objective company");
                bindings.Add(company, System.Array.AsReadOnly(objectives));
            }
            var objectivesSystem = new ObjectiveSystem(composer.World, bindings);
            composer.Services.Bind<IObjectives>(objectivesSystem); composer.AddSystem(objectivesSystem, 800);
            composer.AddViewFeature(Id, 2, (world, viewer) => new ObjectiveView(world, viewer));
        }
    }
}
