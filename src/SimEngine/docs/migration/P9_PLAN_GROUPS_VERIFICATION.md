# P9-A3a — Whole-group plan control evidence

Date:2026-10-05. Current working-source evidence, not committed HEAD/full-regression proof.
Standalone framework only. No Oil/Tycoon/host/platform/renderer gameplay implementation.

## Identity and single owners

Build2.85.0/save55/work-view28. Work22, transformer6, manager2, transformation-view7,
logistics5/logistics-view10/resource-flow5 and all other native layouts unchanged.
The command table/API/private view changes break current engine identity directly; no old reader/shim.

WorkBoard remains the sole IPlanBoard. WorkPlanGroups.cs is a partial file, not another owner.
Group membership derives existing company-scoped PlanEntry.Group fields; no group record/index/codec,
copied priority/pause state or additional scheduler. Existing Order alone owns active priority;
node/job/stock/receipts/tools/cargo/paid progress remain native. ProjectPlan is the shared private
projection for all plans and group queries. Single/group submission share PlanIntent's command serializer.

PlanGroupIntent captures ordered immutable explicit entries; each must name its same explicit group.
Whole preflight combines existing single admission with joint provider-defined identity duplicates,
fresh scoped group labels and checked long allocation range before creating any intent/order/claim/site.
Applied operations re-admit actual state; a violated committed assumption faults, never fake rollback.
Configure/cancel operates on live current members, preserving terminal history. Move requires all live
members pending and preserves their relative order and existing IDs. Repeat requires all members terminal
and an explicit fresh destination group; no unfinished execution/old progress/history is cloned.
Submission is whole intent admission, not simultaneous placement or synchronized physical promotion.

## Exact final gate

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Plan groups: whole admission, ordering and private contracts"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Plan groups: physical pause, cancellation, repeat and saved commands"
```

Final build: zero warnings/errors,2.94s.
Contracts:52 passed, zero failed,217ms.
Physical:50 passed, zero failed,321ms.
Total:102 checks/exactly two suites, zero failures/aborts. Repair repeats used only these names.
Other suites migrate/compile only; no third/family/full run. Physical waits are bounded800 updates
with tick, plan/group/status/blocker diagnostic snapshots. Earlier stage gates were not rerun.

## Proven

Actual service alias and unchanged plan codec; explicit immutable group payloads; late invalid entry
and public mod site refusal with no accepted prefix/allocated ID; joint duplicates; current live
duplicates and fresh-label/identity range refusals; private queries/preview-command parity; stable whole
pending-group moves; no arbitrary priority/group count cap; individual regroup selecting current native
records; unchanged terminal history during live control; live repeat/mixed-state repeat refusal;
fresh repeated IDs and real workflows; pure/detached group facts; pending command/submission/pause/cancel
save and isolated twin continuation; physical native DAG completion; actual worker claims/time frozen
while group-paused; real input consumption and tool holds; cancellation releases only actual owned
machine/tools without refunding spent inputs; automatic physical supply and retained cargo delivery after
requester group pause/cancellation; no ghost pending execution or duplicate cargo/materials.

Public proof reuses PlanProofModule and master-listed plan-groups.json for authored activities and
custom PlannedInspectionDef admission. No proof-owned group scheduler, gameplay timer, stock or ledger.

## Remaining gates

Templates with relative selected-provider geometry/transforms/cross-links, automatic repeat, new plan
provider kinds and policies for simultaneous group activation are not implemented by this slice.
Broader hostile-provider purity/re-admission/reentrancy/fault matrices, full replay/lockstep/shuffled
arrival/fuzz/soak/allocation/stress and all P9/P1–P12 acceptance remain open. Group labels are current
company-scoped membership, not durable container entities or an implicit group-generation model.
Former history/individual regroup can change what a later group operation selects; views report it.
Full roadmap goal remains active/partial; focused tests never establish full regression success.
