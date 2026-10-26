# P9-DP1: authoritative dock provider observations — Design Document

## 0. Problem Statement

P9-DC1's first bounded attempt proves WorkBoard.Vacancy throws after a pure provider touches its entity, but World.Fault remains null and Save still succeeds. WorkDocks.cs has separate manual version guards in DockRequest/Vacancy/DockReach/CaptureDock/DockApproach and unobserved runtime slot getters. Version-only checks miss persisted participant/space changes and callback failures; an engine-owned preview cannot become a healthy save after such a violation.

## 1. Goals

1. Dock binding, slot metadata, request, vacancy, cost, geometry and reach use one existing EntityObservation boundary and preserve the original first fault.
2. Pure callbacks observe actual dock/whole entity, carrier/whole entity and selected space; malformed/null/foreign geometry and enum/cost violations fault before returned authority.
3. Approach may change original motion intent and carrier invalidation version, but may not change endpoint, space, identity, owner, lifecycle, clock or other carrier component payloads.
4. Actual master-listed JSON/public-player tests cover pure previews and live approaches, with no healthy save/hash/retry after faults and isolated sessions unaffected.
5. Healthy physical hauling remains timed, deterministic, private and durable through saved twins/replay; no replacement scheduler/stock/clock or host rules.

## 2. Design

WorkBoard retains dock binding and physical selection responsibility. ReadDock<T>(IWorkDock,Entity,Func<T>) delegates to existing EntityObservation.ReadProvider with actual bound component owner, whole endpoint/carrier codecs and selected spatial observation. DockSlots and Kind read original immutable definition aliases within that guard. All original pure callback result validation occurs inside it; delete manual Pure, endpoint-version and Tick/NextIdentity duplication.

DockApproach observes the endpoint and selected space using the same guard, captures carrier identity/lifecycle facts plus non-motion components using their actual codecs, invokes original IWorkDock.Approach, checks its enum, then verifies the original captured scopes. Only the actual IMotion component and carrier version may change; captured spatial presence must remain unchanged until subsequent game updates.

EntityObservation's existing Scope/ProviderScope gain an explicit allowEntityVersionChange option for this mutating intent boundary. It does not suppress owner/state/lifecycle/clock/identity or payload checks and remains false for pure calls. No new fault, snapshot, codec, reservation or timer owner.

Independent PointDock fault probes are authored in existing point_dock JSON, with a declared activation game-time coordinate where required. They mutate only for negative provider contract proofs, never implement gameplay in the driver.

## 3. API surface

Internal observation option only; no new player command, public dock signature, view, payload or save schema. IWorkDock documentation states pure and approach authority. Engine behavior/protocol identity advances to2.476.0/save154/replay1.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| Modify | Kernel/EntityObservation.cs | 16 | high |
| Modify | Standard/WorkDocks.cs | 90 | high |
| Modify | Standard/DockContracts.cs, Kernel/SaveGame.cs | 8 | medium |
| Modify | tests/ExtensionProof/PointDockProof.cs | 70 | medium |
| New | tests/Beep.SimEngine.Tests/DockProviderObservationTests.cs | scoped | medium |
| Modify | test registration/protocol, stage/evidence/master | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Pure callback touches participant, changes codec/space/clock, throws or returns invalid facts | Original latched fault; no returned readiness/healthy checkpoint |
| Foreign slot alias/null geometry/wrong space/invalid reach or negative cost | Same guarded fault, never typed gameplay fallback |
| Legal Approach requests motion/version invalidation | Movement intent permitted; actual travel still later Update-owned |
| Approach changes endpoint/non-motion participant payload/space/identity/clock | Fault before healthy continuation |
| Expected occupied/unreachable/incompatible provider refusal | Existing typed blocker/wait, healthy deterministic state |
| Subsequent missing/private/empty query after fault | Original healthy gate rejects; retained immutable snapshots stay safe |

## 6. Backward compatibility

Development only; replace partial guards directly, no legacy wrapper or compatibility model.

## 7. Verification

One solution compile and necessary focused repairs only. One NEW exact bounded public-player case at minute/half-minute game time with healthy physical/saved/replayed flows and explicit malicious pure/live callbacks. No historical/family/full runtime, consistent with user test constraints.

## 8. Risks

Observation hashes are scoped to declared codecs and actual selected-space invalidation, not an arbitrary CLR sandbox or global rollback. A co-hosted motion capability's private fields remain its declared owner; do not claim universal isolation of arbitrary undeclared foreign state. Faults stop the session, never compensate mutated state.

## 9. Out of scope

Oil/platform/Draw/Tycoon/protected G19/sole-clock source edits, unowned callback families, complete all-provider/congestion/performance/fuzz/full P1-P12 gates and publication.

## 10. Sub-task execution order

1. Extend original observation scopes for explicitly allowed motion-version invalidation, retaining other facts.
2. Migrate all dock pure callbacks and binding/slots/overlap into the one observed owner; guard Approach's allowed mutation boundary.
3. Author actual provider fault probes and NEW public-player case; preserve all attempts, current evidence and literal original gates.

## 11. Scoped result

Core2.476.0/save154/replay1; original dock binding/pure callbacks/approach use original actual provider/participant/space observation. Version-only guards removed, legal motion intent/version changes preserved, unrelated participant/space/clock mutation faults. ONE NEW bounded FINAL725/0/0 in6588ms, three native/point timed physical saved/replayed/private/isolated flows and47 preview/live negative variants. One solution failure/two clean focused builds and both runtime attempts retained; no executable edit after FINAL or broad runtime. Original221/186checked/35open unchanged; additional203. Broad/co-hosted/global/congestion/stress/full P1-P12 and publication OPEN. [Evidence](../../docs/migration/P9_DOCK_PROVIDER_OBSERVATION_VERIFICATION.md).

Fresh read-only metadata closure:1687 source anchors,936 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2875 paths/1014 Markdown files/8648 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2863/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional203. Eight existing selected executable inputs changed, one new executable input/three new source files, four existing production sources changed/zero new. No runtime rerun for documentation checks.

