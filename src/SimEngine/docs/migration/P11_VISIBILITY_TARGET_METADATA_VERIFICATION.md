# P11-VM1 — Visibility target metadata and physical timing verification

Date: 2026-10-15. Current engine2.635.0/save159/replay1. Focused completion only.

## Root cause and implemented change

The new actual Oil manufacturing case exposed repeated complete map serialization
under static visibility matching. Pre-change Debug reached time242/progress241 of
2160 before its45s stage bound; Release reached1142/progress1141 before the same
bound. Neither manufactured a vehicle. The guard was not enlarged or removed.

Two `dotnet-stack report` snapshots of the explicitly bounded diagnostic child32292
showed the same hot path, first in Verify and then in Capture:

```text
MapLayer<byte>.ValidateValues / CheckValue
MapLayer<byte>.Sync -> TileMap.Sync
EntityObservation.ProviderScope.Hash -> SpatialScope.Verify / constructor
KnowledgeVisibility.Observe -> Allows / CanSeeContent
SpatialLayerAccess.CopyObservationKeys -> TileMap.RefreshVisibility -> World.Step
```

The diagnostic parent33604/session45274 intentionally terminated that child at30s;
it was a profiling observation, never represented as an acceptance pass. Installed
dotnet-stack was used; no profiling package/service was installed.

Every current IVisibilityTarget matched fixed actual content references. The API
now declares readonly typed `Targets` instead of runtime `Matches`. The existing
guarded ReadPolicy captures/validates target references into its transient PolicyFacts
and RuleFacts. Matching those references performs no callback. Target identity uses
ReferenceEquals/RuntimeHelpers hash, not mod-overridden object equality.

All native knowledge/public/sensor/region/fresh rules and independent proof providers
migrate directly; old matching API and shared declaration method are deleted. Existing
single-definition JSON stays authoritative. The new independent multi-target proof
uses an explicitly master-listed `visibility-targets.json` document. Missing/empty/
null/null-item/foreign/duplicate targets refuse, including unused loaded rules before
Initialize. Getter mutation/throw is still inside the original guarded policy read.

**No dynamic authorization result is cached.** Every query rereads policy metadata;
applicable Allows/AllowsEntity/AllowsSample/CopyObservationKeys still execute in
canonical order through original full-space/entity/clock/provider guards. No revision-
only check, codec cache, partial map hash, new hashing algorithm, new visibility state
owner or modified StateHasher/EntityObservation/TileMap implementation was introduced.
No saved layout changed. Actual Oil defines no IVisibilityTarget implementations and
needed no source migration. No Oil/business/platform/Draw source changed.

## Current focused results

All commands selected one exact literal, never a family/profile/full bundle.
Timeout60000ms, termination5000ms, except actual manufacture120000ms with its unchanged
per-stage45000ms bound. Output bounds5000–7000 characters. All below exited0 without
abort/truncation; every observed live session was polled to terminal status.

| Exact case | Passed / failed | Child PID | Suite / process ms |
|---|---:|---:|---:|
| Visibility target metadata: typed actual references avoid runtime match callbacks |26 /0|32732|281 /408|
| Oil canonical manufacturing: actual public company completes sequential physical haul trucks |39 /0|16544|26627 /26763|
| Visibility content contracts: earned private discovery and guarded rules |168 /0|27700|950 /1085|
| Shared spatial observation: original codecs and revisions guard physical native and point callbacks |776 /0|34536|12630 /12763|
| Knowledge freshness: physical survey expiry and exact saved visibility |84 /0|3792|1362 /1496|

Five exact final results: **1093 passed, zero failed**. Actual unchanged36h first
incomplete production stage took13016ms, second12438ms; both canonical vehicles were
released at original time2161/4321 with no premature/overlapping output and conserved
actual inputs. Before the change, the Release stage budget expired after only1141
productive minutes. This is observed local small-world behavior, not a global FPS,
allocation or arbitrary-world-size performance guarantee.

The shared spatial case includes native/point physical construction, saved supply,
earned effects, minute/half-minute timing and layer-payload/revision mutation/throw
matrices. The existing visibility case covers actual earned learning/revocation,
private entity state, sample/key guards, recursive/swallowed-recursive callbacks and
first-fault stop. The freshness case exercises existing native finite/stale reading
and actual survey behavior. These selected cases do not exhaust arbitrary providers.

The new metadata case verifies two actual typed targets, readonly lists, exactly one
dynamic predicate per matching query, no predicate for unrelated content, pure saved
identity, six invalid unused declarations and two getter mutation/throw faults.

## Every failed or diagnostic attempt and every build

Full P5 timing details are in [P5-TW1 evidence](P5_TIMED_WORKFLOW_REVALIDATION_VERIFICATION.md).

- Before implementation2.634 Debug manufacturing:15/1, child32312,53321/53461ms.
- Profiling-only2.634 Debug: child32292, TimedOut at30044ms; two observed managed
  stacks above; no completed assertion aggregate was available.
- Before implementation2.634 Release manufacturing:15/1, child30640,48827/48957ms.
- Initial migrated proof build failed2.79s with CS0200: negative probe attempted to
  assign private-set Def.Id. It now supplies an ordinary unowned/unfrozen EntityDef
  without assigning identity. No production visibility rule failed compilation.
- First existing visibility run:167/1, child31196,917/1048ms. Only failure was the
  obsolete test expectation save154 versus actual159. It now uses the existing
  independently authored canonical ExpectedSaveFormat; no codec/format was changed.

Targeted builds observed across the related P5-TW1/P11-VM1 work:

| Target/configuration | Result | Build elapsed |
|---|---|---:|
| Actual OilCo.PublicProof Release, pre-change |clean|2.81s|
| Framework tests Debug, new manufacturing proof |clean|5.80s|
| Framework tests Release, pre-change comparison |clean|6.27s|
| Framework tests Release, first metadata migration |probe compile failure|2.79s|
| Framework tests Release, corrected unowned probe |clean|5.25s|
| Actual OilCo.PublicProof Release, current core |clean|1.50s|
| Framework tests Release, canonical save expectation |clean|5.28s|

All clean builds had zero warnings/errors. Commands were `dotnet build <specific
csproj> --no-restore [-c Release] -v minimal`. No solution-wide test run or blanket
test-after-every-edit workflow. Other final core cases were not rerun merely because
the unrelated existing case's save expectation in the same test assembly changed;
their case/probe sources and the production core were unchanged afterward.

## Identities and completion boundary

Sorted path/content SHA256 over443 core C# files:
`bb6710f6a98446400a53d8eeb7cac6921d78ba9c8f4d5e2d2af3104ddad53033`.
Release framework DLL:
`4A15E04037124374F7F3ADF1F6179C9BC39104FC97512C50288381EEDA66503C`.
Final Release test DLL (after only save-expectation correction):
`9E1F7BFB8EF1AA602D4443169DB9B659A49C1FEA96F5CCA2B0904E286020C941`.

P5-TW1 adds current research/recovery/construction results; together eight distinct
current-core exact cases total2120/0. That is not a full profile or full-plan result.
Original221 requirements/208 recorded checked/13 open remain unchanged; no original
broad gate closes here. P5.005/.007/.008/.009, P6, broad P11 and full goal remain open.
No version-only native-map shortcut, compatibility API, business change, platform
work, memory write, Git staging/commit/push or publication.
