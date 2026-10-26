# P10 / P4 — One pure operational-signal observation boundary

Current owner after [P10-C8](P10_SIGNAL_CONTENT_CONTRACTS_DESIGN.md): AutomationSignalContracts.ReadFrom over IAutomationSignalContent is the sole reader. Native instance methods were deleted; same observation/fault cutoff and existing state. Earlier evidence below is historical, not rerun on2.337.0.

## Historical specification

AutomationSignalDef.ReadFrom is already the sole selected signal/schema/value interpreter used by
views, automation, execution gates and activity requirements. Its entity revision check cannot
detect a provider changing persisted fields without Touch. The newest work requirement locally
adds a stronger existing codec observation guard; that protection belongs in the shared reader.

## Design

Keep content and actual session-source identity validation before invocation. Missing or incompatible
sources return the existing typed SignalUnavailable fact without calling a substitute. For an actual
compatible component, capture its existing Sync through EntityObservation.CaptureProvider, read and
validate the exact snapshot, and verify the observation in finally. Latch callback/value failures
through the existing first World fault as well. No copied state, rollback, extra registry or codec.

Remove the work requirement's redundant observation wrapper. All signal consumers inherit this
boundary by continuing to call the SAME reader. Invalid caller aliases are ArgumentException, not
evidence of a committed provider failure. Faulted sessions cannot resume, save or hash; faults are
session-local. This is a scoped provider-codec/entity/clock check, NOT a mod security sandbox or a
guarantee about unrelated entities/services or transient state.

## Authored extension and focused proof

Reuse the existing independent AutomationProbe engine-side provider, definition and command handler.
A small extension module declares it, rather than creating another signal implementation. Add explicit
master-listed extension and work JSON via the existing manufacturing fixture's document loader.
Target signal requirements control actual staffed work; no host/test scheduler or gameplay rule.

ONE exact NEW bounded case covers healthy native/independent Boolean/numeric reads, typed missing/
incompatible facts, private and foreign-session refusals, current physical work pause/resume and saved
continuation, hidden codec mutation, revision mutation, malformed facts and throwing callbacks through
the shared public view/direct reader/work preview and normal Update. Existing automation/gate consumers
are inspected, not claimed runtime-verified. No old/family/full/platform/stale run.

Breaking engine build2.249.0/save139; persistent layout unchanged. Update older build assertions as
compile-only metadata. Build once, run only the new case, repair/rerun observed failures only. Freeze
source/public proof/tests/JSON after green; then docs-only tracker, ledger and scoped source inventory.
One unchanged GameClock; canonical engine only. No Oil/host/demo/Draw/platform/business changes.

## Remaining gates

Sibling/cross-entity/service mutation, hostile codecs, quantity/provider breadth, complete automation/
gate fault matrix, replay/stress/allocation budgets and full P4/P10/P1–P12 remain open. This closes a
shared purity gap, not the whole plan. Full goal active; commit/push only after full verified completion.

## Verified scoped checkpoint,2026-10-08

Implemented2.249.0/save139/persistent layout unchanged. THREE builds:first8.20s/0 warnings/1 probe
constructor type error; repaired string identity, clean6.80s; explicit fixture module selection
repair, clean4.95s. TWO invocations of ONE exact NEW bounded case:first2 checks1 aborted109ms
(extension not selected in scenario); final94 checks0 failures/aborts492ms. Repair observed failures
only.115 older identity assertion files/lines compile-only. Source/public proof/tests/JSON frozen
after green; docs-only afterward. No older/family/full/platform/stale gate. Automation/gate caller
integration source-inspected only; broader acceptance open.
[Report](../../docs/migration/P10_SIGNAL_OBSERVATION_BOUNDARY_VERIFICATION.md).
