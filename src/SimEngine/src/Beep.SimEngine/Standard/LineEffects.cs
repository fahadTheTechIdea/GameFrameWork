using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable corridor completion intent; physical admission stays in the original work owner.</summary>
    public interface ILineEffectDefinition : IPhysicalWorkEffect
    {
        LineEffectOperation Operation { get; }
        /// <summary>Actual removal line; segment/finish bind their existing cursor and have no line reference.</summary>
        ILineDefinition Line { get; }
    }

    /// <summary>The sole strict JSON reader for native and independently authored line effects.</summary>
    public sealed class LineEffectDeclaration
    {
        public LineEffectOperation Operation { get; }
        private string _line;
        public ILineDefinition Line { get; private set; }
        private LineEffectDeclaration(LineEffectOperation operation) { Operation = operation; }
        public static LineEffectDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            string operation = reader.String("operation");
            if (operation == "segment") return new LineEffectDeclaration(LineEffectOperation.Segment);
            if (operation == "finish") return new LineEffectDeclaration(LineEffectOperation.Finish);
            if (operation == "remove")
            {
                string line = reader.String("line");
                if (string.IsNullOrWhiteSpace(line)) throw reader.Error("line removal requires an explicit line");
                return new LineEffectDeclaration(LineEffectOperation.Remove) { _line = line };
            }
            throw reader.Error("line effect operation must be segment, finish or remove");
        }
        public void Resolve(ContentResolver content)
        {
            if (Operation == LineEffectOperation.Remove) Line = content.Def<ILineDefinition>(_line);
        }
    }

    /// <summary>One contract admission and dispatch path, never a second cursor or completion algorithm.</summary>
    public static class LineEffectContracts
    {
        public static IReadOnlyList<IContentDefinition> ContentReferences(ILineEffectDefinition effect)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            if (effect.Operation == LineEffectOperation.Segment || effect.Operation == LineEffectOperation.Finish)
            {
                if (effect.Line != null) throw new ContentException(effect.Id + ": cursor effect cannot declare a removal line");
                return Array.Empty<IContentDefinition>();
            }
            var line = effect.Line;
            if (effect.Operation != LineEffectOperation.Remove || line?.Layer?.Space == null)
                throw new ContentException(effect.Id + ": removal dependencies require actual resolved line/layer/space");
            var references = new List<IContentDefinition> { line, line.Layer, line.Layer.Space };
            if (line.Owned)
            {
                if (line.OwnerLayer == null) throw new ContentException(effect.Id + ": owned removal requires its actual owner layer");
                references.Add(line.OwnerLayer);
            }
            return references.AsReadOnly();
        }
        public static LineEffectOperation ValidateMetadata(ILineEffectDefinition effect, ContentResolver content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (!(effect is Def actual) || string.IsNullOrWhiteSpace(actual.Id) || string.IsNullOrWhiteSpace(actual.Module) ||
                effect.Id != actual.Id || effect.Module != actual.Module || !content.Content.Owns(effect) ||
                !ReferenceEquals(content.Content.Find<ILineEffectDefinition>(actual.Id), effect) ||
                !ReferenceEquals(content.Content.Find<IWorkEffect>(actual.Id), effect))
                throw content.Fail("line effect requires its actual immutable catalog aliases");
            LineEffectOperation operation = effect.Operation;
            if (operation != LineEffectOperation.Segment && operation != LineEffectOperation.Finish && operation != LineEffectOperation.Remove)
                throw content.Fail("line effect operation must be segment, finish or remove");
            ILineDefinition line = effect.Line;
            if (operation == LineEffectOperation.Remove)
            {
                if (line == null || !content.Content.Owns(line) ||
                    !ReferenceEquals(content.Content.Find<ILineDefinition>(line.Id), line))
                    throw content.Fail("line removal requires its actual catalog line");
            }
            else if (line != null) throw content.Fail("segment and finish effects bind the existing cursor, not another line");
            return operation;
        }
        public static void RequireCapabilities(World world, ILineEffectDefinition effect)
        {
            if (ReadOperation(world, effect) == LineEffectOperation.Remove)
            {
                if (!world.Services.Has<ILineRemoval>())
                    throw new ContentException(effect.Id + ": line removal requires selected ILineRemoval geometry/execution");
            }
            else if (!world.Services.Has<ILineWorkEffects>())
                throw new ContentException(effect.Id + ": line effect requires ILineWorkEffects");
        }
        public static void ValidatePlan(World world, ILineEffectDefinition effect, IActivityDefinition plan, IActivityNodeDefinition node)
        {
            PhysicalWorkEffectContracts.ValidatePlan(world, effect, plan, node);
            LineEffectOperation operation = ReadOperation(world, effect);
            if (operation != LineEffectOperation.Remove && (operation == LineEffectOperation.Finish) != (node == null))
                throw new ContentException(plan.PlanKey + ": line segment effects require a node; finish effects require whole-order completion");
        }
        private static LineEffectOperation ReadOperation(World world, ILineEffectDefinition effect)
        => ValidateMetadata(effect, new ContentResolver(world.Content, (Def)effect));
        public static Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node)
            => world.Entities.Get(order.Target);
        public static Code CheckIntent(World world, ILineEffectDefinition effect, PlayerId company, Entity target)
        {
            if (ReadOperation(world, effect) != LineEffectOperation.Remove) return Code.None;
            return world.Services.Get<ILineRemoval>().CheckIntent(company,effect.Line,target);
        }
        public static Code CheckStart(World world, ILineEffectDefinition effect, PlayerId company, Entity target)
        {
            Code refusal = CheckIntent(world, effect, company, target);
            return !refusal.IsNone || ReadOperation(world, effect) != LineEffectOperation.Remove ? refusal :
                world.Services.Get<ILineRemoval>().Check(company,effect.Line,target);
        }
        public static Code CheckSubject(World world, ILineEffectDefinition effect, Entity target, Order order, IActivityNodeDefinition node)
        {
            LineEffectOperation operation = ReadOperation(world, effect);
            if (operation == LineEffectOperation.Segment) return world.Services.Get<ILineWorkEffects>().CheckSegment(target, order, node);
            if (operation == LineEffectOperation.Finish) return world.Services.Get<ILineWorkEffects>().CheckFinish(target, order, node);
            if (node == null ? order.NodesDone != order.Nodes.Count :
                !order.Node(node.Id).Progress.Complete(world.Time.Ticks(node.Duration))) return WorkEffectCodes.NotComplete;
            return CheckStart(world, effect, order.Owner, target);
        }
        public static Code CommitSubject(World world, ILineEffectDefinition effect, Entity target, Order order, IActivityNodeDefinition node)
        {
            LineEffectOperation operation = ReadOperation(world, effect);
            if (operation == LineEffectOperation.Segment) return world.Services.Get<ILineWorkEffects>().CompleteSegment(target, order, node);
            if (operation == LineEffectOperation.Finish) return world.Services.Get<ILineWorkEffects>().Finish(order);
            Code refusal = CheckIntent(world, effect, order.Owner, target);
            return refusal.IsNone ? world.Services.Get<ILineRemoval>().Remove(order.Owner,effect.Line,target) : refusal;
        }
    }
}
