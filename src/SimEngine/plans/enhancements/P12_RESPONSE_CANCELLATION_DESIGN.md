# P12-A7: response cancellation and episode durability

## 0. Problem statement

Original P12.acceptance.005 remains unverified: stale episode/cancel/repost/interrupted saves must neither duplicate repairs nor clear newer damage. Prove the native cancellation/resume and episode-bound completion owners through interrupted physical execution before changing them.

## 1. Goals

1. Cancel timed detection, physical isolation, paid stabilization, parallel paid repair/cleanup, blocked readiness and partial restart through public input. Enrollment must resume the same order/episode, preserving progress, paid resources and done nodes.
2. Preserve an independent partly loaded supply job/cargo during response cancellation.
3. Complete a second real incident while an actual old completed order attempts native completion. Neither stale nor unfinished completion may clear current damage.
4. Save pending/partial states, pause, compare restored twins on every Update, conserve stocks/WIP/consumption, settle claims, replay and prove session isolation.

## 2. Design

Reuse Standard/Incidents.cs ResponseWork, Standard/Work.cs WorkBoard.ResumeResponse/WorkSystem.EndOrder and native Incident.CompleteResponse. PhysicalRecoveryScenario adds optional pure JSON authoring for two episodes and explicit96-unit worker fuel stores. Shared A6 observations/diagnostics are reused. Initial stock is264fuel/30steel/4spares/4suppressant/2waste/1reusable kit. No driver changes world, scheduling, progress or reservations.

The non-friend ExtensionProof assembly registers a stateless IWorkEffect in master-listed response-cancellation.json. The native DAG invokes it after detection; it calls actual IIncidentResponse.CompleteResponse with the current unfinished order and, for episode2, the actual prior completed order. Applied requires both StaleResponse refusals and unchanged active episode facts. It owns no repair, incident, resources or persistent state. The target explicitly selects existing ActivityRecord/IWorkOrderRetention so the actual native prior order remains available; no history copy or pruning change.

## 3. API surface

No production API/schema changes planned. Normal CancelOrderCommand, RaiseIncidentCommand, physical hauls, production commands and detached views. One proof-only JSON effect/module.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| New | tests/Beep.SimEngine.Tests/ResponseCancellationAcceptanceTests.cs | 130 | Medium |
| New | tests/ExtensionProof/ResponseCompletionGuardProof.cs | 45 | Low |
| New | tests/ExtensionProof/content/response-cancellation.json | 1 | Low |
| Modify | tests/Beep.SimEngine.Tests/PhysicalRecoveryScenario.cs | 28 | Medium |
| Modify | tests/Beep.SimEngine.Tests/EmergencyResponseAcceptanceTests.cs | 13 | Low |
| Modify | tests/Beep.SimEngine.Tests/Program.cs and tests/ExtensionProof/ExtensionProof.csproj | 2 | Low |

## 5. Error handling

Each physical wait bounds512 Updates with phase/job/fuel diagnostics. Instrumentation failure is a typed blocker, never invented success. Outer90000ms/termination5000ms/output30000 characters. Observe the same handle to terminal; no restart on observation timeout.

## 6. Compatibility

Development only; no compatibility path or second owner. Existing fixture defaults unchanged.

## 7. Verification

One solution compile, then ONE NEW exact case: `Incident durability: cancellation retained work and stale episode completion refusal`. Same-case repair compiles/runs only. [Evidence](../../docs/migration/P12_RESPONSE_CANCELLATION_VERIFICATION.md). Historical/family/full runtime unrequested; full P1-P12 remains open.

## 8. Risks

If execution exposes a defect, repair only the native responsible owner. Additional travel needs authored fuel, never grants/instant recovery. Stale completion must be an actual native call, not a fake order or client-side simulation.

## 9. Out of scope

OilFieldDays, business/Tycoon, renderers/adapters, old/full runtime, arbitrary providers and propagation/environment acceptance, publication before full goal/plan completion.

## 10. Execution order

- [x] Read native response/cancellation/lifecycle/completion owners.
- [x] Author repeated-episode content and stateless contract instrumentation.
- [x] Add one thin driver/registration and reuse observations.
- [x] Compile and run one bounded case; retain every attempt.
- [x] Refresh tracker/audit/source hashes and scoped evidence.
- [ ] Broad regression remains unrequested and unproven.
