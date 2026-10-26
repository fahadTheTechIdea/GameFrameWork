# Optional multiplayer authority and scoped replication: verification and delivery

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Optional multiplayer authority and scoped replication SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Traceable acceptance

| Check | Requirement | Observable acceptance | Runtime status |
| --- | --- | --- | --- |
| NET-A01 | NET-R01 | Forge controller IDs and native acknowledgements from a client. | not_run |
| NET-A02 | NET-R02 | Duplicate/reorder commands and reconnect after admission. | not_run |
| NET-A03 | NET-R03 | Verify that hidden/other-player data stays out of replicated views. | not_run |
| NET-A04 | NET-R02 | Run through the same GameBase/session boundaries; concrete owner/genre names do not enter generic Core. | not_run |
| NET-A05 | NET-R20 | Reject unknown fields, bad types, unresolved references and invalid semantic combinations before launch. | not_run |
| NET-A06 | NET-R21 | Round-trip supported pending/terminal checkpoints and compare continued state/output hashes. | not_run |
| NET-A07 | NET-R22 | Inject failure/cancellation/duplicates; observe preserved state, original diagnostic cause and truthful UI/audit outcomes. | not_run |
| NET-A08 | NET-R23 | Execute the selected real native/headless consumer and record environment, commands, traces and remaining limits. | not_run |


Requirement/check links identify the primary case, not exclusive coverage. Exercise every additional invariant in chapter 01 through the relevant command/transition cases; numeric test count alone does not establish completeness.

## Complete implementation slices

1. Implement and register required capabilities listed in the package index, preserving their existing owners and failure contracts. An unavailable dependency keeps the new profile unavailable.
2. Add the proposed validated definition/runtime state and semantic validator for MultiplayerAuthorityOwner. Extend existing contracts/schema owners where shared behavior actually requires it; do not add unused implementations or a second policy store.
3. Wire a complete operation/query/native journey from AdmitRemoteIntent through a committed result. All side-effect work belongs to the named owner/adapter.
4. Implement checkpoint/restore/replay compatibility and the declared cancellation/duplicate behavior before offering the feature to a player/author.
5. Execute the acceptance cases in a Release build and native host where applicable. Fix failures, publish exact scoped evidence and update this package's status in place.

## Evidence classes and measurements

Structural checks validate these Markdown links, package dependencies and definition JSON Schema. They do not execute MultiplayerAuthorityOwner, prove semantic reachability or qualify performance. .NET behavior checks, real filesystem interruption/recovery checks, native Godot journeys and human usability checks each need their own recorded result. All new runtime checks remain not_run until those consumers exist.

Use existing prototype quality budgets only where applicable and explicitly selected. Definition caps are safety/design settings. Capture real workload size, runtime/editor/renderer/OS, p95/p99 rule/frame/save costs and memory where the package claims scale. Do not publish an actor/roster/network capacity from structure alone.

## Operational acceptance coverage

| Boundary | Required log/audit/notification evidence |
| --- | --- |
| AdmitRemoteIntent | Admitted/denied identity and safe reason; no claimed mutation after refusal. |
| Committed result | Correlated authoritative state/result identity; durable acknowledgement only at the promised boundary. |
| Failure/cancellation | Original cause/reference retained; safe user outcome; no duplicate rollback, refund or success substitution. |
| Save/restore/replay | Selected lineage/content/profile and validated checkpoint; corrupt/incompatible bytes preserved. |
| Native UI/headless | UI delivery is correctly session-scoped when present; mark UI not applicable for a purely headless consumer, without removing logs/audit. |

## Review and release limits

Self-review covers the proposed design only. No new package, SDK, Godot project or runtime behavior is installed by writing these documents. Use the project's shared release checklist for actual selected distribution features, asset/code provenance and accessibility evidence; do not invent operator/legal facts or add unrelated online features. Keep current source pins unchanged until a justified compatible change is separately implemented and tested.

A completed package implementation requires its consumer and all applicable gates. Missing native/provider/runtime evidence remains not_run; a passing document checker cannot mark this package implemented.
