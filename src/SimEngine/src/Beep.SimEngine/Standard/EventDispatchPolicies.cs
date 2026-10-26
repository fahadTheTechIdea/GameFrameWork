using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    /// <summary>Ordinary JSON registration of optional event safety, never an event queue or state owner.</summary>
    public sealed class EventDispatchPolicyDef : Def, IEventDispatchPolicy
    {
        private EventDispatchPolicyDeclaration _declaration;
        public long? MaximumEventsPerDispatch => _declaration.MaximumEventsPerDispatch;
        protected override void ReadCore(JsonReader reader) => _declaration = EventDispatchPolicyDeclaration.Read(reader);
    }
    /// <summary>One strict reusable authored reader, independent of registration inheritance.</summary>
    public sealed class EventDispatchPolicyDeclaration
    {
        public long? MaximumEventsPerDispatch { get; }
        private EventDispatchPolicyDeclaration(long? maximum) { MaximumEventsPerDispatch = maximum; }
        public static EventDispatchPolicyDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ContentException("explicit event dispatch policy required");
            return new EventDispatchPolicyDeclaration(reader.PositiveLimit("maximum_events_per_dispatch"));
        }
    }
    /// <summary>Optional master-selected policy; absence imposes no global event-volume limit.</summary>
    public sealed class EventDispatchPolicyModule : GameModule
    {
        public const string ModuleId = "sim.event-dispatch-policy";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types)
            => types.Section<EventDispatchPolicyDef>("event_dispatch_policies");
        public override void Compose(WorldComposer composer)
        {
            foreach (IEventDispatchPolicy loaded in composer.Content.All<IEventDispatchPolicy>())
                EventDispatchPolicyContracts.Read(composer.World, loaded);
            JsonReader settings = composer.Scenario.SettingsFor(Id)
                ?? throw new ContentException("selected event dispatch policy requires explicit profile settings");
            string id = settings.String("profile"); settings.End();
            var selected = composer.Content.Find<IEventDispatchPolicy>(id)
                ?? throw new ContentException("missing authored event dispatch profile: " + id);
            composer.Services.Bind<IEventDispatchPolicy>(selected);
            composer.AddViewFeature(ModuleId, 1, (world, viewer) => new EventDispatchPolicyView(world));
        }
    }
}
