# Godot native runtime host: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Godot native runtime host SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Run real playable scenes against the same C# game boundary using Godot-native services.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. No project.godot or native runtime exists; Counter is a headless rule demonstration only.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| HOST-R01 | Run real playable scenes against the same C# game boundary using Godot-native services. | HOST-A01 |
| HOST-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under ScenarioGameHost. | HOST-A04 |
| HOST-R03 | Godot owns physics, navigation, animation, InputMap, native timers, resources and Control UI. | HOST-A01 |
| HOST-R04 | _PhysicsProcess runs for active continuous movement; unchanged menus use signals. | HOST-A02 |
| HOST-R05 | Headless synthetic movement cannot count as native route evidence. | HOST-A03 |
| HOST-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | HOST-A05 |
| HOST-R21 | Restore exact committed state/time/content identity, including pending owned work. | HOST-A06 |
| HOST-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | HOST-A07 |
| HOST-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | HOST-A08 |

## Invariants and quality boundaries

- Godot owns physics, navigation, animation, InputMap, native timers, resources and Control UI.
- _PhysicsProcess runs for active continuous movement; unchanged menus use signals.
- Headless synthetic movement cannot count as native route evidence.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
