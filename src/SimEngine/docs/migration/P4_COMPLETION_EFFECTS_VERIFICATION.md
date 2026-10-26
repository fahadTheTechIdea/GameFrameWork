# P4-G3a completion effects — focused verification

2026-10-05, standalone Beep.SimEngine working tree. This is slice evidence, not full
framework regression or Oil/Tycoon/host/platform proof. Existing dirty work was preserved.

## Identity and build

Current build `beep-simengine-2.72.0`, save44, work21, work-view26. Logistics4,
logistics-view9, construction-view3 and native resource codecs are unchanged. The
IWorkEffect.Apply interface and native node/order layout are replaced directly; no
Code-only overload, old save reader or compatibility path.

`dotnet build Beep.SimEngine.slnx` succeeded with zero warnings/errors; **2.52 seconds**.
Earlier incremental `--no-restore` builds were also used during implementation.

## Exactly two suite names

```
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Completion effects: typed contracts and private retry state"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Completion effects: physical blockers and saved continuation"
```

- Typed contracts/private retry: **110 passed**, zero failed/aborted, **359 ms**.
- Physical blockers/saved continuation: **104 passed**, zero failed/aborted, **467 ms**.
- Total: **214 checks**. Same two names repeated while refining; no third/family/full run.

The directly affected point-recovery admission/physical checks execute inside these two
gates. Earlier named suites were compiled only and their historical counts are not
current regression evidence.

## What the gate proves

- Immutable Applied/Pending/Failed results; explicit blockers, terminal Failed even when
  its code is listed, and unlisted Pending treated as a normal terminal refusal.
- Public-only master-listed JSON completion provider, actual machine travel/work,
  released worker/job links, retained progress and dependent nodes waiting for Applied.
- Existing selected JSON profile owns two/four/eight-minute retry backoff; no every-tick
  effect loop. Scheduling timestamps use the actual attempt tick, not the next-frame cursor.
- Company-private detached/pure node/order retry facts. Pending/overdue-paused phases,
  real foreign promises and interleaved saved twins roundtrip identically.
- Pause/resume and cancellation through normal player commands; no duplicate effect
  production, reset work, cleared foreign claim or resurrected cancelled timer.
- Corrupt future deadlines, missing failure counts, invented completed progress, extra
  job links and premature final graph phases refuse hash/save without gameplay mutation.
- A deliberately faulty provider that mutates its target then returns Pending faults
  the normal work Update; partial state cannot hash/save. No rollback or compensation.
- Native point construction consumes eight inputs once, retains four real tools/site
  behind a final foreign promise, then performs actual stock recovery/build after release.
- Native recovery waits after physical machine work, moves no stock while blocked, then
  transfers twenty real units to five authored capacity-four containers after release.
- An applied production effect creates one native claim that blocks ordinary replacement;
  retries/save-load do not reapply it. Release permits native stock-preserving replacement.
- Existing point cancellation/tool-surplus/demolition and later timed haul delivery checks
  remain green inside the focused physical gate.

## Review and limits

WorkBoard is sole saved order/node/job owner, WorkSystem sole progression owner,
IWorkRetryPolicy aliases existing authored content, and existing inventory/receipt/
transaction/spatial/construction owners retain authority. No second queue, stock ledger,
timer, scheduler, generic fake effect or harness gameplay was added. The lifecycle skill
guided the staged design and evidence; full regression remains pending as requested.

The refusal guard checks the actual target, not every possible cross-entity/service
side effect. Native line/incident providers and old direct-effect tests source-migrated
and compiled; broader matrices/replay/soak/performance were not run. Generic executors,
templates, automation/incidents/rivals, full boundary/mod acceptance and P8/P10/P11/P12
remain required. The full framework objective is still active/partial.

Final source scan finds no Code-only order-effect Apply declaration/implementation;
all three RunEffect sites use WorkEffectResult. Build/save/work/view identities agree.
`git diff --check` reports no whitespace errors (Git emits existing CRLF notices).
