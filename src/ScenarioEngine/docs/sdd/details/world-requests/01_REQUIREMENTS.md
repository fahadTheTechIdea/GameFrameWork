# Acknowledged world requests and transitions: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Acknowledged world requests and transitions SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Connect committed rule intent to native work with correlated readiness, failure, cancellation and retry.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. WorldRequest has IDs and generation, but no persisted generic acknowledgement/attempt lifecycle; core.change_zone is a limited global seam.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| WORLDREQ-R01 | Connect committed rule intent to native work with correlated readiness, failure, cancellation and retry. | WORLDREQ-A01 |
| WORLDREQ-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under WorldRequestOwner. | WORLDREQ-A04 |
| WORLDREQ-R03 | Dispatch is never reported as completed work. | WORLDREQ-A01 |
| WORLDREQ-R04 | Duplicate Ready cannot spend twice. | WORLDREQ-A02 |
| WORLDREQ-R05 | Region binding revisions do not invalidate unrelated regions. | WORLDREQ-A03 |
| WORLDREQ-R06 | Current Transitioning saves stay refused until a resumable checkpoint exists. | WORLDREQ-A01 |
| WORLDREQ-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | WORLDREQ-A05 |
| WORLDREQ-R21 | Restore exact committed state/time/content identity, including pending owned work. | WORLDREQ-A06 |
| WORLDREQ-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | WORLDREQ-A07 |
| WORLDREQ-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | WORLDREQ-A08 |

## Invariants and quality boundaries

- Dispatch is never reported as completed work.
- Duplicate Ready cannot spend twice.
- Region binding revisions do not invalidate unrelated regions.
- Current Transitioning saves stay refused until a resumable checkpoint exists.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
