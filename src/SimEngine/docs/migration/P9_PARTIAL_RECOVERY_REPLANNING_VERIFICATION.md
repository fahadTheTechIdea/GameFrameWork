# P9-C5c1 — Partial non-docked recovery verification

Date:2026-10-07. Historical C5c1 dirty checkpoint, not full plan completion.
Current evidence: [C5c2](P9_DOCK_RECOVERY_EGRESS_VERIFICATION.md). Counts/hashes below are historical.
[Stage](../../plans/enhancements/P9_PARTIAL_RECOVERY_REPLANNING_DESIGN.md) /
[master](../../plans/enhancements/MASTER_TRACKER.md) /
[current manifest](P9_PARTIAL_RECOVERY_REPLANNING_SOURCE_MANIFEST.json).

## Direct identity and ownership

Build2.201.0/save108/WorkBoard38/WorkSystem38/work view71. Logistics16/maintenance2/logistics view27
and recurring schemas unchanged. Existing WorkBoard job payload embeds one Fix unloading baseline.
Existing Job owns progress, original amount, remaining cargo and receipts; inventories still own
actual material and promises. Stateless HaulHandling calculates current-leg amount; WorkBoard uses
the same duration/native-prefix calculation for execution, validation and immutable views.
Direct current codec and88 metadata path migrations; no legacy/compatibility reader.

## Actual final gates

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Recovery dispatch: authored policy, private chronology and shared ranking"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Recovery dispatch: physical conservation, cleanup and saved contention"
```

Clean build:0 warnings/errors,4.45seconds. Admission123 passed/0 failed,518ms.
Physical181 passed/0 failed,635ms. Combined304 assertions,0 failures/aborts, only two exact suites.
These same suites were rerun during development. Other suites compile only. No third/family/full/
platform/Draw gate; mechanical expectations are not behavioral regression proof.

## Added proof

Partial delivery refuses a full alternative without changing hash/progress/receipts/time.
Invalid baseline fails explicitly in hashing and replanning before replacement commitment.
An actual partially serviced physical dock refuses retargeting and keeps its bay and promises.
An unoccupied future dock intention may change while retaining a previously delivered prefix.

Native cargo return, public separate-inventory cargo return and loaded evacuation all physically
unload a prefix, reroute the remainder, travel and earn new unloading. Original job ID, worker,
original six-unit amount, creation and actual cargo-out token remain unchanged. New complete
room precedes old-room release; previously delivered inventory is unchanged. New duration uses
remaining-leg amount, with zero unfinished handling credit. Immutable retained job diagnostics
remain detached and pure. Repeated retargeting records cumulative prefix, not a new shipment.
Saves during retargeted travel, partial service and repeated retarget restore identical twins.
Final actual stock totals six, cargo is empty, claims and baseline clear normally.

Existing interruption after a real retarget releases only current claims, resets progress and
preserves residual cargo. Normal recovery dispatch subsequently delivers it physically; saved
twins agree. Existing C5b/C3f/C3e cases remain included in the same two suites.

## Test boundary

Movement, loading, unloading, completion, recovery arbitration and cancellation use engine owners
and Game.Update. Work policies use ordinary player commands. Alternate-stock Take, corrupt-baseline
injection and direct TryReplanDestination with an authored recovery blocker are explicit engine
protocol/fault instrumentation, not player commands and not host gameplay. This proves correct
retarget execution on that signal; it does not prove every provider can generate that signal.
The public proof provider continues to compile against public framework contracts only.

## Clock and exclusions

One unchanged GameClock, exactly one construction. GameClock.cs SHA256:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
No new clock, dispatcher, route store, stock ledger, receipt owner or update loop.
The lifecycle skill kept staged scope, implementation and evidence aligned.

Occupied-dock physical egress is not implemented; retain that next stage explicitly. Endpoint
destruction, broader mixed/shared fleets/spatial providers, fuel/home/service/global purity,
replay/stress/soak/allocation and full acceptance remain open. No Oil/Tycoon/platform/Draw changes.
