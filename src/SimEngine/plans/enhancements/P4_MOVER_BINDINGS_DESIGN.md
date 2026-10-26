# P4-G32 — Contract-owned reusable tile movement

## 0. Problem

Standard/Movement.cs Mover still inherits Component<MoverDef>, despite generic IMotion/IMobility/
navigation contracts and P3-S12 space metadata. Independent JSON component definitions cannot bind
the original native route owner. No new movement scheduler or parallel position model is appropriate.

## 1. Goals

1. Native/independent metadata binds the original Mover through ITileMoverDefinition.
2. One MoverDeclaration reads classes, initial selection, positive speed and paired entity modes.
3. Actual loaded reusable metadata/provider/state/geometry pairs validate before Initialize.
4. Original movement, work, navigation selection, immutable facts, codec4 and one clock remain sole owners.
5. ONE NEW bounded player proof covers physical authored travel/work and independent metadata refusal.

## 2. Design

ITileMoverDefinition extends original IMobilityDefinition/IMotionNavigationDefinition with actual
ITileSpaceDefinition and EntityStateBindings. ITileMover composes existing motion/area/estimate/
navigation/state/checkpoint contracts. Mover becomes Component consuming that actual metadata.
MoverDeclaration owns existing JSON and resolution/local presence-to-space validation. Native
MoverDef and independent ComponentDef<Mover> delegate it without native authoring inheritance.

Original MotionNavigationContracts validates paired reusable contracts and actual owned state/space
aliases for every loaded entity before Initialize. Original selection validation remains its sole
owner; declaration delegates it. No second loader/catalog/class list/current navigation/route/clock.
Original route/ETA/occupancy/arrival/work/stock/state/Sync algorithms stay authoritative.

## 3. API

ITileMoverDefinition.SpaceDefinition/States; ITileMover existing capability composition;
MoverDeclaration.Read/Resolve/ValidateBinding and frozen properties. Native MovingState/StoppedState
convenience selectors removed in favor of actual state bindings. No compatibility base/old reader.

## 4. Files

Standard/Movement.cs, MotionNavigation.cs, Navigation.cs, MotionAreas.cs, MotionArrival.cs,
MotionExecution.cs; original composition validation and catalog lifecycle;
independent engine proof and thin new player case; shared authoring helpers extended, historical
case not rerun. Existing space proof alias query consumes ITileMoverDefinition.

## 5. Errors

Missing/empty/duplicate navigation classes, unlisted initial selection, invalid speed, missing/foreign
state/space/presence, false reusable contract pairing and malformed unused metadata refuse before
Initialize. Live actual-provider observations use existing motion/route/state observation boundaries.
No implicit route choice/speed/default state, instant placement or healthy faulted checkpoint.

## 6. Compatibility

Development only: native metadata dependency removed in place; original mover codec4/save154/
replay1 unchanged. Build identity changes. No legacy loader/shim or duplicated state.

## 7. Verification

ONE NEW exact bounded case: Mover bindings: authored contracts drive physical tile travel and work.
Native/independent movers in independent JSON space; real partial travel, normal route-selection
commands, physical survey/material consumption, immutable/private facts, in-travel save/load twins,
single-clock pause, replay and isolated sessions. Early malformed loaded metadata and scoped actual
speed callback failures. Compilation and this new case only; no historical/family/full runtime.

## 8. Risks

Codec validation reads authored geometry/navigation; this slice does not prove every malicious
metadata callback/cross-provider/whole-world sandbox. Native/provider route matrices, arbitrary
spaces, geometry extremes, fuel/areas/docks/congestion/replanning/removal, full accounting, lockstep/
concurrency/fuzz/soak/allocation/performance and full P1-P12 acceptance remain OPEN.

## 9. Scope

Engine only; no Tycoon scope expansion, business/defaults, renderer/Draw/platform SDK or GameClock
edit. Nothing staged/committed/pushed until the entire goal and plan are finished and verified.

## 10. Order

Inspect original owners → sole declaration and paired contracts → replace native component binding →
actual metadata validation → independent JSON engine proof → thin player case → focused build/runtime →
record actual evidence and refresh master/audit without closing unproved original gates.

## 11. Current scoped result

Implemented paired contracts/shared declaration/original runtime binding; original selection/route/
area/arrival/status reads guard actual provider codec. Core2.369.0/save154/replay1; clean builds5.70s/
5.56s after initial missing import. ONE NEW exact case final269/0/0 in959ms (first268/1/0 exposed
NavService enumeration before null-class validation; original validator now precedes enumeration).
Native/independent original physical travel/route change/exact ETA/work/materials/saved twins/pause/
replay/isolation,36 early malformed binding refusals and10 scoped provider faults verified.
165 selected inputs frozen11:46:13UTC; no subsequent executable edit.
[Scoped evidence](../../docs/migration/P4_MOVER_BINDINGS_VERIFICATION.md).
All broader provider/condition hierarchy/full/global gates remain OPEN; no publication.
