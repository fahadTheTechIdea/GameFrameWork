# P6-I3: Complete the nonspatial process-game progression proof

Status: implemented/focused-verified on unchanged engine2.605.0/save159/replay1,
final70/0. [All attempts and limits](../../docs/migration/P6_NONSPATIAL_PROGRESSION_VERIFICATION.md).
Implements the missing research/objective portion of original
P6 Game A, using the existing externally compiled ProcessProofModule and process.json.
Original broad P6.acceptance.008/full industrial-colony goal remain open.

## Problem and goals

NativeNonspatialStartupTests proves mapless work and manufacturing separately, but
not authored research, persistent plan pause, operation control and objective outcome
in the same external process game. Add that full chain without another simulator,
copying resource/entity/process definitions or requiring a map/business system.

Acceptance: ordinary player commands earn timed material-consuming research, unlock
the original build recipe, pause/resume its actual execution, manufacture two native
outputs and complete dependent objectives. Save/load, replay, immutable states and
two genuinely advancing independent sessions must agree with original owners.

## Design and API

The new process-progression-master.json explicitly lists the original process.json,
existing access/work-retry documents and one new progression document. Register only
existing engine modules and ProcessProofModule. The new document authors technology,
typed material requirement, recipe unlock, objective graph and selected scenario.
No new C# gameplay implementation or scheduler change is required.

Native WorkModule/IPlanBoard owns timed research/pause/priority; TechnologyModule
owns private earned knowledge/unlock; existing resource transactions own consumption;
TransformationModule owns original six-minute sequential batches; ObjectiveModule
owns prerequisite observation and terminal outcome. Commands/views remain original.
The driver supplies intent, bounded updates and assertions only.

## Files and errors

- New master/progression JSON under tests/ExtensionProof/content, explicitly embedded.
- New NonspatialProgressionTests registered in the existing runner.
- No production engine, Oil/Tycoon, host/platform, legacy or compatibility changes.
- Missing prerequisite keeps recipe locked, never invents output or stock.
- Paused plans/disabled operations preserve earned work and inputs; resuming never
  grants retroactive work. Foreign company knowledge/objectives remain private.

## Verification and stages

- [x] I3.1 — Author master-listed progression with original process resources/content.
- [x] I3.2 — Exercise the entire chain via public inputs/views, including pause,
  changed plan priority, operation disable/resume, saved partials and exact material totals.
- [x] I3.3 — Prove canonical replay and two independently advancing sessions.
- [x] I3.4 — Record focused exact-case evidence and reconcile master; no broad closure.

Build the targeted test project before the single new literal case. Existing
full/profile/family tests remain unrun. No Git publication until whole plan finishes.
Broader spatial/mod/fault/durability/global original requirements remain open.
