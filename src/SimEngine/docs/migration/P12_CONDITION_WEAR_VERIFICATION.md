# P12-D2 condition wear — scoped verification

Date: 2026-10-06. Runtime: Beep.SimEngine only. Build **beep-simengine-2.100.0**, save **64**.
[Stage](../../plans/enhancements/P12_CONDITION_WEAR_DESIGN.md).

## Implemented ownership

The existing named condition provider/Sync now owns condition, last change tick, cumulative eligible
exposure and last accepted exposure tick. No second value, ledger, queue, subscriber or scheduler.
IConditionWearDefinition/Provider/Rule extend the existing contracts/catalog. Native equipment_condition
requires explicit wear:null or a real resolved rule identity. Master-listed condition_wear_profiles
select exact native units, positive quantity/rational period and an explicit shared conditions array.
There is no assumed idle, travel, weather or operating wear. Independent game rules derive from the same
public rule contract without editing the scheduler or acquiring friend access.

QuantizedGameRates owns exact cumulative raw quantity/time arithmetic extracted from ResourceRates;
the latter preserves its native catalog/capacity/Bill checks and floor/ceiling behavior. Wear subtracts
wide cumulative totals before narrowing only bounded damage. Fractional, discrete and rational subsecond
session quanta retain exact continuation. Eligible zero-quantum steps earn exposure, not fake damage.
Authored wear explicitly saturates at actual remaining condition; manual damage/repair still refuses
invalid bounds/quantum without clamping. Physical repair does not erase rate quantization history.

The existing EquipmentConditionSystem at370 captures every live provider's eligibility/decision before
any wear mutation, then commits and validates actual provider state. No same-pass signal feedback or
duplicate exposure at a tick. Current codec2/view2/system2 replace their prior schemas directly; signal1
and other owner payloads remain unchanged. System payload remains empty. One shared structural exposure
predicate serves native staged decode and live observation; future timestamps validate against actual
session time at read/checkpoint/post-restore. No old loader or missing-field default.

Immutable company-private ReadWear reports actual profile/unit/exposure/next damage or typed blocker.
Unavailable signals preserve the original source blocker. Existing condition signals/operation gates
pause real WIP; entity.Update alone commits authored entity state. A paused in-flight transformation
retains its Working phase with a typed blocker and no progress, not a fabricated replacement execution.
Existing WorkBoard/DAG/motion/material/tool/once-only completion own physical repair and cancellation.

## Final commands and evidence

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Condition wear: authored rates, exposure contracts and checkpoint guards"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Condition wear: signal-gated degradation, physical service and saved continuation"
```

- Build: zero warnings/errors, **3.52 s**.
- Contracts/rates/checkpoints: **73 passed, 0 failed**, 498 ms, one exact suite.
- Physical service/saved continuation: **28 passed, 0 failed**, 603 ms, one exact suite.
- **101 total passed, 0 failed, 0 aborted suites**. Repairs reran only these same two names.
- An initial physical assertion incorrectly expected the completion/output Blocked phase for a paused
  Working execution; corrected against the existing transformer gate path and additionally verified
  retained WIP/progress. Initial test API compile mismatches were repaired before running these suites.
- 34 other identity assertion files and D1 fixtures/codec instrumentation migrate/compile only.
  No preceding, third, family, long acceptance or full-regression suite was executed.
- Scoped tracked-file diff whitespace check passed (only Git line-ending notices).

The contract suite covers exact catalog/unit aliases, required/null/missing profiles, typed gates,
positive rates/periods, native quanta, pure/private/detached views, fractional accumulation, exhaustion,
entity-state settlement, discrete zero-loss exposure, preserved repair history, staged malformed decode,
future checkpoint/restore rejection, no-rule chronology, wide cumulative totals, stale/same-tick admission,
independent public rule/dishonest observation modes, mapless/no-work composition, rational subsecond
save continuation and shared resource floor/ceiling billing.

The physical suite uses normal engine player/environment commands: queue a process, run real updates
until wear stops unsafe production, disable/restore eligibility and make the source unavailable, then
post physical service. Real crane travel/work spends three spares and holds two tools. Partial service
cannot repair; cancellation releases only unspent tools with no refund. A fresh service pays again and
repairs once. Saves during service preserve WIP/exposure/claims; interleaved worlds retain equal hashes.
Repaired production resumes and re-enabled wear earns the next real exposure. A two-provider test proves
complete-pass signal capture; an impure signal source faults the actual Update. Every workflow wait is
bounded at700 steps and reports tick, condition, wear blocker and process blocker if stalled.

## Source fingerprints (SHA-256, relative to framework root)

```
485c6beab3624efde08d188dfd9fe3a104b9b6797e82c7db480d8315c5edd029 src/Beep.SimEngine/Core/QuantizedGameRates.cs
7691c717993be8ecfe0449e8ea83fee4424b3f26c6557bf042ae0f3a055465b3 src/Beep.SimEngine/Core/ResourceRates.cs
f4ad9155537be64d768fcb97357cec0ceb078a9251660d71af86fb385115af13 src/Beep.SimEngine/Standard/ConditionWear.cs
2f145a1fb86f44ae374845304e38364ed2e7025c20bec09f666687696471409f src/Beep.SimEngine/Standard/EquipmentCondition.cs
8cf0e204e3fec17a33b604bbbc7edaade278a1ffce9af73dd1dbe7dee680135c src/Beep.SimEngine/Standard/EquipmentConditionModule.cs
f9e5698c37467990a0bb9ac3f43f0194f0d7fcf92169686ac018318bb799200e src/Beep.SimEngine/Views/EquipmentConditionView.cs
678f1e7787596274d76c19033438263cd12ef9be99741ed0bb0374e3381241e3 src/Beep.SimEngine/Kernel/SaveGame.cs
d845492ff8fd04b5864212f470ace4647ead6569875edd852561e4977ddf3cdc tests/Beep.SimEngine.Tests/ConditionWearTests.cs
2a284715ef156848b129822e6c625cf7a1b697ef1e4a611f7f9b9d7c3d94c3c1 tests/ExtensionProof/ConditionWearProof.cs
27270656bb86c6ce406072ddfb35d47e96c7f6e181c0b254dc500dc75232b751 tests/ExtensionProof/content/condition-wear.json
```

## Remaining scope / limitations

D3 automatic inspection/service posting, operating/fleet/preparedness reserves and policies, incident
condition effects, arbitrary operation gates, contamination layers, scientific/weather/chemical wear,
provider replacement/reference migration, broad independent wear-provider commit faults, replay/lockstep/
fuzz/stress/soak/allocation acceptance and full P12/P1–P12 remain open. Scoped pure checks cover actual
entity/tick/allocator, not every cross-service mutation or global transactional restore rollback.
Authored native condition quantities still use Fix's actual finite representation; no invented gameplay
cap is added. No OilFieldDays, Tycoon, host/demo/harness, Unity/Godot/platform or Draw changes.
The master framework goal remains active and partial; this evidence is not full completion.
