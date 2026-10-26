# P4-G53: Observed runtime activity metadata — Design Document

## 0. Problem Statement

G52 observes executor methods, but native work/admission/ranking/checkpoint paths still call IActivityExecutor.RequiresWorker without selected entity codec observations. That getter controls whether actual jobs/machine commitments or timed admission are used. A changed return value can disagree with immutable node roles.

## 1. Goals

1. Read runtime worker metadata through the existing executor observation path with the actual order target and assigned machine.
2. Reject worker metadata inconsistent with the node's authored roles before publishing a decision.
3. Migrate original work graph walk, productive frontier/admission, timed admission, assignment view and itinerary checkpoint consumers without introducing another scheduler or metadata cache.
4. Verify native/direct physical execution, save/continue, pause, replay, privacy and invalid getter behavior through one new exact bounded case.

## 2. Design

ActivityExecutorContracts.ReadWorkerRequirement delegates Read and validates roles inside the callback observation. WorkBoard.RequiresWorker resolves its original order/node/job links and supplies actual target/assigned worker. WorkSystem and WorkBoard partial files use that sole method. The bool is transient; no persisted flag, copied machine identity, registry or compatibility path.

The pre-world ActivityGraph compiler and TransformationStaffingGateDef content-resolution getters are not part of this runtime slice; they remain explicit completion gaps. Composition/plan validation already uses G52 guarded callbacks without a runtime target. Arbitrary nonparticipating entity mutation and global CLR sandboxing are not claimed.

## 3. API surface

No new public host API. Existing JSON IActivityExecutor definitions, commands, Update, immutable views and saves remain the public boundary.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | Standard/ActivityExecutors.cs | medium |
| Modify | Standard/Work.cs, WorkAcquisition.cs, WorkStart.cs, WorkScheduling.cs, WorkDocks.cs | medium |
| Modify | Kernel/SaveGame.cs, current protocol fixture | low |
| Modify | Existing executor proof/module/fixture visibility and test registration | low |
| New | ActivityExecutorMetadataTests.cs | medium |

All engine paths are below src/Beep.SimEngine; proof/test paths are below tests.

## 5. Error handling matrix

| Behavior | Result |
|---|---|
| Actual target/worker private persisted mutation without Touch | Original observation faults; no healthy save/hash |
| Clock mutation | Original scalar observation faults |
| Changed worker classification inconsistent with roles | Original typed ContentException |
| Getter throws | Preserve original typed first error, return no facts |
| Healthy native/direct metadata | Original jobs, timers, claims and physical progression |

## 6. Backward compatibility

Development only, no shims. Build2.396.0/save154/replay1; persistent layouts unchanged.

## 7. Verification

Build the completed slice once compilation needs checking. Run only new exact case: Activity executors: runtime worker metadata is observed and role-consistent. Runtime timeout30000ms, owned termination5000ms, output30000chars. Preserve every repair/build/run result. No older/family/full/Oil/Tycoon/platform runtime. Final results and source inventory will be recorded in the paired evidence document.

## 8. Risks

Whole selected entity codec reads cost CPU; this is not an allocation/performance gate. Pre-world metadata, reentry, unrelated/private providers, all dock/lost-worker matrices and complete industrial acceptance remain open.

## 9. Out of scope

Protected GameClock and excluded three Tycoon bindings; no business/defaults/Oil/platform/Draw edits. Original P1-P12/full goal stay open and publishing remains deferred.

## 10. Sub-task execution order

1. Add sole observed runtime metadata decision and migrate original consumers.
2. Reuse master-listed executor fixture and existing direct staffed definition for invalid getter probes.
3. Build and run only new bounded exact case; repair the same case if needed.
4. Freeze selected inputs and update tracker/audit with current evidence, preserving all original compound requirements.

## 11. Delivery status

Scoped verified, not whole-plan completion. Two clean builds14.99s/6.07s. ONE NEW exact bounded case first118 passed2 failed1805ms, FINAL135/0/0 in1742ms; final PID32192 Exited0, observed1897ms, no timeout/truncation/observer error. Repairs changed only incorrect driver expectations about Update's fault-return boundary, not engine behavior.

Native/direct physical/timed workflow, actual materials20->12, saved continuation, pause, replay, private assignment facts and isolated sessions are narrowly proven. Fifteen invalid getter cases (target/assigned worker private codec mutation without Touch, pause, role mismatch, typed throw across Update/assignment/checkpoint) refuse with no healthy save/hash.

Selected403 inputs frozen after final green at2026-10-09 20:04:29 UTC; no executable edits afterward. [Actual evidence](../../docs/migration/P4_ACTIVITY_EXECUTOR_METADATA_VERIFICATION.md). All original compound gates retain their text/checkboxes; pre-world graph/staffing metadata and broader/full gates remain OPEN. Goal active; no staging/commit/push.
