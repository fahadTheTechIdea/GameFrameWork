# P3-LC1 — Native dense layer checkpoint cost verification

Date2026-10-16. Engine2.640.0/save160/replay1. Original acceptance gates are not
closed by a local codec optimization or a partial harbour observation.

## Production change and unchanged invariants

MapLayer<T>.ValidateValues remains sole native sparse validator. Non-quantity
chunks now check nonempty storage once and scan only actual edge padding. Complete
chunks have no padding; there is no other applicable non-material value legality.
The quantity branch is unchanged. SyncValues still hashes/encodes EVERY cell through
the original IStateSync, including provider observations. No cached hash/validated bit,
new layer/state/clock owner, skipped guard, compatibility path or platform dependency.

Null/wrong shape, empty chunk, unstable keys, out-of-space chunks, partial padding,
quantity legality, failed generation, in-flight checkpoints, exhausted counters and
private viewer invalidation remain covered by the original focused cases. Native
edge dimensions include int.MaxValue squared sparse storage; no dimension cap added.

## Baseline and exact framework checks

Extended the existing `Spatial layers: canonical sparse storage and generation`
case with dense96x96 byte, signed int, signed fixed and resource-associated fixed
layers.256 complete native layer hashes after16 warmups; last-cell changes must
still change the full hash, hashing must retain exact current encoded bytes, and
normal saved twins continue identically. This codec fixture is not the marine game.

Baseline2.639: child17400, exit1,190 passed/6 failed,621ms/outer748ms.
All six failures were old empty-layer length expectations4 versus actual8. Current
native encoding already writes sparse chunk count AND resource-binding count via
SpatialResourceCellBindings.StageSync; no production codec change caused these
failures. The test oracles now assert those two actual section counts.

Final2.640 same literal: child10548, exit0,196 passed/0 failed,400ms/outer525ms.
Final material case `Spatial resource values: catalog legality and atomic native generation`:
child33900, exit0,108 passed/0 failed,106ms/outer231ms. Its first attempt19544 was
102/6,106ms/outer233ms, with the same stale empty-length oracle across six quantity
types; corrected in its existing shared helper, production quantity branch untouched.
All output untruncated. Two final distinct framework cases304/0, not a full profile.

| Dense layer | Original ms | Updated ms | Reported layer hash (unchanged) | Encoded bytes (unchanged) |
|---|---:|---:|---|---:|
| terrain byte |55.438|12.299|B453ABBA4DCDC625|9656|
| strata signed int |96.725|57.258|D9D3A366F98C4AC1|37160|
| elevation signed fixed |103.525|56.254|43E338DD3C2392C1|74024|
| lake associated fixed |103.362|56.252|7540F1A78F2392C1|74024|

One baseline/final measurement pair, not statistical benchmarking. Byte run about4.5x
faster, the four totals about1.97x; neither figure is a whole-engine speedup claim.
Reported hash/length equality is measured; cross-build complete native save bytes
cannot be identical because the required engine-build header changes.

## Builds

Targeted Release framework test builds, --no-restore -v minimal:4.57s baseline,
6.99s production change,4.55s resource-oracle repair. All exit0, zero warnings/errors.
Actual consumer OilCo.PublicProof.csproj Release build1.56s, exit0, zero warnings/errors.
No solution/full/profile/family test run. No unchanged canonical case rerun after the
other test file's oracle repair; its source body/core binary were unchanged.

## Canonical actual-consumer observation

Build identity increments per native policy.2.639 saves remain untouched and
incompatible, not header-rewritten or migrated. Therefore explicitly prepare the
SAME96x96 Gulf seed1/two-human/seat1 fixture on2.640, not search for another world.
Original durations/quantities/geometry/content unchanged. Its actual yard[71,74],
offshore field21[58,78],14 candidates and minute11 game hashC68151411C737F1C exactly
match the original2.639 preparation; no fallback or easier world was substituted.

Same literal marine case with --case-input prepare: child26260, exit0,24/0,
7440ms/outer7534ms. Input native save:
artifacts/marine/6d55aa93e27c49009f92048cb213ee8e/game.sav,32931bytes,
SHA256F4D4633460330192FDD45124E240D395A3E7D74A97F710693D74A1B0FDDDD47D.
Its manifest is224bytes, SHA256CE1FD329663D6C0BB428AEDD9F4C7FB952020280A22424E8E22EB3468A6E6DAD.

Same literal with harbour:<that checkpoint.json>, unchanged120s observation threshold,
180s process cap/5s termination/16000 output chars: child10756/session98794, exit0,
22/0,116006ms/outer116106ms, untruncated. Actual one-day observation advanced all1440
normal minute Updates to1451, no fault. Shipyard erect5/3600 and crane working;
depot clear406/480 and bulldozer working; grid clear waits for the same worker.
All original pending sites retained. Native memory/disk/no-mutation hash checks pass.
Printed115120ms includes checkpoint publication. This is HARBOUR PARTIAL, not all
facilities completed or the default full marine workflow passing.

One live stack sample from10756 now showed native MapLayer<byte>.Sync under spatial
observation inside layered navigation cost→NavGrid.EntryCost→PathSearch.Find→physical
haul pickup. The redundant validation loop was not the only cost. This is a sampled
remaining path, not a measured percentage attribution or authorization to skip guards.

First one-day output:
artifacts/marine/8fcb930788a5455ead2cd94243a4d4a6/checkpoint.json,
native game hash192A48D81857AC0D; game.sav134238bytes,
SHA2562998141F3D758D7300E57819B9F855860A0D2FCB251254196BCC5831EC473DED.
Manifest215bytes, SHA256F136E710483337FB60A3C4152359BDD3B7B984E3ADFE0D4A8BF2694262CB6FEF.

Next native day uses that actual progressed2.640 checkpoint, not recreated setup.
Child33236/session92040 TIMED OUT at the unchanged180s outer cap, exit-1,
elapsed180036ms, untruncated. It logged healthy resumed minute1451, then no further
completed stage result or new checkpoint. No assertion totals or final game minute
are available for this terminated attempt; do NOT count it as22/0 or completed work.
An attempted later stack read found the child already absent, then the exact existing
session confirmed TimedOut. No restart or larger deadline was attempted. Thus the
native codec improvement is verified, but does NOT resolve all canonical work cost.
Latest resumable current-build artifact remains8fcb930788a5455ead2cd94243a4d4a6.
Bounds/rules unchanged; no executable edits after the final framework/runtime checks.

## Current artifact identities

444 core C# path/content fingerprint:
831a7fb6c5b065e783afbc1803c22cf4cc701d309bf53353ae85a59dd2f81543.
Framework and actual consumer engine DLL:
E1F7821959D4DC24D7596987A58CE256CE3ABAF867A03DB0CF7E0B90E63DBCBE.
Latest framework test DLL62DD3343B4B7C20E9B1C3A59B0D2C6B5E551903BA42A386F64B6EB5ACBF381C7;
actual proof DLLDD4EA985B58DC6899B7751C69918362615FDD5F0803D12E9B50A3F87CFCEFCB8;
recompiled Oil game DLL4577CA8B2D66E513869C9D0322A5667A1BC94CB7CDC8F8664243656E13F21554
(Oil production source unchanged; referenced engine identity changed).

TileMap.cs2E01F3CAC09F41FF4033B84FA0B3CAF347A176A116F9C0D7880C37566F236143;
SpatialLayerTests.cs88EF6337DFE4C7744ED35A4BE1CF98823EC0C85815D0AD52ACDE6B41D4836C29;
SpatialResourceValueTests.cs712C9DD37F20927C9CDE9D06EE87BF72CF882C9BC150FF345E9FF1504EC05A84.
These are current artifacts, not retrospectively captured baseline test DLL identities.

## Limits and next work

Original221/210 recorded checked/11 open; full goal remains OPEN. No original acceptance
checkbox changed. Harbour/fleet/survey/drilling/delivery/subsea/full scenario gates,
negative arbitrary-provider performance and broad regression remain unproven.
Continue actual healthy harbour checkpoint; measure remaining navigation observation
cost before any contract refactor. Never narrow the original work or bypass full
mutation detection. No business/JSON-content/Draw/platform changes or publication.

Source investigation after the timeout: existing NavGrid.EntryCost wraps actual
rule/actor/space reads; native LayeredTileNavigationDeclaration.Cost evaluates each
condition through TileConditionContracts. Physical LogisticsService.CheckPhysicalContext
rechecks current IWorkerDispatch.CheckJob before each earned pickup/delivery. Preserve
these contract/eligibility checks when measuring the repeated route/observation cost;
do not infer that dropping guards or caching arbitrary mutable-provider results is safe.

Final hygiene: git diff --check exit0 with35 pre-existing LF conversion warnings and
no other diagnostics; new stage-local links valid. No staged files in either repo,
no live focused test workers, latest native checkpoint manifest unchanged. No commits
or pushes. Original221/210 checked/11 open was re-counted from original requirements.
