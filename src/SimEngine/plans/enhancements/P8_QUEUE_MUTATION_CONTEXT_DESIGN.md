# P8-QC1 queue mutation context — Design Document

## 0. Problem Statement

Standard/Transformations.cs AppendQueue and CancelQueue compare queue contents, local provider state, controls, siblings and entity revision after the callback, but do not protect original game-clock/lifecycle/next-entity/selected-space facts across the mutation. A contract provider can cancel correctly while also pausing the only game clock or creating an unrelated entity.

## 1. Goals

1. Queue mutations may change their exact queue state and advance the provider entity revision, but not clock, ownership/lifecycle/settled entity state, next identity or selected space.
2. Preserve existing material/tool/WIP/accounting and first-fault behavior; no rollback fiction or new state owner.
3. Prove ordinary native/point physical returns and independent queue mutations through public player requests and saved/replayed state.
4. Verify declared malicious cancellation callbacks fail before healthy continuation.

## 2. Design

Capture EntityObservation.Capture(World, entity, allowEntityVersionChange:true) and CaptureSpace(World, World.Space) in each existing queue coordinator after normal admission and before before-state/provider reads. Verify both immediately after Enqueue/Cancel, before trusting after-state. Existing queue/controls/sibling/actual-material checks remain authoritative. Existing outer enqueue/cancel exception boundary records the first fault.

No new transaction, timer, context class, queue or saved guard flag. Add JSON-authored negative-only QueueFault metadata to the existing independent PulseTransformer proof provider; cancellation receives the actual World contract, so probes act on the real clock/identity. Normal provider state/codec unchanged.

## 3. API surface

No production API addition. Work/production hosts still use ordinary commands and immutable views. Engine identity advances; save154/replay1 unchanged.

## 4. Files to change

Existing Transformations.cs coordinator, Kernel/SaveGame.cs identity and protocol fixture. Existing independent PulseTransformer authoring/probe, explicit proof JSON packaging, one data-only module/doc and one public-driver case/register. Tracker/evidence/hash anchors reconciled afterward.

## 5. Error handling matrix

| Action | Result |
| --- | --- |
| Exact queue mutation and ordinary Touch | Accepted under existing strict queue/accounting checks |
| Queue callback changes clock pause/speed or unrelated identity | Existing first-fault latch; no healthy save/hash/continued Update |
| Queue callback changes selected map | Same guarded context; broader spatial fault matrix remains open |
| Provider throws after cancellation | Original exception retained; no rollback claim |
| WIP return awaiting original access | Existing typed blocker, original material/tool identity and timed accounting |

## 6. Backward compatibility

Development only; strengthen canonical mutations directly. No shim or parallel coordinator.

## 7. Verification

One solution build and necessary focused repairs only; ONE NEW exact bounded case. Native/point minute/half physical feed, manufacturing and saved cancellation return; independent healthy queue lifetime, authored negative head/tail cancellation clock/identity/exception probes, original fault cutoff and session isolation. No old/family/full runtime.

## 8. Risks

Scopes are observation guards, not a CLR sandbox or rollback system. Unrelated remote state/unknown fields remain outside the promised proof. Map fault probes, all production phases, optional monetary costs, worker/utility/result/continuous matrices and broad/full P8 remain open.

## 9. Out of scope

Oil, platform/Draw/adapters, Tycoon/protected G19/clock implementation changes, broad regression or publication.

## 10. Sub-task execution order

1. Guard both original queue mutation coordinators with existing context scopes.
2. Author negative independent callbacks and actual public-player physical proof.
3. Run one bounded case, preserve all attempts, reconcile canonical tracker/evidence.

## 11. Status

Core2.482.0/save154/replay1; canonical enqueue/cancel coordinators guard actual clock/lifecycle/next identity/selected space while allowing legitimate provider revision. ONE NEW FIRST/FINAL324/0/0 in3765ms;2 independent minute/half queue lifetimes,4 native/point minute/half physical cancellations/returns and8 head/tail clock/identity/exception faults, actual saved/private/pause/replay/isolation behavior. One clean solution9.49s, no repairs or executable edit after FINAL. Original221/186checked/35open unchanged, additional209. Enqueue-negative/map/lifecycle/foreign-state/all-phase/optional business/full P8/P1-P12/publication remain OPEN. [Evidence](../../docs/migration/P8_QUEUE_MUTATION_CONTEXT_VERIFICATION.md).

Fresh read-only metadata closure:1712 source anchors,953 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2900 paths/1026 Markdown files/8666 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2888/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional209. Six existing selected executable inputs changed; three newly created executable inputs/five new source files, two existing production sources changed/zero new. No previously existing input newly admitted. Existing view schemas unchanged (work100/logistics42/transformation19); queue mutation context adds no projected fields. No runtime rerun for documentation validation.
