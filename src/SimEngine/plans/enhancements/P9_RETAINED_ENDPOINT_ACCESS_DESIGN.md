# P9-R2: Recovery through alternate access to retained endpoints

## 0. Problem

`LogisticsService.PreviewDestination` excludes every candidate backed by the current destination endpoint. `TryReplanDestination` also rejects a retained receipt if its physical site/port changes. A surviving store can therefore keep real cargo capacity promised while its carrier cannot reach it through another ordinary authored port after physical site loss. These are distinct physical bindings, not distinct stores.

## 1. Goals

1. Admit a compatible alternate physical access point to the same actual store through the existing recovery policy.
2. Preserve the acquired job, carrier, exact remaining cargo and destination capacity receipt; acquire/release no additional promise when endpoint identity is retained.
3. Treat acquired commitments as commitments, not fabricated fresh demand.
4. Reset unfinished service credit when changing physical access, retain any physically delivered prefix and require normal travel/unloading.
5. Prove normal timed demolition, retained-cargo waiting, alias selection, saved continuation, privacy and command replay for full and partially delivered cargo.

## 2. Design

Extend detached `RecoveryDestinationFacts` with mandatory `CommittedAmount`: zero for new deliveries, validated native amount for an actual continuing receipt. The existing preview validates that exact receipt before emitting these facts. `RecoveryDestinationDeclaration` honors acquired committed quantity independently of new demand gaps; custom policies receive the same typed distinction. No second demand/reservation ledger.

Allow original endpoint candidates in existing destination selection; source-self and evacuation exclusions remain. On successful same-endpoint rebind, validate the existing original receipt and use it unchanged. On a different endpoint, retain existing whole-cargo acquire-before-release behavior. Changing site or port resets only unfinished handling progress and rebases the current leg to the already-delivered amount. Same-site restoration keeps its earned progress.

## 3. API/state

Existing contracts only: required `Fix committedAmount` in the detached facts constructor. No compatibility overload/default. Update all current callers. Engine2.591.0/save157/replay1; existing persistent Job and endpoint codecs already carry all required state.

## 4. Files

`Standard/RecoveryDestinationPolicies.cs`, `Logistics.cs`, `LogisticsRecovery.cs`; protocol identity/expectation; mechanical quantity-fact caller; existing `ScopedRecoveryLifecycleTests` adds a separate exact alias case using its existing command/view helpers.

## 5. Errors

Mismatched surviving receipt remains an engine invariant failure, never substituted stock. Unreachable/unauthorized/insufficient alternate access remains a normal blocked recovery. Fixed manual hauls still do not automatically retarget. No teleport or completion credit transfer to another physical site.

## 6. Compatibility

Current development contract changed directly; no legacy alias, fallback or duplicate recovery scheduler.

## 7. Verification

Final exact new retained-endpoint case143/0 and existing scoped lifecycle167/0:310 distinct successful focused checks. Four clean targeted compiles and all initial/repair/final attempts are recorded in [verification](../../docs/migration/P9_RETAINED_ENDPOINT_ACCESS_VERIFICATION.md). No full/profile/family gate.

## 8. Risks

Docked same-store aliases, simultaneous competing changes, hostile provider callbacks, mixed resources, all route geometries and global lockstep/soak/performance remain broader gates.

## 9. Exclusions

Oil/Tycoon/Godot/Unity/Draw/host gameplay, destroying protected stock, instant site loss, additional claim/job/clock state and Git publication.

## 10. Status

Implemented and scoped verified for ordinary non-docked tile-site demolition and alternate access to surviving independent stores, with full and partial delivery. No new stock/demand/claim/job owner. Broader dock/provider/global requirements remain open; whole goal active.
