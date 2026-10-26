# P9-A5 — infrastructure plans and portable corridors: focused evidence

Historical A5 checkpoint, not rerun by the current
[A6 removal-plan slice](../../plans/enhancements/P9_REMOVAL_PLANS_DESIGN.md). The source hashes and
counts below identify that checkpoint; the immutable A5 manifest is not a current post-A6 manifest.

2026-10-07. [Stage](../../plans/enhancements/P9_INFRASTRUCTURE_PLANS_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Source and ownership at the A5 checkpoint

Build **beep-simengine-2.173.0**, save95, WorkBoard28, WorkSystem30, work view55. Infrastructure
view5/LineService4/LineWork codec2/native recurring6/public4 and serialized owner layouts unchanged.
Existing exact build/content fingerprint gate rejects old semantics; no compatibility reader.

LineDef now implements the existing IPlanTemplateDefinition/IPlanDefinition. Stable line:<authored
ID> keys resolve actual master content. Numeric lay:marker keys and their parser are removed. Strict
immutable space/ordered-tiles arguments identify intent before any entity is allocated. Structural
submission uses LineService.CheckIntent; actual activation uses the same owner's current placement,
ownership, rules and duration/capacity checks. Blocked intent does not create a cursor or stamp tiles.

IPlanDefinition.BindExecution binds the actual native graph. Fixed activity/construction definitions
return their sole immutable graph definition; actual LineWork holds only a transient immutable graph
alias derived from its existing Line/Tiles. Decode invalidates that alias and reconstructs from actual
cursor content. One representative segment validates authoring, never stands in for physical path
work. WorkBoard uses the same binding contract at activation, load and checkpoint validation. No
parallel plan catalog, execution state store, serialized graph or scheduler is added.

LineDef owns strict path/template reading, identity, capture and translation through selected
ISpatialPlacementTemplates. The existing generic board handles mixed construction/corridor groups
and native/public recurring templates without an infrastructure branch. LayLineCommand now submits
a normal plan; Arg0 is its plan ID, not tile count, and the actual target comes from WorkView. Other
result consumers are migrated directly. LineService.Begin remains the single physical primitive.

Mixed proof exposed stale supply obligations inside shared dispatch. WorkSystem now publishes actual
eligible node supply before dispatch and after each successful acquisition before the next offer
rebuild. CopySupply, native bindings, Bill and existing demand identity are unchanged owners. Claimed
material cannot immediately be treated as unmet demand. No forced cargo cancellation/refund, claim
erasure, fabricated stock or retry-policy change is introduced.

Exactly one unchanged GameClock owns time. No corridor clock/update loop or platform rendering.
GameClock SHA256:339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31.

## Final commands and two gates

`dotnet build Beep.SimEngine.slnx --no-restore`: clean, zero warnings/errors; final elapsed2.61s.
Only the following exact names ran, including same-name repair reruns:

| Exact suite | Passed | Failed | Final elapsed |
|---|---:|---:|---:|
| Infrastructure plans: authored corridors, templates and admission | 144 | 0 | 474ms |
| Infrastructure plans: physical segments, controls and saved continuation | 313 | 0 | 3042ms |

Total **457**, zero final failures/aborted suites. LineSegmentTests' two existing registrations are
promoted, including their prior DAG/physical supply/ownership/refused promise/earned prefix/recovery
cases, not separately rerun as additional suites. The new partial InfrastructurePlanTests supplies
JSON, commands and observations only. Bounded waits report plans, orders, endpoint stock/promises and
jobs; terminal plan failure stops the wait early.

infrastructure-plans.json is embedded and explicitly listed in Colony.Factory's existing master
declaration. Its native/public recurring definitions reuse ordinary source contracts. Public proof
assembly has no engine internals. No harness/demo game logic is introduced.

## What is proven

- Actual stable content aliases, no numeric resolver, pre-allocation identity, frozen ordered paths,
  strict nonempty/space/extra-field schemas, native bounds, repeated/noncontiguous path refusals,
  duration overflow, pure/private views and whole-group duplicate no-prefix admission.
- Pending/paused corridors create no cursor/order. Generic capture translates relative path through
  selected tile provider. Mixed master-listed structure/corridor templates use the same board.
- Actual four-segment activation binds the real four-node graph, not the one-node authoring shape.
  Machine travel, native input/cost acquisition and earned tile effects occur over game time. Pausing
  after an earned segment preserves prefix/progress; regroup/resume/save/load continue identically.
- Finished four-segment corridor consumes four real units and20 money. Mixed corridor/tower finishes
  both real facilities, consuming independent four-plus-thirty material bills after the supply fix.
- Active plan cancellation retains the single earned tile, spends exactly one segment bill, stamps
  no future tiles and uses normal physical recovery. Native/public recurring groups complete first
  corridor but cannot repost while the later paused corridor remains live; disable/cancel/save use
  existing source and board owners.
- Prior fast-worker proof still advances at most one physical segment per Update. Current changed
  line ownership fails the later segment without removing earned prefix. A real external inbound
  promise refuses marker handoff without erasing its receipt; normal release allows cleanup.
- Checkpoints reject an active corridor path differing from its admitted intent before writing.
  Waiting/active/partial/paused/ended/cancelled/mixed/recurring states and interleaved twins remain
  deterministic through cursor retirement/order pruning. Sole-clock pause stops all corridor work.

## Repairs and review

Compilation caught an invalid JsonValue.Items assumption; replaced with its actual indexed API.
Existing corruption checks expected malformed state to be written then refused on load. Since active
corridors now bind immutable intent, writing is correctly refused; tests directly reflect that guard.
Mixed proof initially failed with sim.resource.active-promises. More precise diagnostics isolated an
18-unit surplus haul created when assignment claimed stock but the earlier supply request remained.
Refreshing only after the entire pass was too late: mixed offers are rebuilt inside that pass.
The final sole-owner refresh occurs before dispatch and after each successful acquisition. The
unchanged authored retry profile still refuses genuine external promises in the included prior case.
No fixture shortcut or shortened physical scenario hides the defect.

SRP: LineDef owns intent/portable schema, LineWork owns cursor and its derived graph alias, LineService
owns physical line/topology, WorkBoard owns intent/order state, WorkSystem owns advancement/dispatch
and fresh supply obligations. DIP/OCP: generic binding/template contracts support native/public
sources without concrete infrastructure handling in the board. Fixed graph providers use the same
binding contract, not a legacy fallback. Resource/time/geometry owners are unchanged. Lifecycle skill
aligned stage, implementation, scoped verification and tracker.

87 build-identity/board expectation files and20 work-schema expectation files migrate mechanically
(overlap). Most are compile only. StandardTests/ResourceRecoveryTests result consumers migrate but
are not runtime rerun. Single/recurring construction, dispatch/input/reserve/facility/demand-provider
and other suites compile only; previous evidence is historical, not summed as current regression.

Independent point/volume corridor execution providers, survey/upgrade/removal templates, cross-entry
dependencies, runtime libraries/history/pruning/global purity/replay/lockstep/fuzz/soak/performance/
full framework acceptance remain open. No OilFieldDays/Tycoon/host/demo/platform/Draw or third/family/
full test run. Full goal stays active. See
[scoped source manifest](P9_INFRASTRUCTURE_PLANS_SOURCE_MANIFEST.json).

## Evidence reconciliation

At the A5 checkpoint, the scoped manifest identified110 source/proof/ownership-reference files and9 documentation paths,
not the full working tree. All110 checkpoint SHA256 values matched; all9 documents existed; all7 relative
links in this stage/evidence pair resolve. Scoped source trailing-whitespace checks and tracked
diff whitespace checks pass. The GameClock hash above remains unchanged. These static checks do
not replace runtime proof or enlarge the two-suite result to a full regression claim.
