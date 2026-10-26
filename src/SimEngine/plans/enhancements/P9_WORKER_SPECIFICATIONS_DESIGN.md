# P9-B10 — Explicit worker specifications and movement-class authoring

Status: focused slice complete,2026-10-06. Full P9/P1–P12 remains partial.
Build2.111.0/save65/work view35/work system8; clean build and291 checks/two exact suites.
[Source-specific evidence](../../docs/migration/P9_WORKER_SPECIFICATIONS_VERIFICATION.md).

## 0. Problem

Before this slice, WorkerDef.Read invented work_rate1 and MoverDef.Read invented class land when JSON omitted them.
IWorker.WorkRate also published authored base data as a runtime getter with no definition contract;
independent workers can bypass static capability metadata validation. Rates/roles should have one
authored authority; modifiers/readiness remain the existing execution contracts.

## 1. Goals

1. Native worker JSON must explicitly name positive work_rate; native mover must name a valid class.
2. IWorkerDefinition owns immutable base rate/declared roles. Remove runtime base-rate getters, no shim.
3. Validate definition/runtime association and live role facts through one existing execution owner.
4. Private immutable worker execution views expose base rate/roles alongside actual effective rate.
5. Prove native/public/stationary/point/tile authoring, physical rates, guards and saved continuation.

## 2. Design

- Add IWorkerDefinition : IComponentDefinition in existing Capabilities.cs with WorkRate/Roles.
  WorkerDef and independent ContractWorkerDef implement it; no new content registry or worker model.
- Remove WorkRate from IWorker and native/public runtime classes. WorkerExecutionContracts.BaseRate
  reads the actual matching definition with scoped purity/positive/catalog-role checks. Existing
  Read applies the same execution multipliers/gates to that authored base rate. Public requirements
  use BaseRate for declared machine rating, not accidental effective-rate or runtime copying.
- Composition rejects missing/mismatched worker definition contracts, nonpositive base rate and
  empty/duplicate/unknown roles before Initialize. Actual live worker must alias its entity/declared
  component type and expose the same roles. Shared Can validates the requested role against the
  authored set and actual pure provider answer; assignment/current-job/views use it.
- WorkerExecutionView adds detached BaseRate/Roles; host gets no metadata/provider handle. Existing
  WorkSystem/checkpoint/update owns validation, no new scheduler or settings/role/rate state.
- Native mover class is required/nonblank and resolved by existing selected NavService validation.
  Native speed is already required/positive; do not invent a replacement movement-rate default.
- Native WorkerDef/MoverDef and public ContractWorkerDef reject repeated Read/Resolve. Their actual
  loaded metadata cannot be reauthored; arbitrary external-definition immutability remains a wider gate.
- Migrate affected authored JSON literals in place to explicit existing intended rate1/class land.
  No test/runtime helper inserts missing fields, alternate reader or old content translation path.
  Existing explicit rates/classes remain untouched. Prototype fingerprints change naturally.
- Base work rate retains the existing dimensionless work-tick scalar convention; arbitrary unit
  conversions/formulas are not introduced or claimed. Dynamic rates stay in IWorkerExecution.

Build2.111.0/save65/work view35/work system8. Native worker/mover/WorkBoard payload layouts unchanged.

## 3. API surface

IWorkerDefinition.WorkRate/Roles are authored metadata. WorkerExecutionContracts.BaseRate(World,Entity)
and Can(World,Entity,WorkerRoleId) supply guarded engine access. WorkView.ReadWorkerExecution publishes
new immutable BaseRate/Roles facts plus effective rate/gates. No runtime IWorker.WorkRate compatibility.

## 4. Files

| Action | File | Scope / risk |
|---|---|---|
| Modify | Standard/Capabilities.cs,Work.cs,WorkerExecution.cs,WorkModule.cs | Sole definition/execution/dispatch; medium |
| Modify | Standard/Movement.cs,Views/WorkView.cs,Kernel/SaveGame.cs | Explicit class/role query/identity; medium |
| Modify | ExtensionProof/ContractCapabilities.cs,ContractRequirements.cs | Public-only metadata/requirements; medium |
| Modify | Existing authored JSON/literals | Explicit content migration only; low |
| New | Tests/WorkerSpecificationTests.cs | Exactly two named gates; medium |
| Modify | Program.cs, preceding identity/system assertions | Compile-only migration |
| New | docs/migration/P9_WORKER_SPECIFICATIONS_VERIFICATION.md | Source-specific evidence |
| Modify | master/P9/README/AGENTS/help | Accurate current scope |

## 5. Error handling

Missing/null/string/zero/negative/unrepresentable work_rate and missing/null/blank/nonstring class fail
explicitly; unknown declared classes use existing navigation admission. Missing/wrong definition
metadata and empty/duplicate/unknown roles refuse before tick one. Pure live role/rating/provider
violations throw instead of authorizing work; faulted Update cannot checkpoint. No global rollback claim.

## 6. Compatibility

Development-only clean revision. Delete runtime base getters, no aliases/defaults/old JSON/save reader.

## 7. Verification

Build; run only these two exact complete names:

- `Worker specifications: explicit JSON, definition contracts and private facts`
- `Worker specifications: authored physical rates, role guards and saved continuation`

Cover required authoring/metadata, frozen private facts, independent worker/role faults, native and
public timed work/motion/saves/interleaved sessions. Prior suites/content compile/static-audit only;
no preceding/third/family/full run.

## 8. Risks

This deliberately breaks external providers using runtime WorkRate. Existing role order is preserved;
rules become checked against declarations. Wider public declaration immutability/purity and combined
rate overflow/operation-command admission remain separate gates, not hidden fallback behavior.

## 9. Out of scope

Dynamic formulas/arbitrary unit rates/service/areas/routes/future fuel budgets/global provider/replay/
stress/allocations/full P9/framework acceptance. No Oil/Tycoon/host/demo/platform/Draw or size cap.

## 10. Execution order

- [x] Revise actual definition/runtime/read/role dispatch and authoring contracts.
- [x] Migrate explicit authored content; add public/native focused proofs and assertion migrations.
- [x] Build and run exactly the two named gates; repair only within them.
- [x] Record source evidence and align master/parent/guidance; full goal stays active.
