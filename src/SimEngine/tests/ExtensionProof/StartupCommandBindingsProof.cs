using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.ExtensionProof
{
    // Authored startup declarations and negative registration instrumentation; no positive gameplay.
    public sealed class StartupCommandBindingDef : Def
    {
        public string Mode { get; private set; }
        public string Resource { get; private set; }
        public bool Aliases { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Mode = reader.String("mode"); Resource = reader.String("resource"); Aliases = reader.Bool("aliases");
            if (string.IsNullOrWhiteSpace(Resource)) throw reader.Error("explicit resource binding required");
            if (Mode != "valid" && Mode != "missing-provider" && Mode != "prepare-refuse" && Mode != "validate-refuse"
                && Mode != "late-command" && Mode != "late-service" && Mode != "late-event" && Mode != "late-system"
                && Mode != "command-type" && Mode != "command-id" && Mode != "service-contract" && Mode != "persistent-id"
                && Mode != "system-order" && Mode != "system-id" && Mode != "generation-order" && Mode != "generation-id"
                && Mode != "event-type" && Mode != "event-id" && Mode != "event-provider" && Mode != "event-schema"
                && Mode != "view-contract" && Mode != "view-id" && Mode != "view-provider" && Mode != "view-schema"
                && Mode != "service-provider" && Mode != "service-native-owner" && Mode != "service-codec-schema"
                && Mode != "system-native-id" && Mode != "system-native-order" && Mode != "system-schedule"
                && Mode != "system-codec-schema" && Mode != "view-native-contract"
                && Mode != "event-handler" && Mode != "event-subscriber-provider")
                throw reader.Error("unknown explicit composition proof mode");
        }
    }
    public sealed class StartupBindingCommand : Command { public override void Sync(IStateSync sync) { } }
    public sealed class StartupOtherBindingCommand : Command { public override void Sync(IStateSync sync) { } }

    public sealed class StartupCommandBindingsModule : GameModule
    {
        public override string Id => "proof.startup-command-bindings";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(InventoryModule.ModuleId, Version) });
        public readonly InitializeProbe Initialize = new InitializeProbe("startup.commands.initialize");
        public int Prepared, Validated, Generated, CommandCalls;
        private readonly List<string> _trace = new List<string>();
        public IReadOnlyList<string> Trace => _trace.AsReadOnly();
        public override void DeclareContent(ContentTypes types) => types.Section<StartupCommandBindingDef>("startup_command_bindings");
        public override void Compose(WorldComposer composer)
        {
            var declaration = composer.Content.Get<StartupCommandBindingDef>("startup.commands");
            int preparedAt = Prepared, initializedAt = Initialize.Initializations;
            composer.AddSystem(Initialize, 302);
            var first = new BindingHandler<StartupBindingCommand>(this, declaration, "first", preparedAt, initializedAt);
            var second = new BindingHandler<StartupOtherBindingCommand>(this, declaration, "second", preparedAt, initializedAt);
            composer.Commands.Register("startup.commands.first", first); composer.Commands.Register("startup.commands.second", second);
            if (declaration.Aliases) { composer.Services.Bind<IProbeFirst>(first); composer.Services.Bind<IProbeSecond>(first); }
            composer.AddWorldGen(new AdmissionStage(this, "startup.commands.generate"), 111);
            switch (declaration.Mode)
            {
                case "command-type": composer.Commands.Register("startup.commands.another", first); break;
                case "command-id": composer.Commands.Register("startup.commands.first", second); break;
                case "service-contract": composer.Services.Bind<IProbeFirst>(first); composer.Services.Bind<IProbeFirst>(second); break;
                case "persistent-id": composer.Services.Bind<IProbeFirst>(new ProbeState("startup.duplicate")); composer.Services.Bind<IProbeSecond>(new ProbeState("startup.duplicate")); break;
                case "system-order": composer.AddSystem(new InitializeProbe("startup.other"), 302); break;
                case "system-id": composer.AddSystem(new InitializeProbe(Initialize.Id), 303); break;
                case "generation-order": composer.AddWorldGen(new AdmissionStage(this, "startup.other"), 111); break;
                case "generation-id": composer.AddWorldGen(new AdmissionStage(this, "startup.commands.generate"), 112); break;
                case "event-type": composer.Events.Register<PulseEvent>("startup.pulse", 1); composer.Events.Register<PulseEvent>("startup.other", 1); break;
                case "event-id": composer.Events.Register<PulseEvent>("startup.pulse", 1); composer.Events.Register<EchoEvent>("startup.pulse", 1); break;
                case "event-provider": composer.Events.Require<PulseEvent>("startup.pulse", 1); break;
                case "event-schema": composer.Events.Register<PulseEvent>("startup.pulse", 1); composer.Events.Require<PulseEvent>("startup.pulse", 2); break;
                case "view-contract": composer.AddViewFeature<ProbeView>("startup.view", 1, (w, p) => new ProbeView(p)); composer.AddViewFeature<ProbeView>("startup.other", 1, (w, p) => new ProbeView(p)); break;
                case "view-id": composer.AddViewFeature<ProbeView>("startup.view", 1, (w, p) => new ProbeView(p)); composer.AddViewFeature<OtherProbeView>("startup.view", 1, (w, p) => new OtherProbeView()); break;
                case "view-provider": composer.RequireViewFeature<ProbeView>("startup.view", 1); break;
                case "view-schema": composer.AddViewFeature<ProbeView>("startup.view", 1, (w, p) => new ProbeView(p)); composer.RequireViewFeature<ProbeView>("startup.view", 2); break;
                case "service-provider": composer.Services.Require<IProbeSecond>(); break;
                case "service-native-owner": composer.Services.Bind<IResourceTransaction>(composer.Services.Get<IResourceTransaction>()); break;
                case "service-codec-schema": composer.Services.Bind<IProbeFirst>(new MutableCodecState { SuppliedVersion = 0 }); break;
                case "system-native-id": composer.AddSystem(new InitializeProbe("sim.work"), 303); break;
                case "system-native-order": composer.AddSystem(new InitializeProbe("startup.native-order"), WorkModule.SystemOrder); break;
                case "system-schedule": composer.AddSystem(new MutableCodecSystem { SuppliedSchedule = default }, 303); break;
                case "system-codec-schema": composer.AddSystem(new MutableCodecSystem { SuppliedVersion = 0 }, 303); break;
                case "view-native-contract": composer.AddViewFeature<TransformationManagementView>("startup.native-view", 1, (w, p) => null); break;
                case "event-handler": composer.Events.Register<PulseEvent>("startup.pulse", 1); composer.Events.Subscribe<PulseEvent>("startup.handler", _ => throw new InvalidOperationException("registration-only handler must never run")); composer.Events.Subscribe<PulseEvent>("startup.handler", _ => throw new InvalidOperationException("registration-only handler must never run")); break;
                case "event-subscriber-provider": composer.Events.Subscribe<PulseEvent>("startup.handler", _ => throw new InvalidOperationException("registration-only handler must never run")); break;
            }
        }
        private sealed class AdmissionStage : WorldGenStage
        {
            private readonly StartupCommandBindingsModule _module;
            public AdmissionStage(StartupCommandBindingsModule module, string id) { _module = module; Id = id; }
            public override string Id { get; }
            public override void Generate(World world, ScenarioDef scenario) => _module.Generated++;
        }
        private interface IMissingStartupProvider { }
        private sealed class BindingHandler<T> : CommandHandler<T>, IProbeFirst, IProbeSecond, ICompositionPreparation, ICompositionValidator where T : Command, new()
        {
            private readonly StartupCommandBindingsModule _module;
            private readonly StartupCommandBindingDef _declaration;
            private readonly string _name;
            private readonly int _preparedAt, _initializedAt;
            public BindingHandler(StartupCommandBindingsModule module, StartupCommandBindingDef declaration, string name, int preparedAt, int initializedAt)
            { _module = module; _declaration = declaration; _name = name; _preparedAt = preparedAt; _initializedAt = initializedAt; }
            public void PrepareComposition(World world)
            {
                _module.Prepared++; _module._trace.Add("prepare." + _name);
                if (_declaration.Mode == "prepare-refuse" && _name == "first") throw new ContentException("startup.commands: preparation refusal");
            }
            public void ValidateComposition(World world)
            {
                _module.Validated++; _module._trace.Add("validate." + _name);
                if (_module.Prepared != _preparedAt + 2 || _module.Initialize.Initializations != _initializedAt)
                    throw new ContentException("all command participants must prepare before validation/initialization");
                if (!world.Services.TryGet<IResourceTransaction>(out _)) throw new ContentException("startup.commands: actual inventory provider absent");
                world.Content.ResourceId(_declaration.Resource);
                if (_declaration.Mode == "missing-provider" && !world.Services.TryGet<IMissingStartupProvider>(out _))
                    throw new ContentException("startup.commands: required provider absent");
                if (_declaration.Mode == "validate-refuse") throw new ContentException("startup.commands: validation refusal");
                if (_declaration.Mode == "late-command") world.Commands.Register("startup.late", new BindingHandler<StartupBindingCommand>(_module, _declaration, "late", _preparedAt, _initializedAt));
                if (_declaration.Mode == "late-service") world.Services.Bind<IProbeFirst>(this);
                if (_declaration.Mode == "late-event") world.Events.Register<PulseEvent>("startup.late", 1);
                if (_declaration.Mode == "late-system") world.AddSystem(new InitializeProbe("startup.late"), 900);
            }
            protected override Code Check(World world, T command) { _module.CommandCalls++; throw new InvalidOperationException("registration-only command must never run"); }
            protected override void Apply(World world, T command, CommandContext context) { _module.CommandCalls++; throw new InvalidOperationException("registration-only command must never run"); }
        }
    }
}
