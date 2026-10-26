# P5-A21 — Native construction workflows, sites and optional costs

## 0. Problem statement

Actual Oil construction components still declared inline steps/needs/cash/demolition role-hours.
The framework requires an explicit site and a bound authored activity, not an implicit project model.

## 1. Goals

Migrate all18 constructible structures and eighteen removal workflows without changing original node
identities, dependencies, roles, machine-hours, material quantities or18 monetary amounts. Native sites
must hold their whole material envelope and use the same footprint as the completed structure.

## 2. Design

construction.json contains36 non-orderable/unrestricted activities with52 nodes and18 material leaves.
It references the existing oil.activity.staffed executor from work.json; no second executor/scheduler.
Build priorities are explicitly50 (native activity default); no original priority was authored.
Construction/removal final effects remain the existing ConstructionService owners, not authored
standalone final effects. components.construction references site/activity/demolish.activity and the
existing under-construction entity state. Removal salvage is explicitly empty because no yield was
authored; no guessed proportional yield.

construction-sites.json declares eighteen separate site prototypes, exact target footprint/mobility/
blocking, empty finite stock, solid-phase input filters, oil.stock port and actual site.target.
The material sum in the existing storage-slot measure selects a shared capacity policy in inventory.json;
sixteen missing numeric limits added to sixteen prior limits, no duplicated identical policy values.
Capacity admission continues to use the actual WorkBoard supply envelope, not a copied runtime bill.

Eighteen unchanged construction costs/credit intent append to optional Tycoon business-costs.json,
now36 cost bindings total including prior18. Physical construction/work files contain no money.
Every canonical scenario explicitly selects construction.recovery=null. Empty completion/removal works;
stocked cancellation/removal requires real disposition and retains a typed missing-recovery blocker.
Complete Oil recovery content remains open; do not silently generate a container or discard resources.

## 3. API surface

Existing IConstructionDefinition/IConstructionSiteDefinition, IAuthoredActivityDefinition/node/
requirements, IConstructionProjects, IPlanBoard and IActivityCostBinding. Existing ConstructCommand,
CancelConstructionCommand, DemolishCommand and ConstructionView/WorkView/InventoryView.
No old inline authoring reader/alias or new project/state/clock owner.

## 4. Files

Canonical structures/inventory/business-costs/master/scenarios updated; new construction.json and
construction-sites.json listed once by master. Existing native proof registry gains ConstructionModule
as an available optional module; only the new fixture explicitly activates it. Linked canonical files,
one new thin focused driver and registration. Stage/evidence/master/audit updated in place.

## 5. Errors

Missing site/workflow/node/policy, bad graph or insufficient site capacity retain original content
refusal. Wrong/full placement retains native preview refusal. Recovery disabled is explicit null,
not implicit loss, instant return or a fabricated free resource/ledger.

## 6. Compatibility

Development migration only; old inline steps/needs/cash/demolish.role-hours are removed.

## 7. Verification

One new bounded exact case admits all18 projected construction targets/sites and36 actual workflows.
Canonical eight-hour pad material6cement/cost20000 runs physically, with material hauling, machine
progress, site state, saved twins, final publication and two-hour machine demolition. Money absent/enabled.
Other Oil domain components are excluded: this does not prove actual geological drilling, all18 runtime
builds, offshore vessels, business consolidation, recovery or complete Oil gameplay.
Public actual Oil preflight now refuses haul_truck mover missing class before Initialize/Update.

## 8. Risks

Normalized material capacity is not physical density. No authored salvage is not proof of historical
implicit salvage behavior. Explicit null recovery intentionally blocks stocked disposition pending its
proper content migration. Native fixture worker/motion does not prove canonical vehicle profiles.

## 9. Out of scope

Full regression, hosts/demos/platforms/Draw, invented yields/recovery, new construction/work/inventory/
financial/clock owner, business-rule changes and Git writes.

## 10. Execution stages

1. Migrate all construction/removal nodes, sites, finite buffers and optional financial bindings.
2. Explicitly configure recovery absence; advance actual-content loader.
3. Verify one new affected public-input physical construction case and static authoring parity.
4. Update evidence/master/audit, retaining all original acceptance gates.
5. Continue native vehicle class/role/state/handling, recovery content and sales/research consolidation;
   finish actual Oil physical/private/rival/durability acceptance through engine commands/views.
