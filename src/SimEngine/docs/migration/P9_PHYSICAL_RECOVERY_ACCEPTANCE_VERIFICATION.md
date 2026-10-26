# P9-C16 physical recovery verification

## Current C16I: independent wide regions and exact movement — 2026-10-21

Core2.687.0/save160/replay1. Original P9.acceptance.051's exact requirement is now checked again, with its original wording intact: endpoint/road removal, fuel/breakdown/output blocking recover without duplication/deadlocks. This decision uses current execution of every named interruption and the later concrete berth/home failure interaction, not an aggregate test count or an old save header. Full goal/other original requirements remain OPEN; no full/default/profile/family suite, consumer rebuild/runtime or publication.

Architecture/self-review: the first independent composition refuses missing ISpatialRollout before initialization. Original native rules move from TileMap into one stateless TileRolloutRules in the existing TileRollouts file; native and independent selected providers implement the existing contract by delegation. Selected provider placement/occupancy, original guarded EntityRollout boundary, prototype connectivity, explicit budgets, planned prefixes and deterministic static/mobile policies remain unchanged. Keeping native-only policy or copying it into the proof module would duplicate engine logic and is rejected. No new interface, scheduler, reservations, clock or saved state. Separate JSON documents explicitly select independent geometry, rectangular/painted areas and existing policy bindings through the master loader. The proof module owns only its independent spatial storage/capabilities; gameplay remains in engine managers.

After rollout, the actual2x2 output cannot move under its assigned region: existing TileRegionDef rejects every non1x1 sweep. That owner now checks equal positive full footprints and adjacent anchor displacement, using the complete swept rectangle; long jumps, changing/misaligned footprints and excluded diagonal corners remain refused. Both native and independent providers use those same engine region rules. One authored region initially omitted its producer endpoint and was correctly refused; JSON is corrected instead of relaxing policy admission. Manual held movement deliberately overrides automatic work areas. Tests preserve that existing rule, then release the original home intent and verify real constrained return.

The automatic detour reveals a real fixed-point speed-budget defect. An independent exact squared-raw-coordinate oracle catches65 MoveTowards overspends. Original vector math rounds length downward and signed scaling can enlarge represented displacement. FixVec2 now owns exact raw ceiling length and toward-zero integer displacement; native Mover charges whole segment distance/cost upward conservatively through the same owner. Existing ordinary Length remains available for its prior meaning; no epsilon, float, extra clock or invented movement credit is introduced. Tests now compare exact raw squares, not a rounded distance that could conceal an overspend. Final328/0 also checks least enclosing raw length, zero/negative budgets and exact landing.

Current functional evidence: the same ordinary sixty-unit commitment survives service obstruction and non-origin parking overlap on an independent2x2 space with actual work-area policies. It physically clears, returns home, partially unloads, departs and completes production once;100 finite steel/WIP become41 only through the authored60-to1 conversion. Original job/worker/endpoints/promises survive; claims settle; partial home/unloading saves, pause, replay and separate-session hashes pass. Native and independent area cases reject full-shape narrow/painted bottlenecks, return detached/private full-shaped prospective arrivals, and physically detour an excluded non-origin cell under restored automatic policy, with exact speed budget and saved cursor. Current affected provider fault tests preserve clock/layer/throw refusal, first fault and no healthy checkpoint. Native wide, connected rollout, strict budgets/planned prefixes, manual/automatic area authority and earned home traffic are separately revalidated.

Artifacts: `artifacts/p9-independent-wide/1edbd572d52046b0a7a7168e10214c39/`. Every observation is terminal, untruncated, no timeout/observer error; no live process remains. All attempts retained:

| Attempt | Result | Suite / outer ms | PID / exit | Diagnosis/log |
| --- | --- | --- | --- | --- |
| Independent baseline |0 /1 |183 /316 |27832 /1 |Missing declared rollout, `baseline.log` |
| Shared rollout only |379 /1 |1951 /2083 |27512 /1 |Real wide region unsupported, `run2.log` |
| Full-shape region sweep |420 /2 |2270 /2402 |27932 /1 |Authored area omitted producer, `run3.log` |
| Actual refusal diagnostic |420 /3 |2214 /2346 |28036 /1 |Confirms work.policy-outside-area, `run4.log` |
| Corrected endpoint area |1394 /14 |9412 /9546 |27220 /1 |Test incorrectly constrained explicit manual override, `run5.log` |
| Actual automatic return |1536 /4 |9819 /9955 |8760 /1 |Represented movement overspends, `run6.log` |
| New exact math baseline |227 /65 |12 /131 |28532 /1 |Independent integer oracle, `math-baseline.log` |
| FINAL new independent/area case |1540 /0 |11131 /11274 |26616 /0 |`run8.log` |

Eight target builds, all0 warnings/0 errors: `build1.log`13.28s, `build2.log`12.16s, `build3.log`6.44s, `build4.log`4.70s, `build5.log`4.89s, `build6.log`4.32s, `build7.log`5.30s, `build8.log`6.94s. Same command: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`. Build7 supports the math baseline, not an unreported independent-case attempt. No executable edit after the final build. An import-context patch and an out-of-order documentation-hunk patch failed exact verification before applying; both were reapplied against actual source, not by overwriting user files. Some read-only source lookups guessed nonexistent paths, then used discovered files; they are not runtime evidence.

Every runtime command is `dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "LITERAL" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`. Ten selected FINAL scopes:

| Exact literal | Passed / failed | Suite / outer ms | PID | Log |
| --- | --- | --- | --- | --- |
| Producer home: independent wide space preserves region-bound cargo and native rollout |1540 /0 |11131 /11274 |26616 |`run8.log` |
| Framework: movement vectors preserve exact represented distance budgets |328 /0 |15 /138 |29512 |`final-math.log` |
| Producer home: wide machines retain sixty-unit cargo through occupied berth recovery |919 /0 |4439 /4581 |26756 |`final-native-wide.log` |
| Connected rollout: physical clearance resumes saved WIP without remote birth or duplication |63 /0 |715 /849 |29556 |`final-rollout-physical.log` |
| Connected rollout: prospective footprints, budget and strict authored lane admission |24 /0 |371 /506 |22020 |`final-rollout-planning.log` |
| Independent tile world: native layered routing, physical movement and public commands |82 /0 |230 /366 |18860 |`final-independent-navigation.log` |
| Route areas: physical constrained travel, cargo and saved continuation |291 /0 |3225 /3362 |16360 |`final-route-areas.log` |
| Native traffic: returning home and outbound movement clear adjacent slow approaches |227 /0 |670 /799 |10448 |`final-traffic.log` |
| Route areas: provider contracts, admission and private diagnostics |139 /0 |636 /769 |16692 |`final-area-admission.log` |
| Physical recovery acceptance: loaded cargo survives road removal and finite fuel rescue |1848 /0 |18484 /18611 |5088 |`final-interruptions.log` |

All FINAL child exits0; total5461/0 is an evidence inventory, not full-suite qualification. Original six-row recovery waits keep their512-update/60s/5s bounds. New wide recovery uses the already-declared512 updates; small/manual/area stages128. No bound was increased after a failure. No Oil/business/platform/Draw source edit, completed marine reset, old-header rewrite or Git publication.

### Original P9.acceptance.051 completion audit

| Original obligation / reopened counterexample | Current direct evidence |
| --- | --- |
| Road removal and restored reachability | Original six-row case physically cuts/rebuilds actual layer, retains loaded job and releases claims |
| Finite fuel shortage | Same case physically hauls finite rescue fuel to mobile endpoint, paid movement only, cargo/reservations survive |
| Whole and partial endpoint removal | Same case retains original independent receipt/prefix, refuses undersized replacement and atomically redirects exact residual |
| Equipment breakdown | Same case performs staffed physical repair with a finite spare and elapsed work, original cargo survives |
| Blocked multi-port output | Same case retains WIP/tools/work, requires whole receiving capacity, physically drains each vessel and commits once |
| Later idle-output/service berth deadlock | Native and independent sixty-unit wide flows preserve actual commitment through service/parking obstruction and require physical clearance, saved partial unloading and original departure before completion; current home-traffic regression converges |
| No duplication/orphan claims and recoverable workflows converge | Per-step finite stock/WIP, original identities, exact prefix/residual promises, complete settled claim records and terminal once-only assertions in the current cases |
| Durability of those interrupted flows | Blocked/partial saves, saved twins, sole-clock pause, replay identity and independent sessions exercised directly |

That is the unchanged finite acceptance requirement and the known reopened failure class, not a claim that arbitrary malicious mods or permanent impossible geometry always progress. The incompatible2.647 save is preserved, not resumed; contemporary equivalent engine-owned native/independent flows provide the repair evidence. Broader current Oil/native scenarios, full optional-module durability, performance/soak and full regression remain their original open gates. Operational services/assurance gaps remain at [their existing owner](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage). Only .051 is checked; no other original requirement changes. Next P11.acceptance.035 evidence audit should identify missing optional-module shuffled-input/session/save/twin coverage before implementation or more focused runs. Full goal ACTIVE; commit/push deferred until full goal AND plan finish.

Final identities: coreDLL `6B3A55B692E4EA5E81489EBD79948EB0D848EF33C095DF6DF81C8E361BFC8022`; testDLL `07A9FBC951071A8C327E8E60B5ED0053D9E9E47A80A2753593C26DF9D9C22193`; ExtensionProofDLL `0B9E9342E9B5B631BDDB2B7D265E5476FBC0C7D27566D12C9AA559EE29496AE6`. `final-inputs.json` freezes30 selected source/content/DLL identities with0 post-run drift. `original-gates-current.json` records the exact source-checkbox audit: all221 original texts match once,210 checked/11 open/0 unmatched/0 duplicate, .051 alone closed. Prior checked gates are not claimed runtime-requalified on2.687. Both new literals have exactly one profile entry; the thin positive driver has no direct World/service/state mutation. Source scan confirms one TileRolloutRules/FindMobileRollout implementation, no copied native/independent algorithm. Both repository diff checks pass and staged counts remain0; consumer HEAD unchanged. No executable edit after FINAL. Structural metadata checks do not prove any unrun acceptance gate.

<a id="current-c16w-mobile-footprints-and-wide-recovery--2026-10-21"></a>

## Earlier C16W: mobile footprints and wide recovery — 2026-10-21

Engine change: core2.686.0/save160/replay1. The new master-listed wide fixture initially fails0/1 before initialization with `mobile tile presence uses a one-tile footprint`. This is an actual engine limit, not a Godot type or content shortage. The original declaration now accepts positive dimensions that fit its selected world; no artificial1x1/2x2 cap. All full-shape routing, occupancy, clearance, reach, dock and payload bounds remain inside original engine owners. The existing native and independent spatial contracts are extended/migrated in place. No second motion/route/stock/clock/clearance owner or legacy overload is added; no Oil/business/platform/Draw source change.

The original NavGrid entry reader evaluates all cells under its provider/actor/space observation and uses maximum terrain cost, as unborn rollout already did. Native Mover passes actual actor context, publishes full-shaped prospective/area snapshots, validates full path/presence bounds, and reports the union of full footprints for an in-flight adjacent segment. The existing clearance validator admits dimensions up to actual shape+one step, still rejects malformed or mutating providers. Native TileMap/TilePresence relocate original full occupancy with complete edge checks. ITileNavigationSpace.InteractionGoal explicitly receives actor dimensions; native and independent provider signatures are migrated, with one original adjacency calculation. Dock vacancy uses rectangle overlap rather than testing a single origin cell. Saved layout unchanged, but behavior/handshake identity advances; old images remain preserved incompatible history, never header-rewritten.

`producer-home-wide-berth.json` explicitly declares three2x2 machines,100 finite steel,60 input/1 output, half-minute-per-unit loading/unloading, five-minute recipes, movement speed30, actual road/channel samples and service/departure/parking positions. It is embedded and selected by the existing master declaration through the existing factory, not a runtime fallback. Shared thin player proof now tests pairwise full-footprint disjointness. The wide script loads all60 before later player interference, holds the original job/worker/endpoints/receiving receipt through blocked home/service, physically clears a blocker whose footprint overlaps only parking's non-origin column, saves during real home return and partial unloading, finishes original delivery/departure and subsequent work exactly once. Raw stock+protected WIP stay100 before conversion and41 afterward; claims settle, pause freezes both saved sessions, terminal updates repeat no production, replay hashes and session isolation match. Pure previews refuse a blocked non-origin terrain cell and a full-footprint overflow even when their origin is truly passable.

Artifacts: `artifacts/p9-wide-berth/59c2bc77c3a342b3a92bef1fcbd263ed/`. All attempts terminal, untruncated, with no observer error/timeout:

| Attempt | Result | Suite / outer ms | Child PID / exit | Log |
| --- | --- | --- | --- | --- |
| Baseline, unchanged2.685 production |0 /1, one-tile content rejection |63 /194 |30368 /1 |`baseline.log` |
| First2.686 wide positive, before stronger boundary/partial-overlap probes |915 /0 |4297 /4430 |29288 /0 |`run2.log` |
| FINAL wide2x2/60-unit recovery |918 /0 |4286 /4425 |4764 /0 |`final-wide.log` |
| FINAL affected small native combined flow |633 /0 |2750 /2883 |14000 /0 |`final-small.log` |
| FINAL affected independent clearance guard |22 /0 |236 /369 |28804 /0 |`final-clearance.log` |
| FINAL affected native/independent presence binding |1454 /0 |6970 /7108 |15988 /0 |`final-bindings.log` |
| FINAL affected exact-goal navigation |25 /0 |99 /231 |4008 /0 |`final-goals.log` |
| FINAL original six-interruption recovery case |1846 /0 |18725 /18857 |4636 /0 |`final-interruptions.log` |

Four target builds, `build1.log`6310ms (proof/baseline), `build2.log`6710ms (implementation), `build3.log`6780ms (stronger probes/metadata), `build4.log`6560ms (ensure footprint bounds read inside original observation), each0 warnings/0 errors. Command: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`. The3rd build has no runtime claim. There was no full/solution build or full/default/profile/family suite.

Each runtime uses `dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "LITERAL" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`. Six FINAL literals correspond to the table:

- `Producer home: wide machines retain sixty-unit cargo through occupied berth recovery`.
- `Producer home: occupied parking retains loaded replenishment through saved physical clearance`.
- `Native traffic: independent clearance contracts reject mutation and malformed segments`.
- `Tile presence bindings: independent JSON metadata drives original hauling rollout and construction`.
- `Navigation exact goals: blocked admission preserves guarded costs budgets and region alternatives`.
- `Physical recovery acceptance: loaded cargo survives road removal and finite fuel rescue`.

Self-review: no unguarded footprint read was added before the navigation entry observation; bounds, every covered terrain/occupancy cell and actual clearance share the existing scope. Shape-aware bounds use wide arithmetic and authored world dimensions, not a platform-size limit. Complete-route validation, costs, area sweeps, movement budgets, retained claims, output commit and first-fault rules remain original. Prior tests that rejected any positive mobile width beyond1 are intentionally replaced with native/independent positive admission, while invalid/nonpositive/missing metadata remain rejected and actual oversized unused content is explicitly refused. The1454-assertion affected binding case proves those changes, rather than hiding a former negative test. No reported performance improvement is inferred from timings. Wider operational-services/assurance gaps stay at [their original owner](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage).

Six finals total4898/0 is scoped evidence only. Native wide recovery is not equivalent to every independent spatial implementation or operating-area/fault combination. P9.051 and other original gates remain OPEN; old2.647 boat job was not resumed, completed consumer2.682 marine state was untouched. Next exercise wide movement/retained delivery through the existing independent selected tile-space provider and bounded operating areas without engine-specific host logic. Full goal ACTIVE; commit/push remains deferred until whole goal AND plan finish. Earlier C16B/C16R results below are historical, not current2.686 qualifications.

`final-inputs.json` freezes27 selected source/content/DLL paths,0 post-run drift. CoreDLL `35701A43C5103CF2A222A5410849C5B63062CAA85B6759A19878F8BD916FB920`; testDLL `2E5160920917EEFBF899BC78818528F53D01B22C584128752A8446071C0F6297`; ExtensionProofDLL `648991804CAE1AC1183FC5285345F924DCB403E83B946755F2CB8FFD505AB59B`. No executable edit after FINAL. Fresh exact requirement audit matches all221 original texts once:209 checked/12 open/0 unmatched/0 duplicate; no original closure or wording change. Exactly one wide-case profile entry. Both repositories' diff checks pass, staged counts0, consumer HEAD unchanged. Metadata/source checks do not qualify unrun provider/runtime cases.

<a id="current-c16b-combined-loaded-berth-recovery--2026-10-21"></a>

## Earlier C16B: combined loaded berth recovery — 2026-10-21

FIRST successful combined proof:425 passed/0 failed,3098ms suite/3261ms outer, child26964 exit0, no timeout, truncation or observer error. Only the new literal `Producer home: occupied parking retains loaded replenishment through saved physical clearance` ran. Every physical wait is bounded128 Updates, runtime60000ms/termination5000ms/output30000 characters. Core2.685.0/save160/replay1 and ExtensionProof implementation unchanged; no production engine, Oil, business, host, platform or Draw edits.

The existing producer-home owner is now partial; one file adds public-player commands and immutable facts only. One registration and one existing validation-profile entry select the new case. Pure authoring clones a real native machine as parking blocker, adds actual movement/clearance cells and explicitly sets freight speed6 versus output30. The first five-minute recipe actually manufactures its worker, which physically reaches home. While service is still clear, a normal four-steel haul starts; the player redirects the produced machine into service, physically fills its vacated parking with the blocker, then releases the original home intent. No direct World access or gameplay is implemented by this driver.

Actual job1 retains4 loaded steel and its original worker/source/destination endpoints through service obstruction; output publishes Blocked/work.dock-occupied, delivery publishes Paused/blocker.unreachable. A12-update saved wait retains exact inlet4 and all20 finite steel. Sole-clock pause advances neither copy. Foreign control of the visible owned blocker refuses core.not-owned and preserves hash. A normal clearance MoveVehicle command is accepted but does not instantly arrive. Original WorkBoard/native movement/home/claim/production owners subsequently return the output, retry the same job, partially unload, depart and complete the second five-minute recipe. No substitute haul is accepted by the invariant. Saved copies during actual return movement and actual partial unloading advance to identical hashes. Raw stock plus protected WIP total20 before conversion,17 afterward; source/carrier export promises and producer inlet/actual claim records settle. Twelve terminal updates repeat no output. Replay reproduces the final hash; a separate session remains untouched.

Raw artifacts: `artifacts/p9-loaded-berth/57283ac0801b4f12930d10257c85712b/`. All attempts, including authoring/oracle mistakes, are retained:

| Attempt | Build seconds, warnings/errors | Same literal runtime result | Suite / outer ms | Child PID / exit | Diagnosis |
| --- | --- | --- | --- | --- | --- |
|1 |7.06,0/0 |130 /1 |1819 /1980 |24156 /1 |Obstruction existed before dispatch; no loaded job ever admitted |
|2 |5.87,0/0 |132 /1 |1117 /1273 |14904 /1 |Early supply commitment completed before manufactured output became available |
|3 |6.43,0/0 |132 /1 |1207 /1359 |27308 /1 |Added bounded immutable trace confirms no output during actual committed delivery; not a deadlock |
|4 |6.02,0/0 |138 /2 |1562 /1718 |5316 /1 |Later player interference forms real loaded obstruction, but blocker command correctly refused while fractional output footprint still occupied parking |
|5 |6.08,0/0 |233 /1 |2666 /2823 |25924 /1 |Workflow actually recovers; only test's NotFound expectation wrong for visible foreign ownership |
|6 |6.22,0/0 |425 /0 |3098 /3261 |26964 /0 |Correct refusal oracle plus stronger conservation, same-job, partial-unload, pause and claim-settlement assertions |

Logs are `build1.log` through `build6.log` and `run1.log` through `run6.log`. Builds use `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`. Every run uses `dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Producer home: occupied parking retains loaded replenishment through saved physical clearance" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`. No family/profile/default/full suite or unrelated green case was rerun. No process remains live. Two read-only source lookups guessed nonexistent filenames/wildcard paths, then used discovered source paths; no source was changed by those failed lookups. One documentation patch failed exact-context validation and was reapplied against the actual complete paragraph; no partial executable edit resulted.

Self-review: assertions enforce actual earned work, full footprint clearance, original job/claims, bounded physical movement, immutable publications, privacy refusal, partial saves, sole-clock pause, replay and session isolation. Existing authoritative owners and operational error behavior are unchanged. Wider operational-services/assurance gaps remain at [their existing owner](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage). This is native1x1 combined recovery evidence, not universal deadlock freedom or proof that the old2.647 boat60-unit commitment resumed. Original P9.051 remains OPEN. Next extend this same commitment proof to wide-worker/service geometry and equivalent retained volume through authored contracts; no completed marine reset, header rewrite or re-execution. Original221 requirements/checkboxes unchanged; full goal ACTIVE, no staging/commit/push before full goal AND plan finish. C16R below is earlier evidence, not rerun after this test-only extension.

Final selected identities are frozen in `final-inputs.json` alongside the raw logs:10 selected source/content/DLL paths,0 post-run drift. TestDLL `EBFC396B77D4C93FA048629897F960227DA885664C7AF8C380BED5FD4A0E00CE`; new partial file `6351D6B12418E84878AAC438A047EA649C82CF113396EC5845403D774269FF31`; original producer-home owner `FC26EFB490E94D333F1C134B9C1B48497C51F6BFEA63C5C79BAB70BEBF8B92C8`; existing profile `65BFA9BFC5A96F809131025A7D373040099B9B5BCE64E887348AE999322FF1CA`. Core and ExtensionProof hashes remain exactly those recorded in C16R below. No executable edit after FINAL. Fresh source-only audit matches all221 original requirements once:209 checked/12 open/0 unmatched/0 duplicate. Exactly one new profile entry; no original gate changed. Both repositories' diff checks pass and both staged counts remain0. The thin new file contains no direct World/service/state/identity mutation path. These metadata checks are not runtime coverage of unrun cases.

<a id="current-c16r-explicit-endpoint-scopes--2026-10-21"></a>

## Earlier C16R: explicit endpoint scopes — 2026-10-21

Scope: original P9.acceptance.051; core2.685.0/save160/replay1 unchanged. The six-variant case first fails composition after591 assertions because `PoolPortDef.Choices` and `PoolPort.SelectedEndpoint` hard-code `proof.pools`, while the colony fixture actually binds `colony.pools`. The engine correctly rejects the undeclared provider. No production guard, scheduler, inventory/claim owner, timing, movement or stock amount was weakened or changed.

The existing proof capability now requires JSON `endpoint_scope`, using it in both choices and selected identity. Both original registrations use schema4. All23 JSON documents and three embedded authoring sites were migrated explicitly, no default/compatibility resolver. Two negative assertions in the existing port-selection case prove missing and unselected scope refusal. Positive physical proofs use both actual provider namespaces.

Raw artifacts: `artifacts/p9-recovery-revalidation/22564ca9a0034639a31e3508a705ca4f/` in the framework repository. All process observations were terminal, untruncated and without observer error; no timeout or live process remains.

| Attempt | Result | Suite / outer ms | Child PID / exit | Log |
| --- | --- | --- | --- | --- |
| Existing recovery baseline |591 passed /1 failed, actual provider validation |9619 /9803 |2112 /1 |`baseline.log` |
| Target build after explicit scope migration |0 warnings /0 errors |9650 |exit0 |`scope-build.log` |
| Same six-variant recovery case |1846 /0 |29783 /29936 |26500 /0 |`scope-recovery.log` |
| Target build after two negative selection assertions |0 warnings /0 errors |8980 |exit0 |`guard-build.log` |
| Affected resource-port selection |353 /0 |4382 /4534 |16668 /0 |`port-selection.log` |
| Idle manufactured output and physical replenishment |57 /0 |1488 /1676 |13592 /0 |`berth-replenishment.log` |
| Earned home-return/outbound traffic |227 /0 |764 /921 |4540 /0 |`home-traffic.log` |
| Occupied parking, route and area saved recovery |79 /0 |352 /503 |28024 /0 |`occupied-parking.log` |

Each runtime uses `dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "LITERAL" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`. Exact literals, in table order:

- `Physical recovery acceptance: loaded cargo survives road removal and finite fuel rescue` (baseline and repaired six-variant run).
- `Resource port selection: engine-owned controls preserve live physical claims across native and point restoration`.
- `Producer home: idle output clears service access for physical replenishment`.
- `Native traffic: returning home and outbound movement clear adjacent slow approaches`.
- `Producer home: occupied parking and operating areas retry through physical saved movement`.

Both builds: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`. No default/profile/family/full suite, consumer build/runtime, business/platform/Draw change or publication. Total positive2562/0 across five literal scopes is an inventory of evidence, not a substitute for the requirement's scope. The second build changes only the two negative assertions in the other case; the already-green recovery case is not rerun routinely. Recovery-run testDLL SHA256 `2F1E380BF55E5DA911DA1C4F378CBC73299A23C5F56595CE1A08AD1981D104C1`; final testDLL `510F7D66F48598D5DDAB76477C4459E4E38A2F586CD9153147442F88FB04B155`. ExtensionProof unchanged across both builds: `C343222D1EFC3ACD65490515A9B01B3110AB35EDDEF42157B2ADF3CD59E8384F`. CoreDLL unchanged: `7B0034319618A996C499089774D3AD57F4C36F23D1599FCFFFDB57E4D436E56F`. The first identity query guessed the wrong ExtensionProof target folder and failed read-only; the actual netstandard2.1 output was then discovered and hashed. No build/runtime was restarted because of that lookup.

Self-review: one contract implementation owns its declared endpoint scope, selection and alias resolution; independent endpoint books and authoritative resource transactions remain original owners. Finite cargo/fuel/spares, actual physical work, partial claims, whole output commit, saved twins, replay and isolation remain tested. The current six-row count differs from historical1868 because elapsed workflows/assertion repetitions changed over many engine revisions; no original recovery assertion or bound was removed. The shared operational-services/assurance gaps remain tracked in [the existing owner](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage), not claimed globally resolved by these proof-only changes.

Original P9.acceptance.051 stays OPEN: these cases do not establish the old2.647 boat60-unit commitment or universal congestion/provider recovery. Next engine-only proof should combine occupied producer parking, retained loaded replenishment and subsequent saved physical clearance, rather than treating independent green rows as proof of that interaction. Completed consumer2.682 marine artifacts remain untouched/historical; no reset/header rewrite/re-execution. Full goal ACTIVE; no original checkbox or requirement wording changes, and no staging/commit/push before the full goal and plan finish. All2.425-era closure statements below are historical and superseded by the reopened parent gate.

Post-run read-only reconciliation: all221 original requirement texts match exactly one current owner checkbox,209 checked/12 open/0 unmatched/0 duplicate; no original closure. Parsed all72 pool ports in the23 migrated JSON documents: every explicit scope matches the actually selected provider namespace. `final-inputs.json` records36 selected source/content/DLL identities, all36 still match after documentation edits; this is a scoped freeze, not an inventory of every repository input. Both repositories' diff whitespace checks exit0 and both have zero staged paths. Existing dirty work and consumer HEAD remain preserved. These structural checks do not imply runtime qualification of the other migrated authoring consumers or broader/full regression.

## Historical 2.425 continuation: physical multi-port output blocking

FINAL1868 passed /0 failed /0 aborted in45812ms. Child PID21352 exited0, observed46140ms; stdout/stderr untruncated, no observer error or timeout. SAME existing exact case and registration,60000ms outer/5000ms termination/30000character output cap and512Update per physical stage. All six physical road/fuel/whole-endpoint/partial-endpoint/breakdown/output variants executed. One same-case targeted dependency compile succeeded with0 warnings/0 errors in17.19s; build handle31210 observed terminal before runtime handle53565 started and was observed terminal. No second solution compile or other runtime. No executable edit after FINAL.

The native processor's first six-minute batch fills two independently owned two-unit vessels. Its completed second batch retains six earned minutes, same production execution, two physical steel in WIP and the same reusable one-steel tool receipt. Eight blocked updates preserve them. The real transporter hauls one product, then its remaining product: incomplete capacity and one entirely freed port cannot commit a partial second result while byproduct remains full. Physical byproduct loading/hauling clears the other vessel; existing whole conversion commits both second products exactly once, consumes WIP and releases tool claims. Original output book aliases are explicit; actual named source ports remain in jobs. Terminal cargo/export/inlet promises release. Per-resource journal accounting reconciles25 finite steel with raw/WIP/consumed inputs, and each produced product with vessel stock/loaded cargo/delivered stock. Later updates repeat no completion.

Pending/running/blocked/partially cleared/loaded/terminal saves and sole-clock pause, exact twin hashes, detached retained output publications, foreign output route refusal with no metadata, replay and independent session isolation pass. No money, purchase or resource grants, no instant proof movement command. Four proof executable files changed: driver, pure JSON composer, explicit embedded-document inventory and one new physical-recovery-output.json document. No engine source, scheduler/provider/ledger/clock/payload change; native gameplay owners already support this row. Core2.425.0/save154/replay1 unchanged.

Completion audit: original P9.acceptance.051 requirement explicitly names endpoint/road removal, fuel, breakdown and output blocking recovery without duplication/deadlocks. All named interruptions now execute real physical workflows in the same bounded six-variant case and converge with conservation, saved continuation, once-only completion and settled claims. Its original requirement text is preserved and checkbox alone is checked. Original P9.acceptance.052 separately requires queueing and paused plans; this proof does not establish those, so it remains OPEN. P9/full P1-P12/publication remain OPEN; original221/162checked/59open, additional140. Historical full regressions are not claimed current.

Metadata validation: post-FINAL freeze2026-10-10 06:45:52UTC records649 selected executable inputs; three existing selected files changed and one new JSON document selected.1317 source anchors plus13 quickstart hashes refreshed, zero mismatches;2570 indexed paths/888 Markdown/7713 local links, zero broken links;18 five-column master rows, zero malformed. Original221 requirement texts preserved; only .051 checkbox changed to checked;204 line anchors refreshed. git diff --check exit0 with34 existing line-ending warnings; thin-driver mutation matches0. Post-FINAL executable drift0; dirty2560 paths/staged0. Protected clock and all three G19 catalog hashes unchanged. Lifecycle skill keeps the completed literal interruption requirement separate from still-open .052/full-plan validation; no broad/old/runtime or publication inferred.

## Historical continuation: native equipment breakdown and physical repair

FINAL1491 passed /0 failed /0 aborted in37383ms. Child PID38404 exited0, observed37724ms; stdout/stderr untruncated, no observer error or timeout. SAME existing exact case and registration,60000ms outer/5000ms termination/30000character output cap; every physical stage is bounded512Updates. All five scenarios now execute: road loss, fuel exhaustion, whole endpoint loss, partial endpoint loss and actual native damage response. One targeted dependency compile succeeded with0 warnings/0 errors in26.70s, handle25245 observed terminal before runtime handle94250 was started and observed terminal. No second solution compile or other runtime was invoked. No executable edit after FINAL.

The loaded truck travels before an ordinary RaiseIncidentCommand activates its authored incident. The existing incident signal gates native worker/motion execution. Twelve blocked updates with held crane preserve the same job, cargo and inlet receipt without productive repair or free motion. Releasing the real crane produces travel to the mobile service dock, six productive game minutes, consumption of the sole onboard spare through native requirements and ResourceFlow journal, episode resolution, dock departure and once-only original delivery. Finite20steel/72fuel/one-spare conservation, pending/blocked/partial/terminal saves and pause, twin hashes, foreign incident command refusal/private facts, replay and independent session isolation pass.

Five proof executable files changed: thin driver, pure JSON composer, embedded-resource inventory, existing physical scenario JSON and one new physical-recovery-breakdown.json document. No production engine changes, new subsystem/provider/ledger, stock grants, host gameplay rules, privileged instant repair, commerce or platform behavior. Native incident/work/execution/dock/inventory owners already support this row. The explicit truck storage capacity is100 to accommodate32fuel+one spare+eight cargo; this is authored content, not an engine size limitation. The spare is protected by the native outlet reserve. Core2.425.0/save154/replay1/payload layouts unchanged. Original221/161checked/60open unchanged; additional139. Output-blocking/full matrix/.051/.052/P1-P12 remain OPEN. Commit/push remains conditional on actual full completion.

Metadata validation: post-FINAL freeze2026-10-10 06:35:38UTC records648 selected executable inputs; four existing selected inputs changed and one new JSON document selected.1316 source anchors plus13 quickstart hashes refreshed, zero mismatches;2569 indexed paths/888 Markdown documents/7683 local links, zero broken links;18 five-column master rows, zero malformed. Original221 gate requirements/checkboxes unchanged,204 line anchors refreshed. git diff --check exit0 with34 existing line-ending warnings; thin-driver prohibited mutation matches0. Post-FINAL executable drift0; dirty2559 paths/staged0. Protected sole clock and all three G19 catalog hashes unchanged. Only one targeted compile and the one existing bounded runtime executed; historical stages/full suites were not independently rerun or inferred complete. The lifecycle skill keeps this partial proof separate from full acceptance.

## Historical continuation: whole and partial physical endpoint loss

FINAL1147 passed /0 failed /0 aborted in12321ms. Child PID6824 exited0, observed12508ms; stdout/stderr untruncated, no observer error. Same existing exact case, same60000ms timeout /5000ms termination /30000character output cap /512Update physical-stage limit. Four scenarios now run: road removal/rebuild, finite fuel rescue, whole cargo-return endpoint-site loss and partially delivered endpoint-site loss. No new case registration or other runtime was invoked; no executable edit after this continuation pass.

The existing native recovery system successfully retained cargo, same job/carrier, original surviving receipt and real delivery prefix after timed site demolition. A three-unit alternative could not reserve a fragment of a larger remaining load. The native destination-unavailable wait survives save/pause/twin continuation. Expanding the ordinary stock band and physically clearing the demolition crane from the only road admits one exact residual replacement, releases the old receipt and physically delivers the remainder without repeating the original prefix. Original and replacement endpoint books plus actual inventories conserve20steel; terminal claims release. Foreign endpoint diagnostics refuse without facts. Replay and session isolation pass.

This continuation changes five proof executable files: the thin driver, pure scenario composer, existing Colony content registration for the existing PoolPortDef, explicit embedded-document inventory and one new JSON document. It adds no engine scheduler, pool provider, ledger, alias-switch control or host gameplay logic. Existing native pool books and recovery contracts supply behavior. Core2.425.0/save154/replay1/payloads unchanged in this continuation. Local-inventory and active-dock demolition guards are preserved.

### Every continuation compile and runtime attempt

All six compiles were same-case targeted dependency compiles; there was no second solution compile or broad runtime. Attempt2 overlapped a prematurely started stale-source runtime and is not accepted as corrected-source verification. Its live handles were observed to terminal before rebuilding and rerunning; nothing was restarted on a timeout or missing observation.

| Compile | Exit / warnings / errors | Seconds | Observation |
| --- | --- | --- | --- |
| 1 | 0 /0 /0 | 19.75 | Live handle6570 observed terminal |
| 2 | 0 /5 /0 | 20.35 | Live handle45151 observed terminal; file-copy retry warnings due overlapping owned child41804; terminal warning output truncated, summary observed |
| 3 | 0 /0 /0 | 1.72 | Clean compile after both overlap handles ended |
| 4 | 0 /0 /0 | 6.96 | Authored site inventory correction |
| 5 | 0 /0 /0 | 6.78 | Explicit automatic-logistics eligibility correction |
| 6 | 0 /0 /0 | 5.99 | FINAL crane route-clearance command added |

| Runtime | Passed /failed /aborted | Suite ms | Child PID /exit /observed ms | Cause / repair |
| --- | --- | --- | --- | --- |
| 1 | 591 /1 /1 | 8100 | 41968 /1 /8304 | Bound construction stages may not declare standalone final effects; construction retains completion ownership |
| 2 | 591 /1 /1 | 8786 | 41804 /1 /9358 | Premature overlap loaded previous proof assembly; repeated old authoring refusal, not corrected-source evidence; live handle64693 observed terminal |
| 3 | 591 /1 /1 | 6713 | 17708 /1 /6883 | Site prototype lacked explicit empty authored inventory; added native empty storage |
| 4 | 1142 /1 /1 | 8792 | 35056 /1 /8959 | Pools not opted into automatic logistics discovery; explicitly authored public logistics eligibility, without exposing foreign private diagnostics |
| 5 | 1258 /1 /1 | 13524 | 28556 /1 /13686 | Physical demolition crane blocked only road to alternate; ordinary player relocation required; live handle38683 observed terminal |
| 6 | 1147 /0 /0 | 12321 | 6824 /0 /12508 | FINAL whole/partial endpoint loss plus refreshed road/fuel rows pass; live handle83744 observed terminal |

Every runtime exited; no timeout, truncated output or observer error. Aborted classification is one for a thrown suite. Earlier initial evidence below is historical, not a claim that its frozen source remains current.

Continuation read-only metadata:1315 whole-file source anchors /13 quickstart anchors, zero mismatches;221 original gates,204 line anchors refreshed, zero requirement/checkbox changes. Indexed2568 paths /888 Markdown files /7653 local links, zero broken links;18 master rows, zero malformed. git diff --check0 with34 existing line-ending warnings; zero forbidden driver-boundary matches. Freeze2026-10-10 06:21:21UTC /647 selected inputs; five proof executable files edited/new, four existing selected inputs changed and one new JSON input selected. Post-pass executable drift0; dirty2558 paths /staged0. Sole clock and all three protected G19 hashes unchanged. No broad runtime or publication. Original221 /161checked /60open unchanged; additional138. Equipment incident repair, blocked output, full matrix and original P9.acceptance.051/.052/P1-P12 remain OPEN. Publication remains deferred.

## Historical initial road/fuel checkpoint

Scope: only the newly registered exact case `Physical recovery acceptance: loaded cargo survives road removal and finite fuel rescue`. The complete matrix is still in progress. Original P9.acceptance.051/.052 and full P1-P12 remain OPEN; original221 /161checked /60open unchanged. No commit or push.

Engine build2.425.0 / save154 / replay1. Gameplay logic lives in native SimEngine classes, not the driver or pure JSON composition helper. Native mobile docks, existing work claims, original navigation, finite stock, operating input draws, loading/unloading, staffed road removal and line construction are exercised through public commands and immutable views.

## Final observation

FINAL591 passed,0 failed,0 aborted in6555ms. Child PID33440 exited0, observed elapsed6775ms. Stdout/stderr untruncated; no observer error. Timeout60000ms, owned termination5000ms, output cap30000characters; each physical stage is bounded to512Updates with job/clock diagnostics. No executable edit after this pass.

The two scenario variants verify actual loaded eight-unit hauling through physical road loss/rebuild or fuel exhaustion/rescue. The latter physically loads and delivers30fuel from the finite40-unit depot. All steel/fuel inlet and export promises release; destination8/source12/carrier0 steel settles once. Journal consumption plus actual remaining fuel equals initial72 or42. No purchases, creation or production rescue grants. Partial/pending/terminal saves, pause, twin hashes, replay, foreign command refusal and independent session isolation pass.

Mobile service is held through partial fuel delivery by the existing dock claim; it releases naturally on departure. A real authored road at the map boundary is present, but navigation refuses a vehicle origin whose declared dock slots would fall outside the space. Refusal is pure. This is scenario geometry, not an engine map-size limitation.

## Compile observations

One solution compile followed by same-case targeted repair compiles; all exited0 with zero warnings/errors. No family or broad runtime suite was invoked.

| Attempt | Scope | Elapsed seconds |
| --- | --- | --- |
| 1 | Solution, live handle17196 observed terminal | 18.31 |
| 2 | Same-case targeted dependencies, live handle61881 observed terminal | 15.45 |
| 3 | Same-case targeted dependencies | 7.76 |
| 4 | Same-case targeted dependencies | 9.05 |
| 5 | Same-case targeted dependencies | 9.10 |
| 6 | Same-case targeted dependencies | 8.79 |
| 7 | Same-case targeted dependencies | 6.19 |
| 8 | Same-case targeted dependencies | 8.68 |
| 9 | FINAL same-case targeted dependencies | 6.79 |

## Every same-case runtime attempt retained

`aborted` is one when the driver suite threw; the runner itself prints only passed/failed. A failed assertion-only attempt is not an aborted suite.

| Attempt | Passed / failed / aborted | Suite ms | Child PID / exit / observed ms | Cause / repair |
| --- | --- | --- | --- | --- |
| 1 | 0 /1 /1 | 66 | 28828 /1 /257 | Explicit embedded JSON registration missing; added to existing project inventory |
| 2 | 0 /1 /1 | 253 | 41668 /1 /453 | Authoring named an undeclared blocked state; selected the explicitly authored stopped state |
| 3 | 0 /1 /1 | 210 | 5968 /1 /374 | Native docks rejected mobile recipients; extended existing dock/navigation/motion contracts |
| 4 | 8 /3 /1 | 554 | 41412 /1 /727 | Initial fuel classified as residual cargo; authored an existing native protected fuel reserve |
| 5 | 57 /1 /1 | 1421 | 14924 /1 /1617 | Native validation discarded original haul on road loss; routed configured blockers through existing retry owner |
| 6 | 59 /1 /1 | 1429 | 26644 /1 /1613 | Resource-flow view was not selected; explicitly selected existing module for conservation observations |
| 7 | 136 /1 /1 | 1767 | 1888 /1 /1984 | Real native line completion left active plan alias transiently terminal; immediate existing link settlement |
| 8 | 562 /2 /0 | 6496 | 25184 /1 /6675 | Driver assumed zero stock immediately meant fuel shortage; switched to actual next-interval admission |
| 9 | 591 /0 /0 | 6555 | 33440 /0 /6775 | FINAL road/fuel rows pass, service hold and geometry-bound refusal added |

All attempts exited; none timed out, none had truncated output or an observer error. Only this same new exact case ran.

## Ownership and remaining work

The lifecycle skill informed the staged matrix and explicit partial/full evidence distinction; it did not expand runtime scope. No platform/domain/Tycoon implementation or protected clock edit. Saved state layouts unchanged; simulation behavior requires the new build identity, without a legacy save compatibility shim.

Endpoint-site removal with surviving external promises, native equipment incident/physical repair, blocked native output with physical unloading, and full cross-cutting coverage are still required by the same original gate. Road/fuel success is not full industrial acceptance or general deadlock proof. No original checkbox changed.

Read-only metadata validation:1314 whole-file source anchors and13 quickstart anchors, zero mismatches;221 original gates with204 line anchors refreshed and zero requirement/checkbox changes. Indexed2567 paths /888 Markdown documents /7624 local links, zero broken links;18 master rows, zero malformed rows. git diff --check exited0 (34 existing line-ending warnings); zero forbidden driver-boundary matches. Executable freeze2026-10-10 06:04:59UTC contains646 selected inputs:16 executable files edited/new and7 newly selected inputs (3 new executable files,4 existing inputs). Post-pass executable drift0. Dirty2557 paths /staged0. Sole clock and three protected G19 files retain their exact previous hashes. No broad runtime or publication.

[Stage](../../plans/enhancements/P9_PHYSICAL_RECOVERY_ACCEPTANCE_DESIGN.md), [master tracker](../../plans/enhancements/MASTER_TRACKER.md), [completion audit](../../plans/enhancements/COMPLETION_AUDIT.json).
