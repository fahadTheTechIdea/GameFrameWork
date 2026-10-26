# P9-C3 — Compatible recovery quantities across endpoint contracts

## 0. Problem

Logistics.Preview computes the common source/cargo/inlet CapacityQuantum but discards it in
HaulPreview. RecoveryDestinationFacts only exposes a feasible amount. Native demand policies cap
that amount at an arbitrary destination gap. With cargo quantum2 and destination gap3, they choose3;
the second preview floors to2 and the selector faults. Destination-only validation cannot prove a
transfer is native to all participants. This undermines arbitrary resource/container mod content.

## 1. Goals

1. Every ready native haul preview exposes its exact admitted common quantity quantum; refusals do
   not fabricate a valid quantum. Recovery facts carry this same detached fact, without re-derivation.
2. Native and public-only policies floor demand ceilings to that common quantum. Positive demand
   below one transferable quantum returns a typed wait, preserving actual cargo and chronology.
3. Invalid provider ceilings remain contract violations, not silent quantity conversion/fallback.
4. Paid quantum2 return cargo and quantum3-source/quantum2-carrier evacuation physically deliver
   compatible amounts, wait for insufficient gaps, continue after changed demand and save identically.
5. Partial timed unload/player hold retains delivered prefix/payment and quantized residual cargo.

## 2. Design

HaulPreview.QuantityQuantum is set from the existing Preview's selected common quantum. Selection
keeps the actual selected quantum alongside its amount/trip facts; it creates no new amount policy.
Ready construction validates positive exact amount/quantum. RecoveryDestinationFacts requires this
fact and validates positive native feasible amount; no old constructor or inferred quantum.

Native authored demand modes use QuantityQuantum.Floor(Min(feasible,gap)). If zero, return
logistics.recovery-quantum-gap. No-demand nearest/demand-first behavior remains explicitly authored.
Other feasible destinations can still win normally. Caller validates allowed decisions against this
same common quantum, then freshly previews reductions. Custom content proof follows this public
contract; it reports proof.recovery-quantum-gap for its insufficient gap. No copied capacity logic.
If every feasible endpoint refuses, diagnostics prefer actual positive demand, demand priority,
selected travel cost and stable identity. An unrelated no-demand endpoint cannot hide the requested
endpoint's insufficient-quantum blocker. This is transient refusal selection, not another scheduler.

Reuse the existing fixture loader with explicit physical JSON documents and authoring callback,
not a new world/fixture/scheduler. One document authors pair/triple units, capacity policies and
handling. A second authors a genuine stocked evacuation endpoint/profile. The master lists both
explicitly when selected. Carrier prototype selects authored capacity and handling; tests only
author inputs, submit stock/hold commands, observe, save/load and advance bounded engine Update.

## 3. API

Add detached HaulPreview.QuantityQuantum and required RecoveryDestinationFacts quantity quantum.
No mutable world/provider handle, command or settings store. LogisticsView schema changes directly
for its richer preview; work/account/job payloads stay unchanged. Current engine/save identity only.

## 4. Files

Standard/Logistics.cs, RecoveryDestinationPolicies.cs and LogisticsModule.cs; Kernel/SaveGame.cs;
public-only policy proof; two embedded JSON documents; existing fixture loader/helper access; one
new bounded scenario/registration; direct metadata assertion migration; master/readme/AGENTS/evidence.

## 5. Errors

| Condition | Required result |
| --- | --- |
| Gap below common quantum | Typed wait; real resources/payment preserved |
| Nonmultiple allowed provider ceiling | Contract fault, not rounding or fake success |
| Default/failed quantum in ready facts | Invalid contract facts rejected |
| No demand | Existing explicit native mode behavior |
| Saved waiting/held cargo | Existing owners restore; no regenerated quantity or payment |

## 6. Compatibility

Direct current build/save/view identity. Required facts argument; no old reader, overload, adapter or
compatibility mode. Existing native quantity algorithm, physical receipts and single GameClock remain.

## 7. Verification

At the completed slice, one solution build plus ONE exact new bounded scenario. Repair reruns only
for observed defects; record all invocations. Native/public policies, pure ready preview quantum,
insufficient gap, quantum2 paid recovery, partial unload/hold, quantum6 evacuation selection and
physical completion, conservation and saved interleaved continuation. No older/family/full suite.

## 8. Risks

Demand can remain unsatisfied by less than a transferable quantum; this is honest physical admission,
not permission to oversupply or split an indivisible container. Facts are not promises; acquisition
still rechecks native state. Test authoring changes content only; normal execution stays engine-owned.

## 9. Non-goals

The inspection began with remaining endpoint/reroute work, but this demonstrated quantity defect
must be fixed first. New-policy rerouting/endpoint loss, docks/fuel/congestion, adversarial/global
purity, replay/stress, advanced Tycoon and full framework acceptance remain open. No platforms/Draw/
hosts/demos/OilFieldDays. Commit/push only after FULL goal/plan completion and verification.

## 10. Execution

- [x] Preserve selected native quantity quantum in previews/facts; update policy/native validation.
- [x] Author explicit quantity/evacuation JSON and public-only policy consumption.
- [x] Implement bounded thin-player return/hold/evacuation continuations and exact gate.
- [x] Record scoped evidence and reconcile tracker without closing full roadmap/publication.

Focused2.218.0/save122/logistics view28:63 checks0 failures/aborts515ms; final clean build3.90s.
Two clean builds/two invocations of SAME new scenario. Initial61 passed/1 failure exposed unrelated
no-demand diagnostic masking actual partial-unload gap1. Existing selector now ranks refusal facts
deterministically; strengthened assertion names the actual requested endpoint.93 older metadata
files migrate151 identity lines, compile only (149 initially; two remaining save assertions migrated
alongside the observed repair). No stale binary, older/family/full suite or post-gate gameplay edit.
[Evidence](../../docs/migration/P9_RECOVERY_QUANTITY_QUANTUM_VERIFICATION.md). Only narrow execution
scope is complete; full framework goal/publication gate and endpoint/reroute acceptance remain open.
