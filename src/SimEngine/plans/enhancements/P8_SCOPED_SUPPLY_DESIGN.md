# P8-D5b — automatic physical supply to exact resource ports

## 0. Problem statement

TransformerOperatingInputs.RequestInputSupply currently discards scoped requests and sends only
local Bills. LogisticsService.Demand, whole-source replacement and BuildQueue address an entity
inventory rather than the actual feed/tool/operating stores. The explicit D5a physical haul path
already supports named/scoped endpoints; automatic supply must use it, not duplicate it.

Current focused result: core2.317.0/save151/logistics17/logistics-view31. ONE NEW exact case,
FOUR attempts2/1,25/1,61/1,FINAL63/0/0 in8752ms. EIGHT builds, final clean3.49s.
Executable freeze21:14:21 UTC; prior runtimes historical/not rerun. This slice is verified only
within [recorded evidence](../../docs/migration/P8_SCOPED_SUPPLY_VERIFICATION.md); full P8/goal OPEN.

## 1. Goals

1. One whole replacement payload records an explicit named port, actual endpoint and native Bill
   per requested store. Every line preflights before any previous demand row changes.
2. The existing demand list stores those bindings and retains ages for unchanged lines. Named
   aliases and same-resource different stores are neither silently merged nor overwritten.
3. Original scheduling computes gaps from actual unpromised stock and inbound promises; native
   trucks load, travel and unload through D5a. Current selection never redirects acquired cargo.
4. Batch and continuous transformers derive requests from actual authored bindings, including
   original tool references, not a local-inventory fallback. Private views/save/hash/replay use
   the same demand records and execution owners.

## 2. Design

LogisticsSupplyRequest is detached (Port, Endpoint, Quantities), not a stock owner. Replace the
Bill payload on ITransformationSupplyRequests and ILogisticsDemandRequests with the explicit
whole list. Existing work/stock callers construct an explicit entity endpoint request. No old
overload, implicit remote-to-local substitution or compatibility reader remains.

Demand adds named Port and ResourceEndpointId Endpoint. Whole admission validates the source,
company, actual physical site, exact original port/provider, resource quantum and representable
aggregate. Duplicate endpoint/resource lines in one envelope refuse. Empty envelopes withdraw the
source only after its original destination/source admission; live haul claims remain independent.

BuildQueue groups by actual endpoint/resource. Stable dispatch entries encode port/scope/64-bit
identity with lengths, avoiding concatenation collisions. Original priority/age/route-cost/actor
arbitration remains authoritative. Offers retain destination bindings even when refused. Current
acquisition freshly resolves the exact offered destination and acquires through D5a.

Native entity stock-policy operations remain entity-stock policies; they must not include scoped
rows simply because the physical site shares an entity ID. Generic demand protection/gaps share
one endpoint-based algorithm. Automatic source discovery and standing route/export management
are not generalized in this slice; supply can come from existing real entity warehouses.

## 3. API surface

- ILogisticsDemandRequests.CheckRequests/ReplaceRequests: source, company, physical destination,
  IReadOnlyList<LogisticsSupplyRequest>, priority.
- ITransformationSupplyRequests.RequestInputs: actual ITransformer, same explicit list, priority.
- Existing manual haul commands are unchanged. No new host workflow, instant transfer or command
  bypass. Work and stock single-store intents explicitly identify their actual entity endpoint.

## 4. Files to change

Existing owners: Standard/LogisticsDemandContracts.cs, LogisticsDemands.cs, Logistics.cs,
LogisticsScheduling.cs, LogisticsStockPolicies.cs, HaulResourcePorts.cs, TransformationModule.cs,
TransformerOperatingInputs.cs, TransformerFlow.cs, Views/LogisticsView.cs, Kernel/SaveGame.cs.
Migrate current caller payloads and protocol manifest; add JSON-native scoped supply scenario and
one new thin-driver bounded case through the existing proof composition.

## 5. Error handling

Invalid source/port/store/company/resource/quantum/duplicate/range refuses before replacement.
Provider observation/mutation violations fault the normal engine update, not compensation.
Removed sites expire through existing ownership/cleanup. Active acquired hauls retain their
original receipts when a replacement/selection changes. Typed resource shortages wait physically.

## 6. Compatibility

Development only: migrate API/save/schema identities directly. No legacy overload or old codec.

## 7. Verification

Build only for compilation. ONE NEW exact bounded case proves independently scoped demands,
physical delivery/manufacturing, same-resource multi-store separation, saved continuation,
selection/replacement, cancellation/privacy and deterministic replay where exercised. Only that
case may be repaired/rerun. All earlier/family/full/Oil/platform runtimes remain unrun.

## 8. Risks

Same-resource aliases must not duplicate a Bill; aggregate the actual transaction vector first.
Request reads must not mutate providers. Empty replacement must retire only its source. Historical
tests are migrated for compilation, not promoted to current verification evidence.

## 9. Remaining scope

Standing scoped routes/export management, scoped stock bands, automatic discovery of scoped
sources, receiving-only/custom provider/fault/area/dock/congestion/recovery/stress/accounting
matrices and full P8/P1-P12 acceptance remain open. All gameplay remains engine-owned.

## 10. Execution order

1. Migrate request/demand representation and whole admission in the existing owner.
2. Migrate endpoint gap/group/dispatch/acquisition and native transformer binding projection.
3. Migrate compile callers/codecs and add the master-listed normal scenario and exact gate.
4. Record actual build/runtime attempts; update tracker/audit without changing original acceptance
   text or broad completion claims. No commit/push until FULL goal AND plan completion.
