# P9-C9 — Public shared-haul admission verification

Date2026-10-10; canonical Beep.SimEngine only. Core2.407.0/save154/replay1/work-view97 unchanged. [Design](../../plans/enhancements/P9_PUBLIC_SHARED_HAUL_DESIGN.md).

## Outcome

Original P9.acceptance.048 verified through existing generic admission/claim/dock/handling owners plus one new public-player case. Native HaulResourcesCommand always re-evaluates actual selected source/cargo/destination, then LogisticsService.Acquire uses one IResourceTransaction.AcquireClaims vector before binding one original WorkBoard job/carrier. Original ResourceInventoryState checks the actual available stock/capacity before reservation, and ResourceReservationBook rejects duplicate owned records; WorkBoard compares actual authored dock claims and geometry; original ResourceHandlingDeclaration alone converts actual amount and rational duration to game-time steps. No new stock balance, queue, scheduling formula, timer or compatibility path was introduced.

shared-haul-master.json explicitly references the existing process-only world/work/recipes, access/retry and the new shared-haul.json. The selected queue scenario uses engine TileSpace/TileNavigationModule/Mover/TileWorkDock/Storage/Worker/Transporter, with six actors initialized by the exact WorldSetup profile. Authored48x32 geometry and24-unit receive capacity are scenario choices, not framework size limits. PointModule loads its referenced definitions but supplies no selected geometry. No economy/progression/controller/knowledge required, no business defaults invented.

## Current physical evidence

Three normal commands in one saved player inbox acquire exact distinct carriers, one service/two waiting source claims and20 destination units. A fourth compatible idle machine refuses work.dock-full while original dock places are occupied; preview/command agree with no successful reservation/job prefix. A second shipment on an already assigned carrier also refuses without a prefix. Pending command input is hashed, but before Update no stock/job/reservation is changed.

The4-unit load requires4 loading and8 unloading steps;8-unit loads require8 and16, as read from actual native job facts. Queued machines earn no loading while the service bay is retained. Disabling the active worker preserves partial cargo, progress and bay; sole-clock pause stops every process. After physical source/destination place release the fourth normal4-unit shipment becomes admissible. Claims plus delivered stock total24, never more than the actual receive capacity. Every observed advanced step checks all source/cargo/destination reservation amounts/token uniqueness, one live job per carrier, dock claim uniqueness, mobile tile occupancy and total32 physical material.

Saved twins cover the pending inbox, queueing, partial pickup, disabled service, physical release, partial delivery and terminal departure; every continued step hashes equally. Final source8/destination24/all cargo0 and zero reservations prove four shipments deliver24 exactly once. Retained jobs/receipts stay detached; another company cannot read private job/reservation rows. Command-stream replay and interleaved new sessions succeed.

A second explicitly authored scarce WorldSetup profile starts with source12 and enough destination space. Three simultaneous8-unit intents preview against still-uncommitted state. The authoritative saved inbox grants8, then remaining4, and refuses the third: only two jobs and12 destination promises, no third carrier/cargo claims. Physical pickup/departure delivers12 and depletes source0 without fabrication. Saved twin continuation and replay agree. The driver only issues commands, observes immutable facts and advances bounded engine Update.

## Actual gates and repair evidence

Build command: dotnet build Beep.SimEngine.slnx --no-restore.

| Build | Exit | Seconds | Warnings/errors |
|---|---|---|---|
| 1 | 1 | 4.63 | 0/2; test used nonexistent ResourceId/Code names instead of TryFindResource/Refusal. |
| 2 | 0 | 4.85 | 0/0 |
| 3 | 0 | 5.74 | 0/0 |
| FINAL4 | 0 | 5.19 | 0/0 |

Only ONE NEW exact bounded case; same-case repairs/extension only:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Shared haul admission: public stock carrier dock claims and amount-scaled saved handling' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

| Attempt | Passed/failed/aborted | Suite ms | PID | Exit | Observed ms |
|---|---|---|---|---|---|
| 1 | 23/5/1 | 1398 | 33236 | 1 | 1542 |
| 2 | 36/0/0 | 3495 | 20336 | 0 | 3675 |
| FINAL3 | 46/0/0 | 5587 | 19380 | 0 | 5836 |

All children Exited, stdout/stderr untruncated, observer errors empty. Outer30000ms, owned termination5000ms, cap30000chars, each stage256Updates. Attempt1 also incorrectly expected pending input not to hash and admitted a fourth truck after source-only release while destination places were still held. The 1D provider's real segment occupancy caused opposing trucks to block on a single lane. No engine teleport/collision bypass was added; existing 2D engine navigation with explicit circulation geometry is the selected proof. The 1D topology limitation is not a universal no-deadlock claim. Attempt2 proved the physical workflow; final extends the same case with explicit carrier and scarce-stock contention.

No production engine/protocol edits. No prior/family/full/Oil/Tycoon/platform runtime executed. Historical runtime evidence remains historical, compile only.

## Inventory and remaining scope

Post-FINAL executable freeze 2026-10-09 23:17:31 UTC:483 selected inputs, five changed/new paths versus previous480 (Program, ExtensionProof project, SharedHaulAdmissionTests, shared-haul master/content). Three new paths join inventory; no executable edits afterward. Sole clock and protected G19 Tycoon files unchanged. Read-only source hashes, documentation links, tracker shape, dirty/staged status and final drift are recorded in the [completion audit](../../plans/enhancements/COMPLETION_AUDIT.json).

Only original P9.acceptance.048 changes to checked, original text unchanged. Original221/153checked/68open,110 additional scoped deliverables. This verifies shared native stock/carrier/dock admission and amount-scaled physical handling, not arbitrary-provider rollback/fault/geometry matrices, stress/performance, topology/fuel/breakdown recovery, starvation/traffic deadlock freedom or all P9/full P1-P12. Other compound gates remain open. Lifecycle skill guided the stage/evidence/SOLID review: original owners and contract boundary retained. Protected G19 authority remains pending. No staging, commit or push until the entire goal and plan are complete.
