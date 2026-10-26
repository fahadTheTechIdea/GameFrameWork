# P4-LP1 — Stock and outlet policy authority

## 0. Problem
Stock/outlet policy controls and private views lack current entity sight. Grouped outlet previews can expose hidden current/next settings. Physical startup and automatic demand logic must not become dependent on player visibility.

## 1. Goals
1. Existing player admission owners gate healthy/live/owned/current sight before resource/port/settings/provider reads.
2. Typed/ordinary stock and outlet previews plus executed private refusals agree.
3. Current stock/outlet settings and group current/next entries disclose only authorized facts.
4. Original physical startup/configuration/transfer semantics remain unchanged.
5. Prove private pending policy inputs retain real cargo/stock/reservations and policies through saved physical delivery/replay/isolation.

## 2. Design
ConfigureStockPolicyHandler has one shared static admission forwarding the existing stock-service physical check; its command and view use it. LogisticsPolicyRules.CheckChange gains required player authority while its original provider algorithm remains private for physical Configure. Grouped outlet selection uses the same authority. LogisticsView current policy reads share existing CanObserveOwned; typed previews delegate. No new policy ledger, reservation store, demand coordinator or artificial state.

## 3. API
Breaking outlet CheckChange(World,PlayerId,Entity,...); no compatibility overload. New public ConfigureOutletPolicyGroupCommand and PreviewOutletPolicyGroup forward original group rules, including the selected named or unscoped port. Logistics view43-to44 and core2.514-to2.515; save154/physical codecs unchanged.

## 4. Files
Original command/policy/group/view/module/identity/protocol; new thin exact driver and tracker; mechanical older private assertions only.

## 5. Errors
NotFound precedes private resource/port/settings details. Visible malformed/native-band errors retain existing codes. Hidden group members publish neither Current nor Next; complete-set original commit rules unchanged.

## 6. Compatibility
Development-only direct revision; no shim.

## 7. Verification
One Release build/new bounded exact case; same-case repairs only as needed. Existing authored point resource/policy/sight JSON drives physical demand/delivery, private reads/controls, saved inbox, reserve and stock preservation, native progress/twins/replay/isolation. No host gameplay, stock injection or instant transfer.

## 8. Risks
Startup config and physical automatic matching must continue to use existing physical checks, not player visibility. Private views cannot substitute empty policy facts.

## 9. Non-goals
Other carrier/load/trip/route/aggregate policies, all provider/fault/performance/global/full acceptance, Tycoon/Oil/platform/Draw and final full regression.

## 10. Order
Revise existing player admission; preserve physical checks; update private view/group projections; author proof; build/run; refresh scoped evidence/master and leave compound gates open.

## 11. Scoped result

FIRST/FINAL Release496/0/0 in1905ms, PID32684/Exited0; four minute/half source/destination masks prove actual native startup/stock demand/automatic pickup/delivery, private stock/export/group reads/inputs/no visible prefix, saved inbox/twins/reserves/finite stock/replay/isolation. One clean solution7.35s; no executable edits after FINAL. [Evidence](../../docs/migration/P4_LOGISTICS_POLICY_ACCESS_VERIFICATION.md). Core2.515.0/save154/logistics44, original221/193checked/28open unchanged/additional247. Named group ports are source/compile forwarding only; broader carrier/policy/provider/fault/performance/global/full goal and final full regression stay open.

