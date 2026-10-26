# P7-ER11 — Controller-owned live references

## 0. Problem

PlayerControllersSystem owns strategy instances, schedules, pending command tickets and replies. Strategies can own live entity intent, but the system did not participate in the existing replacement-reference contract. A saved dormant strategy could therefore resume against a retired target after a physical upgrade.

## 1. Goals

Pass compatible live-reference handoff to the actual company-owned strategy, refuse incompatible intent before work, preserve scheduling and replies, and prevent another company's controller from receiving private replacement callbacks.

## 2. Design

The existing PlayerControllersSystem implements IEntityReplacementReferenceOwner. Pure admission delegates to a strategy only when its actual binding company owns the source and the strategy declares the same optional contract. The existing EntityObservation guard checks callback purity. Commit delegates to those same actual instances; the original Upgrades coordinator remains the sole authority.

No controller is recreated. Decisions, Last, Next, Awaiting and Answers remain unchanged. Native playbooks own plan ranges rather than live entity references, so they receive no fabricated entity mapping. Historical replies and sightings are not rewritten.

The company filter is intentional: another company's observed activity is historical knowledge, not owned current intent. It cannot be retargeted through a private upgrade notification.

## 3. API and state

Reuse IEntityReplacementReferenceOwner, IPlayerControllerState and IPlayerControllerStateValidation. No new scheduler, controller registry, reference table, clock or saved payload. Core2.568.0/save157/replay1; native controller system schema1 unchanged. Existing independent current-engine-protocol JSON now declares2.568.0 rather than stale2.566.0.

## 4. Files

Original PlayerControllers.cs; current build and protocol declaration; independent ControllerReferenceReplacementProof.cs with its master-listed controller-replacement.json; public-player ControllerReferenceReplacementTests.cs and explicit runner/resource registration.

## 5. Errors

Foreign session callbacks refuse. A nested strategy's typed refusal reaches preview and inbox before the order is accepted. Mutating admission and throwing commit latch the original session fault and cannot save/hash a healthy state. Another company's callback is not invoked.

## 6. Migration

No legacy wrapper or compatibility shim. Strategies without entity references remain unchanged. Strategies with live references opt into the existing contract and retain ownership of their original state and codec.

## 7. Verification

Four native/direct-worker and minute/half-minute public scenarios: first ordinary controller command, dormant unconsumed reply, three-minute staffed upgrade, saved pending/rebound twins, unchanged cadence, private foreign-callback trap, resumed normal inspect command and intermediate physical work followed by authoritative completion history.

Refusal parity, pure-check mutation and commit exception are explicit negative branches. An affected rival-parity case covers actual physical player/rival construction and private terminal diagnostics. Only literal focused cases run.

## 8. Risks

Shared/acquired endpoints and other reference owners remain open. A controller holding another company's historical observation must not convert it into owned live intent. Arbitrary service aliases of nested controller instances are not proved by this stage.

## 9. Out of scope

Knowledge-history rewriting, strategy bonuses, money/business rules, Oil, hosts, rendering/platform adapters, full regression and Git publication.

## 10. Execution

Implemented and scoped verified: new controller-reference proof206/0 and affected rival parity2503/0;2709/0 across two literal bounded cases. Final solution12.12s clean. Original221 requirements/194 recorded checked/27 open remain; additional323. This stage does not close broad acceptance gates. [Evidence](../../docs/migration/P7_CONTROLLER_REFERENCE_OWNERS_VERIFICATION.md).
