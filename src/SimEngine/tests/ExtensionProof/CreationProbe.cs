using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Engine-side lifecycle fault instrumentation over actual native inventories.</summary>
    public sealed class CreationProbeDef : ComponentDef<CreationProbe>
    {
        public string Mode { get; private set; }
        public string Participant { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Mode = reader.String("mode"); Participant = reader.String("participant");
            if (Mode != "throw-create" && Mode != "throw-initialize" && Mode != "change-participant" && Mode != "observe")
                throw reader.Error("explicit creation fault mode required");
        }
        protected override void ResolveCore(ContentResolver content) { content.Def<EntityDef>(Participant); }
    }
    public sealed class CreationProbe : Component<CreationProbeDef>, IEntityInitialization, IEntityReplacementState
    {
        // Per-instance negative instrumentation only; not authored gameplay or saved progress.
        public Action BeforeCreate, BeforeInitialize, BeforeRetain;
        public override void OnCreate()
        { BeforeCreate?.Invoke(); if (Def.Mode == "throw-create") throw new InvalidOperationException("proof result OnCreate failure"); }
        public void RestoreReplacementState(World world, Component source) => BeforeRetain?.Invoke();
        public void Initialize(World world)
        {
            BeforeInitialize?.Invoke();
            if (Def.Mode == "throw-initialize") throw new InvalidOperationException("proof result initialization failure");
            if (Def.Mode != "change-participant") return;
            foreach (Entity entity in world.Entities.All)
                if (entity.Def.Id == Def.Participant && entity.Owner == Entity.Owner && entity.TryGet(out IInventory stock))
                { stock.Take(stock.Stock[0].Resource, Fix.One); return; }
            throw new InvalidOperationException("proof actual participant missing");
        }
    }
    public sealed class CreationTailProbeDef : ComponentDef<CreationTailProbe>
    {
        protected override void ReadCore(JsonReader reader) { }
    }
    /// <summary>Non-authoritative per-instance callback counts, never simulation state or a codec.</summary>
    public sealed class CreationTailProbe : Component<CreationTailProbeDef>, IEntityInitialization, IEntityReplacementState
    {
        public Action BeforeInitialize;
        public int Creates { get; private set; }
        public int Initializations { get; private set; }
        public int Retentions { get; private set; }
        public override void OnCreate() => Creates++;
        public void Initialize(World world) { BeforeInitialize?.Invoke(); Initializations++; }
        public void RestoreReplacementState(World world, Component source) => Retentions++;
    }
}
