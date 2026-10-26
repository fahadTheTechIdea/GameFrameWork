using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public static partial class TransformationContracts
    {
        /// <summary>One actual recipe metadata closure for engine projections and content providers; never a second catalog.</summary>
        public static IReadOnlyList<IContentDefinition> ContentReferences(IResourceCatalog catalog, ITransformationDefinition definition)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var references = new List<IContentDefinition>();
            var seen = new HashSet<(Type Kind, string Id)>();
            void Add(IContentDefinition content)
            {
                if (content == null) throw new InvalidOperationException("recipe disclosure requires actual owned content");
                if (seen.Add((content.GetType(), content.Id))) references.Add(content);
            }
            Add(definition);
            foreach (Bill bill in new[] { definition.Inputs, definition.Outputs, definition.Tools, definition.ToolConsumption, definition.StartResources })
                foreach (ResourceAmount line in bill.Lines) Add(catalog.Get(line.Resource) as IContentDefinition);
            foreach (IReadOnlyList<IResourceRate> rates in new[] { definition.InputRates, definition.OutputRates, definition.ToolConsumptionRates, definition.OperatingInputs })
                foreach (IResourceRate rate in rates) Add(catalog.Get(rate.Resource.Resource) as IContentDefinition);
            if (definition.EntityOutput != null) Add(definition.EntityOutput);
            return references.AsReadOnly();
        }
        // Disclosure is a projection rule only; the original queue keeps executing unchanged.
        internal static bool CanDiscloseExecution(World world, PlayerId viewer, TransformationSnapshot snapshot)
        {
            world.RequireHealthyUpdate();
            foreach (TransformationQueueEntry entry in snapshot.Queue)
                if (!CanDisclose(world, viewer, world.Content.Find<ITransformationDefinition>(entry.Transformation))) return false;
            return true;
        }

        // One transient metadata closure, not another catalog, visibility policy or executing queue.
        internal static bool CanDisclose(World world, PlayerId viewer, ITransformationDefinition definition)
        {
            world.RequireHealthyUpdate();
            if (definition == null || !ReferenceEquals(world.Content.Find<ITransformationDefinition>(definition.Id), definition))
                return false;
            return EntityObservation.ReadProvider(world, definition, null, () =>
            {
                var view = world.ViewFor(viewer);
                foreach (IContentDefinition content in ContentReferences(world.Content, definition))
                {
                    if (content == null || !world.Content.Owns(content))
                        throw new InvalidOperationException("recipe disclosure requires actual owned content");
                    if (!view.CanSeeContent(content)) return false;
                }
                return true;
            });
        }
    }
}

