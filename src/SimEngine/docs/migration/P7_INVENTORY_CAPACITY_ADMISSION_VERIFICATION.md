# P7-IC1 — Shared inventory capacity admission evidence

Status: VERIFIED-SCOPED; whole goal/plan remains open.

[Stage](../../plans/enhancements/P7_INVENTORY_CAPACITY_ADMISSION_DESIGN.md). Core beep-simengine-2.491.0/save154/replay1. Original221/187checked/34open unchanged; additional220. No original gate closes.

## Implementation and single ownership

Existing Core/InstancedCapacityPolicy.cs CapacityPolicyContracts now owns three stateless conformance gateways: Check freezes all callback stock/promised/incoming vectors and rejects undefined status; Quantum rejects undefined or internally inconsistent successful/non-successful raw steps; Offer freezes arguments and requires defined status, nonnegative bounded quantity, refusal-with-zero, exact compatible quantum and the same policy's whole projected Check to fit. Zero/refusal remains valid without fabricated stock.

All13 existing ResourceInventoryState capacity call lines use this gateway: incoming/conversion checks, Add/Offer/Remove, incoming/outgoing reservation/receipt checks, quantum/growth/consumption and load validation. Original book/stock mutations and codec remain unchanged. StorageDeclaration resource filtering, authoring/composition and native inspect also use the gateway, removing a raw Fits bypass that treated invalid status as normal incompatibility. Original network capacity observation wraps the same gateway; duplicate bounded-offer validation and unused allocation parameter were removed. No second evaluator/arithmetic implementation or stock owner.

Healthy direct JSON resource/capacity definitions use native storage on the original point presence, transporter, WIP/tools and physical work owners. Existing public resource workflow is reused with explicit factory/module input and original assertions/default registration retained; its older case was not run. Actual master-listed inventory-capacity.json declares source/destination capacities, while ordinary JSON prototype component definitions author the guarded query. Query instrumentation invokes existing endpoint contracts only. Malicious callbacks activate only within that inspection on the sole paused clock after eight units were physically delivered; no driver internal mutation. Healthy shared cached content is unbound; deliberate negative session capture refuses factory reuse. No arbitrary malicious provider checkpoint support claimed.

## Exact commands and retained history

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Inventory capacity admission: immutable callback vectors exact offers and original physical stock ownership" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
```

Four compile attempts: solution3.62s/exit1/0 warnings/1 new stateless query component missing explicit schema registration argument (production compiled); focused7.67s/exit0/0 warnings/errors; focused11.55s/exit0 after original topology parameter cleanup; focused18.36s/exit0 after discovered storage filter bypass correction, all clean. Query instrumentation declares schema1, no production state schema changes.

| Attempt | Passed | Failed | Runtime ms | PID | Process ms | Result |
|---|---:|---:|---:|---:|---:|---|
| FIRST | 374 | 3 | 3415 | 29264 | 3721 | Invalid check status silently converted to resource refusal by actual storage filter; migrated that bypass to the same validator |
| FINAL | 768 | 0 | 3766 | 11064 | 4019 | Exited0/0 aborted; no truncation or observer error |

Same ONE NEW exact case, not old/family/full regression. Scope60,000ms watchdog/5,000ms termination/24,000 output chars; physical stages bounded512Updates with engine job/state diagnostics. Failed attempt retained; no assertion deletion, changed refusal expectation, material injection or shortened workflow. No executable edits after FINAL.

## Coverage and limits

Four minute/half completion/cancellation flows physically haul eight original units from native source to native receiver, manufacture with original WIP/tools/coupled books, earn authored time, pause/save/continue, retain private immutable facts, settle exact finite quantities and verify saves/replays/interleaved isolation.

Thirty-eight minute/half queries exercise over/fractional/refusal-with-positive/negative/undefined/self-inconsistent offers; zero quantum; undefined Check on incoming/outgoing/conversion; original Check/Offer throws and clock mutations; live stock/projected/request vector mutation attempts. Illegal pure query fails without healthy time/geometry/stock/result delta publication, retains original/malformed diagnostic and first fault, stops Update, refuses healthy hash/save and leaves a separate healthy game unchanged.

Existing topology callers compile on the shared gateway; older network runtime was NOT rerun. Broad every endpoint/receipt/continuous/coupled conversion/global side effects/arbitrary malicious provider/durability/lockstep/shuffle/fuzz/soak/allocation/performance/full gates remain open. Defensive argument allocations have not received a budget proof. No new production public gameplay API/state/clock owner; three stateless contract helpers are public. All optional Tycoon and sole-clock source unchanged. Oil/platform/Draw untouched; authorized final full regression deferred until implementation finishes. No staging/commit/push.

## Source freeze and metadata closure

Freeze2026-10-12 11:23:50 UTC:1,752 source anchors/983 selected executable inputs/13 quickstart anchors match actual files with0 drift. Inventory2,938 files/1,047 Markdown documents/8,712 local links/0 broken links;18 valid master rows;221 original literal gate/line anchors validated with0 errors. Original187 checked/34 open unchanged, additional220. Eleven existing selected executable inputs changed and three new executable inputs; five existing production sources changed, no new production source or state schema. Sole clock and all12 optional Tycoon sources unchanged. git diff --check exit0 with34 pre-existing warning lines;2,926 dirty entries/0 staged. No executable edits after FINAL; no old/family/full runtime or staging/commit/push.
