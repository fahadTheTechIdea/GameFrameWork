# P3-NB1 — Shared live spatial observation baselines

## 0. Problem and specification

Canonical docked construction remains bounded by repeated full spatial codecs
inside nested NavGrid/terrain-condition/route observations. Every SpatialScope
currently captures an initial full codec hash even when an enclosing pure read
already owns that exact selected space's live baseline. Do not remove verification
or replace it with a revision-only/positive-result cache.

## 1. Goals

1. Reuse only an enclosing synchronous same-world/same-space baseline; never reuse
   a previous completed read, update, session, restored save or different space.
2. Retain full original codec checks after the revision getter and after EVERY
   callback, including nested callbacks; all actor/provider/clock/random facts remain.
3. Detect a parent's mutation before entering a nested callback, not accept that
   already-corrupt prefix as a new healthy baseline.
4. Clear active observation scratch on success, exceptions and swallowed faults;
   preserve original errors, first-fault behavior and saved-state formats.

## 2. Design

World holds one internal nullable active SpatialScope, strictly call-stack scratch,
not persistent state. EntityObservation.ReadProvider captures the previous scope,
constructs/verifies its spatial observation, activates that scope only for this
pure interval and restores the previous value in finally. A nested SpatialScope
may copy the enclosing original ProviderScope when world/space identity matches.
It still reads and guards the actual revision getter, compares the enclosing
revision and performs the existing fresh full Verify after the actual callback.

Manual CaptureSpace users (e.g. refused-effect guards) do not activate a frame;
legitimate earned effects retain their original commit rules. No static/thread-
local cache, second observer, replacement codec, skipped inner verification,
hash exemption or persistent observation ledger.

## 3. API surface

No public API or saved layout. Engine2.645/save160/replay1; no old-build migration.
PointSpace proof-only Sync/revision hooks count codec invocations and expose getter
mutation; they are instrumentation, not saved gameplay state or a host rule.

## 4. Files

Kernel/EntityObservation.cs, Kernel/World.cs, engine build/current protocol fixture;
tests/ExtensionProof/PointSpaceProof.cs; new NestedSpatialObservationTests.cs,
registered by existing SharedSpatialObservationTests. Stage evidence/master/P3/
AGENTS/README updated together. Existing native provider/physical case reused.

## 5. Errors

Persisted payload mutation, revision invalidation, clock changes and original
callback errors retain original faulting owner. Mutation before a nested read is
refused before the nested body. Catching an inner error cannot restore health;
the parent frame unwinds in finally. Another world remains independent.

## 6. Compatibility

Development only. No default implementation, old scope path or save-header repair.

## 7. Verification

New exact framework case before/after: fresh/sibling/recursive baselines, manual
scope separation, interleaved sessions, payload/getter/clock/throw/swallowed errors,
save/load purity and no stale baseline across a legitimate between-read mutation.
Count real codec calls, not elapsed-time assertions. Then one existing exact native
navigation fault case and one exact layered physical/save case. Canonical actual
consumer preparation/continuation remains independently bounded. No full/profile/
family suite and no increased timeouts or shortened work.

## 8. Risks

Inherited baselines intentionally make nested reads stricter against a corrupt
parent prefix. All live frames must unwind even when construction or verification
throws. This is scoped purity enforcement, not a sandbox for arbitrary CLR code.
Local hash-count savings need not solve the full canonical workload.

## 9. Out of scope

New gameplay/clock/claims, map caps, Oil/business/platform/Draw logic, callback
exemptions, cache across reads/updates, broad regression or Git publication.

## 10. Execution order

1. [x] Reproduce duplicate initial codec capture and corrupt-parent prefix admission.
2. [x] Implement shared live baseline in the original observer with scoped cleanup.
3. [x] Verify focused purity/physical scopes and canonical boundaries; update evidence.
   Three focused final cases197/0; actual harbour20/1 still fails wall limit despite
   healthy physical progress. No original broad gate or full acceptance closes.

[Current focused evidence and canonical boundary](../../docs/migration/P3_NESTED_SPATIAL_BASELINES_VERIFICATION.md).
