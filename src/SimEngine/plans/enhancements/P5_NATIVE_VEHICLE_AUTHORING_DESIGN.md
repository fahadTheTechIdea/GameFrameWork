# P5-A22 — Canonical vehicle movement, work state and handling contracts

## 0. Problem statement

Actual Oil content stops at haul_truck mover missing class. All seventeen vehicles lack parts of the
explicit native mover/worker contract, and four transporters have no selected handling profile.

## 1. Goals

Migrate all17 vehicles in place, preserve speed/roles/storage/other components, bind native motion/work
states and four physical handling profiles. Land/sea remain authored navigation choices, not host logic.

## 2. Design

Each mover authors class, singleton allowed classes, unchanged speed and moving/stopped state bindings.
Ten previously implicit land classes are now explicit; seven existing sea classes retained.
Each worker authors original roles, base work_rate1 and working/loading/stopped bindings.
Base1 selects the neutral native work rate, not a second modifier store; existing Oil research
execution components remain unchanged. All live decisions/state transitions remain original owners.

Four transporter components select vehicle-handling.json profiles through IResourceHandlingProfile.
Each supported resource has a positive explicit nominal-hold quantity and loading/unloading one-hour
duration. Basis40/120/400/120 comes from original cargo capacities; accepted solid/liquid/gas resources
come from current canonical filters. This is NEW game-content balancing: no prior handling rate was
authored, so historical timing parity is not claimed. Linear partial quantities use the sole native
handling/time calculation. No implicit default, instant transfer, timer copy or cargo owner.

The next real loader error exposed showcase's old horizon_days1500. Move it to existing
oil.time.showcase.horizon as an exact1500-day rational duration, retaining the same authored amount.
Other five time definitions stay unchanged. No scenario compatibility reader or second clock.

## 3. API surface

Existing IMobility/IMotionNavigation/IStatefulWorker/ITransporter contracts and their sole native
declarations; MoveVehicleCommand/SetWorkerHeldCommand/HaulResourcesCommand and immutable motion/work/
inventory views. JSON declares all choices, engine executes them.

## 4. Files

vehicles/master updated; new vehicle-handling.json listed once. scenarios/game-modes move the one
legacy horizon field. Canonical linked resources, one new thin focused driver/registration and existing
fixture data selection. Stage/evidence/master/audit updated in place.
The shared proof master now declares the already-registered inactive ConstructionModule once; the
construction driver's duplicate declaration is removed. This repairs strict manifest admission,
without activating construction in unrelated scenarios or weakening engine validation.

## 5. Errors

Unknown navigation/roles/state/handling references and missing/invalid rates retain strict refusal.
Fluid carriers never accept solid cargo and vice versa. Wrong-class routes retain native refusal;
drivers never repair paths, teleport, transfer stock or directly set state.

## 6. Compatibility

Development migration only; missing-field defaults and horizon_days compatibility are not introduced.

## 7. Verification

One new bounded exact case loads all17 native base-vehicle projections and four handling profiles,
observes original roles/navigation, moves land/sea machines through commands, saves partial motion
and physically hauls cargo through each transporter with partial handling saves/conservation.
Other Oil research/wear/incident/sensor parts are excluded, so this is not full Oil vehicle/gameplay
acceptance. Showcase horizon authoring parity is static, not a1500-day end-to-end run.

## 8. Risks

New handling balance is explicit and editable, not preserved legacy timing. Native base projection does
not prove Oil modifiers/failure/fuel/weather or all scenario behavior. The real loader now reaches a
duplicate network connection on stabilizer; full content still cannot initialize.

## 9. Out of scope

Full regression, platforms/Draw/hosts/demos/adapters, business changes, new scheduler/state/cargo/
clock owner, instant fallback, duplicated engine mechanisms and Git writes.

## 10. Execution stages

1. Migrate all native vehicle declarations and explicit file-owned handling.
2. Move the single old showcase horizon into the authoritative clock definition.
3. Verify the new focused physical movement/cargo case and static full-authoring parity.
4. Record actual loader boundary/evidence/tracker without closing broad gates.
5. Continue network connection authoring, recovery and remaining financial ownership; complete actual
   Oil physical/private/rival/durability gates through engine commands and views.
