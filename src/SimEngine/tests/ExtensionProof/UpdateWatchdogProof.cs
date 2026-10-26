using System;
using System.Collections.Generic;
using System.Threading;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Explicit deliberate provider fault content. Never selected by normal Planet X.</summary>
    public sealed class UpdateWatchdogProfileDef : Def
    {
        public string Behavior { get; private set; }
        public int AtStep { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Behavior = reader.String("behavior"); AtStep = reader.Int("at_step");
            if (AtStep < 0 || Behavior != "healthy" && Behavior != "throw" && Behavior != "block" && Behavior != "noisy-block")
                throw reader.Error("explicit supported watchdog fault behavior and nonnegative step required");
        }
    }

    public sealed class UpdateWatchdogProbeModule : GameModule
    {
        public override string Id => "proof.update-watchdog";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement("planet-x", Version) };
        public override void DeclareContent(ContentTypes types) => types.Section<UpdateWatchdogProfileDef>("update_watchdog_profiles");
        public override void Compose(WorldComposer composer)
        {
            var settings = composer.Scenario.SettingsFor(Id) ?? throw new ContentException("watchdog probe requires explicit profile");
            var profile = composer.Content.Get<UpdateWatchdogProfileDef>(settings.String("profile")); settings.End();
            composer.AddSystem(new ProbeSystem(profile), 900);
        }

        private sealed class ProbeSystem : GameSystem
        {
            private readonly UpdateWatchdogProfileDef _profile;
            public ProbeSystem(UpdateWatchdogProfileDef profile) { _profile = profile; }
            public override string Id => "proof.update-watchdog.execute";
            public override int StateSchemaVersion => 1;
            public override void Update(World world, TickInfo tick)
            {
                if (_profile.Behavior == "healthy" || tick.Tick < _profile.AtStep) return;
                const string marker = "PROVIDER_UPDATE_ENTER proof.update-watchdog.execute";
                Console.WriteLine(marker + " profile=" + _profile.Id + " step=" + tick.Tick); Console.Out.Flush();
                Console.Error.WriteLine(marker + " stderr"); Console.Error.Flush();
                if (_profile.Behavior == "throw") throw new InvalidOperationException("intentional_engine_update_fault");
                if (_profile.Behavior == "noisy-block")
                {
                    // No newline-oriented capture may allocate this unbounded provider line.
                    Console.Out.Write(new string('x', 65536)); Console.Error.Write(new string('e', 65536));
                    while (true) { Console.Out.Write(marker + " still-blocked "); Console.Out.Flush(); Thread.Sleep(5); }
                }
                Thread.Sleep(Timeout.Infinite); // Deliberately never returns; only external isolation can stop it.
            }
        }
    }
}

