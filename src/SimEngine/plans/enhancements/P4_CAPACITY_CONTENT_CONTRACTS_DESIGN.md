# P4-G17 — Open authored capacity policy contracts

Status: scoped verified, 2026-10-09;85 checks/zero failed/zero aborted. Full P1–P12 remains open.

FINAL ONE NEW exact bounded case85/0/0 in1216ms; PID12360 exit0/observed1362ms.
One public-reader build correction9.72s, four clean builds9.41s/6.36s/4.70s/4.57s.
Only the same new case repaired/rerun; earlier2/1,35/4,32/1 driver/fixture attempts recorded.
Core2.349.0/save154/replay1; state/view schemas unchanged;18-input freeze06:04:39UTC.
[Evidence](../../docs/migration/P4_CAPACITY_CONTENT_CONTRACTS_VERIFICATION.md).

## 0. Problem

ICapacityPolicy already owns stateless runtime admission, but inventory, transformation WIP,
networks, spatial resources, public extension ports and content views resolve CapacityPolicyDef.
Independent JSON definitions cannot participate without inheriting that concrete authoring base.

## 1. Contract and authority

ICapacityPolicyDefinition combines IContentDefinition with ICapacityPolicy for actual authored
identity. It does not replace runtime ICapacityPolicy: network component-scaled policies are
computed admission wrappers, not content records.

CapacityPolicyDeclaration is the single original explicit limits reader, resolver, exact measurement,
quantum and bounded-offer algorithm. Native and independent definitions delegate it. Compatibility
callbacks are explicit; offer/quantum still consult the actual policy's Check/Quantum overrides.
No second balance, stock, promise, resource catalogue, registry, scheduler or clock is introduced.

## 2. Resolution and views

Resolve every authored capacity contract after units/resources and before consumers, exactly once.
All original consumers bind through ICapacityPolicyDefinition. WIP identity checks use the actual
owned contract alias. Private content visibility remains authoritative. CopyCapacityPolicies copies
detached metadata under the existing provider observation guard, never exposing executable policies.

## 3. JSON and extension proof

Keep the original limits object and explicit unit identities; no alternate names, legacy readers
or compatibility paths. A master-listed proof file authors direct policies, measured resources,
native comparison policies and an ordinary manufacturing facility.
The public proof compiles without internals and records detached composition/measurement facts.
The thin player case submits normal hauling/manufacturing/policy commands and reads public views.

## 4. Verification

Build only as needed. Run ONE NEW exact bounded case:
Capacity content contracts: independent JSON admission, physical hauling and manufacturing.
Prove actual alias identity, combined measures, specialized rejection, exact discrete quantum,
bounded physical deliveries, original WIP manufacturing, saved phases, sole-clock pause, privacy,
immutable views, replay, session isolation and early malformed/missing-reference refusals.
Only repair/rerun this same new case; no historical, family, full, Oil, Tycoon or platform runtime.

## 5. Remaining gates

This is not a general adversarial-capacity-provider sandbox or full capacity migration acceptance.
Network/spatial runtime, arbitrary callback mutation/faults, concurrent reservations, cancellation,
endpoint loss, congestion, lockstep, fuzz/soak/performance and full P1–P12 remain open.
No Draw, adapter, domain/business changes or mandatory money/budget.
No staging, commit or push until the full goal AND plan genuinely finish and are verified.

[Parent](P4_REUSABLE_MODULES_DESIGN.md), [tracker](MASTER_TRACKER.md).
