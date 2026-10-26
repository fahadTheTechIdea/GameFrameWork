# P9-A6 — persistent JSON-authored removal DAGs: focused evidence

Historical A6 checkpoint, not rerun by the current
[A7 portable removal-template slice](../../plans/enhancements/P9_REMOVAL_TEMPLATES_DESIGN.md).
Hashes and counts below identify that checkpoint, not a current post-A7 manifest or regression run.

2026-10-07. [Stage](../../plans/enhancements/P9_REMOVAL_PLANS_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Source at the A6 checkpoint

Build **beep-simengine-2.174.0**, save95, WorkBoard28, WorkSystem30, work view55, construction view4.
Existing mutable-state layouts are unchanged. The exact build/content gate rejects previous source
semantics; there is no compatibility reader or old command-result alias.

ConstructibleDef's existing DemolishPlan now implements IRemovalPlanDefinition: the ordinary target,
player-admission, graph and authored-priority contracts. Its stable demolish:<prototype> key still
resolves through the existing construction resolver to the actual provider. BindExecution returns
that one immutable graph. ActivityWorkerSelection owns argument parsing, selection and identity;
WorkBoard owns intent, controls, native orders, Sync and immutable facts.

The old single-role/duration shorthand and generated node are deleted directly. Each authored
demolition object explicitly names priority, automatic_supply, salvage and nonempty nodes. Each
node explicitly declares roles/duration and uses the existing activity DAG reader, dependency
compiler, typed requirement resolver and effects. At least one physical machine stage is required.
Absent removal authoring is non-orderable, not an instant/default workflow. Three existing proof
JSON files and an inline native fixture migrate directly; the role-corruption probe is updated.

Structural plan submission may record a real target while evacuation is currently blocked. The
existing physical removal owner supplies activation blockers. Immediate DemolishCommand and
ConstructionView.CheckDemolition use IConstructionProjects.PreviewDemolition for matching current
preflight, then submit to the same board. Arg0 is the plan ID; Entity is the actual target. The
command uses JSON-authored priority, not a framework constant. DurationProgressTests' result consumer
migrates through WorkView; it is compile-only in this gate.

No scheduler, entity/work state, supply/receipt/stock authority or clock is added. One unchanged
GameClock owns all game time. SHA256:
339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31.

## Final build and two exact gates

`dotnet build Beep.SimEngine.slnx --no-restore`: clean, zero warnings/errors, final elapsed3.22s.
Only these exact existing ConstructionPlanTests names ran, including same-name repair reruns:

| Exact suite | Passed | Failed | Final elapsed |
|---|---:|---:|---:|
| Plans: authored intent and construction admission contracts | 174 | 0 | 599ms |
| Plans: physical sites, pause and saved continuation | 235 | 0 | 1348ms |

Total **409 assertions**, zero final failures/aborted suites. Existing construction/activity callbacks
are included beneath these registrations; they were not separately run as additional suites.
The preceding infrastructure suites were not rerun or added to this total.

## Proven behavior

- Exact authored graph aliases, public contracts, strict immutable intent, company-private previews
  and views, canonical machine selection, duplicate live admission and pending save/load.
- Missing priority/supply/salvage/duration/roles, unknown roles/dependencies, cycles, old shorthand,
  empty DAG and instant-machine authoring fail before Update. Missing removal authoring is disabled.
- The new explicitly master-listed removal-workflows.json introduces a facility and two dependent
  removal stages with a real material bill; no existing scheduler or system changes are needed.
- Real player construction precedes removal in native tile and independent point worlds. A foreign
  inbound promise blocks immediate preflight and pending activation without being erased. Releasing
  the actual token permits one ordinary native order to be admitted.
- Machines move and earn elapsed work. Native dependent removal is performed by the explicitly
  selected machine after isolation finishes. Policy pause freezes earned progress without destroying
  the structure or advancing separate time. Saved twins continue identically through removal.
- Fifty real units minus thirty construction inputs minus two removal inputs plus fifteen authored
  salvage equals33. Removal/destruction and terminal plan history save with the old target gone;
  repeat refuses NotFound rather than choosing another entity.
- Active cancellation keeps the facility and its actual two spent inputs, yields no salvage,
  and saves through normal cleanup/history. Immediate command returns the actual plan/target and
  uses the proof JSON's67 priority. Sole-clock pause stops all activity.

## Repairs and scope

Compilation caught a duplicate System import and an ambiguous test Json alias; both were removed.
The first physical run correctly blocked on the point fixture's disabled work policy. The proof now
uses its existing player configuration command, as the original point construction callback does;
no engine fallback or changed worker default was introduced. Bounded waits report actual plan state
and blockers. Final two suites are green after repairs.

81 build-identity expectation files migrate mechanically, mostly compile only. Other demolition,
resource-recovery, duration, role, extension, dispatch, recurring, incident and replay suites compile
but were not runtime rerun. Public ExtensionProof compiles without engine internals. Fixtures use
initial-world setup and ordinary player commands/observations, never implement gameplay rules.

SRP/DIP review: content owns the graph and priority, the existing selection owner owns machine intent,
the board owns plan/order state, the construction service owns preflight/completion/recovery, and
the game clock alone owns time. The lifecycle skill aligned implementation, stage and focused evidence.

Portable spatial removal templates, survey/upgrade providers, cross-entry dependencies, editable
libraries/history lifecycle, wider provider purity/replay/lockstep/stress/performance/full acceptance
remain open. No OilFieldDays/Tycoon/host/demo/Unity/Godot/Draw or third/family/full test run. Full goal
stays active. [Scoped manifest](P9_REMOVAL_PLANS_SOURCE_MANIFEST.json).

## Evidence reconciliation

At the A6 checkpoint, all107 scoped source/proof/ownership hashes matched; all10 documentation paths existed; all6
relative links in this stage/evidence pair resolve. Four affected authored JSON documents parse.
Scoped source trailing-whitespace and tracked diff whitespace checks pass. Removed role/duration
fields and old demolition shorthand are absent from the migrated engine/proof authoring. The sole
GameClock hash remains unchanged. These static checks do not enlarge the two-suite runtime result.
