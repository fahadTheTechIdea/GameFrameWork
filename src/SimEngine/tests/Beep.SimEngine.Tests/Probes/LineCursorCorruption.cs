using System.Reflection;
using Beep.SimEngine.Content;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    // Privileged negative instrumentation only. Never compiled into a portable game module.
    internal static class LineCursorCorruption
    {
        internal static void Path(LineWork work, int index, JsonValue placement)
        {
            JsonValue rows = work.Path.Clone(); var replaced = JsonValue.NewArray();
            for (int i = 0; i < rows.Count; i++) replaced.Add(i == index ? placement.Clone() : rows[i]);
            typeof(LineWork).GetField("_path", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(work, replaced);
        }
        internal static void Order(LineWork work)
            => typeof(LineWork).GetField("_order", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(work, checked(work.Order + 1));
    }
}
