# P3-S20 — actual selected spatial motion dependencies

## Outcome and scope

Engine2.417.0/save154/replay1; all state/view schemas unchanged. Scoped motion-provider admission verified. Original221/157 checked/64 open unchanged; P3.acceptance.004 and full P1–P12 remain open. This is not proof for every arbitrary spatial provider/capability, every optional module or full regression.

Native component registration already rejects an inactive registration provider in [ResolvedContent](../../src/Beep.SimEngine/Content/ResolvedContent.cs). Independently registered `ITileMoverDefinition` can reuse the native Mover without selecting native TileNavigationModule; its active registration module is not the routing capability itself. The consumer-side check now prevents that capability from starting without its required router.

[MotionNavigationContracts](../../src/Beep.SimEngine/Standard/MotionNavigation.cs) is now the sole entity metadata admission owner for tile mobility, selected presence and initial/allowed route classes. Actual navigation metadata validation precedes class enumeration, so malformed independent choice lists refuse ContentException rather than a null-reference failure. Existing provider observations and selected-space filtering remain. [NavService](../../src/Beep.SimEngine/Standard/Navigation.cs) retains sole routing/class/search ownership and registration freeze, checks its actual session/tile space, and no longer duplicates the entity metadata loop. No new engine service/state/contract, update phase, clock, default geometry, fallback route or compatibility path.

## Builds — all attempts retained

1. `dotnet build Beep.SimEngine.slnx`: exit1,13.90s,0 warnings/1 error. New driver treated `SimGame.Modules` string identities as objects with `.Id`; engine/proof/other dependencies compiled.
2. Same-case project repair `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj`: exit0,6.46s,0 warnings/0 errors, all five dependent projects compiled.
3. Same-case fixture repair, same project command: exit0,4.97s,0 warnings/0 errors, all dependencies compiled.

No final full-solution compile rerun. Clean current project/dependency compile does not replace the recorded failed solution attempt.

## ONE NEW exact bounded case — all runtime attempts retained

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Spatial composition: selected tile motion requires actual providers before initialization without map fallbacks' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

1. FIRST:156 passed/5 failed/0 aborted; suite294ms, PID24164, exit1, observed449ms, untruncated stdout/stderr, observer empty. Four wrong fixture assertions expected stopped after arrival, but native Mover supplies a moving state during travel and stopped only when unreachable; arrived selects authored idle. Local-only composition also retained a native component while deselecting its registration module; the original loader correctly rejected it. Repair only the new driver: assert native idle semantics, and use independent active-registration definitions for deliberately unselected-space content. No game-rule change to satisfy the fixture.
2. FINAL:168 passed/0 failed/0 aborted; suite1409ms, PID35692, exit0, observed1566ms; untruncated stdout/stderr, observer empty.

Limits:30000ms runtime/5000ms owned termination/30000 output characters;96Update bound for tile travel and256Update bound for existing point physical production. No previous/family/full/Oil/Tycoon/platform runtime and no executable edits after FINAL.

## Contract/file-based evidence

The [new driver](../../tests/Beep.SimEngine.Tests/SpatialCompositionTests.cs) uses one explicit four-module [master](../../tests/ExtensionProof/content/spatial-composition-master.json), separate [world](../../tests/ExtensionProof/content/spatial-composition-world.json) and [route](../../tests/ExtensionProof/content/spatial-composition-routes.json) files, and [registration/negative composition instrumentation](../../tests/ExtensionProof/SpatialCompositionProof.cs). Existing independent geometry/mover declarations reuse native TileSpaceDeclaration/MoverDeclaration/Mover codecs. The missing-space declaration creates no provider; the foreign-router probe creates no healthy gameplay. Native WorldSetup alone creates/places entities.

32 authoring/composition variants combine native/independent tile root and mover metadata with missing selected space, tile navigation with no selected map, inactive navigation, missing initial/allowed route class, unused selected-space mover with empty startup, wrong-session router and malformed navigation choices. Every refusal occurs with initialization count0. Native inactive components reject at original loader admission; independently registered components reach the required consumer validator. No missing provider is fabricated.

Four valid native/independent combinations execute ordinary MoveVehicle/ConfigureMotionNavigation commands and pure previews. Physical obstruction, nonzero partial travel/remaining game time, entity Update moving→idle state, navigation replan, detached interpolation facts, exact arrival, pending/partial/terminal saved twins, replay and interleaved identity/position isolation pass. A nonspatial local session excludes navigation, retains no map, and saves validly even with unselected independent spatial content. Existing nine-module point composition remains tile-router-free and physically hauls/smelts its actual inputs through ordinary queues; per-update original stock/WIP conservation, twins and replay pass.

All gameplay stays with original map/router/motion/placement/entity state/stock/transformation/clock owners. Drivers contain authoring inputs, commands/views/clock/saves/assertions only. Broader spatial provider/layer/resource/sensor/operating-area, private query, lifecycle and performance/full acceptance remains open. Protected G19 and the sole clock are unchanged; no Oil/Tycoon/platform/renderer/Draw/adapter/legacy/parallel-owner changes. No staging/commit/push before the human's full goal AND plan completion condition.

## Current source freeze

After FINAL, UTC2026-10-10 02:07:02 UTC: 569 selected executable inputs;11 changed/new versus564;5 new executable selections (one thin driver, one negative/registration proof, one master and two authored documents). All selected hashes are recorded in the machine audit; historical input hashes are refreshed without historical runtime. All221 original requirement words and checkboxes remain unchanged;204 source line anchors shift only for the new checkpoint headers.

## Read-only metadata/post-pass validation

UTC2026-10-10 02:09:04 UTC:1226 source anchors/13 quickstart anchors match current hashes;2501 selected paths/860 Markdown documents/7188 local links,0 broken links.18 master rows retain5 columns. git diff check exits0 with34 pre-existing output lines; new driver forbidden-boundary scan0 matches.569 selected executable inputs have0 post-FINAL drift. Actual `git status --porcelain=v1 --untracked-files=all` reports2491 dirty files/staged0. Sole clock and all three protected G19 source hashes match the prior checkpoint. No historical runtime was executed by these metadata checks; full goal remains active.
