# P11-K1: knowledge ledger observation durability — Design Document

## Current M82: original provider lookup observation

Native2.726/save161/replay2. The original CompanyKnowledgeService.Provider now reads provider IDs
through its existing Observe/EntityObservation boundary before lookup can return, including missing
and unsupported-freshness results. Actual24-context old-engine reproduction fails184/24; corrected
negative352/0 plus native freshness84/0 and authored physical discovery44/0 pass narrowly. Known
work consumables/tools are separated from opaque unknown observation output in the existing proof
fixture, retaining18-minute work/eight consumed/two held units and one actual source inventory.
[All attempts, corrected source responsibility, fixture semantics, actual images and remaining
gates](../../docs/migration/P11_KNOWLEDGE_LEDGER_DURABILITY_VERIFICATION.md#current-m82-observed-knowledge-provider-lookup--2026-10-22).
No second ledger/registry/cache, visibility bypass, gameplay default or clock. The problem below
records the earlier K1 repair; it does not imply those earlier callback paths remain unguarded.

## 0. Problem Statement

The original CompanyKnowledgeService returns private stored facts from cached views after session faults. Provider CheckObservation/Retention/ValidateKnowledge are unobserved in several record/save paths; policy/provider sharing and forgetting callbacks use finally verification that can mask first exceptions.

## 1. Goals

1. Every public ledger query/mutation rejects original session faults before early private/missing/invalid shortcuts.
2. Pure provider and policy callbacks use original observation and preserve first exceptions; no malformed facts or partial ledger commit.
3. Shared preview/command policies use detached actual authored subject sets.
4. Physical material-consuming learning, freshness, sharing/forgetting, saves/replay and private immutable facts remain engine-owned.

## 2. Design

One scoped observation helper in existing CompanyKnowledgeService delegates EntityObservation.ReadProvider and guards its existing transient reentry flag, with cleanup only in finally. All record, freshness, policy admission, share/forget and save/load candidate validation callbacks use it. Capture authored subject lists under the policy observation before evaluation. Existing candidate commit, sorted canonical knowledge payload, first/latest retention and one clock remain unchanged. KnowledgeView delegates to this owner; no duplicate registry/ledger/view semantics.

## 3. API surface

Existing public contracts/JSON/view5/state3 unchanged. Build2.456.0/save154/replay1; no compatibility path.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | src/Beep.SimEngine/Standard/CompanyKnowledge.cs | 130 | medium |
| New | engine-side negative policy/subject module, explicit JSON/module and public player driver | 250 | medium |
| Modify | protocol, registrations and checkpoint/evidence tracker | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Callback throws after changing clock | Original exception retained; fault latched; no candidate commit |
| Callback changes clock without throwing | Original observation rejects |
| Faulted cached missing/private/empty/invalid query | Reject before publishing facts |
| Valid unearned/disallowed policy | Existing typed refusal and own-only projection |
| Save/load provider validation fails | Reject checkpoint/loaded session; original session isolated |

## 6. Backward compatibility

Development framework; no legacy or compatibility. No knowledge payload migration.

## 7. Verification

ONE NEW exact bounded case: Knowledge ledger durability: physical learning and policy faults preserve private facts. Existing explicit master/JSON pure factory; normal public player work, input consumption, saved partials, freshness, sharing, forgetting, replay. Negative authored policy admission/runtime and observation callbacks, cached missing/private/invalid queries and no published prefix. One solution compile attempt then necessary repairs only; no old/family/full runtime. Preserve all attempts.

## 8. Risks

Keep legitimate ledger mutations outside pure observation. Avoid recursive Sync/Validate by observing actual providers, not the ledger's own full codec. Nested legitimate private reads remain allowed; recursive writes refuse. Do not claim arbitrary CLR field sandboxing or all optional-provider durability.

## 9. Out of scope

Oil, platform/Draw, protected clock/G19/advanced Tycoon, broad spatial/menu/delta/provider/full/stress/performance gates, publication until whole goal and plan finish.

## 10. Sub-task execution order

1. Inspect sole ledger/private policy owners and author this stage.
2. Refactor existing callbacks and cached facts in place.
3. Add one public-player/JSON proof, compile and run exact bounded case; retain failures.
4. Record evidence and current hashes/gates/tracker without marking broad requirements complete.

Status: scoped verified on2.456.0/save154/replay1; [evidence](../../docs/migration/P11_KNOWLEDGE_LEDGER_DURABILITY_VERIFICATION.md). Lifecycle skill supplies staged ownership/evidence; user no-full-suite rule takes precedence. Full goal and plan remain OPEN.

## Verification result

ONE NEW exact FIRST/FINAL2077/0/0 in1219ms, PID26804 Exited0/process1395ms, no output truncation/observer error. Four native/direct policy minute/half physical learning/freshness/share/forget/cancel/rediscovery/save/replay flows; finite source20 to18 to16 to14, current observations and recipient historical time preserved, minute final29/half52. Nine runtime policy/provider throw/clock/clock-throw variants plus Save validation and composition clock-throw preserve first faults, no fact prefixes or cached private shortcuts. Initial solution4.22s failed on negative proof-only internal JsonValue.Freeze; necessary targeted6.21s clean after immutable authored string capture. Both compiles retained; one runtime after terminal clean build, no executable edits after FIRST/FINAL. Original221/184checked/37open unchanged; broad P3.008/P4.014/P11.033/P11.035/global/provider/full/stress/publication remain OPEN.
## Metadata closure

Current closure:1574 source anchors,13 quickstart anchors,843 selected executable inputs; zero hash mismatches/post-pass executable drift.2766 paths/972 Markdown files/8595 local links, zero broken links;18 valid master rows;221 original literal gate/checkbox anchors exact and unchanged (184checked/37open), additional182. Thin driver boundary scan empty. git diff --check exit0 (34 existing line-ending-warning output lines),2754 dirty paths/zero staged. Sole clock/protected G19 hashes unchanged. Six new files: four executable proof/driver/JSON and two stage/evidence documents, all admitted to source anchors. Five previously frozen executable inputs changed (two production); existing consuming dependencies already frozen. No executable edit after FIRST/FINAL. Metadata integrity is not full gameplay/provider/acceptance evidence.
