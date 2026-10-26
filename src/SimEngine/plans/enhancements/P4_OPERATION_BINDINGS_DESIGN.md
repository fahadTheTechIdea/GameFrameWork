# P4-G25 — Actual operation metadata and guarded controls

## 0. Problem

OperationsView.TryGetOperationSchema and OperationContracts.ReadControl use runtime Component
casts; ComponentDef duplicates authored ControlSettings in DeclaredControls. Pure control reads
and operation status/identity enumeration do not consistently observe the actual provider codec.

## 1. Goals

1. IOperationDefinition exposes the actual registered OperationSchema, in the existing owner.
2. Delete duplicated declared-controls cache and validate actual schema/control metadata once
   in the existing final component validator, including unused definitions.
3. Original OperationContracts owns actual named selection, schema/status/control reads and
   pure control admission under existing EntityObservation, using actual metadata contracts.
4. All existing control consumers use the same owner with explicit executing World.
5. Guard public operation ID/status/schema/control projections; publish only complete facts.
6. Prove original physical manufacturing, public command/refusal parity and save/replay plus
   hidden-state/getter failures through one new bounded thin-player case.

## 2. Design

Add readonly OperationSchema to IOperationDefinition. Remove ComponentDef.DeclaredControls
and its early ResolvedContent copy/validation. Original final ComponentDef.Validate verifies
actual registered schema alias, Controllable against ControlSettings and authored control ID.

OperationContracts uses EntityDef's original named slot to select IOperationDefinition from
the actual live IOperation. ReadSchema, Read and ReadControl use existing ReadProvider.
Check observes the complete admission callback, retaining original bounds/refusal semantics.
Commit remains the original explicit state mutation; no pure-read guard wraps Configure.

OperationsView uses this owner and detached ID staging for exception-safe ID publication.
Migrate explicit World through all callers and remove redundant pure-control wrappers where
the original operation owner now supplies the complete codec guard.

## 3. API

Metadata contract gains readonly schema. Public view/command signatures unchanged.
Internal ReadControl takes World explicitly. Persistent engine schemas unchanged.

## 4. Files

Operations, Defs, ResolvedContent, OperationsView; existing ControlActionPolicies,
OperationPolicies, Transformations, TransformationStaffing, TransformerOperatingInputs,
TransformerFlow consumers; existing authored public signal/control proof extended in place,
new thin OperationBindingTests; protocol build; stage/evidence/master/completion audit.

## 5. Errors

Invalid aliases/controls/identity refuse before Initialize. Missing/private reads remain false
with empty/default out facts. Pure provider/metadata exception or scoped mutation faults through
original observation.provider; no partial facts, healthy continuation or retry.

## 6. Compatibility

Development-only breaking internal/metadata enhancement; delete old selector/cache, no shim.

## 7. Verification

Compilation build and ONE NEW bounded exact case:
Operation bindings: actual metadata owns controls status and physical command flow.
No historical/family/full suites; full acceptance remains open.

## 8. Risks

Framework Component/state codec remains required; not arbitrary CLR/content sandbox.
Mutating Configure transaction breadth and operation policy decomposition remain separate.

## 9. Out of scope

Tycoon/resources pending authority; business/defaults; platforms/rendering; publication.

## 10. Sequence

Schema contract/final validation; sole guarded read/admission owner; consumer migration;
public extension probes and authored thin player; bounded proof; current-source evidence.

Status: narrowly verified, core2.360.0/save154/replay1; final NEW bounded case499/0/0 in1556ms.
[Actual evidence](../../docs/migration/P4_OPERATION_BINDINGS_VERIFICATION.md).
Earlier runs and repairs are recorded; only this new case was run. Full P1–P12 and goal remain
OPEN; arbitrary provider/CLR sandbox, full operation policy/capability decomposition, complete
mutating-control transaction scope, spatial deep immutability and global acceptance unproved.
