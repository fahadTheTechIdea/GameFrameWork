using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent authored metadata binds the SAME Worker and its four-field codec.
    public sealed class IndependentWorkerDef : ComponentDef<Worker>, IStatefulWorkerDefinition
    {
        private WorkerDeclaration _declaration;
        private string _fault;
        internal Action<string> Observe;
        public Fix WorkRate { get { Observe?.Invoke("rate"); return _fault == "rate" ? Fix.Zero : _declaration.WorkRate; } }
        public IReadOnlyList<WorkerRoleId> Roles { get { Observe?.Invoke("roles"); return _fault == "roles" ? null : _declaration.Roles; } }
        public EntityStateBindings States { get { Observe?.Invoke("states"); return _fault == "states" ? null : _declaration.States; } }
        protected override void ReadCore(JsonReader reader)
        { _declaration = WorkerDeclaration.Read(reader); _fault = reader.String("proof_fault", "none"); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public enum WorkerBindingFault { Exception, HiddenState, Touch, PauseClock, AllocateIdentity }
    public sealed class WorkerBindingModule : GameModule
    {
        private World _world;
        public bool NativeBinding { get; private set; }
        public int Reads { get; private set; }
        public override string Id => "proof.worker-bindings";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Component<IndependentWorkerDef>("independent_worker", 1);
        public override void Compose(WorldComposer composer)
        {
            _world = composer.World;
            var definition = composer.Content.Get<EntityDef>("machine").Component<IWorkerDefinition>();
            var states = (definition as IStatefulWorkerDefinition)?.States;
            NativeBinding = definition is IndependentWorkerDef && definition.ComponentType == typeof(Worker)
                && states != null && ReferenceEquals(states["working"], composer.Content.Get<IEntityStateDefinition>("worker.productive"));
        }
        public void Arm(string point, WorkerBindingFault fault)
        {
            foreach (var entity in _world.Entities.All)
                if (entity.Def.Id == "machine")
                {
                    var worker = entity.Get<IWorker>();
                    ((IndependentWorkerDef)entity.Def.Component<IWorkerDefinition>()).Observe = name => {
                        if (name != point || Reads != 0) return; Reads++;
                        if (fault == WorkerBindingFault.Exception) throw new InvalidOperationException("proof.worker.observation");
                        if (fault == WorkerBindingFault.Touch) entity.Touch();
                        if (fault == WorkerBindingFault.PauseClock) ((GameClock)_world.Clock).Pause();
                        if (fault == WorkerBindingFault.AllocateIdentity) _world.Spawn(_world.Content.Get<EntityDef>("source"), new PlayerId(0));
                        if (fault == WorkerBindingFault.HiddenState)
                        {
                            // Deliberately change an original codec field without Touch. No mirror store.
                            using (var stream = new MemoryStream())
                            {
                                var writer = new StateWriter(stream);
                                int job = worker.Job; EntityId home = worker.Home; bool held = worker.Held, arrived = !worker.HeldArrived;
                                writer.Int(ref job); writer.Entity(ref home); writer.Bool(ref held); writer.Bool(ref arrived);
                                var bytes = stream.ToArray(); ((Component)worker).Sync(new StateReader(bytes, 0, bytes.Length));
                            }
                        }
                    };
                    return;
                }
            throw new InvalidOperationException("missing worker observation probe");
        }
    }
}
