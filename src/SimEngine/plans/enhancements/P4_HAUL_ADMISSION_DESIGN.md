# P4-HA1 — Manual hauling player authority

## 0. Problem
Manual HaulResourcesHandler and LogisticsView currently consume physical IHaulExecution selection without complete private participant admission or worker/priority query parity. The engine-side physical capability is legitimately used by DispatchSourceProof and must stay sight-independent for autonomous dispatch and acquired shipments; manual players need a segregated capability on that same owner.

## 1. Goals
1. Original public haul owner checks actual seated player, current owned destination/source and any explicit worker before native request/provider access.
2. Purchase observation retains existing supplier ownership rules but requires a visible actual supplier; business/settlement rules remain unchanged.
3. A private original winning carrier yields whole NotFound, not selection of another visible carrier.
4. Command, typed view and fresh public acquisition use the same manual authority and priority.
5. Acquired haul jobs/claims/cargo continue under sight masks using original physical rules.
6. Prove minute/half source/destination/carrier masks, visible alternative versus denied implicit winner, saved refused inbox/no jobs/stock duplication, ordinary timed delivery/twins/replay/isolation.

## 2. Design
Engine-side DispatchSourceProof already consumes IHaulExecution for autonomous dispatch, so that physical contract remains unchanged. Add small IHaulPlayerControls in existing WorkContracts, bound to the same LogisticsService instance. PreviewPlayerHaul performs authority then calls original private Preview, withholding a private selected carrier. AcquirePlayerHaul enters that same check inside existing Acquire before any job/receipt mutation; automatic/extension AcquireHaul retains physical Preview. HaulResourcesHandler and LogisticsView forward player authority, explicit worker and priority through the same control contract. This is interface segregation over one owner, not another selection/state/ledger/scheduler or compatibility path.

## 3. API
IHaulPlayerControls exposes PreviewPlayerHaul and AcquirePlayerHaul with explicit player authority and ordinary priority, implemented by the original owner; IHaulExecution remains its independent physical capability. Worker/priority appended to existing typed view. Core2.517-to2.518/logistics46-to47, save154/replay1 retained. No compatibility overload.

## 4. Files
Original WorkContracts, Logistics, HaulResourcesCommand, LogisticsView, LogisticsModule, SaveGame/protocol; mechanical affected old private assertions, thin exact new proof and profile entry.

## 5. Errors
Private explicit request participants return NotFound before malformed resource/ceiling/priority/ports; visible malformed requests retain original InvalidRequest. A private original winning carrier after a valid selection is wholly unavailable; no candidate/source/resource/travel/quantity facts are published. Normal handler uses original status/blocker mapping. Fresh manual acquisition rechecks before any claims/jobs.

## 6. Compatibility
Development-only direct revision; no old signature overload or compatibility mapper.

## 7. Verification
One completed-slice Release solution and new bounded exact case; same-case repair only. Driver only authors JSON and sends normal players/Update/save/replay/immutable observations. No raw world/stock/progress writers.

## 8. Risks
Automatic arbitration, recovery, continuing reservations and physical toils cannot acquire a player visibility prerequisite. A visible alternative must not mask a private original winner. Optional purchase settlement behavior cannot change.

## 9. Non-goals
Full purchase runtime, indirect hidden unselected candidate/blocker information, content/demand-source closure, arbitrary port/provider/fault/fuel/recovery/disposal/lockstep/performance/global acceptance, Tycoon/Oil/platform/Draw. Original broad gates stay open.

## 10. Order
Revise existing public/physical boundary and forward typed queries; migrate private assertions; author native physical proof; build/exact case; update evidence/master without broad closure.

## 11. Scoped result

FIRST/FINAL Release794/0/0 in2928ms, PID41452/Exited0; one solution12.44s clean. Six minute/half source/destination/carrier masks prove manual typed/command private parity, real visible alternative but no replacement for a private original winner, saved refused inputs/no job/stock charge, ordinary acquired cargo/hidden physical delivery/conservation/twins/replay/isolation. Core2.518.0/save154/logistics47; original221/193checked/28open unchanged/additional250. Profile-info718included/0runtime; no executable edits after FINAL. [Evidence](../../docs/migration/P4_HAUL_ADMISSION_VERIFICATION.md). Indirect private candidate/content/source and purchase/external masked automatic/provider/fault/performance/global/full goal/final regression stay open.

