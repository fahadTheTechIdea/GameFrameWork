# P4-G19 — Resource content contracts and the sole catalog

Status: G19c VERIFIED-SCOPED,2026-10-12; Core2.488.0/save154/replay1, FIRST/FINAL146/0/0 in1306ms. Approved mechanical resource/unit API bindings only; business rules unchanged. Final full regression remains deferred until implementation finishes.
G19a/G19b prerequisites below are historical and not rerun in G19c.
G19b/core2.352.0/save154: FIRST/FINAL90/0/0 in455ms; one clean build7.44s.
40 inputs frozen06:56:57UTC; [current boundary evidence](../../docs/migration/P4_RESOURCE_SNAPSHOT_VERIFICATION.md).
G19a runtime below is historical, not rerun on2.352.0. Full G19 remains unproved.
The catalog/consumer migration is now authorized; actual ResolvedContent remains the sole catalog and ID table.
G19a/core2.351.0/save154: one NEW exact case69/0/0 in607ms after a proof-composition refusal.
Three clean builds;37 executable inputs frozen06:47:13UTC. No catalog or Tycoon edits.
[Scoped evidence](../../docs/migration/P4_RESOURCE_DECLARATION_VERIFICATION.md); not full G19 acceptance.
The file-scope decision below was approved by the user. Full P1–P12 remains open.

## 0. Problem and inspected sources

Content/ResolvedContent.cs:17/50/65 stores and publicly returns ResourceDef.
Its Add method:119–123 registers only ResourceDef and writes its internal numeric Resource setter.
Its resource resolution pass:250 and generic exclusion:253 also select the concrete base.
G19a moved the original strict metadata reader/resolver to ResourceDeclaration in Content/Resources.cs;
ResourceDef delegates to it. The remaining catalog/selection dependency is still concrete.
An ordinary Def implementing IResourceDefinition cannot become a catalog resource.

Engine callers, views, transactions, claims, handling and recovery also enumerate/select ResourceDef.
This violates P4's actual contract aliases and one-file/provider extension requirement.
Do not introduce a native-only accessor, wrapper definition, copied resource catalog or compatibility
facade to retain these concrete consumers.

## 1. Required outcome

1. Every actual authored resource implements IResourceContentDefinition, combining immutable
   IContentDefinition with existing IResourceDefinition and one stable identity.
2. Original ResolvedContent registers/enumerates/returns those actual definitions, retaining its
   single numeric ID table, stable-ID lookup, fingerprint and content alias ownership.
3. One ResourceDeclaration reads/resolves original explicit quantity unit, category, phase, traits
   and capacity measures. Native and independent definitions share it; no copied metadata formula.
4. Actual provider numeric identity and referenced units/metadata are validated before capacity
   policies, consumers or Initialize, including unused malformed providers.
5. Existing resources/stock/reservations/receipts/work/WIP/flows/views use the same actual definitions.
   Physical travel/service/build/manufacture timing remains with original engine owners.

## 2. Catalog and numeric identity

Revise existing _resources to IResourceContentDefinition entries and Resources/Resource(ResourceId)
to return actual content contracts. IResourceCatalog remains the sole catalog abstraction.
Add detects duplicate stable IDs across resource kinds explicitly before publication.
Preserve original collection/load ordering; do not renumber via a second sorted catalog.

The existing catalog owns numeric identity. Metadata resolution reads its assigned ResourceId through
ContentResolver.Resource(stableId); native/direct providers retain that immutable authored identity.
Do not add a public ID setter, gameplay resource registrar, host callback or parallel ID allocator.
Verify returned resource identity aliases exactly this table and stable-ID map.

Resolve actual resource contracts once after unit contracts and before capacity/other consumers.
Native BasicResourceDef remains a legitimate built-in authoring kind, not a legacy path.

## 3. Common authoring and metadata validation

Extract original strict fields/read/resolve/HasTrait into ResourceDeclaration.
Retain required explicit traits (including authored empty arrays) and positive per-quantity measures.
Resolve actual IUnitContentDefinition aliases; never infer missing density or native measures.
Require unambiguous one-measure-per-dimension, matching owned numeric/content identity and valid
quantity metadata. Fractional coefficients for discrete capacity measures remain valid when the
existing exact quantum admits a representable native amount; do not impose an arbitrary coefficient
or quantity ceiling.

Independent providers may supply different authored semantics through the same contracts, subject
to original validity/ownership rules. Fail missing/default/invalid metadata before Initialize.
No broad CLR execution sandbox is claimed.

## 4. Original consumers and immutable views

Migrate concrete selections/locals in original inventory, flow ledger, physical handling, recovery,
claim/transaction checkpoints, transformations, upgrades, work resources, rates, spatial layers,
quantity signals and resource controls to actual resource content contracts.
Public extension visibility/knowledge/endpoint/environment/signal consumers migrate in place.
Native kind registrations and legitimate native subclass declarations are retained.

PlayerView.CopyResources enumerates actual catalog definitions. Copy traits/measures/unit facts into
detached immutable views under existing observation/visibility rules; do not expose executable
providers or share a mutable external traits list. Preserve actual resource IDs in every receipt,
quantity, bill, save and flow fact. No new balance or material-progress store.

## 5. File-scope decision: necessary optional Tycoon bindings

Current AGENTS excludes Tycoon work. The breaking framework catalog cannot be completed cleanly
without updating its already-existing concrete consumers:

- Standard/Tycoon/Suppliers.cs:35 assigns Content.Resource(resource) to ResourceDef. Changing the
  framework return contract makes this assignment fail compilation.
- Standard/Tycoon/BusinessValues.cs:53/65/110 stores/selects/enumerates ResourceDef. The value catalog
  must inspect actual resources, not cast independent definitions back to a native class.
- Standard/Tycoon/BusinessValues.cs:66/67/78/92 and BusinessAccountDefinitions.cs:34 resolve concrete
  UnitDef instead of the authored unit contract; these are the already-recorded P4-G18 boundary.

Approved scope,2026-10-12: mechanical resource/unit content type bindings in these THREE files only.
Leave prices, money/budget defaults, business modes, valuation formulas, purchases, accounts,
reservations, content JSON, timing, command behavior and optional module selection unchanged.
Do not run Tycoon/full/historical tests or modify unrelated Tycoon files.
This is not permission to add business features or make money mandatory.

Do not change unrelated Tycoon files or retain compatibility accessors to hide the dependency.
No prices, balances, formulas, business modes or default-money behavior changes. The separate final full regression permission does not authorize broad runtime during this migration stage.

## 6. Public surface and durability

Add IResourceContentDefinition and shared ResourceDeclaration API, not a second model/catalog.
ResourceCapacityMeasure authoring must be usable by independent providers with validated units/
positive coefficients; central resolution enforces actual ownership.
All references select existing content contracts; no alternate JSON names or old schema readers.
Use the normal breaking engine build identity; no state codec changes unless execution proves a
real schema change is necessary. Exact existing ResourceId representation and clock remain.

## 7. Verification after the scope decision

Build only as needed. Add ONE NEW exact bounded case:
Resource content contracts: independent JSON catalog, physical hauling and manufacturing.
An explicit master-listed resources-contracts.json is the sole author of selected direct resource
IDs; omit corresponding native fixture entries rather than runtime patching/overlays.
Reuse existing units/capacity content and native physical owners.

Prove actual catalog/alias/numeric identity, unit/measure/trait metadata, direct/native compatibility
math, immutable private views, physical hauling/WIP/input/tool/output/flow accounting, saved phases,
sole-clock pause, replay/session isolation, unused malformed and duplicate/wrong-kind refusals.
Every workflow stage and child process has explicit bounds/diagnostics.
Only repair/rerun this same NEW exact case. No historical/family/full/Oil/Tycoon/platform runtime.
Record exact commands/build/process/timings/current input hashes; retain all original acceptance
requirements and checkboxes. A scoped case never closes the broad catalog/transaction/full gates.

## 8. Risks and remaining requirements

Numeric identity drift, incorrect compile-only casts, mutable provider metadata, mixed-resource
reservations, remote/spatial/network/resource-control breadth, arbitrary fault/rollback, lockstep,
shuffled content/input, stress/fuzz/soak/performance and full P1–P12 remain open.
Older public fixtures must still compile; no historical runtime promotion.
Optional business provider runtime is not proved by a framework-only case/build.

## 9. Non-goals

No business behavior/content expansion, mandatory money/budget, domain/Oil edits, Draw, Unity/Godot/
adapters, duplicate clocks/catalogs/balances, compatibility path, staging or publication.
Commit/push only after the full goal AND plan genuinely finish and are verified.

## 10. Execution order

G19a (engine-only): extract the existing strict resource reader/resolver/trait lookup into the sole
public ResourceDeclaration; make ResourceCapacityMeasure explicitly valid and publicly authorable.
Native ResourceDef delegates to that declaration, retaining the existing numeric ID owner and public
catalog return types. No compatibility accessor, copied parser or independent catalog is introduced.
Use one NEW bounded exact case, `Resource declarations: shared JSON metadata and timed native hauling`,
for native/specialized JSON, malformed unused metadata, immutable views, physical hauling and saved twins.
This is prerequisite proof only, NOT independent resource catalog registration or broad acceptance.
Record build/evidence and leave the three-file Tycoon authorization pending.

G19b (engine-only): finish the existing ResourceInfo boundary. Copy trait strings, measures and unit
facts into detached readonly collections; CopyResources observes metadata through the same existing
EntityObservation.ReadProvider guard used by CopyUnits/CopyCapacityPolicies. Keep the original
visibility predicate and staged atomic list publication. No catalog return-type or Tycoon changes.
One NEW bounded exact case, `Resource snapshots: detached metadata and guarded atomic publication`,
proves retained copies against mutable metadata instrumentation, nested unit read exceptions/clock
mutation, hidden content, pure healthy reads, saved continuation and interleaved sessions.
This does not prove independent resource registration or arbitrary CLR/content mutation sandboxing.

Then file-scope authorization → original content contracts/catalog → original consumers/metadata views
and permitted mechanical optional references → explicit direct content/public proof/thin player →
focused build/runtime → source freeze/evidence/master/audit reconciliation.

[Parent](P4_REUSABLE_MODULES_DESIGN.md), [resources](P7_RESOURCES_DESIGN.md),
[unit checkpoint](P4_UNIT_CONTENT_CONTRACTS_DESIGN.md), [tracker](MASTER_TRACKER.md).


## G19c scoped delivery —2026-10-12

Core2.488.0/save154/replay1; state/view schemas and compact ResourceId encoding unchanged. ResolvedContent now registers, resolves, validates and returns actual IResourceContentDefinition providers through its existing numeric table and stable-ID map. ResourceDeclaration binds that original numeric identity during its single resolution and validates every loaded resource's catalog aliases, quantity-unit ownership, traits and dimensional capacity before consumers or Compose. Native ResourceDef remains a legitimate built-in authoring helper; no native-only catalog accessor, public ID setter, wrapper, fallback or legacy load path was added.

Original inventory/handling/claims/movement/conversion/flows/recovery/work/upgrades/operation/rate/layer/environment/control/view callers now use resource content contracts. Public extension endpoint/knowledge/visibility/signal consumers migrated directly. Native class-specific fixture assertions are retained; the old readonly collection cast is mechanically updated to the actual returned contract type.

The three approved Tycoon files contain only ResourceDef→IResourceContentDefinition and UnitDef→IUnitContentDefinition token substitutions. Comparison against preserved full source is exact after only those substitutions. Prices, defaults, business modes, valuation/payment/purchase/accounting formulas, resource receipts, content JSON and timing are unchanged. Other Tycoon files and sole clock implementation are unchanged; no Tycoon runtime was run.

### Exact compile/runtime evidence

- Solution command: dotnet build Beep.SimEngine.slnx --no-restore;9.04s,0 warnings/1 new driver error (JsonValue has no RemoveAt). Engine and independent modules compiled. New authoring driver now replaces its actual JSON array through the supported API; no assertion removed.
- Focused repair command: dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore;6.73s, clean0 warnings/0 errors.
- ONE NEW FIRST/FINAL exact case:146 passed/0 failed/0 aborted in1306ms; PID18340 Exited0/process1470ms, no truncation/observer error. No executable edits after FINAL.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Resource content contracts: independent JSON catalog, physical hauling and manufacturing" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
```

Four minute/half-minute public workflows (completion/cancellation) load master-listed resources-contracts.json, with corresponding native supply metadata absent, plus a valid unused independent resource with fractional count-capacity coefficient. Actual aliases and single resolution, compact public IDs, detached readonly metadata, actual eight-unit physical hauling, two-unit WIP, reusable tool, partial/save/pause/private process facts, correct single completion/return, replay and interleaved session isolation pass. Fourteen unused malformed identity/category/unit/phase/trait/measure and duplicate/cross-kind/wrong-kind definitions refuse before Compose. Driver uses public seats/views/commands/Game.Update/saves only. Stage512Update and60000ms process bounds with diagnostics; no host stock/work/time decisions.

No old/family/full case ran; previously existing cases compile-only. This proves the direct catalog/consumer migration and named workflows, not every optional business/resource/utility/network/quality/lot/provider/fault/lockstep/shuffle/stress/allocation path. Original221/187checked/34open unchanged; additional216. Full implementation/goal remains open. The user-authorized one final full regression stays deferred until implementation is finished. No Oil/platform/Draw edits or staging/commit/push.


## Post-FINAL source/metadata closure

Freeze: 2026-10-12 09:51:43 UTC;1736 source anchors/970 selected executable inputs/13 quickstart anchors.27 previously selected inputs changed;6 existing inputs newly admitted plus3 new executable files.33 existing executable files change (22 production/11 proof-test-registration inputs);4 new files total (probe/JSON/driver/evidence). Existing Upgrades.cs also joins source anchors. Existing G19 plan reused. No new production source or state codec/schema, and no executable edits after FINAL.

Read-only closure:0 source/quickstart mismatch/0 executable drift;2922 paths/1039 Markdown/8692 local links/0 broken;18 valid master rows;221 original literal requirement/checkbox/line anchors validated with0 errors. Driver boundary0. Direct current master record identity/solution/focused/exact runtime/timings/limits checks all pass. git diff --check exits0 with34 existing warning output lines; dirty2910/staged0. Original221/187checked/34open unchanged; additional216; goal_complete=false.

Tycoon approval audit: all3 changed files match their preserved full source after ONLY ResourceDef→IResourceContentDefinition and UnitDef→IUnitContentDefinition token substitution;9 other Tycoon source anchors remain exact. Sole-clock implementation hash unchanged. Prices/defaults/formulas/business modes/behavior JSON unchanged; no Tycoon runtime claim. Production resource concrete-consumer scan0 (only legitimate native authoring declarations remain).
