# P4-G58 — Earned completion and active-node durability

## 0. Problem statement

Standard/ActivityCompletionHistory.cs and Views/WorkView.cs can publish earned history, active nodes, progress and requirement results after the original World has faulted. The original P4.acceptance.011 requires prior completion facts and active nodes to survive saved twins; historical proof inspects mutable provider state and is not current evidence.

## 1. Goals

1. Existing completion and active-work query owners refuse faulted reads before early returns; failed queries leave outputs cleared.
2. Original compact completion history drives an authored dependent graph across blocked, partial, paused, queued cancellation, repeated and terminal saved states.
3. Native and independent prior-requirement contracts share the original board and evaluator; private, detached facts do not change simulation state.

## 2. Design

Add the original RequireHealthyUpdate gate to completion identity/read methods and selected WorkView completion, requirement, node, progress and order queries. Build callback-dependent detached lists locally and publish only after successful healthy evaluation. No history/graph/reservation/state/clock copy, compatibility path or new scheduler. The original Sync payloads remain unchanged.

Use explicitly master-listed completion-durability.json through the existing PriorActivityTests.Factory input factory (not its runtime case). Its dependent two-stage graph combines existing native target/company facts and an independent contract requirement. An explicitly armed negative update fault in the existing proof module tests stopped-engine reads; it never implements gameplay.

## 3. API surface

Existing IActivityCompletionHistory and WorkView signatures only. Faulted observations throw InvalidOperationException carrying the original latched fault. Healthy missing/private queries retain their current typed results.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | Standard/ActivityCompletionHistory.cs, Views/WorkView.cs | medium |
| Modify | Standard/WorkModule.cs, Kernel/SaveGame.cs, current-engine-protocol.json | low |
| Modify | ExtensionProof/PriorActivityProof.cs, ExtensionProof.csproj, test Program.cs | low |
| New | ExtensionProof/content/completion-durability.json, CompletionDurabilityTests.cs | medium |
| Modify | Primary P4 design, MASTER_TRACKER, completion audit and current headers | low |

## 5. Error handling

Healthy missing/private intent remains false or typed refusal; faulted queries reject even invalid/missing inputs. Local callback observations never publish a partial result prefix. Preserve original first fault; do not invent completion or refund consumed input.

## 6. Compatibility

Development only; no compatibility layer. Build and work-view identity advance; persistent codecs unchanged.

## 7. Verification

One NEW exact bounded case for this slice, same-case repairs only. Build the completed slice once as needed. No old/family/full/Oil/Tycoon/platform runtime. Original P4.acceptance.011 can close only if current runtime proves its stated scope; aggregate/full gates remain open.

## 8. Risks

Do not mistake a partial order for earned full completion. Keep compact company/target generation/execution identities canonical. Retained views remain detached; changes to a later execution must not alter an earlier fact. Pauses use the sole game clock or actual work policy, not test-controlled progress.

## 9. Out of scope

G19 protected Tycoon references, rendering/adapters, arbitrary provider purity, full graph/transaction/fault/stress/soak matrices, full regression and publication.

## 10. Execution order

1. Read original owners and authored chain; add healthy publication gates in place.
2. Author and master-list the dependent graph; add thin public-player saved-chain proof and negative stopped-engine probe.
3. Build and run only that exact bounded case; preserve failed attempts.
4. Record current-source evidence and update original requirement/tracker without closing aggregate scope.

## P4-G58 delivery — original completion and active-node durability

[Stage](P4_COMPLETION_DURABILITY_DESIGN.md), [evidence](../../docs/migration/P4_COMPLETION_DURABILITY_VERIFICATION.md). Core2.407.0/save154/replay1/work-view97. Original history/graph/work/resource/clock owners require healthy completion and active-work queries; detached callback-dependent facts publish only after successful healthy evaluation. ONE NEW exact FINAL78/0/0 in676ms (PID26368 Exited0/819ms observed), outer30000ms; two same-case attempts/two clean builds retained. Explicit master-listed native/independent prior requirements drive blocked/partial/policy-paused/first-and-second-node/queued-cancel/repeated/terminal saved twins, exact compact identities after pruning, actual24 total consumption/8 left, private detached facts/replay/isolation and stopped-engine query refusal. Original P4.acceptance.011 is checked with unchanged text; only that checkbox changes.475 selected inputs frozen2026-10-09 22:55:22UTC,11 changed/new, no post-pass executable edits. Arbitrary providers/callback-time faults/other lifecycle/global/full gates remain OPEN; G19 pending; no publication.
