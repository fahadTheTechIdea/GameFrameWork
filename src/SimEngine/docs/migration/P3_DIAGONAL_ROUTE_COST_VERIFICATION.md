# P3-NC1 — Rounded diagonal route-cost verification

2026-10-18. Final2.659.0/save160/replay1; all component/service schemas unchanged.
[Design](../../plans/enhancements/P3_DIAGONAL_ROUTE_COST_DESIGN.md).
Original221 requirements/209 recorded checked/12 open, goal ACTIVE.

## Native investigation and actual defect

First advanced the retained actual2.658 save206d9a...1992 by ONE ordinary Update.
Separate exact original offshore literal with case-input harbour-step:<absolute
checkpoint>, timeout30000/termination5000/output12000.24/0, worker2697/outer2853ms;
Update889ms. No repeated day or full workflow. Native checkpoint5958158a6ca4490ea8a22a6d69682b0f
at1993/hash8B90E5B1A1A40652 was verified by memory/disk reloads. Crane earns447/3600,
trucks move and grid waits actual stock; no host gameplay or resource grant.

Concurrent read-only diagnostic found actual worker33372 by exact command/input,
then dotnet-stack report --process-id33372 sampled its actual stack:

```text
StateHasher.Bytes -> ByteLayer/TileMap.Sync -> ProviderScope.Verify
 -> SpatialScope..ctor -> TileConditionContracts.Matches
 -> LayeredTileNavigationDeclaration.Cost -> NavGrid.EntryCost
 -> PathSearch.Find -> NavGrid.ProbeStructuralRoute -> Mover.CheckRoute
 -> MotionAreaContracts.Plan -> WorkBoard.PlanRoute -> DockJourney
 -> SelectDock -> SelectRemainingJourney -> ReplanDockBindings
 -> WorkSystem.RunJob -> World.Step
```

This is ONE live stack, not CPU percentages or proof that guards are dispensable.
Existing retained dock admission already preferentially validates its actual
binding; no redundant bypass/cache was added. Performance remains OPEN.

The route audit independently found original PathSearch's heuristic inconsistent
with its actual rounded edge costs. Each diagonal edge costs floor(c*14/10),
but the old heuristic rounded only after summing geometric distance. With low
authored minima it can overestimate and close a suboptimal route. A12x12 candidate
was first found by a temporary in-memory arithmetic exploration, then embedded
as explicit JSON and reproduced against the ACTUAL engine before repair:
expected19, actual20, twice (initial and post-failure scratch reuse).

## Sole-owner correction

Only original Navigation.cs Heuristic changes behavior. It sums
diagonal*floor(minCost*14/10) + straight*minCost, with saturated products/sum.
This is the exact shortest obstacle-free lower-bound graph distance, consistent
with each actual edge. Constraints/blockers only remove edges/increase cost.
PathHeap priority,row,column comparator is unchanged. Actual edge costs, search
budgets, once-per-query cost scratch, corner gates, direction, provider/entity/
space/clock observations, motion/work owners and all gameplay JSON are unchanged.
No second navigator, route cache, reopening authority, compatibility or fallback.

Correct route decisions may differ, so EngineInfo/current protocol advance to
2.659. Old native5958158.../earlier saves are PRESERVED HISTORICAL/INCOMPATIBLE;
no header rewriting or compatibility migration. This is a correctness fix, NOT
a performance improvement; admissibility can require more expansions than the
old incorrect heuristic.

## Focused commands and every attempt

Framework cwd C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.
Both target builds used:
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v minimal`.
Baseline build10050ms and final11160ms:0 warnings/errors. Each yielded live
session13482/15171; the SAME handles were awaited to terminal success BEFORE
running any tests, not restarted or overlapped with DLL reads.

Each case below was selected separately using
`dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact '<literal>' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 12000`.
The new baseline alone used output9000; no output was truncated.

| Build/case literal | Passed/failed | Worker ms | Outer ms |
|---|---:|---:|---:|
|2.658 baseline: Navigation diagonal costs: rounded authored edges retain optimal deterministic routes|358/2|50|241|
|2.659 same literal|360/0|57|229|
|2.659 Navigation exact goals: blocked admission preserves guarded costs budgets and region alternatives|25/0|160|340|
|2.659 Layered navigation: authored roads, depth gates and physical saved work|48/0|2933|3110|

New case is separately registered through the EXISTING CapacityLogisticsStressTests
partial; stress Case/Run were NOT executed. It reads explicit embedded
testdata/diagonal-route-costs.json. Independent test-only Dijkstra uses exhaustive
minimum selection and no heuristic/PathSearch helper. Fixed optimum19,48 seeded
weighted maps (minima1..19, blockers, directed gates, corner on/off, region goals),
int.MaxValue costs, query read counts, route edge legality, deterministic repeats,
zero budget, original exception identity and reused scratch are covered.
This numerical oracle is not normal gameplay in a host or a parallel engine.

Exact-goal case covers original early refusals, corner/region alternatives,
budgets and callback/clock failures. Layered case covers actual authored cheaper
roads/depth changes, public player commands, timed physical travel/work, saved
twins and replay. Three framework finals433/0, not full regression.

## Current consumer composition and compatible artifact

Consumer cwd Beep.OilFieldDays/src/oilCo-engine/src/SimEngine:

```powershell
dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v minimal
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact 'Offshore: vessels survey, drill and ship from the sea' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 12000 --case-input prepare
```

Consumer build2400ms MSBuild/3076 command,0 warnings/errors. No consumer source
or gameplay JSON changes. One PREPARATION-ONLY selection24/0, worker7207/outer7336ms.
Same canonical Gulf96x96 seed1, two humans/seat1, no Tycoon/cash, yard71,73,
field21. Same actual native world hash40370764A1DBECED at11. No full/default
offshore case, no long harbour day, no faster work/stock/placement shortcut.

CURRENT2.659 consumer checkpoint:
artifacts/marine/c75792a88ada4d00ad72b9bea4b73cc5/checkpoint.json,
minute11/hash40370764A1DBECED,
verified harbour_projects_admitted/next harbour_execution. Actual memory/disk
reload hashes verified. Harbour/fleet/berth and native update performance remain
OPEN. Do not resume old2.658 saves with rewritten headers, increase stage limits,
cache world hashes or skip provider/route/fuel/claim/departure requirements.

## Final identities, SHA256

Core448 sorted C# path/hash LF digest:
615e531e59f7508e8a4e335980d08b17bcd69dc42dc5b30e7c2d15db812c8952.

| Artifact | SHA256 |
|---|---|
|Core DLL, Tests copied core, consumer copied core|E59763A3457711D9C1905C96700886241083A699BEC700BB15B400249F2B35A8|
|Tests DLL|C0DD2541499E1E00744C2327C55A58613AE8A2FFA7BCE2D827FE98A3248B5776|
|ExtensionProof DLL|D2A3046260F05A669D280D4ED5BC100875C350E0DE0348C71EA03948E7D6FF75|
|OilCo.PublicProof DLL|4444DE328D9505A3A8B7FD4AAF8E3BF205CE74B5760086F05BE2B2C493474BBF|
|Navigation.cs|A670B776992C78946281B87049E343EC75D2FF0479DD68B6CD53EA55DA4F8C0E|
|New test-only partial|14FF7506C8E8C689AC8AC67B80DC1EF1F66C1BA2BEF16838B5680B78D4233E3B|
|Explicit diagonal proof JSON|B79A1EE829A306CE4D1BD68FB65981EF39A8B627F92DA43A6E64E63D590B77F3|
|Unchanged actual Oil structures.json|E63495E70EE54C3F39D4C00E0DBE75EBF003851BD0CB4C71A606ED766BAE9A28|
|Current native game.sav|D131AEA0AB2DA8724DBE0D6FC625AD22D19051F79BC217147112FE23D5FDB95D|

## Remaining scope and hygiene

Fresh original221 text/checkbox audit:209 recorded checked/12 open. No original
broad requirement closes from these narrow checks. Earlier dock/combined
transformation failures remain open; shared-spatial776/0 is prior2.658 evidence,
not rerun/claimed current here. No full/profile/family test or platform/Draw/
business/Oil gameplay edit. Framework diff --check exit0,35 existing LF warnings,
no other findings; staged0 in both repositories and no live bounded workers at
inspection. No executable edits after final focused observations. No staging,
commit or push until whole goal AND plan finish.
