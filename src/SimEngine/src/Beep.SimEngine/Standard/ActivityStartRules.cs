using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional immutable start binding. Check is pure; Commit may settle state only on success.</summary>
    public interface IActivityStartRule : IContentDependencyDefinition
    {
        IAuthoredActivityDefinition Activity { get; }
        Code Check(World world, PlayerId company, Entity target);
        Code Commit(World world, PlayerId company, Entity target);
    }
    internal static class ActivityStartRules
    {
        internal static void Validate(World world)
        {
            var activities = new HashSet<string>(StringComparer.Ordinal);
            foreach (IActivityStartRule rule in world.Content.All<IActivityStartRule>())
                EntityObservation.ReadProvider(world, rule, null, () => {
                    IAuthoredActivityDefinition activity = ReadBinding(world, rule);
                    if (!activities.Add(activity.PlanKey))
                        throw new ContentException("activity start requires one actual immutable rule per activity");
                    return true;
                }, world.Space);
        }
        private static IAuthoredActivityDefinition ReadBinding(World world, IActivityStartRule rule)
        {
            IAuthoredActivityDefinition activity = rule.Activity;
            if (!(rule is Def actual) || !actual.IsFrozen || !world.Content.Owns(rule) ||
                !ReferenceEquals(world.Content.Find<IActivityStartRule>(rule.Id), rule) || activity == null ||
                !ReferenceEquals(world.Content.Find<IAuthoredActivityDefinition>(activity.Id), activity))
                throw new ContentException("activity start requires an actual immutable rule and activity");
            return activity;
        }
        private static IActivityStartRule Find(World world, IActivityDefinition activity)
        {
            world.RequireHealthyUpdate();
            IActivityStartRule selected = null;
            foreach (IActivityStartRule rule in world.Content.All<IActivityStartRule>())
                EntityObservation.ReadProvider(world, rule, null, () => {
                    if (!ReferenceEquals(ReadBinding(world, rule), activity)) return false;
                    if (selected != null) throw new ContentException("activity start has multiple rules for one activity");
                    selected = rule;
                    return true;
                }, world.Space);
            return selected;
        }
        internal static Code Check(World world, IActivityDefinition activity, PlayerId company, Entity target)
        {
            IActivityStartRule rule = Find(world, activity);
            return rule == null ? Code.None : EntityObservation.ReadProvider(world, rule, target, () => {
                Code refusal = rule.Check(world, company, target);
                if (!ReferenceEquals(ReadBinding(world, rule), activity))
                    throw new InvalidOperationException("activity start binding changed during admission");
                return refusal;
            }, world.Space, observeTargetState: true);
        }
        internal static void Commit(World world, IActivityDefinition activity, PlayerId company, Entity target)
        {
            world.RequireHealthyUpdate();
            try
            {
                Code refusal = Check(world, activity, company, target);
                if (!refusal.IsNone) throw new InvalidOperationException("activity start changed after admission: " + refusal);
                IActivityStartRule rule = Find(world, activity);
                if (rule == null) return;
                refusal = rule.Commit(world, company, target);
                world.RequireHealthyUpdate();
                if (!refusal.IsNone) throw new InvalidOperationException("activity start settlement failed: " + refusal);
                EntityObservation.ReadProvider(world, rule, null, () => {
                    if (!ReferenceEquals(ReadBinding(world, rule), activity))
                        throw new InvalidOperationException("activity start binding changed during settlement");
                    return true;
                }, world.Space);
            }
            catch (Exception error) { world.LatchFault("work.start", error); throw; }
        }
    }
}
