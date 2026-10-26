# P9-C5c2 — Physical occupied-dock recovery departure verification

Date:2026-10-07. Historical dirty framework checkpoint; not full plan completion.
Current follow-up: [C5d destination-loss recovery](P9_DESTINATION_LOSS_RECOVERY_VERIFICATION.md).
[Stage](../../plans/enhancements/P9_DOCK_RECOVERY_EGRESS_DESIGN.md) /
[master](../../plans/enhancements/MASTER_TRACKER.md) /
[current manifest](P9_DOCK_RECOVERY_EGRESS_SOURCE_MANIFEST.json).

## Current identity and ownership

Build2.202.0/save109/WorkBoard39/WorkSystem39/work view72. Logistics16/maintenance2/logistics view27
and recurring schemas unchanged. Existing WorkBoard job Sync embeds two Code identities: recovery
cause and retained old bay. Existing jobs remain the sole dock-claim source. Existing cargo and
inlet receipt owners, original job/amount/creation, partial-delivery baseline and clock remain.
No future-room mirror, route store, scheduler, copied stock or independent timer.

## Actual final gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Recovery dock egress: retained bay, physical capacity wait and saved continuation"
```

Clean build:0 warnings/errors,7.41seconds. Exactly one new named scenario suite:81 passed,0 failed/
aborted,693ms. Older suites compile only;88 identity metadata paths migrated, not behavior reruns.
No older/family/full/platform/Draw gate. The failing new scenario alone was rerun during diagnosis
and repair, not automatically after every edit.

## What this proves

Three point-space fixtures: native cargo return, public separate-inventory cargo return and loaded
evacuation. Actual timed loading/movement/unloading first commit a real partial delivery at a dock.
A normal player operating-area command invalidates the old service endpoint while allowing actual
bay/exit/new output geometry. Existing logistics begins the same job's loaded LeaveDock phase.
Original worker, amount, cargo-out token, creation and unfinished handling remain. Both old bay and
selected exit remain actual job-derived claims; prospective output has no fabricated reservation.

During real movement between bay and exit, another pickup's pure dock probe cannot claim the old
bay, even after the first carrier has moved off its center. A normal engine route-access command
pauses movement without losing bay/cargo/progress. A blocked departure checkpoint restores twins.

A competing engine owner's actual inbound claim consumes all unpromised destination room without
creating stock. After route reopening, the carrier reaches the physical exit, clears only its bay
and waits with typed DestinationUnavailable. Original inlet/cargo remain protected. The old bay
then becomes available to a pickup; the carrier stays at its real exit while game time advances,
without moving stock or earning service. Waiting checkpoints restore identical twins.

Releasing the competing claim makes the output feasible. Same existing selector rechecks full room
and actual trip eligibility, then promises new room before releasing old room. The new destination
selects service geometry, not an unrelated departure slot. Same job/worker/amount/creation/cargo
token persist; completed delivery becomes its unloading baseline and unfinished service resets.
Real new travel, timed unloading and final departure conserve all six units. Claims/baseline/cause/
retained bay clear normally and saved twins agree. Retained immutable departure views remain
detached from current waiting views; another company cannot read those job details.

## Admission and selector correction

Empty service-stop lists remain invalid for ordinary/fresh work. Only the actual owned loaded
recovery-departure job can use an empty service-stop list; worker readiness/roles still apply,
and its actual dock exit/continuous route remain subject to area, occupancy and motion gates.
The prospective selector composes old departure and new delivery using actual geometry/duration.
Current-job dock-slot retention now applies only to that actual endpoint, not a different future
output sharing slot IDs. Departure is physical movement, not new service admission at the old site.

## Test and development boundary

Player policy and route changes use normal commands. All movement/handling/dispatch decisions are
engine-side Game.Update. Direct pure dock probes and a foreign owner's inbound reserve/release are
explicit contract instrumentation, not player commands or host-owned gameplay. No synthetic stock
or secondary update loop. Public provider compilation remains public-contract-only.

Initial compiler repairs corrected nullable selected cost and a guessed inventory write API;
capacity contention now uses real owned reserve/release, not fabricated stock. An initial fixture
at60 units/hour crossed the one-unit exit in one update, so its explicitly authored rate is30 to
observe in-transit retention. This is content, not an engine speed/size limit. The repaired scenario
then exposed actual premature job failure: generic admission rejected empty service stops after
entering departure. Current admission accepts only the actual retained egress context. Bounded
failure diagnostics now report game step, position, current job/phase/loaded amount and cause.

## Clock, scope and open gates

Exactly one GameClock construction; unchanged GameClock.cs SHA256:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
The lifecycle skill kept stage, implementation and scoped evidence tied to the master tracker.

This is point-space dock proof, not a broad geometry/fleet result. Other docks, fuel/maintenance/
shared fleets, endpoint destruction, cancellation-origin routing, global purity/replay/stress/
allocations and full acceptance remain open. No Oil/Tycoon/Unity/Godot/Draw/adapter/demo changes.
Overall plan and persistent implementation goal remain incomplete.
