# P9-C14 — Named fleet batching controls: scoped verification

2026-10-08, canonical Beep.SimEngine only.
[Stage](../../plans/enhancements/P9_FLEET_LOAD_CONTROLS_DESIGN.md).
Full P1–P12 remains ACTIVE/incomplete; commit/push only after FULL verified goal AND plan finish.

## Implementation and ownership

SAME HaulLoadPolicyContracts becomes partial, with canonical whole selection in HaulLoadSelection.cs.
It copies/sorts explicit unique actual IDs, requires a seat and ownership, uses original CheckConfigure
and Read, and returns private detached immutable HaulLoadSettings/current blockers. Actual provider
entity alias must match; actual existing codec/scoped observation protects Read and CheckLoadPolicy.
No second policy/persistence authority. Original Configure preflights freshly, skips identical intent,
then verifies exact settings/touched version/live identity/company/clock/allocator and worker job/
home/held facts. Whole set preflights before stable commits; violation fail-stops original session,
not malicious-mod rollback or a global sandbox. Normal admission refusal changes no prefix.

SAME MachineFleetService uses original ControlledFleet/current IDs and delegates. Records remain
company/name/profile/canonical members ONLY. Normal ConfigureFleetLoadPolicyCommand uses original
sender stamping/inbox copying/HaulLoadSettings.Sync. FleetView4 exposes private immutable per-member
facts with normal command ownership semantics. No fleet policy overlay/automatic override loop.
Original LogisticsService evaluates the selected IHaulLoadProfile using current carrier settings and
actual request/last-dispatch age. No age reset, minimum/wait copy, new timer, cargo/job/claim store.

Disabled batching blocks future intake, not accepted handling/travel/delivery. Native HaulLoadPolicy
now projects its disabled state only while idle and not moving; it no longer overrides actual retained
work or home movement. Original entity Update resolves states. Original strict JSON/master pipeline
authors native/public prototypes, profiles and startups. Public declaration reuses existing independent
ContractHaulLoad and OddHaulLoadProfile implementations; dispatcher has no provider-type switch.
Tests assemble modules/documents, submit normal player commands and inspect/advance engine state only.

Build2.269.0/save140/FleetView4/membership1/native-public load2/LogisticsView29/WorkView80;
physical payloads unchanged. Sole GameClock SHA256
339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31 unchanged.
No Oil/Tycoon business source/host/demo/Draw/platform edits.

## Every build/runtime attempt

All builds: `dotnet build Beep.SimEngine.slnx --no-restore`.

| Attempt | Exit | Reported seconds | Warnings/errors | Observed repair |
|---|---|---|---|---|
| 1 | 1 | 10.06 | 0/2 | NEW ReservationToken assertions used unsupported ==; replaced with original Equals |
| 2 | 0 | 4.95 | 0/0 | None |
| 3 | 0 | 5.77 | 0/0 | NEW live-job retention assumptions repaired after first runtime below |

Build1 yielded live session59676 after10.0028097s; one completion poll returned failure. This is ONE
build invocation, not two. Build2 tool wall5.5974602s; Build3 wall6.3939653s.

Only runtime command, TWO invocations SAME ONE exact NEW bounded case:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Fleet batching: whole controls, original wait history and protected saved cargo"`.

| Attempt | Exit | Passed | Failed | Suite aborts | Milliseconds | Observed repair |
|---|---|---|---|---|---|---|
| 1 | 1 | 94 | 2 | 1 | 2022 | NEW test counted first finished shipment as retained board job; Prune retires it. Use actual live new-job identity |
| 2 | 0 | 122 | 0 | 0 | 763 | None |

Runner counts thrown suite exception as one failure; first two failures include one incorrect count
assertion plus bounded-stage timeout/abort. First tool wall3.7364697s; second2.2846888s. No hidden failed
attempts or broad reruns. Engine/public proof/JSON unchanged after first runtime, test-only repair.
Gameplay/public proof/tests/JSON frozen after final green; documentation only afterward.133 older
strict build identity files/two prior FleetView identities/two old disabled-state assertions/native
factory module-document assembly compile-only; their previous runtime cases were NOT rerun.
Each physical stage1500 Update bound reports actual clock/jobs/worker/cargo/blockers/positions on stall.
Observed763ms is not CPU/allocation budget. This gate is not full regression or engine acceptance.

## Proven scope

- Actual master-selected native HaulLoadPolicy and independent public ContractHaulLoad in one fleet;
  original worker/mover/storage/transporter owners and no economy dependency.
- One no-policy member refuses whole command with valid native prefix unchanged, no fallback settings.
- Canonical pure private immutable member/settings/native-minimum facts, frozen snapshots after edits.
- Malformed sets, observer control, foreign settings, missing rule/settings/over-capacity or unbounded
  unrepresentable duration, empty named fleet, actual sender refusal semantics.
- Pending complete settings copied from caller, saved inbox and native/public owner state/twins.
- All requested fields retained: rule identity, disabled, steel minimum3, exact decimal12.5-minute
  wait, emergency bypass. Same-value command reports zero/no provider touch; home/job/held retained.
- Disabled actual intake creates no shipment. New enabled minimum8/wait10 retains original request
  origin and dispatch coordinate. Actual short three-unit public offer waits until original age10;
  waiting saves, equal explicit local Pause stops sole clock, original dispatch history advances only
  on successful admission. No fictional minimum-sized load or consumed stock while waiting.
- Timed partial public loading saves; disabling future intake retains original job/worker/destination
  claim and dispatch history. Removing fleet metadata leaves physical cargo alive. Loaded cargo must
  travel before actual three-unit unload/departure; saved owners finish identically.
- Public physical return clears destination exit before ordinary held control. Edited current fleet
  contains native only; public disabled settings remain unchanged. Original authored odd rule refuses
  an even two-unit offer, accepts an actual odd three-unit native offer, without scheduler edits.
- Native acquired loaded trip remains live after batching disable; original destination claim and
  worker remain. Entity Update reports authored Moving, not falsely Stopped. Loaded state saves and
  physically delivers/departs;6 destination+14 source+cargo0=20 total, equal twins/final roundtrip.
- Interleaved session/public initial settings and optional module omission isolate runtime state.
- Five strict original master-selected JSON defects refuse composition. One existing public
  CheckLoadPolicy mutation is detected by actual provider observation, faults session and refuses save.

## Open and not claimed

Fleet fuel/service controls/docked homes/formation/corridors, every provider/read/commit fault/corrupt/
hidden-content/privacy/global purity/interdependent commit matrix, malicious-mod rollback, all
emergency/preemption/priority combinations, non-tile/no-work geometry breadth, incident end-to-end,
replay/lockstep/shuffled command/fuzz/soak/CPU/allocations/full P9/P1–P12 acceptance. Emergency bypass
field retention is not a physical emergency incident proof. Settings idempotence applies through
the protocol; arbitrary direct provider calls are not a player control surface. Prior gates historical
NOT rerun2.269.0. Full goal remains active; no commit/push.

## Source checks

Canonical selected source/document hashes and local links recorded in
[manifest](P9_FLEET_LOAD_CONTROLS_SOURCE_MANIFEST.json); own manifest excluded from its own hash.
Selected717 paths/148 Markdown/2174 local links/0 broken/0 duplicate selected paths. Actual git
diff --check exit0 with existing LF/CRLF warnings. These are scoped inventory checks, not full
compilation/runtime/acceptance for every selected file. Final readonly SHA and five-column phase
table checks are recorded in the manifest. Parent P9 now explicitly points existing recurring/dock/
motion/trip owners out of its stale unfinished-responsibilities prose; no duplicate implementation.
