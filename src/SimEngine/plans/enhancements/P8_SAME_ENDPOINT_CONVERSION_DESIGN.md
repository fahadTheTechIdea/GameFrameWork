# P8-NC1: atomic same-endpoint conversion — Design Document

## 0. Problem Statement

Transformer.CheckFlow calls CheckIncoming on output stock before consuming same-endpoint input. ResourceTransactions.PrepareConversion likewise demands separate inbound room and CommitConversion reserves output before consuming. Full same-store circuits therefore stall even when their complete physical conversion fits. The generic engine needs net admission, not a new processor/inventory or temporary capacity-credit balance.

## 1. Goals

1. An explicit IResourceConversionEndpoint admits the whole native consumed/produced vector and commits exact owned receipts inside its original inventory owner.
2. Existing ResourceTransactions captures every endpoint, validates all original receipts, projects complete capacity, checks generation once and verifies final stock/books before publishing deferred facts.
3. Continuous previews/admission and commit use the same net evaluator; no speculative input spending to make output room.
4. Existing other-endpoint output reservations retain complete preflight protection, and unrelated incoming promises/reusable tools remain honored.
5. Actual JSON/master connected and local full buffers process over game time; genuine positive net growth waits without acquiring/spending/progress.

## 2. Design

Core/ResourceEndpointConversions.cs declares the coupled endpoint capability; original ResourceInventoryState implements pure projected capacity and exact owned-receipt conversion against the same stock/book. Storage delegates to that state; no balance copy, saved capacity credit or fallback store.

ResourceTransactions.EvaluateConversionClaims performs pure outbound-claim and output admission, capturing ALL participants before callbacks. Refactor output admission into one helper shared with original receipt-based PrepareConversion. Coupled outputs use the endpoint contract only; unsupported coupled providers refuse explicitly. Distinct destinations retain existing receipt admission. Commit invokes whole coupled inventory mutation, updates original captured expectations and verifies stock/records/version before deferred logical consume/produce facts; no partial success event/checkpoint or compensating refund.

Transformer.CheckFlow removes its conservative output-only checks and uses the shared net evaluator before acquiring exact inputs. Original recipe, tool, running rate, progress, WIP, cancellation and generation owners remain.

## 3. API surface

IResourceConversionEndpoint.CheckConversion(consumed,produced) and ConvertReserved(owner,inputs,produced). IResourceTransaction.EvaluateConversionClaims(company,owner,inputs,outputs) is pure planning, not receipt authority. Existing authored transformation/resource-port/stock JSON is unchanged. Endpoint capability is mandatory for coupled conversions, never silently emulated through destructive consume-first fallback.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| New | Core/ResourceEndpointConversions.cs | 25 | high |
| Modify | Core/ResourceTransactions.cs | 5 | medium |
| Modify | Standard/ResourceInventoryState.cs, Inventory.cs | 90 | high |
| Modify | Standard/ResourceConversions.cs | 160 | high |
| Modify | Standard/TransformerFlow.cs | 35 | high |
| New | actual net-conversion JSON and bounded public-player proof | scoped | high |
| Modify | engine/protocol identity and documentation | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Unknown/foreign/wrong receipt/duplicate input | Original typed refusal, no mutation |
| Same-endpoint vector fits including unrelated inbound promises | Ready pure preview; exact atomic inventory commit |
| Positive net growth exceeds capacity | NoCapacity; no claim/progress/stock prefix |
| Coupled endpoint lacks declared atomic capability | Incompatible; never destructive fallback |
| Provider throws/mutates/refuses after ready | Existing terminal fault; no healthy checkpoint/success facts |
| Recipe loses original binding | Existing typed availability wait |

## 6. Backward compatibility

Development only; replace conservative flow path, no compatibility shim or duplicate conversion coordinator.

## 7. Verification

One solution compile, necessary focused repairs only. One NEW exact bounded public-player case, original JSON/master/seats/views/Update and saved twins/replay/private/isolation. No historical/family/full suite. Preserve every compile/runtime attempt.

## 8. Risks

Input capacity credit must derive only from validated actual receipts or complete pure claim admission. Existing tool leftovers and incoming promises are not consumed/released implicitly. Deferred consume/produce deltas describe ordered internal mutations in one fully verified commit, never externally visible partially committed stock.

## 9. Out of scope

Oil/platform/rendering/Tycoon expansion/protected G19/clock edits, new queues/ledgers/defaults, full arbitrary-provider/fault/stress/whole-plan acceptance.

## 10. Sub-task execution order

1. Add the explicit coupled endpoint contract to the original stock owner.
2. Share net-output admission between proposed claims and original receipt conversion; verify exact coupled commits.
3. Route continuous processing through the single evaluator.
4. Prove full-buffer progress and positive-net refusal with original physical workflows, then update evidence/master/audit.

## 11. Scoped result — 2026-10-12

Core2.473.0/save154/replay1. Original ResourceInventoryState/Storage implement explicit atomic same-endpoint capability; original ResourceTransactions shares complete output admission with pure proposed-claim preview and verifies actual coupled commits. Continuous processing no longer demands artificial extra room before its own input consumption. No saved capacity credit or duplicate stock/coordinator/clock owner.

ONE NEW exact FINAL753/0/0 in19085ms, process19229ms, PID30924 exit0. Eight native/independent metadata minute/half owned/public point flows physically fill network12, then both actual network and local full12 circuits process net-zero inputs/outputs, preserve tool remainders, wait on real growth, drain2 physically and generate exactly2 before blocking again. Private previews/journal/accounting/saves/replay/isolation agree. All four builds/four attempts retained in [evidence](../../docs/migration/P8_SAME_ENDPOINT_CONVERSION_VERIFICATION.md). No historical/full runtime or executable edit after FINAL.

Original221/185checked/36open unchanged; additional199. Arbitrary atomic providers/mixed dimensions/multi-endpoint/fault/inbound/stress/native-tile/global/full gates remain OPEN. No staging/commit/push until full goal AND plan finish.

## 12. Metadata closure

Fresh read-only metadata closure:1669 source anchors,926 selected executable inputs,13 quickstart anchors, all SHA drift0. Inventory2857 paths/1006 Markdown files/8636 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors,185checked/36open. New driver boundary scan0; git diff --check exit0 (34 existing warning lines), dirty2845/staged0. Protected sole-clock/G19 files remain exact; goal_complete=false/additional199. Eight prior selected inputs changed, five added (three new/two existing production files newly frozen); six existing production files/one new contract, five new source anchors. Original requirements unchanged and no runtime rerun for documentation checks.

