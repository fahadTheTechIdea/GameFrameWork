# P4-G37 — Work-effect contracts: scoped verification

2026-10-09. Core beep-simengine-2.375.0/save154/replay1.
Full original P1-P12 remains OPEN; no staging/commit/push.

## Changes and authority

WorkEffectContracts validates every loaded actual frozen IWorkEffect before Initialize, including
unused declarations. Definition-owned IPersistentService state is rejected. Original WorkBoard
plan/preview/run paths delegate to this same boundary; no new effect catalog, completion queue,
retry clock, learned ledger, scheduler or compatibility reader.

Refusal captures the actual target Entity.Sync through original EntityObservation.CaptureProvider,
plus actual entity scalar/clock/identity and optional TileMap version. A Pending/Failed callback must
preserve those facts; private target state mutation without Touch is caught. Callback exceptions
and malformed results fault work.effect and withhold healthy advancement/hash/save. Actual order,
declared effect/node and native Completing stage are required before Apply. Applied effects remain
allowed to commit their target/identity change. This is scoped guarding, not global rollback.

Native TechnologyCompletionDef now directly implements the existing effect contracts. Shared
TechnologyCompletionContracts validates final-only research and delegates Check/Apply to original
ITechnologyKnowledge/CompanyKnowledge. The direct independent completion adds explicit nullable
consumer JSON; null is deliberate opt-out, not a fabricated default. Named utility gating reads the
same guarded actual consumer. Existing board failures/deadlines/effect-applied state remain sole owners.
WorkView.PreviewOrderEffect adds an engine-owned, company-private final-effect preview; it and node
previews guard the actual target codec, not host interpretations or a second model.

The existing utility-research authored fixture accepts explicit module/document/configuration hooks.
The new case reuses it instead of another factory or gameplay driver. The prior144-check case compiles
only and was not rerun after this change.

## Compilation

Four clean --no-restore solution build invocations:17.77s,6.52s,4.47s,5.40s.
Zero warnings/errors. Historical cases, public-only extension assembly and thin quickstart compile.
No historical/family/full/Oil/Tycoon/platform runtime.

## ONE NEW exact bounded case

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Work effect contracts: earned research and scoped refused completion" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 50000
```

1.Initial2 passed/1 failed,188ms; PID21712 exit1, observed339ms. New selected retry document omitted
required authored work executors. The fixture now explicitly retains work-retry.json executor
declarations and replaces only its authored retry-profile array. No engine fallback added.
2.19/1,762ms; PID36836 exit1, observed924ms. Native physical completion passed, but independent
proof used the string-only reader for explicit null. It now consumes Value and requires exact
string-or-null type; missing/empty/unknown input still refuses.
3.FINAL117/0/0,1385ms; PID26108 exit0, observed1547ms.

All three processes terminal; no truncation, observer error or timeout/restart. Failed attempts
ended with new-case exceptions. Final209 selected executable/content inputs frozen with SHA256 at
2026-10-09 13:47:06 UTC before execution; no executable changes afterward. Earlier207/209 freezes
remain historical at13:44:50/13:45:45 UTC. Selected identity is not full dependency closure.

## Exercised behavior

- Actual direct native and independent completion aliases; normal composition and no Tycoon dependency.
- Real machine/physical staffed30-minute graph, eight actual consumed materials and reusable tools.
- Premature final preview is not-complete; foreign preview is not-owned; no early learned grant.
- Partial save/load and sole-clock pause; original company-private learned record after earned work.
- Independent final utility policy retains a fully earned Completing graph with typed inactive
blocker, actual failure count/deadline, no refund/double consumption or guessed success.
- Private whole-order preview is pure, including target codec and actual retry state.
- Pending retry reloads original board; normal processor enqueue starts actual utility and permits
once-only learned completion. No host completion timer or second queue.
- Detached retained retry facts, known-research refusal, original build unlock admission, unchanged
learned timestamp on later updates, completed reload, interleaved session and normal replay.
- Five actual refused-effect faults: private target counter mutation without Touch, target Touch,
game-clock pause, actual entity spawn/identity allocation, and callback exception. Each first
callback faults work.effect, prevents that Update's time completion and refuses healthy hash/save.
- Missing/empty/unknown independent consumer authoring, unused persistent work-effect definition
and wrong completion-kind fail before any Initialize.

## Remaining gates

Other native/independent effect categories, node-effect matrices, heterogeneous utility/network
completion, successful-effect timing misuse, unrelated cross-entity/service mutation, arbitrary CLR
sandbox/rollback, global purity/accounting, lockstep/corruption/fuzz/soak/allocation/performance remain
unproved. Optional TileMap mutation guard is source/compilation-only here. Original
P11.acceptance030 remains unchecked for wider independent provider/effect breadth.
Original221 gate IDs/text/checkboxes are untouched; only line offsets refresh.
Tycoon/G19 resource type-binding authority remains unanswered. The sole GameClock source, excluded
Tycoon files, Oil/hosts/demos/platforms/Draw are unchanged. Full goal/plan is not complete or publishable.
