using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Lifecycle counters/fault instrumentation only; no authored gameplay or persisted state.</summary>
    public sealed class StartupStageProbe : WorldGenStage, ICompositionPreparation, ICompositionValidator
    {
        private readonly InitializeProbe _initialize;
        private int _preparedAt;
        public StartupStageProbe(InitializeProbe initialize) { _initialize = initialize; }
        public override string Id => "proof.startup-stage";
        public bool Refuse;
        public bool InspectContracts;
        public StartupContentContractFacts ContentFacts { get; private set; }
        public int Prepared, Validated, Generated, EntryValidated, EntryGenerated;
        public void Compose(WorldComposer composer, bool decoderRefusal)
        {
            composer.Services.Bind(this); composer.AddWorldGen(this, 110);
            composer.AddSystem(_initialize, 300);
            if (InspectContracts) ContentFacts = new StartupContentContractFacts(composer.Content);
            if (decoderRefusal) ((PointSpace)composer.World.Space).PlacementReadFault =
                () => throw new ContentException("proof startup placement decoder refused");
        }
        public void PrepareComposition(World world) { Prepared++; _preparedAt = _initialize.Initializations; }
        public void ValidateComposition(World world)
        {
            Validated++;
            if (Prepared != Validated || _initialize.Initializations != _preparedAt)
                throw new InvalidOperationException("startup stage not prepared/validated once before Initialize");
            if (Refuse) throw new ContentException("proof startup-stage validation refusal");
        }
        public override void Generate(World world, ScenarioDef scenario) { Generated++; }
    }

    /// <summary>External native startup node. The native stage owns graph/lifecycle; this is instrumentation.</summary>
    public sealed class StartupAdmissionProbeDef : Def, IWorldSetupDefinition
    {
        private bool _refuse;
        public IReadOnlyList<string> Dependencies { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            if (reader.Raw.Get("after")?.Kind != JsonKind.Array) throw reader.Error("explicit after array required");
            Dependencies = Array.AsReadOnly(reader.Strings("after")); _refuse = reader.Bool("refuse");
        }
        public void ValidateComposition(World world)
        {
            world.Services.Get<StartupStageProbe>().EntryValidated++;
            if (_refuse) throw new ContentException(Id + ": external startup declaration refused");
        }
        public Code Check(World world, IWorldSetupContext context)
        { context.RequireEntity(Dependencies[0]); return Code.None; }
        public EntityId Generate(World world, IWorldSetupContext context)
        {
            context.RequireEntity(Dependencies[0]);
            world.Services.Get<StartupStageProbe>().EntryGenerated++;
            return EntityId.None;
        }
    }
}
