# P9-B11.3b retained hauling — verification

Date: 2026-10-07. Working-tree checkpoint, not clean HEAD attribution or full regression.
Build2.190.0/save103/board36/work32/movement6/logistics11/work view65/motion view9/
logistics view21/trip component1. [Design](../../plans/enhancements/P9_RETAINED_HAUL_TRIP_DESIGN.md).

## Ownership and implementation

WorkBoard's remaining itinerary selector serves both existing dock rebinding and pure current
funding. LogisticsService remains the sole haul owner, exposing ReadHaulTripBudget through
IHaulExecution. Existing trip/operating-input/inventory contracts calculate duration, cumulative
native credit, floors and available stock. Acquired returned fuel is protected by real receipts
exactly once. Movement gates before operating-input commit; WorkSystem pauses that same job,
stops verified actual motion and preserves phase, handling, loaded cargo and claims. Assignment
and dock FIFO use physical-only capture so incumbent budget selection cannot recurse.

The actual job's one new TripHeld flag is embedded in its existing board Sync. No copied budget,
deadline, stock, promise, route or timer is saved. Normal commands/private immutable views use
engine logic. Existing stock-band logistics physically refuels stationary held machines.

## Focused final gate

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Haul trip policy: authored contracts, budgets and atomic private admission"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Haul trip policy: physical funding, return commitments and saved continuation"
```

Final build: zero warnings/errors. Admission58 + physical147 =205 passed, zero failures or
aborted suites. Only the same two exact trip suites ran; all separate suites compiled only.
Callbacks also retain the preceding fresh policy tests, not a broad family filter.

Evidence covers private/pure smaller remaining budgets; native/public pre-input holds and
actual same-job resume; unchanged cargo/destination receipts and handling; held checkpoint
twins and sole-clock pause; partial load/unload saves; returned fuel never counted twice or
borrowed; public physical courier stock-band refueling; longer native static detours holding
at the actual outlet floor; repaired geometry resuming without teleport; safe endpoint removal
after ordinary interruption; pending loaded disable/zero-rate operation commands and views.
Physical stages fail after at most300 normal updates with current game-time diagnostics.

## Corrections and limits

Initial added tests used == on ReservationToken, which has value equality but no operator;
corrected tests to its actual Equals contract. A forced-removal test correctly failed because
the destination still owned live resource claims. The test now proves that refusal and uses
ordinary player interruption before engine removal. No resource guard, stock or physics rule
was weakened. Native detour fixture spawn/removal is not a player build/removal acceptance test.

Current build/save metadata migrates directly across86 C# paths (85 mechanical metadata paths
plus the functionally extended trip-policy suite). This does not establish that
separate suites with historical schema expectations pass. Existing dirty changes are preserved.
The preceding B11.3a report/manifest remains historical checkpoint evidence, not current hashes.

GameClock alone still owns current time; World constructs once, SimGame reuses it, and World.Tick
reads Clock.StepIndex. GameClock.cs SHA256 remains unchanged:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.

The lifecycle skill kept implementation, per-stage evidence and the master tracker aligned.
Literal current dependencies/docs appear in [the source manifest](P9_RETAINED_HAUL_TRIP_SOURCE_MANIFEST.json).

B11.3b is focused verified; B11.3c real timed service intervals remain open. Native dock refueling
under congestion, all provider/cargo-store combinations, arbitrary provider faults/global purity,
CPU/allocations, future stock/traffic, non-movement operating costs, replay and full framework
acceptance remain unproven. No Oil/Tycoon/host/demo/platform/Draw work or full/family/third gate.
