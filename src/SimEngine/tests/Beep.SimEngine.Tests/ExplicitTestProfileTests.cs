using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Beep.SimEngine.Testing;

namespace Beep.SimEngine.Tests
{
    internal static class ExplicitTestProfileTests
    {
        internal const string Case = "Explicit validation profiles: complete exact coverage immutable metadata and strict refusal";
        internal static void Register() => TestRunner.Add(Case, Run);
        private const string Data = "{\"schema\":1,\"id\":\"profile.proof\",\"termination_timeout_ms\":5000,\"output_chars\":24000,\"cases\":[{\"name\":\"proof.short\",\"include\":true,\"timeout_ms\":60000,\"exclusion_reason\":null},{\"name\":\"proof.long (long)\",\"include\":false,\"timeout_ms\":300000,\"exclusion_reason\":\"explicit separate ownership proof\"}]}";
        private static readonly string[] Names = { "proof.short", "proof.long (long)" };
        private static void Run(Check c)
        {
            var profile = ExplicitTestProfile.Parse(Data, Names);
            c.Equal("profile.proof", profile.Id, "authored profile identity");
            c.Equal(2, profile.Entries.Count, "every known case explicitly assigned");
            c.That(profile.Entries[0].Included && !profile.Entries[1].Included, "include/exclude explicit, not long-name skipping");
            c.Equal(300000, profile.Entries[1].TimeoutMilliseconds, "long case has explicit positive deadline");
            c.Equal("explicit separate ownership proof", profile.Entries[1].ExclusionReason, "exclusion reason preserved");
            c.Throws<NotSupportedException>(() => ((IList<ExplicitTestProfile.Entry>)profile.Entries).Clear(), "profile facts immutable");
            foreach (string defect in new[] { "schema", "unknown-field", "missing-field", "empty", "missing-case", "unknown-case", "duplicate-case",
                "no-inclusion", "zero-timeout", "negative-grace", "zero-output", "included-reason", "unreasoned-exclusion", "wrong-boolean" })
            {
                var root = JsonNode.Parse(Data).AsObject(); var cases = root["cases"].AsArray();
                switch (defect)
                {
                    case "schema": root["schema"] = 2; break;
                    case "unknown-field": root["legacy"] = true; break;
                    case "missing-field": root.Remove("id"); break;
                    case "empty": cases.Clear(); break;
                    case "missing-case": cases.RemoveAt(1); break;
                    case "unknown-case": cases[0]["name"] = "not-registered"; break;
                    case "duplicate-case": cases[1]["name"] = "proof.short"; break;
                    case "no-inclusion": cases[0]["include"] = false; cases[0]["exclusion_reason"] = "none"; break;
                    case "zero-timeout": cases[0]["timeout_ms"] = 0; break;
                    case "negative-grace": root["termination_timeout_ms"] = -1; break;
                    case "zero-output": root["output_chars"] = 0; break;
                    case "included-reason": cases[0]["exclusion_reason"] = "hidden skip"; break;
                    case "unreasoned-exclusion": cases[1]["exclusion_reason"] = null; break;
                    case "wrong-boolean": cases[0]["include"] = "yes"; break;
                }
                c.Throws<ArgumentException>(() => ExplicitTestProfile.Parse(root.ToJsonString(), Names), "invalid explicit profile refuses " + defect);
            }
            c.Throws<ArgumentException>(() => ExplicitTestProfile.Parse(Data, new[] { "proof.short", "proof.short" }), "duplicate actual registry refused");
            c.Throws<ArgumentException>(() => ExplicitTestProfile.Parse(Data, new[] { "proof.short", "proof.long (long)", "new.case" }), "new unassigned registry case refused");
            c.Throws<ArgumentException>(() => ExplicitTestProfile.Parse(Data.Replace("\"schema\":1", "\"schema\":1,\"schema\":1"), Names), "duplicate JSON key refused");
            var snapshot = TestRunner.CaseNames;
            c.That(snapshot.Contains(Case), "current exact case is registered metadata");
            c.Throws<NotSupportedException>(() => ((IList<string>)snapshot).Clear(), "registry metadata detached immutable");
        }
    }
}
