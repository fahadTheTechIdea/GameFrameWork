# P7-D8: Physical topology commit observation

## 0. Problem statement

`NetworkService.ApplyTopologyCore` checks redistributed stock before calling physical geometry erasure, then verifies only geometry afterwards. A callback can erase correctly but mutate the sole clock/random stream or reintroduce stock/claims into a retired endpoint and still publish a healthy graph.

## 1. Goals

1. Reproduce this through the original public topology contract and actual point geometry callback, with both authored time quanta.
2. Observe clock/random/identity facts across the mutating geometry callback without forbidding its intended geometry writes.
3. Recheck original stock owners after callback completion using one shared resource invariant validator.
4. Preserve original callback exceptions and the existing first-fault owner; refuse healthy continuation/save/hash after interruption.
5. Retain normal physical construction/removal/reconnection, save/replay and capacity refusal behavior.

## 2. Design

Use the existing `PointSpace.GeometryObservationFault` test hook at the end of actual `Erase`. Direct protocol tests inject pause, speed, random advancement/restoration, original throw, retired stock and retired receipt changes. Preparation remains ordinary player commands and physical engine updates. Tests do not implement removal, redistribution, scheduling or rollback.

At the existing commit owner, extract the current resource checks into `VerifyTopologyResources`. Call it before and after successful geometry mutation. Capture `EntityObservation.Capture(world)` immediately before invoking geometry, then verify original scalar facts and engine health. Do not freeze the intended map, copy stock, add a second fault owner, or attempt rollback. Existing geometry parity validation still runs before publication. A thrown callback retains its original exception.

## 3. API surface

No public contract or state layout change. Production behavior change bumps engine build to 2.620.0; save159/replay1 remain unchanged.

## 4. Files to change

- `Standard/Networks.cs`: original commit observation and shared invariant validation.
- `Kernel/SaveGame.cs` and current protocol JSON: build identity only.
- Existing point geometry proof and topology erase tests: callback instrumentation, literal protocol proof, stale save assertion and bounded physical waits.
- Existing stage tracker/checkpoints and verification report.

## 5. Error handling matrix

Correct erase with scalar mutation: commit faults. Correct erase with retired stock/receipts: commit faults. Callback throws after mutation: exact original exception survives. Incorrect erase: existing geometry parity refusal survives. Ordinary capacity refusal: no geometry commit. Already committed resource facts are not falsely rolled back.

## 6. Backward compatibility

Development only; no compatibility path or duplicate subsystem.

## 7. Verification

Build the affected test project. Run only three exact literals: new commit observation, existing erase postconditions, existing capacity admission. Each process has a 60-second watchdog; physical wait stages also have a 20-second elapsed bound and update limit. Record failed reproduction and all final results. No full/profile/family runtime.

## 8. Risks

This is scoped observation of original commit participants, not arbitrary CLR mutation sandboxing. Direct fault injection is protocol evidence, not public player acceptance. Intended geometry writes must remain allowed. Original pending movement facts may exist before erase failure; do not claim transactional rollback.

## 9. Out of scope

Tycoon, oil gameplay, hosts, demos, platforms, Draw, arbitrary provider state, broader topology stress and full P7.017 completion.

## 10. Execution order

1. Add actual callback reproduction and bounded diagnostics; build/run the new literal before production changes.
2. Repair the sole original topology commit boundary; update build identity.
3. Build/run the three focused literals; inspect the final diff and record evidence.
4. Update master tracker without closing broad requirements or publishing Git changes.
