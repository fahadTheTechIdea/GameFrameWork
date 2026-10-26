# P5-MF1 — Native marine fleet continuation

> Current engine2.682: [Engine implementation/focused proof](../../docs/migration/P3_OBSERVATION_HASH_COST_VERIFICATION.md#current2682-visibility-facts--2026-10-21); [sole current native state and next action](../../docs/migration/P5_MARINE_FLEET_CONTINUATION_VERIFICATION.md#current2682-physical-continuation--2026-10-21). No full test suite or Git publication. Earlier build-specific records below are historical evidence, not current execution instructions.

> Current engine2.671: [P9-RS1 native result and exact next continuation](../../docs/migration/P9_RANKED_DOCK_JOURNEYS_VERIFICATION.md). Consumer copy verified; actual harbour8111/fleet admission8127/full first day9567. Fleet day211/0 PASSES112330ms under original120000ms (old2.670 failed120532ms) with EXACT same game hash. Current consumer artifacts/marine/8be38c0b32dd4267871c50de85cfad65/checkpoint.json,9567/hash31D2C58FE3414413,fleet_progress/next fleet_execution. First vessel1268/6000/seven original orders/no vessel yet. No live worker; next bounded fleet continuation NOT RUN. Preserve THIS compatible progress; no rebuild/reset/prepare/readmit/header rewrite merely to continue. Full fleet/sea/general performance/whole plan remain OPEN, no full/profile/family tests, source/gameplay/content/observer changes or publication. Earlier CURRENT labels below historical.

> Current engine2.670 and actual native continuation: [P3-BA2 evidence/handoff](../../docs/migration/P3_BYTE_ACCUMULATOR_VERIFICATION.md). The2.669 harbour completed8111 and seven original fleet orders admitted8127, but its day failed the unchanged120s bound at9267. Actual sampled dominant byte loop is now optimized without changing canonical hashes/guards; four focused framework cases pass, initial native days improve in their measured scope. Full fleet/sea/general performance remain OPEN; no duplicate admission, gameplay shortcuts, old-header rewriting or full-suite run. Older2.669/2.668 records below are historical/incompatible, not current resume input.

> Current engine2.669 and compatible continuation are in [P3-BA1 evidence](../../docs/migration/P3_BYTE_ACCUMULATOR_VERIFICATION.md). The byte kernel is faster in its measured scope but busy-day engine timing nearly flat; fleet/native performance remains open. CURRENT consumer c48a1f08e00e4e1c8865852be317ab3a/checkpoint.json,2891/hash7BB9F98358A81ED2,harbour_progress/next harbour_execution. Old2.66810227 fleet images below are preserved historical/incompatible; no header rewrite. Do not repeat the old fleet admission or imply manufacturing is complete on current build.

## Current execution scope

Native staged continuation is implemented in the original MarineRun and strict
checkpoint owner; the existing full Play and staged route/recipe sequence share
one admission implementation. Current source is2.682/save160/replay1. The P5
verification linked above owns the sole current native save/command; P3 owns
the engine allocation evidence. No second resume guide is maintained here.

Current2.682 physically completes the harbour at8111 and admits the original
fleet once at8127. All seven original vessels finish by55407, retaining all768
authored manufacturing hours and real supply. Twenty-eight further bounded
continuations5907/0 form one verified native save chain from15327; one read-only
inspection12/0 confirms all seven AtHome, no blockers, empty queue and no work.
Source/binaries/content remain unchanged. The optional marine survey completes
its physically supplied3600-minute work by59188, four original staged commands/
observations1374/0. Pad construction completes61229 with original24h work and
unchanged wall bounds, then staffed drilling completes all180h and a real well
at72810. Thirteen pad/drill observations8550/0; source/binaries remain unchanged.
Terminal60h construction then completes77731, positive physical crude reaches
shore77852, and six supplied3h subsea segments complete79833. Eleven selected
terminal/shipping/subsea/final-refusal invocations5318/0; one rejected CLI literal
selection is separately retained, not hidden or counted as passing. Subordinate
C3 is now functionally verified; monolithic/general performance acceptance remains open.
Earlier2.681 pressure results remain historical evidence.

- [x] C1 Revalidate current source/binaries and complete the current physical harbour;
  admit the original fleet sequence once and save native queue/route facts.
  Current2.682 completes8111/admission8127 with no reset or duplicated sequence.
- [x] C2 Observe current physical manufacturing, all seven water rollouts and saved continuation under original bounds.
  Current2.682 all seven water outputs and saved chain verified at55407; read-only
  public inspection confirms original vessel IDs16–22 AtHome without blockers.
- [x] C3 Continue sea survey/drilling/material shipping/subsea through the original engine-owned workflow; retain full original acceptance gaps.
  Current survey59188/pad61229/well72810/terminal77731/crude77852/subsea79833
  verified through original staged commands, full authored work and saved hashes.
  Terminal checkpoint rejects earlier admission/execution without runtime load.
  This is functional staged acceptance, not a monolithic/full-suite timing claim.

All physical timing, readiness, shortages, access, queues and decisions remain in
the engine. The proof only submits original player commands, drives ordinary
Updates and observes public facts. No default/full/profile/family test, instant
vessel, shortened recipe, stock grant, repeated admission or old-header rewriting.
Original P5.005/.008, P9.051 and the full plan remain OPEN.

## Historical implementation and follow-up

> Current2.647 follow-up: same driver and original recipe/route sequence earn full harbour and TWO vessels (100h survey,60h supply). The earlier2.646 truck cycle is fixed; a different loaded delivery now exposes home/service berth conflict. [Current artifact/all observations/diagnosis](../../docs/migration/P3_NATIVE_TRAFFIC_PROGRESS_VERIFICATION.md), [next engine owner audit](P9_PRODUCER_BERTH_HOME_DESIGN.md). Five vessels/full marine remain open. The2.646 implementation-stage details below are historical, not current native checkpoint instructions.

## 0. Problem and specification

The canonical2.646 Gulf harbour physically completed at minute8051. Existing
MarineRun.Play contains the next nine public logistics-route commands and seven
ordinary manufacturing orders, but native continuation admits only harbour work.
Recreating the world would discard earned physical progress; duplicating the
player sequence or moving production rules into the harness is incorrect.

## 1. Goals

1. Continue the actual harbour_complete native save without changing engine,
   content, money, stock, recipes, work durations, world, company or clock.
2. Extract ONE existing fleet-admission player sequence shared by Play and native
   continuation; never re-enqueue on a progress checkpoint.
3. Keep schema1's exact eleven intent keys; validate explicit stage pairs and
   reject unknown/missing/duplicate keys or wrong stage BEFORE native Update.
4. Observe actual named queue/progress/output/route views and durable native saves.
   Preserve768 authored manufacturing hours and the original60-day fleet bound.

## 2. Design

Original MarineRun owns one LoadCheckpoint identity/content/hash/geometry/project
validator and one existing SaveCheckpoint writer. The validator accepts current
harbour and fleet stage pairs, with the requested next stage enforced before
loading a runtime. No old-header conversion or second loader/driver.

fleet-prepare:<checkpoint> admits the original nine routes and seven recipes once,
then saves fleet_admitted/next fleet_execution. fleet:<checkpoint> only advances
the actual game through bounded one-day observations and saves fleet_progress or
fleet_complete/next marine_survey. inspect:<checkpoint> stays read-only and supports
finished facilities as well as sites. No second gameplay loop or progress store.

The original TestKit.Order advances one actual engine step per public command.
The first route's native CreatedTick plus9 route commands plus7 recipe commands
therefore identifies the original fleet wait start. Derive this from actual
immutable route facts, never add a parallel saved clock or reset a deadline on
resume. Exact route identities/settings/creation cadence and original queue
execution IDs/order/output prefix must agree before advancing.

## 3. API surface

Proof CLI inputs only; existing schema1 intent shape and explicit stages remain.
Current native saves must match the current strict engine build and content;
save160/replay1 do not make an earlier build compatible. Only the current P5
verification handoff identifies an executable continuation; historical2.646 saves are not inputs.

## 4. Files

Actual consumer OilCo.PublicProof/OffshoreTests.cs, project embedded-resource entry
and testdata/marine-intent-syntax.json. The standalone negative registry case uses
that explicit syntax-only fixture, never a replacement native save or game content.
This stage/evidence and framework master/P5/AGENTS/README record evidence and limits.
No Oil gameplay/business/JSON/platform/Draw or production engine edits.

## 5. Errors

Wrong stage/unknown keys/hash/configuration refuse before commands/Update. Missing
or duplicated routes/recipes/results, altered canonical durations, incompatible
save or engine fault cannot publish a verified checkpoint. A healthy wall-limit
failure may retain actual native progress but still fails; no instant fallback.

## 6. Compatibility

No compatibility shim or legacy file reader. Both existing harbour and new fleet
stages use the same strict current native checkpoint contract.

## 7. Verification

Targeted consumer build; one new exact wrong-stage/intent refusal check with both
the explicit syntax-only fixture and actual native artifacts; original marine literal selected only for fleet admission
and bounded native progress. Verify queue7/NextExecution8, exact recipe order,
full authored durations, no instant vessel, private views, native save/hash and
actual material travel/work. No full/profile/family/default whole marine run.

## 8. Risks

Real recipes take32 nominal game days plus supply/settlement. Stage support does
not mean the fleet has completed. Real shortages, power, incidents, access or
physical traffic must be diagnosed in the engine, never bypassed in the proof.

## 9. Out of scope

New engine rules, grants, cheaper/shorter recipes, economic activation, world reset,
schema/header migration, host gameplay, broader suites or Git publication.

## 10. Execution order

1. [x] Share strict checkpoint validation and original fleet player commands.
2. [x] Verify stage/intent refusals and actual once-only queue admission/save.
3. [x] Observe bounded native fleet progress and document actual remaining blockers.
   Earlier2.672 continuation earned actual100h survey vessel and60h supply boat,
   both home; third recipe physically resupplied and working1370/8400 at19647.
   Six original day observations plus two read-only inspections1290/0; no source/
   content/observer edits, new commands, reset or repeat of passing framework cases.
   Historical native truck livelock retained; selected P9-CT1 correction verified.
4. [x] Complete all five remaining ORIGINAL recipes/material supply/connected rollout
   and native saved queue/output identity under unchanged60-day deadline and stage
   bounds before accepting fleet_complete/next marine_survey. Actual2.672 reaches
   all7/queue0/Idle/no jobs at55407,32days20h from admission, within original60days;
   all7 AtHome/no blockers independently inspected without Update.25 forward
   observations plus one read-only inspection5286/0; no source/content/observer
   edit, rebuild/reset/readmission/resource grant/steering/header rewrite or full
   suite. Historical2.672 f9285b9a64604f9aa53a8cdf427de14d/hash80D49FC1F2F39AF7
   is NOT a compatible current resume image. Historical2.675 revalidation also
   earns ALL7/queue0/Idle/no jobs at55407 with the SAME game-state hash, all7 AtHome
   without Update. Remaining25 staged observations+inspection5286/0, no source/
   content/proof/build change; original60-day and120s bounds retained. Historical
   8595ef2ad8d4491888f7a058fcb8c4c3/checkpoint.json is fleet_complete/next marine_survey.
   This closes selected fleet continuation only, NOT full marine/P9.051 acceptance.
   Historical2.676 has only the first100h survey vessel fully manufactured, rolled out
   and inspected AtHome; recipe2Working848/3600 at15327, SIX recipes still queued.
   No current-build full fleet closure. Preserve83b525bdf0f4484c98012e59ec64aac9
   as STRICT-INCOMPATIBLE with current builds; current continuation is identified
   only by the top P3 handoff. Never rewrite headers or repeat current preparation.

[All selected attempts/current native save/next engine defect](../../docs/migration/P5_MARINE_FLEET_CONTINUATION_VERIFICATION.md).
