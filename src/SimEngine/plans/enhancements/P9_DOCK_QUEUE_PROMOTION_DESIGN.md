# P9-QP1: one authoritative dock queue promotion selector — Design Document

## 0. Problem Statement

Standard/WorkDocks.cs ApproachDock has a separate waiting-to-service candidate loop that chooses Before(kind/identity), while SelectDock now chooses kind/provider cost/stable identity. An arrived vehicle can therefore choose a farther bay during promotion despite the same engine policy pricing a nearer fresh service. Duplicate admission logic risks diverging queue/route/policy/physical gates.

## 1. Goals

1. Arrived waiting jobs promote through the existing PreviewDock/SelectDock owner, not a second candidate algorithm.
2. Only ready service slots are eligible; minimum actual current-origin cost precedes stable identity ties.
3. Actual FIFO eligibility, cross-facility physical claims/occupancy, operating areas, complete journeys and retained material/work remain unchanged.
4. Physical blocked bays are freed only by ordinary worker home movement; no test or host edits occupancy.
5. Authored point-space minute/half and shuffled-slot scenarios exercise hauling promotion, equal-cost work promotion and shared-geometry older eligible precedence, saved waiting/service continuation, pause, privacy, finite accounting and replay.

## 2. Design

Replace ApproachDock's WorkDockSlot? next/ServiceAvailable/QueueBlocker/Before loop with PreviewDock(endpoint,carrier,Request(job,source),null,...current:job,productiveOnly:true). For source handling, pass the existing RemainingDelivery continuation to retain complete receiving-route admission. PreviewDock already delegates to SelectDock, retains only permitted service commitments, compares provider cost, respects QueueBlocker, and returns readiness without mutation.

On Code.None, update only the job's original source/destination slot. On refusal, retain waiting intent and original physical wait behavior; do not allocate a slot book, reset handling/progress or move a vehicle instantly. ServiceAvailable remains the original nonrecursive competing-waiter eligibility check within QueueBlocker; it is not a second promotion selector.

## 3. API surface

No new public contract, command, policy, view, payload, clock or save schema. Core behavior identity advances to2.477.0/save154/replay1.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| Modify | Standard/WorkDocks.cs | 12 | high |
| Modify | Kernel/SaveGame.cs, current-engine-protocol.json | 2 | low |
| New | actual master-listed dock queue JSON and public-player test | scoped | medium |
| Modify | embedding/registration/stage/evidence/master | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Arrived waiting job, eligible service bays | Original selector uses actual cost then identity |
| All service bays occupied or older eligible claimant | Retain waiting claim and real occupancy |
| Invalid provider callback | Original P9-DP1 first-fault boundary, no promotion prefix |
| Equal-cost eligible bays | Stable ordinal slot identity; authored order irrelevant |
| Physical source promotion | Complete receiving continuation still preflighted |
| Service/handling already earned | Retain actual original commitment/progress; no reset |

## 6. Backward compatibility

Development only; remove duplicate promotion loop, no compatibility path.

## 7. Verification

One solution compile and necessary focused repairs only. ONE NEW bounded public-player case using actual JSON/master, normal plans/hauls/home commands, immutable facts, Game.Update, saves/replay/one clock. No historical/family/full runtime.

## 8. Risks

One-dimensional occupancy is real and must not be bypassed. Authored blockers return to noncrossing home positions; FIFO proof uses separate physical departures. Costs remain deterministic provider estimates, not guaranteed elapsed ETA. Future native/arbitrary-provider/congestion/stress matrices remain open.

## 9. Out of scope

Oil/platform/Draw/Tycoon/protected G19/sole-clock source changes, broad/full P1-P12 closure and publication.

## 10. Sub-task execution order

1. Replace duplicate waiting promotion candidate loop with the original pure selector.
2. Author actual multi-bay/idle blocker/home/shared-geometry intent in JSON and verify physical public-player cases.
3. Record all builds/attempts/current source metadata; preserve every original gate literal and broad verification qualification.

## 11. Scoped result

Core2.477.0/save154/replay1; duplicate identity-only promotion loop removed; original PreviewDock/SelectDock owns arrived waiting service cost, physical FIFO/claims/occupancy and complete route admission. ONE NEW FIRST/FINAL403/0/0 in3943ms, twelve point-space minute/half ordered/reversed timed hauling/tie/shared-geometry company flows, ordinary physical home movement, saved twins/pause/private facts/completion/replay/isolation. One solution failure/one necessary clean focused compile6.89s retained, no executable edit after FINAL or old/full runtime. Original221/186checked/35open unchanged; additional204. Broader/native/provider/congestion/stress/full P1-P12/publication OPEN. [Evidence](../../docs/migration/P9_DOCK_QUEUE_PROMOTION_VERIFICATION.md).

Fresh read-only metadata closure:1691 source anchors,938 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2879 paths/1016 Markdown files/8651 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2867/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional204. Five existing selected executable inputs changed, two new executable inputs/four new source files, two existing production sources changed/zero new. No runtime rerun for documentation checks.

