# P9-A4 — authored recurring intents: focused evidence

2026-10-07. [Stage](../../plans/enhancements/P9_AUTHORED_RECURRING_INTENTS_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Current source and scope

Build **beep-simengine-2.171.0**, save95, WorkBoard27, WorkSystem28, work view53, motion view6,
MovementSystem6, native recurring codec6/public recurring codec4. Existing serialized layouts unchanged.
Full content plan keys and explicit arguments replace the old activity field directly; old builds
are rejected by the existing exact build/fingerprint gate, not converted.

RecurringPlanContracts resolves IPlanDefinition exclusively through WorkBoard's existing resolver.
Native/public definitions no longer resolve ActivityDef or republish its graph. AuthoredIntent carries
frozen JSON; ordinary target modes require arguments:null and the same target-intent provider contract.
Composition validates graph/time/roles/effects and arguments. Reserve/facility exemption cannot be
borrowed by arbitrary JSON intent. Existing entity-target orderability/target validation remains
contract-owned. Source observation uses existing Kernel.EntityObservation rather than a private guard.

One unchanged GameClock is still the sole time owner; its SHA256 is
339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31.
Cadence derives from last accepted intent and that clock. No source current-time field, repetition
system, alternate board, shadow construction graph, material balance or instant completion is added.

## Commands and final gates

`dotnet build Beep.SimEngine.slnx --no-restore`: clean, 0 warnings/errors; final build elapsed3.43s.
Only these exact suite names ran (same-name repair reruns included):

| Exact suite | Assertions passed | Failed | Final elapsed |
|---|---:|---:|---:|
| Authored recurrence: plan contracts, JSON and private admission | 272 | 0 | 1232ms |
| Authored recurrence: physical construction, repeat and saved continuation | 199 | 0 | 2570ms |

Total **471**, zero final failures, zero aborted suites. The two existing recurring suites were
promoted to these names and extended in the same partial test class, not additionally rerun by a
family filter. Earlier inspection/maintenance/interval/codec/public-provider cases remain included.
New proof is independently master-listed recurring-plan-intents.json through the existing PlanProofModule.
Its independent public recurring provider is not a native wrapper or harness gameplay implementation.

## What these gates prove

- Missing/unknown/non-intent plans, null/array/invalid/extra/foreign-space arguments, incompatible
  targeting, selected-source worker nodes/reserve purpose/facility policy and obsolete fields refuse.
- Actual construction definition aliases, frozen arguments, private/pure views, fresh source origins,
  admitted timestamps, detached retained observations, manual duplicate and native/public ranked
  collision use the existing board and typed admission.
- Ordinary enable/disable/priority/interval/group/pause/cancel commands govern actual physical work.
  An outstanding paused, active or occupied pending plan prevents another source posting.
- Native/public sources complete two physical point constructions: eight real hauled inputs each
  from an initial20; only4 remain. A third plan remains unfinished under the actual shortage.
  Cancellation recovers/preserves those4 in actual endpoint inventories, never refunds consumed16.
- The selected point provider permits coincident placements. The engine invents no extra spacing
  policy. Native occupied tile placement instead remains pending with Codes.Blocked; normal timed
  demolition frees placement, and ordinary promotion creates its real site only afterward.
- Saved paused/active/ended/partial-supply/pending-placement/source states, real order pruning,
  interleaved twins/session isolation and sole-clock pause remain deterministic.
- Forged valid-but-different authored request arguments and mutation of clock pacing fault actual
  Update before clock completion; the faulted game cannot checkpoint partial gameplay.

## Repair evidence

First compilation identified a proof assertion incorrectly asking ComponentDef for AuthoringPath;
the assertion now reads the real owning EntityDef metadata. First physical run found three included
older assertions expecting Codes.AlreadyDone instead of the existing source-specific Outstanding
code, and a newly arranged point fixture whose worker policy was authored disabled. Assertions were
updated to the actual source link semantics; physical proof enables that worker through the normal
ConfigureWorkPolicy command. No engine fallback, fabricated input or shortened workflow was introduced.
Same two gate names only were rerun after the fixes and final provider guards.

## Ownership review and remaining work

SRP/DIP: WorkBoard resolves/admits/stores, WorkSystem executes, existing construction/placement
providers own sites and geometry, ports/transactions own stock and receipts, and sources own only
settings plus last admitted reference. OCP/LSP: IPlanDefinition handles provider-owned arguments without
scheduler changes; IEntityTargetPlanDefinition owns static targeting as well as intent creation.
ISP: generic sources need no target-intent capability unless their explicit binding uses it.
The lifecycle skill kept this stage, code, tracker and scoped verification together.

81 source/test build-identity files mechanically migrate; most are compile-only, including prior
stage acceptance assertions. Other migrated home/facility/reserved-service suites and original plan/
template/input/dispatch/entity/state/provider suites compile only, not runtime revalidated here.
Historical manifests/test counts remain historical and are not summed as current regression proof.

Grouped automatic templates, runtime editable repeat libraries, history lifecycle/pruning, arbitrary
provider private/global mutation, new workflow-provider proof, replay/lockstep/fuzz/soak/performance/
allocations/full framework acceptance remain open. This is completed P9-A4 focused work, **not**
P9 or full-plan completion. No third/family/full test run. No OilFieldDays/Tycoon/host/demo/platform/
Draw changes. See the [scoped source manifest](P9_AUTHORED_RECURRING_INTENTS_SOURCE_MANIFEST.json).
