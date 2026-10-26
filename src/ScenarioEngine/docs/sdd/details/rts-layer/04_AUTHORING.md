# RTS and base-defence layer: scenario and definition authoring

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [RTS and base-defence layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Concrete design assets

Start with the [filled definition](examples/definition.json) and validate it against the [standalone schema](contracts/definition.schema.json). The example is original engineering test configuration. It is not a shipping scenario, approved game tuning, or a fixture that proves the future layer exists.

## Setup sequence

1. Choose the registered RtsLayer layer and exact profile/version. Require the dependencies listed in the package index before the scenario becomes selectable.
2. Assign stable IDs and create the definition document; specify all settings and units explicitly.
3. Create/resolve every referenced item, character, activity, route, scene role, effect and schema this definition uses. Do not turn an unresolved reference into a default.
4. Run structural validation, then the package semantic rules: Group membership, input observation budgets and native activeActorBudget are separately validated; production definitions refer to real producers and recipe schemas.
5. Declare initial state and the sources/sinks of resources, time, information and outcomes. Check the budget below against a complete path through the operation.
6. Author the supported transitions and their rejected/interrupted variants. Use the first operation, rts.group_order, as the beginning of the minimal behavioral case and record the resulting committed state.
7. Map permitted UI actions/views and native roles. Native scenes/resources are trusted role registrations rather than executable paths supplied by public JSON.
8. Run the [acceptance matrix](06_VERIFICATION_DELIVERY.md), including save/replay continuation and real native evidence where required, before offering the content as playable.

## Resource and state budget worksheet

| Budget | Author must declare/check |
| --- | --- |
| Identity | Definition/profile/content versions and every referenced owner/instance; uniqueness and lineage boundaries. |
| Quantities | Initial/free/reserved/consumed stock, destinations/capacity and every terminal effect; no negative balances. |
| Time | Selected unit, advancement/parent-clock policy, deadlines and mandatory interruptions. |
| Information | Who may see choices, targets, estimates and outcomes; unknown facts remain explicitly unknown. |
| Instances | The bounded native/active versus persistent logical collections; explicit overflow/refusal behavior. |
| Closure | Supported success/failure/cancellation results, next state/return route and final versus scoped completion. |
| Recovery | Exact checkpoint and pending work required to resume; what a failed native/I/O operation preserves. |

## Integration with existing JSON packs

The current loader accepts its existing normalized content schema and registered layer ContentSchema. This new standalone definition is not an automatically supported top-level document kind. Implement the required DTO/schema, loader reference rules, capability registration, state codec and complete consumer together; then embed/reference the definition through the selected layer's validated layerData or an explicitly supported pack document type. A JSON file cannot introduce the missing runtime.

## Author definition of done

All references resolve; semantic budgets and supported branches are checked; invalid examples are refused with paths/reasons; a supported operation has an observed runtime route; save/replay continuation agrees; native geometry/UI works where selected. Keep evidence separate from these proposed assets. No new artwork or external library was installed by this SDD task.
