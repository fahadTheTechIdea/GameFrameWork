# P3-PC1 — Spatial proximity contract and physical work verification

Date2026-10-16. CURRENT engine2.642.0/save160/replay1;2.641 intermediate evidence
retained below. Full goal and original broad gates OPEN.

## Diagnosis from actual current work

Reproduced only the actual2.640 progressed native harbour input
artifacts/marine/8fcb930788a5455ead2cd94243a4d4a6/checkpoint.json, minute1451.
Same original marine literal,180s process/5s termination/16000 chars, no build or altered
input. Child6444/session49502 TIMED OUT, exit-1,180042ms, untruncated, no new checkpoint.
No runtime assertion total/final game minute is known. Existing process was confirmed
live before both stack samples; the same handle was polled to its authoritative terminal
result. No observation timeout was mistaken for termination or used to restart it.

First live sample: full spatial hashing→NavGrid.EntryCost→PathSearch.Find→Mover.CanReach
→SpatialInteraction.Check→WorkBoard.PreviewDock→frontier assignment. Second: full spatial
hashing→layered cost→NavGrid.EntryCost→PathSearch.Find→Mover.CanReach→WorkSystem.CheckAssignmentCore
→RunJob. Source confirms the latter caller only distinguishes WithinReach. Prospective
dock/candidate/approach queries actually require reachability and are NOT removed.
These are call-site samples, not statistical percentage attribution.

## Engine change

Mandatory CheckProximity extends the existing ISpatialInteraction; typed WithinReach,
OutsideReach and Unsupported results never promise a route. Existing SpatialInteraction
is sole dispatch owner, TileInteractionRules sole shared tile geometry owner, PointSpace
sole independent point geometry owner. TileMap and independent SensorTileSpace delegate
to the shared tile implementation; no concrete provider branch or default shim.

The proximity dispatch checks health before early returns and observes provider, full
actor/target entity state, selected space and clock using the original EntityObservation.
No hash cache/exemption, guessed geometry, parallel route/state/clock or host rule.
Only physical contact comparisons migrate: assignment's already-at-stop check, fixed-work
contact and undocked physical delivery occupancy. Full route/area/candidate/dock admission,
Approach, travel and material handling remain authoritative and unchanged.

The affected nonspatial test found a real pre-existing WorkSystem.RefreshSupply null
dereference: its material-free automatic plan runs with no logistics service, as startup
validation permits, but empty withdrawal used _supply unconditionally. Original supply
owner now withdraws nothing for no-input/no-service composition; actual material inputs
still require IWorkSupplyRequests or fail. No invented inventory or fallback supplier.

## Intermediate2.641 focused checks and every attempt

Targeted Release framework test builds --no-restore -v minimal:
13.93s initial,6.79s first diagnostic,4.92s original-fault Detail diagnostic,
6.72s supply fix,4.64s exact refusal-oracle correction. All exit0, zero warnings/errors.
Actual consumer OilCo.PublicProof Release build1.59s, exit0, zero warnings/errors.
No full/profile/family/default full marine run.

New literal `Spatial interaction: guarded proximity is independent of route reachability`:
FIRST child29664,74/0,495ms/outer624ms; FINAL child14704,74/0,506ms/outer642ms.
Exact90s bound/5s termination/12000 chars. Existing point reachability hook proves zero
full-route calls during repeated proximity AND ordinary CheckAssignment; explicit full
query still executes and refuses closed access. Pure reads/save/hash/private entity
identity, ordinary native travel/saved twins and seven provider faults exercised:
actor/target Touch, private target-position mutation without Touch, private space access
mutation without revision, clock pause, original throw and target mutation+original throw.
Every failure faults, refuses save and future invalid reads, and stops Update. Original
callback exceptions are retained; independent session healthy. Native and point runtime
coverage; SensorTileSpace's delegate is compile-verified, not a separate new runtime matrix.

Existing literal `Optional space: stationary standard work uses contracts and survives saves`:
FIRST child11876,9/3,149ms/outer277ms: old assertions masked the actual fault with
an empty-snapshot index error. SECOND8728,8/1,149ms/outer284ms revealed native sim.work
NullReferenceException. THIRD22980,8/1,177ms/outer340ms printed original Detail and identified
RefreshSupply line2173. FOURTH29752,19/1,241ms/outer370ms: production null fault fixed,
only stale NoWorker refusal expectation remained. The real worker exists but unrelated
nonspatial target is unreachable; update exact oracle to original BlockerUnreachable.
FINAL27700,20/0,229ms/outer359ms.30s bound/5s termination/10000 chars throughout.
Retains full three-minute physical work, authored processing/idle entity states, immutable
nonspatial snapshots, saved continuation and refusal of remote instant work. New assertions
cover actual nonspatial self-contact and unsupported unrelated geometry. No map invented.

Existing literal `Itinerary admission: permitted alternatives, physical queues and saved execution`:
FIRST/FINAL34356,218/0,4707ms/outer4837ms,90s/5s/12000 chars. Original real native/point
physical itinerary/controlled travel, areas, finite cargo/queues and saved execution.
All final output untruncated. THREE distinct final framework cases312/0. Do not add
earlier attempts or call this full regression.

## Intermediate2.641 canonical scenario

Old2.640 native artifacts preserved/incompatible by normal strict build identity; no
header rewrite or migration. Explicit SAME96x96 Gulf seed1/two humans/seat1 on2.641:
yard[71,74], offshore field21[58,78],14 native suggestions, original finite stock,
worker roles and full durations. Prepared minute11 hashC68151411C737F1C matches BOTH
prior engine builds. Same original literal prepare: child21820,24/0,6349ms/outer6447ms.
Current-build input artifacts/marine/b3c3332e3a5043c3b0127054f9375ea3/checkpoint.json.

First saved-day continuation: child35232/session76128,22/0,47257ms/outer47357ms,
printed stage46369ms including save/load publication. Native1440-minute advance to1451,
hash192A48D81857AC0D EXACTLY matches the prior2.640 one-day result. Same original5/3600
shipyard erection and406/480 depot clearing; no changed work, geometry, supplies or policy.
Observed outer116106→47357ms for this SAME state transition, not a universal engine
speedup claim.120s observation threshold/180s process/5s termination/16000 chars unchanged.
No fault, native memory/disk hash identity retained. Still HARBOUR PARTIAL.
Output artifacts/marine/d32393f94fb84b39a7ecda844dc74ee1/checkpoint.json.

Second2.641 saved day uses this actual output, not recreated setup. Child35304/session23377
exited1,20/1,141971ms/outer142071ms.120s stage threshold failed, but healthy native boundary
was saved at1931/hash49513CD595B257E8, artifacts/marine/01c7589dfcad4f799846513200dcf966.
Shipyard485/3600, depot clearing complete/erect waits for busy crane, grid clearing
complete/erect waits for stock, trucks carrying8/16. This attempt is NOT green.
Live sample from35304 identified full current-position reachability inside PreviewDock's
future-origin continuation under LogisticsService.PreviewSource/BuildQueue. Source proved
that result was ignored whenever origin != null; actual future route is checked separately.

## Intermediate2.641 identities (not current)

444 core C# path/content fingerprint748dec1f5b282117a4f730bac5b73600db131517c6c66bab07a4c0577c486e3f.
Core DLL9342068CDC74B3AABECCC344A5038AA73244A2DAF7CC0406F4524887C89F3D44;
framework test DLL21B357706664A73936C788EC980614F51EAF6A47C8A492F94451BAA902340C64;
independent proof DLL0B433D2C7F595FF2B7730E025C1C6FDC3B1826F4485EA46CEAC12E9EC84C7B10.
SpatialInteractions.csBB315F53F367BD35B77E8D8F177071C100DF70143A31282809CC1669498B55C9;
Work.cs3886499EF18F47A0787F25EFD241F92317D8A7CCDE23CB7FF5AAEA035A98BE8D;
ItineraryProximityTests.csEC29CAFB028763C6BC7E3C94205B3896C58663A8A5CC56377AD0732F1CECD72E;
OptionalSpaceTests.cs933E989F651552BF51450C7661E827A43AF743AFB0294DB54D646DAF5E9976C3.

## Final2.642 future-leg correction and focused verification

PreviewDock now leaves current-position status absent for supplied future origins instead
of running the unused query. No fabricated WithinReach/Reachable default; real future
PlanRoute, geometry, area, duration and continuation checks remain. Explicit point-provider
assertions prove zero irrelevant current-route calls AND actual closed future-route refusal
with unchanged hash. This is not removing a required route check.

Targeted framework build6.37s, then independent-tile coverage build5.40s; consumer
build2.02s. All exit0/zero warnings/errors. Proximity intermediate30280:77/0,
493ms/outer622ms; FINAL13092:82/0,572ms/outer708ms. New independent SensorTileSpace
contact/save facts reuse its ORIGINAL Factory/New helpers (made internal). Native tile,
independent tile and independent point proximity all run through actual contracts.

Affected admission literal `Itinerary admission: prospective provider routes and atomic private previews`:
33796,142/0,1763ms/outer1897ms,90s/5s/12000 chars. Physical literal33872:218/0,
5091ms/outer5239ms,90s/5s/12000 chars. Nonspatial34408:20/0,261ms/outer404ms,
30s/5s/10000 chars. Four distinct current-build framework finals82+142+218+20=462/0.
Later test-only helper visibility/proximity coverage did not change existing case bodies
or production core; they were not redundantly rerun. No full/profile/family test.

Same canonical2.642 preparation12744:24/0,6613ms/outer6715ms; minute11 hash again
C68151411C737F1C, artifactffb59b42a5c44659bdb2a61f0d38face. Old2.641 artifacts preserved/
incompatible, never header-rewritten. Same96x96 Gulf seed1/two humans/seat1, same yard/
field/full durations/finite stock. First saved day30284/session34546:22/0,
43342ms/outer43445ms, printed42399ms, minute1451/hash192A48D81857AC0D exactly matches
previous builds. Artifact76196dc8b78e4a508b969afe5b785203.

Second saved day27124/session44060:22/0,118440ms/outer118545ms, printed117509ms,
minute2891/hash3F2F436C3F243A63, artifacta5a9b4658da5497eb6af6ac1799e2ce2.
No fault; full1440 native minute Updates each day, original limits, memory/disk/no-mutation
hash checks pass. Same original literal180s/5s/16000 chars. Both explicitly HARBOUR PARTIAL.
Shipyard1445/3600, depot erect waiting for sole busy crane, grid erect waiting for stock.

## Read-only current-state traffic diagnosis

Haul17 paused blocker.unreachable, carrying16, worker#6, source#1→grid site#12,
phase2 (delivery approach), RetryAt2902. Truck#5 idle at(70.5,77.5); truck#6 stopped
at(70.5,78.5); grid site position(70,76). These facts suggest local traffic interference,
but alone do not prove all terrain/route obstructions. BOTH trucks ALREADY have home#1:
WorkView.ReadHome reports#5 Blocked/blocker.unreachable and#6 Working/none. Do not invent
missing homes as the fix. Next inspect footprints/terrain/route/egress and existing
physical traffic/dock recovery contracts; preserve16 cargo/claims and ordinary movement.

Same proof owner gains inspect:<checkpoint>, reusing strict load/project validation.
NO command/Update/save; original public work/position/home facts and unchanged hash.
Expanded Diagnostic adds only existing worker/endpoint/phase/retry/position facts, no
route decisions. Consumer diagnostic builds1.50s/1.92s clean; inspection25840:18/0,
1072ms/outer1169ms, then final11976:18/0,1147ms/outer1245ms,30s/5s/16000 chars.
These are read-only checks, not completed workflow results. Normal advancement branches
were not rerun after diagnostic/inspection-only edits; production core did not change.

## Current2.642 identities and artifacts

444 core fingerprint1996963306aa865370fae82368dec4d888d6cffb6dcf0cfb875f4c2567940b5b;
framework/actual consumer core DLLFDE221CC38715EBDD9477B0F9D37B8D015564D19E826F9BDD20E219D94E31C7C.
Latest framework test DLL821A5566406DFCAE225C8FEF92BFA69CEEF616B84F545AEA3425FBC488EBB4E9;
independent proof DLLA5E21D61AC4E6136D6ECEA2D378E00BA002BF08078DA802B7620D9FDF94C7E16.
WorkDocks.csC926E97CD89135D6812277AC56CEBDD69D3FA9E1B913A15F2A488FAB82C6D51F;
final ItineraryProximityTests.cs9858E3FDC48EE787D3343A17B7D5F5ECE72A3DAC6CF7B8BF3CB2BBC848CE0ADA;
shared IndependentTileSensorTests.cs602DE72F6EA9E76DADE3DD8C03976E37BEB261093582119323D23BC2961F5464.
Unchanged SpatialInteractions.cs/Work.cs/OptionalSpaceTests.cs hashes are in the
intermediate section; those particular source files match, not the old core fingerprint.

Native saves under actual consumer artifacts/marine, all retained:

| Build/stage | Directory | Bytes | game.sav SHA256 |
|---|---|---:|---|
|2.641 prepare|b3c3332e3a5043c3b0127054f9375ea3|32931|741EDF2595FB3D6684509889D5F82D657551FDD7D5E208DCF1A07552177F8A5A|
|2.641 day1|d32393f94fb84b39a7ecda844dc74ee1|134238|E01C1F5E9EB09BA340FF064957A2DA487C38121FD9ACDC4F60B6AD6AA4DF5811|
|2.641 timed partial|01c7589dfcad4f799846513200dcf966|136535|D6EE1DB516297F3B0C6B802BC4D4025468E3D2D576FA9463145DACC90499E677|
|2.642 prepare|ffb59b42a5c44659bdb2a61f0d38face|32931|C4E80CDB667A5610397B1260E8B4C55048B7A6346C4C8F51099626825010F63A|
|2.642 day1|76196dc8b78e4a508b969afe5b785203|134238|81870A6F704174C4071B8531FEDBD11048B37AE54F9001A72FA05DE1287E4B64|
|CURRENT2.642 day2|a5a9b4658da5497eb6af6ac1799e2ce2|135917|B9BE8BFC8E1566E2444793F51BE7147C1D0363F912CEBAD2F6C7F154F8E75C68|

Current checkpoint.json215bytes, SHA2560519973B884FDC36FE74227161F4EBEEC796365925D613DA9E87EC5ACA444FBB,
schema1/harbour_progress/next harbour_execution, native hash3F2F436C3F243A63. Do not count
the intermediate failed attempt as another completed day or infer full harbour completion.

Final consumer diagnostic source OffshoreTests.cs01CF4014AE6C00700B7813027B106C3706399F8317A756F307EDAB3B2666B2FF;
current proof DLLBC409CF305ED579F2CC7A8B895253760F1030DAB109A75054C8B13619B04A4C5;
recompiled Oil DLL8731E6BD3B99078369B8DE410510174C8BDF3F562E422A3EE49408420FA4EDA6
(production source unchanged, referenced core identity changed).
Final original criterion recount221/210 checked/11 open. Framework diff-check0 with35
pre-existing LF warnings/no other diagnostics, consumer targeted diff-check0, staged0
in both repositories and no live focused test workers. One documentation patch failed
context validation and changed nothing; corrected against actual text without broad rewrite.

## Remaining scope

Original221/210 recorded checked/11 open; no original checkbox closes from these focused
proofs. Full harbour/sea completion, all original game workflows, broader provider/fault/
scale matrices and authorized regression remain OPEN. No Oil production, Tycoon/business,
JSON gameplay content, platform/Draw, staging/commit/push. Continue real saved harbour work;
do not substitute pending work, larger timeouts, more supplies or an easier world for completion.
