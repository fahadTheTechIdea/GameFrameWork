using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Unused negative admission instrumentation only: never a runtime geometry fallback.
    public sealed class DockAdmissionPresenceDef : ComponentDef<DockAdmissionPresence>, ISpatialPresenceDefinition
    {
        public string Space { get; private set; }
        protected override void ReadCore(JsonReader reader) => Space = reader.String("space");
        protected override void ResolveCore(ContentResolver content) => content.Def<ISpatialSpaceDefinition>(Space);
        public void ValidateComposition(World world, EntityDef entity)
        {
            if (!ReferenceEquals(entity.Component<ISpatialPresenceDefinition>(), this) || world.Space?.Id != Space)
                throw new ContentException(entity.Id + ": negative presence needs its actual declared space");
        }
    }
    public sealed class DockAdmissionPresence : Component<DockAdmissionPresenceDef>, ISpatialPresence
    {
        public string Space => Def.Space;
        public void BeginTick() => throw new InvalidOperationException("unused admission presence must not update");
        public SpatialSnapshot Capture() => throw new InvalidOperationException("unused admission presence must not publish");
    }
    public sealed class DockAdmissionDef : ComponentDef<DockAdmission>, IWorkDockDefinition
    {
        public IReadOnlyList<WorkDockSlot> Slots { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            JsonValue rows = reader.Array("slots") ?? throw reader.Error("explicit slots required");
            var slots = new WorkDockSlot[rows.Count];
            for (int i = 0; i < slots.Length; i++)
            {
                var row = new JsonReader(rows[i], reader.Where + ".slots[" + i + "]");
                string id = row.String("id"), kind = row.String("kind"); row.End();
                if (kind != "service" && kind != "departure" && kind != "waiting") throw row.Error("explicit slot kind required");
                slots[i] = new WorkDockSlot(new Code(id), kind == "service" ? WorkDockSlotKind.Service :
                    kind == "departure" ? WorkDockSlotKind.Departure : WorkDockSlotKind.Waiting, WorkerRoleId.None, ResourceId.None);
            }
            Slots = Array.AsReadOnly(slots);
        }
    }
    public sealed class DockAdmission : Component<DockAdmissionDef>, IWorkDock
    {
        public IReadOnlyList<WorkDockSlot> Slots => Def.Slots;
        private static Exception Unused() => new InvalidOperationException("unused negative dock must never execute geometry");
        public SpatialSnapshot CaptureSlot(World world, Code slot) => throw Unused();
        public Code CheckRequest(World world, Entity carrier, WorkDockRequest request) => throw Unused();
        public Code CheckVacancy(World world, Entity carrier, Code slot) => throw Unused();
        public bool TryEstimateCost(World world, Entity carrier, Code slot, SpatialSnapshot origin, out long cost) { cost = 0; throw Unused(); }
        public bool IsAtSlot(World world, Entity carrier, Code slot) => throw Unused();
        public bool IsApproachingSlot(World world, Entity carrier, Code slot) => throw Unused();
        public SpatialInteractionStatus Check(World world, Entity carrier, Code slot) => throw Unused();
        public SpatialInteractionStatus Approach(World world, Entity carrier, Code slot) => throw Unused();
        public Code CheckRemoval(World world) => throw Unused();
    }
    public sealed class DockSpatialAdmissionModule : GameModule
    {
        public override string Id => "proof.dock-spatial-admission";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public readonly InitializeProbe Initialize = new InitializeProbe("proof.dock.initialize");
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<UnavailableTileSpaceDef>("unavailable_tile_spaces");
            types.Component<DockAdmissionPresenceDef>("dock_admission_presence", 1);
            types.Component<DockAdmissionDef>("dock_admission", 1);
        }
        public override void Compose(WorldComposer composer) => composer.AddSystem(Initialize, 301);
    }
}
