# P9-C5d — Retained recovery after destination loss

Status: historical focused-verified,2026-10-07. Build2.203.0/save109; existing schemas unchanged.
Current source checkpoint: [C3g selected haul-cost authority](P9_HAUL_CANDIDATE_COST_DESIGN.md).
Continues [C5c2](P9_DOCK_RECOVERY_EGRESS_DESIGN.md).
[Evidence](../../docs/migration/P9_DESTINATION_LOSS_RECOVERY_VERIFICATION.md) /
[source manifest](../../docs/migration/P9_DESTINATION_LOSS_RECOVERY_SOURCE_MANIFEST.json).

## 0. Problem and specification

Standard/Work.cs RunJob cancels a haul with a missing destination when the existing
LogisticsService.TryReplanDestination cannot find a complete output. Actual cargo survives,
but a later replacement job loses the original shipment identity and delivered prefix.
An already departing recovery also cannot reach geometry belonging to a removed dock.
Retain authorized loaded recovery in its existing Job until a real alternative is feasible.

## 1. Goals

- The same job, worker, original amount, creation, cargo receipt and committed prefix survive loss.
- Explicit selected JSON reroute_blockers must include core.not-found; fixed/unlisted hauls do not opt in.
- No destination with full feasible room means a typed stationary wait, not cancellation or invented delivery.
- Current selector rechecks compatibility, stock policy, worker, area, dock, route and trip gates.
- New full room is promised before old surviving room is released; then actual travel/service runs.
- A lost dock cannot require reaching phantom departure geometry; no position is changed to simulate exit.
- Existing job validation, immutable private views, save/hash and cancellation cover the wait.

## 2. Design and authoritative owners

Extend IHaulExecution.TryReplanDestination's handled result to include an authorized
missing-destination wait. LogisticsService alone checks selected recovery policy and
current outputs. Missing geometry is never substituted. Existing Job.Blocker/State own
the wait; no new cause, stock, destination, reservation or retry ledger is introduced.
WorkBoard validates that a non-egress destination wait belongs to an actual loaded,
completed-pickup recovery whose destination is missing and whose selected policy permits loss.
WorkBoard's existing ReadRecoveryStage projects WaitingForDestination from that live wait.
WorkSystem already returns when IHaulExecution handles recovery; no second dispatcher.

## 3. Contracts and API

No added service or public command. Existing IHaulExecution returns true when it begins
departure, retargets, or retains an authorized missing-destination wait. False still means
the cause/phase is not eligible or a surviving original destination cannot be replaced.
Normal World.CheckDespawn/Despawn guards still refuse removal of protected material/promises.
Endpoint-loss verification uses explicit internal participant-loss fault injection, not a
player removal command, privileged game mechanic or public destruction shortcut.

## 4. Files

Existing Standard/LogisticsRecovery.cs, Standard/Work.cs, Standard/WorkDocks.cs and
Standard/WorkContracts.cs; Kernel/SaveGame.cs build identity; existing RecoveryDispatchTests.cs.
Current owner payloads, save format and schema layouts remain unchanged. Caller build identity
metadata migrates directly; no old reader or compatibility facade.

## 5. Error handling

| Condition | Result |
|---|---|
| Fixed haul or missing cause not authored | Existing cancellation/owned cleanup; cargo survives |
| Actual loaded recovery loses output, no feasible alternative | Same job paused with DestinationUnavailable |
| Output later gains complete room | Same job rechecks, reserves and physically delivers |
| Dock disappears during departure | Actual position remains; removed geometry is not traversed |
| Worker/cargo receipt missing or corrupt | Existing explicit refusal/cleanup/protocol error |
| Cancel retained wait | Release surviving owned promises only; never delete actual cargo |

## 6. Compatibility

Development-only direct current identity; no legacy loader, alias or fallback. Build advances
for changed simulation semantics; no payload changes require a format/schema increment.

## 7. Verification

One new named scenario only at the completed slice. Clean build when compilation needs checking.
Native/public separate cargo and evacuation, lost destination before/after partial service,
lost dock during egress, capacity wait, saved twins, private immutable diagnostics, cancellation
and fixed/unlisted refusal. No previous, broad family or full test run.

## 8. Risks

Destroyed endpoint stock is not recreated or counted as delivered to a new endpoint. Removed
receipt identities may remain only in the actual retained job until replacement/cleanup; the
existing validator already permits genuinely removed participants. No output is reserved in
advance and no travel/service is earned while waiting. Surviving docks still require real egress.

## 9. Out of scope

Industrial destruction/content loss accounting, broader fleet/congestion/provider failures,
full replay/stress/acceptance, Oil/Tycoon, hosts/demo, Draw, Unity/Godot and independent clocks.

## 10. Tasks

- [x] Retained missing-output admission/wait and lost-dock selector integration.
- [x] Existing validator and immutable state projection.
- [x] Clean build;96 assertions/one new named scenario,0 failures/aborts. Older suites compile only.
- [x] Current evidence and tracker update; overall plan remains incomplete.
