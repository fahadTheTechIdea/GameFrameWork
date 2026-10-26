# P7-ER8 — Connected flow replacement references

## 0. Problem

NetworkService.FlowCursor owns elapsed quantized flow time and live actor/endpoints. Retiring a connected entity currently leaves that state on its old identity; AdvanceFlow starts a fresh cursor. FlowTurn, by contrast, is historical successful-service ordering and must not be rewritten.

## 1. Goals

Use the existing replacement contract to retain compatible flow timing in the existing network owner. Preserve pool stock/topology and successful-service history. Prove slow push/pull continuation through staffed upgrades, saved twins and native/independent port metadata.

## 2. Design

NetworkService implements IEntityReplacementReferenceOwner. Admission checks the actual named network/resource/direction, retained port component codec, native actor endpoint and unchanged effective transfer quantum. The existing pool remains authoritative. Commit validates actual destination ports/buffer and rewrites only cursor actor and entity-owned endpoint identities.

No new topology, flow-credit ledger, timing counter or provider registry. Sort cursors through their existing comparator. Keep FlowTurn identity/time as historical ordering positions. Normal Flow still reevaluates readiness and resets/prunes inactive, interrupted or changed-pool cursors; elapsed time is not stored resources or promised delivery.

## 3. API/state

Existing replacement owner contract; typed network replacement binding/endpoint refusals. Existing network service codec6 unchanged. Build2.565.0/save157/replay1.

## 4. Files

Networks declaration and a responsibility-specific partial of the same service; existing independent port registration; authored network-replacement JSON and focused public-player test; protocol and tracker.

## 5. Errors

Missing connection/direction, incompatible metadata, alias endpoint or changed quantum refuses before replacement. Actual commit binding mismatch faults the original boundary; no invented flow, stock or restart.

## 6. Compatibility

No legacy cursor migration, reflected fields, duplicated scheduler or host-side repair.

## 7. Verification

Public physical corridor construction then connected upgrades with slow authored flow, native/direct ports, both directions, saved twins and conservation. Typed refusal parity and immutable/private facts. Focused tests only.

## 8. Risks

Topology/gate/demand changes remain legitimate normal flow interruptions, not permission to preserve expired credit. Successful-service identities are cursors/history, not live references.

## 9. Out of scope

Independent/shared actor endpoints, arbitrary provider fault/quantum/replay/lockstep/stress breadth, other owners, Oil/business/clock/platform/host gameplay and Git publication.

## 10. Execution

Inspect sole flow state; add reference participation; author physical proof; focused verification and metadata. Implemented and scoped verified:408 new checks and45 affected slow-flow checks,453/0 across two exact cases. Final solution6.74s clean. Initial compile and older fixture admission failures are retained in [evidence](../../docs/migration/P7_NETWORK_REFERENCE_OWNERS_VERIFICATION.md). Original221 requirements/194 recorded checked/27 open unchanged; additional320. No full regression or Git publication.
