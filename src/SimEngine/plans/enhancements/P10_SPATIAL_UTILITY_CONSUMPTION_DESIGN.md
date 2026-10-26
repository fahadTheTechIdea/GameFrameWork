# P10-A18: Selected-space utility consumption — Design Document

## 0. Problem Statement

`UtilityContracts.Read` in `src/Beep.SimEngine/Standard/UtilityConsumers.cs` guards utility provider and entity codecs but observes spatial state only through a TileMap cast. Point/custom layer mutations by utility callbacks can escape that boundary. P10.acceptance.003 and P11.acceptance.026 still require non-tile utility consumption and blockers.

## 1. Goals

1. Observe the actual selected ISpatialSpace codec around the complete existing utility demand/binding read, including arbitrary-provider callbacks.
2. Prove owned point corridors physically supply a timed native processor via original utility demand policy, resource ports and NetworkService.
3. Prove isolation/shutdown/shortage/reconnection preserve consumed stock, earned work, saves, immutable private facts and replay through the sole clock.
4. Refuse custom layer mutations from query/update callbacks, preserving the first fault and publishing no healthy prefix.

## 2. Design

Reuse EntityObservation.ReadProvider for the selected space and original provider/entity scopes. A deliberately nonspatial session has no space scope. Delete the TileMap-only utility scope, not introduce another observer or utility execution loop.

JSON authors an ordinary point plant, recipe, inventory port, network demand and utility-dependent activity. Existing point writer, network, processor, work and clock retain all state and execution. Existing utility negative instrumentation gains explicit spatial-layer modes; those are engine-side probes, never driver mutations.

## 3. API surface

Existing public UtilityContracts.Read and all views/previews retain their signatures. No platform API, new utility store, clock or scheduler. Pure authoring factories forward optional module/declaration inputs already supported by PointGame.Factory.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | src/Beep.SimEngine/Standard/UtilityConsumers.cs | 12 | medium |
| Modify | tests/ExtensionProof/UtilityOwnerObservationProof.cs | 12 | low |
| Modify | tests/Beep.SimEngine.Tests/SpatialCorridorExecutionTests.cs | 5 | low |
| Modify | tests/Beep.SimEngine.Tests/SpatialNetworkGraphTests.cs | 8 | low |
| New | tests/ExtensionProof/content/point-network-utilities.json | 50 | low |
| New | tests/Beep.SimEngine.Tests/SpatialUtilityConsumptionTests.cs | 190 | medium |
| Modify | tests/ExtensionProof/ExtensionProof.csproj, tests/Beep.SimEngine.Tests/Program.cs | 2 | low |
| Modify | src/Beep.SimEngine/Kernel/SaveGame.cs, tests/Beep.SimEngine.Tests/testdata/current-engine-protocol.json | 2 | low |

## 5. Error handling matrix

| Operation | Required outcome |
|---|---|
| Missing physical connection/supply | Existing demand/work typed blocker, no fabricated consumption |
| Disabled operation | Existing inactive utility and paused work |
| Custom layer mutation during read | Original first-fault owner rejects facts and later updates/saves |
| Callback mutation plus exception | Callback first error remains authoritative |
| Private query | Existing company withholding and unchanged state |

## 6. Backward compatibility

Dev only; no compatibility path. Replace the tile-only utility boundary in place. Existing persisted state owners/schemas remain unchanged.

## 7. Verification

One NEW exact bounded case: FINAL168/0/0 in11161ms, with three failed attempts preserved. Minute/half physical supply/interruption and eight negative selected-space reads pass. One solution compile11.60s and four necessary focused same-case compiles10.13s/10.73s/10.38s/14.78s, all clean. No old/family/full suites, solution rerun or executable edits after FINAL; broad gates remain open. Original cumulative journal owns committed consumption/production; current utility facts reset at completion. Half-minute zero-input exposure is valid; shortage proof awaits the actual depleted receiving pool. Waiting service is cancelled by a normal player before reconstruction, never preempted by fixture mutation.

## 8. Risks

Actual operating-input phase timing and live pool stock affect when shortages appear: derive readiness from engine views, never expected host formulas or forced writes. Geometry/provider/fog/private CLR/performance breadth is not proved by this fixture.

## 9. Out of scope

Oil, platforms, Draw, adapters, protected Tycoon/G19, all-provider/fog/claims/pressure/stress/full gates and publication. No aggregate original gate closure from selected proof.

## 10. Sub-task execution order

Read existing owners; record stage; replace utility spatial observation; author point utility content and thin public-player test; compile and run exact case; retain attempts and source identities; refresh tracker/audit with qualified evidence.

Metadata closure: 1,625 source anchors, 875 selected executable inputs and 13 quickstart anchors match frozen SHA-256 values. All 221 original gate literals/checkboxes are unchanged (184 checked, 37 open); 191 additional scoped deliverables do not establish aggregate completion. All 8,618 local links resolve; 18 master rows, the new driver boundary scan and git diff --check are clean. Two production sources changed (utility observation/build identity), no production source was added. The sole clock and protected Tycoon sources remain unchanged. No executable change after FINAL and nothing staged.
