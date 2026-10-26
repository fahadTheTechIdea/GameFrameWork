using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    // Authoring callback instrumentation only; no game loop, scheduler or session state.
    public sealed class LifecycleProbeDef : Def
    {
        public int Reads, Resolves, Validations;
        public Action OnRead, OnResolve, OnValidate;
        public int Value { get; private set; }
        protected override void ReadCore(JsonReader reader)
        { Reads++; Value = reader.Int("value"); OnRead?.Invoke(); }
        protected override void ResolveCore(ContentResolver content)
        { Resolves++; OnResolve?.Invoke(); }
        protected override void ValidateResolvedCore(ContentResolver content)
        { Validations++; OnValidate?.Invoke(); }
    }
    public sealed class LifecycleProbeComponentDef : ComponentDef<LifecycleProbePart>
    {
        public int Reads, Resolves, Validations;
        public Action OnRead, OnResolve, OnValidate;
        public int Value { get; private set; }
        protected override void ReadCore(JsonReader reader)
        { Reads++; Value = reader.Int("value"); OnRead?.Invoke(); }
        protected override void ResolveCore(ContentResolver content)
        { Resolves++; OnResolve?.Invoke(); }
        protected override void ValidateCore(EntityDef owner)
        { Validations++; OnValidate?.Invoke(); }
        public static LifecycleProbeComponentDef ReadProbe()
        {
            var part = new LifecycleProbeComponentDef();
            var reader = new JsonReader(Json.Parse("{\"value\":23}"), "negative lifecycle probe");
            part.Read(reader); reader.End(); return part;
        }
    }
    public sealed class LifecycleProbePart : Component<LifecycleProbeComponentDef> { }
}
