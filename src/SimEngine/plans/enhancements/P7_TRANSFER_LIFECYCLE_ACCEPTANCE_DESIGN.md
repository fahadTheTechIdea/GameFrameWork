# P7-C5: physical transfer lifecycle acceptance — Design Document

## 0. Problem Statement

The original P7.018 gate still requires combined transfer/cancel/despawn/save
accounting and physical position proof at partial pickup and delivery. Existing
`LogisticsService`, `WorkBoard`, inventory/receipt owners, guarded entity removal
and resource recovery implement those rules; historical separate proofs are
not current acceptance evidence for the complete lifecycle.

## 1. Goals

1. Native and separately bound cargo, minute/half-minute clocks complete a
   normal physical haul before interrupting another at each handling phase.
2. Live claims prevent endpoint packing/removal. Saved pause/cancellation
   preserves exact partial cargo, spent stock, handling prefix and position.
3. Earned endpoint packing/removal accounts for stock at its actual placement;
   stranded cargo returns physically to an authored receiver with exact receipts.
4. Loaded carrier retirement refuses until disposition; earned empty-carrier
   retirement is physical, timed and once-only. All checkpoints/replays isolate.

## 2. Design

Reuse the existing pure `TransporterRetirementTests.Compose` master/JSON factory
and its ordinary player activity helper, exposed internally without changing
old test bodies or engine behavior. One new public-input driver combines a
complete4-material haul with an8-material interrupted shipment, endpoint
stock packing/removal, residual cargo return and final carrier retirement.

The original20-material finite balance is checked across each completed Update.
Private engine facts supply actual endpoint, promise, cargo, job and position
observations; no copied inventory, carrier, reservation or work algorithm.

Carrier and cargo-owner IDs remain distinct for bound providers. Source/delivery
endpoints are removed only by authored paid-in-time activities after owned
promises release and stock disposition. Actual inventory location determines
packing, actual carrier location determines physical travel/retirement service.

## 3. API surface

No engine API/schema/state/clock changes expected; current2.450.0/save154/replay1.
Only test factory/player helper visibility changes; one runner registration.
If the integrated proof finds a real failure, repair its existing engine owner
and update identity before further runtime, never add a driver fallback.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | `tests/Beep.SimEngine.Tests/TransporterRetirementTests.cs` (factory/helper visibility) | low |
| Modify | `tests/Beep.SimEngine.Tests/Program.cs` | low |
| New | `tests/Beep.SimEngine.Tests/TransferLifecycleAcceptanceTests.cs` | medium |
| New | `docs/migration/P7_TRANSFER_LIFECYCLE_ACCEPTANCE_VERIFICATION.md` | low |
| Modify | Original P7.018 checkbox only if exact combined evidence qualifies | low |
| Modify | Master/current checkpoints/audit | low |

## 5. Error handling matrix

| State | Expected outcome |
|---|---|
| Partial haul with owned claims | Packing waits `ActivePromises`; no stock/removal prefix |
| Saved pending cancellation under paused clock | Input/work/position unchanged until normal resume |
| Cancelled haul with actual cargo | Old receipts released; prefix/cargo/position retained |
| Loaded carrier after claims release | Removal waits `UndisposedStock`, never discards cargo |
| Removed endpoint | Actual stable reference absent; no rebinding to another provider |
| Residual cargo recovery | Same actual carrier/inventory owner; exact physical return once |
| Empty carrier retirement | Real service/travel/duration then one original removal effect |

## 6. Backward compatibility

Development-only; no compatibility/legacy path. Original current schemas remain.

## 7. Verification

ONE NEW exact case:

`Transfer lifecycle acceptance: saved pickup delivery cancellation and physical removal conserve stock and position`

Eight native/bound × minute/half × pickup/delivery flows. One solution compile
attempt, necessary targeted repairs only; terminal clean build before runtime.
Outer60000ms/termination5000ms/output30000chars; each physical stage512Updates
with last12 private job/stock/position diagnostics. Preserve all attempts; no
old/family/full suite. Public commands, views, saves/replay and Game.Update only.

## 8. Risks

Point-space single-route occupancy is real: the service actor must return to its
authored home before carrier recovery, never teleport/cross. Bound stock stays
at its actual separate owner until verified physical disposition. No arbitrary
provider/stress/deadlock-freedom/security/global accounting claim.

## 9. Out of scope

Quality/lot selection, whole P7.017/.020, topology/conversion/spill accounting,
protected clock/G19/Tycoon, Oil, adapters/Draw, full regression and publication.
Only exact original P7.018 may close; full goal and other gates stay open.

## 10. Sub-task execution order

1. Read actual retired carrier/endpoint fixture and public diagnostic contracts.
2. Add combined new driver using existing pure factory/ordinary input helper.
3. Compile and execute only the declared exact bounded case; repair existing
   owners if a real failure is evidenced, preserving all attempts.
4. Freeze executable bytes, qualify only matching original requirement, update
   master/evidence/current metadata and validate source/gate/link integrity.

Status: scoped verified at2.450.0/save154/replay1 unchanged. FINAL23773/0/0 in
34606ms; eight native/bound minute/half pickup/delivery complete→cancel→endpoint
removal→residual return→carrier retirement flows. Three clean compiles/all three
runtime attempts retained, including28 wrong fixture assertions and one timeout.
Only original P7.018 checkbox qualified:221/182checked/39open, additional176.
No staging/commit/push. [Evidence](../../docs/migration/P7_TRANSFER_LIFECYCLE_ACCEPTANCE_VERIFICATION.md).

## Metadata closure

1495 source anchors/761 selected executable inputs/13 quickstart anchors match;
zero post-FINAL executable drift.960 Markdown documents/8576 local links have
zero broken links;18 master rows valid. All221 literal gates validate; preceding
baseline differs only by P7.018 checkbox, no requirement text/document/line shift.
182checked/39open, additional176. Driver boundary matches0; diffcheck exit0 with
34 existing line-ending warnings.2720dirty/staged0 preserved, four protected
clock/G19 hashes unchanged. Two existing test inputs changed/one new driver;
zero production source changes. No gameplay compile/rerun during closure.
