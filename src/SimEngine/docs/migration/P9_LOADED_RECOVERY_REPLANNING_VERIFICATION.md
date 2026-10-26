# P9-C5b — Loaded recovery replanning verification

Date:2026-10-07. Historical C5b dirty checkpoint, not full regression/plan completion.
Current evidence: [C5c1](P9_PARTIAL_RECOVERY_REPLANNING_VERIFICATION.md). Counts/hashes below are historical.
[Stage](../../plans/enhancements/P9_LOADED_RECOVERY_REPLANNING_DESIGN.md) /
[master](../../plans/enhancements/MASTER_TRACKER.md) /
[current manifest](P9_LOADED_RECOVERY_REPLANNING_SOURCE_MANIFEST.json).

## Direct identity

Build2.200.0/save107/board37/work37/logistics16/maintenance2/work view70/logistics view27.
Existing WorkBoard job codec gains one nullable recovery-kind byte; no parallel intent/receipt/
retry store. Current strict recovery JSON requires immutable reroute_blockers. Empty means no
reroute, not a fallback. Native/public content use the same reader. No compatibility reader.

## Actual final gates

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Recovery dispatch: authored policy, private chronology and shared ranking"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Recovery dispatch: physical conservation, cleanup and saved contention"
```

Clean build:0 warnings/errors,5.74seconds. Admission111 passed/0 failed,632ms.
Physical130 passed/0 failed,1005ms. Combined241 assertions/0 failures/aborts, only two exact suites.
Same suites were rerun during development; no third/family/full/platform/Draw gate.
Other suites compile only;88 exact current metadata migrations are not behavioral reruns.

## Added proof and responsibility boundary

Missing/null/non-array/non-string/duplicate/blank reroute policy refuses explicit content.
Full alternative refuses without changing job, cargo receipt, destination receipt, history or hash.
Empty authored policy retains blocked destination and loaded promises. Fixed player haul has no
recovery provenance and remains at its original target. Native return, independent public separate
cargo return and loaded evacuation retarget the existing job when a normal player area command
invalidates the original delivery while another compatible output has actual capacity.
Same actual worker/cargo promise and job creation survive; old room is released only after new
complete room is reserved and validated. No stock moves during retarget. Original pickup toils
remain unchanged; only delivery target/slot/toils change. Immutable job views expose provenance.
Loaded saves restore exact itinerary, claims and current provenance. Physical new travel, timed
unload and departure conserve six units and produce identical saved twins through the same clock.

The fixture's alternate-stock Take is explicit endpoint protocol instrumentation representing
another engine provider consuming stock, not host-owned selection/movement/delivery rules.
Player work-policy changes use normal public commands. Existing202 C3f assertions remain included.

## Repaired development failures

Initial build used CheckReservedIn without its content argument (CS7036), and token == instead
of Equals (CS0019); corrected against actual contracts before behavioral gates.
Missing reroute array initially inherited JsonReader.Strings' empty default; a new focused assertion
caught this. The current reader requires an actual JSON array and strict typed identities.
No stale no-build tests ran after failed builds. Final gates above supersede development failures.

## Clock and explicit exclusions

Exactly one GameClock construction remains. GameClock.cs unchanged SHA256:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
No new current-time owner or update loop. Existing retry owner remains when no reroute is possible.
Only fully loaded delivery travel is retargeted. Pure occupancy guard preserves an already arrived
bay; real service-bay/partial unloading/egress combinations are not proven by these point fixtures.
Endpoint destruction, arbitrary docks/spatial providers, mixed/shared fleets, partial handling,
broader trip/home/service/global purity/replay/performance/soak/full acceptance remain open.
No OilFieldDays/Tycoon/platform/Draw work.
