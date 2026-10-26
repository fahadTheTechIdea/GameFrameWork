# P3-S36 — Whole-set tile layer mutations

## 0. Problem

Owned corridor stamping and removal separately write ownership and topology layers. A later preflight failure (illegal sample, material guard or exhausted revision) can leave a changed prefix. A contract-based engine must delegate whole-set storage admission/publication to its selected provider.

## 1. Goals

1. Preflight every layer/cell, material guard and aggregate revision before publishing any requested sample.
2. Keep original sparse stores, resource-cell books, visibility cursors, line/network/work and clock ownership.
3. Prove native/independent no-prefix refusal and normal physical corridor/topology continuation through focused tests.

## 2. Design

ITileSampleMutationSpace accepts immutable TileSampleMutation intents. TileLineGeometry requires that actual selected capability and sends ownership/topology stamping and complete removal as one set. No compatibility path invokes sequential layer writes.

Native MapLayer stages a shallow sparse chunk index and clones touched chunks only. It checks all sample values, duplicates and original resource-cell guards, then prepares existing TileMap.ValueWrite facts. The selected map validates all staged layers/cursors and aggregate public/private/global counter deltas before publishing data and original cursors; endpoint notifications follow successful whole-set publication. Single Set/Fill behavior stays with original owners.

The independent proof stages candidates over its original dictionaries and checks owner/revision/resource guards and aggregate observation revision before whole-set publication. Candidates are short-lived transactional data, never saved parallel layers or a second ledger.

## 3. API/state

One selected spatial mutation capability and immutable cell intent. Build2.586.0/save157/replay1; sparse payloads unchanged. A multi-cell batch advances each changed layer once rather than once per cell; engine identity changes accordingly.

## 4. Files

Map/TileSpatialContracts.cs, TileMap.cs, TileLineGeometry.cs; existing independent provider; focused atomic mutation tests and engine/protocol identity/registration.

## 5. Errors

Wrong/missing layer, duplicate cell, illegal value/location, protected material, stale staging and aggregate counter exhaustion refuse before any requested sample prefix. Post-publication hostile notification exceptions remain terminal failures rather than a promise of arbitrary callback rollback.

## 6. Compatibility

Whole-set contract replaces line writes directly; no sequential fallback, legacy model or additional mutation loop.

## 7. Verification

Two targeted compiles clean. Seven exact cases pass818 checks: atomic protocol56, physical native/independent corridor319 each and four affected stocked topology/ownership cases124 combined. [Evidence](../../docs/migration/P3_ATOMIC_TILE_MUTATIONS_VERIFICATION.md). No full/profile/family run.

## 8. Risks

Arbitrary hostile guard/notification callbacks, allocation failure, all multi-layer visibility topologies and global lifecycle/recovery/lockstep/fuzz/soak budgets remain open.

## 9. Exclusions

Oil/Tycoon/platform/host rules, dense copies/size policy, copied resource/network/gameplay coordinators, full regression and Git publication.

## 10. Status

Whole-set mutation migration and stated refusal/physical/topology verification complete. Broader hostile callbacks, physical stocked recovery/lifecycle/utility and global gates remain open. Full goal active; no original broad checkbox closes.
