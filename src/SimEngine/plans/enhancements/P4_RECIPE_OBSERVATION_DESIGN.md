# P4-G22 — Detached and guarded recipe-management facts

Current M89/native2.729/save161/replay2: actual sibling-control/space mutations reproduce778/120;
existing view scopes now observe whole actual bound entity and selected space.44-context938/0
and directly affected1724/0 qualify this selected correction, not full P8 or arbitrary CLR state.
[All attempts, source responsibility and remaining gates](../../docs/migration/P4_RECIPE_OBSERVATION_VERIFICATION.md#current-m89-whole-participant-recipe-projections--2026-10-22).

## 0. Problem statement

Recipe rate facts are already detached through ResourceRateInfo, ResourceInfo and UnitInfo.
M89 closes a remaining purity gap: observing only the selected processor/scalar entity facts
misses saved sibling-control and selected-space mutation during selection/projection/disclosure.

## 1. Goals

1. Publish readonly rate values with deeply detached resource/unit facts.
2. Guard selection, visibility, support checks and projection together; publish no partial
   out value if a callback fails or changes scoped entity/provider/clock/identity state.
3. Preserve native resource math, physical processing, public refusal semantics and privacy.
4. Verify one new bounded exact case; leave broad regression and full P1–P12 open.

## 2. Design

Existing `Hosting.ResourceRateInfo` is a readonly view value (not `IResourceRate`), reusing
the existing `ResourceInfo`/`UnitInfo` projections. The rate-read return type is already
directly migrated. No compatibility method or second rate calculator.

Use the one private generic recipe-fact projection in `TransformationManagementView` for
tool bills, tool rates and input/output/tool/operating ports. Resolve actual owned named
processor first; the existing `EntityObservation.ReadProvider` then guards recipe
selection, viewer visibility, actual support and complete projection with whole target Sync
and selected space. Queue disclosure and enumeration use the same existing helper options;
definition-only output-ID projection guards selected space, not unrelated entity state.
Only publish after guard verification. The existing immutable `Bill` remains authoritative.

## 3. API surface

`ReadToolConsumptionRates(..., out IReadOnlyList<ResourceRateInfo> rates)` replaces
the old live-provider return. Quantity and exact period facts remain unchanged;
`Resource` is detached `ResourceInfo`, not an executable definition.

## 4. Files

- Engine: `Hosting/PlayerView.cs`, `Views/TransformationManagementView.cs`.
- Public extension proof: recipe callback instrumentation using the shared declaration.
- Thin player test, registration and mechanical old consumer cast migration.
- Stage evidence, master tracker and completion audit (no completion-gate removal).

## 5. Errors

Foreign/missing processor: `NotFound`; missing/hidden recipe: `UnknownDef`;
unsupported actual recipe: original `NotMadeHere`. All publish empty facts.
Callback exception or scoped mutation: original `observation.provider` fault, no
partial result, no subsequent healthy Update/save/hash or callback retry.

## 6. Ownership and formats

No shim or legacy overload. M89 changes no public signature, payload codec or save/replay schema.

## 7. Verification

Use the targeted test-project build, not a full solution/profile gate. Run the new literal
`Recipe participants: pure projections protect sibling controls and selected space` before/
after repair and the three directly affected recipe/management/physical accounting literals
recorded in the evidence. Each uses60000ms/5000ms/30000-character bounds. Keep negative raw
reflection explicitly separate from ordinary public-command physical/replay acceptance.

## 8. Risks

The guard is scoped observation, not an arbitrary CLR sandbox. No claim is made that
all metadata mutation or every management method is guarded by this slice.

## 9. Out of scope

Tycoon/business-rule changes, platforms/rendering, alternative
clocks/stores/calculators, historical/full suite execution and publication.

## 10. Execution order

Inspect original owners; replace live view return and unify projection; add proof and
thin player case; build and run that case; record actual evidence and retain open gates.

Status: scoped verified, 2026-10-09. Final258/0/0 in2717ms; core2.357.0/save154/replay1.
[Evidence](../../docs/migration/P4_RECIPE_OBSERVATION_VERIFICATION.md). Full goal/plan remains open.
