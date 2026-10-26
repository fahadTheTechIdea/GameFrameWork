# P3-S5f legal spatial resource cells — scoped verification

Date: 2026-10-06. Current dirty-workspace source; not clean HEAD or full-regression proof.
Build **beep-simengine-2.115.0**, save **65**, native map **5**, public point space **3**.
Native byte/int/fixed and public samples retain existing wire payloads; view/work owners unchanged.
[Stage](../../plans/enhancements/P3_LAYER_RESOURCE_VALUES_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Shared contracts, not another resource authority

SpatialResourceValues.CheckValue/ConvertQuantity uses ISpatialLayer, IUnitDefinition,
IResourceDefinition and the unchanged UnitConversion owner. Only explicit
resource_measure:quantity denotes material availability. Associated or unlinked channels retain signed
semantics; ConvertQuantity refuses them with InvalidUnit rather than infer native material. Quantity
values must be nonnegative, satisfy both stored/native discrete units, convert without a raw remainder
and fit native Fix range. Exact zero remains real empty spatial availability, never inventory creation.

MapLayer<T> remains the sole native sparse value/generation/checkpoint owner. Byte/int/fixed subclasses
supply exact scalar-to-Fix representation only. Set validates before sparse allocation, value mutation
or invalidation. Fill validates the complete transient generation before replacing live values.
Strict indexers and both Sync directions reject malformed live/serialized material cells. Existing
candidate restore retains prior authoritative storage after a late invalid chunk. No duplicate balance,
policy registry, provider-local conversion formula or zero fallback was introduced.

Independent public-contract PointLayer replaces its former local quantity conversion check with that
same helper. It validates actual writes, reads and both checkpoint directions. Restore stages owner
and all sample records before replacing its existing dictionary. Failure retains original owner and
values. Public PointSpace returns actual same-value no-ops before invalidation preflight; changed values
still preflight existing counters before sample commit. This adds no counter or sample authority.

Native/public metadata retains exact immutable catalog aliases. JSON remains explicit and master-listed.
The new layer fixture authors byte/int/fixed deposits, whole/fractional/scaled material units and signed
associated/nonresource channels. Point proof layers author a half-count unit while the existing supply
resource remains discrete. Providers own topology, visibility and Sync; hosts implement none of it.

This stage fixes native legality and unifies the point provider's already-existing admission check.
It does not claim that the old point writer accepted negatives. The missing point guarantees were
live read/write-checkpoint validation, staged owner/sample restore and exhausted-counter no-op behavior.

## Final commands and results

Working directory: C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Spatial resource values: catalog legality and atomic native generation"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Spatial resource values: provider checkpoints, private queries and saved continuation"
```

- Final build: **0 warnings,0 errors**, **4.55 seconds**.
- Catalog legality/atomic native generation: **108 passed,0 failed**, **126 ms**.
- Provider checkpoints/private queries/saved continuation: **84 passed,0 failed**, **286 ms**.
- Total **192 checks,0 failures,0 aborted suites**, exactly two distinct complete suite names.
- Initial test compile repairs used actual JSON indexing/catalog methods, actual point-space definition
  access and explicit byte encodings. A no-build call after an unsuccessful build matched zero new
  suites, not a passing gate. The first executed checkpoint suite stopped because its point request
  omitted the second human seat; the fixture now selects both seats explicitly.
- Same-name repair/expansion/final reruns cover only these two names.48 prior build expectation files
  mechanically migrate2.114→2.115 and compile only; their workflows are not reverified.
- No preceding dock/layer/family/third/full/soak/replay/lockstep/OilFieldDays/Tycoon gate was run.

## Proven behavior

- Shared rules distinguish InvalidAmount, FractionalDiscrete, Unrepresentable and OutOfRange without
  rounding, clamping or inferred resources. Stored discrete pair units remain mandatory even when the
  corresponding native result could be whole. Two half-units equal one native part; scaled mass and
  tiny representable thirds remain exact.
- Actual byte/int/fixed writes refuse illegal values both in existing and unallocated chunks. Original
  values/hash/layer/owner/foreign cursors remain unchanged. Late invalid Fill refuses the whole staged
  generation. Valid generation publishes the same sparse store, replaces prior cells and removes empty
  chunks through real zero writes. Six native quantity layers exercise distinct unit/range constraints.
- Associated below-datum elevation and unlinked int.MinValue channels remain legal signed data.
  Associated/unlinked conversion refuses rather than interpret that data as material stock.
- An actual int.MaxValue-wide authored space stores a legal sparse quantity at its far edge. Refused
  native quantum and exact save/load preserve that geometry/value without a new map cap. No full-area
  generation/performance claim is made for that wide space.
- Late corrupted native byte/int/fixed/discrete/overflow/inexact chunk records refuse staged restore
  while both prior chunks remain unchanged. Instrumented malformed live quantities cannot read, encode
  or hash. Restoring the instrumented valid value resumes the same authority.
- Authorized private native queries expose actual stored-unit values; foreign queries return Unavailable
  and clear output. Exact material scaling does not silently alter host query units.
- Public point previews and actual commands share negative/native-quantum refusal and retain owner/
  foreign cursors. Ordinary engine commands create legal two-half samples. A candidate owner transfer
  plus late invalid native quantity refuses restore without committing either owner or samples.
- Public malformed live material cannot query/save/hash. Existing private visibility clears refused
  outputs. A same-value write succeeds at exhausted observer counter without any state change; a real
  changed quantity refuses before sample mutation.
- Native and public provider sessions load identical checkpoints and advance eight interleaved Update
  steps with identical hashes. No host timer, extraction rule, stock grant or alternate value store exists.

## Explicitly open

Physical extraction/depletion/owned receipts and provenance, hydrology/3D/generation science, arbitrary
provider/global purity/rollback, broad replay/lockstep/stress/allocation/full framework acceptance remain
open. Generation callback side effects on unrelated layers are not globally rolled back. No Oil/Tycoon/
host/demo/platform/Draw changes. Focused legality is not the full simulation-game plan's completion.

The lifecycle skill kept implementation, stage, single tracker and scoped evidence aligned.

## SHA-256 source fingerprints

Captured after final source build and both exact suites. Only selected source/fixture boundaries
are fingerprinted; Units.cs is the unchanged existing conversion owner, not a new implementation.

| File | SHA-256 |
|---|---|
| src/Beep.SimEngine/Kernel/SpatialResourceValues.cs | DC98D4A7B2CC8AD8A9CF3BE91A5D84ABDAEB74750E5FE15F7080CAC91CDD3F5D |
| src/Beep.SimEngine/Core/Units.cs | CCDF986CB531617BBE0D2695391D5CC5CEA9FCE5BDA82AD51A64ACD405872880 |
| src/Beep.SimEngine/Map/TileMap.cs | 0D5B03E73E9F24DDA9D3FD935F58785E0B2ACE21759FA1CF3397387FE1E2DCB3 |
| src/Beep.SimEngine/Kernel/SaveGame.cs | 7D9F09750EE7C99419A8B4D867E2E939005978B8B2C546732DC21D129DE4091D |
| tests/ExtensionProof/PointLayers.cs | 773822E334F9E9CF038F9151A83ADE60766C2614572A9B263F3EECB43C8F81AA |
| tests/ExtensionProof/PointSpaceProof.cs | A57EE871412CED3F42A941277A753A18C758823E17F592D4C63F085480D2E5DA |
| tests/ExtensionProof/ExtensionProof.csproj | 46614E8AF6FC90F3FA0D61BB297144EF7A494245A368474147B4627D017C9746 |
| tests/ExtensionProof/content/spatial-resource-values.json | 0DF84669FC0259194F718D7FA0722E8580DC2B4055495CE67F4A717FB0B4E18E |
| tests/Beep.SimEngine.Tests/SpatialResourceValueTests.cs | 6510EE1903A36057859000A5C2366F71B4352907FF2E8A10FBCC3F45ADA51B5C |
| tests/Beep.SimEngine.Tests/Program.cs | 119EC2976FB92738D68D3650C7A5E326B7B9F33617185A0B84BBCFE4384CEA29 |

