# P9-B4d — Authored priority and starvation ordering

Follow-on [B4e mixed dispatch frontiers](P9_MIXED_DISPATCH_FRONTIERS_DESIGN.md) repairs execution batching
across higher-ranked provider intents. This B4d evidence remains historical, not rerun on2.298.0.

## 0. Problem statement

The original WorkSystem.CompareAssignments hard-codes starvation before explicit priority.
The industrial plan specifies emergency → player priority → aging → cost → stable identity.
The generic framework must also permit an explicitly authored starvation-first game without a
copied scheduler. Existing IWorkerDispatchPolicy/DispatchProfileDef own this decision.

## 1. Goals

1. Expose required typed ordering on the existing policy contract, with strict JSON authoring.
2. Keep ready/emergency precedence and stable final ties engine-owned for both orderings.
3. Stamp detached offers only through the original RankOffer; providers cannot supply policy rank.
4. Prove real physical priority-first execution and authored starvation-first behavior, privacy,
   saved pending/travel/work continuation and content refusal in one bounded new case.

## 2. Design

WorkerDispatchOrdering has PriorityFirst and StarvationFirst. Selected native and independent
profiles require explicit ordering strings priority-first/starvation-first. No missing-field reader,
implicit selected-profile ordering or legacy path.
WorkerDispatchOffer carries derived immutable ordering; unranked/native no-policy offers use the
engine's ordinary explicit-priority order. RankOffer stamps the selected frozen profile alongside
the existing alert effective priority and actual-age threshold. Clone operations retain it.
CompareAssignments checks one common valid ordering and retains ready/emergency guards. PriorityFirst
compares effective player priority before starved/age; StarvationFirst retains the explicitly authored
aging promotion. Existing travel/target/company/scope/identity/entry ties are unchanged.
Both mixed productive commitments and machine/timer/source assignment use the SAME rank/comparison owner.
No new queue, age table, resource reservation, clock, controller or persistent policy store.

## 3. API

Existing policy adds WorkerDispatchOrdering Ordering. Existing immutable offer exposes the stamped
value. Native/public provider JSON directly migrates; WorkView schema increments for exposed facts.
Build identity increments; unchanged persistent layouts keep save143.

## 4. Files

Standard/DispatchPolicies, WorkContracts, WorkScheduling, WorkModule and Kernel/SaveGame;
existing independent ShipmentDispatchProfile and two master-listed dispatch JSON catalogs;
one new DispatchOrderingTests case and Program registration. Older identity assertions compile-only.

## 5. Errors

Missing/invalid selected ordering refuses composition; incompatible derived ordering faults comparison.
Neither authored profile can rank blocked work ahead of eligible work or ordinary work ahead of emergencies.
Actual existing admission/material/worker guards still decide physical execution.

## 6. Compatibility

Development only, no compatibility. Existing fair profiles explicitly author starvation-first;
new industrial profiles explicitly author priority-first. This preserves intentional content, not
an old-reader fallback.

## 7. Verification

Build only as needed; ONLY exact NEW case:
Dispatch ordering: authored player priority, starvation and physical saved execution.
Bound physical stages and report every attempt. Freeze executable source/proof/tests/JSON after final green.
No older/family/full/Oil/platform/stale runtime. Broad final P6 gate requires human authorization.

## 8. Risks

Priority-first cannot promise low-priority progress against endless higher-priority work.
Starvation-first deliberately trades strict player ordering for age promotion. Both are visible content
choices, never hidden bonuses or immediate execution. Broader provider/performance matrices remain open.

## 9. Exclusions

No utility rewrite, rendering/platform adapters, new business behavior, selection UI, broad full regression
or completion/publication claim.

## 10. Execution

Inspect original rank owner → update existing contract/content/offer/comparison → thin physical driver →
compile → one new case → freeze → evidence/master/parent updates. Full goal stays active.

## 11. Focused outcome

Implemented through the existing contracts and rank owner. Build2.297.0/save143/work-view84.
Two clean solution builds16.74/5.75s; two attempts of the ONE new case:110/6 717ms, final116/0/0 777ms.
Only the observed new pause-driver assertions were repaired to call normal Game.Update, rather than
the explicitly pause-ignoring tool batch. Executable source frozen at2026-10-08 12:27:01 UTC.
Native priority-first, public priority-first, authored starvation-first, emergency precedence,
physical resources/travel, private facts, saved twins, isolated sessions and canonical replay scoped.
164 older identity paths migrated compile-only. No older/full/Oil/platform runtime; full plan remains open.
[Detailed evidence](../../docs/migration/P9_DISPATCH_ORDERING_VERIFICATION.md).
