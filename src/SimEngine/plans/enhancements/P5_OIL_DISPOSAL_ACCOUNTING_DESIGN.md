# P5-A27 — Native disposal processing and resource/environment diagnostics

## 0. Problem statement

Oil DisposalDef/CommerceSystem directly removes stock and bypasses committed resource history.
Water disposal must be physical engine processing, not a second sink scheduler or stock authority.

## 1. Goals

Remove the Oil disposal model/path. Author the existing continuous transformer at30barrels/hour
with ordinary operation control/gates and original inventory/claims. Select native flow history and
environment diagnostics in all six scenarios. Money remains optional.

## 2. Design

water_disposal receives a named disposal transformer, existing finite WIP policy and native Oil
operation gate. dispose_produced_water is a JSON continuous recipe with no outputs, stop cancellation,
named oil.stock input and no money/tools/operating materials. Existing Transformer/ResourceTransactions/
ResourceConversions own progress, claims and consumption. No new disposal class, clock or scheduler.
The shared validator now permits nonempty input-only continuous processing, rejecting empty primary
flows. IdleTransformation/idle_transformation replaces finite-only AutoRepeat/auto_repeat.
The original queue selector chooses actual supported batches or continuous streams; continuous
queue repeat is still forbidden. No deprecated field/property reader survives in active source.
The old hourly lump is replaced by continuous physical throughput; same authored hourly capacity,
no compatibility pulse path. Safe disposal is consumed material, not spills, flaring or pollution.

ResourceFlowsModule and EnvironmentalMetricsModule are declared once in the master/game definition
and selected by six scenarios. Explicit journal retention is authored512records/company.
A cumulative disposed-water metric filters actual consumed produced_water by recipe cause in barrels
per hour. This is diagnostics history, not stock or an engine world-size limit.
Oil commerce retains quantity-day closing only; financial sales remain optional Tycoon.

## 3. API surface

Existing ITransformerWipDefinition now declares IdleTransformation instead of AutoRepeat.
ConfigureOperationCommand, TransformationManagementView, ResourceFlowView and
EnvironmentalMetricsView. No new host API or gameplay. Missing metric/history is typed, not zero.

## 4. Files

Oil Operations/Module, structures/production/master/scenarios, resource-diagnostics/environment-metrics.
Existing shared transformation declaration/contract and six affected older caller/test/provider sources,
engine build/protocol marker and new focused public proof. Stage/tracker/audit/evidence.

## 5. Errors

Missing input/endpoint/provider, bad rate/unit and unsafe operation retain original refusal semantics.
Only available stock can be consumed; reservations, native quantities and save continuation use
the original owners. Wrong cause/unit does not contribute to the metric. Private views clear/refuse.

## 6. Compatibility

Delete old DisposalDef/component/path. No legacy JSON, duplicate sink model or save compatibility.

## 7. Verification

One new bounded exact public Oil proof: authored starting disposal structure/resources, partial
processing, stop/restart, journal/metric conservation, money-off execution, private facts and saved twins.
Also exercise Sold history via an actual terminal, without rerunning the older sales case/full suites.
FINAL99/0/0 in11782ms/observer11925ms; two startup failures retained in
[evidence](../../docs/migration/P5_OIL_DISPOSAL_ACCOUNTING_VERIFICATION.md).
Affected public and separate ExtensionProof compiles clean; older runtime suites not revalidated.
Not physical construction, incident pollution/cleanup or full-size/performance acceptance.

## 8. Risks

Net-input-only continuous processing must preserve actual claims and correctly journal them.
Retention/window expiry is explicit. Thirty barrels/hour is game content, not a universal scientific
injection model; no fictitious subsurface pressure/geochemistry behavior is added.

## 9. Out of scope

New sink schedulers, duplicated stock, Tycoon changes, platform/Draw/host logic, full regression,
construction/rival/fault/stress acceptance and Git publication before whole goal completion.

## 10. Execution order

1. Replace Oil disposal with existing JSON continuous processing and native operation gates.
2. Select existing journal/environment modules and explicit metric policy through master/scenarios.
3. Compile and run only the affected new public proof; retain failures/diagnostics.
4. Update current tracker/audit without closing broader original gates.
