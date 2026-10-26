# P7-ER6 — Incident propagation replacement references

## 1. Scope and requirement

Extend the existing entity-replacement contract in the sole incident exposure system. A physical upgrade must not erase a delayed exposure by leaving it attached to a retired identity. Incident response work remains owned by the existing activity DAG; no second incident ledger or scheduler is introduced.

## 2. Design and ownership

IncidentPropagationSystem implements IEntityReplacementReferenceOwner through the existing service/system enumeration. Its existing records own emitter bindings and pending targets. Replacement checks require the same named incident capability, component key, runtime type, state codec, and actual workflow. Emitter records additionally require the same named emitter, source capability, codec and actual rule.

Commit verifies the retained runtime incident lifecycle before changing any record. Entity IDs rebind in place; expected episode, scheduled time, due time, observed source episode and next cycle remain unchanged. Pending records are resorted by their existing due-time/identity rule.

ScanAfter, ScanThrough and the round-robin cursor are ordering positions, not live reference bindings. They are not rewritten or reset. Normal due-time checks still evaluate company, incident episode/activity and authored geometry/conditions.

## 3. Contracts and persistence

Use the existing replacement owner, phased incident definition/response and propagation contracts. One typed refusal, incident.propagation.replacement-binding, reports incompatible bindings.

Build2.563.0/save157/replay1. The existing propagation system codec1 is unchanged: no additional state, registry, compatibility path or new clock. Existing pending exposure fields already represent the entire migration.

## 4. Files and content

Modify IncidentPropagationSystem, IncidentPropagationCodes and the current build protocol. Add one exact public-player test and master-listed incident-replacement.json, explicitly embedded by ExtensionProof.

The proof reuses existing native episode, propagation, work, spatial and startup owners. Native and independent emitter implementations participate through the same contracts. Its copy of the shared response sample removes optional cost annotations and the cost requirement entry; the original financial sample and authored business amounts are untouched.

## 5. Validation and failure semantics

Admission is pure through the original observation boundary. Missing incident/emitter, changed workflow or changed rule refuses before replacement. Commit that does not retain the actual episode throws into the original first-fault boundary; it does not fabricate a new episode or retry schedule. Existing active-order replacement guards remain authoritative.

## 6. Compatibility policy

No legacy facade, alias repair, host event handler, duplicate backlog or load-time substitution. Exact current binding and state contracts are required.

## 7. Focused verification

Four physical scenarios cover native/direct emitters and ordinary/already-active due targets. They save before replacement completion and again with the rebound pending exposure. Original due time, exact episode, immutable prior facts, private views and interleaved-session isolation are checked. Four refusal configurations share preview/inbox semantics.

Run only this exact case and the affected existing entity-reference replacement case. Do not execute a full regression.

## 8. Risks and remaining work

Ordering cursors may refer to retired numeric positions by design; future scans use normal ordering, not invented cursor rebinding. Active incident jobs and arbitrary response-owner migrations, removed targets, wider propagation/resource faults, replay/lockstep and stress breadth remain open. Scoped tests do not close P7.017 or the full plan.

## 9. Out of scope

No Oil, financial ownership, Tycoon, clock, rendering, platform/adapter or host gameplay changes in this slice. No Git staging, commit or push while the full goal/plan is unfinished.

## 10. Execution and status

Implemented and scoped verified: new exact122/0 and affected exact96/0, total218/0. Final solution build4.91s, no warnings/errors. Initial compile and fixture failures are retained in the evidence. Original221 requirements/194 recorded checked/27 open unchanged; additional deliverable318. See [verification](../../docs/migration/P7_INCIDENT_REFERENCE_OWNERS_VERIFICATION.md).
