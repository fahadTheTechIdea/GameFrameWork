# P5-TA1 — Original-to-current temporal authoring audit

Completed scoped slice2026-10-16, unchanged2.639.0/save160/replay1:849 temporal facts
and11 deliberate drifts; three distinct exact finals2477/0. One initial new-test compile
failure and named-metadata runtime misuse are retained in the
[verification report](../../docs/migration/P5_TEMPORAL_AUTHORING_AUDIT_VERIFICATION.md).
No production mismatch/repair; original221/210 checked/11 open, P5.005/full goal active.

## 0. Problem and specification

Original P5.acceptance.005 covers timed production/construction/hauling/recovery/research.
Completed canonical examples do not establish every authored duration, dependency or
machine rating. The existing OilManifestContentTests already owns original-archive and
actual-master evidence; extend it with temporal facts, not another loader or simulator.

## 1. Goals

1. Verify immutable archive/file identities before reading original content.
2. Compare all original construction/removal graphs, ten work graphs, batch recipes,
   technologies, corridor segments, movement/work ratings and research-facility capacity.
3. Treat the drilling complete→casing/cementing plus zero-time final effect as an explicit
   authored migration. Preserve durations, roles and dependencies, not just totals.
4. Verify six selected minute profiles and explicit positive load/unload timing. Old
   instantaneous handling is deliberately replaced, never installed as a fallback.
5. Prove the audit detects deliberate temporal drifts. Run only exact content-audit cases;
   no generated campaign, full profile, family or full regression.

## 2. Design

Extend OilManifestContentTests with a Timing partial. Extract the existing archive reader
and actual-file inventory once into the same owner and reuse them from financial/main/
timing checks. Financial facts and authored business behavior do not change.

Read current definitions through public OilCoGame.LoadContent and resolved contracts:
IConstructionDefinition/IRemovalPlanDefinition, IAuthoredActivityDefinition,
ITransformationDefinition, ITechnologyDefinition, ILineDefinition, IMobilityDefinition,
IWorkerDefinition, ICompanyActivityRateDefinition and IResourceHandlingProfile.
Use the original GameTime converter, not a second duration parser or game clock.

Expected facts derive from original JSON hours/points/roles/dependencies/speeds. The
original worker work_rate omission means1 according to the archived implementation;
this is test provenance, not a fabricated default in the current engine. Research points
map to nominal days at the original points/day facility rating. Compare full fact keys
and per-node counts to expose missing/extra/reordered prerequisites or altered durations.

Handling profiles are a deliberate new physical requirement: assert one hour per authored
original carrier capacity for each explicitly declared supported resource and actual
profile binding. Do not claim old instantaneous loading is preserved. Continuous disposal
has no original batch counterpart and remains an explicitly identified separate feature.

Pure definition resolution only; no new worlds, Update, money, stock or progress writes.
Correlate existing physical timing evidence separately, not rerun or count-inflated here.

## 3. API surface

No planned engine API, schema, authored JSON, business-rule or clock change. If a real
mismatch appears, diagnose its original owner/authoring before altering expectations.

## 4. Files

Existing tests/Beep.SimEngine.Tests/OilManifestContentTests.cs and Financial partial;
new Timing partial; this design and corresponding migration report; master/AGENTS/README/P5.
No second source archive, content catalog, compatibility layer or gameplay owner.

## 5. Error handling

Archive/file hash mismatches, unexpected missing/extra facts and malformed contracts fail
the exact audit. Do not suppress them, invent replacement data or map unknown changes
silently. Record every actual attempt and any uncovered difference.

## 6. Compatibility

Historical schemas are read only as immutable test evidence. They never enter game
composition or provide runtime aliases, fallback content or old execution paths.

## 7. Verification

One targeted framework-test build, then the new temporal exact case and affected existing
financial/master exact cases individually (60000ms bounds/5000ms termination). These are
metadata audits, not full/game-profile runs. Keep P5.005 open unless the entire original
requirement is actually supported by the reconciled matrix.

## 8. Risks

Static preservation is not physical liveness, complete scenario replay or arbitrary mod
correctness. Intentional additions must be identified explicitly. Financial amounts and
runtime optional economics remain untouched. Source/archive inspection is not a game run.

## 9. Out of scope

Full/profile/family tests, new gameplay, financial rules, platform/Draw/adapters and Git
publication before the whole goal and plan finish.

## 10. Execution order

1. [x] Share existing evidence readers without changing their checks.
2. [x] Implement complete temporal fact mapping and explicit drift probes.
3. [x] Compile once ready and run only the three affected literal audits.
4. [x] Record discoveries/evidence and remaining original scope, with no unsupported closure.
