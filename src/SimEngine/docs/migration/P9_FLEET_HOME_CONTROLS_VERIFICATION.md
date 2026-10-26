# P9-C13 — Named fleet home intent: scoped verification

2026-10-08, canonical Beep.SimEngine only.
[Stage](../../plans/enhancements/P9_FLEET_HOME_CONTROLS_DESIGN.md).
Full P1–P12 remains ACTIVE/incomplete. Commit/push only after FULL verified goal AND plan finish.

## Implementation and ownership

SAME WorkerHomeContracts becomes partial; WorkerHomeSelection.cs adds canonical copied unique IDs,
seat/actual ownership/original CheckHome admission, detached immutable current WorkerHomeView facts.
Foreign entries disclose no actual home facts. Original ReadBinding now guards actual worker codec
and scoped entity/session/clock facts, including on throwing callbacks. No second persistence owner.
ConfigureGroup preflights the whole set before original Configure in stable order. Same Home is an
exact no-op; changed counts derive from actual intent. Violated commit fail-stops the original session,
not malicious-mod rollback, whole-world sandboxing or a resumable partial checkpoint.

SAME MachineFleetService resolves CURRENT members through existing ControlledFleet and delegates.
IMachineFleets PreviewHome/ConfigureHome and normal ConfigureFleetHomeCommand use original sender/
copied inbox/Sync. FleetView3 returns private immutable admission/current facts; foreign target
refusals conceal existence, commands retain NotOwned semantics. Fleet records remain company/name/
profile/canonical membership ONLY. Service codec1/save140/WorkView80/physical payloads unchanged.

IWorker.Home remains the sole intent. Original WorkerHomeContracts.Configure/UpdateIdle, work policy,
spatial approach and motion execute real return. Home recording can retain currently area-blocked
future intent; it does not authorize movement. Held control remains held; acquired jobs and actual
receipts/cargo continue. No new fleet home/job/route/reservation/stock/clock overlay or instant return.
Master-selected original WorldSetup JSON authors initial home references. New public declaration
reuses existing independent ContractWorkerDef and ProofWorkPolicyDef, not a duplicate implementation.
Tests assemble modules/documents, submit player commands, advance Game.Update and inspect facts only.

Build2.268.0/save140/FleetView3; membership1/WorkView80 unchanged. Original sole GameClock SHA256
339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31 unchanged.
No Oil/Tycoon business source/host/demo/Draw/platform edits.

## Every build/runtime attempt

ONE build: `dotnet build Beep.SimEngine.slnx --no-restore`.
First attempt exit0, reported9.61s,0 warnings/errors. Tool yielded live session4486 after10.0119229s;
one completion poll returned exit0. This is ONE build, not two invocations. No repairs.

ONE exact NEW bounded case:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Fleet homes: whole intent, protected physical cargo and saved return"`.
First attempt exit0,90 passed0 failed0 aborted1095ms; tool wall2.8901413s. No reruns or repairs.
132 older strict build identity files, one old FleetView identity and shared factory assembly
compile-only; no older/family/full/platform/stale runtime. Gameplay/public proof/tests/JSON frozen
immediately after green; documentation only afterward. Prior gates historical, NOT rerun2.268.0.
Each physical stage1500 Update bound reports real clock/jobs/worker/cargo/blockers/home/positions.
Runtime1095ms is not CPU or allocation budget. This focused proof is not whole-engine acceptance.

## Proven scope

- Native Worker and independent public ContractWorker own real home state in one named fleet.
- Actual selected master JSON/public module, original initial references, no economy dependency.
- Canonical pure detached immutable current-home/held-state facts; held snapshots stay frozen.
- A later self-home refuses ALL, leaving valid native prefix unchanged; real sender/private semantics.
- Malformed/duplicate/empty selection, observer control, non-worker, foreign missing facts,
  missing/negative target and explicitly empty fleet refuse without invented providers/homes.
- Pending clear copied from caller and saved in original inbox; actual two-owner changes and twins.
- Explicit None does not release held control; repeat clear reports zero/no owner touch.
- Edited current membership affects native only; removed public member keeps own intent.
- Future home outside current operating area is recordable but actual return remains blocked;
  released machines do not move/teleport or acquire invalid hauling. Normal area edit admits work.
- Actual selected native/public workers acquire original hauls, timed partial loading saves,
  fully loaded positive-time travel precedes delivery. Route disable stops future work only.
- Busy home edit retains original job/worker/destination claims/full cargo and reports Working,
  not an immediate home job or delivered stock. Changed intent and actual commitments save.
- Equal explicit local Pause on saved twins stops original clock and all authoritative progress.
- Remove fleet metadata while loaded: original timed unload/departure completes16 at destination,
  source4, cargo0, total20. Original idle behavior then physically returns to selected source.
- Entity Update settles authored moving state; partway return saves and resumes to actual AtHome
  over additional game time, equal saved hashes and final roundtrip. No fleet required to continue.
- Independent session/public authored homes and optional fleet omission remain isolated.
- One existing public commit-ignore fault admits structurally but faults actual named commit;
  a healthy save is refused. Not a complete provider/read/commit fault matrix.

## Open and not claimed

Docked homes/service selection/fuel/service/load controls, formations/corridors, removed/transferred
member/home lifecycle, every geometry/provider/read fault/corruption/privacy/hidden-content/global
purity/interdependent commit scenario, malicious-mod rollback, non-tile/no-work breadth, emergency
incident end-to-end, replay/lockstep/shuffled commands/fuzz/soak/CPU/allocations/full P9/P1–P12.
Default return intent is not guaranteed dock/service readiness or a future route reservation.
Older cases only compiled; no broad regression claimed. Full goal remains active; no publication.

## Source checks

Selected canonical source/document hashes and local links are recorded in
[manifest](P9_FLEET_HOME_CONTROLS_SOURCE_MANIFEST.json). Own manifest is excluded from its own hash.
Selected706 paths/145 Markdown/2159 local links/0 broken/0 duplicate selected paths. Actual
git diff --check exit0 with existing LF/CRLF warnings. These checks are a scoped inventory,
not a full compilation/runtime/acceptance claim for every selected file. Final readonly SHA and
five-column phase-table checks are recorded in the manifest.
