# P9-C2 — Native stock and export policy verification

Date: 2026-10-05. Current working tree: **2.56.0 / save30 / logistics3 / logistics-view6**.
New logistics_policy component1; work/work-view13 and resource-flow4 unchanged.
[Stage](../../plans/enhancements/P9_STOCK_EXPORT_POLICIES_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Final scoped gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Logistics policies: strict native bands, shared admission and private settings"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Logistics policies: physical refill, export contention and saved commitments"
```

Build: **0 warnings / 0 errors**, 1.38 seconds. Final gate: **112 checks / 2 suites / 0 failures /
0 aborted suites**. Only these same two exact directly affected suite names ran during development.

| Exact suite | Passed | Runtime |
| --- | ---: | ---: |
| Logistics policies: strict native bands, shared admission and private settings | 63 | 266 ms |
| Logistics policies: physical refill, export contention and saved commitments | 49 | 226 ms |

Reported suite runtime totals 492 ms. No broad/full regression, Oil/Tycoon, host/demo/renderer/platform,
replay/lockstep, stress/fuzz/soak or allocation gate ran. C1/B2/B1 and earlier reports are historical,
not rerun. Existing command/demand references in the named source files were migrated and compiled;
those unrelated suites were not executed. The first runner invocations after a test compile error
found zero suites in stale binaries and are not counted as evidence. Qualifying the JSON type fixed
that compile issue. A development reserve assertion ignored an already-owned outbound promise;
the final assertion uses real unpromised availability, not a fabricated starting balance.

Final read-only handoff checks passed: scoped tracked-file `git diff --check` and an explicit
trailing-whitespace scan of new policy source, proof, tests and stage documents were clean. Git
reported only the checkout's LF-to-CRLF normalization notices. Source searches found no old
SetStockTarget command/handler/registration, PlayerStockPriority or demand.Level/Priority access
under src/tests. Build/save constants, service3, view6, component1 and both new command registrations
match this report. Documentation-only handoff changes did not trigger another build or test run.

## One owner per responsibility

Existing LogisticsService.Demand implements ILogisticsDemand. Immutable LogisticsDemandSettings
replace Level/Priority fields; the same row owns enabled/minimum/desired/maximum/priority, original
creation age and the saved refill latch. ILogisticsStockPolicies aliases that same service/table.
There is no second stock target, inventory, reservation or scheduler store.

Replenishment starts below minimum and remains active until actual unpromised stock reaches desired.
Incoming promises prevent duplicate acquisition but do not pretend cargo has arrived. Priority-only
edits preserve the active cycle. Disabled/minimum-zero settings stop it; minimum zero deliberately
provides a manual-only receiving ceiling. Stock maximum limits new inbound promises using actual
quantity plus existing inbound commitments. It does not erase real stock or revoke already acquired
claims when lowered. Ordinary native workflow Bills remain exact targets and additive demand.

LogisticsPolicyDef profiles are explicit JSON from the master-listed point-layers.json proof document.
Stock entries resolve native resources in the existing catalog; enabled, bands and priority are
required. Outlet enabled, reserve Bill, export priority, allowed prototype destinations (explicit null
or array) and disabled entity state are required. Complete profile/edited maxima and reserves use
the existing capacity contract; no mixed-unit free space or implicit resource default is introduced.

The normal LogisticsPolicyComponent seeds stock rows through existing new-entity Initialize only.
It stores outlet settings only; current stock policy is never copied into the component. Restore and
replacement do not reseed profile state. IOutletPolicy permits another engine-side provider. Shared
LogisticsPolicyRules supplies native/profile checks; external providers reuse those rules and codec.
Entity.Update alone commits the authored disabled state. No Draw, second loop or host gameplay.

## Shared physical admission

The one haul preview/acquisition checks current export enabled/filter, protected native stock and
receiving ceiling. Automatic demands, standing routes, direct acquisition, evacuation and cargo
return call this same path. Export reserve protects max(active desired quantity, explicit reserve),
not their sum. Existing owned endpoint/cargo/worker claims still acquire atomically. Selected export
priority precedes provider route cost and stable entity identity within existing source selection;
this is not unified demand/route or company-alert scheduling.

Typed blockers identify disabled export, destination restriction, protected reserve and stock maximum.
Policy changes affect future promises only. Already acquired hauls still physically load/travel/unload
against their real receipts. A retained ready preview cannot authorize new acquisition after an edit.
Private immutable demand and outlet views return detached settings/current latch/source facts; a
foreign outlet read returns NotOwned with null settings, not private provider state.

ConfigureStockPolicyCommand replaces SetStockTargetCommand/handler/registration. The fixed
PlayerStockPriority is deleted; priority is explicit player/authored intent. ConfigureOutletPolicyCommand
uses the actual provider's admission/mutation contract. Generic source refresh still uses one native
Bill coordinator; it cannot bypass stock capacity rules by using the sim.stock source directly.

One logistics service codec3 saves settings/latch/age and rejects invalid canonical/native records.
Outlet settings use the owning component codec1; no mirrored stock codec. Save30/view6 replace prior
layouts without a legacy reader. Restored settings survive changes rather than reverting to profiles.

## Proof and limits

Public-only ContractOutletPolicy demonstrates a mod-owned exporter without changing source selection.
Focused proof covers pure/private admission; strict JSON; native count and dimension refusal; two
individually fitting maxima rejected by the complete native envelope; stable age/settings snapshots;
disabled entity state; actual multi-load refill and quiet-band behavior; saved partial-cycle priority
edits; source reserves; route/direct/competing-carrier limits; farther higher-priority source selection;
disabled loaded export/requester continuation; lowered ceilings with grandfathered receipts; saved
hash identity; and independent interleaved sessions. Physical waits are bounded to 320 updates per
stage with actual job/state/blocker diagnostics.

This scoped gate does not prove every transport failure, provider mutation/fault, cancellation/rebind,
replacement, non-entity resource endpoint or alternative spatial topology. Global demand/route
ordering, company alerts/starvation/preemption, editable/all-dispatch areas, fleet/service/fuel,
persistent plans/templates, timed docks/congestion, replay/stress/performance and full framework
acceptance remain open. No full-stage or roadmap completion is claimed.
