# P4-G7 — Authored activity content contracts verification

Historical proof on2.339.0; not rerun on2.340.0. [P4-G8 work catalogs](../../plans/enhancements/P4_WORK_CONTENT_CONTRACTS_DESIGN.md) replaces remaining native requirement/executor/role lookups with actual content contracts and shared group/resource/staffed owners. [Current 75-check scope](P4_WORK_CONTENT_CONTRACTS_VERIFICATION.md); wider provider/runtime/full acceptance remains open.

Scoped verified 2026-10-09; full P1–P12 and the active goal remain OPEN.

## Single ownership and engine boundary

IAuthoredActivityDefinition extends the existing activity/player/target-plan contracts and IContentDefinition with explicit orderability and target metadata. Bound component/service/construction plans still use IActivityDefinition: execution graphs are not required to pretend to be standalone file definitions.

ActivityDeclaration is the sole JSON settings/node parser and compiler of the existing ActivityGraph/ActivityNodeDef. Native ActivityDef and direct external declarations delegate to it; no alternate graph, registry, scheduler, reservation, progress store or codec. ActivityDefinitionContracts owns the existing target and player-intent behavior, including optional effect admission observation. Specialized native overrides remain real implementations, not compatibility fallbacks.

WorkPlanCatalog captures actual owned authored contract aliases. WorkView previews, construction/infrastructure, incident workflows, transformation staffing, technology unlocks and activity knowledge visibility resolve the same contract. Composition validates canonical identity, graph and target metadata after resolution and before Initialize. WorkBoard and native physical systems alone execute jobs/material use/tool holds and completed history.

The external proof declares three independent activity definitions from an explicitly master-listed activity-contracts.json, with no ActivityDef inheritance: player inspection, five-stage incident response and physical construction. Native documents remove those exact definitions from this fixture rather than retaining duplicates. The shared authoring helper takes explicit document inputs; it contains no live gameplay decisions. The player driver uses public commands, sole Clock/Update, detached seat views, save/hash and replay only.

## Build and bounded runtime evidence

All builds used `dotnet build Beep.SimEngine.slnx`. No historical suites were run.

1. Clean6.68s, zero warnings/errors: core contract migration, before new driver/proof compilation.
2. Failed5.56s, zero warnings/one error: new JSON authoring fixture used nonexistent JsonValue.Items.
3. Clean4.55s after using the actual indexed JSON array API.
4. Clean4.64s after correcting completed-history observation and adding tool availability checks.
5. Final clean4.30s, zero warnings/errors after correcting the authored inventory signal source ID.

Only ONE NEW exact bounded case ran; repairs reran only that same case:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Activity content contracts: independent JSON graphs drive player plans, phased response and physical construction" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 24000
```

- Attempt1:25 passed/one failed,634ms; PID40528 exit1/observed786ms. Driver incorrectly queried live nodes after a completed manual order had been compacted into native plan history. Read the parallel join while active and then assert actual PlanState.Done. No engine retention behavior changed.
- Attempt2:81 passed/one failed,1156ms; PID29632 exit1/observed1302ms. Driver used inventory rather than the authored feed source ID. Corrected input reference; no fallback provider or engine behavior change.
- Final:84 passed/zero failed/zero aborted,1129ms; PID19516 exit0/observed1278ms. Output untruncated, no observer error; current assembly built successfully first.

No full/family/historical/Oil/Tycoon/platform runtime. Historical callbacks/helpers and QuickGame compile only.

## Verified public-player scenario

- Actual direct owned aliases, construction binding to the same authored definition and once-only Initialize.
- Pure normal requirement previews; bound response refuses unbound player ordering.
- Persistent paused manual plan creates no work/material grant; saved paused twins and foreign plan privacy.
- Activation uses the same native order; timed preparation, staffed inspection and parallel timed calibration, then joined finish.
- Machine physically moves from position4 to target20; real worker/job, positive elapsed work and immutable/private node facts.
- Two materials consumed20→18 once; two physical tools held, available inventory16 while running, then available18 when released.
- Sole paused clock cannot earn work. Saved partial twins remain hash-identical. Retained snapshots do not mutate.
- Five-stage incident response uses native phase mapping and dispatch. Repair/verification run in parallel; eight real materials18→10 consumed once.
- Ordinary CancelOrder resumes the same incident episode/order and retained progress, without spending inputs again.
- All five phase histories complete through the existing incident effect owner, not driver-side repair.
- Native construction command creates only a physical site, then real supply hauling/staffed work completes the structure at30. Saved active-site twin stays identical.
- Completed state save/load, normal full command-stream replay and interleaved fresh-session isolation.

Sixteen malformed branches refuse before Initialize: empty/duplicate graph nodes, missing dependency/cycle/executor/requirement/role, negative duration/priority, invalid/duplicate target tags, player-orderable response, node incident completion, and player-orderable/tag-restricted/final-effect-bound construction workflow.

## Protocol and limits

Core2.339.0/save154/replay1. All existing component/system/view schemas and sole GameClock unchanged. Executable inputs frozen2026-10-09 03:32:06 UTC.

Requirement/executor/role file catalog contract migrations, arbitrary bound-provider/malicious callback fault matrices, global purity, all alternative/tool/fluid/area/technology paths, broader construction/infrastructure/staffing/unlock/visibility runtime, environmental consequences, lockstep/soak/stress/scale/full regression and final P1–P12 acceptance remain OPEN. Their source migrations compile; this focused case is not their full acceptance certificate.

The lifecycle skill guided staged ownership, specification and scoped evidence. Repository/user constraints took precedence over broad regression and early publication guidance.

No compatibility loader/native fallback, duplicate graph/progress/reservation/intent, mandatory money, Tycoon/business expansion, Draw/platform/host gameplay or second clock. Original primary acceptance text and checkbox values unchanged. Commit/push condition NOT satisfied; no staging/commit/push.

[Stage](../../plans/enhancements/P4_ACTIVITY_CONTENT_CONTRACTS_DESIGN.md), [tracker](../../plans/enhancements/MASTER_TRACKER.md), [audit](../../plans/enhancements/COMPLETION_AUDIT.md).
