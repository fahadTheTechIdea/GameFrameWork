# P9-B11.2b — Selected complete trip duration verification

2026-10-07 dirty working-tree checkpoint, not clean-HEAD attribution or full regression.
Build beep-simengine-2.188.0; save101; work view64; motion view8; logistics view19.
Saved board35/work system32/movement system6/logistics service11 payloads remain unchanged.
The full framework implementation objective remains active.

## Implemented ownership

The existing WorkBoard.PreviewDock, SelectDock and DockJourney return the selected
MotionTravelEstimate alongside their actual structural arrival and slot. Native/public
motion contracts still own geometry, controlled rates and duration. The sole successful
approach, optional waiting-to-service leg and selected departure contribute to the total.
Rejected alternatives never contribute. Loading ends at the selected service bay;
unloading and work include a feasible departure. Stationary local work has proven zero
travel, without requiring fictitious motion. Optional providers lacking prospective
duration retain typed unknown, not zero, a guessed distance/rate or a substitute route.

LogisticsService.Preview captures the destination selection in the successful source
continuation. Its repeated destination preview is deleted. Existing carrier ranking,
native capacity/stock checks, claims and fresh acquisition remain under the same owners.
Return cargo has only the selected destination journey, never an invented pickup.

HaulPreview.Travel is detached complete conditional movement duration for the actual
chosen carrier and slots. Refusals publish no duration. MotionArrivalContracts.Combine
validates both known/unknown shapes and adds checked signed64-bit intervals. First ordered
unknown propagates; overflow publishes estimate-overflow, never a partial known prefix.
The immutable company-private LogisticsView and IHaulExecution expose the same result.

Duration is measured in the sole game clock's authored update intervals. It is not an
arrival promise: queue delay, loading/unloading, service, future traffic, changed controls
and changed routes are separate. No future slot/road/resource commitment or saved route
is created by the query. Existing phase budgets remain a separate resource evaluation.
No fleet default, duplicate selector/ledger, provider fallback or compatibility overload.

## Focused verification

Final build: `dotnet build Beep.SimEngine.slnx --no-restore` — success, zero warnings/errors,
4.26 seconds reported by the build. Public-only ExtensionProof compiles with the same contracts.

Only the same two existing itinerary suites ran:

- `dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Itinerary admission: prospective provider routes and atomic private previews"`
  —142 assertions passed, zero failed;651ms.
- `dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Itinerary admission: permitted alternatives, physical queues and saved execution"`
  —218 assertions passed, zero failed;1,490ms.

Total360 final assertions, two suites, zero final failures/aborts. No full/family/third gate.
Existing B11.2a and prior itinerary callbacks remain in these same registrations.

New callbacks prove selected alternate service/departure geometry, wait-to-service duration,
native no-area and public unsupported duration, wide exact totals and checked overflow,
malformed later-leg rejection, company privacy, pure queries and detached retained totals.
Quiet native trip totals18 movement intervals (2 approach +10 delivery +6 departure);
quiet public point trip totals10. Actual accepted operating exposure equals both prior
whole-trip previews while real handling/workflow waits take additional game time. Stock
delivers once, no cargo duplicates, and loaded saved twins continue with identical hashes.
Public cargo return selects its actual source destination and takes4 movement intervals.
Pause freezes both physical state and game time using the existing single clock.

The proof authors an empty movement-input bill expressly to observe actual accepted
movement through the existing operating-input exposure owner without a fuel confounder.
It neither adds a timer nor changes cargo stock/reservations or motion behavior.
The dedicated native scenario explicitly authors48×48 tiles so all service/departure
positions are valid. This is fixture content, not an engine/platform size limit.

## Corrections and verification boundary

Initial runtime compilation exposed four migrated internal PreviewDock callers in
DockGeometryClaimTests, SharedDockQueueTests and DockReplanningTests. They now discard
the added result directly; no old signature survives. These suites compile only, not run.
The original two gates passed304 assertions before the new whole-trip callbacks.

Initial new proof placement was outside its authored48×32 scene; corrected only the
dedicated fixture's JSON height. The destination fixture's actual bay is named bay,
not the source's bay.b. A subsequent purity assertion tried hashing pending creation
events; the fixture now finishes normal Update before its checkpoint. No engine guard
was bypassed. The same two suites were rerun and passed after these fixture corrections.

79 current C# build-identity paths migrate mechanically to2.188.0; LogisticsModule declares
view19 for the added immutable preview field. Other metadata/assertion suites compile
only. Clock/save/payload code and saved schemas do not change. Existing dirty changes
are preserved; this report does not claim all file differences originated in this stage.

## One game clock and remaining work

GameClock is the only owner of current time. World creates it once; SimGame reuses it;
World.Tick reads Clock.StepIndex and is not another clock. Travel and accepted exposure
are durations measured from this same clock, not independent current-time authorities.
GameClock.cs SHA256 is unchanged:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.

B11.2a/b are focused verified. B11.3 must connect explicitly authored fleet reserve/service
rules to this selected duration and B11.1's existing phase budgets. No new fleet rule uses
these facts yet. B11.4 broader acceptance, traffic/dynamic retargeting, all provider/geometry/
rounding combinations, global purity, allocation/performance, replay and full framework
acceptance remain open. No Oil/Tycoon/host/demo/platform/Draw implementation or full gate.

The development-lifecycle skill kept this stage's implementation, focused proof and master
tracker aligned without treating conditional trip duration as finished fleet control.
Literal source/dependency fingerprints appear in
[the source manifest](P9_ITINERARY_DURATION_SOURCE_MANIFEST.json).

Final static checks:109 source/dependency and10 document fingerprints match, with zero
duplicate paths, trailing-whitespace lines or missing documentation/help links. Exactly
one runtime GameClock construction remains; old current-build references in C# are zero.
Scoped git diff whitespace check passes (line-ending normalization warnings only).
These static checks do not prove full regression, global purity or performance.
