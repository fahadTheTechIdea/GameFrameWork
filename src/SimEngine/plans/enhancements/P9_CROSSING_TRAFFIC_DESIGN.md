# P9-CT1 — Retain physical right of way through transient crossing traffic

## Problem and acceptance

Actual2.671 native fleet at11007 and12447 retains the exact same jobs39/40,
cargo40/0 and positions in parallel rows. A48-update clone trace proves a repeating
18-update cycle at12447/12465/12483. Manufacturing advances2708->4148/6000 from
already supplied inputs, so a healthy time/hash or one passed day is NOT logistics
liveness. Preserve native610415a02f0b4af9a9154cebef8f9e64; no blind days, free
materials, manual vehicle steering, raised limits or rewritten headers.

Reused NativeTrafficTests factory/master-listed native-movement-traffic.json and
Journey assertions author two ordinary movers, two parked obstacles and crossing
destinations. Baseline exact case945/5: normal quantum/reverse-order controls
arrive, but half-minute/default order cycles with unchanged actual goals. No test
body weakening, instant placement during movement or artificial cycle detection.

## Sole-owner design

Mover.Advance already owns the route, segment and physical position. When its
next actual segment is blocked, retain that same route and wait if ALL blockers
are lower-priority moving traffic. Stable entity identity is the total right-of-way
tie-breaker. Other actors continue existing dynamic replanning and can clear the
crossing. No traffic ledger, waypoint/goal swap, scheduler or second route owner.

NavGrid owns the pure classification through its existing guarded entry pipeline:
one internal probe may disregard ONLY moving higher-ID occupants/clearances to
decide whether waiting is justified. It MUST NOT authorize a physical step.
Actual FindPath, CostFor, TrySegment and movement still use every original live
occupant/sweep/corner, terrain, operating-area and provider check. The actual
segment must become physically clear before any movement. Idle/mobile and static
blockers do not grant waiting priority; neither does a moving-intent peer whose
actual original MotionExecution readiness (rates, controls, area, fuel/trip and
worker/endpoint gates) blocks movement. Status alone is insufficient: first
candidate fixed crossing630/0 but expanded disabled-peer proof exposed1354/2.
The same guarded execution owner now qualifies whether a peer can yield;
no copied readiness model or silent status substitution. Genuine topology changes still replan or
become unreachable. All observed provider callbacks remain guarded and validated.

Existing route state/codec alone persists waiting; age derives from the one clock.
No new saved flag, reservation, cached world hash, duplicate pathfinder, compatibility
shim or platform logic. Production correction advances strict build identity;
old native images remain historical without header rewriting.

## Stages

1. [x] Continue actual native workload and prove the18-update cargo cycle.
2. [x] Reproduce with ordinary generic traffic commands and unchanged physical assertions (945/5 baseline).
3. [x] Implement in existing NavGrid/Mover owners without weaker physical permissions; qualify actual peer readiness, not only Moving intent.
4. [x] Verify exact crossing, opposing traffic, clearance/stop/provider faults, choke/topology and home intent scopes; record every attempt (six final scopes1634/0,945/5 and1354/2 failures preserved).
5. [x] Verify actual new-build native liveness/physical delivery: eleven bounded stages1075/0 earn all harbour structures8111, once-only fleet admission8127, jobs45/46 at11007 beyond old39/40 cycle, and48 actual updates of unloading/next source loading. This closes only the selected defect, not full fleet/performance/P9.051.

Only literal bounded cases, no broad family/profile/full suite or automatically
repeated passes. Framework maintainer implements/self-reviews; project owner
remains accountable for product/Git policy. Reuse World.Fault, original codecs/
views/replay and existing operational/assurance matrices. No new I/O, channel,
UI, dependency, observer gameplay, business, renderer, Draw or extra clock.
Full fairness under arbitrary traffic, starvation/soak, arbitrary provider and
original P9.051/whole-plan gates remain open. Original221/209 recorded checked/
12 open/full goal ACTIVE, no staging/commit/push.

[Every attempt, current source/consumer identity and native gap](../../docs/migration/P9_CROSSING_TRAFFIC_VERIFICATION.md).
Framework and rebuilt consumer2.672/save160/replay1, no new state or Oil/gameplay/
content/observer edits. The original2.67112447 cycle is retained historically,
incompatible with current binaries; never rewrite its header. Stage5's selected
native liveness is now verified, but CPU cost remains near the original bound
(second fleet day115463ms/120000ms), not a performance improvement claim.
At the end of this selected CT1 gate, compatible consumer8576996fec014496aab0303a859df952/checkpoint.json,
11007/hashA83252930225CD43,fleet_progress/next fleet_execution,head2708/6000;
no manufactured vessel. Disposable trace reaches11055 but publishes no new save.
That next command has since run. [P5-MF1 is the single current native handoff](../../docs/migration/P5_MARINE_FLEET_CONTINUATION_VERIFICATION.md#current2672-continuation--2026-10-19);
do not resume this older pointer, rebuild/reset/readmit or infer full acceptance
from the selected crossing fix.
