# P4-G52: Direct activity strategies and observed executor callbacks — Design Document

## 0. Problem Statement

`Standard/ActivityExecutors.cs` retained the obsolete `ActivityExecutorDef` base. Fixed and timed execution algorithms existed only in native definitions, while staffed execution already had a reusable strategy. Worker checks, rate reads, reapproach and itinerary callbacks used scalar entity observations without whole selected worker/target codecs; persisted mutations without Touch could evade those checks.

## 1. Goals

1. Native staffed/fixed/timed content implements existing IActivityExecutor directly on Def, with no compatibility base.
2. Fixed/timed JSON and execution share sole engine strategies, matching the existing staffed strategy.
3. Validate actual immutable executor aliases, roles and all loaded composition callbacks before Initialize.
4. Observe actual selected target/worker codecs, clock, identity and map during executor callbacks, including retained itinerary validation.
5. Prove independently authored mixed timed/staffed/fixed work, physical travel/materials, cancellation and durability through one new bounded case.

## 2. Design

Keep IActivityExecutor, WorkBoard, jobs/nodes, phase catalog, productive commitments, movement, stock/claims and game clock authoritative. Delete ActivityExecutorDef. Native registrations delegate StaffedActivityExecution, FixedActivityExecution or TimedActivityExecution; direct modules delegate the same helpers. Strict raw phase/blocker identities validate before Code construction. No parallel executor registry, itinerary owner, progress model or clock.

ActivityExecutorContracts.Read admits the actual frozen Id/Module/catalog alias and uses original EntityObservation provider/target/worker/whole-entity-codec/clock/map scopes. Original CheckExecutorWorker, ExecutorRate, ReapproachActivity, itinerary creation/matching and restored/retained itinerary checks use that same path. WorkSystem composition observes every loaded executor, unused included; original WorkBoard plan validation also checks role agreement independently of provider callbacks.

Only methods in this slice gain scoped observations. Arbitrary metadata getters in all other scheduling/graph paths, recursive providers/global CLR sandboxing and performance budgets are not claimed proven.

## 3. API surface

Existing IActivityExecutor remains the extension contract. FixedActivityExecution.Read and TimedActivityExecution.Read expose shared strict JSON strategy authoring. Modules register direct Def implementations and master-list their content. Public plan commands, candidate previews, immutable work/entity views, Update, clock controls, saves and replay are the thin host boundary. No renderer/Draw/platform API.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| New | src/Beep.SimEngine/Standard/ActivityExecutionStrategies.cs | medium |
| Modify | src/Beep.SimEngine/Standard/ActivityExecutors.cs | medium |
| Modify | src/Beep.SimEngine/Standard/Work.cs, WorkDocks.cs, WorkScheduling.cs | medium |
| Modify | src/Beep.SimEngine/Kernel/SaveGame.cs, protocol fixture | low |
| Modify | tests/ExtensionProof/WorkContentContractsProof.cs, historical executor lookups | low |
| New | tests/ExtensionProof/ActivityExecutorContractsProof.cs | low |
| New | tests/ExtensionProof/content/activity-executor-contracts.json | low |
| New | tests/Beep.SimEngine.Tests/ActivityExecutorContractTests.cs | medium |

## 5. Error handling matrix

| Callback/admission | Result |
|---|---|
| Missing/empty/invalid phase or blocker, nonpositive rate, extra field | Typed content refusal, no defaults |
| Unknown phase/purpose, mismatched immutable alias or worker roles | Composition/plan refusal before Initialize |
| Selected target/worker persisted mutation without Touch | Original observation fault; no healthy checkpoint |
| Throw/clock/map/identity mutation during callback | Original fault boundary; no silent substitute provider |
| Valid strategy decision | Original job/node/phase/work owners execute over game time |

## 6. Backward compatibility

Development only; base removed, historical references migrated compile-only. Build2.395.0/save154/replay1; no changed persistent layouts or old reader.

## 7. Verification

TWO clean completed-slice builds15.83s/8.77s, zero warnings/errors. ONE NEW exact bounded case, TWO attempts:171 passed15 failed1169ms; FINAL182/0/0 in992ms. PID27060 Exited0, observed1152ms; no timeout/truncation/observer error. [Actual evidence and repairs](../../docs/migration/P4_ACTIVITY_EXECUTOR_CONTRACTS_VERIFICATION.md). No historical/family/full/Oil/Tycoon/platform runtime.

## 8. Risks

Scoped codec checks are not global rollback or a CLR sandbox. Observation cost, recursive providers, nonparticipating entities, all metadata accessors, dock/broken-route/lost-target matrices and full industrial acceptance require broader evidence.

## 9. Out of scope

Tycoon authority G19 unanswered; protected GameClock/business files untouched. No Oil, hosts, Draw/platforms. Full P1–P12, lockstep/fuzz/soak/allocation and authorized full profiles remain OPEN.

## 10. Sub-task execution order

1. Extract sole fixed/timed strategies; delete native base and migrate all native definitions/references.
2. Add shared actual executor/selected codec observations in original callback consumers.
3. Master-list direct strategies and mixed workflow; reuse existing direct staffed proof implementation.
4. Run completed-slice build and only the new exact bounded case, preserving any repair evidence.
5. Freeze selected inputs and reconcile stage/tracker/audit without changing original compound gates.

## 11. Delivery status

Scoped verified, not whole-plan completion. Native/direct shared strategies execute a file-owned timed/physical join then fixed work through original jobs, motion, input claims and progress. Stock20→12→4, private facts, detached progress, partial/fixed/completed/cancelled saves, continued twins/pause/replay/isolation,34 native/direct authoring/role refusals,3 unused alias/composition refusals and7 selected callback faults are proven. Typed composition errors now remain the original first error instead of a secondary fault-state exception.

Selected400 executable inputs hashed AFTER final green at2026-10-09 19:51:14 UTC; no executable edits afterward. All221 original compound gate text/checkboxes remain unchanged. Broader metadata/reapproach/provider/global/performance/full gates stay OPEN. Full goal active; no staging, commit or push until the entire original goal and plan are complete and verified.
