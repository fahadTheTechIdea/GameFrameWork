# P9-B13 — Contract-owned reusable recurring activity bindings

Status: scoped verified142 checks on core2.385.0/save154/replay1. Original P1–P12 gates remain open.

## 0. Problem Statement

`Standard/RecurringPlans.cs` still declares `RecurringActivitySource : Component<RecurringActivitySourceDef>`.
Independent metadata cannot reuse its posting/completion/template state without duplicating a runtime.
The existing WorkBoard validates recurring plan metadata, but reusable binding callbacks and signal
conditions need actual immutable aliases and scoped observation before Initialize and during reads.

## 1. Goals

1. Native/direct JSON metadata binds the original recurring runtime through a focused contract.
2. One strict declaration owns source identity, schedule, targeting, conditions and reference resolution.
3. The existing WorkBoard validates every loaded reusable binding, including unused definitions;
   original codec8, plan history, DAG, resource/motion owners and GameClock stay authoritative.
4. One new public-player case proves physical recurring work, cancellation, shortages, conditions,
   saved continuation, private views, replay/isolation and malformed/faulting metadata refusal.

## 2. Design

Add `IRecurringActivitySourceDefinition : IRecurringPlanBindingDefinition, IRecurringOperationPolicyDefinition`
with `Conditions` and pure `ValidateBinding(World)`. `RecurringActivityDeclaration` owns the previous
native parser/resolver, using original settings/binding/condition readers. Native definition delegates;
native runtime derives nongeneric Component and selects the actual contract alias. No compatibility base.

`RecurringActivityBindingContracts` observes actual immutable owner/runtime/codec/conditions through
existing EntityObservation. The existing WorkBoard's validation loop observes the actual metadata,
invokes reusable binding validation and retains all original plan/template/policy/target admission.
A transient recursion guard rejects callbacks that recursively compose. Existing schedule and native
request/trigger reads observe actual provider and entity codecs; no second recurrence state or scheduler.

## 3. API surface

New content contract/declaration only. Existing recurring/work/plan commands, previews and views remain
the player's public API. Master-listed JSON selects native or independent component metadata explicitly.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| Modify | src/Beep.SimEngine/Standard/RecurringPlans.cs | 90 | medium |
| New | src/Beep.SimEngine/Standard/RecurringActivityBindings.cs | 170 | medium |
| Modify | src/Beep.SimEngine/Standard/WorkRecurringPlans.cs | 20 | high |
| New | tests/ExtensionProof/RecurringActivityBindingsProof.cs | 90 | low |
| New | tests/ExtensionProof/content/recurring-bindings.json | 40 | low |
| New | tests/Beep.SimEngine.Tests/RecurringActivityBindingTests.cs | 180 | medium |

## 5. Error handling matrix

| Failure | Result |
|---|---|
| Wrong reusable runtime/codec/alias or missing conditions/plan/target/policy | Before-Initialize refusal |
| Invalid schedule, targeting or exact session interval | Original typed validation, no defaults |
| Signal unavailable or mismatched | Authored blocker, no instant work or substitute target |
| Metadata/provider callback changes scoped codec/clock/identity or recursively composes | Session fault; no healthy save/hash |
| Outstanding/cancelled/failed cycle or resource shortage | Existing plan/history/retry owner controls progress |

## 6. Backward compatibility

Development only; no old readers, runtime copies or compatibility wrapper.

## 7. Verification

Build completed slice. ONE NEW exact bounded case; repairs rerun only the same case. No old/family/
full/Oil/Tycoon/platform runtime. Preserve all attempts and actual process termination evidence.

## 8. Risks

Scoped codec guards are not a CLR sandbox/global rollback. Metadata observation must not recurse
through runtime Sync. New definitions must not bypass original plan/template provenance admission.

## 9. Out of scope

Native tile/facility/template breadth, all other provider callbacks, stress/lockstep/fuzz/soak/full
acceptance and pending G19 authority. No business/platform/host/clock changes.

## 10. Sub-task execution order

1. Introduce sole declaration and focused reusable definition contract; migrate native definition/runtime.
2. Observe all-loaded composition and live schedule/request/trigger reads in existing owners.
3. Author an independent metadata module and one thin physical public-player case.
4. Build/run bounded proof and update stage/master/audit without closing original compound gates.

## 11. Current evidence

Native/independent metadata now shares the original recurring runtime and sole strict declaration.
ONE NEW exact case finished142/0/0 in1145ms, PID36052 exit0, observed1292ms. Three builds and
two attempts of that same case are disclosed in the [verification record](../../docs/migration/P9_RECURRING_BINDINGS_VERIFICATION.md).
First build's negative installer lacked two abstract members; first runtime proof inspected completion
acknowledgment before the next coordinator pass and authored an invalid numeric gate band. Repairs
were proof-only; no gameplay timing or condition/parser defaults changed between attempts.
Native/direct equal chronology, real travel4→25, stock20→12→4, pause/disable/cancel/retry, earned
completion cadence, private facts, saved twins/replay/isolation,19 early refusals and4 scoped live
metadata faults are narrowly verified. Original template/facility/native tile/provider/stress/full gates
remain OPEN. Selected314 executable inputs were hashed AFTER final green at2026-10-09 17:08:51 UTC;
not a pre-run freeze or complete transitive closure. No executable edits followed; no old/full runtime.
