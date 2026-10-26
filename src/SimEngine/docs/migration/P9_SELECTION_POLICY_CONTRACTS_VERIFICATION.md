# P9-B14 direct selected policy contracts — scoped verification

2026-10-09, canonical Beep.SimEngine worktree. Core2.398.0/save154/replay1; persistent layouts unchanged. Full original goal remains active. No staging/commit/push.

## Authoritative change

Deleted FleetPolicyDef and ActivityFacilityPolicyDef. Native and existing external content implement IFleetPolicy/IActivityFacilityPolicy directly on Def. Standard behavior still delegates sole FleetPolicyDeclaration/ActivityFacilityPolicyDeclaration. MachineFleetService owns named membership; WorkBoard owns facility admission and physical execution. No parallel policy/stock/worker/clock state.

Original fleet callback boundary observes frozen actual catalog alias, existing fleet.Sync, actual whole member.Sync, scalar clock/identity and selected tile-map version. Successful verification precedes publication; only the transient reentry flag is cleaned in finally. Facility metadata/rank observations include actual selected worker/target codecs; recurring compatibility reads Node once outside ordinary incompatible-node handling. Thrown callbacks retain their typed first error and faulted sessions cannot save/hash.

## Build evidence

Nine clean completed-slice builds, zero warnings/errors:14.36s,6.69s,4.67s,4.57s,4.49s,4.24s,4.47s,4.96s,FINAL4.68s. Historical NamedFleetTests and three existing custom authoring definitions were mechanically migrated and compiled, not run.

## ONE new exact case, all attempts preserved

`Selection policies: direct contracts guard metadata and physical fleet facility work`

| Attempt | Result | Owned process | Observed wall time |
|---|---|---|---|
| 1 | TimedOut; no aggregate counts returned | PID43200, exit-1 | 30055ms |
| 2 | TimedOut; native service completed, fleet entered | PID1664, exit-1 | 30034ms |
| 3 | TimedOut; native cargo loaded | PID39488, exit-1 | 30036ms |
| 4 | TimedOut; native cargo loaded | PID39456, exit-1 | 30042ms |
| 5 | 44 passed,1 failed,1 aborted suite; suite21225ms | PID2020, Exited1 | 21367ms |
| 6 | TimedOut; native flows completed, direct loaded checkpoint restored | PID8272, exit-1 | 30044ms |
| 7 | TimedOut; four explicit assertion failures for a wrong metadata test boundary | PID38184, exit-1 | 30049ms |
| FINAL8 | 159 passed,0 failed,0 aborted; suite34660ms | PID20468, Exited0 | 34823ms |

No output truncation/observer errors. First seven attempts used outer30000ms. FINAL used outer60000ms, owned termination5000ms, output30000chars. Actual terminal processes were observed; no missing-handle restart or full-suite fallback.

The initial timeout was not diagnosed as engine recursion. Stage logging demonstrated completed work and live progress. Attempt5's32-update delivery stage was too short for the authored default-speed route; it reported actual step71/job1 Assigned/worker4. New case uses explicit JSON speed120 and128/96-update stage bounds; production defaults are unchanged and loading/travel/delivery remain physical. This is not proof that all default-speed/route/stress workflows satisfy the same runtime budget.

Attempt7 injected classes-pause into a membership preview that does not consume navigation metadata. Its four failures were a driver boundary mistake, not proof of an engine mutation gap. FINAL observes the actual CopyPolicies metadata query. Repeated identical rank fault invocations were removed. No gameplay rules were put in the test driver to force completion. Increasing the combined outer budget to60s preserves the four physical native/direct flows and negative cases;34.7s remains explicit performance follow-up evidence, not a performance pass.

## Scope actually proven

- Native/direct master-listed JSON policies select the same real facilities, move an actual machine, consume exactly three spares and repair only after work completes.
- Accepted service survives schedule disable, sole clock pause, partial save/load, continued twins, replay, retained immutable facts and isolated sessions.
- Native/direct named fleets use the same exclusive membership owner and declared navigation/role facts. Actual hauling loads eight steel before delivery, restores loaded state and conserves20 total units through completion/replay.
- Foreign company facility/fleet views are private; copied policy lists are immutable.
- Selected facility Node/rank and fleet member/navigation/dependency/class callbacks reject private persisted mutations without Touch, clock mutation and typed exceptions at their actual consuming boundaries.
- Invalid unused fleet Validate callback is rejected before Initialize. Faulted reads cannot checkpoint or publish hashes.

These are narrow current-build facts, not proof for every custom provider, arbitrary nonparticipating CLR state, callbacks that mutate only once during baseline codec acquisition, all reentry, full stress/performance, lockstep/fuzz/soak, Oil migration or full acceptance. All compound original gates keep their requirement text and checkboxes.

## Input identity

Selected422 executable inputs frozen after FINAL green at2026-10-09 20:38:26 UTC;14 changed/new compared with the previous412-input checkpoint. No executable edit follows the freeze. Source anchors and metadata are refreshed in the authoritative completion audit separately; audits do not close runtime gates.

[Stage](../../plans/enhancements/P9_SELECTION_POLICY_CONTRACTS_DESIGN.md). Full P1-P12 remains OPEN; protected clock/Tycoon files unchanged and publishing deferred.
