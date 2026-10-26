# P3-LA1 spatial layer access provider guards

## 0. Problem

Kernel/SpatialContracts.cs SpatialLayerAccess calls actual layer CanRead, visibility CanSeeContent/CanSeeSample and CopyObservationKeys directly. Built-in KnowledgeVisibility self-guards, but another valid IVisibility/ISpatialSampleVisibility owner does not have to duplicate engine protections. A mutating extension can change its persisted policy or selected space/clock during a read. Observation keys also append a visible layer prefix before validating all sample keys, exposing a partial result on callback failure.

## 1. Outcomes

Actual provider enumeration, layer admission and every selected visibility callback run through existing EntityObservation guards, retaining original provider/space codecs and sole clock. Invalid or foreign layer/viewer references refuse without calling unowned layer callbacks. Refused/throwing/mutating visibility cannot publish samples or key prefixes. Successful metadata links, ownership and sample policies remain authoritative, including alternate point geometry. No game logic moves to clients.

## 2. Implementation

Extend SpatialLayerAccess in place: guard actual selected provider and its runtime layer identity, then the same IVisibility instance around content-link checks. CanObserve first verifies actual ownership, then guards the actual layer CanRead. Guard optional sample authorization and all observation-key callbacks against actual provider and selected space. Build key output in transient local lists, validate every opaque key inside observation, and append only after admission succeeds. Existing cursor caches, learned knowledge, world state and version owners remain untouched.

Reuse existing engine-side ViewObservationVisibility for independent no-self-guard extension proof. Add optional sample/content/key hooks preserving old defaults and codec1. A JSON-authored proof module supplies ranges, hidden resource and negative-only pause-triggered callbacks, binds this same owner and adds a read-only key feature. Public driver uses original point master/layers, previews/commands/physical worker Game.Update, saves/replay and pure reads; never World/TestKit/direct stock or progress writers.

## 3. Contracts and files

No new production contracts or save state. Same SpatialLayerAccess public API and IVisibility/ISpatialSampleVisibility semantics; changing CanRead ordering deliberately avoids invoking an unowned layer. Core advances2.494.0; save154/replay1 unchanged. Production SpatialContracts.cs/SaveGame.cs; existing proof visibility hooks/current protocol/Program/complete test profile; one proof module/content file/driver and stage evidence.

## 4. Failure semantics

Hidden/missing/foreign layer or invalid viewer: same unavailable semantics, no fabricated values. Malformed key, callback throw, persisted visibility/selected space/clock mutation: original first engine fault, no partial key result; retained viewer publications and stopped sole clock. Caller-owned destination failures are not transactional rollback; malicious arbitrary unrelated world writes and global sandboxing are not claimed.

## 5. Compatibility and limits

Development only, no compatibility shim/default loader/parallel visibility or geometry model. Existing native/point state owners and built-in knowledge rules retained. Allocation cost and all private payload/layer/menu/knowledge/query/delta breadth remain open, as do P3.acceptance.008/P11.acceptance.026/033. No Tycoon/Oil/platform/Draw/clock implementation changes. Final full regression only after implementation finishes.

## 6. Ordered verification

1. Apply shared guards/atomic key admission and independent JSON proof hooks.
2. One solution build, necessary focused repairs only.
3. One new exact bounded case covering two physical time quanta, owned samples/resource metadata, hidden sample refusals/pages, key stability/atomicity, saves/replay/isolation and independent callback faults.
4. Metadata-only complete profile assignment and source/gate audit. No old/family/full suites; retain all attempts.

## Status

VERIFIED-SCOPED: NEW FINAL787/0/0 in1725ms; two physical time quanta with pre-completion saves/twins/replay, hidden metadata/private/sample/page/key boundaries and27 independent callback faults. FIRST28/10 retained (master module omitted from explicit scenario). Solution2.87s failed1 internal proof call; focused5.60s/5.83s clean. No executable edits after FINAL. Profile-info693 included/0 excluded/0 game suites, no full executor run. Original221/188checked/33open unchanged; additional225. [Evidence](../../docs/migration/P3_SPATIAL_LAYER_ACCESS_GUARDS_VERIFICATION.md).

## Source identity

Freeze2026-10-12 13:17:14 UTC:1,774 source anchors/996 selected executable inputs/13 quickstart anchors match actual dirty source with0 drift. Inventory2,959 files/1,057 Markdown documents/8,737 local links/0 broken links;18 valid master rows;221 original literal gate/line anchors validated with0 errors. Original188 checked/33 open unchanged, additional225. Six previously selected executable inputs change; four added inputs are three new proof inputs and previously existing ViewObservationProof, also newly source-anchored. Two production sources change; five new files (three proof inputs/two docs), no new production state/classes. Sole clock/all12 Tycoon/Oil/platform/Draw implementations unchanged. git diff --check exit0 with34 pre-existing warning lines;2,947 dirty entries/0 staged. No executable edits after FINAL, no old/family/full suite, profile executor or staging/commit/push.
