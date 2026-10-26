# P4-IC1: Explicit inventory compatibility and metadata observation

## 0. Problem

InventoryView.StoreAccepts queried only a singular local IInletPortDefinition, so callers had no way to select named-only stock metadata. InventoryView.ReadResourcePort directly called the named metadata Accepts callback outside observation and did not withhold valid-but-private resource IDs before invoking compatibility.

## 1. Goals

- Require one explicit store selector for prototype compatibility.
- Preserve the distinction between the real local inlet and named output/service endpoints.
- Route prototype/live compatibility through one guarded evaluator using actual contracts.
- Withhold private resource information without invoking compatibility callbacks.
- Reject callback throw, clock mutation and live target mutation through existing session fault/save guards.

## 2. Design

InventoryView.StoreAccepts(string definition, ResourceId resource, Code port) replaces the old signature. Code.None explicitly selects the actual local IInletPortDefinition; a named Code selects exactly definition.Component<IResourcePortDefinition>(port). Neither absence chooses another endpoint. There is no overload or compatibility shim.

Private Accepts is the single selection/observation algorithm for prototype and live reads. It observes the actual metadata provider, selected space/clock, and actual live target codec when present. ReadResourcePort checks resource visibility before invoking metadata. Hidden resource IDs return NotFound; invalid resource/port inputs retain InvalidArgument.

Existing callers intentionally querying local inventory now pass Code.None. Their semantics are unchanged; named callers can explicitly select actual metadata. The single local inventory is not merged with independent output vessels.

## 3. API

Breaking required selector argument on StoreAccepts. No command, stock, reservation or persistent payload schema changes. Core2.554.0/save156/replay1. No host/renderer/platform change.

## 4. Files

Framework InventoryView.cs and EngineInfo/protocol identity. Eight existing focused-test caller files migrated mechanically. New independent InventoryCompatibilityProof.cs/JSON and thin InventoryCompatibilityTests; registrations and staged records.

## 5. Errors

No matching local/named metadata returns false/NotAccepted, never a guessed compatible provider. Existing EntityObservation latches callback failures and mutation. Private-resource queries run no compatibility callback and return no endpoint facts. Faulted missing-content queries reject rather than publish a healthy false.

## 6. Compatibility

Development-only API replacement; no legacy signature, fallback or duplicate owner.

## 7. Verification

One new exact non-friend, file/master-authored mapless endpoint proof checks local/named selection, named-only endpoints, private resources, saved/interleaved identity and five callback faults. One affected exact mixed output/WIP case retains distinct local feed/output semantics and physical saved batch/flow behavior. Targeted framework test-project and Oil public-consumer builds only; no full/family regression.

## 8. Risks

Frozen metadata is a contract, not a sandbox for arbitrary CLR fields. Instrumentation is explicitly test-only. Global provider/private-state/indirect metadata closure, all resource media/flows, performance, full physical/lockstep/soak matrices and broad acceptance remain open.

## 9. Out of scope

New stock/capacity/reservation/state/catalog/clock models, business rules, Oil gameplay, host/demo logic, adapters/Draw/platforms and full regression.

## 10. Status

New FINAL40/0 and affected FINAL80/0;120 focused final passing checks. Original221 /194 recorded checked /27 open unchanged. No original gate closes. [Evidence](../../docs/migration/P4_INVENTORY_COMPATIBILITY_VERIFICATION.md).

