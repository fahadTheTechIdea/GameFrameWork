# P9-C13: Named fleet home intent — Design Document

Status: scoped implementation verified,2026-10-08. Full P1–P12 remains open.

## 0. Problem and specification

Named fleets currently control navigation and work policies, but hosts would have to loop individual
SetHome commands. A later member could refuse after a valid prefix changed. Engine-owned whole
admission must delegate to the existing WorkerHomeContracts and sole IWorker.Home owner.

Success: canonical whole selection, no mutation on ordinary refusal, current named membership,
immutable private facts, idempotent set/explicit clear, physical return and retained loaded work,
pending/partial saved continuation, native and independent public worker contracts.

## 1. Design and dependencies

WorkerHomeContracts becomes partial. PreviewGroup copies/sorts actual unique IDs, requires a seat,
checks ownership and original CheckHome, and reads detached original WorkerHomeView facts only for
owned workers. ConfigureGroup freshly preflights all members before original Configure in stable
order. Changed count uses actual old Home. Provider read observations use the actual existing codec;
violated commit faults the original session, not fabricated rollback or a resumable partial save.

Original home recording accepts current route/area/readiness waits as future intent, not permission
to travel. Original idle return, operating policies, motion, cargo recovery and assignment own all
execution. Acquired jobs and manually held machines are not commandeered. Explicit None clears.

MachineFleetService uses its current ControlledFleet gate and delegates current IDs. IMachineFleets
adds PreviewHome/ConfigureHome; ConfigureFleetHomeCommand carries fleet ID and actual runtime Home,
using normal sender/inbox/Sync. FleetView3 exposes immutable private per-member admission/current
home facts; membership codec1/save140/WorkView80/native worker payloads remain unchanged.

No persistent fleet home, composite policy, depot type, return job, claim, route or clock copy.
Existing master-selected WorldSetup JSON authors initial references; no new home-content schema.
Independent proof declaration reuses ContractWorkerDef and ProofWorkPolicyDef, not a new worker.

## 2. Files and stages

1. WorkerHomes.cs plus new WorkerHomeSelection.cs; existing fleets/module/commands/view; build ID.
2. Independent public declaration and strict master-selected JSON; shared factory assembly only.
3. ONE new exact bounded proof/registration; older identity assertions compile-only.
4. Build/exact proof, freeze source/tests/JSON, record EVERY attempt, update parent/master/evidence.

## 3. Error handling

Invalid/null/empty/duplicate selection → InvalidArgument; invalid seat → NotSeated. Missing actual
member/target/fleet → NotFound; foreign command member/target → NotOwned, private view → NotFound.
Self-home → InvalidArgument; non-worker → NotWorker; unsupported spatial capability → original
home-unsupported. Empty named fleet → fleet.empty. Refusal changes no prefix; lying provider exact
commit or pure-read mutation fail-stops the session. No fallback home or teleport.

## 4. Verification

Only ONE distinct NEW bounded runtime case; repair/rerun observed failures in that same case only.
Native/public workers, whole refusal, input copying, private/frozen facts, idempotence, current member
edits, loaded receipts unaffected, real positive-time delivery and return, sole-clock pause, saved
twins, conservation, isolation, and one public commit violation. Stage bound1500 Game.Update calls
with engine diagnostics. Build all canonical projects; old suites compile-only and NOT rerun.
After final green, freeze gameplay/public proof/tests/JSON; documentation only.

## 5. Risks and non-goals

Scoped provider observation is not hostile-mod global rollback or isolation. Reentrant/interdependent
commit violations fault. Current home remains intent even when operation is blocked. Docked homes,
service/fuel/load controls, formations/corridors, every provider/fault/corruption, non-tile breadth,
replay/lockstep/stress/CPU/full P9/P1–P12 remain open. No Oil/Tycoon business source/host/demo/Draw/
Unity/Godot/platform changes. No legacy or compatibility reader. Commit/push only after FULL goal
AND plan finish and verified; this slice cannot authorize publication.

## 6. Scoped verified outcome

All four slice stages complete. Build2.268.0/save140/FleetView3/membership1/WorkView80; physical
payloads unchanged. ONE build, first attempt9.61s0 warnings/errors; ONE exact NEW bounded case,
first attempt90 checks0 failures/aborts1095ms.132 older strict build identities plus one prior view
identity/factory assembly compile-only; no old/family/full/platform/stale runtime. Source/public
proof/tests/JSON frozen after green. Native/public actual worker homes, no-prefix refusal/private
pure frozen facts/current membership/pending clear/idempotence/area blockers/physical loading/
protected loaded jobs/metadata removal/real delivery and subsequent return/saved twins/sole pause/
20-unit conservation/isolation/omission/one lying public commit fail-stop proved.
[Evidence](../../docs/migration/P9_FLEET_HOME_CONTROLS_VERIFICATION.md).
Each physical stage1500 Update bound;1095ms is not CPU/allocation budget. Full acceptance open.
