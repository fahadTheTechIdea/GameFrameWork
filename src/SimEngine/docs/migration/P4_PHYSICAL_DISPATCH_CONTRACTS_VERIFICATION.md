# P4-G38 — Physical dispatch contracts: scoped verification

2026-10-09. Core beep-simengine-2.376.0/save154/replay1. Full P1-P12 remains OPEN.

## Engine changes

IPhysicalWorkEffect declares subject/capability/check/commit callbacks. PhysicalWorkEffectContracts
owns the existing actual-order/binding/machine-ancestry/admission/completion algorithm. Original
PhysicalWorkEffectDef implementations now delegate to it; the base remains an active implementation
for other categories, not a legacy compatibility facade. Native TransformationDispatchEffectDef is
direct Def/ITransformationDispatchEffect, not a concrete physical-effect subtype.

TransformationDispatchDeclaration owns strict JSON recipe/capability/count/repeat/home authoring.
Native and public-only independent definitions share this declaration and dispatch helpers. Every
loaded physical effect validates capabilities, and every loaded dispatch validates actual selected
recipe metadata before Initialize, including unused content. Target capability support and positive
machine ancestry are checked through the same original WorkBoard validator.

Pure physical subject resolution/check callbacks guard actual target/subject Entity.Sync plus the
existing scalar/provider/map facts. Refused subject commits preserve that same actual codec/scope.
The general completion boundary now checks clock reference/StepIndex/speed/pause for every result,
including Applied. Applied remains allowed to commit legitimate target/identity changes. There is
no new clock, scheduler, dispatch queue, output store, persistent effect state or rollback mechanism.

The existing manufacturing JSON fixture accepts explicit extension document text and authors exact
module dependencies from the extension contract. Tests contain only authored input, normal player
commands, game-clock control and immutable observations. Proof modules use transient diagnostics,
not gameplay progress/queue/timing rules. Historical cases were compiled only, not rerun.

## Compilation

Five --no-restore solution builds. First8.31s failed because the new public proof assumed
ITransformer.QueueCount; fixed to use the existing private immutable queue view. Second8.03s failed
because the new driver assumed WorkProgress equality operator; fixed to Equals. Three clean builds
6.61s/4.10s/3.85s, each zero warnings/errors. No historical/family/full/Oil/Tycoon/platform runtime.

## ONE NEW exact bounded case

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Physical dispatch contracts: authored effects earn timed manufacturing" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 50000
```

1.2 passed/1 failed,84ms; PID40888 exited1, observed244ms. The reused fixture hard-coded an
operations-only extension dependency. It now authors the actual module dependency declarations;
engine manifest validation was preserved, not bypassed.
2.198/2,2559ms; PID8112 exited1, observed2706ms. Physical workflows passed, but two assertions
expected one transient callback across both live and restored twin sessions. Each twin dispatched
once. Corrected the diagnostic assertion to two total callbacks, one per twin; engine logic unchanged.
3.FINAL200/0/0,2600ms; PID33480 exited0, observed2750ms.

All three processes terminal, untruncated, no observer error/timeout. Final220 selected executable/
content inputs frozen with SHA256 at2026-10-09 14:08:04 UTC, before execution. Earlier220-input
freezes at14:06:32/14:07:12 remain historical. No executable edits after final freeze.
Selected identity is not a complete dependency closure.

## Runtime coverage

- Direct native and independent immutable dispatch definitions, actual recipe aliases/sole queue.
- Both final and finish-node effects after real machine preparation in the same authored DAG.
- Ordinary player PostOrder preview/command; actual machine progress, material/tool commitments.
- Private pure final/node effect readiness preview, zero unearned queue, foreign-view refusal.
- Partial preparation save/load, sole-clock pause, deterministic saved twins and exactly-once dispatch.
- Exact two-batch queue metadata, disabled production, detached immutable queue and company privacy.
- Earned preparation consumes2 of60 real opening materials; tools are not fabricated consumables.
- Normal operation command starts real ten-minute production. Actual WIP/tool state persists;
sole-clock pause freezes it. Sequential batches consume3/generate1 each, ending at54 actual stock.
- WIP/tool release, completed reload, interleaved isolated session and normal command replay.
- Ordinary cancellation before earned completion creates no queue and preserves60 material.
- Twelve invalid authored/metadata cases, including unused zero-count provider and timer-only ancestry,
fail before Initialize; missing/unknown/wrong-kind inputs and legacy keys never substitute defaults.
- Three actual independent commit faults: real two-batch enqueue followed by pause/speed mutation
or exception. First callback faults work.effect, withholds that Update's time completion, cannot
resume/save/hash, and does not claim to roll back the already committed queue.

## Limits and continuation

Other physical categories, worker-resolved subject refusal/preview mutation, native TileMap callback
mutation, every unrelated cross-entity/service change, arbitrary CLR sandbox/global rollback,
all clock pacing controls, whole-session accounting/corruption/lockstep/fuzz/soak/performance and
original compound acceptance gates remain unproved. Guards exist beyond this runtime proof but
source-only paths are not reported as exercised. Native entity-producing effects may allocate
identities legitimately; this case produces resources, not a new entity via an effect callback.

Original221 gate IDs/text/checkboxes are preserved; line offsets only refresh. Broader independent
provider/effect and full P1-P12 gates remain OPEN. G19/Tycoon type-binding authority is unanswered.
GameClock source, excluded Tycoon files, Oil/hosts/demos/platforms/Draw unchanged. No staging/commit/push.
