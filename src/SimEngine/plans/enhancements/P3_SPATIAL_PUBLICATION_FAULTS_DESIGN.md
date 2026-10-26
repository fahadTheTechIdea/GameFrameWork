# P3-S37 — Terminal spatial publication failures

## Current M67: intrinsic contract-owned publication — 2026-10-22

Native **2.722.0/save161/replay2**. [Complete current evidence/caller matrix/attempts](../../docs/migration/P3_SPATIAL_PUBLICATION_FAULTS_VERIFICATION.md#current-m67-spatial-publication-ownership-and-material-semantics--2026-10-22).
Old2.721 notification hash actually escaped2/2. The original SpatialMutation is now one per-world
intrinsic ISpatialMutation/ISimulationOperationBoundary service, bound under engine ownership before
module composition. Same Kernel nesting primitive, existing World provider-index/checkpoint/clock
admission and first fault. Native/independent tile/point writers call its contract; static Commit
is removed with no shim. No copied stock/book, phase registry, clock, codec or provider inheritance.
Pure Check/Facts helpers remain with the same owner; no gameplay policy moved into a host/proof.

Compiled native census132calls (111resource+21spatial),14inventory/book and8selected generic-layer
field stores,450matching source/PDB checksums,0unknown/unmapped/indirect. Quantity/sample/corridor/
terrain/startup callers have explicit dispositions; this is not every semantic mutation or balance
equation. The shared LineDefinitionContracts gate now rejects quantity topology and owner channels;
Associated resource metadata remains valid. [Same line owner](P4_LINE_DEFINITION_CONTRACTS_DESIGN.md).

New324-context admission4086/0; existing notification174/0, physical extraction86/0, public spill856/0,
line semantic admission16/0 and native/direct physical line222/0: latest5440/0/four public replays
across captured build4/build6 images, not final-image/full regression. Spatial owners unchanged
after their selected pass; only line guard/tests changed later. No original gate closure, broad test,
consumer/business/platform/Draw work or publication. Next typed reference-owner and remaining actual
classified accounting/stage stress. Original221/210source-recorded checked/11open/full goal ACTIVE.

## Historical S37 design and then-current exclusions

## 0. Problem

Whole-set sample admission is atomic, but a hostile resource-cell notification can throw after values are published. Outside Game.Update, native and independent mutation methods could propagate the error without latching World.Fault, allowing a partial transaction to be treated as healthy.

## 1. Goals

1. Preserve the original exception and latch the sole world fault if publication fails.
2. Refuse later mutation, healthy hashes/saves and updates while leaving preflight refusals nonfatal.
3. Prove direct native/independent resource notification failures, private query refusal and isolated sessions through focused checks.

## 2. Design

SpatialMutation.Commit is an engine publication boundary, not a fault or rollback owner. It requires a healthy session, invokes the selected owner's publication and delegates failure to the existing World.LatchFault. Native and independent batch writers use it only after successful whole-set preflight, preserving existing no-prefix healthy refusals.

Negative protocol tests replace the actual bound resource guard with a forwarding notification probe, without another stock/book or gameplay implementation. After the actual debit, the probe throws one known exception. The engine must retain that exception/fault, prohibit healthy continuation/save/query publication, and leave an interleaved session untouched.

## 3. API/state

One stateless module-facing publication helper over existing World ownership. Build2.587.0/save157/replay1; payloads unchanged.

## 4. Files

Kernel/SpatialMutation.cs; Map/TileMap.cs; independent provider; focused publication fault tests, registrations and engine/protocol identity.

## 5. Errors

Argument/preflight refusal before publication remains healthy and unchanged. Failure after publication begins latches spatial.mutation without replacing the original exception or first fault. No automatic rollback, retry or fabricated healthy save.

## 6. Compatibility

Direct writer publication calls are replaced in place; no alternate fault state or legacy fallback.

## 7. Verification

Two targeted builds clean. Four exact cases pass269 checks: direct native/independent notification fault32, atomic refusals56, independent physical extraction/survey89 and native visibility-enabled extraction/survey92. [All attempts](../../docs/migration/P3_SPATIAL_PUBLICATION_FAULTS_VERIFICATION.md). No full/profile/family run.

## 8. Risks

Arbitrary clock mutation without throwing, hostile pure guards, global allocation failure and all mutation API/lifecycle/utility/fault recovery matrices remain open.

## 9. Exclusions

Rollback promises, second fault/store owner, Oil/Tycoon/platform/host rules, full regression and Git publication.

## 10. Status

Publication exception boundary and stated focused proof complete; nonthrowing clock mutation, hostile pure guards, other mutation APIs and physical stocked recovery remain open. Full goal active; no original broad checkbox closes.
