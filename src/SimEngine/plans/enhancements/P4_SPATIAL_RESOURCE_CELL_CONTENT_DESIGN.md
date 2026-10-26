# P4-G61 — spatial resource-cell content contracts

## 0. Problem and scope

SpatialResources.cs exposes an abstract native SpatialResourceCellDef as the required authoring/endpoint/extraction API. Native tiles and independent point cells inherit it. This leaves a concrete mandatory content base inside the otherwise contract-driven layered resource system. Remove it, do not preserve a compatibility alias. Existing actual layer quantities, embedded receipt books, endpoint versions, ResourceTransaction and WorkBoard remain authoritative.

## 1. Design and ownership

ISpatialResourceCellDefinition declares immutable identity/layer/capacity/access plus selected-space validation, binding and site compatibility. One SpatialResourceCellDeclaration reads/resolves/validates common metadata and binds the existing generic SpatialResourceEndpoint<TLocation>; one TileResourceCellDeclaration owns native tile location semantics. Native TileResourceCellDef implements the interface directly and delegates. Existing independent point geometry delegates common declaration and retains only provider-owned coordinate checks. All endpoint enumeration/lookup and extraction Cell references use the interface, not a native base.

Original endpoint composer admits every frozen actual cell alias before binding, detects duplicate scoped identities, validates actual quantity/capacity/access metadata and selected provider callbacks before Initialize. Binding may register the original embedded receipt book but must not change time or spawn entities. CheckSite is a pure guarded provider observation. No stock copy, replacement catalog, balance/receipt/scheduler/state duplication or instant extraction.

The actual tile master exposed a disclosure regression: valid line and layer definitions may have the same string ID because the existing catalog scopes identity by definition kind. The sole WorkBoard disclosure walk now checks uniqueness by actual kind/ID, retaining frozen identity/ownership validation and duplicate-alias refusal. No unrelated definition renaming or alternate visibility path. Extraction effect validation invokes the same common metadata rule before dereferencing a provider layer, so malformed direct metadata refuses as ContentException rather than NullReferenceException.

## 2. API and migration

Delete SpatialResourceCellDef. ResolveCell takes ISpatialResourceCellDefinition; SpatialExtractionEffectDef.Cell resolves that same alias. Native/direct JSON share reader and location strategies. Update existing point geometry registration and historical native owner-test type references in place (compile only). Core behavior2.420.0/save154/replay1; field/codec schemas unchanged. Existing IResourceOutlet still inherits IResourceInlet, finite deposits explicitly reject inbound material rather than inventing replenishment.

## 3. Verification

ONE NEW exact bounded case: authored native/direct tile metadata and independent non-tile extraction through ordinary activity/hauling commands, actual finite layer debit/inventory credit, timed machine work, blockers, immutable/private provenance, pending/partial saves/twins/replay/session isolation. JSON startup profiles arrange deposits/entities, never driver stock writes or instant spawn commands. Negative metadata/duplicate identities/missing references/wrong selected geometry/invalid direct providers refuse before Initialize. Preserve actual failed attempts; targeted same-case repairs only. No old/family/full/Oil/Tycoon/platform runtime.

Scoped FINAL896/0/0 in3317ms, owned process27816 exit0/3464ms. Eight compile attempts and six same-case runtime attempts are retained in the evidence. The point fixture authors a carrier on the delivery side and an additional real service slot: its one-dimensional machines still cannot cross or overlap a stationary extractor. Explicit retry metadata retains exhausted work for saved blocker/cancellation assertions. Stage complete only within this scope; no broad gate checked.

## 4. Tracking

Original221 words/checkboxes remain159checked62open unless whole matching requirement is proven. This advances P2.003/P4.003/P7/P3 layered extensibility, not their full broad acceptance. Arbitrary provider CLR purity, spatial-layer definition migration, inbound/refill content, all withdrawal failure matrices, performance/lockstep/full P1–P12 remain OPEN. Protected G19/sole clock untouched. No staging/commit/push before full goal AND plan finish.
