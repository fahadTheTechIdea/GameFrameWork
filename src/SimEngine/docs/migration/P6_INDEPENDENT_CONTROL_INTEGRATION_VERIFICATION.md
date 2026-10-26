# P6-I2 independent control/response integration — focused evidence

Date2026-10-15. Engine2.605.0/save159/replay1 unchanged from P9-L2. Full goal and
original broad P6 acceptance remain open; this closes the existing control scenario's
specific physical-layout failure, not all independent-game/mod requirements.

## Actual cause and correction

The historical35/1 failure at update2005 was physical: the carrier started between
the extractor and the research/incident stations in a no-passing line. Moving or
finishing work instantaneously would have hidden it. Later parallel manufacturing/
construction at80 would likewise require opposing machines to cross.

The master-listed control document now authors one empty parking structure at100
and an ordinary startup carrier there with that home. Original entities remain;
no machine was removed or given multiple roles, and the only starting stock remains
one research spare plus one repair spare. No duplicated stock/provider/scheduler.

The shared Planet X mine's original dock has extractor service19/departure18 and
ore service21/departure22, plus separate waiting places. The original beacon and
its temporary construction site declare builder and alloy bays on opposite sides
using P9-L2's same role/resource metadata and independent geometry provider.
Their prototype positions are30 so all declared offsets fit even before runtime
placement; zero/default positions are not used to bypass content validation.

The test simulates a deliberate player sequence, using only public commands and
views: configure safety automation; observe physical detection and technology
blocker; post research; observe repair/restart and physical home return; enqueue
six refine batches and six extraction orders; wait for all output and carrier home;
manufacture one builder; clear its home through SetHomeCommand; construct at30.
Builder has no home while working, so automatic home return does not oppose the
carrier's supply route. The engine still decides admission, jobs, material supply,
reservations, motion, productive time, incidents, automation actions and completion.
The driver contains no game-rule, transfer, scheduling, cost or state-transition
implementation. Authored module gameplay stays inside the framework's external
game module, not a host or demo.

## Full original chain retained

The exact original control case still verifies:

- Non-tile composition; no Tycoon/business feature or dependency.
- Exactly two initialized alloy spares and12 finite underground ore.
- Pure private knowledge/signal/incident/pressure views; foreign incident/schema refusal.
- Unknown research locks assembly and construction with preview/command parity,
  creating no completed machines/projects from rejected requests.
- Pending unsafe control action saves; clock pause freezes it; only a later update
  disables the actual refinery. Retained views remain detached.
- Real detection reaches the technology blocker without consuming repair stock.
- One finite pressure command/episode, spent1 and available0; no hidden refill.
- Staffed two-minute research, partial save, exact spare consumption and private
  technology completion; knowledge alone cannot resolve the incident.
- Staffed repair/restart, partial save, positive episode duration and exact spare
  consumption; safe enable is deferred to the next update and survives saves.
- All six finite extractions, native timed refinery batches, physically loaded
  resource transfers, manufactured builder and actual supplied construction.
- Original final provenance totals: initialized2, spatial transfer12, produced6,
  consumed20; no AuthoredCreation/StockPurchase, zero remaining ore/alloy/deposit.
- Saved-twin hashes during every staged wait, finished save/load, canonical recorded
  player replay and another unchanged session.

New observations also require all four dedicated extractor/ore/builder/alloy bays
to appear in actual jobs. Every observed update checks strict digger < carrier,
and digger < builder < carrier after manufacture; overlap or crossing fails with
public diagnostics. Carrier remains physically at100 during research/response.
Existing2000-update/30-second per-stage diagnostic bounds and60000ms outer timeout
were not increased. Extra checks do not move or repair anything.

## All build/runtime attempts

Targeted builds use:

`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`

1. First content/driver build clean7.15s, exit0. Before runtime, review identified
   that new negative beacon dock offsets need an explicit valid prototype origin;
   changed beacon/site positions0 to30. No invalid-content runtime was launched.
2. Content repair build clean6.23s, exit0.
3. Existing exact control literal126/0,17,327ms; PID10640 exit0/17,483ms.
4. Added explicit per-update spatial-order, real dedicated-bay and parked-carrier
   observations; targeted build clean5.23s, exit0, zero warnings/errors.
5. Final same exact literal128/0,25,913ms; PID16672 exit0/26,128ms, no output
   truncation, abort or timeout. Running handles96843 and84561 were each polled
   to terminal; neither was restarted on a yielded observation.

Runtime command:

`dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Planet X control acceptance: authored research, deferred safety control and physical incident recovery" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 4500`

No executable edits after final. No full/profile/family runtime and no other old
Planet X cases rerun; shared JSON changes therefore do not prove every prior
scenario's expectations remain valid. Prior P9-L2 physical/checkpoint858 and compiled
audit10 are separate scoped evidence, not rerun or counted as this scenario's checks.

## Remaining scope

Original P6.acceptance.008 still needs the complete nonspatial/spatial/mod breadth,
alternative providers/roles, congestion/replanning and other explicit original gates.
The full industrial acceptance's objective/rival/emergency-policy breadth is not
proved by this scenario. Other unchanged-session checks here prove no mutation,
not the separate requirement that every optional module advances in interleaved
sessions. No native filtered-queue/recovery/fault/stress or broad durability claim.

Only this scenario and P9-L2 integration substage are closed. Original221/195 recorded
checked/26open historical counts and original broad checkboxes are unchanged.
No engine implementation, Oil/Tycoon rules, host gameplay, platform integration,
Draw/rendering, arbitrary map-size limit or Git publication change in P6-I2.
