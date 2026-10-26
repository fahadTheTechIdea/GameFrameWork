# P4-G58 — Earned completion and active-node durability: verification

Verified2026-10-10. Core `beep-simengine-2.407.0`, save154/replay1/work-view97. Original WorkBoard52, WorkSystem45, history/node/resource codecs and sole clock unchanged.

## Implementation

Existing IActivityCompletionHistory identity, target/company and service queries require healthy World state before missing/private early returns. Existing WorkView completion, requirement, node, progress and order queries do the same. Callback-dependent detached lists/progress are constructed locally and published only after successful healthy evaluation; no partial list is published by an engine callback failure. The original board/history/graph/work/resource owners and Sync payloads remain. This local-publication behavior is source-reviewed; callback-time fault-prefix injection is not separately executed in this slice.

Two explicitly master-listed proof JSON documents define a two-stage dependent graph and an independent prior requirement. Existing native target/company requirements and the independent contract share original completion evidence and evaluator. Factory inputs are reused, not the historical PriorActivityTests runtime. The new driver uses only ordinary commands, detached views, Update, clock, saves and replay; it never writes history, progress or inventory. The existing proof module arms one negative update fault and binds its history observation only at that exact faulting system; it owns no positive simulation logic.

## Exact runtime

Only ONE NEW exact case, two attempts of the same case:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Completion durability: earned prerequisites and active nodes in saved player chains' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

| Attempt | Passed | Failed | Aborted | Suite ms | PID | Exit | Observed ms |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | 75 | 3 | 0 | 692 | 42424 | 1 | 834 |
| FINAL 2 | 78 | 0 | 0 | 676 | 26368 | 0 | 819 |

Both children authoritatively Exited; stdout/stderr untruncated; observer errors empty. Limits outer30000ms, owned termination5000ms, output30000chars; physical stages bounded256 Updates with actual order diagnostics.

The first pass completed the positive saved chain. Three direct fault checks observed another healthy session because a shared proof handle was assigned during each composition. The probe now binds only in the exact negatively armed update; no positive world handle remains in composition. This repaired instrumentation, not engine fallback. No older/family/full/Oil/Tycoon/platform runtime.

## Builds and source

`dotnet build Beep.SimEngine.slnx --no-restore`: both exit0, zero warnings/errors;6.52s and FINAL4.72s.

FINAL executable freeze2026-10-09 22:55:22 UTC:475 selected inputs,11 changed/new against previous470; no executable edits after FINAL green. Five existing/new paths join the selected inventory. Original requirement text changes0; checkbox changes1, P4.acceptance.011 only.

## Original requirement evidence

Original text: `Prior completion facts/active nodes survive saved twins.`

Blocked dependent nodes save without acquiring work. Partial prerequisite physically consumes8 from32 but earns no fact. Saved twins finish real preparation, prune its active order and retain the exact target/execution/creation/completion fact. That compact fact persists beside an active dependent graph containing earned first-node progress and a pending second node. Worker-policy pause and sole-clock pause preserve actual progress. Saves during productive first and second nodes retain state, job/worker/completed-by identities, progress, productive steps, blockers, choices, resources and dependencies; complete saved-state hashes agree. The actual second node executes only after the first earns completion.

Dependent work does not repeat input consumption. Saved queued cancellation of a repeated preparation leaves prior earned evidence unchanged and consumed8 unrefunded. A subsequent actual repeat earns a fresh execution and replaces only the compact latest key; retained prior/node/progress views remain detached. Total three preparation attempts consume24, leaving8. Other-target scope remains blocked while explicit company scope returns actual earned evidence. Foreign seats see no private completion/orders. Ordinary replay and interleaved resource-only session isolation pass.

Negative update failure tests completion, service, identity, missing/pruned nodes, criteria, invalid previews, progress, orders, save and hash refusals. Outputs clear and the original first fault survives. Previously detached facts remain usable; no new completion is invented.

## Remaining scope

Original P4.acceptance.011 is verified with text unchanged. All other original checkbox states are unchanged; full goal/aggregate gates remain open. Arbitrary provider purity, callback-time faults, all lifecycle/removal/generation matrices, shared reservation/all-any contention, stress/fuzz/soak/lockstep and full regression are not proven by this case. G19 protected Tycoon authority pending. No Draw/platform/adapter, second clock/scheduler/history owner, legacy/shim, staging, commit or push changes.
