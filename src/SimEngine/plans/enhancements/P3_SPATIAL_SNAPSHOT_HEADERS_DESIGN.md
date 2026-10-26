# P3-SH1 spatial snapshot headers — Design Document

## 0. Problem Statement

Hosting/PlayerView.cs ViewOf accepts null/foreign-space ISpatialPresence.Capture results. Hosting/GameSnapshot.cs Capture likewise accepts null/foreign-space world captures. Kernel/SpatialContracts.cs snapshot base constructors permit absent space identity and negative world revision. Provider purity alone does not validate returned facts.

## 1. Goals

1. Selected spatial entities publish a non-null snapshot identifying their actual selected space.
2. Selected spaces publish a non-null world snapshot with matching identity and a nonnegative viewer-local revision.
3. Never equate snapshot revision with global ObservationVersion, which can disclose private invalidation.
4. Malformed or mutating capture faults before publication; existing lifecycle retains last healthy viewer facts.
5. Preserve native/point/nonspatial behavior, private snapshot streams, deterministic saves/replay and original state ownership.

## 2. Design

Validate snapshot base headers: nonempty space identity; world version nonnegative. At existing entity capture boundary, require actual presence.Space and result.Space to match world.Space.Id; null is valid only when no presence is selected. At existing world capture boundary, require returned Space to match the selected provider Id and version nonnegative; null is valid only for a nonspatial session.

Perform returned-header validation within EntityObservation.ReadProvider, observing actual presence/target codec and selected space for entity capture. Existing world capture guard and snapshot lifecycle first-fault/retention stay authoritative. No new snapshot owner, publication stream, clock or persistence state.

Extend existing independent point authoring with optional negative-only capture metadata. World probes trigger after ordinary access closure; entity probes trigger after actual movement reaches an authored position. Probe null/foreign/blank/negative headers, original exceptions and observed mutation without a second spatial provider or callback scheduler.

## 3. API surface

Same immutable snapshots and capture interfaces; invalid headers now fail explicitly. Viewer-local world revision may legitimately differ from ObservationVersion. Save154/replay1 unchanged; engine identity advances.

## 4. Files to change

SpatialContracts.cs base header invariants, PlayerView.cs entity capture, GameSnapshot.cs world capture, Kernel/SaveGame.cs and protocol fixture. Existing PointSpaceProof.cs negative authoring/capture; one public-driver case/register and tracker/evidence/anchors. Reuse actual point/native/nonspatial masters and JSON.

## 5. Error handling matrix

| Provider result | Engine result |
| --- | --- |
| Actual selected space/nonnegative viewer revision | Normal publication |
| No spatial authority/presence | Existing null spatial projection |
| Null selected capture or foreign identity | First capture fault; no malformed publication |
| Blank identity or negative revision | Header constructor fault inside observed callback |
| Pure capture mutates or throws | Existing first-fault and retained last healthy viewer snapshots |

## 6. Backward compatibility

Development only; strengthen canonical contracts directly, no compatibility path.

## 7. Verification

One solution build and necessary focused repairs only. ONE NEW bounded public-player case: native/point/nonspatial headers, actual point motion and saved/replayed snapshots, ordinary access controls, independent declared malformed world/entity probes, first-fault/last healthy retention/clock cutoff/no healthy save/hash and isolated sessions. No old/family/full runtime.

## 8. Risks

Header/purity guards are not an arbitrary CLR payload immutability sandbox. Native layer metadata is already frozen and work bindings already immutable. Broad custom payloads, every spatial query/visibility/private-cell/delta/performance matrix and full P3/P11 remain open.

## 9. Out of scope

Oil, platform/Draw/adapters, Tycoon/protected G19/clock implementation, broad regression and publication.

## 10. Sub-task execution order

1. Validate existing snapshot headers at original observed capture boundaries.
2. Author independent negative capture probes and public-player proof.
3. Record actual evidence without modifying original gate scope/text/status.

## 11. Status

Core2.484.0/save154/replay1. Existing constructors/capture boundaries require actual selected space identity, non-null selected captures and nonnegative viewer-local revision; no global revision equality or new snapshot state. ONE NEW FIRST/FINAL286/0/0 in740ms;2 point minute/half actual motion/saved/replay flows, native/nonspatial headers and6 world/5 entity null/foreign/blank/negative/throw/mutation probes, per-viewer healthy retention/clock cutoff/isolation. One clean solution10.93s, no repair compile or executable edit after FINAL. Original221/186checked/35open unchanged, additional211. Arbitrary payloads/all spatial queries/visibility/private-cell/delta/performance/full P3/P11/P1-P12/publication OPEN. [Evidence](../../docs/migration/P3_SPATIAL_SNAPSHOT_HEADERS_VERIFICATION.md).

Fresh read-only metadata closure:1718 source anchors,956 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2906 paths/1030 Markdown files/8672 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2894/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional211. Seven existing executable sources changed: six previously selected and existing GameSnapshot.cs newly included in selected freeze; one new executable driver/three new source files, four existing production sources changed/zero new. Existing view schemas unchanged; returned header validation adds no projected fields. Only the primary P3 row was updated; the distinct historical P3 row remains untouched. No runtime rerun for documentation validation.
