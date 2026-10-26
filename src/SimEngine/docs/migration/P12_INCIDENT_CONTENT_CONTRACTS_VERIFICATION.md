# P12-A4 / P4-G6 — Incident and effect content contracts verification

Historical proof on2.338.0; not rerun on2.339.0. [P4-G7 authored activity contracts](../../plans/enhancements/P4_ACTIVITY_CONTENT_CONTRACTS_DESIGN.md) now replaces the concrete ActivityDef response link and proves an independent five-phase authored DAG through the same physical owners. [Current scoped evidence](P4_ACTIVITY_CONTENT_CONTRACTS_VERIFICATION.md). Requirement/executor catalogs and full incident breadth remain open.

Scoped verified 2026-10-09; full P1–P12 and the active goal remain open.

## Ownership and implementation

IWorkEffect now extends IContentDefinition. WorkBoard execution/validation/preview, authored activity intent, entity activities, technology and network consumers resolve actual IWorkEffect aliases. Native WorkEffectDef remains an implementation base, never a consumer requirement. WorkEffectContracts owns the original refusal/retry classification independently of provider inheritance. Existing observation guards cover optional effect preview/plan-validation callbacks; no new effect registry, job store or state.

IIncidentCompletionEffect declares explicit source/bound-response metadata. Native and independent effects use one IncidentCompletionDeclaration parser and ResponseWork.ApplyCompletion implementation. It admits the actual owned content alias, actual native order/final invocation, episode and physical whole-order completion before calling the existing response owner. Node, forged, stale and already-applied invocations cannot bypass completion. The incident component alone mutates episode timestamps; WorkBoard alone owns nodes, materials/jobs, completion flags and history.

IncidentDef and IncidentSignalDef resolve IIncidentWorkflowDefinition. One IncidentWorkflowDeclaration parses native/independent workflow JSON. Reference binding occurs in resolution; full phase/physical-response/effect admission occurs during IncidentResponseModule composition AFTER all content resolves and before Initialize. This removes dependence on section/file ordering. Every selected workflow uses the same IncidentPhases validation, actual catalog aliases, native graph and typed completion contract. Phase views derive actual board nodes and expose no live progress handles.

Master-listed incident-contracts.json owns the direct workflow pressure.leak and effect pressure.resolve; original response document no longer defines those IDs in this fixture. Independent implementations have no IncidentWorkflowDef/WorkEffectDef inheritance and delegate the shared engine authoring/completion owners. The existing thin authoring/player helpers are reused, not copied. No driver-side spawn/stock/progress/repair logic.

## Build and runtime evidence

All builds: `dotnet build Beep.SimEngine.slnx`.

1. Clean7.14s, zero warnings/errors, core migration.
2. Failed4.37s, zero warnings/four syntax errors: new invalid-content assertion had an extra closing parenthesis.
3. Clean4.68s, zero warnings/errors.
4. Clean6.03s, zero warnings/errors after relocating workflow validation to composition.
5. Failed2.49s, zero warnings/one error: final classifier extraction left TechnologyCompletionDef calling the removed inherited Outcome.
6. Final clean6.96s, zero warnings/errors after migrating that caller.

Only ONE NEW exact case ran, with the same case rerun after changes:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Incident content contracts: independent workflow and completion drive physical episodes and saved phase history" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

- Attempt1:0 passed/one failed,172ms; PID31092 exit1/observed323ms. Found a real resolve-order dependency: independent workflow section preceded the response activity graph. Moved full validation to composition after complete resolution; did not reorder fixtures or fabricate a graph.
- Attempt2:77 passed/zero failed/zero aborted,700ms; PID6856 exit0/observed845ms, before final classifier extraction.
- Non-evidence stale-artifact run after build5 failure:77/0/0,704ms; PID20332 exit0/observed852ms. This inadvertently executed the previous binary and is explicitly NOT current-source verification.
- Final current-source run after clean build6:77 passed/zero failed/zero aborted,696ms; PID3164 exit0/observed841ms. No truncated output/observer error.

No full/family/historical/Oil/Tycoon/platform runtime. Old suites and QuickGame compiled only.

Verified direct owned aliases, once-only Initialize, immutable/private detached phase metadata and truthful no-episode facts; unbound player-order refusal/pure views; normal incident/automation/work commands; actual emergency dispatch and machine travel to position20; eight real materials20→12; partial repair, pause, normal cancellation/resumption with same order/episode and retained progress/no repeat cost; resolved timestamps/history and native phase completion; second distinct episode physically spends another eight12→4; settled updates do not duplicate effects; saved raised/partial/interrupted/resolved/second-episode twins, replay and interleaved-session isolation.

Sixteen malformed authoring branches refuse before Initialize: kind, response reference, empty/duplicate phase identities, absent/duplicate/unmapped nodes, final effect, unbound/both/invalid-source completion binding, component workflow reference, player-orderable response, node completion, graph cycle and missing physical machine work.

## Protocol, current-source boundary and remaining work

Core2.338.0/save154/replay1; incident component2/response system1/incident view8, WorkBoard52/WorkSystem45/work-view94, automation5/operations-view11 and all other existing state schemas unchanged. Executable inputs frozen2026-10-09 03:13:30 UTC. Sole GameClock unchanged.

The shared workflow JSON response link still uses the existing authored ActivityDef catalog. Independent activity/requirement/executor catalogs remain a separate open migration, not proven by this workflow/effect stage. This proof uses native incident state and the existing two-phase response, not complete arbitrary incident-state providers or all five-stage industrial emergencies. Optional effect preview/plan-validation guard migration is source/compile-only here, not a full malicious callback/fault matrix. Global provider purity, invalid direct callback metadata, environmental propagation/accounting, lockstep/soak/stress/scale/full regression/final acceptance remain OPEN.

No compatibility parser/old loader/native-base fallback, duplicate workflow/DAG/phase progress/reservations, mandatory money, business expansion, host/Draw/platform gameplay or second clock. Original primary acceptance requirements/checkboxes unchanged. Full goal AND plan must genuinely finish and be verified before staging/commit/push; publication condition NOT satisfied.

[Stage](../../plans/enhancements/P12_INCIDENT_CONTENT_CONTRACTS_DESIGN.md), [tracker](../../plans/enhancements/MASTER_TRACKER.md), [audit](../../plans/enhancements/COMPLETION_AUDIT.md).
