# P9-B11.3c2b4 multi-source service readiness — verification

Date: 2026-10-07. Dirty working-tree checkpoint, not clean HEAD attribution or full regression.
Build2.196.0/save104/board36/work35/logistics14/work view69/logistics view25/native recurring8/
public recurring5,5,6. Owner payloads remain unchanged.
[Design](../../plans/enhancements/P9_MULTI_SOURCE_SERVICE_WINDOW_DESIGN.md).

## Canonical engine behavior

RecurringPlanContracts.ReadServiceWindow derives immutable ServiceMovementWindow facts from
ResolveCadence and the session's sole game clock. Existing CheckMovementWindow delegates to
those facts. Earned service remains actual whole-cycle success, not attempted/retry admission.
Pending completion has no usable duration; expired service refuses even zero movement. At an
exact deadline, zero movement is valid. Disabled future sources are explicitly unrestricted.

HaulTripPolicyContracts.ReadServiceWindow evaluates the actual selected profile's sources under
the existing entity observation guard. Stable source-ID order selects the first typed blocker;
otherwise the earliest earned deadline wins, with stable source-ID ties. Empty/all-disabled
source sets are explicitly unrestricted with no invented deadline or duration. Disabled trip
policies remain blocked. CheckDispatch uses this same aggregate; unknown movement and eligible
emergency precedence remain unchanged. Retained cargo still uses the existing physical CheckTrip
budget and is not interrupted by this fresh movement allowance.

LogisticsView.ReadHaulTripServiceWindow shares existing company ownership and returns detached
facts. It is a service-cadence diagnostic, not a complete fuel/safety/area/worker admission or
queue/handling ETA. Other normal physical gates remain authoritative. Hosts neither combine
service deadlines nor maintain service state. There is no new saved field, codec, scheduler,
reservation, service ledger, clock or JSON default.

## Focused public/native proof

Fixtures author two actual distinct workflows: repair and inspection. One machine carries both
a native recurring source and the existing public ProofRecurringSource. Both use service_first,
the same physical board/facility/material/tool protocols, and ordinary player commands. Source
roles are reversed and profile source order is reversed to exercise independent provider/order
combinations. Bounded Game.Update and source/plan views prove both workflows really complete;
saved pending twins produce identical final hashes.

Private diagnostics select pending sources deterministically, never leak foreign company facts,
and are pure. They select either source as the strict earliest deadline, then resolve equal
earned deadlines by stable identity. Actual movement fitting the limiting allowance succeeds;
overlong movement refuses through the same shared calculation. Commands disable sources/policy
without changing retained snapshots. Saved reconfiguration derives the same unrestricted result
without persisting a window. Cancelled initial service reports an expired typed diagnostic with
unknown usable duration rather than granting retry credit.

The first fixture attempted identical activity/target/worker plans from both sources. The existing
duplicate-plan guard correctly refused the second, producing eight admission assertions and a
physical assertion/abort failure. The fixture was corrected to author distinct inspection intent;
the engine deduplication and service dispatch rules were not weakened. All final counts below
come from a successful fresh build after that correction and additional order/deadline assertions.

## Final focused gate

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Completion service: authored cadence, private admission and retained success"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Completion service: timed work, materials, restoration and saved continuation"
```

Clean build: zero warnings/errors. Admission338 + physical364 =702 passed, zero final failures
or aborted suites. Only these same two exact suites ran, including reruns. Other suites compile
only. No full/family/third/platform/Draw gate. Physical waits remain bounded at400 updates;
existing grouped proofs480. No arbitrary-fleet performance or deadlock guarantee is claimed.

81 build identity literals migrate directly.80 paths are mechanical identity-only changes, with
CompletionServiceTests included in functional proof instead; six logistics schema assertions
migrate to14 and compile only. Logistics view identity becomes25. Save104 and owner payloads,
work board/system/view and recurring provider schemas remain unchanged. No legacy reader.

## Single clock and boundary

GameClock.cs is unchanged, SHA256:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
World constructs it once; SimGame reuses it. Existing Tick properties only project the same
clock's update coordinate. New due/remaining values are immutable observations, not time owners.

The lifecycle skill kept the stage, implementation, focused evidence and tracker aligned. Current
fingerprints are in [the source manifest](P9_MULTI_SOURCE_SERVICE_WINDOW_SOURCE_MANIFEST.json).
Older reports/manifests are historical checkpoints; the dirty worktree is preserved.

B11.3c2b4 is focused verified. Broader B11.3c2b/B11.4 fleet fairness, queue/congestion, arbitrary
failures/providers, propulsion rescue, allocation/CPU budgets, global purity/replay and full
acceptance remain open. The active full implementation goal is not complete. No Oil/Tycoon,
host/demo/platform or Draw gameplay changes.
