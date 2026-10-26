# P9 / P4-H2 — Spatial-neutral player worker control

Current M108/native2.742: hold ends the actual haul commitment and keeps earned cargo at the
applied hold position. Release enables dispatch, not the same old job or instant delivery.
Explicit selected recovery now uses original request provenance for a fresh physical job;
private receivers need real demand, and emergency eligibility remains requester-owned.
[Current network/save/replay proof and limits](../../docs/migration/P10_COMPANY_ALERT_AUTOMATION_VERIFICATION.md#current-m108-held-emergency-cargo-serves-its-original-private-demand--2026-10-23).
Original worker control/state/clock implementation remains; older paid-control suite is not rerun.

## 0. Problem

Manual interruption currently requires MoveVehicleCommand and concrete tile navigation. WorkModule
also owns a vehicle-named release command. A generic machine using IMotion or no movement cannot be
held/released through player intent alone. MoveVehicleHandler directly duplicates held-state and
interruption execution. Replace that path, not add a compatibility wrapper or second state machine.

## 1. Goals

1. Player intent can hold/release any actual owned IWorker without tiles, money or movement requirements.
2. One IWorkerControl contract, implemented by the existing WorkSystem, owns admission and execution.
3. Work policy interruptibility, existing job/resource/cost release, paid progress and cargo remain real.
4. Hold prevents automatic reassignment; release enables normal dispatch, not instant productive work.
5. Preview/command share exact admission; existing views expose worker-held state and private refusals.
6. Current command streams and saved held/reassigned/paid continuations remain deterministic.

## 2. Design

Add IWorkerControl.CheckHeld/SetHeld(World,PlayerId,EntityId,bool). Bind the same WorkSystem instance,
never another service/loop/state store. Check actual live identity, ownership, IWorker and existing
CheckInterruption. Release of a nonheld worker returns AlreadyDone; hold is idempotent and stops any
manual movement. SetHeld rechecks and uses actual Interrupt/Reopen or haul EndHaul before changing
the existing IWorker.Held/HeldArrived flags. MotionExecutionContracts.Stop stops actual IMotion; lack
of motion is valid. Verify resulting worker flags/job/home/company/session identity and mark changed
entity state. Ordinary pending work may be assigned to another unheld compatible machine normally.

Replace ReleaseVehicleCommand/handler/registration with SetWorkerHeldCommand { Worker, Held }.
MoveVehicle remains a tile-specific destination command, but delegates any worker-control mutation
to the same contract before actual GoTo; it owns no budget/resource/interruption implementation.
Existing WorkView gains PreviewWorkerHeld; existing ReadHome is the immutable held-state view.
Stationary held workers resolve their authored stopped state during entity Update; moving held
workers still publish their actual movement state. No new rendering/state clock/selection mechanism.

Authored policy JSON is explicitly embedded/master-listed. Tests submit hold/release/policy/build
commands against native work/budgets and public-only point motion, never manipulate assignments,
holds, cash or progress. Existing all-money-off mode proves generic control independent of Tycoon.

## 3. API

New SetWorkerHeldCommand and IWorkerControl. Remove old vehicle release API, no alias/reader/shim.
WorkView.PreviewWorkerHeld shares exact CheckHeld. No host/platform implementation.

## 4. Files

Standard WorkerPlayerControl/WorkContracts/WorkModule/Work/StandardCommands; Commands worker control
and old command removal; Views WorkView; direct current engine/save identity; new policy JSON and
fixture factory; one bounded new scenario; three older release callers compile-migrated only.

## 5. Errors

Missing/private/foreign entity -> NotFound; no IWorker -> NotWorker; noninterruptible
live job -> existing NotInterruptible; not held release -> AlreadyDone. Provider violation after
committed interruption -> deterministic fault, no compensation. Pure previews never change state.

## 6. Compatibility

Direct current identity. Old release command is removed, not supported alongside the new command.
Existing worker/job/account payloads remain their sole persistence owners.

## 7. Verification

One completed-slice build and ONE exact new bounded scenario: held idle/travel/paid construction,
actual budget reacquisition after release, policy refusal, current pure preview/command parity, saved twins,
JSON command round-trip, and resource-only physical control. No older/family/full gate. Record all
actual invocations and repair only observed failures. Cargo/fault/endpoint/replay stress stay open.

## 8. Risks

Holding a machine does not pause all work intent; another eligible machine may take it. Keep peers
held using player commands when asserting the selected worker. Paid work does not receive refunds,
lose progress or consume materials twice. Actual removal/disposition is a separate physical workflow.

## 9. Non-goals

Platform movement APIs, a new home/worker state store, instant resource/fuel grants, legacy command
support, advanced Tycoon or full framework completion. Commit/push only after FULL goal/plan finish.

## 10. Execution

- [x] Replace duplicated tile-coupled hold/release execution with the same contract-backed owner.
- [x] Author policy JSON and one bounded player-control scenario through existing physical owners.
- [x] Build and run only the exact new gate; update scoped evidence and master, leave full goal open.

Checkpoint2.216.0/save120/work view75:149 checks0 failures/aborts438ms; one clean build4.09s,
first attempt. Existing payloads unchanged; no post-gate gameplay edits or older/family/full suite.
[Evidence](../../docs/migration/P9_WORKER_PLAYER_CONTROL_VERIFICATION.md). Tile-specific runtime,
full cargo recovery/endpoint/policy/provider/fault/replay/stress and full framework remain open.
This scoped slice is complete, not the full P9/H2 roadmap or publication gate.
