# P2-F9 — Manifest-only content catalog

## Problem and goals

ContentTypes.Generator and ResolvedContent.Build still admit programmatically
generated definitions outside the explicit master-listed JSON documents.
EntityDef.Generated and unused native inventory/tile-presence constructor helpers
support that old path. This conflicts with file-owned, contract-based game content.

Remove this alternate admission route in place. Modules register section/component
contracts and their implementations; selected master documents author every
catalog definition. Existing ContentSet/ResolvedContent strict readers, aliases,
ownership, component schemas, references, validation and publication remain sole
owners. Runtime spatial intents and derived activity execution graphs are not new
catalog definitions and remain their existing owners; no second parser/catalog.

## Stages and files

1. Delete generator registration/list/entry from ContentTypes.cs and generated
   addition pass from ResolvedContent.cs. Delete EntityDef.Generated from Defs.cs
   and unused StorageDeclaration.Make/StorageDef.Make/TilePresenceDef.Make/Create.
2. Migrate every catalog-generator test fixture to explicit authored JSON. Preserve
   declaration/inactive-provider/ownership/codec/schema/frozen-publication assertions
   against the sole authored path. Retain internal negative component attachment
   probes only in tests; no positive gameplay or catalog generator helper.
3. Add one new exact public-player case: master-selected independent line cursor
   metadata, physical multi-stage execution, saved twins/pause/private views/replay/
   interleaved isolation and malformed master-listed content before Initialize.
   Verify the removed API surface and changed authored fingerprint on file changes.
4. One solution compile, necessary targeted repairs, then ONE NEW exact bounded case
   only; same-case repairs if required. No old/family/full suites.
5. Update source/input evidence, tracker and literal completion audit. Keep broader
   original gates open without matching complete evidence.

## Contract, errors and durability

No generator/constructor fallback or compatibility path. Unknown sections/components,
inactive declarations, duplicate IDs, malformed authored fields or broken references
refuse through existing readers before Initialize. Engine2.443.0,save154/replay1;
native payload layouts and sole game clock unchanged. Actual master/document content
identity describes authored prototypes. No programmatic additions can enter via a
public module generator callback.

## Scope and risks

SimEngine only; no Oil consumer, platform/Draw, advanced Tycoon or protected G19.
Full P1–P12/global custom-provider/fault/performance acceptance remains open. Older
generator tests are migrated and compiled, not claimed runtime-verified. Every
changed assertion's migration is recorded; no test suite disappears. No staging,
commit or push before full goal AND plan finish.

[Evidence](../../docs/migration/P2_MANIFEST_ONLY_CATALOG_VERIFICATION.md)

## Current scoped result

Implemented: FINAL147passed/0failed/0aborted in2687ms, PID45488 Exited0.
Actual read-only closure checks1445source anchors/731selected inputs/13quickstart
anchors,221literal gates and18master rows. No hash drift, missing local link or
positive-driver boundary match; git diff --check exit0 (34line-ending warnings).
Original178checked/43open unchanged; additional167. Fourteen current headers
replaced in place without duplicating historical header blocks or shifting original
requirements. Old suites are migrated/compiled only. No executable edits after
FINAL, full/family runtime or staging/commit/push. Full goal and plan remain active.
