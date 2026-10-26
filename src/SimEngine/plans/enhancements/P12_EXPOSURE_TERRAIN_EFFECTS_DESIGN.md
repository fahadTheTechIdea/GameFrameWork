# P12-E2: Delayed incident exposure changes persistent terrain

## 0. Problem

P12-E1 connects earned cleanup to existing terrain state, but delayed incident propagation does not author terrain deposition. Original propagation already owns scheduled/expected episodes and once-only due delivery. Extending that owner avoids an additional contamination loop or deposition ledger.

## 1. Goals

1. Allow a propagation rule to select an optional contract-based exposure effect by authored stable ID.
2. Preflight complete target terrain change during eligibility and again at due delivery, before raising the target incident.
3. Publish a prepared single-use change in the same Update after the exact raised-episode receipt; stale/failed commit faults the session rather than retrying a partial incident.
4. Extend the existing terrain effect declaration/algorithm with explicit set/add operations; cleanup and deposition share one layer writer.
5. Prove delayed once-only addition, blocked duplicate raises, two episode cycles, physical cleanup, saves/pause/replay and native/independent storage.

## 2. Design

Optional IIncidentExposureEffectRule supplies a resolved IIncidentExposureEffect. Rules without that capability/effect intentionally have no terrain mutation. Existing TileIncidentPropagationDeclaration resolves the explicit exposure_effect field; native and independent rule metadata delegate that same declaration. Original shared rule admission validates the selected immutable effect before Initialize, unused rules included.

Original IncidentPropagationSystem includes effect preflight in its observed eligibility and obtains a transient prepared mutation before Raise. The original expected-episode/due queue and target episode remain once-only authorities. Preparation stores no persistent facts; commit verifies original layer revision/owner, target identity/owner and current game step before using the existing atomic sample batch.

Existing tile terrain metadata gains mandatory operation set/add. Add uses exact current values with overflow/sample validation; resource-quantity channels remain excluded. No new stock, terrain, work, propagation scheduler or game clock.

## 3. API/state

Exposure effect/preparation contracts; optional rule capability; explicit terrain operation. Current callers/files migrate directly, no compatibility default. Engine2.594.0/save157/replay1; no persistent layout changes.

## 4. Files

IncidentPropagationContracts/Content/System, TileIncidentPropagation declaration, existing TileTerrainWorkEffects owner; direct rule proof; protocol expectations; authored deposition add-on/master composition; existing cleanup driver extended with two exact deposition cases; tracker/evidence.

## 5. Errors

Unknown effect/wrong alias/unsupported layer/operation refuses content. Unrepresentable cell addition is a normal eligibility blocker before episode/terrain changes. Stale prepared data or mutation failure is terminal engine fault, never a successful prefix or repeat. Resource stock is never synthetic contamination mass.

## 6. Compatibility

Current authoring/API only. Absent optional exposure effect means no effect, not an invented definition or fallback. Mandatory terrain operation has no old-value shim.

## 7. Verification

Two clean targeted builds10.18s/7.44s. Final exact native/independent deposition cases393/0 each:786 distinct successful focused checks. Initial runs were also green; final review adds actual source-codec observation around preparation, followed by the same exact gates. [All attempts](../../docs/migration/P12_EXPOSURE_TERRAIN_EFFECTS_VERIFICATION.md). Bounded waits/process watchdog; no full/profile/family run.

## 8. Risks

Scientific spreading/weather/chemistry, real spill material capture/disposal, hostile arbitrary providers, private/multi-company effects and global lockstep/fuzz/soak/allocation remain broader gates.

## 9. Exclusions

Oil/Tycoon/platform/host/Draw gameplay, additional deposition state/clock, material deletion/generation, full regression and Git publication.

## 10. Status

Implemented and scoped verified. Two delivered episode cycles add terrain once after authored delay and physically clean it through original owners; representability refusal creates neither target episode nor terrain prefix. Pending/active/partial saves, pause and replay are deterministic. Scientific spreading, material spill/disposal, private/hostile/custom/global matrices remain open; whole goal active.
