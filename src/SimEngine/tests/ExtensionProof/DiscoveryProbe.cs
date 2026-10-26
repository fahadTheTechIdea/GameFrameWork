using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    // Deliberately failing external providers: discovery must never invoke them.
    public sealed class DiscoveryReadGuardDef : Def
    {
        protected override void ReadCore(JsonReader reader) => throw reader.Error("unused provider Read invoked");
    }
    public sealed class DiscoveryResolveGuardDef : Def
    {
        protected override void ReadCore(JsonReader reader) { }
        protected override void ResolveCore(ContentResolver resolver) => throw new ContentException("unused provider Resolve invoked");
    }
    public sealed class DiscoveryProbe
    {
        public int Compositions, Initializations;
        public GameModule Module() => new ProbeModule("unused", new ModuleVersion(1, 0, 0),
            new[] { new ModuleRequirement("test", new ModuleVersion(1, 0, 0)) },
            content: types =>
            {
                types.Section<DiscoveryReadGuardDef>("unused_reads");
                types.Section<DiscoveryResolveGuardDef>("unused_resolves");
            },
            compose: composer => { Compositions++; composer.AddSystem(new InitializationCounter(this), 300); });

        private sealed class InitializationCounter : GameSystem
        {
            private readonly DiscoveryProbe _probe;
            public InitializationCounter(DiscoveryProbe probe) { _probe = probe; }
            public override string Id => "unused.initialization";
            public override int StateSchemaVersion => 1;
            public override void Initialize(World world) { _probe.Initializations++; }
            public override void Update(World world, TickInfo tick) { }
        }
    }
}
