# P3-S5h focused verification — receipt-protected timed spatial extraction

Date: 2026-10-06. Workspace: `C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine`.
Runtime: **beep-simengine-2.117.0 / save66 / native map6 / public point space4**.
Existing WorkBoard23/work system10/work view37 remain the execution owners.

## Outcome and evidence boundary

The bounded P3-S5h slice is implemented and focused-verified. This is not full P3 or P1–P12
completion. No prior, family, third, full-regression, platform or Oil/Tycoon gate was run.
54 preceding test assertion files migrated current build/save identities and compiled only;
native/public map schema assertions migrated in the existing resource value/depletion suites.
Those older suites were not executed and their historical pass counts are not current proof.

Final build: `dotnet build Beep.SimEngine.slnx --no-restore`: success, **0 warnings / 0 errors**,
2.52 seconds reported by MSBuild.

Exactly these complete names selected, each returning one suite and exit code 0:

| Exact suite name | Passed | Failed | Suite duration |
|---|---:|---:|---:|
| Spatial extraction: authored ownership, receipts and actual transfer provenance | 109 | 0 | 706 ms |
| Spatial extraction: timed work, blockers and saved physical continuation | 86 | 0 | 768 ms |
| Total | 195 | 0 | two suites |

Invocation for each: `dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "<exact suite name>"`.
Earlier attempts of these same two names exposed a reference-order defect and incorrect test
assumptions (same-factory sessions share immutable content; duplicate active activity is refused).
They were repaired before the final successful build/runs. A preliminary compile error was fixed
before executing tests; no stale binary was used after a failed build.

## Actual implementation owners

- `ISpatialResourceQuantity<TLocation>` binds actual cells through the selected provider.
  `ISpatialResourceCellGuard` defines pure admission, mutation, ownership and staged-book hooks.
- `SpatialResourceCellBindings` is the one shared per-layer binding/callback/codec owner. It holds
  aliases, not material balances or a second reservation book, and leaves geometry to the provider.
- `SpatialResourceEndpoint<TLocation>` reads stock from actual `ReadQuantity`, owns one existing
  `ResourceReservationBook` and persistent endpoint version, and debits through actual `Withdraw`.
  Receipt commitment state is transient and cannot checkpoint. Outlets also expose an inlet;
  finite deposits explicitly reject incoming material rather than invent replenishment.
- Native `MapLayer<T>` and public point layers protect actual writes/generation/owner changes
  against promises and exhausted endpoint counters before committing values. Their existing Sync
  stages books against candidate stock. No persistent stock exists outside the layer values.
- Master-listed JSON declares stable cell identity, actual provider geometry, resource link,
  capacity/access, extraction bill and activity. The endpoint provider is a pure alias index.
- `IResolvedContentValidator` runs once after all definitions resolve references and before
  publication. Linked metadata validation does not require special spatial definition ordering
  or a repeated Resolve. The independent point extension declares cells after effects.
- `SpatialExtractionEffectDef` inherits existing positive-physical-work validation. Existing
  WorkBoard/WorkSystem/jobs/mobility own real travel, earned work, retry/blockers and cancellation.
  Existing `IResourceTransaction` commits deposit-to-inventory movement; existing journal emits
  `StockTransfer` with actual scoped source receipts, not `Produced` or `Initialized` output.
- Public `ExtensionProof` adds point geometry and JSON definitions through contracts only;
  no private engine access, provider-specific scheduler, host timer or copied resource store.

## Focused proof

Admission covers invalid cell identities/bounds/access, nonquantity/missing layers, duplicate
identities/geometry, wrong native resource bills, zero work and timer-only extraction; all refuse
before tick one. Public linked validation also refuses incorrect bills when cells resolve later.

Native/public endpoints prove actual native stock, rejecting inlets, pure availability previews,
owned and foreign receipts, protected ordinary debit/write/owner change, protected native Fill,
partial movement, remaining receipt release and double-spend refusal. Foreign companies cannot
claim private stock or inspect another company's extraction history. Foreign catalogs cannot
alias real bindings; same-factory saved sessions legitimately share immutable definitions while
retaining independent actual books/stock. Invalid staged resource books do not publish. Exhausted
persistent endpoint versions refuse before material/book/cursor mutation and roundtrip exactly.

Ordinary player PostOrder/preview drives positive machine travel and work on native and independent
point providers. Partial work does not grant or consume material; direct premature effect calls fail.
Saved work continues identically. Completion transfers exact native quantity, conserves source/dest
deltas, journals actual source receipt/version provenance, releases machine and leaves no source
receipt. Private work views reject foreign readers. Completed extracted stock remains durable.

Native completed work waits on real inbound destination promises without consuming deposit stock,
creating prefix source claims or earning duplicate work. Saved twins retry after actual capacity
release and converge. Two distinct authored activities compete for one finite deposit; duplicate
active plan admission remains refused. The losing earned operation reports NoStock, can be cancelled
through an ordinary command, and leaves no source receipt. All physical waits have a 480-update bound
with tick/order/node/blocker diagnostics; no unbounded acceptance loop was added.

## Native breaking identities

Save66/native map6/public point space4 contain the embedded actual cell binding identity, persistent
endpoint version and existing reservation book after actual layer values. Older readers are not
retained. No compatibility shim, synthesized default stock or parallel checkpoint path exists.

## Remaining work and limits

This proves the named native/public extraction paths, not arbitrary malicious providers or global
rollback. Future-stock reservations during earlier work are not supplied: source claims occur at
the existing final transfer boundary, so competition may legitimately block completion.

Hydrology/replenishment, reservoir science, 3D and other transport/content modules remain deferred.
Broad replay/lockstep/shuffled arrival/fault-injection/fuzz/soak/allocation and full-roadmap acceptance
were not run. No Oil/Tycoon/client/demo/platform/Draw implementation or artificial world-size cap
was added. Scoped mutation refusal is not universal transactional rollback.

## Final verified source fingerprints

These SHA-256 fingerprints were captured after final build/tests, before documentation-only updates.
They identify working files, not a clean committed HEAD.

| File | SHA-256 |
|---|---|
| src/Beep.SimEngine/Content/Defs.cs | 3A541D78E9A375DEACD6CE6C0026BB15E9E4C3C94B3FF6990284747BA8D2C396 |
| src/Beep.SimEngine/Content/ResolvedContent.cs | B31379433DE2CF80A9654A3E2787F57594EBBA9F8483D45A3636C095A4A9C01E |
| src/Beep.SimEngine/Kernel/SpatialResourceCells.cs | B2B2E0D66043A57BA17962EA71C2B96DCCBEFBC6A3A3046C2ED88DD78BCE8E27 |
| src/Beep.SimEngine/Kernel/SpatialResourceValues.cs | 852AF21552568C01A99FB26811BB9D938F6B276AB74708B6212D82055D693C7C |
| src/Beep.SimEngine/Standard/SpatialResources.cs | E533B9D834EDBC538A8B266F64394578FACB7457A8CA8F608B4701E0AF0798AE |
| src/Beep.SimEngine/Standard/SpatialResourcesModule.cs | F4B2ECF71047C73E2370F92AD551344D5B33619A9A4DD0A80960C11DE895FE6E |
| src/Beep.SimEngine/Map/TileMap.cs | 1B7140F6E6FCEDE87FDA6D4462669344F0D332264CBA842EC9E5FFA782650620 |
| src/Beep.SimEngine/Kernel/SaveGame.cs | 1A7722019918D6F95E858AEB0C70DDBFE9347FC5D4AF27D73A1E44B09C7B0E16 |
| tests/ExtensionProof/PointLayers.cs | EB2D746FDDEC56E28EE6CC10D91AB84635CF87D0374BA8B20F12613389F7667D |
| tests/ExtensionProof/PointSpaceProof.cs | 9460880C6BEDA474372EAA7F72ABD76645988FA70649D63DD9F25A3C7DC1ABCA |
| tests/ExtensionProof/SpatialResourcesProof.cs | D7361F462ABC28586629FE66DB4ABD2A3B342AF8540D5CA9918F081E5C6E09C4 |
| tests/ExtensionProof/ColonyGame.cs | BAD1E8FA16BE7304F2125374DD3AE781FE70E85788892922CC0111250D7951F2 |
| tests/ExtensionProof/content/spatial-extraction.json | 6EC671BFB7DB6E5ACDD705BC489D0031953A270161D9A23D7F5C3C5A5EDC468D |
| tests/ExtensionProof/content/point-spatial-extraction.json | C891D3D91682D38FE1C0893E11AD51D4BBFC8EFD77646D82F5A4D866F95D9EBA |
| tests/ExtensionProof/ExtensionProof.csproj | D1D34FC0DC7F115734AE71207F4FEEB643A58A7D0EBC63E625AB3AD043B37C32 |
| tests/Beep.SimEngine.Tests/Program.cs | A201FCA00044BDFE06CED303793BDF2C0AE3F7F643AECA7CCC7E663E0CDCC59F |
| tests/Beep.SimEngine.Tests/SpatialExtractionTests.cs | 3586522B6044449212B08F9439104DBAF15849847B3CA439056C26072AC3E3A0 |

