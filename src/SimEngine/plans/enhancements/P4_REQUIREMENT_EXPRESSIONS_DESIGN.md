# P4-G2 — Authored requirement expressions, candidates and acquisition

Status: G2a focused-verified on 2026-10-05: **213 checks / two exact named suites**, zero
failures/aborts, build2.46.0/save21. See [evidence](../../docs/migration/P4_REQUIREMENT_EXPRESSIONS_VERIFICATION.md).
G2b1 worker candidates/committed branches are subsequently focused-verified (192 / 2, build2.47.0/save22).
See [worker design](P4_WORKER_CANDIDATES_DESIGN.md). G2b2a/G2c1 resource source/tool wiring is subsequently
focused-verified (159 / 2, build2.49.0/save24/work9/view9), including actual node receipts, physical
startup consumption, held tools, interruption and saved ownership. See
[resource design](P4_RESOURCE_REQUIREMENTS_DESIGN.md) and
[evidence](../../docs/migration/P4_RESOURCE_REQUIREMENTS_VERIFICATION.md).
G2b2b/P9-A1 subsequently verifies immutable JSON target-region leaves through that same evaluator:
**73 / two exact suites**, build2.52.0/save26/work11/view11. Full-footprint rectangle/painted shapes,
private pure facts, physical pause/re-entry/cancel and non-tile saved twins pass. See
[target-region stage](P4_TARGET_REGIONS_DESIGN.md) and
[evidence](../../docs/migration/P4_TARGET_REGIONS_VERIFICATION.md). Editable/all-dispatch/route policies
remain open; no second region store/selector is introduced.
Remaining G2b/G2c stay required; this document stages the same approved G2,
not a replacement for its provider/tool/resource acquisition scope. Parent: P4_REUSABLE_MODULES_DESIGN.

## Problem and goals

The former positional Requirements list and implicit AND evaluator have been removed. Each node
now carries one authored IWorkRequirement reference, supporting nested named alternatives and
individual blockers. WorkRequirementDef is extended in the
existing content registry, not a parallel requirement catalog, scheduler or state owner.

G2a acceptance: named all/any groups from master-listed JSON, arbitrary forward references,
strict missing/duplicate/cycle admission, iterative deterministic evaluation, typed invalid/pending/
blocked/satisfied aggregation, immutable private per-entry views, and normal timed/machine/save/cancel
execution. No input, money or worker is spent/reserved during pure requirement evaluation.

## G2a contracts and single owners

- ActivityNodeDef/IActivityNodeDefinition.Requirement is one optional IWorkRequirement reference.
  JSON uses requirement: "id"; remove flat requirements arrays and params-array Make authoring.
  A null reference explicitly means no additional guard, not an implicitly supplied requirement.
- WorkRequirementGroupDef derives from WorkRequirementDef in section work_requirement_groups.
  Author mode: all|any and nonempty entries [{id,requirement}]. Stable entry IDs and referenced
  definitions are unique within each group. Resolve immutable aliases through the sole content set.
  Nested groups implement IWorkRequirementGroup; leaves keep the existing IWorkRequirement contract.
- Extract the current ActivityGraph dependency algorithm into one reusable DependencyGraph<T> over
  IDependencyNode. ActivityGraph retains typed immutable nodes/physical ancestry, delegating topology
  to that sole compiler. Requirement references use the same iterative topology/cycle validator.
  WorkBoard validates all selected requirement definitions before Initialize. No second compiler,
  global requirement store, hidden content default or arbitrary graph/expression depth cap.
- WorkBoard remains the one evaluator. An iterative frame stack evaluates every declared entry,
  including unselected alternatives, and validates leaf results. Invalid always dominates (never
  hidden by a satisfied alternative). For all: blocked dominates pending, otherwise satisfied.
  For any: satisfied dominates pending, pending dominates blocked. Ties use ordinal entry ID.
  Group results never sum incompatible quantities; leaf observations remain individually measured.
- Any satisfied result exposes the stable SelectedEntry as a pure readiness fact, NOT an acquired
  provider, durable branch commitment or authorization. G2b1 now stores committed selection explicitly
  on the existing ActivityNodeExecution while the job owns its actual worker claim. G2c1 now owns
  actual resource/tool receipt links and physical lifetime; preview choices never authorize spending.
- WorkView reuses the same evaluation traversal for immutable per-entry paths, provider IDs/results
  and group outcomes. Paths are detached segment lists, not ambiguous concatenated strings.
  Private/pure reads disclose no runtime/provider handle. No host implements expression semantics.
- Existing named execution, materials, cash, jobs, wide progress, Sync and entity state remain owners.
  New expression results are derived readiness, not an additional saved progress/choice ledger.
  Break engine/content/view identities together; no old array parser or compatibility wrapper.

Example accepted G2a syntax:

```json
{
  "work_requirement_groups":[
    {"id":"field.safe","mode":"all","entries":[
      {"id":"permit","requirement":"site.ready"},
      {"id":"reserve","requirement":"company.reserve"}
    ]},
    {"id":"field.authorized","mode":"any","entries":[
      {"id":"normal","requirement":"field.safe"},
      {"id":"emergency","requirement":"site.emergency-authorized"}
    ]}
  ],
  "activities":[{"id":"field.prepare","automatic_supply":false,"nodes":[
    {"id":"prepare","roles":["crane"],"hours":1,"requirement":"field.authorized"}
  ]}]
}
```

## G2b — Typed candidates and durable selection (worker slice verified; remainder required)

Extend the same requirement family/context with scope and immutable typed candidate offers for worker
roles, tools, sources/targets and allowed areas. Preview returns actual availability/cost/refusals,
not an acquired provider. WorkSystem assignment uses those offers and existing travel/capability checks.
Store chosen entry/provider identity only on the existing named node/job; scope/owner/generation and
active references validate on load. Reselection releases owned unspent commitments, preserves real
progress/cargo and reports a typed reason. No silent default, privileged entity lookup or new selector
state store. G2b1 has removed single-role node authoring and implemented actual worker offers/claims,
nested committed branches, pause/save/reselection/cancel and private views. G2b2a adds scoped source/
tool offers, joint native selection and ownership proofs; area/target policies remain required.

## G2c — Atomic acquisition and physical spending (startup resources verified; remainder required)

Selected all/any requirements acquire real stock/tool/provider commitments all-or-none using P7
tokens and existing worker.Job/WorkBoard ownership. No new reservation ledger. Spent consumables
stay spent; continuous fuel/fluid is consumed over actual elapsed work. Cancellation/loss releases
only its commitments and uses actual physical cargo/WIP recovery. Save/hash/replay include selected
branch/provider/token identity via the existing node codec. G3 generic executor declaration remains
separate and replaces closed toils rather than layering another scheduler.

G2c1 now connects owned consume/hold receipts to the same node/job lifetime. Reach and positive work
gate once-only startup consumption; pause retains tools, interruptions release unspent claims and
preserve spent branches/progress. G2c2 subsequently deletes Needs/Cash/OnCredit, TrySupply and early
spending through the same named requirements, supply/jobs and node codec. Aggregate ordinary-cost
admission precedes assignment; sole StartWork rechecks current money and receipts after reach/positive
work and pays/consumes once. Exact multipliers support generated lines; salvage is explicit native yield.
Its build2.50.0/save25/work10/view10 passes **245 / two exact suites**. See
[work-start stage](P4_WORK_START_DESIGN.md) and
[evidence](../../docs/migration/P4_WORK_START_VERIFICATION.md). No new money ledger or reserved budget
is implied. G2c3 subsequently verifies continuous work-normalized native fluid/fuel budgets, actual
next-input supply, source-only commitments, cumulative native quantization, fractional saved progress
and private immutable budget facts. AdvanceWork alone consumes/advances; no remainder ledger.
Build2.51.0/save26/work11/view11 passes **364 / two exact suites**; see
[continuous stage](P4_CONTINUOUS_WORK_DESIGN.md) and
[evidence](../../docs/migration/P4_CONTINUOUS_WORK_VERIFICATION.md). Area/target policies, elapsed-time
utility/idle/travel input breadth, budgets and broader provider/stress gates remain.

## Files, sequencing and verification

G2a: Kernel/DependencyGraph.cs (extracted algorithm), Standard/ActivityGraph.cs, WorkRequirements.cs,
Work.cs/WorkModule.cs, WorkRequirementResult.cs, Views/WorkView.cs and current standalone proof JSON/
consumers. Build2.46.0/save21/view7; work codec7 has no added state in G2a.
Read/resolve/reference admission → expression evaluator → shared private views → source/content
migration → two exact named suites → evidence/README/AGENTS/master/parent.

Only two suites: expression/graph admission and physical requirement execution/durability. Include
the directly affected G1 graph/physical methods within those exact gates (not all existing test families).
Stages are bounded with diagnostic failure; old migrated suites compile but are not broadly rerun.
No full gate without explicit user authorization. Track final-source counts, not repeated assertions.

## Risks and remaining gates

Invalid runtime capabilities are errors even inside any; authors must use pending/blocked for ordinary
unavailable alternatives. Providers are trusted executable modules, not a security sandbox.
Changing flat guard authoring touches generated/public proof callers; migrate once, do not preserve
aliases. Derived readiness can change before acquisition; it is not durable G2b selection.
Remaining G2b/G2c area/target/elapsed-time input/budget and broader acquisition/rebinding proofs,
G3–G5/P5 and broader framework
replay/stress/soak gates remain open. Oil, Tycoon, hosts/demos, Draw/platforms are not changed.
