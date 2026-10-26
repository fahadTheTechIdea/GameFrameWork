# P2-F4 — Core view observation boundary

Implemented and focused-verified on2.288.0/save141/replay1. FIRST one new exact
case179 passed0 failed/aborted3525ms. Build1 had two NEW test IGameClock.Pause
compiler errors; repaired actual-clock instrumentation, Build2 clean0/0. No engine
runtime repair/rerun. Executable inputs frozen; full P2/P3/P6/P1-P12 remain open.
[Evidence](../../docs/migration/P2_CORE_VIEW_OBSERVATION_VERIFICATION.md).

## 0. Problem statement

The current SimGame.World property is already internal. PlayerView.CanSee,
CanSeeContent, Inspect and ViewOf still invoke extension callbacks without the
existing EntityObservation provider guard. ViewFeatureRegistration.Create invokes
its lazy factory unguarded and allows direct mutable engine owners as view contracts.
SimGame.PublishSnapshot commits the observer before preparing each seat; a later
projection failure can leave different viewers on different publication revisions.

This completes a real part of P2/P3/P6, not the entire plan or a provider sandbox.

## 1. Goals

1. Core observation callbacks verify their actual provider codec, target entity,
   entity identity allocator and sole clock through existing EntityObservation.
2. Known direct mutable engine-owner view aliases refuse at registration and return.
3. Core entity/catalogue/inspection copies publish no partial result on callback failure.
4. Every viewer snapshot is prepared before publication. Fault publication retains
   the last safe projections, without calling failed providers again.
5. Public commands, physical work, saved continuation and replay still use one loop.

## 2. Design

Extend EntityObservation with ReadProvider<T>(World, object, Entity, Func<T>).
It combines the existing provider codec guard and existing target/scalar scope;
exceptions latch the first observation.provider fault and never return an answer.
It is not another hash format, rollback mechanism, copied world or saved authority.

PlayerView uses that helper for selected visibility, inspector and spatial-presence
and selected spatial-space callbacks. The complete publication also verifies the
original world-scalar scope (including callbacks comparing detached spatial facts).
Inspect accumulates facets privately; core list-copy methods accumulate
private values and copy them to the cleared caller destination only after success.
ViewOf takes its actual World explicitly; all original callers migrate, with no
compatibility overload. TileMapView uses the same projection and guards the actual
tile presence footprint after checking visibility.

ViewFeatureRegistration remains the sole declaration/factory owner. It rejects
object, World, IEntity, IEntityCapability, GameSystem, IPersistentService, IGameClock,
and the core mutable entity/service/player/command/event containers as direct view
contracts or actual returned objects. Its factory runs through ReadProvider. No
new view registry, marker protocol or trust fallback is added.

SimGame prepares observer and all seat snapshots into temporary local values, then
publishes all without callbacks. PlayerSeat prepares/commits its original _scoped
snapshot through internal methods. Any capture exception latches snapshot.capture
only if there is no earlier fault, clears pacing via the original GameClock.Halt,
and publishes fault metadata with each viewer's previous immutable projection.
GameSnapshot.Retain uses that same projection, no captured live state: empty deltas,
previous revision linked normally, old facts Tick preserved and Alpha zero. Initial
failure uses an empty viewer-specific projection, never observer facts for a seat.
Update.Time is settled after publication so Updated sees final fault/pacing facts.

Known direct-owner rejection is not proof of arbitrary returned-object immutability.
Module authors remain trusted to expose read-only custom models and detached spatial
facts. Scoped codec checks do not detect arbitrary foreign-service mutation or
mutable objects excluded from the provider's codec. Those wider gates remain open.

## 3. API surface

No new public gameplay API, command, clock, rendering method or persistent codec.
Internal ViewOf now requires World. Internal seat publication is prepare/commit.
GameSnapshot.Tick identifies the retained facts epoch on a fault, not an unsafe
partially projected epoch; Game.Tick/Clock remain the actual authoritative time.
Build advances to 2.288.0; save141/replay1 and existing module schemas stay unchanged.
Earlier explicit build identities migrate compile-only; no compatibility reader.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | src/Beep.SimEngine/Kernel/EntityObservation.cs | 20 | medium |
| Modify | src/Beep.SimEngine/Kernel/ViewFeatureRegistration.cs | 30 | medium |
| Modify | src/Beep.SimEngine/Hosting/PlayerView.cs | 70 | medium |
| Modify | src/Beep.SimEngine/Hosting/GameSnapshot.cs | 20 | medium |
| Modify | src/Beep.SimEngine/Hosting/SimGame.cs | 45 | high |
| Modify | src/Beep.SimEngine/Map/TileMapView.cs | 15 | medium |
| Modify | src/Beep.SimEngine/Kernel/SaveGame.cs and earlier identity fixtures | mechanical | low |
| New | tests/ExtensionProof/ViewObservationProof.cs and explicit JSON documents | 180 | medium |
| New | tests/Beep.SimEngine.Tests/CoreViewObservationTests.cs | 220 | medium |
| Modify | existing test registration/resource project | 3 | low |
| New/Modify | stage evidence and authoritative tracker/parents/README/AGENTS | scoped | low |

## 5. Error handling matrix

| Operation | Failure | Result |
|---|---|---|
| Observation | provider mutation/exception | first terminal fault; no returned facts |
| Feature registration | known mutable contract | composition refusal before initialization |
| Feature factory | null/mutable result/mutation/exception | no cached feature; first terminal fault |
| Core list/inspect | later callback fails | cleared destination, no partial published values |
| Snapshot preparation | any viewer fails | all viewers retain their own safe facts; faulted metadata |
| Faulted Update | prior provider fault | no capture callback retry, no game time advance |

## 6. Backward compatibility

Development-only; no legacy overload, old build acceptance or alternate observer path.

## 7. Verification

ONE new exact bounded runtime case:
`Core view observations: guarded callbacks, private facts and atomic fault publication`.
Use explicit JSON/public extension contracts and existing physical player commands.
Check pure reads/private entities, persisted provider mutation without Touch, target/
clock/identity mutation, throwing callbacks, uncached failed factories, composition
refusals, fault publication across all viewers, saved twins/replay and real hauling/
construction. Each physical wait is bounded by 2000 Updates with diagnostics.

Build Beep.SimEngine.slnx once after complete edits; repair only observed build
failures. Run only this new case, rerunning only after observed same-case failure.
Record every attempt. Freeze source/proof/test/JSON after final green; docs-only
handoff checks afterwards. Older/family/full/platform/stale runtime gates are not
authorized. Full P6 and full goal completion remain open; no commit/push yet.

## 8. Risks

Scoped observation guards can detect, not roll back, a bad provider. A terminal
fault forbids saving/hashing partial mutation. Fault publication must not invoke
any provider again or leak observer projections into a seat. Additional codec work
has not been performance/soak/allocation validated. Host-supplied IList writers and
arbitrary custom view methods are outside the callback-purity guarantee.

## 9. Out of scope

Tycoon/business changes, Oil/sibling migration, hosts/demos, Draw, Unity/Godot,
whole-world purity/security, all external spatial providers, replay/lockstep stress,
full regression and final plan closure are not proven by this slice.

## 10. Sub-task execution order

1. Extend original observation/registration owners and migrate all core projection callers.
2. Replace incremental snapshot publication with original-owner prepare/commit/retain.
3. Author one external JSON-driven proof and one new bounded case; migrate build identities.
4. Build and run only that exact new case; record observed failures and scoped evidence.
5. Freeze executable inputs, update tracker/parents and inspect source/link/diff inventories.
