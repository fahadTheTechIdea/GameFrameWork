# P4-I8: Mandatory authored game-mode selection — Design Document

Status: scoped implemented/verified,2026-10-08. Full P1-P12 active, publication deferred until full verified completion.

ONE clean build12.05s0 warnings/errors; ONE exact NEW bounded case FIRST attempt91 passed/0 failed/
0 aborted1772ms on2.277.0/save140. Source/tests/proof/JSON frozen after green.142 identity paths
(EngineInfo+141 older fixtures) and old assembly/index migrations compile-only.25 actual JSON scenario
documents explicitly authored; no old/full/family/platform/stale runtime. Remaining old inline
scenario authoring and full release acceptance OPEN; not a full baseline regression claim.
[Evidence](../../docs/migration/P4_MANDATORY_GAME_MODES_VERIFICATION.md).

Subsequent [P4-I9](P4_AUTHORED_PROOF_SCENARIOS_DESIGN.md) directly authors12 valid diagnostic scenario
declarations and extracts canonical reusable worlds/masters into six files;70 checks ONE distinct
new case, older gate not rerun. Diagnostic file extraction/custom startup/full acceptance remain open.

## 0. Problem statement

Kernel/Modules.cs ScenarioDef optionally reads game_mode/mode_overrides, optionally resolves its
GameModeDef and may expose null configuration. Kernel/GameModeConfiguration.cs identity/selected
capability checks accept null. The approved P4-I2 plan explicitly leaves this migration unfinished.
Default authored colony selects no mode. Existing game-mode-properties.json mixes canonical eight
business schemas with three unrelated proof-only properties, preventing clean reuse by the colony.

## 1. Goals

1. Every scenario explicitly names a stable game_mode and authors mode_overrides (including {}).
2. Remove nullable/unconfigured resolution/identity bypass; one immutable configuration exists before
   any gameplay composition/Initialize, through original ScenarioDef/GameModeConfiguration owners.
3. Directly author missing mode selections for all actual proof JSON scenario assets; no runtime loader
   defaults or inferred genre profiles. Empty property schema means unavailable, not fabricated false.
4. Default colony selects JSON survival profile with all eight monetary features explicitly false,
   uses shared canonical schema file, registers optional Tycoon code but does not select/compose it.
5. Resource work/hauling/building/manufacturing remain physical and independent of money; typed
   profile/scenario/request provenance, capability refusal, save/hash/pause/session isolation survive.

## 2. Design

ScenarioDef.ReadCore requires game_mode String/ValidateId and mode_overrides Object/ReadAssignments.
ResolveCore uses Def<GameModeDef>, ValidateResolvedCore always builds original immutable projection.
ResolveGameMode has no no-profile branch; request capture and overrides still use original owner.
GameModeConfiguration.SyncIdentity uses required configuration rather than nullable mode/count;
ValidateSelectedCapabilities rejects null instead of silently skipping. No new mode service/store/loop.
World.GameMode documentation states required authored configuration. Save140 wire layout unchanged;
new semantic Build identity refuses old builds, no nullable save reader or compatibility shim.

Keep existing game-mode-properties.json as the sole canonical eight business schema definitions.
Move its three proof-only definitions into game-mode-proof-properties.json. Original mode and
business proof manifests/factories that load game-modes.json list/load that explicit proof document.
Update direct old property-index probes to their actual document/index; compile-only, not old runs.
No copied business schema/defaults, mode enum or new economics owner. Existing native Tycoon behavior
source remains untouched; disabled configuration metadata may name registered but unselected code.

Default colony master adds canonical game-mode-properties.json and new colony-game-modes.json to
its declaring module; available TycoonModule and explicit empty master module declaration satisfy
existing registered-capability validation without selecting it. colony.survival explicitly sets all
eight flags false; colony.resource-defaults has explicit {} and uses schema-authored false defaults.
Actual colony scenario selects survival with {} overrides. Factory only loads explicit documents.

All actual embedded JSON scenario files without a mode gain explicit per-game resource-only profile
content in the same declaring document; each stable ID is authored data, not generated at runtime.
Existing profiles remain selected; missing overrides become authored {}. Remove obsolete unconfigured
mode fixture/scenario and update its old negative probes to actual missing-field invalid data. Migrate
shared compiled TestGame.Defs explicitly. Other old inline C# fixture scenarios remain a separately
tracked direct-authoring migration; they will fail strict reads, never receive compatibility defaults.

## 3. API surface

IGameModeConfiguration/ScenarioDef/SimGame/GameFactory owners unchanged; successful games expose
non-null configuration. Required native scenario fields and direct proof content split. No new command,
host gameplay, economic account default, genre enum, Draw/platform API or size limit.

## 4. Files to change

Kernel/Modules.cs/GameModeConfiguration.cs/World.cs identity semantics; actual scenario JSON assets,
original shared mode schema/proof manifests/factories, colony master/modes/factory, TestGame.Defs,
ExtensionProof resources. ONE new public-player case/Program/identity fixtures compile-only.
Stage/evidence/source manifest/master/README/AGENTS/P4/P6/game-mode plans. No Tycoon business source.

## 5. Error handling matrix

Missing/null/empty/unknown mode or missing/null/array overrides → ContentException before usable
game/composition. Unknown/type-invalid/forbidden/conflicting overrides use existing strict resolver.
Enabled settings without selected owner refuse before gameplay; false never selects business code.
Invalid metadata/save identity rejects, never substitutes an empty configuration or inferred profile.

## 6. Backward compatibility

Direct development content migration, no nullable legacy path, alias, fallback or old save reader.
Remaining inline fixtures require explicit authored migration before any full acceptance claim.

## 7. Verification

ONE exact NEW bounded case: scenario metadata/profile/preview/configuration identity; survival all
eight off with profile provenance and no business feature; property-default/scenario/request false
provenance, strict invalid fields/settings/capability before composition, metadata collection purity,
actual timed physical loaded haul/staged build/manufacture and shortage, saved twins/pause/isolation.
Native no-business-schema fixture exposes explicit empty profile with unavailable property, not null
or invented false. Max2000 Updates per stage with public diagnostic snapshot. Record every build/run;
repair/rerun only observed same-case failures. Freeze source/proof/tests/JSON after final green.
All old identities/content/API/index probes compile-only; no old/family/full/platform/stale runtime.

## 8. Risks

Stricter core reads expose unmigrated inline fixtures, tracked explicitly rather than silently patched
by loaders. Registering available Tycoon code is not composing/activating business; verify selected
module/features instead of registration count alone. Empty-schema games remain valid physics intent,
but unavailable monetary properties are not claimed disabled by authored flags. Full mod/immutable
authoring/provider/global purity/advanced business/replay/stress gates remain separate.

## 9. Out of scope

Every old inline fixture scenario conversion, Tycoon behavior/advanced activation, mode-name branches,
dynamic in-session settings, generic profile inheritance, global lifecycle sealing/hostile mod sandbox,
full provider/industrial/Oil/replay/lockstep/soak/allocation acceptance. No Draw/platform/host edits.

## 10. Sub-task execution order

1. Remove optional scenario/null configuration path; split canonical schemas from proof-only fields.
2. Direct actual JSON/shared fixture authoring and explicit default colony mode master composition.
3. One new public-player strict/physical/identity case; build/exact gate/freeze.
4. Source checks/evidence/tracker including remaining inline migration; full goal remains active.
