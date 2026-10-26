# P9 — Contract-based plan-reference observation

Current M106,2026-10-23: native2.741.0/save161/replay2, two existing focused cases624/0/0.
[Actual controller schema/codec baseline, implementation and limitations](../../docs/migration/P9_PLAN_REFERENCE_OBSERVATION_VERIFICATION.md#current-m106-actual-controller-schema-admission-and-checkpoint-observation--2026-10-23).
The full framework goal remains active. No original requirement closes through this slice.

## Requirements and single ownership

IPlanHistoryRetention remains the pure extension contract. The existing WorkBoard owns actual
plan history and the existing EntityObservation owns read-only callback validation. A module
may provide component, system, persistent-service or controller retention through existing
composition; service/system aliases are deduplicated by actual instance. JSON remains explicit
and master-listed, including group/enabled policy and controller selection. There is no default
enabled value, directory discovery, parallel pin index or compatibility reader.

Ordinary explicit and automatic retirement share original whole-selection admission. Native
dependencies, recurring cycles/sources, live work/disposition and controller replies retain their
existing precedence. Only the original terminal list and ID lookup are pruned after successful
admission; structures, resources, work, counters and the sole GameClock remain untouched.

## Actual bound-controller schema and codec

The original PlayerControllersSystem.ControllerSchema helper observes StateSchemaVersion
through EntityObservation.ReadProvider and the actual IPlayerControllerState.Sync codec.
Creation reads once into the existing Binding.Schema; composition validation and Sync use
the same helper. Keep original null/stateful/unique/positive/stable-schema checks. Metadata
must not pause time or change encoded state during admission, hash, save or hash breakdown.
Original first-fault and callback causes remain authoritative; no partial healthy checkpoint.
There is no new schema store, serializer, wrapper controller, cache of independent authority,
saved field or scheduler. A proof-only subclass installs negative hooks before composing the
same native controller module once; it supplies no gameplay or alternate system.

M106 adds one healthy control, five startup and30fresh/restored checkpoint fault contexts
to the existing reference case586/0. Existing network handover38/0 proves physical16of20
material construction, saved sessions and three actual canonical replays. No new case or
profile entry. Earlier M103physical/automatic/strategy proofs retain their own image, not
rerun or relabelled current.

## Retention callback boundary

WorkBoard.ProviderRetainsPlan sets its existing transient reentry flag before observation.
Reentry latches observation.provider through World.LatchFault and throws immediately, so an
adversarial callback cannot swallow a nested refusal and publish a successful outer answer.
The flag is cleared in finally; it is not persisted or used as a second history authority.

The callback executes through EntityObservation.ReadProvider with observeTargetState:true.
The shared helper captures the actual provider's existing Component/GameSystem/IPersistentService
codec. For components it infers the real owning Entity before reading and observes the full
existing entity codec, including private sibling state that does not call Entity.Touch.
Identity/version/clock facts retain the same original observation. No copied entity state,
alternate codec, whole-world hash, rollback or cross-query cache is introduced.

Read RetainsPlan exactly once, require the original world to remain healthy, then return its
answer only after successful shared observation. The shared helper latches and rethrows original
callback errors. Do not put a second Verify in finally: that can replace the first exception.
Mutation detected after an otherwise successful callback also faults the original session.
Neither a false nor a true retention decision exempts the provider from observation.

PlayerControllersSystem already observes the actual bound controller through ReadProvider.
Retain that existing forwarding and controller state owner; never create a second controller
for observation. Its original replies/awaiting commands continue to pin genuine plan IDs.

## Failure behavior and durable state

Unexpected provider throws are terminal, including unchanged throws in external previews.
Original ApplicationException causes survive a simultaneous mutation. Swallowed inner failures
or restored fields cannot clear World.Fault. Normal Update returns no completed step; public
healthy views, save and hash refuse terminal partial state. No accepted retirement result or
history-deletion prefix may be published after an impure admission. Actual mutation is not
silently reversed into a healthy checkpoint.

Saved layouts remain save161/replay2 and their current native component/service/system codecs.
The build changes directly to2.739; old images are not rewritten or accepted by a fallback.
The proof private sibling uses an existing component codec, not new engine state.

## Focused verification and test integrity

The existing reference-contract, automatic-reference and finite physical-history helper bodies
have their own three literal registrations. Their old broad-bundle calls are removed. Existing
failed-execution/controller helper tails remain in their original broad physical registration;
no test body is deleted or executed twice. Only the three affected literals run for this slice.

Coverage includes all four real provider kinds, actual alias deduplication, saved continuation,
own-state returning/throwing corruption, unchanged throws, swallowed reentry and nested failure.
Saved component cases vary retention true/false, preview/command and return/throw, using actual
private sibling codec mutation with unchanged entity version. Automatic cleanup includes these
sibling faults. Terminal record diagnostics read the original private WorkBoard list only in
negative framework tests; public healthy gates must not be weakened for inspection.

The physical proof uses ordinary commands and native worker/logistics/construction owners.
It builds two dependent structures with finite material, saves paused retirement/repeat/new
commands, preserves structures and stock while retiring history, then continues saved twins.
Recording starts before player policy configuration and full replay must reach the final live
hash, not a passing prefix. Tests and hosts never perform work, manufacture stock or move time
through an alternate clock.

## Operational assurance and limits

Use original World first-fault, command results, notifications, journal and replay boundaries.
[Shared operational coverage and wider gaps](../../docs/migration/P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain authoritative; UI notification is not applicable to this headless boundary.

Scoped codec checks are not a CLR sandbox. Unencoded fields, unrelated services, other
sessions, static/reflection/async mutation and every arbitrary provider are not certified.
The matched compiled resource-caller audit is structural, not full resource accounting.
No broad template bundles, full/default/profile/family/soak, performance, CI, consumer/Oil,
Tycoon/business, platform, adapter or Draw implementation/validation is included.

## Stages

- [x] One existing provider/whole-component-owner observation path and original first-fault boundary.
- [x] Existing JSON/contracts, real owner proofs, focused extraction and current profile assignments.
- [x] Earlier M103: three focused cases295/0, complete physical replay and original evidence reconciliation.
- [x] M106: actual schema/codec admission, five startup/thirty checkpoint faults and two existing
  affected cases624/0, with three physical network replay verifications and current input freeze.
- [ ] Broader optional-module durability, global purity, pressure/lockstep/soak and full framework gates.

Earlier A13.3/A13.4 evidence is retained in its dated verification sections. Their old build,
broad-suite totals, finally guards and external pure-throw health do not describe current behavior.
