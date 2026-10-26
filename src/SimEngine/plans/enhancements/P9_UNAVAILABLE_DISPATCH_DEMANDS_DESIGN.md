# P9-DQ1: visible blocked demand with unknown physical gap — Design Document

## 0. Problem Statement

Standard/LogisticsScheduling.cs BuildQueue discards a demand when PhysicalPort(...).Store is null. It also suppresses a previously quiet stock band before checking whether its original store is available. An enabled original intent disappears from the public dispatch view on disconnection; hosts cannot distinguish unknown stock from no work. LogisticsDispatchOffer.Requested is currently nonnullable, so reporting zero would fabricate a computed gap.

## 1. Goals

1. Enabled positive active demands with unavailable original physical ports remain visible as blocked offers with the exact typed blocker and original stable endpoint/source/age/priority.
2. Requested becomes nullable: null explicitly means unknown delivery gap; ready offers must always contain a positive known quantity.
3. A previously quiet enabled stock band is still visible when unavailable, without changing its original Replenishing state or pretending shortage.
4. Ready quiet/fulfilled bands and zero/disabled/expired demands keep existing suppression semantics.
5. Blocked offers never acquire a worker, stock or receipt; restoration recalculates actual gap through the existing demand/stock/dispatch owners.

## 2. Design

Generalize existing LogisticsDispatchOffer.Requested to Fix? and its constructor argument likewise; validate unknown quantities are only unready typed blocked demand observations. No alternate demand model, queue, stock or saved unknown flag.

BuildQueue checks physical availability while grouping active positive private demands. Only a ready binding may suppress a quiet stock band or satisfied emergency gap. The original deterministic group/tie ordering remains. An unavailable selected demand yields one PolicyBlocked HaulPreview with actual binding.Blocker, Requested:null, unchanged source/endpoint/age/priority/emergency facts. No capacity/stock read or job allocation is attempted.

Known ready bindings retain original DeliveryGap/PreviewSource calculations. TryAcquire only consumes Requested.Value after normal fresh readiness and explicit known-quantity validation; no GetValueOrDefault/zero fallback. Route batch and recovery quantity remain known, unaffected.

Logistics view feature schema advances41 to42 and behavior identity2.479.0/save154/replay1; no simulation/save payload change. Existing immutable CopyDemands still carries policy Desired/Replenishing separately from an unknown computed dispatch gap.

## 3. API surface

LogisticsDispatchOffer.Requested: Fix?; null is unknown, never an authorization to transfer. Existing commands/contracts/selectors remain. No legacy property or nullable compatibility projection.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| Modify | Standard/LogisticsScheduling.cs | 35 | high |
| Modify | Standard/LogisticsModule.cs, Kernel/SaveGame.cs, protocol identity | 3 | low |
| New | public-player unavailable-demand proof | scoped | medium |
| Modify | test registration/stage/evidence/master | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Active positive original port unavailable | Typed blocked offer, Requested:null, no acquisition |
| Quiet enabled stock band becomes unavailable | Same original intent visible, no fabricated shortage/replenishment mutation |
| Ready positive gap | Existing exact known quantity and actual physical selection |
| Ready quiet/fulfilled band or zero/disabled/expired demand | Existing no-dispatch semantics |
| Ready offer lacks known positive quantity | Engine invariant fault; never zero-default transfer |
| Foreign company query | No private demand/endpoint/blocker leakage |

## 6. Backward compatibility

Development only; migrate quantity contract directly, no legacy facade.

## 7. Verification

One solution compile and necessary focused repairs only. ONE NEW bounded public-player case using actual native/point master files, ordinary held/stock/route-selection/haul commands, immutable queues/demands, Game.Update/saves/replay and sole clock. No old/family/full runtime.

## 8. Risks

Unknown gaps are not stock values or delivery predictions. Aggregation remains original per-endpoint/resource group policy, not a new alias selector. Larger emergency/provider/demand-class/route/recovery/congestion/stress/full matrices remain open.

## 9. Out of scope

Oil/platform/Draw/Tycoon/protected G19/sole-clock source changes, full P1-P12 closure and publication.

## 10. Sub-task execution order

1. Make the existing derived offer quantity explicitly nullable with checked ready acquisition.
2. Retain unavailable positive active demand observations and defer quiet/emergency suppression until actual binding is ready.
3. Prove active/quiet loss/restoration, physical delivery/private/saved/replay behavior; record evidence without shrinking original gates.

## 11. Scoped result

Core2.479.0/save154/replay1/logistics-view42; original offer quantity explicitly unknown when original port unavailable, blocked active/quiet intents retained by same scheduler/demand/stock owners. No acquisition or saved unknown state; restoration recomputes actual gap. ONE NEW FIRST/FINAL377/0/0 in7377ms, four native/point minute/half actual policies/selection/physical hauling/private/saved/pause/replay/isolation workflows. One clean solution20.72s, no repairs/older/full runtime or executable edits after FINAL. Original221/186checked/35open unchanged; additional206. Broad/emergency/provider/demand-class/alias/route/recovery/stress/full P1-P12/publication OPEN. [Evidence](../../docs/migration/P9_UNAVAILABLE_DISPATCH_DEMANDS_VERIFICATION.md).

Fresh read-only metadata closure:1699 source anchors,943 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2887 paths/1020 Markdown files/8657 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2875/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional206. Five existing selected executable inputs changed, one new executable input/three new source files, three existing production sources changed/zero new. Logistics view42 records changed nullable quantity semantics. No runtime rerun for documentation checks.

