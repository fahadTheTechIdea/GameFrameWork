# P5-ST1 — Unshortened supplied survey timing

2026-10-16, unchanged engine2.639.0/save160/replay1.
[Design](../../plans/enhancements/P5_CANONICAL_SURVEY_TIMING_DESIGN.md).
One existing exact case, first/final attempt: **4903 passed /0 failed**, no abort/timeout.
Original221 requirements remain210 recorded checked/11 open; P5.005/full goal remain open.

## Change and ownership

Actual consumer root:
`C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine`.
Only executable source changed: `OilCo.PublicProof/ActivityContractTests.cs`.

Removed its six-minute projection of survey_2d_work. Actual work.json still declares
36h/2160 minute work and ten survey charges; seismic_truck still has rate1/native motion.
Same registered case, factory, Step/Send, ordinary preview/submit, physical hauling/work,
Oil effect and native history. Reuse HeadquartersRecoveryTests.Total for actual owned
stock facts rather than add another resource aggregation/helper implementation.

The fixture retains its existing24x24/two-seat Sandbox, seed17, seismic/haul trucks and
one-minute frames. Native terrain, reservoir settings, worker/motion/handling rates,
requirements and original opening stock are unchanged. Authored Sandbox has80 survey
charges: physical transfer conserves them and productive work consumes exactly10,
leaving70. Money is absent. No stock, position, progress or knowledge writes in the driver.

## Actual physical sequence and durability

- Posted at2, original order1 and stake identities returned by native command.
- Real carrier loads8 by14, ten by26, moves while loaded through26/38, and unloads
  progressively (7.333 still carried at50). The test observes current-interval physical
  displacement, not a stale old position or merely a loaded stationary truck.
- Seismic machine travels after delivery through62/74/86; first earned work is observed
  at91. Partial work reloads both sessions through normal saves.
- Every active node reports original2160 required minutes; periodic12-update full hashes
  agree, with stock totals restricted to80 or70 at those checkpoints. No duplicated/lost
  bill is observed there; this is not a separate every-update transaction stress proof.
- At2159 productive minutes, the original seismic machine owns the job; native productive
  duration also reports2159 minutes and there is no completion. Save/reload both worlds
  at this exact final incomplete minute; held detached facts remain unchanged.
- The last interval[2249,2250) completes that same order/stake, records history at2249
  and advances the sole published clock once to2250. Native effect retires the stake;
  completion survives save/load and the other company cannot read its private history.
- Three further one-minute updates reach2253 with identical twins, stable original history
  and70 charges, not another spend. An untouched posted sibling session remains unchanged.

Resolved executions are not artificially retained. The proof reads native completion history
after pruning and follows the existing executing-interval timestamp convention, not an
alternate clock or duplicated completed-node model.

## Build, exact invocation and result

Actual consumer working directory above:

```powershell
dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v minimal
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Oil activity contracts: detached survey order physical supplies saved progress and completion" --timeout-ms 180000 --termination-timeout-ms 5000 --output-chars 16000
```

One clean target build1.38s, zero warnings/errors. One runtime attempt:4903/0,
suite71758ms / bounded process71860ms, child29536 / parent15552 / exec session77401.
Exited/exit0; stdout/stderr untruncated. Native workflow stage70019ms. Actual handle polled
to terminal, no restart, failure, retry, profile/family/full command or historical case rerun.

The new unshortened workflow limit is240 handling/travel minutes plus the2160-minute
goal (2400 updates total), with120000ms inner and180000ms external bounds declared
before the first attempt. These bounds were not enlarged after failure. This replaces
the obsolete240-update whole-workflow limit for six-minute work; it does not change
normal simulation limits. Detailed arrival output remains; repetitive productive output
is reduced without removing12-update hash/resource checks or per-update node checks.

## Identity and remaining scope

444-file core path/content SHA256 unchanged:
`0327e6ff4473656c75e2e8a889d804e5cc8d8029b027065aa772b5bb27b7973a`.
Actual framework DLL/consumer copy unchanged:
`E811B410F353520502501CF34A6E848B47EB67F0B72DEA42E1E645D3ADA0A6D7`.
Actual OilCo.Game unchanged:
`2E0C64518B20CA4CCB0AB1FC459BC52D6DE3BACC18742F97C58CD652049DFFA1`.
Final proof source / Release DLL:
`6D463ED6C4FBBAAFAEE23A2FD2ADC9D935DB6C49C99A9BCC6BD7713B5F3C2C15` /
`E75EDAB4ABBD68D83E1A3D6FEADA1196E34E00CB64E72FBBF54A0B430D9C92D4`.
Unchanged actual work.json:
`AD180D3F42EE354648EF50C727D5CC09F65DF3FFCB5869273B54C7BF99496D80`.
Pre-edit proof source:
`B4CCB39E30191BD59F418100EFFF0F7E4719CB68FF5760A764B04438FF67292B`.

No engine/gameplay/content/business/platform/Draw/adapter change or protocol bump.
The lifecycle skill guides staged evidence/tracker updates; the user's focused-only
rule overrides its generic full-regression suggestion. Earlier six-minute results remain
historical, not canonical timing evidence. Research/manufacturing/drilling/recovery results
remain separately scoped and are not rerun or added to4903 here.

This supplies missing actual canonical survey timing, not all scenario/profile/marine/
incident/route/fault/replay/stress or full-stage acceptance. Next reconcile original
P5.005's complete temporal authoring/workflow matrix and identify any remaining concrete
gap; do not turn these selected examples into a full-profile claim. No original checkbox
or audit text changes. No memory, staging, commit or push; both indexes remain unstaged.
