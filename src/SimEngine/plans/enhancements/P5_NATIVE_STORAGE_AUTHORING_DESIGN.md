# P5-A20 — Canonical storage policies and paired resource ports

## 0. Problem statement

Actual Oil initialization refuses the first storage component because capacity/fluid scalar authoring
does not select the framework's existing storage/capacity contracts. Full content cannot initialize.

## 1. Goals

Migrate all26 stores across25 structures/seventeen vehicles, preserve capacities/public flags/accepted
resource lists, make phase compatibility explicit, and expose one named oil.stock port per inventory.
Use one inventory owner; preserve every outlet's inlet capability. No physical work requires Tycoon.

## 2. Design

oil/inventory.json declares sixteen shared capacity policies for the sixteen original numeric limits,
in the actual unit.storage-slot measure already authored for all18 Oil resources. The game master
lists this file under oil; no directory scanning or framework defaults. Storage components reference
their policy and explicitly author initial_stock, accepts, phases, public and evacuate. fluid=true maps
to the current liquid/gas phases; nonfluid maps to solid. Original explicit accepts remain authoritative.

Each storage entity selects existing InventoryPortDef with oil.stock, reusing its actual native
inventory. Eight previously authored bindings are retained, eighteen missing bindings added; no
second balance, reserve, endpoint, cargo store or port registry. Native IOutletPort extends IInletPort.
Existing StorageDeclaration/ResourceInventoryState/CapacityPolicyDeclaration/resource transactions
and logistics remain the sole owners. No framework algorithm, schema or clock change.

## 3. API surface

Existing IStorageDefinition/IStorage, IInventory/IOutletPort/IInletPort, ICapacityPolicyDefinition and
IResourcePortDefinition. Existing public HaulResourcesCommand, InventoryView and LogisticsView.
No compatibility reader for capacity/fluid.

## 4. Files to change

Oil structures.json/vehicles.json and master; new canonical inventory.json. Linked canonical resources
in the existing ExtensionProof project; one thin focused storage driver/registration.
Stage/evidence/master/audit updated in place.

## 5. Error handling

Unknown policy and phase-incompatible accepted resource references must refuse content admission.
Capacity filters never substitute another store or invent unlimited room. Full destination refuses a
new physical haul while already-earned quantities remain real inventory.

## 6. Backward compatibility

Development-only migration. Scalar capacity/fluid authoring is removed, not retained.

## 7. Verification

One new bounded exact case loads all26 canonical storage declarations in an explicit native projection,
checks all18 resource compatibilities, pairs outlet/inlet contracts and physically hauls solid/liquid/gas
with cargo/destination limits, conservation, saved partial cargo and twins. Other canonical entity
components remain excluded in that projection; no full Oil gameplay claim.
Public actual Oil preflight has advanced to company_outpost construction missing site.

## 8. Risks

storage-slot is an authored normalized measure, not physical density. Phase compatibility translation
uses current content identities. Native fixture handling/motion does not prove original Oil vehicle
profiles. All provider/port purity/stress/private/full Oil gates remain open.

## 9. Out of scope

Tycoon/business changes, renderer/Draw/adapters/platforms, instant stock writes in drivers, new
inventory/reservation/clock owner, full regression and Git writes.

## 10. Execution stages

1. Migrate all storage declarations and file-owned shared capacities/ports.
2. Validate whole authoring parity and actual-content admission boundary.
3. Compile and run only the new affected bounded case; fix its genuine failures if present.
4. Record evidence and tracker without closing original broad acceptance gates.
5. Continue native construction/mover/worker/handling declarations, financial consolidation and
   full Oil physical/private/durability acceptance under focused verification.
