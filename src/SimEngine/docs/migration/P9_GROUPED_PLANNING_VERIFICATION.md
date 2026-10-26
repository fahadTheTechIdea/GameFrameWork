# P9-A15 — Grouped planning controls and template admission: verification

Verified 2026-10-10. Core `beep-simengine-2.406.0`, save154/replay1/work-view96. Existing plan/work/site/inventory/clock persistent codecs unchanged.

## Engine implementation

Original WorkBoard remains the only plan/group/template execution owner. ReadTemplateProvider now requires healthy state and rejects transient recursive observation. Callback success verifies original plan/template/order/job counters, entity identity/clock facts and actual selected tile-map version. Expected ContentException/OverflowException authoring refusals verify healthy observation before the existing per-entry evaluator converts them to typed blockers. Unexpected exceptions latch/rethrow the original failure; finally clears only transient admission. A swallowed inner fault cannot publish a healthy result.

Template entry points and original stored-template selection/lookup/edit/copy paths refuse faulted queries before early invalid/missing returns. Original authored lookup also refuses. ExpandPlanTemplate now publishes its immutable out intent only after complete existing PreviewPlanGroup admission; any rejected group returns null, never partial executable intent. WorkView exposes existing engine-owned structural checking and expansion through delegates, not host translation or new admission logic. No duplicate compiler/catalog/group store/progress/clock/state-machine owner.

The existing independent construction registration delegates sole ConstructionDeclaration and binds the same reusable runtime. Only explicitly armed negative template callbacks are added to the proof module; ordinary gameplay remains in the engine. The grouped-planning.json template is explicitly master-listed through existing WorkModule authoring. Original phase requirement P9.acceptance.013 is now checked; its text is unchanged. This does not close any aggregate phase/full-regression gate.

## Exact runtime

Only ONE NEW exact case was run (four attempts of that same case):

```text
Grouped planning: truthful entry admission and saved physical pause reorder repeat cancel
```

| Attempt | Passed | Failed | Aborted | Suite ms | Child PID | Exit | Observed ms |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | 177 | 3 | 1 | 1402 | 35012 | 1 | 1563 |
| 2 | 340 | 0 | 0 | 1476 | 14484 | 0 | 1620 |
| 3 | 415 | 0 | 0 | 1414 | 8680 | 0 | 1556 |
| FINAL 4 | 447 | 0 | 0 | 1856 | 3916 | 0 | 2007 |

All children authoritatively Exited, untruncated stdout/stderr, empty observer errors. Limits outer30000ms, owned termination5000ms, output30000 characters; each physical stage bounded256 Updates with actual plan/stock/cargo diagnostics.

First attempt completed both positive native/direct workflows, then showed the negative anchor probe called ordinary expansion, which does not call structural anchor validation. It now invokes the actual engine structural query; failed fault assertions no longer dereference a missing fault. Second pass established all five callback paths. Third adds faulted selection/stored/authored early-return coverage. FINAL adds active-plus-pending cancellation from a saved command inbox, not merely pending cancellation. These are strengthened checks of the same case, not a family/full run.

## Build evidence

`dotnet build Beep.SimEngine.slnx --no-restore`:

| Build | Exit | Elapsed seconds | Warnings | Errors |
|---|---:|---:|---:|---:|
| 1 | 0 | 6.99 | 0 | 0 |
| 2 | 0 | 6.07 | 0 | 0 |
| 3 | 0 | 5.81 | 0 | 0 |
| FINAL 4 | 0 | 4.28 | 0 | 0 |

Historical grouped/template/construction cases compile only, not executed. No old/family/full/Oil/Tycoon/platform runtime.

## Requirement-to-evidence map

| Original grouped-planning requirement | Current authoritative evidence |
|---|---|
| Invalid/valid templates and per-entry results | Late unknown entry, late native out-of-bounds entry and duplicate geometry preserve each member's distinct blocker; rejected command has no prefix/spend/ID allocation and failed expansion nulls intent. Valid expansion is pure and uses engine-derived relative coordinates. |
| Pause | Actual first construction has physically supplied, consumed material and productive work; group pause preserves node progress, dependent pending intent and changed priorities. Paused save/load twins and the sole clock preserve identical state. |
| Reorder | Pending whole group moves behind another group without changing member order, stable IDs or dependency links. Mixed active/pending group reports NotPending only for the active member. |
| Repeat | Cancelled group yields fresh IDs/internal links and only the eligible predecessor starts physical work. Live repeat reports NotEnded per actual member. Completed group repeat records pending intent at occupied real sites without copied completion, resources or structures. |
| Cancel | Pending original/repeated groups cancel without ghost projects or refunds. Separate actual partially built predecessor plus pending dependent cancel together through queued saved command; consumed8 is not refunded, neither structure completes and cancelled work does not resurrect. |
| Truthful engine state and progress | Actual linked completion times, work node facts, private group admission, detached retained views, active relative capture, terminal blockers and both real entity states use the original owner. |

Each positive workflow runs through native and independent contract construction definitions with identical completion coordinates. Two linked structures physically consume8+8 from source20, leave4 and no cargo/site residue. Separate partial cancellation consumes8 and retains12 without invented salvage/refund. No business module is selected. Original ordinary player replay, pause/partial/queued/terminal saves and interleaved session isolation pass.

Twenty-five negative callback probes cover validation, structural anchor, capture, placement and activation, each with original throw, clock pause, identity mutation, recursive query and swallowed inner fault. Original first error survives; save/hash and even invalid template/selection/stored/authored queries refuse after fault. A healthy expected authoring exception stays typed per-entry and changes no state.

## Source freeze

FINAL executable inventory recorded2026-10-09 22:45:54 UTC:470 selected inputs,13 changed/new compared with prior466. No executable edits after FINAL green. Original gate text changes0; checkbox changes1 (P9.acceptance.013 only). Remaining original requirements71; aggregate completion is not inferred.

## Remaining scope

This proves the named grouped-planning control requirement using original provider-neutral paths and point geometry/native and independent construction. It is not aggregate P9/P1-P12 acceptance, exhaustive all-provider purity, tile-map mutation runtime proof, stored-library edit/retention regression, concurrent templates, all areas/logistics/recovery/conditions/faults, performance/fuzz/soak/lockstep or full regression. Broader original gates remain open. No renderer/platform, Tycoon/protected G19, secondary scheduler/clock, staging, commit or push changes. Full goal/plan completion is still required before publication.
