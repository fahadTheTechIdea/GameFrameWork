# P5-A39: Remove external test privilege and migrate native generator proof

## 0. Problem

The framework still granted OilCo.Tests internal access, and OilCo.Game granted the same test assembly access. The old generator suite created entities and changed inventory through Game.World rather than authored content/player facts. The canonical Oil test project also had131 stale-API compile errors before this stage.

## 1. Goals

- Remove framework/game friend access for external Oil test drivers.
- Preserve and migrate the existing generator-operation case, not duplicate or delete its intent.
- Initialize through actual master-listed WorldSetup JSON rather than raw spawn/stock writes.
- Use public operation/signals/stock and native scheduled fuel depletion for saved proof.
- Report the remaining old-suite migration honestly; no broad green baseline claim.

## 2. Design

Framework AssemblyInfo permits only the framework's own white-box test assembly; OilCo.Tests friendship is removed. OilCo.Game's test friend attribute is removed. The old project comment no longer permits privileged drivers.

OilCoGame registers the existing WorldSetupModule as an optional available module, and the master declares it with no default documents. Normal scenarios do not select it. The migrated fixture explicitly lists its generator-start document and selects its profile. No new startup class, spawn rule, inventory or clock owner.

The existing OilOperationTests.cs moves from OilCo.Tests into OilCo.PublicProof; its original case name is preserved. Old registration is removed and exactly one new registration added. The fixture authors canonical gas_generator initial0.8 natural_gas and a placement via TilePlacement.Arguments. The power system's existing0.4-per-scheduled-hour consumption naturally exhausts it; no direct inventory drain.

The driver reads OperationsView, signals, named stock and immutable snapshots, requests Update and native saves, and checks assembly metadata. It never obtains World or live components. SimEngine.slnx adds the actual public proof project; old sibling solutions/adapters remain untouched.

## 3. API

No gameplay command/schema changes. Optional WorldSetup content registration only. Core2.555.0/save156/replay1; Oil view schemas unchanged. No compatibility bridge or widening of internal visibility.

## 4. Files

Framework AssemblyInfo/EngineInfo/protocol identity. Oil module definition, game project, master, canonical solution, old test project comment/registration. Move and rewrite one existing generator suite; public registration. Stage/evidence/current trackers.

## 5. Errors

Invalid startup remains refused by native world setup. Signals/stock are read from actual engine providers. Foreign information is unavailable as NotFound/NotOwned according to visibility. The bounded proof uses a larger diagnostic timeout only after the first terminal timeout was observed; no live process was restarted.

The old test project's remaining stale/private callers are migration work, not permission to restore friendship or a claim that its build/profile is green.

## 6. Compatibility

No legacy friend grant or host-world shim. One original source/case moves; no duplicate runner registration.

## 7. Verification

Targeted current public-consumer builds and one migrated exact generator case. Native productive output, finite scheduled fuel exhaustion, foreign privacy, pure reads, detached earlier facts, framework/game friend metadata and saved twin continuation. No full/family/old suite runtime.

## 8. Risks

Other old Oil suites still contain obsolete APIs, world-state writers and private domain references. Their full migration/compile/regression, all optional modules and all host/client audits remain open. The older OilCoEngine/sim/adapters are explicitly outside this stage.

## 9. Out of scope

Power/fuel rates or game rules, financial rules, new initializer/stock/state/clock, older sibling engines/adapters, rendering/Draw/platforms, full regression and broad gate closure.

## 10. Status

Public boundary implemented; migrated exact FINAL144/0 in126342ms/observer126539ms. Initial timeout retained; current old test build still fails133 unique errors. [Evidence](../../docs/migration/P5_PUBLIC_TEST_BOUNDARY_VERIFICATION.md). No original acceptance gate closes from this scoped migration.

