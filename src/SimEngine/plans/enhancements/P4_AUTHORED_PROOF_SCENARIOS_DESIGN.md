# P4-I9: Authored proof worlds and explicit remaining scenario intent — Design Document

Status: scoped implemented/verified,2026-10-08. Full P1-P12 active; publication only after full verified completion.

Six authored files/12 diagnostic declaration migrations. TWO builds:first3.10s0 warnings/one old const
default CS1736, then clean3.89s0/0; ONE exact NEW bounded case FIRST attempt70/0/0 648ms. Code/tests/
proof/JSON frozen after green. Engine code/Build2.277.0/save140/clock unchanged; content owns data
identity. Older fixtures compile-only; no older/full/family/platform/stale runtime or broad acceptance.
[Evidence](../../docs/migration/P4_AUTHORED_PROOF_SCENARIOS_VERIFICATION.md).

## 0. Problem statement

P4-I8 requires explicit scenario mode/overrides, but 16 C# files still contain scenario text. Three are
string-replacement tests or the already-migrated shared TestGame; one contains intentionally malformed
metadata. Remaining valid point/mapless/projection/controller/fault fixture declarations omit required
metadata. Reusable TestGame/PointGame/CompositionProbe still compile their canonical JSON worlds.

## 1. Goals

1. Directly author mode/overrides for every valid remaining embedded scenario; preserve deliberately
   incomplete negative metadata. No runtime reader/factory may synthesize a missing mode.
2. Move the three reusable canonical proof worlds into explicit embedded JSON assets, retaining
   original content owners, IDs, resources, clocks, physical roles and provider declarations.
3. Move the static TestGame/PointGame masters and TestGame extra prototype document to authored files;
   retain normal strict master composition, not a new loader or content registry.
4. Verify a distinct bounded player-only proof of file-owned tile/point/mapless game configuration,
   physical loaded transport/work, frozen observations, saved twins/pause/isolated sessions.

## 2. Design

TestGame.Manifest/Defs/ExtraDefs and PointGame.Definitions become explicit FrameworkProofContent.Read
properties. TestGame.Content/PointGame.Factory optional content arguments use null to select their
already-authored base document, never to synthesize fields/default profiles. Mechanical callers whose
default parameter formerly required a const become null selectors of that exact authored document.
PointGame's static master moves intact into point-master.json; existing explicit optional module/
document enrollment remains pure assembly, not gameplay. CompositionProbe world moves into its owned
composition-probe-world.json; variable module-version fault instrumentation keeps original metadata
assembly. Exact original world JSON text is preserved apart from explicit mode metadata; no old alias.

Point world authors point.resource-only and probe world probe.resource-only, properties {}. Existing
TestGame profile test.resource-only remains unchanged. Remaining compiled dynamic/fault fixtures
explicitly author per-fixture empty profiles/overrides in their own JSON inputs; selected extension
fixture explicitly references its required TestGame profile. Missing-field negative tests stay invalid.
Those diagnostic test inputs are not a compatibility path or host gameplay; further extraction of
every diagnostic string is separate from canonical reusable game authoring.

Engine simulation code/Build2.277.0/save140/codecs/GameClock unchanged: content fingerprints identify
authored world changes. No extra build-identity bump for test/data-only migration. No business source,
Oil/sibling/host/demo/platform/Draw changes, no new world-size restrictions or parallel responsibility.

## 3. API surface

No engine API changes. Reusable proof data properties retain original member names, now read actual
authored files. Pure test assembly retains original public contract providers and command boundary.

## 4. Files to change

TestGame.cs/PointSpaceProof.cs/CompositionProbe.cs; valid remaining inline fixture declarations;
SelectedContentTests default data selector; six authored JSON assets/ExtensionProof resources;
one new exact public-player case/Program; stage/evidence/source manifest/master/README/AGENTS/P4/P6.

## 5. Error handling matrix

Missing files/unknown or duplicate content/invalid metadata → original strict content errors.
No old inline canonical world retained, no profile/mode fallback. Invalid unselected metadata remains
typed and does not contaminate unrelated selected games. Saved current content identity remains exact.

## 6. Backward compatibility

Development migration only. No duplicate world constants, old loader or old save reader.

## 7. Verification

ONE exact NEW bounded case, at most1000 Updates per physical stage with public diagnostic snapshot.
Prove actual master/file ownership, canonical metadata and no implicit monetary service; direct JSON
mode edit changes preview/start identity; strict missing fields still refuse; ordinary point order
and loaded physical shipment progress over time; saved partial work/cargo twins/pause/material
conservation/session isolation and pure metadata. No previous/family/full/platform/stale runtime.
Record all build/run attempts; repair/rerun only observed same-case failures; freeze source after green.
Older authored fixture migrations compile-only, not broad baseline regression evidence.

## 8. Risks

Const-to-file properties affect default parameters, migrated mechanically after actual reference
inspection. Preserve literal JSON whitespace used by deliberate old negative string probes.
Independent fault fixtures still author malformed data deliberately; do not turn them into success.
Different games can own the same reusable authoring in their own explicit master, never twice in one
selected catalog. Bound physical travel stages rather than fabricating teleport/resource fallback.

## 9. Out of scope

Full old diagnostic string extraction, custom colony startup/rules migration, global purity/lifecycle
sealing/hostile mod guarantees, advanced economics/Oil/replay/lockstep/stress/allocation/full P1-P12.

## 10. Sub-task execution order

1. Inventory valid/intentional invalid scenario declarations; directly author missing metadata.
2. Extract canonical reusable worlds/static masters/extra prototype into explicit JSON files.
3. Add one distinct bounded public-player case; build/exact gate/freeze.
4. Final source-only checks/evidence/tracker; preserve full goal and deferred commit/push.
