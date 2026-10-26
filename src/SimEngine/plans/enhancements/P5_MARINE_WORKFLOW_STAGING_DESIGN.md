# P5-MS1 — Bounded original marine workflow and native checkpoints

> Current engine2.682: [Engine implementation/focused proof](../../docs/migration/P3_OBSERVATION_HASH_COST_VERIFICATION.md#current2682-visibility-facts--2026-10-21); [sole current native state and next action](../../docs/migration/P5_MARINE_FLEET_CONTINUATION_VERIFICATION.md#current2682-physical-continuation--2026-10-21). No full test suite or Git publication. Earlier build-specific records below are historical evidence, not current execution instructions.

Historical preparation slice verified2026-10-16 on unchanged2.639.0/save160/replay1, final24/0.
Two first-seat fixture failures and intermediate23/0 retained in the
[verification report](../../docs/migration/P5_MARINE_WORKFLOW_STAGING_VERIFICATION.md).
That historical preparation saved pending projects at minute11; it did not prove
harbour completion or later marine stages. Its then-recorded221/210 checked/11 open
count is historical, not current. The current handoff above owns active native
continuation and the reopened original gates; do not resume the old preparation.

## 0. Problem and specification

Existing actual-consumer OffshoreTests owns the unverified full marine workflow. It
searches up to79 generated worlds, shares a static SimGame and waits for Sold even
though current Gulf is resource-only. Long physical waits lack a wall-time stage bound.
Improve this owner in place; no second sea scenario/controller or shortened activity.

## 1. Goals

1. Use one explicit canonical96x96 Gulf seed and public founding/coastal previews;
   fail with real facts if unsuitable, never generate a hidden fallback sequence.
2. Make the proof session-owned, not a shared static game.
3. Distinguish public player commands from observer-only fixture field selection.
4. Make current resource-only Gulf assert physical crude delivery; leave money/sales
   verification in the existing optional Tycoon sales proof. Do not activate business.
5. Bound each physical stage and retain diagnostics. Preserve all actual durations,
   materials, manufacturing outputs and the original sea workflow's scope.
6. Provide a prepare-only invocation of the same case that admits the actual harbour
   projects, verifies their pending graph timing and writes an opaque native save for
   later staged continuation. This is not a completed marine workflow result.

## 2. Design

Only actual consumer OilCo.PublicProof/OffshoreTests.cs: preserve original registration,
move its active session/helpers into one instance-owned MarineRun, retain shared typed
ObserverFields for the separate classification proof. Replace FindWorld's seed loop
with explicit Gulf/seed1/two humans, playing the authored second seat, and ordinary
founding. Retain actual geometry/content; the first seat remains unfounded. Choose a
coherent field/site pair from engine-suggested coastal sites within the original18-tile
site radius and16/26-tile fixture reach limits, not merely the first arbitrary suggestion.

Same full workflow remains shipyard/depot/grid→seven actual vessel recipes→marine survey
→pad→drilling→terminal→delivery→subsea flowline. Native Wait receives120s wall and original
game-day limits, prints native work/jobs/stock/queue diagnostics and throws on failure.
No successful pending/failure fallback, teleports, instant work or provider shortcuts.

Run --case-input prepare only through the first three admitted coastal projects.
Use native views to verify pending sites, exact graph durations, no completed structures,
optional money absence and actual saved hash identity. Emit a fresh unique artifact
directory beneath actual consumer artifacts/marine containing game.sav plus a small
intent/checkpoint JSON (native hash, next proof stage, selected field/yard intent).
CreateNew prevents overwriting earlier artifacts. Do not serialize resource/work/clock
state outside the engine save or claim a posted project is completed.

The focused run is prepare-only with180s process/5s termination. Any failed seed,
placement or native admission is evidence to diagnose, not authorization for hidden
world search, smaller geometry, inflated supplies or changed rules. Resume/physical
harbour completion and remaining stages stay explicit next work until actually verified.

## 3. API surface

No production engine API, JSON, business-rule, schema or clock change planned. The
existing test case gains explicit prepare-only observer input; default still means
the full original workflow, never silently just prepare.

## 4. Files

Existing actual OilCo.PublicProof/OffshoreTests.cs; this design and migration report;
master/P5/AGENTS/README. Runtime proof artifacts are opaque native saves, not source,
content catalogs, duplicate progress models or compatibility data.

## 5. Error handling

Wrong prepare input, unsuitable explicit seed, refused command, unavailable facts,
stalled physical work, fault and save mismatch fail explicitly with native diagnostics.
Do not increase failed bounds or infer success from a timed-out observation.

## 6. Compatibility

Development only. No old seed-search path, static world, mandatory sale fallback,
legacy graph or old command API is retained.

## 7. Verification

Targeted Release OilCo.PublicProof build, then existing literal marine case with
--case-input prepare, timeout-ms180000, termination-timeout-ms5000, output-chars16000.
No full/profile/family run or default full marine invocation in this setup slice.
Inspect emitted native checkpoint and report stage scope separately from full completion.

## 8. Risks

Canonical large-world initialization/queries may be expensive. Setup does not prove
physical harbour completion, sea rollout, survey, cargo or drilling. Do not substitute
this narrower check for original P5.005/P5.008 or the default full workflow.

## 9. Out of scope

Changed money/default modes, quantities, geometry, durations, engine adapters/Draw,
host gameplay, full tests or publication before the full goal and plan finish.

## 10. Execution order

1. [x] Refactor existing owner and correct stale Gulf/default/wait assumptions (later full branches compile-only).
2. [x] Admit/inspect/save the first coastal projects through the same public path.
3. [x] Build and run only prepare; retain all failures/results/artifact identity.
4. [x] Record partial completion and actual native checkpoint for later harbour continuation.
   Original full marine workflow and overall goal remain open.
5. [x] Extend the existing MarineRun/LoadCheckpoint/SaveCheckpoint and factor the
   original Play survey player commands ONCE, removing duplicated paths rather than
   adding another sea runner. Resume the actual fleet_complete/marine_survey image;
   validate exact stage pairs and actual queue/output/knowledge facts before runtime
   advancement. Inspect/negative wrong-stage input must remain read-only or refuse
   BEFORE loading/Update. Do not create a new checkpoint parser/schema/persistence
   owner. Optional survey is the original scenario's player choice, NEVER a seismic/
   logging/drilling prerequisite. Preserve original30-day survey and120s stage limits.
   Implemented in original OffshoreTests only; one clean targeted4.94s build,
   nine literal invocations1663/0. Original fleet save preserved; actual supply
   boat delivery/survey vessel travel/full60h work/private earned knowledge and
   completed native save verified through three bounded continuations. Current
   bc4046e3773946b7a57da7c957721ba8/checkpoint.json at59188/hash65E5CC3580A76EED,
   survey_complete/next marine_pad; engine/core/domain game identity unchanged2.672.
   Survey vessel is normally Returning/no blocker, not teleported home. This is
   HISTORICAL2.672 verification, not a compatible2.673 resume pointer. Pad and
   drilling CLI now exist below; no full marine/primary gate closure.
   Historical2.675 revalidation earns the full60h survey at59188 with the SAME
   65E5CC3580A76EED game-state hash; four selected admission/continuation calls
   1374/0, unchanged30-day/120s limits, no source/content/build change. Current
   survey7e827021149f4e12a5400911cd1e6ed3 is a historical2.675 milestone,
   strict-incompatible with2.676; the newest continuation is owned by the top handoff.
6. [x] Bound original offshore-pad player admission and physical execution through
   the same engine previews/suggested placement/projects/work/transfers and native
   saves. Admit ONCE, then derive intent/dependencies/elapsed time from original
   authoritative order/history facts, not a saved proof timer. Keep original40-day
   pad limit, durations/materials/machines and cumulative deadline across resumes.
   Current2.682 revalidates the full physical path without source/content edits,
   rebuild, repeated admission, grants or changed timing. Actual pad admitted59189;
   material deliveries and manufactured crane18 earn896/1440 minutes by60629,
   then complete the original24h node and native structure25 at61229. Admission
   470/0 and both continuations383/0+380/0 pass, stage69014/8698ms under unchanged
   120000ms. Original40-day cumulative limit, private history, retired site and
   no remaining order/jobs verified; source/current pointer/all logs in the sole
   P5 handoff above. This scoped timing gate is now verified, not universal
   performance or broad P5.005/.008/full-goal closure.
   Historical2.672 pad completed61229; two prior120s wall gates FAILED
   and remain retained failures. Later2.673 functional reproduction is recorded below; all
   older native images are preserved without header rewriting. Historical2.675 pad
   observation FAILS382/1 at59609/121431ms against120000ms. Healthy native
   progress06e52421aed944e1bb81a4132d28fb99 is preserved; no pad completion is
   claimed for that historical run. Paired active/post-supply profiles are retained in P3.
   Historical2.673 follow-up also physically completes the pad at61229, but its two
   prior wall gates FAIL at59429/60209. Those failed observations are not erased
   by current2.682's successful unchanged-bound verification.
7. [x] Resume original drilling through normal engine activity commands and views,
   preserving original60-day limit, actual staffed machine travel, material/fluid/tool
   supply and well output. No host scheduling/completion rules, instant well or
   mandatory survey/logging branch. Record blockers and actual physical completion.
   Extend SAME MarineRun with drill-prepare:/drill:; share original Play admission,
   existing ObserveActivity clock driver and strict eleven-key native checkpoint
   loader/writer. Validate actual activity:drill_offshore_well order, four original
   nodes (position24h -> drill120h -> casing_cementing36h -> complete0h), manufactured
   offshore rig, unchanged material requirements and engine-earned completion plus
   real well. Derive cumulative60-day deadline from original order/history, including
   replacement of the actual pad; do not require a removed pad to remain alive after
   completion. Keep prior pad wall-time failures OPEN; later productive completion
   does not erase them. No work-quantity/geology/business/JSON rule or native header
   changes. A required generic correction must bump strict engine identity, as below.
   Actual2.672 attempt earned180h then failed work.replacement-active-execution.
   The static-only generic effect contract was revised in its original owner:
   [contract follow-up](P9_ENTITY_ACTIVITY_UPGRADES_DESIGN.md). Core2.673 is an
   intentional strict identity change, NOT a native header edit. Independent
   outcomes/static/reference checks and unsurveyed Oil108h completion pass;
   full offshore180h completion was unverified at the earlier18207 handoff.
   Current2.673 continuation now earns all seven original vessel recipes, marine
   survey, actual pad, position24h/drill120h/casing36h and the completion effect.
   Native fe15fdf923c54180a35b68e4c9994889 at72810/hash4B5854872D7BA4DE is
   drill_complete/next marine_terminal: actual offshore_well26 replaces pad25,
   original history/order/company/geology facts agree and no running duplicate
   remains. Final literal623/0; preceding eight drilling days all pass original
   bounds, including the busy casing-supply day106405ms. Save/load checks retain
   actual native state; no new preparation, reduced duration or header rewrite.
   This closes ONLY this numbered drilling continuation task, not P5.005/P5.008,
   pad performance, full marine/durability/stress or the whole engine plan.
8. [x] Continue original terminal construction and crude shipping with30/40-day
   limits. Actual physical crude at the terminal is the resource-only success fact;
   money/sales remain optional Tycoon behavior, not a substituted default. Keep
   normal ports/carriers/docks/fuel/work and all original refusal semantics.
   Implemented: terminal-prepare:/terminal: in SAME MarineRun/parser/native
   loader/writer and ObserveActivity clock driver. Share original Play terminal
   admission once, use actual typed construction site/order/history and untouched
   clear12h -> erect48h graph. Original30-day deadline comes from native creation
   facts across every resume; a posted site is not a terminal. Terminal completion
   advances intent to marine_shipping only; it never claims crude delivery, income
   or full marine success. Add shipping-prepare:/shipping: through the SAME owners:
   original one100-unit crude route, authoritative RouteView.CreatedTick/ports/
   settings and actual initially empty terminal stock. Physical arrival, not route
   posting, ends the stage. Preserve cumulative40-day deadline and automatic
   carrier selection; no forced shuttle, teleported cargo, sale or new timer.
   Both branches were unverified at the original implementation checkpoint.
   Two targeted clean proof builds1.61/1.59s, focused parser/wrong-stage checks,
   plus real current-build harbour completion and once-only fleet admission/two
   manufacturing days:12 original-literal invocations1407/0. Terminal/shipping
   positive execution is STILL UNRUN; these counts do not close this requirement.
   Core/domain/content unchanged2.673; preserve latest native image in MS1 and
   continue, no reset/readmission/version bump/header rewrite. The11007/first
   recipe2708/6000 handoff from this earlier slice was superseded at18207.
   Current2.673 follow-up now verifies actual terminal construction77731 and
   physical positive crude at77852, native d1abdfc2753e4872a9da311f03e609e3/
   hash62B7A9A7243110BB/shipping_complete/next marine_subsea. Four terminal
   execution observations430/431/433/425 and physical shipping457 assertions
   pass original bounds; terminal admission530 and route admission571 also pass.
   Original routes1–9 remain, only native crude route10 is added, and automatic
   land tanker7/shuttle20 selection remains engine-owned. No income, forced
   carrier or instant cargo is substituted. Earlier source-only claims and pad
   performance failures remain historical/open; no primary gate closes here.
9. [x] Continue original subsea-line placement/physical work and final public-view
   assertions with unchanged original limits. Use the SAME Play command sequence;
   full result must account for every original sea assertion, not just vessel count.
   Focused literal per affected phase plus explicit malformed/wrong-stage cases only;
   full/profile/family/default whole-workflow runs remain unauthorized. Save/load/
   continue, truthful failure snapshots and exact source/consumer identities must
   accompany each accepted phase. Diagnose engine blockers in their existing owners,
   never fix gameplay inside this thin player/observation driver.
   Implemented subsea-prepare:/subsea: in SAME MarineRun/strict eleven-key checkpoint
   protocol and ObserveActivity. Share original Play line intent once, preserving
   original well-relative endpoints and native SuggestLinePath/placement refusals.
   Read path, plan/order/target identity and timestamps from immutable PlanView;
   never persist a second path, clock, segment cursor or completed-tile counter.
   Pending execution validates actual segment.i.install nodes/full3h durations/
   dependencies and manufactured layer vessel. Completion requires native Done
   plan/earned history/retired marker AND every intended pipe cell, not unrelated
   nonzero cells elsewhere in a bounding box. Preserve original cumulative30-day
   deadline and native resource handling. Positive execution remains OPEN until
   physically run; successful preparation/negative/parser checks are not completion.
   One clean targeted5.16s build, two exact refusal invocations321/0+333/0, and
   five forward native fleet days211/0 each:7 selected invocations1709/0. Actual
   survey vessel16 and supply boat17 manufactured on water; crane-vessel recipe
   waits for physical materials at18207. No terminal/shipping/subsea positive claim.
   Existing fleet validator now preserves exact nine-route depot prefix while
   recognizing ONLY original later crude route10; duplicate/unrelated routes still
   refuse. That tenth-route positive branch remains unverified. Core/domain/content
   unchanged2.673; latest compatible image is recorded in MS1, no reset/header rewrite.
   Current follow-up verifies the actual positive path: original six-segment line
   earns its physical supplied3h/segment work, native Done plan, exact order/company/
   history/terminal timestamp, retired marker and every intended owned pipe cell.
   First day528/0 and completion521/0 retain original limits; final335/0 rejects
   malformed/wrong-stage input and completed-stage re-entry without loading runtime.
   CURRENT native1fd4de01d4dc4c81b53e0790ddf19a5a at79833/hash77C1562263238536,
   subsea_complete/next complete. No further marine execution/admission is valid.
   Two pre-admission proof failures303/3 and310/2 remain recorded: submission
   intentionally validates intent, not activation readiness, and authored land
   policy uses oil.line-land-blocked. Same proof now reads a two-entry JSON player
   template through native PreviewPlanTemplateActivation, preserving both typed
   terrain refusals and asserting pure/no-work/no-time intent readiness. No gameplay
   rule was moved to the driver. The intermediate public-template-selection compile
   error and all four build attempts are retained in MS1. Stage6's two current pad
   wall failures remain OPEN; this task closure is functional staged acceptance,
   not a green monolithic/default/full-suite/performance or full-goal claim.
