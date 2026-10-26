# P11-D7 — Physical refresh of remembered observations

2026-10-07. Scoped implementation verified:84 checks/ONE exact NEW bounded case,0 failures/aborts,
423ms, first attempt; ONE clean build0 warnings/errors5.15s. Full framework goal remains active.
[Evidence](../../docs/migration/P11_OBSERVATION_REFRESH_VERIFICATION.md).

## 0. Problem statement

`Standard/CompanyKnowledge.cs` retains the first payload forever and rejects a changed reading.
The existing public `StockKnowledgeEffectDef` cannot repeat an ordinary material-consuming survey.
Learned membership is not a live sensor. No new ledger, clock, survey queue or host rule is needed.

## 1. Goals

1. Required per-subject provider retention chooses permanent first learning or latest observation.
2. SAME company ledger owns immutable payload, original learned time and actual last observation time.
3. Ordinary authored physical work refreshes readings; partial/cancelled work does not publish them.
4. Reads never consult current inventory or advance observation time. Saved continuation stays identical.
5. Master JSON/public provider proves explicit first/latest subjects without scheduler modifications.

## 2. Design

`IKnowledgeProvider.Retention(string subject)` returns `KnowledgeRetention.FirstLearned` or
`LatestObservation`; zero/unknown enum is invalid, not a default. Permanent technology and authored
fixed facts are intrinsically first learned. Public stock subjects explicitly author resource and
retention in `stock_knowledge_subjects`; existing stock effect resolves the actual subject definition.
No new stock provider or balance. This definition is intent/validation, never live stock.

`KnowledgeObservation.ObservedTick` derives from SAME World/GameClock projection. First observation
sets LearnedTick=ObservedTick. Latest refresh preserves LearnedTick and replaces only the detached
payload/ObservedTick in SAME sorted candidate list. Same-data later observation refreshes chronology;
same-data same-time and permanent same-data are no-ops. Changed permanent payload refuses.
Whole candidate semantic validation precedes replacing the ledger. Save/load/hash use SAME Sync,
including both times; reject negative/future/reversed chronology and refreshed permanent records.

This is remembered typed observation refresh, NOT dynamic spatial exploration, sample-cache replay,
sharing/forgetting/expiry, a sensor geometry or a universal service mutation sandbox.

## 3. API surface

Required retention method on existing provider; immutable ObservedTick on existing observation.
Existing Record/CheckRecord/TryRead/CopyKnown and KnowledgeView remain the sole boundary.
Knowledge codec/view advance to2; build2.230.0/save134 direct migration, no old reader.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | src/Beep.SimEngine/Standard/CompanyKnowledge.cs | 65 | high |
| Modify | src/Beep.SimEngine/Standard/CompanyKnowledgeModule.cs, Technology.cs | 3 | medium |
| Modify | tests/ExtensionProof/KnowledgeProof.cs | 65 | medium |
| New | tests/ExtensionProof/content/knowledge-refresh-work.json | 25 | medium |
| Modify | Existing knowledge work JSON/helper/codec instrumentation | 15 | medium |
| New | tests/Beep.SimEngine.Tests/ObservationRefreshTests.cs | 150 | medium |
| Modify | Program.cs, SaveGame.cs and exact older identity assertions | mechanical | low |

## 5. Error handling matrix

| Operation | Result |
|---|---|
| Missing subject/definition, malformed quantity/policy | Explicit refusal/authoring error |
| Changed permanent reading | Existing knowledge.already-known, no mutation |
| Partial/cancelled physical work | Native work/cancellation owner; no observation refresh |
| Invalid decoded timestamps/payload/schema | Reject entire candidate, retain settled ledger |
| Provider invalid enum | Contract error; no guessed policy |

## 6. Backward compatibility

Development-only direct migration. No legacy payload/JSON reader or compatibility shim.

## 7. Verification

ONE distinct NEW bounded case: `Knowledge refresh: physical observations and durable chronology`.
Build once before that case; repair/rerun only an observed failure. Older metadata/content/codec
instrumentation migrate/compile only. Freeze gameplay/JSON/public proof/tests after final green.
Record every invocation and remaining broad gates. No family/full/platform/OilFieldDays run.

## 8. Risks

Latest observations are not current truth: demonstrate inventory changes while remembered data and
timestamp remain unchanged. Permanent technology prerequisite chronology must stay unchanged.
Cross-provider validation and transient candidate records do not introduce another knowledge store.

## 9. Out of scope

Dynamic sensors/spatial remembered samples, sharing, expiry, full visibility/privacy/provider/fault/
replay/stress gates, business, OilFieldDays, hosts/demos, rendering/platforms, commit/push.

## 10. Execution order

1. Extend SAME provider/ledger/codec and migrate permanent providers.
2. Resolve explicit JSON subjects in SAME public stock effect/provider and migrate old content.
3. Add one bounded physical repeat/cancel/save/private/invalid-codec proof; build and exact gate.
4. Freeze source; record evidence/inventory and update tracker/README/AGENTS/help docs only.
