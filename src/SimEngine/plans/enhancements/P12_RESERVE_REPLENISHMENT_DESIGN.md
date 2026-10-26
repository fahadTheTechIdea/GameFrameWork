# P12-D10 — Emergency reserve replenishment

Status: focused verified, 2026-10-06. Extends P9/P12 stock preparedness; maintenance-specific exemptions stay open.

Current working-tree build `beep-simengine-2.162.0`, save89, LogisticsService10, logistics view18,
WorkSystem23. WorkBoard27/work view50/company policy2/view2/native-public outlet codecs2 unchanged.
Clean build; 291 checks in exactly two suites (116 contracts;175 physical), zero failures/aborted
suites in the final gate. [Evidence](../../docs/migration/P12_RESERVE_REPLENISHMENT_VERIFICATION.md).
One unchanged GameClock; update numbers are coordinates of that clock, never independent clocks.

## 0. Problem

D9 permits emergency exports from an explicit outlet reserve, but `ProtectedStock` still protects
every source stock target. A reserve depot with a refill band therefore cannot lend those supplies.
This prevents an authored reserve from both supporting an incident and automatically recovering.

## 1. Goals

1. Existing immutable stock settings explicitly author whether their desired stock is protected or
   available to genuine emergency demand. No default/legacy constructor or extra reserve balance.
2. Both outlet and stock policies must allow access; ordinary routes/hauls/refill never borrow it.
3. Non-stock workflow obligations remain protected regardless of reserve policy.
4. Existing refill latch, demand owner and physical dispatch restore reserve stock with real deliveries.
5. Native/public policy providers seed the same stock rows through one engine-owned initializer.
6. Commands/private views/controls/save/hash use the same settings and preserve access on band edits.

## 2. Design

Rename the shared public access enum to `LogisticsReserveAccess`, used by the existing outlet and
stock settings. Add required `ReserveAccess` to `LogisticsDemandSettings`, authored as required
`stock.<resource>.reserve_access` (`protected` or `emergency_only`). Replace constructors/readers/
wire layout directly. Existing demand rows alone own the settings/latch/identity/age.

Keep public `DemandOf` reporting all active intent. Its sole calculation can omit only explicitly
emergency-accessible stock rows when the private validated emergency export path also sees an
emergency-accessible outlet. Never omit workflow/response/tool demands. Stock access is not a demand
emergency classifier: reserve refill is ordinary work and respects standby/company worker floors.

Existing band-only automation/configuration preserves access. Reject emergency access on a stock
target without actual outlet policy/port. Restore rejects non-stock rows carrying stock-only access.
Move the native initialization loop into one shared `LogisticsPolicyRules.InitializeStock`; public
proof policy uses it through entity initialization instead of rejecting profiles containing stock.
Existing source demand/physical stock/receipt and refill logic remains the only replenishment owner.

`WorkSystem.PlanOrder` keeps `CopySupply` before the unavailable-requirement branch for every valid
automatic-supply result. Physically delivered but unclaimed inputs remain a workflow obligation
while waiting for a compatible actor. Previously withdrawing that obligation let ordinary refill
export response stock, then request it again indefinitely. Existing node bindings/choices exclude
acquired or spent inputs; no second supply cache or synthetic worker is introduced.

## 3. API

Required sixth stock constructor argument `LogisticsReserveAccess reserveAccess`; existing policy
commands/previews/views persist it. Enum rename is deliberate, without alias. One public shared
initializer supports ordinary engine extension providers; no new host command or scheduler.

## 4. Files

Modify LogisticsPolicies, LogisticsStockPolicies, LogisticsCargo, LogisticsDemands, Logistics,
LogisticsModule, Work (PlanOrder ordering/schema), SaveGame, public proof policy and explicit existing fixtures. Add one JSON fixture,
one test class and evidence document. Update master/parent/current docs.

## 5. Errors

Missing/unknown/mistyped access fails content composition. Missing export capability refuses the
stock control; invalid quantum/envelope refuses before mutation. Unknown/truncated access rejects
the settings codec. Ordinary or workflow obligations never gain emergency authority from a band.

## 6. Compatibility

Development-only breaking replacement. No aliases, default constructor, loader or parallel policy.

## 7. Verification

Clean solution build, then only two exact suites:

- `Reserve replenishment: strict stock access, shared controls and protected obligations`
- `Reserve replenishment: physical reserve use, refill and saved continuation`

Cover native/public outlets and policies, strict JSON, private/frozen settings, band edit preservation,
ordinary versus actual emergency admission, source obligations, physical incident and refill, donor
stock conservation, partial handling save/load, interruption/pause/session isolation. Bounded waits
report real jobs/blockers. No third/family/full suite or oil/platform gate.

The physical suite also repeats incidents, exhausts a finite donor, saves unresolved refill demand,
and proves an emergency-only worker does not perform ordinary refill until explicitly enabled.
A public haul-only worker delivers supplies without pretending to repair; those supplies remain
protected while the missing repair capability is reported. Every physical wait is bounded at 400
game updates with engine-derived job/resource diagnostics; the bound was not increased to hide stalls.

## 8. Risks

Do not confuse a refill target with an emergency request. Refill must not borrow reserved workers or
materials. Existing stock maximum and native capacity still bound admission. A depleted reserve is
a real shortage, not a synthesized stock promise. Scoped tests do not prove all provider/global purity.

## 9. Non-goals

Maintenance-specific exemptions, distinct-role readiness, fuel/traffic, science/contamination,
supplier/dock matrices, global purity/replay/stress/full plan. One unchanged game clock; no host logic.

## 10. Tasks

- [x] D10.1 — Replace shared access/settings protocol and centralize stock initialization.
- [x] D10.2 — Extend existing stock protection and preserve unclaimed workflow obligations through the existing supply owner.
- [x] D10.3 — Add and run two exact suites after a clean build.
- [x] D10.4 — Record evidence and update authoritative tracker/current docs with remaining full scope.

P9/P12 and the overall implementation remain partial. Older gates are historical/not rerun;
compilation of migrated fixtures is not proof of their runtime behavior.
