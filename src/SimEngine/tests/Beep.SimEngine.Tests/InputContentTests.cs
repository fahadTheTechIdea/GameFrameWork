using Beep.SimEngine.App;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Tests
{
    public static class InputContentTests
    {
        public static void Register() => TestRunner.Add("Input: framework maps and content validation", FromContent);

        private static void FromContent(Check c)
        {
            c.That(TestGame.Factory().InputMap().TryMap("q", out InputAction quit) && quit.Action == "app.exit",
                "no content map: the framework default");
            string bad = TestGame.Defs.Replace("\"scenarios\": [", "\"input_maps\": [ { \"id\": \"default\", \"bindings\": { \"k\": \":oops\" } } ], \"scenarios\": [");
            c.Throws<ContentException>(() => TestGame.Content(bad).Resolve(new[] { "test" }), "a malformed binding fails the content load");
            string empty = TestGame.Defs.Replace("\"scenarios\": [", "\"input_maps\": [ { \"id\": \"default\", \"bindings\": {} } ], \"scenarios\": [");
            c.Throws<ContentException>(() => TestGame.Content(empty).Resolve(new[] { "test" }), "a map with no bindings fails the load");
        }
    }
}
