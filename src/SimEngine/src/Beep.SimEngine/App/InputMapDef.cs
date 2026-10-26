using System.Collections.Generic;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.App
{
    /// <summary>An input map as content (section "input_maps"). Read once; <see cref="Build"/> makes the map an app uses.</summary>
    public sealed class InputMapDef : Def
    {
        private readonly List<KeyValuePair<string, string>> _bindings = new List<KeyValuePair<string, string>>();

        protected override void ReadCore(JsonReader r)
        {
            JsonValue b = r.Object("bindings") ?? throw r.Error("an input map has bindings");
            for (int i = 0; i < b.Count; i++)
            {
                string name = b.KeyAt(i);
                JsonValue v = b.ValueAt(i);
                if (v.Kind != JsonKind.String || v.Text.Length == 0 || v.Text[0] == ':') throw r.Error("binding '" + name + "' must be an action id (optionally action:argument)");
                _bindings.Add(new KeyValuePair<string, string>(name, v.Text));
            }
            if (_bindings.Count == 0) throw r.Error("an input map binds at least one input");
        }

        public InputMap Build()
        {
            var map = new InputMap();
            for (int i = 0; i < _bindings.Count; i++) map.Bind(_bindings[i].Key, _bindings[i].Value);
            return map;
        }
    }
}
