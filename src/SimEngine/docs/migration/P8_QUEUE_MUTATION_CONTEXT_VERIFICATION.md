# P8-QC1 queue mutation context verification

## Engine change

Core2.482.0/save154/replay1. Existing AppendQueue and CancelQueue now capture the original entity/game-clock facts and selected space through EntityObservation scopes, before provider before-state reads and mutation. The fact scope permits only the legitimate entity revision change. Immediately after the queue callback, both scopes verify before any after-state is trusted. Existing queue identity/prefix/count/phase/progress/controls/sibling/WIP/tool checks and enqueue/cancel first-fault boundaries remain.

There is no new queue, inventory, reservation ledger, timer, saved context flag or rollback mechanism. Entity lifetime/owner/settled state, sole clock pause/speed/time, next entity identity and selected space cannot change merely because a provider appends/cancels a queue. Ordinary downstream Update execution may still create a real result after work earns its duration.

The existing independent PulseTransformer proof receives optional negative-only QueueFault authoring. Cancellation already has the actual World contract, so probes alter the actual clock, prepare/commit one actual free result, or throw the original error after an otherwise exact head/tail removal. No extra live probe state/codec or new provider implementation. A data-only module and actual master-listed queue-context.json compose its prototype; explicit embedded-resource packaging makes that document loadable.

## Exact build/runtime

One clean solution compile: dotnet build Beep.SimEngine.slnx --no-restore,9.49s,0warnings/errors. No repair compile.

ONE NEW exact FIRST/FINAL324/0/0 in3765ms; PID39384 Exited0, process3928ms. Zero aborted suites, stream truncation or observer error. Clean compile precedes runtime; no executable edit after FINAL. No old/family/full runtime.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Transformation queue context: legitimate physical cancellation and original clock identity mutation faults" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
```

Outer60000ms/termination5000ms/output24000chars. Physical/independent stages bounded1000Updates with actual clock/queue/phase/blocker/progress diagnostic.

## Public-player proof

Two independent minute/half-minute workflows use ordinary queue controls and commands. Exact append of three real requests is accepted; saved state matches. Tail cancellation removes only its chosen request, preserving original head and unrelated next identity. Normal enable earns real work; working head cancellation leaves the final request. That remaining request creates exactly one actual engine result after its duration, not a harness counter. Retained snapshots stay detached; final save/replay and isolated session identity match.

Four native/point minute/half workflows physically haul eight steel to the existing manufacturing facility. A two-entry batch earns actual progress and holds the original WIP/tool. Disable and tail cancel preserve active execution/progress/material/tool token. Ordinary selection removes original raw-port access, then head cancel enters Returning with exact OriginalUnavailable cause, retained WIP and same tool. Saved wait and the paused single clock earn no time or repeated utility costs; foreign views withhold private return state.

Restoring original raw access returns WIP once and releases original tool claims. Actual final raw equals eight less already consumed operating input; no input refund for earned consumption and no cancelled output. Terminal saves and full input replay match; untouched interleaved native/point sessions remain unchanged.

Eight authored negative cases cover head and tail callbacks that pause the real game clock, change its actual speed, commit an unrelated real entity creation (changing next identity), or throw after cancellation. Pure normal previews do not invoke those mutations. Normal submitted commands run through Update and latch first fault; the faulted session cannot advance, save/hash as healthy or escape through an early missing-queue preview. Original callback error and first fault identity remain; a separate session stays unchanged. Partial mutation is not advertised as rolled back.

The new driver uses only public seat previews/commands/results/views, Game.Update, saves/replay and ordinary clock control. No World, TestKit, raw stock/progress/job/geometry writes. Declared hostile callbacks are engine-side extension proofs, not host gameplay.

## Qualification

Original221/186checked/35open unchanged; additional209. No original checkbox/text/line changes or gate closure. Positive point cases exercise legitimate selected-space guarding; no hostile map/lifecycle callback is claimed proved. Enqueue-negative, broader provider state, unknown remote CLR state, every phase, optional monetary charges, complete utility/staffing/entity-output/continuous/disposition/stress/performance and full P8/P1-P12 gates remain OPEN. Older proofs compile only, not rerun.

Canonical SimEngine only. Protected sole-clock implementation/G19/Tycoon unchanged; no Oil, Draw/platform/adapters, staging, commit or push. Full goal AND plan remain incomplete.

## Metadata closure

Fresh read-only metadata closure:1712 source anchors,953 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2900 paths/1026 Markdown files/8666 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2888/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional209. Six existing selected executable inputs changed; three newly created executable inputs/five new source files, two existing production sources changed/zero new. No previously existing input newly admitted. Existing view schemas unchanged (work100/logistics42/transformation19); queue mutation context adds no projected fields. No runtime rerun for documentation validation.
