# Narrative and interaction runtime: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Narrative and interaction runtime SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Run resumable conversations and world interactions whose choices and game effects commit together.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. No dialogue cursor, offered-choice checkpoint or narrative-runtime adapter exists in the current engine.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| NARR-R01 | Run resumable conversations and world interactions whose choices and game effects commit together. | NARR-A01 |
| NARR-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under NarrativeOwner. | NARR-A04 |
| NARR-R03 | Uncommitted interpreter callbacks perform no file/network/native side effects. | NARR-A01 |
| NARR-R04 | A choice may be selected only if offered at the cited revision. | NARR-A02 |
| NARR-R05 | Float/opaque interpreter data has an explicit compatible encoding; no silent conversion into integer state. | NARR-A03 |
| NARR-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | NARR-A05 |
| NARR-R21 | Restore exact committed state/time/content identity, including pending owned work. | NARR-A06 |
| NARR-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | NARR-A07 |
| NARR-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | NARR-A08 |

## Invariants and quality boundaries

- Uncommitted interpreter callbacks perform no file/network/native side effects.
- A choice may be selected only if offered at the cited revision.
- Float/opaque interpreter data has an explicit compatible encoding; no silent conversion into integer state.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
