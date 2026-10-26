# P4-G62 — spatial layer content verification

## Current M79: spatial opening accounting and owned resource admission — 2026-10-22

Native **2.725.0/save161/replay2**, master HEAD
`cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty/untracked work.
Original221/210 source-recorded checked/11open/full goal active; no original requirement closes.

### Existing owners and the reproduced defect

The original SpatialLayerContentTests.Physical now checks every published catalog resource across all owned
entity inventories/cargo plus the actual finite deposit, on each primary and saved-twin Update.
Initial layer quantities remain opening material, not inventory initialization or production facts.
All lifecycle totals remain at their actual opening values; extraction adds exactly one two-unit
StockTransfer from the authored scoped cell to the receiving entity. Saved pending/productive/final
states do not grant resources again. Actual classification3/control7/lake-level1.5 remain unchanged:
the lake's Associated resource link is descriptive, never inferred water volume. No new JSON, stock
store, accounting service, endpoint, game loop or production rule is introduced.

The strengthened existing literal also exposed an unrelated-to-test-assertions admission defect:
its original unused `resource-foreign` negative callback returns an unread BasicResourceDef. The
layer validator dereferenced Resource before proving catalog ownership, causing NullReferenceException
instead of its required ContentException. Initial run1901/1 preserves this reproduction. The first
repair checked ownership earlier, revealing the second cause: ResolvedContent.Owns passed a null
unread Def.Id to its dictionary, producing ArgumentNullException; second run1901/1 is retained.

The same original owners now handle this correctly. SpatialLayerDeclaration requires the actual
owned IResourceContentDefinition/catalog alias before reading its compact resource identity.
ResolvedContent.Owns returns false for an unread null-ID definition. It still requires reference
identity; equal names or independently allocated definitions never acquire ownership. The existing
ContentOwnershipTests collection case adds absent/unread resource/unread layer refusal plus positive
actual-owned-resource checks. Invalid unused layers still refuse before Initialize; the original
throwing metadata callback still propagates its own exception. No native subclass requirement,
compatibility shim, fabricated resource or blanket exception translation was added.

Only Content/SpatialSpaceDef.cs, Content/ResolvedContent.cs and EngineInfo.Build change among450
native sources versus M78. Native identity advances2.724→2.725; save161/replay2 codecs and authored
timings/quantities are unchanged. Intermediate2.725 build2 is not relabelled the final build3 image.

### Exact final evidence and retained attempts

Artifacts: [M79](../../artifacts/optional-module-durability/4964e219664e44f89c62389bf00e1db5/),
[all attempts](../../artifacts/optional-module-durability/4964e219664e44f89c62389bf00e1db5/attempts.json),
[final tested inputs](../../artifacts/optional-module-durability/4964e219664e44f89c62389bf00e1db5/build-3-inputs.json),
[compiled audit](../../artifacts/optional-module-durability/4964e219664e44f89c62389bf00e1db5/compiled-caller-audit.log),
[original gates](../../artifacts/optional-module-durability/4964e219664e44f89c62389bf00e1db5/original-gates-current.json),
[frozen inputs](../../artifacts/optional-module-durability/4964e219664e44f89c62389bf00e1db5/final-inputs.json),
[structural checks](../../artifacts/optional-module-durability/4964e219664e44f89c62389bf00e1db5/structural-checks.json).

Three targeted builds succeeded0warnings/0errors:11.67/10.87/6.96seconds. All three source/binary
manifests are captured. The raw two failing spatial logs and intermediate values108/0 remain.

| Final exact literal, all build3 | Passed/failed | Suite/observer ms | PID/exit |
| --- | --- | --- | --- |
| Spatial layer content: direct and native layered metadata drives physical tile and point extraction | 1902/0 | 3555/3696 | 21880/0 |
| Content ownership: work plans, transformations and ports expose frozen collections | 52/0 | 283/423 | 27100/0 |
| Spatial resource values: catalog legality and atomic native generation | 108/0 | 118/252 | 15892/0 |

Final **2062/0 in three exact cases**, not a broad regression. The first has four actual native/direct
tile/point physical contexts and36 negative pre-Initialize metadata refusals. Physical assertions use
authored inputs/public views/ordinary player commands, Update and public pause/resume, not low-level stock/progress/provider writes;
metadata mutation attempts are explicitly negative immutability probes. The two other cases include
low-level ownership/value protocol probes, not player workflows. The values case also retains its
existing int-max sparse extent, byte/int/fixed conversion, signed Associated/non-resource samples
and refusal/no-write checks. No platform map-size cap is added or inferred.

The same recorded primary survives every save; only the twin is reloaded. Every replay now requires
one actual command, every committed Update checkpoint and the **final live hash**, not Success-only
prefix verification. Four final complete replays:

| Context in actual execution order | Steps/checkpoints | Final live = replay hash |
| --- | --- | --- |
| Direct metadata, native tile geometry | 0→20 /20 | A671E7F33EF3C7AF |
| Direct metadata, independent point geometry | 0→24 /24 | F6028C09752FC957 |
| Native metadata, native tile geometry | 0→20 /20 | 82DF1343F601C16F |
| Native metadata, independent point geometry | 0→24 /24 | F6028C09752FC957 |

Equal point hashes represent equal actual saved world state, not proof that the metadata definitions
have the same CLR implementation; the original factory's explicit native/direct JSON selection is
the content evidence. The diagnostic `metadata-view` field names the detached snapshot, not the
authored definition class. There is no new variant driver or resource ledger.

The read-only compiled audit matches450 native sources/PDB checksums,2936types,12923IL methods,
344955instructions,137typed resource/spatial calls,14resource primitive writes/eight selected spatial
layer stores and zero unknown/unmapped/indirect sites. This is structural call coverage, not runtime
execution of all callers or a global material certificate. Final native DLL SHA256:
`8D7A7E5AC25C14BFE8058BC04B6254CA5BF7164E5C7F777543C0CDD300B2683E`.
Actual commands are retained in attempts.json: one targeted test-project Release build per image;
each runtime uses its exact literal with60000ms bound/5000ms termination/30000-character output.

### Self-review and remaining obligations

Self-review only. Catalog admission reports ContentException through its existing loader boundary;
runtime World first-fault, typed results, flow journal and notifications are unchanged. No swallowed
exception, new logger or channel. Broader protected audit/correlation/retention/channel-outage work
remains with [the existing assurance matrix](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage),
not a claim from these local cases. UI/Draw/platform adapters are out of scope for this headless fix.

Finite opening-versus-metadata semantics now have direct physical/accounting proof for these authored
contexts. Broader provider/quality-lot/shared-endpoint/lockstep/fault/stress/P8/P10/P11 gates remain
open, as do the original consumer and forbidden full-profile gates. Next review the remaining seven
optional financial resource call sites without changing business rules. No consumer/Oil/marine run,
Tycoon source/rule edit, dependency, allocation/soak/full/default/profile/family test or publication.
Earlier runtime records below retain their actual older images, not automatic2.725 certification.

## Earlier implemented ownership change — 2.421.0

Core2.421.0/save154/replay1; actual map/provider value fields and all codecs unchanged. ISpatialLayerDefinition now declares read-time SpaceId and resolved space/type/value/access/unit/resource/vertical/immutable metadata. SpatialLayerDeclaration is the sole reader/resolver/admission rule; native SpatialLayerDef is ordinary JSON registration only. Independent Def metadata uses the same declaration without inheriting the native class.

ResolvedContent resolves units/resources/capacity first, then every layer alias once and validates it before dependent cell/line/condition/visibility/startup resolution. Invalid unused aliases cannot hide until a scenario requests them. Actual catalog identity/ownership, selected space/unit aliases, enum ranges, native quantity dimension/resource identity and frozen object metadata are required. Native layer authoring’s optional resource/vertical fields retain their original semantics in this one reader; hosts do not fabricate them. Protected G19 resource-catalog return migration is unchanged.

All production consumer boundaries now use the interface: native tile composition/MapLayer/value layers, SpatialLayer base/provider registration, World selected-provider completeness, layer/sample visibility, knowledge/freshness/region rules, readings, sensors/occlusion, tile layer conditions, line definitions/generation and resource cells. SensorVisibilityDef’s generic bound is now the existing IContentDefinition contract, not Def. Original point geometry/layer/startup and direct metadata callers migrated in place; their gameplay/persistence owners did not change. No new map/layer registry, material balance, receipt book, scheduler, visibility state, clock or compatibility overload. Remaining concrete mentions are normal content registration/native definition and explicit historical native-owner assertions/negative foreign alias.

The lifecycle skill guided staged migration and existing-owner reuse. The new non-friend positive definition only delegates metadata; negative definitions deliberately return invalid fields. The new driver edits authored JSON inputs, submits normal player commands and reads detached public facts. It does not write stock, map values, progress or world state. It uses original Colony/PlanetX factory owners, not a replacement factory. Older registered cases compiled only and were not invoked.

## Requirement-matched proof

ONE NEW exact case: `Spatial layer content: direct and native layered metadata drives physical tile and point extraction`.

Four positive scenarios: native tile, direct tile, native point and direct point. Direct tile moves all original colony layers plus the finite deposit into the master-listed independent section. Original byte water/topology/lane and integer ownership layers still resolve selected native conditions, line generators and provider admission. Authored added byte classification3, integer control7 and fixed lake-level1.5 use original WorldSetup; the lake is associated with water and explicit vertical domain/unit/reference/offset, never counted as native water volume. Typed mismatched queries refuse and clear output. Native/direct point providers retain their original fixed quantity owner.

Tile startup stores12 half-native units (6 native Steel), point startup12 native ore. Empty receiving entities get two native units only after ordinary staffed travel/productive work. Every Update conserves actual finite deposit + receiving inventory; pending input cannot instantly extract. Saved pending/productive/terminal checkpoints and twin updates are hash-identical; pause earns no yield, completed restore grants no opening stock again. Original resource/vertical aliases and frozen metadata survive each registration/provider. Retained metadata/collections reject mutation, foreign private sample/metadata reads disclose no prefix, ordinary command stream replays and independent sessions retain separate values/books/work.

Thirty-six pre-Initialize refusals: missing space/type/value_type/visibility/unit; unknown key; missing space/unit/resource/vertical-unit references; wrong native quantity dimension; invalid resource interpretation; absent vertical domain; invalid unused layer; ambiguous mixed native/direct deposit alias; and21 unused direct provider modes covering wrong identity/owner/space identity/null space/no type/zero or invalid value and visibility/null unit/null or foreign resource/no or invalid resource measure/inconsistent vertical domain/null vertical unit/blank reference/null, mutable or array metadata/thrown metadata callback. Strict reader and actual alias admission run through the complete master before Initialize. Callback exception type remains visible; no arbitrary mutable CLR provider sandboxing is claimed.

## Actual builds and runtime

1. Solution build: exit1,2.74s,0 warnings/1 error, CS0311. SensorVisibilityDef still required Def; its generic consumer boundary was migrated to class/IContentDefinition.
2. Targeted dependency build: exit1,10.09s,0 warnings/2 errors. New negative fixture instantiated abstract ResourceDef and invoked internal JsonValue.Freeze. It now uses native BasicResourceDef only as a deliberately foreign alias and an ordinary non-object JSON array; no engine accessibility was widened.
3. Targeted: exit1,8.81s,0 warnings/4 errors. Historical native-owner assertions accessed AuthoringPath/Read/Resolve through the new interface. Those assertions explicitly cast their actual Def only, rather than polluting the new public metadata contract with authoring mutation methods.
4. Targeted FINAL: exit0,5.42s,0 warnings/0 errors; all five dependency projects compiled.

FIRST/FINAL runtime1:438 passed/0 failed/0 aborted, suite1688ms; owned PID39444 exit0, observed1845ms, outcome Exited. Output not truncated, no observer error. No executable edits after FINAL.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Spatial layer content: direct and native layered metadata drives physical tile and point extraction' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

Limits: outer30000ms, owned termination5000ms, output30000chars, physical work bounded256 ordinary Updates. Failure assertions include actual game clock/deposit/stock. No old/family/full/Oil/Tycoon/platform runtime. Previous runtime evidence is historical on2.421.0; compile success does not revalidate it.

## Scope and publication

Read-only metadata gate:1267 source anchors+13 quickstart anchors,0 mismatches;2533 indexed paths/872 Markdown files/7369 local links,0 broken;18 master rows,0 malformed; git diff --check exit0 with34 preexisting output lines. New driver boundary matches0; selected executable drift0; actual git status --porcelain=v1 --untracked-files=all reports2523 dirty paths,0 staged. Original221 requirement words/checkboxes unchanged,204 line anchors refreshed. Sole GameClock and all three protected Tycoon hashes unchanged. Metadata checks are not historical runtime verification.

Post-FINAL executable freeze:2026-10-10 03:39:54 UTC,599 selected inputs. Thirty-eight changed/new versus590; nine newly selected paths include three new executable files (driver, independent/negative metadata registration and authored addon) and six existing owner/caller sources. No executable edits after FINAL; canonical completion audit records actual SHA-256 inventory.

Original221 requirement words/checkboxes remain159checked/62open. This removes the required native spatial-layer metadata dependency toward P2.003/P4.003/P3 layered extensibility. It does not complete all provider/visibility/sensor/line/fault/loss/scale/performance/lockstep matrices, full regression or full P1–P12. Protected G19, sole clock and optional Tycoon behavior unchanged; no OilFieldDays/platform edits. The implementation goal remains active; nothing staged, committed or pushed before full goal AND full plan completion.
