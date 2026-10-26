# P12-A1 — Authored persistent incident phases

Current M72 on unchanged native2.723/save161/replay2 adds finite simultaneous
response scarcity proof: four named sources/two waves, one response machine/carrier,
local/native and opaque scoped/independent effects at minute/half-minute quanta.
Final10,626/0/16 episodes/six actual setup/continuation replays; original DAG,
retry, inventory and phase owners, no new scheduler/state or chemical propagation.
[Current source, exact replay scope, allocation metrics and remaining gaps](../../docs/migration/P7_CONVERSION_ACCOUNTING_REVALIDATION_VERIFICATION.md#current-m72-simultaneous-incident-scarcity-and-recovery--2026-10-22).
Earlier implementation/version-specific records below remain historical, not
current full-P12 or every-provider qualification.

Current ownership: [P12-A4](P12_INCIDENT_CONTENT_CONTRACTS_DESIGN.md) admits independent workflow/effect contracts after all content resolves; same WorkBoard/episode/phase owners. Historical runtime below not rerun on2.338.0.

Status: focused verified, 2026-10-06; full P12 remains partial. Parent: [P12](P12_INCIDENTS_ENVIRONMENT_DESIGN.md).
Build 2.94.0/save61; new incident component codec1, incident view3; other native layouts unchanged.
**110 checks / exactly two named suites**; [evidence](../../docs/migration/P12_INCIDENT_PHASES_VERIFICATION.md).

## Authority and scope

Extend the selected `sim.incidents` module, named `IIncidentResponse`, the existing activity DAG,
`WorkBoard` execution records, resource commitments and `IncidentsView`. No incident scheduler,
phase-progress table, repair command, emergency inventory, compatibility reader or host rule is added.

Master-listed JSON `incident_workflows` declares kind, response activity and named phases. Each phase
maps a nonempty set of native node IDs. Every node belongs to exactly one phase. The activity DAG,
not array order or a hard-coded oil lifecycle, owns sequencing, parallelism, requirements and effects.
Missing/duplicate/blank IDs, unknown nodes, uncovered nodes, invalid final effects and instant-only
responses fail before initialization. The workflow must finish through the existing explicit
`bound_response` effect, never another source or entity replacement.

The native named `incident` capability refers to the workflow and an explicit active entity-state
binding. Its one codec owns only episode, raised tick and resolved tick. `IPhasedIncidentResponse`
exposes workflow metadata and immutable lifecycle facts, allowing independent engine-side providers
without scheduler changes. It never owns work progress, material receipts or state transitions.
Entity.Update remains the sole entity-state commit owner.
The existing work board retains the exact current phased response episode, including completed/failed
history. Named incidents do not implement the singleton `IWorkOrderRetention` disposition capability;
multiple incidents may share one entity without competing ownership or another history store.

## Execution and cancellation

IncidentResponseSystem posts ordinary work. A cancelled response for the same source/episode resumes
the same authoritative order: completed nodes/effects, partial elapsed work, spent material/cost
bindings and selected alternatives remain; unspent released promises are reacquired by native work.
Only terminal scheduling flags/blockers are reset, and new jobs use normal travel/admission. A failed
response remains diagnosable and is not automatically retried every tick. A new episode gets a new
order and cannot accept the old episode's completion.

No stage advances through view reads. Phase observations are detached native node facts, including
dependencies, worker/job identity, progress, blockers and commitments; aggregate completion is derived.
Lifecycle facts distinguish never raised, active and actually resolved episodes. After resolution,
the last episode/order remains inspectable through the same board. Company-scoped views allow only
the owner or the explicit `PlayerId.None` observer, never arbitrary negative viewer IDs.

## Implementation stages

1. Add open workflow/phase contracts and strict JSON definitions in the existing module/catalog.
2. Add the native incident capability and one validated staged codec; retain 64-bit timestamps.
3. Extend existing response lookup/resumption, preserving node/receipt authority and episode guards.
4. Extend IncidentsView with immutable lifecycle/phase projections over native ActivityNodeView.
5. Prove a non-oil machine-leak workflow via a master-listed JSON file: detect → isolate, parallel
   repair/cleanup → restart. Native roles, timed travel and consumed/held material remain mandatory.
6. Build and run exactly two named suites, then update parent/master/evidence/README/help/AGENTS.

All six steps are implemented for this slice. The non-oil proof master-lists `incident-phases.json`
through the existing public Colony factory; the test harness supplies player commands and explicit
fixture/fault checkpoints, never implements response progression or material handling. New workflows
use authored DAG nodes/effects/requirements and phase labels; no scheduler switch is added.

## Focused acceptance

- Strict mappings, explicit final recovery, selected module ownership, immutable authored data.
- No instantaneous recovery, real travel/time, parallel stages and native blockers.
- Cancellation after partial work preserves progress/spending and completed effects, with fresh jobs.
- Save/load during active, cancelled and completed stages; interleaved hash-identical continuation.
- Private views, detached snapshots, pure reads, bad codecs and wrong/stale/forged completion refusal.
- Explicit new episode; no duplicate enrollment, job, consumption or completion.

These focused checks pass, including wide time, permanent-failure retention, rejected staged decode,
future-timestamp restore refusal and two named incident owners sharing one workflow. Existing response
cancellation assertions and28 build identity assertions migrate/compile only; no other suites ran.

## Remaining P12 work

Phase names alone do not isolate operations or networks. Operation gates, propagation, environmental
accounting, condition/maintenance, preparedness, failed-response retry policy, richer phase-specific
entity states, broader provider/fault/replay/stress and full P12/P1–P12 acceptance remain open.
No OilFieldDays, Tycoon, Unity, Godot, adapters, rendering or platform-size restrictions.
