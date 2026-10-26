# P11-F1 — Process-only composition verification

Date: 2026-10-10. Canonical Beep.SimEngine only. Core2.407.0/save154/replay1/work-view97 unchanged. [Design](../../plans/enhancements/P11_PROCESS_ONLY_DESIGN.md).

## Outcome and scope

Original P11.acceptance.036 is verified: this fixture registers and master-lists exactly points, sim.access, sim.inventory, sim.logistics, sim.motion, sim.operations, sim.transformations, sim.work and sim.world-setup. No progression, objectives, player controller, company knowledge or economy module is registered or selected. Root engine codecs remain legitimate engine-owned state. Empty mode properties and a zero authored cost require neither a budget nor a fabricated default balance.

Four explicit embedded JSON documents describe the master, world/setup, work/handling and recipe. Existing access/retry documents are explicitly referenced. PointModule is the existing engine contract spatial/motion provider, not a host gameplay algorithm. Native Transformer, WorkBoard, LogisticsService, inventory/port/tool reservations and sole game clock execute every rule; no engine/runtime/protocol source changed in this slice.

## Physical player workflow proven

Normal seat previews/commands enqueue the process. Pending inbox saves, actual carrier pickup, supplied productive work, disabled-operation pause, sole-clock pause, cancellation and terminal saves continue as equal-hash twins. The recipe holds three input units in original WIP and reserves one reusable tool at its actual source endpoint; the tool is not copied into WIP. The first batch takes authored30-minute productive work and earns two outputs, leaving source16. A second partial batch hauls only three more units, then cancellation returns unconsumed WIP to the factory, leaving source13 and factory input4. A third batch reuses these local inputs/tool: two successful batches total consume six and produce four, with source13/factory input1/cargo0. No cancellation output, remote refund or instant resource creation occurs.

An ordinary authored machine inspection moves/works and earns its exact order/target/time completion fact. Other-company process queries and commands refuse; retained process facts remain detached. Command-stream replay, final saves and interleaved independently initialized sessions succeed. Missing inventory/work modules refuse at the module graph; missing points refuses at explicit master content loading. These are distinct real refusal boundaries, not a compatibility fallback.

## Actual verification

Command (only ONE NEW exact case, repaired reruns of that case):

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Process-only game: physical manufacturing without progression controllers knowledge or economy' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

Each stage has256 engine Update attempts; the outer child timeout is30000ms, owned termination5000ms and output cap30000chars. All five children Exited, outputs untruncated, observer errors empty. No older, family, full, Oil, Tycoon or platform runtime executed.

| Attempt | Passed/failed/aborted | Suite ms | PID | Exit | Observed ms | Interpretation |
|---|---|---|---|---|---|---|
| 1 | 1/1/1 | 83 | 19852 | 1 | 226 | Fixture operation unit must be actual unit.work-rate. |
| 2 | 1/1/1 | 79 | 18000 | 1 | 217 | Optional entity_output must be omitted, not explicit null. |
| 3 | 10/2/1 | 1372 | 12096 | 1 | 1521 | Core engine codec owner was incorrectly excluded; source public=false prevented automatic discovery. |
| 4 | 43/2/0 | 2919 | 43228 | 1 | 3063 | Physical flow passed; missing module negatives expected wrong exception type. |
| FINAL5 | 45/0/0 | 2823 | 41156 | 0 | 2971 | Scoped process-only acceptance verified. |

All five builds used dotnet build Beep.SimEngine.slnx --no-restore, exit0/warnings0/errors0:6.21s,4.91s,5.19s,4.67s,FINAL4.23s. Full-suite runtime is not inferred from build success or45 checks.

## Evidence inventory and completion boundary

Executable SHA256 freeze: 2026-10-09 23:07:41 UTC; 480 selected inputs. Seven changed/new paths versus previous475-input snapshot: test Program, ExtensionProof project, ProcessOnlyGameTests and four process-only JSON documents. Five new paths join the inventory. No executable edits after FINAL green. Protected sole-clock and three G19 Tycoon files remain unchanged.

Only P11.acceptance.036 changes unchecked→checked; original requirement text and all other original checkbox states remain. Counts221 total/152 checked/69 open;109 additional scoped deliverables. Source hashes, local documentation links, tracker shape and dirty/staged status are checked separately in the machine-readable [completion audit](../../plans/enhancements/COMPLETION_AUDIT.json). Runtime absence and ordinary execution are evidence for this one fixture, not all optional combinations, performance/soak, provider fault matrices, remaining full original plan gates or publication. G19 authority remains pending. The original full goal stays active; no staging, commit or push before the goal and entire plan finish.
