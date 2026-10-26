# P5-A12: Oil authored navigation — Focused Verification

## Result and boundary

Core2.537.0/save154/replay1 unchanged. Canonical Oil terrain/water/road metadata and land/sea rules now use original SimEngine declarations/interpreters. No production framework executable source changed. Oil integration still fails10 compiler errors; no native full Oil content/gameplay validation. Original221/193 recorded checked/28 open unchanged; additional276. No original gate closure, staging, commit or push.

## Source changes

Removed MovementClassDef/movement_classes and manual map-layer/profile creation. Oil code/master require sim.tile-conditions (9 direct requirements); code/master contain16 modules. Oil master lists20 documents including oil/navigation.json. Scenarios already select Oil and rely on declared transitive dependencies; no scenario edits needed. One attempted scenarios patch failed without changing that file.

Native public byte layers use explicit oil.tile-code and metadata. Ten native band conditions and two native profiles retain terrain costs and paved/dirt road precedence; unknown terrain6+ is blocked explicitly. Units/costs are authored Oil choices, not platform or engine size caps. No private knowledge layer or parallel navigation owner.

## Compilation

- Focused test-project build1: clean,0 warnings/errors,11.39s.
- OilCo.Game scoped build: exit1,0 warnings,10 errors,1.85s (15 previously). Remaining: Commands.cs8 founding calls and OilModule.cs2 unresolved FreeEconomy/LineService. Changed navigation/settings files have no new diagnostic.
- Focused build2 after JSON repair: clean,0 warnings/errors,12.44s.

## Exact case

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Oil navigation: canonical JSON native costs layers and saved scenarios" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 100000
```

Attempt1:0 passed,1 failed,0 aborted;118ms case/342ms outer,PID41912,exit1. Native spatial declaration correctly refused resource:null because an optional association must be absent or a string. Removed that field from all three canonical Oil layer definitions; no core fallback introduced.

Attempt2 FIRST successful/FINAL:1,435 passed,0 failed,0 aborted;401ms case/648ms outer,PID32748,exit0. Both attempts terminal and untruncated.

Coverage: original native composition admits actual linked Oil navigation/spatial files; six existing geometries; public sparse layer samples; plains/forest/marsh/desert/tundra/mountain/wet/sea/lake/unsupported terrain; paved/dirt precedence; water range; minimum-cost calculation; out-of-bounds refusal; read/hash purity; original saved load/update twins; interleaved independent sessions.

Only this one new case ran (twice for its own repair). No old/family/full suites. No executable edits after successful attempt. Fixture setup is native authored engine initialization; proof view delegates to original NavGrid; driver uses public session/view/clock/save/hash APIs only. Canonical sibling files are linked, not copied into another production dataset.

## Not verified

This is native Oil DATA admission in a stripped consumer proof, not full Oil module/runtime admission. Physical movers/routes/haul/construction, knowledge privacy/restore, complete Oil saves/replay/lockstep, rival parity, all providers and stress remain open. Existing mover/worker/producer payloads and infrastructure/founding/economy migrations remain pending. Tycoon business and clock unchanged.

## Metadata verification

Current audit:2,015 source anchors,13 quickstart anchors,1,157 selected executable inputs, no hash mismatch or post-success executable drift. All221 original criterion texts/checkboxes/line anchors unchanged (193 recorded checked/28 open), additional276; no original closure. All18 master rows valid;8,663 local links, zero broken; new driver has no state-mutation boundary matches. Canonical diff check exit0 (34 warning/output lines),3,124 dirty paths preserved, zero staged. Core production source, GameClock and all12 recorded Tycoon sources unchanged. Two refused patch attempts retained: unnecessary scenario module insertion (scenario selection is transitive) and audit-entry comma context; neither changed its target before repair. Goal remains open; focused tests ONLY including final, no full regression authorized or run.
