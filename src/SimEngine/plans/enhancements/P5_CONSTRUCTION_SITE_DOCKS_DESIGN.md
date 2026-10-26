# P5-SD1 — Authored construction-site service and departure

## 0. Problem and specification

Canonical2.642 saved minute2891 has two opposing trucks at70,77 and70,78. Native
structural route reaches grid site#12 at70,77, occupied by truck5; shoreline and
shipyard footprint constrain approach. BOTH have home#1;5 cannot return through6.
The18 authored construction-site entities have ordinary ports but no IWorkDock.
Thus they do not use the existing finite service claims and physical departure.

## 1. Goals

1. Declare service/departure for all18 existing authored construction sites through
   the existing tile_work_dock/IWorkDock contracts and master-owned JSON document.
2. Preserve every existing site footprint, resource, capacity, cost, graph and duration.
3. Preserve all original worker roles/material filters; no invented stock or supplier.
4. Validate finite distinct exterior slots and normal engine loading/placement/work
   through focused checks and bounded canonical continuation.

## 2. Design

Extend each existing entity in actual consumer content/oil/construction-sites.json.
Its work_roles are the unique roles of its existing oil.build.<target> graph, and its
resources exactly its existing storage accepts. Four exterior service slots and four
separate outward departure slots offer authored cardinal alternatives to land/marine
workers. Existing dock selector checks actual routes, occupancy, claims and request
compatibility; actual jobs own all claims and movement/handling/time. No traffic
manager, second scheduler, implicit slot generator or host-side maneuver algorithm.

Coordinates are explicit JSON relative to each existing footprint. No waiting slots:
unavailable service retains demand at source via the existing scheduler. Route-valid
alternatives can differ by coast/terrain; unsupported slots are not fabricated usable.
Native placement suggestions remain authoritative and may reject newly obstructed
dock layouts. No hidden seed search or manual relocation workaround.

## 3. API surface

No engine contract or save layout change in this stage. Authored against2.642;
current physical runtime validation is2.646/save160/replay1 via P9-SJ1. Existing
master already owns construction-sites.json; no duplicate entity definitions/catalog.
Content fingerprint changes intentionally; old native saves remain untouched/incompatible.
Use the same canonical96x96 Gulf seed1 fixture to verify the changed authored capability.

## 4. Files

Actual consumer content/oil/construction-sites.json; existing framework OilManifestContentTests
partial/registration for focused metadata; current marine proof diagnostics only if needed.
Stage/evidence/master/P5/AGENTS/README record results. No renderer/business code.

## 5. Errors

Unknown roles/resources, interior/duplicate slots, absent required journey and invalid
provider geometry fail through existing native validators. Invalid/unreachable placement
or stalled physical work fails the original bounded proof, never granting a fallback.

## 6. Compatibility

Development only. No legacy dock path, duplicate store or checkpoint migration.

## 7. Verification

One focused new authored-dock case, existing timing audit to protect original durations,
then canonical preparation and saved physical harbour stages under original deadlines.
Keep evidence scopes separate. No full/profile/family runs or publication.

## 8. Risks

Exterior slots can face water or another facility; engine route/placement validators
must select/refuse them. More physical departure may extend elapsed construction time,
not shorten authored work. Construction completion/removal still uses original owners.

## 9. Out of scope

Generic arbitrary-road yielding, changed world geometry/materials/durations, teleports,
forced job release, extra cash, business/Draw/platform work, full tests or Git publication.

## 10. Execution order

1. [x] Inspect actual native geometry, footprints, homes and structural route.
2. [x] Author existing site dock data and validate the same resolved contracts.
3. [x] Verify original timing and bounded actual physical construction/handling.
   All18 definitions metadata-validated; three canonical harbour facilities complete
   through real supply/work/departure at8051 on2.646. Not all18 end-to-end builds,
   full marine acceptance or universal traffic liveness.
4. [x] Record all results and leave unproven original/full gates open.

[Evidence and canonical physical outcome](../../docs/migration/P5_CONSTRUCTION_SITE_DOCKS_VERIFICATION.md).
