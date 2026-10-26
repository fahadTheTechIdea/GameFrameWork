# P11-VM1 — Typed visibility target metadata, not runtime matching predicates

Status: implemented, scoped verification complete. Full original gates/goal open.
[All builds/runs, profiling, performance and limits](../../docs/migration/P11_VISIBILITY_TARGET_METADATA_VERIFICATION.md).

## 0. Problem Statement

P5-TW1's new actual Oil manufacturing case did not complete its first36h cycle
within the existing45s stage bound: Debug stopped at242 minutes, Release at1142.
Two managed stack samples independently located repeated complete TileMap.Sync /
MapLayer.ValidateValues hashing under unrelated visibility-rule Matches callbacks.
The30s diagnostic child was intentionally bounded and terminated; it is not a pass.

Every current target implementation matches fixed resolved content references.
IVisibilityTarget.Matches incorrectly makes immutable content association a runtime
callback, multiplying whole-space observations for every unrelated rule.

## 1. Goals

1. Replace Matches with required typed immutable Targets metadata on the same rule
   contract; delete the old API and migrate every native/independent implementation.
2. Capture/validate actual target aliases in the existing guarded policy read, then
   match detached references without callbacks. No positive visibility-result cache.
3. Keep all dynamic Allows/AllowsEntity/AllowsSample/CopyObservationKeys callbacks
   and original whole-space/entity/codecs/clock/first-fault guards unchanged.
4. Prove current actual manufacturing completes within unchanged bounds, while
   visibility/privacy/provider-fault cases remain correct. Do not weaken hashes,
   replace original codecs with revision checks or increase timing bounds to pass.

## 2. Design

IVisibilityTarget exposes IReadOnlyList<IContentDefinition> Targets. Existing JSON
declarations still resolve their original typed `definition` IDs. The shared target
declaration exposes the corresponding readonly list. Multi-target extensions may
declare explicit actual references without editing KnowledgeVisibility.

Existing PolicyFacts captures each rule and copied target references inside the
original observed ReadPolicy interval. Validate nonnull/nonempty/unique frozen
catalog-owned targets, including unused loaded rules before Initialize. Matching
is ReferenceEquals against these detached per-query facts. Dynamic predicates
still execute only when their actual declared target applies, in original canonical
rule order and original observer boundaries. Policy facts are transient metadata,
not a registry, learned table, authority/result cache or second policy owner.

All standard target classes and independent proof implementations migrate directly.
Oil declares no IVisibilityTarget implementations and requires no source migration.
StateHasher/EntityObservation/TileMap codecs and every saved layout remain unchanged.
Build2.634→2.635; save159/replay1 unchanged.

## 3. API surface

Required Targets replaces Matches; no compatibility method/overload. Shared native
declaration and one-target view helper support contract implementations. Current
JSON requires no rewritten rules or changed authorization values.

## 4. Files to change

Original Standard/KnowledgeVisibility and native sensor/fresh/region rules; existing
independent visibility proofs; exact target metadata/negative tests; build identity
expectation; P5-TW1/P11 stage evidence/master tracker/README/AGENTS.

## 5. Error handling matrix

| Metadata | Result |
|---|---|
| Missing/empty/null target | Content refusal before Initialize |
| Duplicate/nonactual target | Content refusal; no guessed definition |
| No matching rule | Original explicit unlisted policy |
| Matching denied dynamic rule | Denied as before |
| Rule mutation/throw | Original observation fault, no healthy result prefix |

## 6. Backward compatibility

Development-only contract replacement; no legacy matching path or renamed duplicate.

## 7. Verification

Targeted Release framework/consumer build and exact cases: new metadata contract,
actual Oil manufacturing, existing visibility content, spatial observer/first-fault
and selected physical timing. No full/family/profile run. Record every attempt and
retain full original gates unless all their requirements are actually proven.

## 8. Risks

Preserve typed reference identity even for mods overriding object equality. Do not
cache dynamic visibility, ignore later rule callbacks or infer target sets from CLR
types/names. Independent content must expose actual references explicitly. Copies
of target lists do not authorize arbitrary mutable metadata/provider behavior.

## 9. Out of scope

New hashing algorithm/codec cache/revision-only protection, platform/Draw, business
rules, changed Oil durations/resources, broad profile/stress and publication.

## 10. Sub-task execution order

1. Replace the static association contract and same-owner metadata dispatch.
2. Add strict positive/negative target metadata proof and migrate current providers.
3. Build once after the completed slice; run only selected exact affected cases.
4. Record performance/behavior evidence and original gate limitations.

Completed: required typed target metadata and original guarded per-query policy
facts replace the legacy matching callback; all current providers migrated, no
positive authorization cache or hash weakening. Five exact finals1093/0; related
current Oil timing cases bring eight distinct results to2120/0. Actual two36h native
vehicles complete inside the unchanged stage bounds. All earlier failed/diagnostic
attempts retained. Original221/208 recorded checked/13 open unchanged.
