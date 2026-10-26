# P5-A47 — Oil activity contracts and external runtime evidence

Date:2026-10-14. Core2.569.0/save157/replay1. [Design](../../plans/enhancements/P5_OIL_ACTIVITY_CONTRACT_BOUNDARY_DESIGN.md).

## Original owners

The existing IActivityExecutionOrders aliases the original WorkBoard. PostActivity delegates to that owner's existing Post and returns the existing detached ActivityServiceOrder. Oil SurveyHandler receives the real identity; no mutable order escapes into the domain result. Its existing placement, resource access, stake and final survey effect remain unchanged.

Research now uses IPlanBoard/IActivityCatalog and its company's WorkView. Both selected research plans and direct generic technology commands remain discoverable; paused linked studies are excluded. ResearchView.ActiveProgress returns original immutable native progress. No duplicate research/work state, clock, scheduler, graph reader or compatibility path.

## Structural acceptance

The complete SimEngine.slnx lists one canonical external Beep.SimEngine project and one OilCo.Game domain assembly. All six local csproj files were inspected and all13 ProjectReference paths resolved. The domain project points to the canonical framework; clients reference the domain/headless layer, and tests also reference the canonical testing support. Both local Directory.Build files import the canonical framework's corresponding file. No embedded engine project or linked framework source is present.

The compiled runtime case8/0 verifies one domain assembly, one direct framework reference, one loaded framework, no OilCo.Sim reference, no framework types embedded in the domain assembly, no privileged Oil friend assembly, and no public SimGame.World property. This proves the structural P5.acceptance.001 requirement, not every domain/public-provider or gameplay requirement.

## Focused runs and all earlier attempts

Initial Oil solution10.59s clean. Public survey proof compile2.99s failed missing CopyNodes; repair2.80s failed guessed CopyActivityNodes; actual TryCopyNodes compile1.98s clean. No stale binary was used after either failed compile.

Initial composition8/0 in25ms, PID25056/exit0, observer166ms. Initial survey0/1 in329ms, PID4576/exit1, observer477ms: fixture asked for two seats from one-seat sandbox. Affected research95/0 in18385ms, PID28228/exit0, observer18535ms.

Authored two-seat repair compile1.76s; framework solution10.53s clean. Survey0/2 in1380ms, PID26308/exit1, observer1582ms: the newly authored100-percent-land fixture yielded no valid start. Diagnostic compile1.79s; survey0/2 in720ms, PID27472/exit1, observer815ms confirmed Invalid start/out-of-bounds. The fixture now retains canonical terrain percentages/towns; no terrain fallback was added to the engine.

Temporary48-tile fixture compile1.45s; watchdog PID16332 timed out at60189ms. Returned24-tile canonical terrain with periodic diagnostics/hash checkpoints, compile1.84s; watchdog PID26256 timed out at60046ms after posting. Explicit frame_ticks1 fixture compile1.88s; watchdog PID27320 timed out at60034ms with live assigned travel jobs at ticks14/26. These are retained terminated attempts, not success or current live handles.

Longer same-case watchdog PID20336 timed out at180048ms with jobs advancing from1 to2 through tick98; no silent completion claim. Nearby click fixture compile1.70s; watchdog PID28236 timed out at180053ms through tick86. Diagnostic phase/cargo/position compile1.80s; final single300-second bounded survey run116/0 in160121ms, PID21824/exit0, observer160264ms. Canonical machine speeds, stocks, charges and engine movement remain unchanged; the fixture's six-minute work duration is explicit, not canonical36-hour timing evidence.

The successful trace records actual hauling: loaded8 at tick14, loaded10 and movement at ticks26/38, unload down to7.333 at tick50, then seismic travel at ticks62/74/86 and earned work at tick91. Native requirements consume the original ten survey charges. Saved posted/partial-work/completion and periodic continuation hashes agree. The original effect retires the consumed stake and another company cannot read its completion. No stock, actor position, work or ending is written by the driver.

Affected worker-effect case152/3 in879ms, PID28020/exit1, observer1019ms caught preexisting stale save154 and private NotOwned assertions. Only expectations migrated to original current protocol and existing NotFound privacy semantics; no effect behavior changed. Targeted framework compile5.90s clean; repaired155/0 in810ms, PID25644/exit0, observer953ms.

## Remaining validation

Final affected research95/0 in18736ms, PID28248/exit0, observer18899ms; final composition8/0 in19ms, PID27952/exit0, observer187ms; compiled client boundary38/0 in42ms, PID1148/exit0, observer203ms; repaired worker effects155/0 in810ms, PID25644/exit0, observer953ms. Final aggregate412/0 across five literal cases, all terminal without aborted final suites, truncation or observer error. No executable changes after these final observations. Framework solution10.53s/targeted5.90s and Oil solution1.98s/final public1.80s clean; earlier full Oil solution10.59s also clean.

Only P5.acceptance.001 closes. Original221 requirements/195 recorded checked/26 open; additional324. Full canonical timing, provider/client/domain ownership, optional profiles, faults, replay/lockstep/stress/performance and the full goal remain open. Nearby surveyed geometry and six-minute authored work do not prove far/canonical-duration performance. No full regression/profile/family filter, business rule/clock/platform/host gameplay change or Git write.

Metadata verification at historical P5-A47:2239 active source anchors/13 quickstart anchors/2016 selected executable inputs matched that checkpoint.221 original literal requirements/checkboxes/line anchors verified without errors, including the sole earned P5.acceptance.001 checkbox change.8011 local links resolved and18 master rows were valid. Public driver had no direct World/resource/progress writes. Diff checks exited0 in both repositories; sole clock and three protected Tycoon source hashes unchanged. Staged files0 in both repositories; no executable drift after those final observations. This is not current P5-A48 metadata verification or another game test run.

## P5-A48 incremental verification — 2026-10-14

Historical incremental results below belong to build2.570.0; not rerun wholesale on2.571.0.

Core2.570.0/save157/replay1. Existing domain views now guard terminal sessions and publish complete staged lists; entity scoreboard disclosure and technology menu/lock lookup use the original guarded native authorities. No host, clock, business-rule, canonical content or state-codec edits.

- Targeted OilCo.PublicProof build: initial4 errors from ambiguous Oil/native ExplorationView and ObjectiveView test names (4.91s); aliases repaired, clean build1.78s, zero warnings/errors. No test ran against the failed build.
- New exact case `Oil view boundary: terminal queries cleared collections native disclosure and session isolation`:79 passed/0 failed,7158ms; bounded observer7289ms, PID22888, exit0,30s limit.
- Existing exact case `Oil native research: authored DAG facility gates plans fees and saved knowledge`:95 passed/0 failed,19135ms; bounded observer19310ms, PID15540, exit0,60s limit.
- Existing exact case `Oil thin clients: native shell input view purity saved state and engine automation handoff`:TimedOut, PID24092,30055ms,30s limit; terminated by the bounded runner. No final check count, no success claim, no retry. No claim that this timeout establishes a gameplay regression or its cause.
- Diff checks exit0 for the touched tracked files; no Git index/write/publication. No full/profile/family execution. Original acceptance checkboxes unchanged; no whole-goal completion claim.

The new proof uses public player attachment to trigger the original `player.decide` first fault. It writes no World/entity/inventory/work progress. It proves terminal/hidden-menu behavior and scoped saved-session isolation, not a provider exception midway through a collection, universal privacy/purity, broad replay/lockstep or full canonical acceptance. P5-A47 audit hashes are deliberately historical until the next consolidated metadata audit; avoid treating them as current source verification.

## P5-A49 incremental verification — 2026-10-14

Build2.571.0/save157/replay1, no payload change. One TechnologyService fact projection now serves scoped individual and catalog reads. Oil Has uses that contract, so hidden authored earned knowledge is retained but not disclosed. Native view/service list outputs clear before terminal guards.

Focused observations:

- Native single-read case:31/0,341ms; PID22664/exit0/observer512ms.
- Existing physical progression-disclosure case, extended with individual hidden/transitive/discovered/earned/revoked queries:71/0,2062ms; PID22880/exit0/observer2202ms. Real discovery/research, original forgetting, saved twins and existing replay verifier run; no host state injection.
- New Oil individual-read case first34/2,1481ms; PID26404/exit1/observer1590ms. Only stale-list clearing failed; production TechnologyView guard repaired. Same exact case final36/0,1555ms; PID9824/exit0/observer1660ms. No unrelated runtime reruns.
- All limits30s; no timeout or truncation in these runs. A48 thin-client timeout remains historical/incomplete and was not retried.
- Targeted framework test builds clean15.66s and7.79s (the second keeps actual definition getter observation inside the shared projection). Oil targeted build initially1.78s/one fixture variable-shadowing error; renamed local, clean1.44s. After the real stale-list repair, clean3.19s. No runtime launched from a failed compile.

The first two cases preceded the final list-clearing-only repair; their individual fact/physical behavior code was unchanged by that repair. The final Oil case directly exercises the repaired native guard and current scoped contract. No broad suite/family/profile/full regression claim. Original acceptance checklist unchanged, consolidated audit hashes remain at historical A47, no Git staging/commit/push or clock/business/platform/content-rule changes.
