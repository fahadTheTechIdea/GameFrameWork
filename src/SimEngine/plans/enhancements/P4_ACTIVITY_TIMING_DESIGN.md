# P4-G54: Sole activity timing owner — Design Document

## 0. Problem Statement

IActivityNodeDefinition.RequiredTicks(ISimulationTime) duplicates the executing session's duration converter. Work, cumulative resource consumption, graph/line analysis, checkpoints and views invoke that callback although nodes already declare exact GameDuration. Mod-authored node conversion can therefore disagree with the single game-clock policy.

## 1. Goals

1. Delete the node conversion callback and native/captured implementation, without a compatibility helper.
2. Every original engine consumer converts the node's authored Duration through its actual session ISimulationTime.
3. Preserve exact wide/fractional progress, continuous input accounting, save/hash/replay and public immutable required-work facts.
4. Prove native/direct ninety-second timed/physical DAGs under sixty-second and ten-second quantums, bounded range/refusal and physical input consumption through one new exact case.

## 2. Design

Nodes retain immutable Duration only; existing ISimulationTime.Ticks is the sole conversion owner. Migrate ActivityGraph analysis, WorkBoard validation/advance/restore, WorkRequirementContext, running input budgets, docks, LineService and WorkView. RequiredTicks remains a detached view scalar, not a provider method. No graph compiler/state/scheduler/resource/clock duplicates. Historical test consumers migrate mechanically and compile only.

Existing master-listed executor fixture gains a world-input edit hook, never normal execution logic. New test authors ninety-second nodes and a continuous eight-unit input in JSON, uses ordinary commands and detached views, and drives only the engine Update.

## 3. API surface

Breaking removal of IActivityNodeDefinition.RequiredTicks and ActivityNodeDef.RequiredTicks. Modules supply Duration, engine applies its selected session converter. Public clock controls/commands/views unchanged; no platform/Draw API.

## 4. Files to change

| Action | File group | Risk |
|---|---|---|
| Modify | Standard/ActivityGraph.cs, Work.cs, WorkContinuous.cs, WorkRequirements.cs, WorkStart.cs, WorkDocks.cs, Networks.cs | medium |
| Modify | Views/WorkView.cs | medium |
| Modify | Kernel/SaveGame.cs and protocol fixture | low |
| Modify | Existing test call sites, shared executor fixture and Program registration | low |
| New | ActivityTimingTests.cs | medium |

## 5. Error handling matrix

| Authored input | Result |
|---|---|
| Positive rational subquantum duration | Original converter's positive ceiling |
| Wide representable duration | Exact long work goal; no int narrowing |
| Unrepresentable duration | Typed composition refusal before Initialize |
| Healthy saved earned work/resources | Original codecs validate against same converter |
| Provider custom conversion | API absent, no fallback/shim |

## 6. Backward compatibility

Development only, breaking callback removal, no compatibility reader/helper. Build2.397.0/save154/replay1; persistent layouts unchanged.

## 7. Verification

Build completed slice when compilation needs checking. ONE new exact bounded case: Activity timing: session converter owns graph work and cumulative physical inputs. Runtime30000ms, owned termination5000ms, output30000chars. Preserve all attempts and repairs. No historical/family/full/Oil/Tycoon/platform runtime; full regression remains open.

## 8. Risks

Different update quantums round authored duration differently by explicit engine policy. This is not a claim that travel/whole-world chronology is identical across quantums. Custom graph metadata purity/reentry, nonparticipating state, performance/lockstep/soak and full acceptance remain open.

## 9. Out of scope

Protected GameClock and excluded Tycoon files, Oil/platform/Draw/hosts. Pre-world executor metadata is a separate remaining gap. Full P1-P12 and publishing remain open/deferred.

## 10. Sub-task execution order

1. Delete callback and migrate all current engine calls through original session converter.
2. Mechanically migrate historical tests without running them.
3. Reuse JSON fixture for native/direct multi-quantum physical/continuous input proof.
4. Build and run only new exact bounded case; preserve observed repairs.
5. Freeze selected inputs and reconcile tracker/audit without changing compound requirements.

## 11. Delivery status

Scoped verified, not whole-plan completion. Build1 failed6.51s with one new driver comparison error; subsequent clean builds4.59s/3.98s, zero warnings/errors. ONE NEW exact case first20 passed1 failed with one aborted suite507ms (PID31228 Exited1/observed655ms); FINAL129/0/0 in1953ms (PID2504 Exited0/observed2115ms), no timeout/truncation/observer error. Repairs changed only driver WorkProgress equality and completed-order retention assumptions.

Native/direct ninety-second graph nodes run physically in2 productive units under60-second quantum and9 under10-second quantum. Continuous input uses actual work denominator and consumes exactly8 units20->12; graph/view goals and actual remaining completion agree. Saved running inputs/itineraries, partial continued twins/pause/replay/privacy/isolation,2.4b-wide goals, subquantum ceiling and overflow before Initialize are narrowly proven. All current engine RequiredTicks method calls/declarations removed; ten historical test call-site files migrated compile-only.

Selected412 inputs frozen after final green at2026-10-09 20:14:32 UTC; no executable edits afterward. [Evidence](../../docs/migration/P4_ACTIVITY_TIMING_VERIFICATION.md). All original compound gates retain text/checkboxes; broader pre-world/provider/global/performance/full gates stay OPEN. Full goal active; no staging/commit/push.
