# P4-AG1: Activity graph disclosure — Design Document

## 0. Problem Statement

`WorkBoard.WalkDisclosure` in `Standard/WorkPlanCatalog.cs` checks explicit activity disclosure aliases but not the actual graph's executor, role, requirement, node-effect or final-effect references. Generic `ActivityDef` supplies only itself. A visible declaration can therefore disclose hidden graph metadata through normal previews or work diagnostics.

## 1. Goals

1. Traverse the original compiled graph using existing contracts in the original catalog owner.
2. Include every grouped requirement alternative, regardless of the currently selected branch.
3. Keep private previews, commands, progress and completion history consistent without changing earned physical execution.
4. Verify authored nested requirements and node/final effects through normal public commands, discovery, forgetting, game updates, saves and replay.

## 2. Design

Extend `WorkBoard.WalkDisclosure`; do not create another registry or scheduler. Validate actual immutable aliases as before. Explicit duplicate aliases remain invalid. Repeated graph references are legitimate and are visited once. Read requirement group entries under the existing provider observation guard. Resolve effect and role IDs through the selected content catalog. Continue recursively traversing activity aliases.

## 3. API surface

No new public interface. Existing `IActivityGraph`, `IActivityNodeDefinition`, `IWorkRequirementGroup`, `IActivityExecutor`, `IWorkerRoleDefinition` and `IWorkEffect` are authoritative. Engine build identity changes; saved state layout does not.

## 4. Files to change

Original catalog, engine build identity and protocol fixture; independent visibility content declarations; authored proof JSON and registration; one thin public-player test; stage evidence and master tracker.

## 5. Error handling matrix

Invalid aliases fail composition. Hidden graph content withholds the whole activity through existing typed refusals, never partial metadata. Existing physical work continues after forgetting. No requirement evaluation, material reservation or effect application occurs during disclosure traversal.

## 6. Backward compatibility

Development only; no compatibility shim or legacy path.

## 7. Verification

Build after the completed change, then run only the new exact bounded case. Record any failures and same-case repair attempts. The user-authorized single final full regression remains deferred until implementation is complete.

## 8. Risks

Shared graph references must not be mistaken for duplicate explicit disclosure entries. Group traversal must terminate and must not bypass provider observation guards.

## 9. Out of scope

Provider-private internal content references are not inferred; typed requirement/effect dependency contracts remain a separate review. No Tycoon business rules, Oil domain, platforms, draw loop, clock or scheduler changes. This slice does not close broad original gates or the active goal.

## 10. Sub-task execution order

1. Extend original catalog graph traversal.
2. Author independent typed visibility rules and JSON workflow.
3. Verify the exact new public-player case and retain diagnostics.
4. Update evidence and master checkpoint without changing unproven gate checkboxes.

## Stage outcome

Scoped runtime proof delivered: FINAL246passed/0failed/0aborted; original221/193checked/28open unchanged, additional253. No aggregate gate or goal closure. [Evidence](../../docs/migration/P4_ACTIVITY_GRAPH_DISCLOSURE_VERIFICATION.md). Initial metadata validation:1899source anchors/1081selected inputs/13quickstart anchors,0hash mismatch/drift/broken links; all12Tycoon sources and sole clock unchanged. One final full regression remains authorized but deferred until implementation is complete.
