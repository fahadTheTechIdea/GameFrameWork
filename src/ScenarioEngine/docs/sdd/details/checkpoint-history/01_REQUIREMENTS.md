# Checkpoint storage, replay segments and receipt retention: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Checkpoint storage, replay segments and receipt retention SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Extend save/replay owners for consistent long-lived state without weakening deduplication or recovery.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. Current saves/tapes are bounded monolithic documents; replay is limited to 10,000 calls/16 MiB and receipts to 100,000 IDs.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| HISTORY-R01 | Extend save/replay owners for consistent long-lived state without weakening deduplication or recovery. | HISTORY-A01 |
| HISTORY-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under ScenarioHistoryOwner. | HISTORY-A04 |
| HISTORY-R03 | No acknowledged manifest references missing/unverified sections. | HISTORY-A01 |
| HISTORY-R04 | A discarded replay prefix is not advertised as replayable. | HISTORY-A02 |
| HISTORY-R05 | Compaction never re-enables an old command. | HISTORY-A03 |
| HISTORY-R06 | Current format-1 interpretation remains explicit and supported until a verified migration exists. | HISTORY-A01 |
| HISTORY-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | HISTORY-A05 |
| HISTORY-R21 | Restore exact committed state/time/content identity, including pending owned work. | HISTORY-A06 |
| HISTORY-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | HISTORY-A07 |
| HISTORY-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | HISTORY-A08 |

## Invariants and quality boundaries

- No acknowledged manifest references missing/unverified sections.
- A discarded replay prefix is not advertised as replayable.
- Compaction never re-enables an old command.
- Current format-1 interpretation remains explicit and supported until a verified migration exists.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
