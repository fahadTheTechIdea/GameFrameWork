# P9-C3g — Selected haul-cost authority verification

Date:2026-10-07. Source-focused dirty checkpoint; whole stage/plan not fully verified.
[Stage](../../plans/enhancements/P9_HAUL_CANDIDATE_COST_DESIGN.md) /
[master](../../plans/enhancements/MASTER_TRACKER.md) /
[current source manifest](P9_HAUL_CANDIDATE_COST_SOURCE_MANIFEST.json).

## Current identity and ownership

Build2.204.0/save109. WorkBoard39/WorkSystem39/logistics16/maintenance2/work view72/logistics view27;
native recurring8/public plan6/service5/company5 unchanged. No payload, format or layout change.
Existing LogisticsService.Preview owns carrier/itinerary/cost admission. Its existing PreviewSource
and PreviewDestination now read that selected candidate cost through one private checked accessor,
instead of discarding it for another requester/source or endpoint proximity calculation.

Owned-stock and supplier source branches preserve non-interruption preference, explicit export
priority, then selected cost/stable actual endpoint IDs. Owned stock still precedes supplier fallback.
Recovery output matching uses selected cost/actual endpoint ID for ordinary and departing continuations.
Successful preview without a nonnegative cost faults explicitly; no zero or alternate-distance fallback.
No new cost store, spatial model, provider kind, dispatcher, route cache, clock or reservation.

## Actual scoped gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Haul candidate cost: complete selected trip, policy ties and saved acquisition"
```

Clean build:0 warnings/errors,5.92seconds. One newly named source-cost scenario:52 passed,0 failed/
aborted,775ms. One build and one scenario execution, no per-edit reruns. Older suites compile only;
81 old build-identity assertion files migrate mechanically. No old/family/full/platform/Draw run.

## Source behavior proved

Five physical authored point fixtures: a carrier at40, customer at10, source at9 and alternate at25.
The old requester proximity metric favors9, but actual selected pickup+delivery costs32 versus30
favor25. Pure direct haul previews, private queue selection and actual acquired job all agree on
the same selected cost/carrier/source. Native and public-only outlet policy providers need no matcher
changes. Higher explicit export priority still beats cost; equal priority/cost uses actual stable
endpoint identity, including independently reversed authored start order.

Unclaimed intent and actual acquired source/cargo/destination receipts save/load identically. Real
movement/loading/unloading/departure complete through engine Game.Update over the one game clock.
Authored forty units are conserved, cargo empties and owned claims clean normally. Retained preview
facts remain detached, and saved twins agree through physical completion.

A sixth changed-supply fixture observes the cheaper selected source, then explicit endpoint protocol
instrumentation consumes that source's existing unpromised stock. Normal acquisition rebuilds and
selects the remaining real source; it does not execute the stale winner, invent stock or duplicate
claims. The checkpoint before acquisition restores identical fresh choices. Fixture JSON passes
through the existing master-listed document loader; no host gameplay scheduler or new content path.

## Scope of evidence

The changed supplier fallback and recovery-destination branches compile and share the same checked
accessor, but this source scenario does not execute those branches. Their separate physical cost
counterexamples remain open; earlier recovery/supplier suites were not rerun. No claim of current
full-stage or full-regression proof. Existing duration/ETA contracts remain separate: provider cost
is not guaranteed arrival, congestion prediction or exact arbitrary geometry/path simulation.
Prospective-origin/multi-dock cost refinements and wider providers/fleet/replay/stress remain open.

Exactly one unchanged GameClock construction. GameClock.cs SHA256:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
The lifecycle skill kept the source/code-only evidence distinctions and staged tracker explicit.
No Oil/Tycoon/host/demo/Unity/Godot/Draw/adapter changes. Persistent implementation goal remains active.
