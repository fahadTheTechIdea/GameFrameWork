# P7-B — Authored capacity policies

Current integration: [P10-A2c network rates](P10_NETWORK_RATE_DESIGN.md) uses the actual source and
destination capacity policies to combine exact transfer quanta. A one-unit/hour rate can earn a
two-unit receiver batch over two eligible hours; this does not relax native-unit admission or create
another capacity evaluator. Build2.126.0/save69;86 checks/two focused network suites, clean build.
Earlier capacity suites compile only, not rerun; broad capacity/stress/durability gates remain open.

Status: B2 implemented and B3 focused-verified on 2.29.0 / save 13 (122 checks, two exact suites).
B1's 2.28.0 / save 12 proof (105 checks) is historical, not rerun as a whole.
Endpoint-owned receipts are now C1 focused-verified; selected-carrier/tool atomic acquisition,
stress/allocation and stage regression gates remain open. See [C design](P7_OWNED_RESERVATIONS_DESIGN.md).
See [current evidence](../../docs/migration/P7_LIVE_CAPACITY_VERIFICATION.md).
This is a subdivision of P7, not a replacement roadmap or another inventory authority.

## 0. Problem statement

The audited `Standard/Inventory.cs` summed unlike quantities into `Total` and scalar `FreeSpace`.
Production compared `Bill.Total` with that space; networks summed pool quantities independently.
Generated construction buffers imposed unauthored numeric limits. B2 removes those paths together,
using P7-A resource measures and B1 policies on the existing owners, not another balance authority.

## 1. Goals

1. B1: resolve JSON-authored mass, volume, slot/count or combined policies through ContentTypes and
   ResolvedContent, with no extra registry, mutable balance, default limit or platform dependency.
2. B1: evaluate stock + inbound promises + a complete incoming vector using exact rational arithmetic.
   Each declared dimension must fit independently. Never infer density, truncate overflow or round
   an unrepresentable measurement into a smaller occupied amount.
3. B1: return detached per-limit stock/promised/incoming/projected facts and typed resource/unit
   refusals. Evaluation neither changes inputs nor reserves or transfers anything.
4. B2: replace existing scalar inventory/network APIs and all consumers together. Reuse each current
   provider's stock/promise state; remove scalar totals and hidden generated capacities, without aliases.
5. B3: prove manufacturing, upgrades, mixed cargo, promises and physical saved continuation against
   those migrated owners. B1 tests alone cannot close goals 4–5 or P7-C atomicity.

## 2. Design

`Core/Capacity.cs` owns the small public policy and result contracts. `Content/CapacityPolicies.cs`
owns authored policy validation and the built-in measured evaluator. An abstract CapacityPolicyDef
uses sealed Read/Resolve with extension property hooks, like ResourceDef. Its virtual Check permits
engine-side specialized policies; the built-in BasicCapacityPolicyDef uses the common implementation.
Providers must return deterministic, immutable results, validate the whole intention, and own no stock.

```csharp
CapacityCheck ICapacityPolicy.Check(
    IReadOnlyList<ResourceAmount> stock,
    IReadOnlyList<ResourceAmount> promised,
    IReadOnlyList<ResourceAmount> incoming,
    long instances = 1);
CapacityOffer ICapacityPolicy.Offer(
    IReadOnlyList<ResourceAmount> stock,
    IReadOnlyList<ResourceAmount> promised,
    ResourceId resource, Fix ceiling, long instances = 1);
```

The policy captures the existing resolved resource catalog, not a copied resource table. Inputs are
non-null nonnegative vectors in native resource units; duplicate resources are valid and additive.
Unknown resource indexes and fractional native discrete quantities refuse even when their amount is
zero. Every referenced resource must have an explicit capacity measure for every declared dimension.
Zero amounts validate identity/quantity but occupy nothing. No implicit zero-measure fallback exists.

For resource native quantity q, authored per-quantity measure m, source measure unit s and limit
unit t, measured raw occupancy is exactly:

`q.Raw * m.Raw * s.ScaleNumerator * t.ScaleDenominator /
 (Fix.One.Raw * s.ScaleDenominator * t.ScaleNumerator)`.

Use BigInteger intermediates, then require no remainder and a representable nonnegative Fix result.
Do not round each product or sum distinct dimensions. Validate discrete measurement/limit units too.
Validate all three vectors before selecting a limit-exceeded refusal; a malformed later entry must
not hide behind an earlier full tank. Results are stable in authored-unit ID order. Facts distinguish
stock, promises, incoming and projected occupancy; there is no dimensionless aggregate.

The master manifest lists policy documents normally. Core declares `capacity_policies` using
BasicCapacityPolicyDef; an extension can declare a derived policy section through existing ContentTypes.
Policies resolve once after units/resources and before other definitions, including entity components,
so later-declared derived policies are ready for consumers. Base indexes alias the same policy object.

```json
{"capacity_policies":[{"id":"cargo.combined","limits":{"unit.kg":10,"unit.litre":4}}]}
```

Limits must be explicit, nonempty, nonnegative, valid for their unit's discrete policy, and cannot repeat
a dimension through different units. An authored zero limit is a legitimate closed capacity, not an
implicit missing policy. No arbitrary engine cap is added; limits describe game-owned equipment.

## 3. API surface

ICapacityPolicy exposes stable Id, readonly CapacityLimit facts, Check and resource-specific Offer.
Positive instance counts scale authored equipment limits; invalid/overflowing counts refuse.
Offers select the greatest exact representable native quantity below a ceiling, not generic FreeSpace.
CapacityCheck has typed
status, failing Resource/Unit, and detached readonly CapacityUsage vector. Its default value refuses.
Null vectors are programmer errors (ArgumentNullException), not implicit empty inventories.
Unknown catalog indexes produce UnknownResource; unrelated extension exceptions are not swallowed.
PlayerView copies immutable policy/limit metadata without exposing executable provider contracts.

## 4. Files to change

| Action | File | Risk |
| --- | --- | --- |
| New | Core/Capacity.cs; Content/CapacityPolicies.cs | Medium: exact arithmetic and contract semantics |
| Modify | Content/ContentTypes.cs; Content/ResolvedContent.cs | Medium: explicit resolution phase |
| Modify | Hosting/PlayerView.cs | Low: detached authored metadata only |
| New | Tests and master-listed capacity proof JSON | Low: standalone engine-side extension proof |
| Implemented B2 | Standard inventory, networks, production, logistics, work, upgrades, generated storage | High: migrate together |

## 5. Error handling matrix

| Input | Result |
| --- | --- |
| Missing/unknown unit, empty limits, negative limit, repeated dimension, misspelt key | ContentException before Initialize, with owner/file/definition path |
| Unknown resource, negative amount, fractional native discrete amount | Typed check refusal, no mutation |
| Missing authored dimension measure | MissingMeasure with resource and limit unit |
| Unrepresentable measurement, fractional discrete measure or overflow | Typed check refusal, no rounding/clamp |
| Projected occupancy above any independent limit | LimitExceeded plus the complete calculated usage vector |
| Exact boundary or all-empty vectors | Fits, unless specialized provider rejects |

## 6. Backward compatibility

Development only. No old field loader, alias or compatibility wrapper is introduced. B1 adds no mutable
state payload, so its save layout remained 12. B2 removes scalar APIs/authored fields together and
uses build 2.29.0 / save 13; storage component/view schemas are 2. Old builds are rejected, not adapted.

## 7. Verification

Build the standalone solution. B1 ran two exact suites on its source. B2 runs only the two exact
Live capacity suites: measured stock/promises/exact offers/saved admission, then whole production/
physical build/hauling/network continuation. Their 51 + 71 checks pass. Do not run older families as
a broad substitute. Stress/allocation and full stage regression remain open.

## 8. Risks

Existing anonymous promises cannot distinguish a delivery's own reservation. B2 delivery releases
its recorded inbound promise before Receive; this is not an owner token or atomic guarantee. C must
replace anonymous acquisition/release in place. Endpoint/carrier exact quanta may differ and need a
common commitment quantity; one endpoint's Offer does not prove all participants can accept it.
ResourceId is a session-local compact index; callers must use the policy's bound content, not indexes
from a different content set. No cross-catalog provenance guarantee is claimed in this slice.
Capacity Check may allocate detached diagnostics; allocation budgets and fast-path optimization remain
later work and must preserve the same exact evaluator, not introduce a second decision algorithm.

## 9. Out of scope

B1's historical proof did not migrate live owners. B2 now migrates admission/normal network flow/
generated buffers. Topology redistribution/removal accounting, atomic carrier assignment, dock queues,
owned tokens, DAG, lots, fluid physics and oil are not completed by this slice.
No hosts, demos, Tycoon, renderers, Unity, Godot or adapters change.

## 10. Execution order

### B2 implementation contract

Remove IInletPort.Capacity/FreeSpace, IInventory.Total, StorageDef.Capacity, NetworkDef.CapacityPerTile
and Bill.Total. An endpoint exposes CapacityPolicy, InboundPromises, CheckIncoming(vector) and
Offer(resource, ceiling); an outlet also exposes OutboundPromises. Offer is a bounded quantity in the
resource's native unit, never generic space. Measured policies compute the greatest exact quantum
below that ceiling using rational coefficients and all limits; native/source/target discreteness and
Fix precision define that quantum. Offers may floor a ceiling, while exact incoming checks reject
fractional discrete intentions. A policy's optional positive instance count scales authored equipment
limits for network tiles without a second evaluator or fabricated capacity.

Storage remains the sole stock/promise owner. Optional JSON `phases` filters replace `fluid`; omitted
filters allow any explicitly measured compatible phase, not an inferred solid/liquid default.
Add/Receive include inbound promises, filtering and unit
validation; Remove/Take and TryConsume cannot steal outbound promises. ReserveIn checks the whole
admission; ReserveOut checks native units/availability. Releases reject mismatched quantities instead
of clamping; owner tokens still require C. Sync validates canonical lists and measured capacity.
Production checks its entire Bill vector; replacement checks retained stock/promises against the target
policy. Provider violations fault rather than silently lose transferred/output quantities.

Authored storage/independent-port fields become `capacity_policy` references. Construction declares
`site_capacity` and a required construction_settings definition with `salvage_capacity`; line definitions
declare `work_capacity` and get a marker definition per line. Networks declare `capacity_policy` per
tile. Generators reuse these references, not hidden numeric limits. Oversized line bills refuse during
the same preview/command check. Salvage splits physically into policy-sized piles, never clamps away
remainders; an incompatible/closed salvage policy faults explicitly rather than erasing resources.
Network redistribution/removal accounting and spill behavior retain their P10 requirements; this slice
changes normal pool admission, not those entire topology semantics.

All standalone consumers/fixtures migrate in place. New suites cover mixed measured storage with
promises/admission/consumption/save and bounded production/hauling/construction/line saved continuation.
Only those two exact named suites run. Build/save and changed codec identities reject old source.

- [x] B1a: implement authored common contracts/evaluator/registration and explicit resolve phase.
- [x] B1b: prove ordinary external provider/content, immutable metadata and saved content identity.
- [x] B1c: build, run two exact suites, publish source-specific evidence and update the master.
- [x] B2: migrate all live capacity owners/callers and generated authored buffer policies in place.
- [x] B3 focused proof: whole-bill admission, replacement, promises and bounded physical saved workflows.
- [ ] B3 stress/allocation and authorized stage regression gate; focused proof does not close these.
- [x] P7-C1: endpoint-owned receipts and physical consume/release (separate following responsibility).
- [ ] P7-C2/C3: atomic selected-carrier/tool acquisition and provider protocol.
