# P3-DS1 direct spatial view admission

## Problem and scope

PlayerView.Space guarded provider purity but accepted a selected provider returning null or a foreign-space snapshot. GameSnapshot.Capture separately validated the same result. A controller reading the live view during a command could therefore receive facts that publication would subsequently refuse. This is an engine boundary defect, not host gameplay.

## Single responsible owner

Extend the existing EntityObservation guard with ReadSpace(World, PlayerId). Both original live and publication paths use it. Require healthy session state, select only world.Space, preserve null only for nonspatial sessions, capture through the actual provider and selected space codec/revision, and validate non-null result, actual identity and nonnegative viewer-local revision inside observation. Existing first-fault and retained per-viewer publications remain authoritative. Never substitute a different space or equate viewer revision with global observation version.

No new public model, spatial state, registry, clock, cache, fallback, compatibility layer, rendering or business logic. Save154/replay1 and existing codecs unchanged; engine identity advances to2.493.0.

## Verification stages

1. Centralize existing validation and remove duplicate publication implementation.
2. Extend independent engine-side proof command ownership with a negative-only direct capture injection. Close actual access, read World.ViewFor(command.Player).Space before publication, and throw an unmistakable sentinel if the invalid result escapes. Existing point provider JSON fault modes remain the authority; no direct state writers in the public driver.
3. One new exact bounded case: minute/half-minute physical inspect work, live/publication agreement, purity, detached reads, saved twins, replay, native and nonspatial reads; six malformed/mutating/throwing modes for each of two real seats, first fault, retained viewer facts, no false deltas, clock cutoff and isolation.
4. One solution build and exact-case gate; metadata-only explicit complete profile update. Record every attempt and current source identity. No old/family/full suites.

## Acceptance and exclusions

Live and published reads share one admission rule; null/foreign selected results cannot return to a controller. Missing spatial authority remains a legitimate absent fact. Failures retain last healthy per-viewer publications and refuse later live reads. Test driver submits commands and advances Game.Update only; all execution and negative injection are engine-owned.

This slice does not prove arbitrary payload immutability, every private layer/sample/menu/knowledge stream, allocation budgets, global callback sandboxing or whole P3.acceptance.008/P11.acceptance.033. Those original gates remain open. Final full regression stays deferred until implementation finishes; no staging/commit/push.

## Status

VERIFIED-SCOPED: ONE NEW exact FIRST/FINAL818/0/0 in998ms, both healthy physical time quanta and12 direct two-seat negative captures. First solution6.64s failed4 driver property names; focused repair8.79s clean. No executable edits after FINAL. Profile-info692 included/0 excluded/0 game suites, no full executor run. Original221/188checked/33open unchanged; additional224. [Evidence](../../docs/migration/P3_DIRECT_SPATIAL_VIEW_VERIFICATION.md).

## Source identity and limits

Freeze2026-10-12 12:55:46 UTC:1,768 source anchors/992 selected executable inputs/13 quickstart anchors match actual dirty source with0 drift. Inventory2,954 files/1,055 Markdown documents/8,732 local links/0 broken links;18 valid master rows;221 original literal gate/line anchors validated with0 errors. Original188 checked/33 open unchanged, additional224. Eight existing selected inputs change (four production sources and four existing proof/metadata inputs); two new proof inputs/two docs. Sole clock/all12 Tycoon sources unchanged, Oil/platform/Draw unchanged. git diff --check exit0 with34 pre-existing warning lines;2,942 dirty entries/0 staged. No executable edits after FIRST/FINAL, no old/family/full suite, profile executor or staging/commit/push.
