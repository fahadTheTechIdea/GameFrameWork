# P3-S20 — required spatial motion providers before initialization

## 0. Problem and original requirement

Original P3.acceptance.004 requires missing spatial providers to fail composition without a default map. Native component registration already requires the selected `sim.tile-navigation` module in ResolvedContent.ReadComponents. Independently registered metadata can implement the same ITileMoverDefinition and use the original Mover while its active registration module selects only the motion phase. `NavService.ValidateComposition` owns selected tile-mobility class checks; when NavService is absent, those checks never run. `MotionNavigationContracts.ValidateDefinitions` validates immutable metadata but not its required selected navigation provider. That independent capability can therefore initialize before an actual route is requested.

## 1. Goals

For the selected motion phase, reject selected tile mobility without its actual tile space, NavService or each declared navigation class before any Initialize. Check unused selected-space entity definitions too. Do not require tile navigation for non-tile motion, nonspatial entities or content from an unselected space. Never create a map/router/profile fallback.

## 2. Design and API

Move the original tile mobility/presence/class checks into `MotionNavigationContracts.ValidateDefinitions`, the existing capability metadata owner called by MovementSystem during composition. The original NavService alone still owns routing/classes/search; its composition validator checks its exact session and selected tile-space binding and freezes registration. Delete its former entity-metadata loop rather than leaving duplicate validation owners. Metadata observations retain EntityObservation's existing purity/fault boundary. No new engine contract, service, state, schema, update phase, command, second clock or compatibility path.

## 3. Errors and risks

Missing selected provider or class returns ContentException during composition before Initialize. Wrong-session NavService also fails composition. Existing unselected-space filtering remains explicit; no optional module is silently selected. Runtime unreachable routes remain normal typed movement refusals, not configuration errors. Custom providers must still declare their own requirements; this native motion repair alone does not certify every arbitrary spatial extension or full P3.acceptance.004.

## 4. Files and proof

Modify MotionNavigation and Navigation; update current build identity. Add one negative/registration-only proof module using existing independent tile/mover definitions, one explicit four-module master, separated world/routes JSON and ONE NEW thin bounded driver. Native WorldSetup creates actual entities; native tile map/router/mover/clock handle all healthy motion. Test missing/mismatched selected map, absent navigation, missing initial/allowed class, unused selected movers and wrong-session provider; verify native/independent valid physical travel, obstruction, partial saves, arrival, replay and isolation. Existing non-tile physical fixture proves no tile requirement leaks into point motion.

## 5. Execution and limitations

1. Reassign the existing checks to their required consumer and delete the duplicate producer-side loop.
2. Author the explicit file-based composition fixture and ordinary-input case.
3. Build once at the completed slice; run exactly the new bounded case, repairing only that case if needed.
4. Record all attempts/current source hashes and update the master without claiming complete P1–P12 or broad spatial/provider gates.

No older/family/full/Oil/Tycoon/platform/runtime gate, Draw, renderer, adapter, legacy compatibility, default geometry or driver gameplay. Protected G19 authority remains pending; no staging/commit/push until full goal AND plan finish.
