# P3-NQ1 — Nested navigation queries retain active scratch

2026-10-18. Engine2.662.0/save160/replay1.
[Design](../../plans/enhancements/P3_NESTED_NAVIGATION_QUERIES_DESIGN.md).
Scoped correctness fix, NOT whole-scenario performance acceptance.
Original221 requirements/209 recorded checked/12 open; full goal ACTIVE.

## Reproduced original-owner defect

NavService already correctly owns one PathSearch shared across all its grids.
However, Find stored distance/parent/closed/entry-cost/heap scratch directly in
instance fields. A synchronous nested route query from a pure provider callback
cleared and overwrote the still-active parent query's collections. This could
mix profiles' cached costs/frontiers/parents or throw a duplicate-key exception.
The defect concerns transient algorithm state, not a mutation that the existing
world/provider/spatial observation codec can detect.

The actual baseline registers an independently compiled JSON-declared outer
ITileNavigationRule, then exercises both its normal contract registration and
the existing raw persistent-owner delegate registration. During an ordinary pure
cost read, it queries the explicitly authored inner uniform profile (cost1 versus
outer10) through NavGrid.FindPath. Both public outer queries throw duplicate key
[2,2]. Those duplicate-key exceptions occur in scratch code AFTER the provider
observation itself succeeds; no malicious gameplay mutation is required.

The same case separately reproduces overwritten scratch in cost, permitsStep and
reached callbacks, including a caught bare-algorithm child error. All ten actual
baseline failures are retained below. This is not inferred from source alone.

## Single responsibility and implementation

Only the existing Standard/Navigation.cs PathSearch implementation changes runtime
behavior. Its five collections now belong to a private Scratch frame. One
per-PathSearch depth-indexed pool lends each active Find an exclusive frame;
FindCore retains that frame in a local parameter. A finally returns the depth on
all early exits, successful paths and exceptions. Once a query returns, that
frame is reusable. Ordinary sequential searches still reuse the root frame.

There is still ONE A* algorithm, NavService and route authority. No per-profile
pathfinder, second saved route, cross-query cost cache, full-world search table,
alternate clock, map size restriction or world-hash cache. Entry costs are read
through their original guards and memoized only within their actual invocation.
Original heuristic, per-edge diagonal rounding, corner rules, expansion counts,
heap tie ordering, occupancy/area/fuel/dock decisions and observation intervals
are unchanged. All five old shared scratch fields are removed, not retained as
a compatibility path. Save/codec layouts remain unchanged; runtime identity2.662
requires exactly matching saves/peers as before.

## Test ownership and covered behavior

The existing NavigationObservationDef gains one explicitly authored nested-query
mode; it resolves its real inner navigation profile and selected space. The new
embedded navigation-nested-observation.json supplies module/document ownership,
the inner profile, source/goal and64-node inner budget. The existing fixture
composition/master mechanism selects it. No duplicate fixture factory/module or
provider implementation is introduced. NestedQueries is diagnostic instrumentation,
not serialized gameplay or an alternate resource/path owner.

The SAME CapacityLogisticsStressTests owner adds one separately registered partial
case. It checks actual inner invocation, unchanged gameplay hash/time, exact path
agreement with the flat ten-cost control, saved roundtrips, repeated interleaved
original/loaded sessions and another untouched session. Bare-algorithm tests
exercise three active query levels, cost/step/reached callbacks, original child
exception identity and subsequent clean queries. The caught-child-error test is
explicitly a PathSearch unit test, NOT permission to resume a World after an
actual provider fault; original World first-fault semantics remain unchanged.

The separately selected existing rounded-cost case retains its independent
shortest-path oracle, authored low/max costs, directed/corner alternatives,
budgets, exceptions and deterministic ties. The selected independent tile-world
case proves native layered physical movement/public commands and saved routing
through the existing providers. No stress-family/profile/full suite was run.

## Every build and runtime observation

Working directory: C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.
Two target builds, both terminal exit0/0 warnings/0 errors before runtime:

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v minimal
```

Baseline-proof MSBuild7240ms; final runtime-fix MSBuild9240ms. No incomplete build
was used for tests. All runtime outputs untruncated; every row is a separate exact
literal process with timeout30000ms/termination5000ms. Nested output cap14000,
other cases12000 chars.

| Attempt | Passed/failed | Worker ms | Outer ms | Outcome |
|---|---:|---:|---:|---|
|2.661 nested baseline|20/10|291|439|exit1; actual registered/raw/callback scratch corruption|
|2.662 same nested case|130/0|954|1474|exit0|
|Existing rounded-cost oracle|360/0|60|262|exit0|
|Existing independent physical tile world|82/0|281|432|exit0|

Exact literals:

```text
Navigation nested queries: original router isolates active profile scratch and saved sessions
Navigation diagonal costs: rounded authored edges retain optimal deterministic routes
Independent tile world: native layered routing, physical movement and public commands
```

Final572 assertions across THREE selected cases; not572 suites and not a full
regression. Final nested coverage executes additional assertions because baseline
failed queries could not continue through healthy save/repeat checks. No previously
passing spill/salvage/dock families were automatically rerun or counted again.

## Final identities — SHA256

Core448 C# sorted relative path/hash records, LF joined:
c47dc3a8477dbcdd71ff39b78d024beb6219420de4ab140706ed32127204dc97.

| Artifact | SHA256 |
|---|---|
|Core DLL and Tests copied core|5F927EF880E30632D0AADC9C2CE592676422F0518AB32C3BB7F4176E20B2882D|
|Tests DLL|9F1B09037CBC88CD8D4AE882D2D379C4C29AD394C803F769DC107C01F6833FE4|
|ExtensionProof DLL|2E32BD65B8A1036BC566A25A34AB3336883ECC1A5B288ED34E9C60960BD7011D|
|Navigation.cs|38E1F0A3DCDE755612FDAA05744390BAB055CDC48359ECDBDB2557A179FB9275|
|New same-owner partial proof|08DF0FC4D0C50BFD6A350633F63A776D6B258BB960F08EB2121D2A032EEE15B0|
|Explicit nested-query JSON|65CF44E7024AC48C2CA3AAF1252C4C670370D3A84B0E5305DCC5F93FAA8A109A|

## Limits, consumer boundary and original audit

No speedup/harbour/fleet/per-Update allocation or soak claim. The earlier full
spatial hashing/route cost remains a separate measured performance problem; this
stage neither bypasses it nor adds a cached world-fingerprint authority. Acyclic
synchronous nested queries are covered; infinite mutually recursive mod callbacks,
intentional output-list aliasing, concurrent calls mutating one world, pool
high-water allocation and arbitrary private CLR purity are not proved here.

No Oil/business/Tycoon/platform/Draw/host gameplay edit or consumer rebuild. The
consumer PublicProof copied core remains previous2.659 SHA
E59763A3457711D9C1905C96700886241083A699BEC700BB15B400249F2B35A8.
No compatible2.662 native checkpoint exists from this stage. Older checkpoints
are preserved historical/incompatible, never header-rewritten; no expensive day
was blindly restarted for this correctness fix.

Fresh original text/checkbox audit221 total/209 recorded checked/12 open, unchanged.
All original broad acceptance gates and full goal remain OPEN. Framework
diff --check exit0/35 existing LF warnings/no other findings, both repositories
staged0, no live bounded/worker processes. No executable changes followed final
runtime observations. No staging/commit/push until whole goal AND plan finish.
