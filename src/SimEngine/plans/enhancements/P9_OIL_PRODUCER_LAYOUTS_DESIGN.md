# P9-PBH1.O — Canonical Oil producer layout migration

Subsequent [P9-PBH1.P](P9_PARKING_ROUTE_SELECTION_DESIGN.md) implements original-owner
arrival/ranked selection on2.652 and retains current native progress at1092;
[evidence](../../docs/migration/P9_PARKING_ROUTE_SELECTION_VERIFICATION.md).
This stage's2.650 source preservation/results below are historical; layouts unchanged.

Status: authored and focused-verified on unchanged engine2.650.0/save160/replay1;
harbour continuation TIMED OUT, physical acceptance remains partial.
[Every attempt and identity](../../docs/migration/P9_OIL_PRODUCER_LAYOUTS_VERIFICATION.md).
Parent [connected rollout](P9_CONNECTED_ROLLOUT_DESIGN.md) and original
P9.acceptance.051 remain OPEN; original221/209 recorded checked/12 open.

## 0. Problem and specification

Before this stage, actual Oil company_base, vehicle_factory, maintenance_depot
and shipyard had no physical dock/launch metadata. The corrected engine refuses
mobile production without it. The reference fixture is not proof that these
canonical facilities or starting fleets work.

## 1. Goals

1. Author actual layouts in existing master-listed oil/structures.json using
   the SAME engine dock/rollout/parking contracts, not host movement or policies.
2. Preserve all existing prototype fields, recipe amounts/times, startup kits,
   money/optional Tycoon rules, canonical geography and scenario configuration.
3. HQ supports every actual starting kit (maximum11 machines) through complete
   planned-prefix admission, not a one-bay layout that prevents founding.
4. Existing resource/repair service remains possible alongside launch and idle
   parking. Land and sea compatibility is evaluated by the original engine.
5. Prove composition, canonical founding and affected marine preparation through
   public commands/views and bounded exact checks; keep broader acceptance open.

## 2. Existing owners and design

Only four actual entity component objects gain tile_work_dock. The existing
WorkBoard/NavGrid/presence/motion/home/creation/queue/reservation/clock authorities
remain unchanged. No gameplay logic goes in harness/headless/demo or tests.

HQ4x4 has explicit first-fringe launch locations, real north/south service,
second-fringe exits/waiting and separate parking, enough for11 simultaneous
initial outputs. Its lane lists the actual ten land prototypes used by supported
recipes or startup kits; repeated truck/tanker instances still need distinct
physical placement and egress against the entire prefix.

Vehicle factory4x4 and maintenance depot3x3 use actual supported output lists,
service/queue/departure geometry and separate parking. Their physical resource
policy lists authored solid resources compatible with existing storage; known
work roles remain explicit. No price, resource amount or work permission is
granted by physical service eligibility.

Shipyard5x4 uses authored first-fringe launch alternatives around its footprint
for sea prototypes and real land/marine service access. Departures and parking
are separate, with rear clearance retained for the existing harbour depot/grid
layout. At the former yard71,74, only-wet-contact70,73 stays a launch/service
cell, not overwritten or made nonblocking. Current engine suggestions may choose
another valid site after complete layout checks. Idle vessels must leave service physically.
No fixed coastal geography or work durations are changed to fit the proof.

All geometry is literal JSON intent; authoring tools may construct that text,
but no runtime layout generator, second registry or game-specific engine branch
is introduced. Baseline/delta verification removes precisely the four new
component fields and compares every other normalized source character.

## 3. API / JSON

No new API. Existing tile_work_dock fields: work_roles, resources, slots and
rollouts. Rollouts reference actual prototype IDs and existing Service/Rollout
starts and distinct Departure exits, with an explicit search budget. Parking
does not become service or launch. No missing-binding or ring-search fallback.

## 4. Files

Actual Oil content/oil/structures.json; focused public proof registration/cases
only if needed; this design, evidence and authoritative framework tracker/docs.
Engine implementation changes only if an actual generic defect is reproduced;
any such change needs its own focused proof and explicit build identity.

## 5. Errors

Invalid references/capabilities reject before Initialize. Full/unreachable
birth/exit/service/parking returns existing typed blockers. A failed starting
kit preview creates nothing; a blocked completed recipe retains WIP. No host
relocation, refunds, grants, lowered work or lost loaded cargo is an acceptable
remedy. Report actual failures and keep the corresponding gate open.

## 6. Compatibility

Development only. No shim, legacy reader or header rewrite. Older native saves
remain historical; new native acceptance must be earned normally.

## 7. Verification

Static exact delta and reference/geometry checks, targeted consumer build, focused
canonical Frontier founding and native Gulf prepare. Verify the largest startup
kit and real saved founding continuation. Use the existing bounded literal runner;
do not run full/profile/family suites. Existing stage limits remain real gates.

## 8. Risks

Too few starts prevent founding; parking on launch/service creates renewed
deadlock; every auxiliary slot must fit native bounds and avoid fixed neighbours.
Large alternative sets can increase guarded routing cost. Measure actual cases,
do not suppress observations, claim performance broadly, or increase bounds to
make a stalled scenario look green. Service eligibility must not disable hauling
or repairs when adding a physical layout.

## 9. Out of scope

New business rules, adapters/renderers, host gameplay, fake native saves,
geography/recipe/start-kit edits, broad tests or Git publication.

## 10. Execution order

1. [x] Read actual four producers, startup kits, role/mobility metadata and native
   founding/rollout/placement owners.
2. [x] Author layouts and prove exact non-layout content preservation.
3. [x] Focused composition/founding/max-kit/saved and marine preparation checks.
4. [x] Update parent/master/evidence with actual results and next remaining work;
   full fleet/offshore/general recovery/provider/performance gates remain open.
