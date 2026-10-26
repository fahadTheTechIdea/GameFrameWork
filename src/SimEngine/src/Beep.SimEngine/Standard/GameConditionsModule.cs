using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional pure content. No controller, state, scheduler or clock is installed.</summary>
    public class GameConditionsModule : GameModule
    {
        public const string ModuleId = "sim.conditions";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<SignalGameConditionDef>("signal_conditions");
            types.Section<CompositeGameConditionDef>("composite_conditions");
        }
        private sealed class ConditionNode : IDependencyNode
        {
            public string Id { get; }
            public IReadOnlyList<string> Dependencies { get; }
            public ConditionNode(IGameCondition condition)
            {
                Id = condition.Id;
                var dependencies = condition is IGameConditionDependencies group ? group.ConditionIds : Array.Empty<string>();
                if (dependencies == null) throw new ContentException(Id + ": explicit condition dependencies required");
                // Capture provider enumeration inside the same pure metadata interval. The
                // existing DependencyGraph remains the sole topology validator/compiler.
                var captured = new List<string>();
                foreach (string dependency in dependencies) captured.Add(dependency);
                Dependencies = captured.AsReadOnly();
            }
        }
        public override void Compose(WorldComposer composer)
        {
            var nodes = new List<ConditionNode>();
            foreach (var condition in composer.Content.All<IGameCondition>())
            {
                nodes.Add(EntityObservation.ReadProvider(composer.World, condition, null, () => {
                    if (!composer.Content.Owns(condition) || !ReferenceEquals(composer.Content.Find<IGameCondition>(condition.Id), condition))
                        throw new ContentException("game condition must be an unambiguous actual content alias");
                    return new ConditionNode(condition);
                }, faultPhase: "condition.observation"));
            }
            _ = new DependencyGraph<ConditionNode>(nodes, Id);
            composer.Services.Bind<GameConditionContracts>(new GameConditionContracts(composer.World));
            composer.AddViewFeature(Id, 2, (world, viewer) => new GameConditionsView(world, viewer));
        }
    }
}
