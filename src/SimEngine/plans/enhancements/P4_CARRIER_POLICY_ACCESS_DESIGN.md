# P4-HP1 — Carrier load/trip authority and cargo disclosure

## 0. Problem
Load/trip control and group admission omit current sight; private carrier cargo/return/disposition and settings views also disclose unseen state. Physical load/trip checks must remain independent of player visibility.

## 1. Goals
1. Original load/trip contract owners gate healthy/live/actual/owned/current-sight player checks before settings/provider reads.
2. Single/group typed previews and normal commands agree on private refusals and expose no hidden group settings.
3. Current cargo/return/disposition/load/trip/service facts share actual carrier sight.
4. Existing physical load/trip/stock/receipt/job/time owners remain unchanged.
5. Prove acquired cargo/assignment/settings survive refused hidden inputs, saved inbox/twins, delivery/replay/isolation.

## 2. Design
Both CheckConfigure methods gain player authority, while their original provider algorithms stay private for physical Configure. Existing group selection delegates the same checks. Add public group commands in each existing command file and typed group forwarding in LogisticsView; register in the original logistics module. Current carrier views use CanObserveOwned before provider reads. No new carrier, policy, route or reservation model.

## 3. API
Breaking required player argument; new group command/view methods. Logistics view44-to45, core2.515-to2.516; save154/physical codecs unchanged. No compatibility overload.

## 4. Files
Original load/trip contracts/selection/commands/view/module/identity/protocol; new thin exact proof and tracker; mechanical older private assertions only.

## 5. Errors
Private targets refuse NotFound before invalid/null settings or unsupported capabilities. Group shape errors stay typed; private Settings null. Original exact commit/fail-stop remains; no rollback, teleport or stock injection.

## 6. Compatibility
Development-only direct revision.

## 7. Verification
One Release build/new bounded case; same-case repairs only as needed. File-authored native point load/trip profiles and sight module drive real acquired hauling, hidden settings/cargo/diagnostic withholding, actual saved refused single/group inputs/no prefix, revealed group control, delivery/twins/replay/isolation.

## 8. Risks
New settings must not drop acquired cargo/home/job. Manual policy toggles are player intent, not an alternative physical progression system. Actual absence is not fabricated empty state.

## 9. Non-goals
All fuel/service/provider/fault/performance/global matrices, route/demand aggregates, Tycoon/Oil/platform/Draw and final full regression.

## 10. Order
Revise original admission/provider split; add group forwarding and private view gates; author bounded physical proof; build/run; record current evidence/master without closing broad gates.

## 11. Scoped result

NEW FINAL Release332/0/0 in1550ms, PID45932/Exited0; FIRST332/0/0 in1623ms. One clean solution10.02s; profile-info repaired for the new exact case716included/0runtime; no executable edits after FINAL. Authored minute/half carrier load/trip policies, actual acquired cargo/budget/job, private saved single/group refusals/no prefix, hidden physical delivery/saved twins/replay/isolation proven. Core2.516.0/save154/logistics45; original221/193checked/28open unchanged/additional248. [Evidence](../../docs/migration/P4_CARRIER_POLICY_ACCESS_VERIFICATION.md). Broader fueled/reserve/service/provider/disposal/recovery/aggregate/fault/performance/full goal and final full regression remain open.

