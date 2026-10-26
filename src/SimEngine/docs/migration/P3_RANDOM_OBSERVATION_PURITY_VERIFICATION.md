# P3-RP1 random observation purity evidence

## Outcome and original scope

NEW FIRST/FINAL305 passed/0 failed/0 aborted in5510ms; bounded PID41876/process5741ms/exit0, no stdout/stderr truncation or observer error. One solution13.19s/0 warnings/errors. Engine2.499.0/save154/replay1. No executable edits after FINAL. Original221/189checked/32open unchanged; additional230. Whole goal and plan remain incomplete.

This proves the scoped random-purity correction, not every preview input/yield/throughput/ETA/blocker, every private snapshot/query/delta, global arbitrary provider writes, corruption/replay/lockstep/soak/profile or whole P3.008/P8.015/P6 gate. No original checkbox/text/line-anchor changes.

## Existing authority and implementation

Original DetRandom remains the one stream with unchanged SplitMix64 seed and xorshift64* values. Its internal transient ObservationVersion invalidates every successful draw or explicit State write, including equal-state writes. Draw-and-restore cannot hide a mutation. This is local invalidation like entity/spatial versions, not game time, another generator, a saved ledger or replay remainder. Checked invalidation overflow refuses before changing state. Zero State setter refuses before mutation; no fabricated seed/default.

Existing EntityObservation.Scope compares original RNG state and invalidation along with its current clock/entity/identity facts. Existing provider/target/space/codec/fault boundaries remain. No duplicate validator, compensation, cached random result or whole-world copy.

World.SyncCore writes/hashes the original ulong state without invoking its setter. Read mode rejects zero with CorruptStateException, then restores the same state. Save/replay layout remains154/1; build2.499.0 rejects older builds rather than adding migration compatibility. Transient invalidation counts differ across restored worlds but do not affect gameplay output or hash identity.

Four production sources change: Math/DetRandom.cs, Kernel/EntityObservation.cs, Kernel/World.cs and Kernel/SaveGame.cs identity. Sole game clock and all12 Tycoon sources remain unchanged. No Oil/platform/Draw or business-rule changes.

## Proof coverage

- File-authored seed31, initial15517599431202433770 and four exact known outputs:18281420769787665529,17546502330705449244,15643592069812409975,5764732886259472402. Original algorithm values unchanged; per-draw invalidation, equal writes, invalid zero/bounds and continuation from saved state verified with a standalone primitive, not a game model.
- Actual healthy save bytes are altered only at the independently identified unique world-section RNG field, then raw SAVE checksum resealed. Restore specifically refuses random stream has zero state; existing session hash stays untouched. No copied loader, direct gameplay state writes or checksum-only false proof.
- Twelve independent JSON direct/raw cost sessions: draw, draw-and-restore, equal assignment and zero assignment refuse/latch first faults; nested original World.Hash and SaveGame.Write remain legal/pure. Last healthy viewer publication and idle-session isolation hold. Existing public Guard helper reused, no old registry case executed.
- Three actual independent PulseTransformer ReadTransformation callbacks mutate RNG only after real Update and explicit host pause. ReadExecutionPreview returns no out-fact prefix; prior detached preview/last healthy own/foreign spatial publication remains, next Update preserves first fault and clock, corrupted session cannot save, idle session unchanged. JSON snapshot fault is negative instrumentation, not gameplay.
- Four existing public-only RecipeDisclosure physical workflows run native/direct definitions at minute/half-minute quantum, with real refusal/discovery/manufacturing, actual supply/earned work/output, pause/saved twins, immutable retained previews, replay and session isolation. Same helper, no copied scheduler or old registry execution.

Existing independent navigation/pulse proof owners are extended in place. TransformerTests factory merely accepts authored replacement of its already master-listed file; old defaults/assertions/registrations are unchanged. New driver has no World/TestKit/RunTicks/balance/progress execution path. New embedded JSON vector and exact profile entry are verification metadata, not engine defaults.

## Commands and selection metadata

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Observation random purity: scoped draws restored writes and saved physical previews" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --profile-info tests/Beep.SimEngine.Tests/testdata/framework-validation-profile.json
```

One compile and one new bounded runtime attempt, no repair runs. Profile-info698 registered/included,0 excluded,0 game suites executed, exit0. Selection metadata is not a full executor/runtime pass. No old/family/full suite. User-authorized one final full regression remains deferred until implementation finishes.

## Source freeze / remaining work

Post-pass freeze2026-10-12 17:12:06 UTC:1794 source anchors/1007 selected executable inputs/13 quickstart anchors match actual source with0 drift. Inventory2978 files/1067 Markdown documents/8758 local links/0 broken links;18 valid master rows;221 original literal gate/line anchors validated with0 errors. Original189 checked/32 open unchanged, additional230. Eleven previous executable inputs changed; two newly created proof inputs and previously existing Math/DetRandom.cs added to freeze/source anchors. Four existing production files change, no new production source or authoritative layout. Sole clock/all12 Tycoon sources unchanged. git diff --check exit0 with34 pre-existing warning lines;2966 dirty entries/0 staged. Final documentation annotations hashed separately; no executable edits after FIRST/FINAL. No original checkbox/text/line-anchor changes, full goal/plan and broader acceptance remain open; no old/family/full registry run or staging/commit/push.
