# P3-S5g exact spatial resource depletion — scoped verification

Date: 2026-10-06. Current dirty-workspace source, not clean HEAD/full-regression proof.
Build **beep-simengine-2.116.0**, save **65**, native map **5**, public point space **3**.
Actual sparse/sample payloads and existing view/work owners are unchanged.
[Stage](../../plans/enhancements/P3_LAYER_DEPLETION_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Authoritative owners and public contract

Optional ISpatialResourceQuantity<TLocation> extends the existing actual layer contract. Its
ReadQuantity returns existing UnitQuantity with the linked resource's actual native unit. Strict
bounds/unsupported/corrupt reads never fabricate empty stock. CheckWithdrawal is a pure current
admission; Withdraw rechecks actual stock, not a retained result. SpatialResourceWithdrawal is
detached typed facts, never a promise, receipt, stock store or permission to overspend.

SpatialResourceValues remains the sole material calculator. Explicit quantity metadata is required;
associated/unlinked signed measurements refuse. Existing UnitConversion alone performs exact forward
and inverse rational conversion. Positive native amounts must satisfy native quantum; the remaining
stored quantity must satisfy authored quantum and actual byte/int/fixed shape. No rounding, density,
default resource or provider-local arithmetic formula is introduced. Corrupt live material faults
rather than turning into zero availability.

Native MapLayer<T> derives facts from its actual typed indexer and commits through existing Set.
Sparse allocation/removal, cursor preflight and Sync remain in that same owner. Native subclasses add
exact validated Fix-to-stored conversion, not balances/codecs. Independent PointLayer implements the
public contract without friend access, delegating actual geometry/write/invalidation to its existing
PointSpace. PointSpace uses the same bounds predicate for writes and withdrawal; unregistered/foreign
layer instances cannot claim another owner's samples. No additional provider/sample registry exists.

Existing master-listed JSON supplies resource, unit, value-type, visibility and geometry metadata.
The new suites reuse the existing factory/fixture instead of introducing another catalog. Continuous
byte/int test inputs are authored explicit mutations of that fixture, not runtime gameplay defaults.
Framework tests instrument actual stores/cursors only for fault injection; they do not implement
extraction, conversion, scheduler or conservation rules.

## Final commands and results

Working directory: C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine.

~~~powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Spatial depletion: exact native admission and atomic stored remainder"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Spatial depletion: independent geometry, private facts and saved continuation"
~~~

- Final build: **0 warnings, 0 errors**, **3.13 seconds**.
- Exact native admission/atomic remainder: **213 passed, 0 failed**, **103 ms**.
- Independent geometry/private facts/saved continuation: **61 passed, 0 failed**, **243 ms**.
- Total **274 checks, 0 failures, 0 aborted suites**, exactly two distinct complete suite names.
- Initial admission run:186 passed/1 failed. A test incorrectly assumed Fix could represent a whole
  value beyond int.MaxValue. That assertion was corrected to the actual largest whole fixed-point
  value; an upper-range fractional check and real int.MaxValue stock debit now prove shape/narrowing.
  No engine range or artificial world cap was changed to satisfy that test.
- Repair/expansion/final reruns use the same two names only.49 preceding build assertion files
  mechanically migrate2.115→2.116 and compile only; their suites were not reverified.
- No preceding layer/dock/third/family/full/replay/lockstep/stress/soak/Oil/Tycoon gate was run.

## Proven scope

1. Native byte/int/fixed, half-counts, discrete pairs, scaled mass and tiny thirds derive exact native
   stock and unit identity from actual cells. Pure reads/admission leave hash/cursors unchanged.
2. Nonpositive/discrete-invalid/too-large/empty-cell/invalid-location withdrawals refuse without
   value, sparse allocation or layer/map/private cursor changes. Default results cannot report success.
3. Stored pair quantum, inverse rational remainder and continuous byte/int shape remain mandatory.
   A fractional remainder is never rounded or truncated. The actual largest whole int quantity
   depletes by one without narrowing; invalid existing shape is corrupt state.
4. A retained ready result cannot spend twice: fresh withdrawal reads current stock and refuses a
   stale amount. This is not a reservation or multi-endpoint commit guarantee.
5. Native successful writes touch existing cursors once; private depletion does not invalidate other
   companies. Final depletion removes empty chunks through the same sparse owner/Sync.
6. Instrumented actual map/point invalidation exhaustion refuses before debit. Durable material/hash
   remains unchanged. Scoped atomicity does not prove arbitrary provider/global callback rollback.
7. An actual int.MaxValue-wide authored map depletes its sparse far-edge cell and saves/loads without
   dense enumeration or platform cap. No full-area generation/performance claim is made.
8. Public independent non-tile fractional coordinates use exact half-unit native material. Owner
   views report actual stored-unit remainders; foreign views clear/refuse and keep private cursors.
9. Instrumented corrupt native/public cells cannot be read as material, admitted or spent. Restoring
   their actual valid values permits checkpoints again; no fallback or fabricated stock was used.
10. Native and public partial/depleted states roundtrip through existing Sync and independently
    continue withdrawals/Update with matching hashes. Foreign unregistered public layers refuse.

## Explicitly open

This completes engine-side **quantity/depletion primitives**, not player extraction. No inventory
receives anything from a bare withdrawal. Next must bind actual cells to existing IResourceOutlet/
IResourceInlet endpoints, embed the existing owned reservation book with one responsible owner,
protect actual cell/ownership mutations and validate unique bindings/checkpoints. Real positive-time
activity work must invoke existing transaction movement from deposit to inventory with source
receipt provenance. Produced/Initialized grants, copied stock, parallel extraction queues and
instant player commands are not substitutes.

Timed extraction, receipt durability/orphans, whole-workflow conservation, replenishment/hydrology,
reservoir science/3D, arbitrary provider/global purity/rollback, replay/lockstep/stress/allocation/
full-framework acceptance remain open. No Oil/Tycoon/host/demo/platform/Draw changes were made.
The lifecycle skill kept source, stage, tracker and honest scoped evidence aligned.

## Verified source fingerprints

SHA-256 of the final source used by the two named gates; preceding identity-only files compile but
are not individually claimed as behavioral proof.

| File | SHA-256 |
|---|---|
| `src/Beep.SimEngine/Kernel/SpatialResourceValues.cs` | `4836345B2A57571914ACF985B7FEE81BAA85D8064F0BC0C3C93F96CF15E4234B` |
| `src/Beep.SimEngine/Core/Units.cs` | `CCDF986CB531617BBE0D2695391D5CC5CEA9FCE5BDA82AD51A64ACD405872880` |
| `src/Beep.SimEngine/Map/TileMap.cs` | `40AB453B52BF9A20F710733D51BBBAA781AA2DB68CFE0ACBAA9E75B383E84897` |
| `src/Beep.SimEngine/Kernel/SaveGame.cs` | `C5367A066CE3AE37C8BE5847A35FD8E1BEE4B8634AF0A03496C45E127D4C4C1C` |
| `tests/ExtensionProof/PointLayers.cs` | `428590832F83E9120E18F198EFDAA9CF52494120168DBBF703DDE028F254BA5F` |
| `tests/ExtensionProof/PointSpaceProof.cs` | `A57EE871412CED3F42A941277A753A18C758823E17F592D4C63F085480D2E5DA` |
| `tests/Beep.SimEngine.Tests/SpatialResourceValueTests.cs` | `41D21CDAD88F8652B6797C71C1AF5D7F7FDFECDB1B3CCF034B71DEED8D43FC46` |
| `tests/Beep.SimEngine.Tests/SpatialResourceDepletionTests.cs` | `8B14F9E702DEB71AF3D8D2808406ED4650FEAF8CBE1788EDDF2451E06A45A6A1` |
| `tests/Beep.SimEngine.Tests/Program.cs` | `119EC2976FB92738D68D3650C7A5E326B7B9F33617185A0B84BBCFE4384CEA29` |
| `tests/ExtensionProof/content/spatial-resource-values.json` | `0DF84669FC0259194F718D7FA0722E8580DC2B4055495CE67F4A717FB0B4E18E` |

