# P9-A2a — persistent authored activity intents

Historical stage on build2.69.0. [A2b](P9_CONSTRUCTION_PLANS_DESIGN.md) replaces its activity-only
API with PlanIntent/PlanView/PlanState and IPlanDefinition through the same WorkBoard authority.
No compatibility types/commands remain. The original 211-check evidence below is historical;
A2b's two suites retain this policy/physical proof and add authored generic construction.

## Scope and ownership

Implement the first persistent-plan slice in the existing WorkBoard. IPlanBoard aliases that same
instance; its existing Sync owns intent identities, ordering, grouping, pause and native execution
links. There is no second scheduler, resource store, graph catalog, progress record or project ledger.
ActivityPlanIntent names an existing JSON-authored activity PlanKey and owned target. The existing
master manifest, content compiler and requirement/effect providers remain the only authoring path.
IPlayerActivityDefinition owns pure player admission. ActivityDef implements it, and new workflows
can supply registered content/contracts without changing the plan loop.

This stage is NOT construction/template completion. Structure/infrastructure placement intents,
relative-position templates, automatic repeats and bulk group commands remain later work. A group
is an explicit named organization field, not a new graph or execution container.

## Lifecycle

1. Submit through the pure shared admission used by direct activity commands. Paused intent holds
   no order, job, worker, resource receipt or completed entity. Duplicate live intent is refused.
2. At WorkSystem's existing Update phase, unpaused pending intents revalidate current ownership,
   provider admission, content access, target compatibility and native duplicate execution. Priority
   then explicit board ordering determine admission, not another job-dispatch pass.
3. Promotion posts exactly one existing physical order. Its graph alone discovers shortages,
   requests automatic supply, claims workers/tools/materials, travels and progresses over game time.
   Promotion is admission to that workflow, NOT a claim that resources are present or construction
   is completed. Project-prerequisite gating remains the future construction-plan slice.
4. Pause immediately stops the actual assigned motion before its movement phase and retains native worker, receipts, spent
   inputs and elapsed work. Open jobs cannot acquire workers while paused. Unacquired automatic
   supply intent withdraws; already acquired cargo/receipts keep their real physical delivery.
   Unpause uses ordinary
   current admission. No refunds, teleports or duplicate execution.
5. CompleteUpdate captures the actual terminal order result before normal pruning. History retains
   intent, execution identity and terminal refusal, never copied physical progress. Cancel delegates to native work
   cancellation. Repeat submits new intent with a fresh stable identity and fresh admission.

Pending/history priority belongs to the intent. While active, priority exists ONLY on the native
order, including direct SetOrderPriority edits; the intent codec omits it. Movement affects only
pending intent ordering. Active physical job age/ranking stays with the existing dispatcher.

## Commands, views and durability

Submit/configure/move/cancel/repeat commands and WorkView previews call identical IPlanBoard checks.
WorkView returns detached owner-private ActivityPlanView facts. Settings and IDs are immutable to
hosts. Blockers are current derived facts, not another saved decision cache. A removed or changed
pending target waits with its actual refusal. Active target loss follows native cancellation.

One work codec stores intents after native orders/jobs. Validate positive unique wide intent IDs,
counter bounds, company, timestamps, authored references, settings, state, duplicate live intent and
exclusive exact native execution links on saves/hashes and restores. No old-save reader. Build/save/
work/view identities change together; no extra service codec from the capability alias.

## Focused verification gate

Exactly two names, no broad/family run:

- `Persistent plans: contracts, private intent and queue controls`
- `Persistent plans: physical execution and saved continuation`

Cover admission/preview purity and refusal parity, detached/private views, duplicate rejection,
queue move/priority/group edits, pause/unpause, independent file-authored DAG, cancellation/repeat,
real paid inputs/tool holds/progress, saved pending/active/paused/ended intent, interleaved twins and
session isolation. Every physical wait is bounded with native diagnostic context.

## Status

Focused-verified: **211 / two exact suites** (83 policy; 128 physical), zero failures/aborts and
zero build warnings/errors. Build2.69.0/save41/work19/work-view24/logistics4/logistics-view9.
[Evidence](../../docs/migration/P9_ACTIVITY_PLANS_VERIFICATION.md). Only these two names ran.
Pending command saves, actual priority/reordering, direct native blockers/cancellation and paused
unacquired/acquired supply are covered. Other suites compiled only. P9, full framework acceptance,
construction/templates/bulk controls/automatic repeat/history lifecycle/provider/replay/stress gates
remain partial. The lifecycle skill guided this staged implementation and focused gate.
