# P3-S26 verification — 2026-10-14

Current build2.577.0/save157/replay1. Native TilePresence composition/movement use ITileSpatialSpace/ITileRelocationSpace, not TileMap services. Exact authored dimensions are checked through the original geometry declarations. Original occupancy and native coordinates/history/state/clock own execution. Successful relocation callbacks are checked against the original source codec/entity/clock observation before coordinates publish.

The independent module now uses native tile_presence and WorldSetup JSON. Its custom presence definition/runtime and Starts stage are deleted, not shimmed. A fourth explicit master-listed startup document selects ordinary setup entries. The external proof assembly has no friend access, concrete map wrapper, alternate optical algorithm, coordinate runtime or startup scheduler.

## Builds and attempts

Only tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj was built with --no-restore. Clean builds7.15s/6.07s preceded first verification. Initial independent60/1 case (274ms, PID27068/exit1/observer449ms) compared a cached last-update player snapshot against newly restored current presence after a direct coordinate protocol write. Native physical regression64/0 passed in3295ms, PID25896/exit0/observer3483ms. The fixture was corrected to compare current presence facts and independently verify Update-driven snapshot publication; no engine fallback/refresh was introduced. Clean5.09s compile and independent63/0 (195ms, PID8216/exit0/observer351ms) followed.

Authored dimension parity and source/clock mutation guards were then added before final validation. Final targeted build7.61s, zero warnings/errors. Final exact cases, each --timeout-ms30000/--termination-timeout-ms5000/--output-chars9000:

- `Tile sensor contracts: independent sparse space, native control and guarded samples`:73/0 in166ms, PID26516/exit0/observer327ms.
- `Sensor occlusion: authored screens, physical sight and saved continuation`:64/0 in3139ms, PID18488/exit0/observer3302ms.
- `Spatial presence composition: unused geometry contracts require actual providers before initialization`:81/0 in1923ms, PID25984/exit0/observer2079ms.

No final truncation or observer errors. All processes terminal; no executable edits after finals. These are three selected cases, not a profile/family/full run.

## Scope

Independent proof covers native JSON presence/setup, independent selected storage, optical disclosure/control, actual sampling/revisions, saves/session isolation, blocked/out-of-bounds/foreign relocation refusal, coordinate/interpolation codec continuation, Update-only settlement/snapshot publication, and source/clock mutation faulting before coordinate publication. Direct coordinate writes are low-level protocol checks, not physical player movement acceptance. Native physical regression covers actual ordinary commanded travel through the existing mover. Composition regression covers selected/unused and absent geometry, malformed callbacks, native tile and independent point continuation.

Native presence movement is contract-neutral now; independent pathfinding/work/utility integration and every provider/lifecycle/privacy/replay/lockstep/stress/allocation matrix are not proven. No original broad gate closes, no full-goal claim. The consolidated A47 metadata/hash report remains historical. No Oil/Tycoon/platform/host changes or Git staging/commit/push.
