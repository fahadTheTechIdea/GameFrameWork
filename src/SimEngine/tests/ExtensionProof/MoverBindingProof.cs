using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent metadata uses the SAME Mover and codec4, not another motion implementation.
    public sealed class IndependentMoverDef : ComponentDef<Mover>, ITileMoverDefinition
    {
        private MoverDeclaration _declaration;
        private string _fault;
        internal Action ObserveSpeed;
        public string MovementClass => _fault == "class" ? "missing.route" : _declaration.MovementClass;
        public string InitialNavigation => MovementClass;
        public IReadOnlyList<string> NavigationClasses => _fault == "classes" ? null : _declaration.NavigationClasses;
        public Fix TilesPerHour { get { ObserveSpeed?.Invoke(); return _fault == "speed" ? Fix.Zero : _declaration.TilesPerHour; } }
        public ITileSpaceDefinition SpaceDefinition => _fault == "space" ? null : _declaration.SpaceDefinition;
        public EntityStateBindings States => _fault == "states" ? null : _declaration.States;
        protected override void ReadCore(JsonReader reader)
        { _declaration = MoverDeclaration.Read(reader); _fault = reader.String("proof_fault", null); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateBinding(owner);
    }
    public sealed class UndeclaredMoverDef : ComponentDef<Mover>, IMobilityDefinition, IMotionNavigationDefinition
    {
        private MoverDeclaration _declaration;
        public string MovementClass => _declaration.MovementClass;
        public string InitialNavigation => MovementClass;
        public IReadOnlyList<string> NavigationClasses => _declaration.NavigationClasses;
        public Fix TilesPerHour => _declaration.TilesPerHour;
        protected override void ReadCore(JsonReader reader) => _declaration = MoverDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateBinding(owner);
    }
    public enum MoverBindingFault { Exception, HiddenAge, Touch, PauseClock, AllocateIdentity }
    public sealed class MoverBindingModule : GameModule
    {
        private World _world;
        public int Reads;
        public bool Independent;
        public override string Id => "proof.mover-bindings";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(TileNavigationModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        { types.Component<IndependentMoverDef>("independent_mover", 4); types.Component<UndeclaredMoverDef>("undeclared_mover", 4); }
        public override void Compose(WorldComposer composer)
        {
            _world = composer.World;
            var definition = composer.Content.Get<EntityDef>("survey.machine").Component<ITileMoverDefinition>();
            Independent = definition is IndependentMoverDef && definition.ComponentType == typeof(Mover)
                && ReferenceEquals(definition.States?["moving"], composer.Content.Get<IEntityStateDefinition>("survey.moving"));
        }
        public void Arm(MoverBindingFault fault)
        {
            foreach (var entity in _world.Entities.All)
                if (entity.Def.Id == "survey.machine")
                {
                    ((IndependentMoverDef)entity.Def.Component<ITileMoverDefinition>()).ObserveSpeed = () => {
                        if (Reads != 0) return; Reads++;
                        if (fault == MoverBindingFault.Exception) throw new InvalidOperationException("proof.mover.speed");
                        if (fault == MoverBindingFault.Touch) entity.Touch();
                        if (fault == MoverBindingFault.PauseClock) ((GameClock)_world.Clock).Pause();
                        if (fault == MoverBindingFault.AllocateIdentity) _world.Spawn(_world.Content.Get<EntityDef>("survey.site"), entity.Owner);
                        if (fault == MoverBindingFault.HiddenAge)
                        {
                            // One original codec field changed without Touch; no mirror progress store.
                            using (var stream = new MemoryStream())
                            {
                                var mover = entity.Get<Mover>(); var writer = new StateWriter(stream); mover.Sync(writer);
                                byte[] bytes = stream.ToArray();
                                Array.Copy(BitConverter.GetBytes(checked(mover.TicksMoving + 1)), 0, bytes, bytes.Length - 24, 8);
                                mover.Sync(new StateReader(bytes, 0, bytes.Length));
                            }
                        }
                    };
                    return;
                }
            throw new InvalidOperationException("missing original tile mover");
        }
    }
}
