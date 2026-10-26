# P6-I3 nonspatial process-game progression — focused evidence

Date2026-10-15. Engine2.605.0/save159/replay1 unchanged. This implements the
nonspatial research/objective integration missing from original P6 Game A.
Original broader P6.acceptance.008, spatial/mod breadth and full goal stay open.

## Authoritative implementation

The separate non-friend ExtensionProof assembly already contains ProcessProofModule
and its SingleResourcePort contract implementation. Its original process.json
supplies the resource,20 starting material, capacity, three authored entity states,
assembler, six-minute recipe, token output, minute quantum and native startup.
None of those definitions was copied or changed for this stage.

New process-progression-master.json explicitly lists all eleven existing modules,
the original process.json, access/work-retry documents and the new progression
document. Those two new files are explicitly embedded. The new progression file
authors one two-stage timed technology with a typed two-material requirement and
recipe unlock, then a dependent research/production objective chain. Native outcome
effect ends the game only after two actual manufactured tokens. Scenario settings
explicitly select native startup, work retry and objective enrollment.

No C# gameplay implementation was added to the external module or engine. No new
catalog, processor, objective/knowledge store, work scheduler, clock, port or stock
owner. Original contracts/content already support the workflow; the test driver
only composes them, submits normal player commands, updates and reads public views.
It uses neither mutable Game.World, private reflection nor runtime stock/entity writes.

The game is genuinely mapless: no spatial provider/coordinates, tile navigation,
logistics or Tycoon selected. Player identity scopes knowledge/objective enrollment;
it supplies no business, prices, budget, company economics or oil calendar.

## Executed chain and invariants

1. Start with original20 material and no tokens/knowledge. No foreign objective
   enrollment is fabricated. Locked build preview and ordinary command agree,
   creating neither queue nor output.
2. Submit a persistent research plan, earn actual work and consume exactly2 input.
   Change priority30 to70 and pause through ConfigurePlanCommand. Immutable original
   and restored plan views expose70/paused. Eight interleaved updates preserve all
   research node progress, stock18 and unlearned knowledge in both saved twins.
3. Another session starts its own research through the same commands, genuinely
   advances its clock/hash and earns its own knowledge while the first is paused.
   A separate GameClock pause freezes the first session's simulation age.
4. Resume the original plan; saved twins earn completion. Retained unlearned
   snapshots remain detached; another player's knowledge remains unknown. The
   research objective completes, but no token or terminal outcome appears.
5. Other session queues one batch; first queues two. Original productive state is
   visible through Entity.Update. Disable the real named operation during productive
   work using ConfigureOperationCommand; save/load and eight interleaved updates
   retain exact earned progress and stock, with authored blocked entity state.
   The other session finishes exactly its own one token/stock16 and does not win.
6. Resume operation normally. First token leaves the second sequential batch and
   objective unfinished. Continue through another saved checkpoint until second
   token and dependent objective effect set process.completed. Final stock14 is
   exact:20 minus2 research and two net-two recipe batches. Queue empty, both
   objectives completed, terminal save/load and canonical player replay hash equal.
   Other session remains independently unfinished with one token/stock16.

All staged updates compare loaded-twin hashes. Waits are bounded120 updates and15
wall-clock seconds for diagnostics; the test does not implement game-time work.

## All build/runtime attempts

Targeted build command:

`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`

- First build clean11.32s, terminal exit0/session24066 before runtime.
- First new exact case67/0,869ms; PID23588 exit0/1065ms. No abort/truncation.
- Added direct public restored-plan priority/pause and productive-state assertions;
  second targeted build clean5.19s, exit0, zero warnings/errors.
- Final same new exact case70/0,856ms; PID22104 exit0/1069ms. No abort, timeout
  or output truncation. No executable edits after final.

Runtime command:

`dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Nonspatial process game: authored research, paused plans, operating control and objective replay" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 4500`

Only this new literal ran (twice as its assertions were completed). No full,
profile, broad family or historical case executed. P6-I2's128 and P9-L2's858 are
historical scoped checkpoints, not rerun/countable as current-stage verification.

## Limits and next original work

Game A's composed nonspatial progression scenario is now covered, not the entire
P6 A/B/C requirement. Spatial congestion/replanning, alternative role providers,
the complete separate-assembly worker/project/endpoint/signal/response proof and
other original broad durability/privacy/provider/fault/global gates remain open.
Interleaving is proved for these selected eleven modules/commands only, not the
original all-optional-module isolation requirement. No fuzz/lockstep/stress,
arbitrary map-provider or optional business behavior was verified here.

Original broad checkboxes and historical221/195 recorded checked/26open counts
are unchanged. No production framework implementation, Oil/Tycoon rules, host/demo
gameplay, platform/Draw rendering, arbitrary spatial limit or compatibility path
was added. No staging, commit or push; full goal remains active.
