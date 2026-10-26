# P4-G51: Contract-owned corridor completion effects — Design Document

## 0. Problem Statement

`Standard/LineDefinitions.cs` still resolves `LineEffectDef` for final and terminal effect admission and only recognizes that native class when refusing early cursor changes. Independently authored effects cannot participate through contracts despite the existing physical work-effect protocol.

## 1. Goals

1. Admit native and direct independently authored segment/final effects through one focused immutable contract.
2. Use one strict JSON operation declaration and the original physical completion algorithm.
3. Validate all loaded metadata, unused included, before Initialize; refuse wrong node/final positions and early cursor mutation.
4. Preserve earned physical construction, stock, recovery, saved execution, replay and the sole game clock.
5. Verify one new exact bounded native/direct case; broader/full gates stay open.

## 2. Design

`ILineEffectDefinition : IPhysicalWorkEffect` adds only immutable `Operation`. `LineEffectDeclaration.Read` owns strict segment/finish JSON. `LineEffectContracts` owns actual catalog alias/operation admission and delegates subject checks/commits to original `LineService` methods. Native `LineEffectDef` is a normal registration, not a required extension base. Direct definitions delegate these same helpers and `PhysicalWorkEffectContracts`; no cursor, scheduler, stock, recovery or clock is copied.

`ResolvedContent` validates every loaded effect before freeze. `WorkEffectContracts.ValidateDefinition` revalidates frozen effects inside existing provider observations before Initialize and during completion. Line metadata resolves effect contracts and rejects any early line-effect contract, regardless of its concrete class. Plan admission requires segment effects at nodes and finish effects at whole-order completion, alongside positive machine-work ancestry. Selected reads retain original actual entity/provider codec and clock guards.

## 3. API surface

Master-listed module documents declare operations `segment` and `finish`. Extensions implement `ILineEffectDefinition` directly on `Def`; no native inheritance or parallel effect registry. Normal public commands, previews, immutable views, Update, clock control, saves and replay remain the host boundary. No Draw or platform API.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| New | src/Beep.SimEngine/Standard/LineEffects.cs | medium |
| Modify | src/Beep.SimEngine/Standard/WorkEffects.cs, LineDefinitions.cs | medium |
| Modify | src/Beep.SimEngine/Content/ResolvedContent.cs, Kernel/SaveGame.cs | medium |
| New | tests/ExtensionProof/LineEffectContractsProof.cs | low |
| New | tests/ExtensionProof/content/line-effect-contracts.json | low |
| New | tests/Beep.SimEngine.Tests/LineEffectContractTests.cs | medium |
| Modify | test registration, proof resources, protocol fixture and historical concrete lookups | low |

## 5. Error handling matrix

| Admission/read | Result |
|---|---|
| Missing/empty/unknown operation or extra field | Typed content refusal; no default or fallback |
| Invalid operation or mismatched actual aliases, unused included | Refusal before Initialize |
| Segment as final, finish as node or early cursor effect | Existing plan/line admission refuses |
| Throw/clock/entity/cursor mutation during selected preview | Existing scoped fault, checkpoint/hash refusal |
| Earned completion | Original physical work and resource recovery owners commit |

## 6. Backward compatibility

Development only; no legacy reader, compatibility base or duplicate model. Build2.394.0/save154/replay1, with unchanged persistent layouts. Exact build/content identity remains mandatory.

## 7. Verification

ONE clean completed-slice build15.47s, zero warnings/errors. ONE attempt of ONE NEW exact bounded case FIRST/FINAL136/0/0 in1715ms; PID28340 Exited0, observed1870ms, no timeout/truncation/observer error. [Actual evidence](../../docs/migration/P4_LINE_EFFECT_CONTRACTS_VERIFICATION.md). No historical/family/full/Oil/Tycoon/platform runtime.

## 8. Risks

Scoped codec/clock observations are not a CLR sandbox or global rollback guarantee. Stocked cursor evacuation, owned/foreign geometry, retries, all providers and topology/performance matrices require their own evidence. Do not generalize this case into complete industrial baseline proof.

## 9. Out of scope

Tycoon authority G19 remains unanswered. Protected clock/business files, Oil, hosts, Draw/platforms untouched. Full P1–P12, provider breadth, allocation, lockstep, fuzz/soak and final acceptance remain open.

## 10. Sub-task execution order

1. Introduce focused immutable contract and sole declaration/admission/dispatch helper.
2. Replace concrete line consumers and validate every loaded effect in existing lifecycle.
3. Master-list direct effect content and add one thin public-command/native-direct physical case.
4. Build completed slice and run only that new exact bounded case; preserve any repair evidence.
5. Hash actual post-green inputs and reconcile tracker/audit without altering original compound gates.

## 11. Delivery status

Scoped verified, not whole-plan completion. Native/direct effects earn the same physical three-segment chronology through original machine travel, parallel stages, tools/materials and graph completion. Stock40→38→34, then32 after another earned prefix and ordinary cancellation; no future tile is earned. Original cursor retirement, private readiness, immutable progress, paused clock, partial/completed/cancelled saves, continued twins, replay and isolated sessions succeed. Eight unused malformed effects, six positional/reference refusals and four live selected observation faults are proven.

Selected386 executable inputs hashed after final green at2026-10-09 19:32:02 UTC; no executable changes afterward. Original221 compound gates retain their exact text/checkboxes. Full goal active; no staging, commit or push until the entire original goal and plan are finished and verified.
