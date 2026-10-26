# P9-C3f — Actual cargo binding and guarded provider observation

Status: historical focused-verified, 2026-10-07, before C5b. Full acceptance remains open.
Current source/evidence: [C5b loaded recovery replanning](P9_LOADED_RECOVERY_REPLANNING_DESIGN.md).
[Evidence](../../docs/migration/P9_CARGO_BINDING_OBSERVATION_VERIFICATION.md) /
[source manifest](../../docs/migration/P9_CARGO_BINDING_OBSERVATION_SOURCE_MANIFEST.json).

## 0. Problem statement

C3e shared recovery used ReadCargoDisposition for quantities, but RecoveryStock and return preview
read ITransporter.Cargo before reaching its validation guard. A provider could mutate and return
null, or mutate its own persisted fields without Touch, on that initial read. Several shared
preview/acquisition paths also repeated unguarded cargo binding reads. Contract-based transport
must not presume cargo inventory is on the worker entity.

## 1. Goals

1. One LogisticsService helper observes and validates actual cargo bindings before use.
2. Initial getters, including null-returning or persisted-state mutation, fail explicitly.
3. Separate owned cargo inventories use their actual stock policies and reservation endpoints.
4. Existing shared scheduling, physical handling, saves and the single clock remain authoritative.
5. Independent public-only authored provider proves physical delivery and session-local load binding.

## 2. Design

Standard/LogisticsCargo.cs owns private ReadCargoInventory(Entity worker). It captures the actual
provider through existing EntityObservation.CaptureProvider, verifies capability-worker identity,
reads Cargo, then verifies the provider's existing Sync and scoped facts. Returned inventory must
be live, owned, registered in this session and the exact IInventory capability of its actual entity.
ReadCargoDisposition retains actual stock classification and uses that helper, not another reader.

Standard/LogisticsRecovery.cs uses this helper for RecoveryStock and initial PreviewReturnCargo.
Logistics.cs uses it for return sources, candidate cargo, fresh acquisition, destination preview
and actual pickup cargo. Matching, receipts, jobs, quantities and progress remain existing owners.
No new stored binding or inventory is introduced in the runtime framework.

## 3. Public contract proof

tests/ExtensionProof/BoundCargoProof.cs is an engine-side ITransporter content provider, not host
gameplay. BoundCargoDef declares cargo prototype, handling profile and explicit fault mode through
strict JSON. Actual binding records one existing owned entity ID; component Sync owns that identity.
Actual inventories own stock and receipts. No quantity or resource copy is stored by this provider.

The existing proof module registers proof_bound_cargo schema1 and a stateless session-context
system. OnNewGame uses World.Settle before binding unique authored actual owners; missing or
ambiguous owners fail. OnLoaded rebinds transient World context without reselecting saved identity.
Update is empty: no assignment, movement, timing, transfer or independent update loop.
The module/content document are explicitly master-listed by the existing recovery fixture.

## 4. Files and ownership

- Runtime: Standard/LogisticsCargo.cs, LogisticsRecovery.cs, Logistics.cs, Kernel/SaveGame.cs.
- Public provider: tests/ExtensionProof/BoundCargoProof.cs and LogisticsRecoveryPolicyProof.cs.
- Focused proof: tests/Beep.SimEngine.Tests/RecoveryDispatchTests.cs, same two suite registrations.
- Mechanical current build literals:82 source/test paths, listed in the source manifest.
- Stage, parent P9, master, AGENTS/README/help and current evidence are reconciled together.

Runtime paths above are relative to src/Beep.SimEngine. No OilFieldDays, Tycoon or platform changes.

## 5. Error handling

| Condition | Result |
|-----------|--------|
| Mutating getter returning null | Scoped observation fault, before null is interpreted |
| Getter mutates persisted fields without Touch | Existing provider codec hash detects and latches fault |
| Foreign/dead/non-actual cargo or wrong worker binding | Explicit invalid provider refusal |
| Missing/ambiguous authored cargo owner | Explicit proof-provider initialization refusal |
| Valid separate inventory | Actual inventory policy/claims, worker-owned intent and movement |
| Build failure | No stale no-build behavioral gate |

## 6. Direct identity and persistence

Build2.199.0. Save106/WorkBoard36/WorkSystem36/Logistics16/maintenance2/work view69/logistics view27
are unchanged from C3e; native recurring8/public recurring5,5,6 unchanged. No runtime payload change.
Optional independent proof adds component schema1 and stateless context-system schema1; current
codec manifests describe them normally. No compatibility reader or fabricated fallback.

## 7. Actual verification

Clean solution build:0 warnings/errors. Only the same two exact suites ran, now96 admission and
106 physical assertions (202 total), zero final failures/aborts. Existing C3e180 checks remain.
New checks prove first-getter fault detection, separate inventory identity/policies/receipts,
private pure observations, session-local saved binding, loaded save continuation, timed delivery,
protected retained stock and departure with no phantom return. See evidence for exact commands.

## 8. Risks and remaining requirements

Scoped observation is not a global provider sandbox. An unrelated entity not captured by a callback
still requires wider purity analysis. Shared cargo across multiple workers, mixed resources,
endpoint-removal chains, operating inputs/trip budgets, home/service/emergency interaction,
larger fleets/congestion, replay/performance/soak and full acceptance remain open.
Other suites compile only; optional provider schema proof is not general mod acceptance.

## 9. Out of scope

New clock/scheduler/stock store, compatibility, OilFieldDays/Tycoon, hosts/demos, Draw/Unity/Godot,
platform limits, new transport media and full/family/third-suite tests.

## 10. Completed and remaining tasks

- [x] Consolidate guarded cargo identity reads under existing LogisticsService.
- [x] Reject initial null-return mutation and untouching persisted mutation.
- [x] Prove independent authored separate-inventory transport without scheduler modification.
- [x] Prove actual endpoint claims, reserves, timing, departure and saved session isolation.
- [x] Build and run only two exact affected suites;202 assertions.
- [x] Record current identity/evidence and update the canonical tracker.
- [ ] Broader fleet/shared inventory/mixed-resource/home/service/provider interactions.
- [ ] Global purity/replay/performance/full framework acceptance.

## 11. Continuation

Read the canonical master and preserve the dirty worktree. C3f's scoped binding migration is
verified; continue remaining provider/fleet requirements rather than creating another cargo
reader, reservation ledger, dispatcher, clock or compatibility layer.
