# P4-G20a — Canonical transformation contract metadata

Status: scoped verified, 2026-10-09; core2.354.0/save154/replay1. ONE NEW exact case111/0/0 in819ms;
37 inputs frozen07:26:05UTC. [Evidence](../../docs/migration/P4_TRANSFORMATION_METADATA_VERIFICATION.md).
Full P1–P12 remains open. No publication.

## 1. Specification

P4-G20 enabled actual independent recipe contracts in the original processor. Its central validation
did not yet enforce every resolved requirement for arbitrary contract providers. Unused definitions
must obey the same material, tool, rate and port semantics as native authored recipes, before entity
consumers or initialization.

## 2. Single-owner design

Extend existing TransformationDeclaration.Validate, not a second validator/catalog/parser/scheduler.
Build expected startup totals through original Bill normalization. Validate rate quantities and rational
periods through original ResourceRate constructor and unit invariants; verify actual catalog identity
and uniqueness separately. Require complete unique routes with valid authored port identities.
Move native resolved tool/result/route semantic checks into this owner; Resolve remains reference
resolution and canonical construction only. Native JSON readers remain strict.

Batch has a positive duration and resource/entity result, finite input/output/tool bills and no
continuous rates. Continuous has zero duration, no entity/finite input/output/consumption bills,
and explicit nonempty output rates. Both retain held tools and operating rates. Startup equals
inputs plus tools exactly. Completion consumption cannot exceed held tools; tool rates require held
resources. Every mapped input/output/tool/operating resource has one explicit route, no unrelated
or duplicate mappings. Endpoint existence/acceptance stays original component/physical validation.

## 3. JSON and extension proof

Master-listed transformation-metadata.json authors independent healthy batch/continuous unused
definitions. Public proof provider delegates original declaration, with explicit test-only fault metadata
to report deliberate inconsistencies. No stock/progress writers, runtime system or alternate loader.
The native recipe reader and central validator are also exercised with malformed unused native recipes.

## 4. Bounded verification

Compile solution once after the completed slice; ONE NEW exact case:
`Transformation metadata: canonical requirements and owned routed rates before initialization`.
Only this new case may be repaired/rerun. Validate healthy aliases/resolution/Initialize, twelve public
Updates with saved twins, one-clock pause and completed save/load. Reject inconsistent unused startup,
tool, mode, rate and route metadata before Initialize. No historical/family/full/Oil/Tycoon/platform runtime.

## 5. Limits and remaining stages

This stage is content metadata validation, not arbitrary CLR-provider mutation/fault containment or continuous
production runtime proof. Subsequent [G20b](P4_TRANSFORMATION_TIMING_DESIGN.md) removes duplicate recipe
conversion callbacks entirely and narrowly proves independent continuous physical execution. G20a was
not rerun on2.355.0. Broader network/staffing/technology/knowledge
runtime breadth, cancellation/removal, shuffling/lockstep/concurrency/fuzz/soak/performance/global gates
remain open. Resource catalog migration remains pending explicit excluded three-file Tycoon scope.
No business behavior/defaults, Oil, adapter, Draw, renderer, platform or clock changes.
Build identity changes; save154/replay1 and persisted schemas unchanged.
Commit and push only after genuine full goal AND plan completion and verification.

[Previous stage](P4_TRANSFORMATION_CONTENT_CONTRACTS_DESIGN.md),
[parent](P4_REUSABLE_MODULES_DESIGN.md), [tracker](MASTER_TRACKER.md).
