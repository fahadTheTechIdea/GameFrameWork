# Play contexts and encounter rulesets: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Play contexts and encounter rulesets SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Switch exploration, encounters and other game modes without resetting shared progress or losing resumable state.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. One layer runtime exists per session, but no shared active-context/return/result checkpoint contract exists.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| CONTEXT-R01 | Switch exploration, encounters and other game modes without resetting shared progress or losing resumable state. | CONTEXT-A01 |
| CONTEXT-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under PlayContextOwner. | CONTEXT-A04 |
| CONTEXT-R03 | Do not call GameBase.Init again for every battle. | CONTEXT-A01 |
| CONTEXT-R04 | Character/inventory identities survive context changes. | CONTEXT-A02 |
| CONTEXT-R05 | Duplicate completion cannot duplicate rewards or casualties. | CONTEXT-A03 |
| CONTEXT-R06 | No context animation callback mutates authoritative rules. | CONTEXT-A01 |
| CONTEXT-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | CONTEXT-A05 |
| CONTEXT-R21 | Restore exact committed state/time/content identity, including pending owned work. | CONTEXT-A06 |
| CONTEXT-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | CONTEXT-A07 |
| CONTEXT-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | CONTEXT-A08 |

## Invariants and quality boundaries

- Do not call GameBase.Init again for every battle.
- Character/inventory identities survive context changes.
- Duplicate completion cannot duplicate rewards or casualties.
- No context animation callback mutates authoritative rules.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
