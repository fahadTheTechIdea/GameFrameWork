using System;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public static partial class TransformationContracts
    {
        public static readonly Code HomeRequired = new Code("transformation.home-required");

        /// <summary>Resolve authored policy once at admission. The original queue stores the selected identity.</summary>
        public static Code ResolveOutputHome(World world, ITransformer processor, ITransformationDefinition definition,
            EntityId requested, out EntityId home)
        {
            RequireOutputHomeContext(world, processor, definition);
            home = requested;
            if (requested == EntityId.None)
            {
                if (definition?.OutputHome == TransformationOutputHome.Producer) home = processor.Entity.Id;
                else if (definition?.OutputHome == TransformationOutputHome.Required) return HomeRequired;
            }
            return CheckOutputHome(world, processor, definition, home);
        }

        /// <summary>Revalidate a retained selection without resolving a missing home as another destination.</summary>
        public static Code CheckOutputHome(World world, ITransformer processor, ITransformationDefinition definition, EntityId home)
        {
            RequireOutputHomeContext(world, processor, definition);
            if (home.Value < 0) return Codes.InvalidArgument;
            if (!home.IsValid) return definition.OutputHome == TransformationOutputHome.Required || definition.OutputHome == TransformationOutputHome.Producer ? HomeRequired : Code.None;
            if (definition.EntityOutput?.HasCapability<IWorker>() != true) return Codes.InvalidArgument;
            return WorkerHomeContracts.CheckOutputDefinition(world, definition.EntityOutput, world.Entities.Get(home), processor.Entity.Owner);
        }
        private static void RequireOutputHomeContext(World world, ITransformer processor, ITransformationDefinition definition)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.RequireHealthyUpdate(); SupplySource(world, processor);
            if (definition == null || !ReferenceEquals(world.Content.Find<ITransformationDefinition>(definition.Id), definition) || !Supports(processor, definition))
                throw new ArgumentException("output home requires the actual supported transformation");
        }
        internal static bool IsResolvedOutputHome(ITransformationDefinition definition, EntityId home)
            => home.Value >= 0 && (home.IsValid ? definition.EntityOutput?.HasCapability<IWorker>() == true :
                definition.OutputHome != TransformationOutputHome.Producer && definition.OutputHome != TransformationOutputHome.Required);

        /// <summary>After actual engine creation, commit only the original worker-owned binding through its sole owner.</summary>
        public static void BindOutputHome(World world, ITransformer processor, ITransformationDefinition definition, EntityId home, Entity result)
        {
            try
            {
                Code refusal = CheckOutputHome(world, processor, definition, home);
                if (!refusal.IsNone || result?.Alive != true || !ReferenceEquals(world.Entities.Get(result.Id), result) ||
                    !ReferenceEquals(result.Def, definition.EntityOutput) || result.Owner != processor.Entity.Owner)
                    throw new InvalidOperationException("created output/home differs from complete transformation admission: " + refusal);
                if (result.Get<IWorker>() != null) WorkerHomeContracts.BindProduced(world, result, home);
            }
            catch (Exception error) { world?.LatchFault("transformations.output-home", error); throw; }
        }
    }
}
