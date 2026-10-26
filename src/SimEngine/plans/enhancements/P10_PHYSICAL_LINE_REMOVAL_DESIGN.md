# P10-A6: Physical line removal — Design Document

## 0. Problem Statement

`LineService.CheckRemoval/Remove` in Standard/Networks.cs preflight actual shared-layer pools but are engine primitives, not earned activity effects. Players cannot author physical removal using the existing work graph.

## 1. Goals

1. Extend the sole line-effect declaration with explicit `remove` and an actual line contract reference.
2. Remove only the actual fixed target footprint, after positive-time machine ancestry completes.
3. Delegate complete topology disposition to INetworkTopology; refuse foreign tiles, active claims and stocked last pools without loss or invented salvage.
4. Preserve ordinary player admission, work travel, cancellation, save/load and immutable previews.
5. Native and independent JSON implementations delegate the same algorithm without mandatory inheritance or another scheduler.

## 2. Design

ILineEffectDefinition adds Line (null for segment/finish). LineEffectDeclaration resolves explicit line only for remove; all other fields remain strict. LineEffectContracts validates actual catalog aliases and operation/reference consistency, uses IWorkEffectIntentAdmission for the actual fixed ITilePresence target, and enumerates its footprint in stable row/column order. No relative remote tiles or supplied live handles. CheckSubject requires earned node/whole-order progress; CommitSubject freshly delegates to INetworkTopology.Remove. Original PhysicalWorkEffectContracts, WorkBoard, LineService, resource transactions and game clock retain all authority. Target entity remains after removal; topology does not imply structure demolition or salvage.

## 3. API surface

Existing line_effects JSON: `{ "id":"field.cut", "operation":"remove", "line":"field.pipe" }`. Existing PostOrderCommand, activity nodes, plans and views perform submission and diagnostics. No instant command or new state codec.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | Standard/LineEffects.cs and WorkEffects.cs | 90 | medium |
| Modify | IndependentLineEffectDef proof | 12 | low |
| New | PhysicalLineRemovalTests.cs | 150 | low |
| Modify | protocol, registration and tracker | 20 | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Unknown/missing line or malformed operation | Content refusal before Initialize |
| Wrong space, moving/non-tile target | Typed intent refusal |
| Unearned machine work | work.effect-not-complete |
| Foreign/missing tiles, active pool claims or unsafe stock disposition | Original topology refusal, no mutation |
| Cancellation before completion | Existing cancellation; geometry retained |
| Provider mutation on refused check | Existing observed effect fault, no checkpoint |

## 6. Backward compatibility

Development only; no legacy reader or compatibility shim. Exact engine build advances; original save154/replay1 layouts remain.

## 7. Verification

Completed narrowly: [evidence](../../docs/migration/P10_PHYSICAL_LINE_REMOVAL_VERIFICATION.md), FINAL107/0/0 in1266ms; two clean builds and two same-case passes. Ordinary hauling and segment work physically reconnect the cut; the original incident propagation cut/reconnect acceptance remains untested.

Build the completed slice and run one NEW exact bounded case only. Include native/direct physical work, partial saved twins, cancellation, fixed target/foreign admission and stocked last-tile refusal. No historical/family/full/Oil/Tycoon/platform runtime. Broader gates stay open.

## 8. Risks

Footprint removal is deliberately local and physical; it does not author arbitrary corridor demolition cursors. All shared-layer networks must pass the existing preflight, never just the named line's primary network.

## 9. Out of scope

Weather, chemistry, generic non-tile topology, arbitrary corridor removal-plan authoring, salvage, renderer/platforms and Tycoon. Network propagation disconnection/reconnection acceptance remains separate until actually exercised.

## 10. Sub-task execution order

1. Extend original declaration/contracts/native/direct definitions.
2. Add one ordinary-player JSON proof with physical worker and topology observations.
3. Build once complete; repair and rerun only that new case if needed.
4. Record exact evidence and keep compound/full acceptance open; publication remains deferred.
