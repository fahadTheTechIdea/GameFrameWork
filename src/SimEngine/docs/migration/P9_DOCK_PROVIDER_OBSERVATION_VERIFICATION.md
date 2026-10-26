# P9-DP1 authoritative dock provider observation verification

## Engine changes

Core2.476.0/save154/replay1. The existing WorkBoard now routes dock binding/slot aliases, request, vacancy, reach, cost, geometry, policy and overlap observations through ReadDock and the original EntityObservation owner. Actual endpoint and carrier full codecs plus selected spatial codec/revision are verified before facts return. Result enum/cost/geometry/alias validation runs inside that fault boundary. Version-only Pure and duplicate Tick/NextIdentity guards are deleted, not wrapped.

Original Scope/ProviderScope gain an explicit allowEntityVersionChange option for mutable motion intent only. Other identity, ownership, lifecycle, clock and payload checks remain. DockApproach observes endpoint/space, carrier lifecycle, all actual non-motion component payloads and pure presence captures. Original motion intent/version invalidation is permitted; actual position remains Update-owned. No new scheduler, stock, claim, geometry, fault, save payload or game-clock owner.

## Build and attempts

One solution build only: dotnet build Beep.SimEngine.slnx --no-restore failed10.08s,0warnings/1error: negative probe called Pause on read-only IGameClock. Corrected to the actual GameClock in engine-side negative code only; clock source unchanged.

Two necessary focused builds: dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore, clean11.27s and FINAL7.92s, each0warnings/errors.

| Attempt | Passed | Failed | Suite ms | PID | Process ms | Outcome |
|---|---:|---:|---:|---:|---:|---|
| FIRST | 720 | 5 | 4601 | 43924 | 4916 | Native stock reader filtered point resource supply instead of process.input; carrier fault probe assumed a missing SingleResourcePort. Correct actual projection and actual IWorker.Job codec mutation |
| FINAL | 725 | 0 | 6588 | 18104 | 6835 | All scoped healthy physical flows and callback fault variants pass |

All processes Exited; failures exit1/FINALexit0, no aborted suites, observer error or truncated stream. No executable edits after FINAL; no historical/family/full runtime. FINAL compile precedes passing runtime.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Dock provider observation: pure previews and live motion intent preserve original authority and first faults" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
```

Outer60000ms, termination5000ms, output24000chars. Healthy hauls bounded400Updates with clock/job/toil/dock/blocker diagnostic; live negative callbacks bounded100Updates.

## Actual public-player proof

ONE NEW case uses actual embedded master-listed dock-cost-points.json, existing PointGame master, and existing native shared-haul master/files. Driver authors JSON and uses ordinary haul previews/commands/results, immutable views, Game.Update, sole clock controls, saves and replay only. It does not access World or modify stock, job state, geometry, claims or progress.

Three healthy physical flows cover point-space minute and half-minute clocks and native tile minute clock. Actual eight supplies load, travel, unload and depart over game time; saved loaded cargo/twins, paused clock, private views, finite source/destination totals, settled saves, replay and interleaved session isolation pass. Point totals12source/8destination/0cargo and native24source/8destination/0cargo. Expected closed point route returns a pure typed refusal, remains unfaulted and can save/load.

Thirty-one preview fault variants cover request/vacancy/cost/geometry changes to dock version, actual persisted service payload without version invalidation, selected space, clock, and original thrown exceptions; request/vacancy/cost also mutate carrier version or actual IWorker.Job payload without version invalidation. Slot touch/foreign aliases, null/foreign geometry and negative cost are rejected inside the boundary.

Sixteen live fault variants cover reach/approach changing endpoint version/payload, carrier payload, space, clock or throwing; invalid reach, endpoint approach touch, invalid approach and direct carrier position mutation. Probes activate at declared sole-clock coordinate3, after accepted physical intent is saved. Original and loaded sessions fault at identical time with identical first error.

Each violation latches the original first fault, cannot return healthy readiness, cannot save/hash, and cannot bypass cached/private/missing work query health gates or complete another game update. Subsequent refusals preserve the exact first fault; retained immutable snapshots stay safe and an interleaved session remains healthy.

The endpoint payload probe increments only SingleResourcePort's original persisted service count without invalidation. The carrier probe changes only original IWorker.Job. No parallel probe balance/state/codec. Fault activation is authored JSON metadata, not another clock or host action.

## Qualification

This closes P9-DC1's demonstrated vacancy callback fault-latching gap for the tested scopes. No original gate closes or shrinks:221/186checked/35open; additional203. Overlap dispatch is migrated in source but new malicious overlap-provider variants are not claimed as runtime-proven. Native half-minute, every arbitrary/co-hosted capability, undeclared foreign CLR state, all policy/route callbacks, global preview/command parity, queue fairness, congestion, fuzz/performance/stress and full P1-P12 validation remain open.

No compensation/rollback or invented default repairs a violated provider; the session stops. Canonical SimEngine only, protected G19/Tycoon/sole-clock source unchanged. No Oil/platform/Draw/adapters, staged files, commit or push. Entire goal AND plan remain incomplete.

## Metadata closure

Fresh read-only metadata closure:1687 source anchors,936 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2875 paths/1014 Markdown files/8648 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2863/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional203. Eight existing selected executable inputs changed, one new executable input/three new source files, four existing production sources changed/zero new. No runtime rerun for documentation checks.

