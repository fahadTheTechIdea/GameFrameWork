# P7-AR2 committed resource sources — Design Document

## 0. Problem Statement

Standard/WorkResources.cs EvaluateResource queries every offered endpoint even after an occurrence has a committed original source. An unrelated alternative's invalid check can poison original work. Its final refusal branch preserves selected source checks only for running uses and discards missing-source, so a held input's actual Pending/Blocked policy refusal becomes generic lost-claim.

## 1. Goals

1. After commitment, only the original endpoint undergoes stock/source checks; alternative metadata cannot authorize substitution or poison work.
2. Preserve original candidate CheckSource status/blocker for held and running resources; absent original reports missing source rather than falsely invalidating an existing receipt.
3. Maintain initial deterministic alternatives, pure company-private observations and original receipt/stock/scheduler ownership.
4. Prove independent contract providers, ordinary policy controls, original holds/running consumption/cancellation/saves/replay and malformed source boundaries.

## 2. Design

In the existing EvaluateResource loop, skip candidates whose endpoint differs from bound.Choice.Endpoint before stock/provider checks. Candidate metadata validation remains mandatory for the full enumeration. No alternate-selection state is stored.

If no candidate succeeds and there is a committed binding, retain its original intent. A matching original offer's result replaces the fallback for every use, including held and typed missing-source. A missing original offer reports missing-source: omission is not evidence that the actual reservation has been lost. True invalid receipts still use ResourceRefusal(InvalidReceipt) and the existing lost-claim recovery. Spent consumption remains exempt from repeated physical-source admission.

Use an independent JSON-authored requirement provider over existing ResourceRequirementDeclaration, public IResourcePort.ResolveStore, real pool ports and existing AutomationProbe control state. It enumerates actual original/alternative ports and uses CheckSource for an authored policy guard. A negative check on non-original candidates proves the engine never invokes them after commitment. No additional provider state, book, scheduler or host rule.

Evidence-driven repair: missing original offers were wrongly classified as invalid receipts and released valid holds. Missing-source now pauses without losing original intent. Malformed resource preview callbacks/validation also escaped without a session fault. WorkBoard.EvaluateRequirement now uses the existing EntityObservation.ReadProvider guard over the canonical algorithm, original target/worker codecs and actual selected space; all preview/live callers receive the same first-fault behavior. The private core method contains the existing algorithm, not a second evaluator or recovery model.

## 3. API surface

No new production contract. Existing IResourceWorkRequirement and immutable work criteria/candidate semantics become stricter; work view and engine identity advance. Save154/replay1 unchanged.

## 4. Files to change

WorkResources.cs original evaluator, Work.cs canonical guarded entry, WorkModule.cs view registration, Kernel/SaveGame.cs identity, protocol fixture; one independent module/JSON document, one public-driver case, explicit embedded-resource packaging and registration. Tracker/evidence/anchors update after verification.

## 5. Error handling matrix

| Situation | Engine outcome |
| --- | --- |
| Original held/running CheckSource Pending or Blocked | Exact original status/blocker; retained receipt/intent |
| Different alternative during committed occurrence | No stock/provider check or substitution |
| Original omitted from offered sources | Missing-source pause, original intent/valid receipt retained |
| Malformed/duplicate/cost or unavailable-plus-candidates enumeration | First provider invariant fault, no healthy continuation |
| Provider mutates or throws in pure query | Original session fault, no healthy save/hash; independent session untouched |

## 6. Backward compatibility

Development only; change the canonical evaluator directly, no legacy branch.

## 7. Verification

One solution build plus necessary focused repairs only. ONE NEW bounded public-player case over actual master-listed point files: real physical supply to both original/alternative stocks; actual operation control commands; held/running and cancellation/restoration, source omission, spent consumption, saved pause/private/replay/isolation; independent negative query probes. Bounded stages and outer timeout. No old/family/full runtime.

## 8. Risks

Initial alternatives still participate in stable metadata validation and ordinary uncommitted selection. This is not a provider sandbox or broad network/graph/worker-local regression. Full gates remain open.

## 9. Out of scope

Oil, platform/Draw/adapters, protected G19/Tycoon/clock implementation, broad regression and publication.

## 10. Sub-task execution order

1. Constrain existing committed source checks and retain original refusals.
2. Author independent provider/content and simulated-player proof.
3. Run one focused gate; retain all attempts, update canonical tracker and evidence.

## 11. Status

Core2.481.0/save154/replay1/work-view100. Existing evaluator checks only committed original; exact held/running status and blocker retained. Missing offered original is missing access, not evidence of invalid receipt, and pauses valid original claim/intent. Canonical requirement entry guards actual target/worker codecs and selected space through existing observation/fault owner. NEW FINAL1455/0/0 in16594ms;32 point minute/half policy/use/cancellation workflows,2 spent-input workflows and9 fault cases. Six compiles/four attempts retained, focused final13.31s clean; no executable edit after FINAL. Original221/186checked/35open unchanged, additional208. Worker-local/elapsed/network/all-graph/actual-invalid-receipt/private CLR/concurrency/stress/broad/full P1-P12/publication remain OPEN. [Evidence](../../docs/migration/P7_COMMITTED_RESOURCE_SOURCES_VERIFICATION.md).

Fresh read-only metadata closure:1707 source anchors,950 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2895 paths/1024 Markdown files/8663 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2883/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional208. Seven existing selected executable inputs changed; three newly created executable inputs/five new source files, four existing production sources changed/zero new. No previously existing input newly admitted to freeze. Work view100 records original-source/blocker/fault semantics. No runtime rerun for documentation validation.
