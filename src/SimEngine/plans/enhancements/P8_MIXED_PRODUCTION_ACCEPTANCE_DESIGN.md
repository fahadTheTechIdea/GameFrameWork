# P8-MX1: mixed industrial production acceptance — Design Document

## 0. Problem Statement

Original P8.acceptance.011 is already implemented and checked. Its existing thin-player proof exercises mixed solid/liquid ports, remotely held catalyst wear, physically supplied utilities, multi-output and entity results together. The current P8.017/P7.017 gap is broader accounting qualification, not a missing recipe/production/stock subsystem.

Current M137 on unchanged native2.767/save161/replay2 extends the SAME driver with per-update material conservation and complete-live replay checks. Actual original baseline181/0 becomes11712/0; no native, proof-content, rule or schema change is needed. [Current execution, ownership, limitations and next work](../../docs/migration/P8_MIXED_PRODUCTION_ACCEPTANCE_VERIFICATION.md#current-m137-per-update-mixed-material-accounting--2026-10-23).

## 1. Goals

1. Actual master-listed JSON authors a mass/volume mixed batch with remote catalyst, liquid operating draw, two resource outputs and a legally placed entity result.
2. Machines physically haul finite source materials and staff the activity; no resource injection or host gameplay.
3. First batch commits all results/catalyst wear once after elapsed work; second full-output batch retains WIP/tools and burns no further utility fuel.
4. Clearing only one output never publishes a partial result or entity; clearing both resumes whole conversion.
5. Private/pure previews, saved physical/working/blocked twins, resource journal accounting, replay and interleaved isolation agree.
6. Preserve the original completed P8.011 scope and wording; do not close P8.017/P7.017 from one selected workflow.
7. Compare each material's actual buffers/cargo/WIP/vessels with its immutable original journal after every driven primary update, including ordinary commands. Initialization cannot repeat, and unrelated creation/loss/sales classifications cannot compensate a leak.
8. Verify both full recordings start at the original unsupplied primary and finish at its actual live step/hash, with every command and checkpoint covered.

## 2. Design

Use original PointGame physical space/motion, WorkModule/StaffedTransformationGate facility jobs, Inventory/Logistics hauling, Transformer/ResourceTransactions and EntityRollout lifecycle. EndpointProofModule and OutputPortProofModule expose actual remote vessels through existing port contracts/stock books. No test-only conversion command, slot write or second quantity/timing model.

JSON resources declare explicit positive mass/volume measures; carriers/work buffers have authored compatible policies and handling rates. A long facility support activity assigns a dedicated machine role. Two queued six-minute batches each consume2 solid/4 liquid, hold0.5 remote catalyst with0.25 completion wear, use liquid fuel over game time, produce1 solid/2 liquid plus one empty-state result entity. Numbers are fictional authored gameplay content, not scientific engineering claims.

## 3. API surface

Existing HaulResourcesCommand, PostOrderCommand, EnqueueTransformationCommand, ConfigureOperationCommand, CancelOrderCommand, engine views/saves/replay/Update. No new production API/schema or mandatory business module.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| Existing, unchanged | tests/ExtensionProof/content/mixed-production*.json | authored | medium |
| Extend existing assertion/replay path | tests/Beep.SimEngine.Tests/MixedProductionAcceptanceTests.cs | scoped | medium |
| Existing, unchanged | proof resources/runner registration | scoped | low |
| Maintain | original requirement text/statuses, existing evidence/master/audit | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Unfilled remote tools/feed/fuel or missing staffed machine | Existing typed wait, no completed result |
| One full output vessel | Whole WIP/tool/result wait |
| Only one vessel drained | No partial resource/entity output or extra catalyst wear |
| Both vessels physically emptied | Existing complete conversion/rollout once |
| Invalid quantity/packing/placement | Original refusal, no stock fallback |

## 6. Single implementation

Development only; no shim or duplicate workflow. Use accepted existing JSON, not historical illustrative schema.

## 7. Verification

Compile only the targeted test project when assertions change. Run only the EXISTING exact mixed-production literal at minute and half-minute quantum, retaining600Updates per stage and the current60000ms/5000ms termination bound. Verify complete-live replay identity, material conservation after each driven primary update and saved twin equality. Profile-info is metadata only. No solution/full/family/profile/soak runtime or unrelated green reruns; preserve every attempt and the original gate statuses.

## 8. Risks

Every resource must declare both policy dimensions explicitly; no fabricated zero measure. Dedicated operator avoids stealing the physical carrier. Remote vessels are actual service endpoints, not local copies. Entity placement follows selected spatial provider rules, not platform drawing.

## 9. Out of scope

New science/business/platform/Draw/cargo media/catalog migration; arbitrary provider/fault/quality/paid costs/stress/performance/global/full acceptance.

## 10. Sub-task execution order

1. Preserve the already-authored workload, JSON module documents and actual engine owners.
2. Extend only the existing driver assertions; ordinary player policies/orders/hauls remain the execution path.
3. Compile the changed test project, verify native/proof image preservation, and run the one literal within its existing bounds.
4. Record the two actual complete replay identities and per-update resource accounting; update existing evidence/master without treating a selected proof as full P8/P7 closure.

## 11. Historical scoped result — 2026-10-12

Core2.474.0/save154/replay1 unchanged; no production source/API/schema change. Existing contracts execute actual authored mixed mass/volume WIP, remotely held catalyst/completion wear, timed liquid utility draw, real machine staffing, whole two-resource outputs and entity rollout. Separate authored areas/service bays/exits respect existing physical collision and dock rules.

ONE NEW exact FINAL181/0/0 in6916ms, PID10504 Exited0/process7121ms; minute/half-minute physical supply/working/full-output/one-vessel/all-vessel saves/private previews/journal/replay/isolation. All five builds/four attempts retained in [evidence](../../docs/migration/P8_MIXED_PRODUCTION_ACCEPTANCE_VERIFICATION.md). No executable edits afterward or old/family/full runtime.

Original P8.acceptance.011 ONLY now checked, original words/line unchanged. Original221/186checked/35open; additional201. Broader/provider/paid-cost/quality/mixed-ratio/native-tile/CLR/fault/congestion/performance/stress/global/full gates OPEN. No staging/commit/push until full goal AND plan finish.

## 12. Historical metadata closure — 2026-10-12

Fresh read-only metadata closure:1680 source anchors,933 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2868 paths/1010 Markdown files/8642 local links, broken0. All18 master rows parse and all221 original literal/checkbox/line anchors validate with0errors. Only P8.011 changes checkbox, wording/line unchanged;186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2856/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional201. Two prior selected inputs changed, four new executable inputs/six new source anchors, zero production sources changed/created. No runtime rerun for documentation checks.

