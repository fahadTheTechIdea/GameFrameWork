# P9-DJ1 — Noncompetitive departure journey verification

Date2026-10-16. Engine2.644.0/save160/replay1. Local implementation verified;
full goal and11 original acceptance gates remain open.

## Production implementation and correctness boundary

In the original WorkBoard.DockJourney.Finish loop, after original guarded
DockPolicy and DockCost, a candidate is skipped only when a genuinely feasible
incumbent exists and the EXISTING Cheaper comparator says it cannot win. Every
better alternative, every candidate before first feasible success and all
original refusal/Pending ordering when none succeeds remain. Service selection,
retained claims, route/area/terrain/callback guards of evaluated journeys, elapsed
work and physical departure are unchanged. No positive-result cache, second
scheduler/router, new ranking algorithm or map/hash mutation exemption.

The original comparator ranks slot kind, actual provider cost, then stable ID.
This is not a lower-bound shortcut: the same cost used to choose the winner is
known before deciding not to evaluate a losing route. A losing route's callback
is intentionally not invoked, like other irrelevant branches. It cannot change
the chosen feasible result; its unused provider fault does not become gameplay.
Actual policy/cost failures and selected/needed route errors retain original guards.

Only engine build/current protocol fixture advance. No save/codec/replay schema
changes, old-build save migration or header rewrite. There are no business,
platform/Draw or host gameplay changes.

## Focused proof and all attempts

New literal: `Dock departure selection: dominated routes lose only after a feasible guarded incumbent`.
Existing DockTravelSelectionTests.Factory/PointGame/PointMotion own the proof.
Additional authored test-only motion witnesses throw if the dominated far/tied
exit is routed. A third witness returns the existing typed Outside blocker for
the cheap exit, proving a later more expensive fallback must still be evaluated.
Every mode runs with ordinary and reversed authored slots and independent native
save/load. Preview hashes/time/claims remain unchanged and the actual selected
service/departure and travel estimate are checked. No new probe scheduler/store.

| Attempt | Child | Result | Runtime / observer |
| --- | --- | --- | --- |
|2.643 baseline new case|15900|0/1;exit1;untruncated;real losing route invoked|262/399ms |
|2.644 new case first|22464|52/1;exit1;untruncated;far/tie checks passed, blocked fixture returned unsupported arbitrary reason|234/360ms |
|2.644 new case final|34176|78/0;exit0;untruncated|231/362ms |
|2.644 existing physical dock selection|22304|241/0;exit0;untruncated|1978/2111ms |

The intermediate failure is a TEST witness error: MotionAreaContracts correctly
rejects arbitrary blocker codes. It was corrected to the existing Outside code;
no engine admission rule was relaxed. The existing physical proof ran on the
same final production core before that test-only correction; its normal motion
definition uses no new witness. It is not rerun/count-inflated.

Existing physical literal:
`Dock travel selection: actual prospective cost precedes stable identity without displacing physical claims`.
It checks near/prospective/tied cases, minute/half-minute quantum, reversed slots,
physical loaded movement/unload/departure, exact stock/cargo accounting, retained
claims, private views, saved twins, successful replay and original illegal cost
callbacks. Two distinct final framework cases total319/0; not a broad family run.
Bounds: new30s, existing90s; termination5s/output10000chars for each.

Target Release --no-restore project builds: baseline6.61s clean, implementation
7.30s clean, corrected witness5.59s clean, consumer1.57s clean. No full solution
test/profile/filter or full marine workflow.

## Actual canonical consumer

Same Gulf96x96/seed1/two humans/seat1, authored site docks, canonical work amounts,
no Tycoon, grants, terrain edits, shortened work or seed search. Prepare24/0,
child25356/session65593,12678ms/observer12776ms,exit0/untruncated. Same minute11
hashE7B4CC98D6977D89 as2.642/2.643 docked preparation. Single observation, not a
statistical benchmark or global performance claim.

Native checkpoint:
`artifacts/marine/c77fed3dff4941ac9dc6577e70b46ff0/checkpoint.json` in actual Oil consumer.
Save SHA25639D89EC7C92AF3C92D1689891368CD6824C1DFEB4CAA05E5756ADB970A0EA649.
First-day continuation session56958 used that original native artifact,
180s process cap,5s termination,18000chars,120s inner threshold checked after native
batches and original25-day harbour deadline. Child27216 FAILED the inner wall
limit:20/1,137673ms/observer137772ms,exit1,untruncated; diagnostic136739ms. Native
minute1031/hash33528DFB2CE103DE, no engine fault: shipyard clearing completed,
dozer moving to depot, two real trucks loading16.666 each. The same wall threshold
previously reached791 on2.642/2.643 docked first observations; different terminal
minutes are not equal-work timing samples or a global speedup claim. The minute1031
gameplay hash matches the earlier2.643 native continuation exactly.

LATEST CURRENT native checkpoint:
`artifacts/marine/06942bc45f614917a712621d9bbc30e2/checkpoint.json`, game.sav44781bytes,
SHA25649B2E5B8CE87701C97FF97A83564D0F00753CFE47A1DF30B7D91CD7DBDDA35AC.
Its stage remains harbour_progress/next harbour_execution. No live consumer worker
remains after authoritative terminal polling/process inventory. No claim of a
passed one-day observation, completed harbour or completed full marine workflow.
The next work is actual remaining cost in logistics/dock route observation, then
continuation from this artifact, not rerunning preparation or relaxing bounds.
One confirmed-live sample on this final worker still showed MapLayer<byte>.Sync,
LayeredTileNavigation cost, NavGrid/PathSearch and DockJourney/LogisticsService
preview. It does not statistically attribute all elapsed time to one method.

## Current identities and handoff

Core444-source path/hash-list SHA256
db60148d09ab7a3825136ccada37c80ebf6bc76a4097fdbdaa83180376840125.
Core DLL SHA256 C8484B7514954E79565F2956D036367C74074CBDC79B3FCE198F6635E310793A.
Framework tests DLL SHA256 E7D3BC2E5F2B220143D0370F84C3896833CEC56A1FEF05423AA59AA54DB04F84.
Consumer proof DLL SHA25609759B515E053A216DFA966719689C63035907EB4244C2EDA211D402A3438710.
Original requirement audit re-read current221 requirements against their unchanged
document checkboxes:210 checked/11 open, no unmatched requirement. No gate closes.

Earlier2.643 checkpoints remain preserved, incompatible by explicit build identity.
Their real clearing/loading/hauling progress and failed wall limits are retained
in [authored-dock evidence](P5_CONSTRUCTION_SITE_DOCKS_VERIFICATION.md), not passed
off as current2.644 results. No staging/commit/push until whole goal and plan finish.
See [design](../../plans/enhancements/P9_DOMINATED_DOCK_JOURNEYS_DESIGN.md).

## Scoped handoff checks

Six new/updated stage design/report documents have no broken relative links.
Framework git diff --check exits0 with35 pre-existing line-ending warnings and no
other findings; targeted consumer diagnostic/JSON diff check exits0 with no messages.
Both index inventories remain empty. No full/profile/family regression was run.
