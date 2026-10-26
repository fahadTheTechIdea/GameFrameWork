# P7-D2 / P3-S11 authored provider recovery — focused working-tree evidence

Date: 2026-10-05. Scoped working-tree proof, not clean HEAD/full regression/framework completion.

## Identity and actual gate

Build **2.71.0 / save43 / work20 / work-view25 / logistics4 / logistics-view9 / construction-view3**.
Existing native inventory/resource/job/work/logistics codecs remain unchanged. No new recovery state
codec or compatibility reader; global semantic/save identity and immutable construction view change.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Resource recovery: guarded removal, live stock and cancelled-site continuation"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Resource recovery: timed construction, native salvage and saved physical delivery"
```

Final build **0 warnings, 0 errors, 2.38 seconds**. Final exact-suite results:

| Exact suite | Passed | Failed | Runtime |
| --- | ---: | ---: | ---: |
| Resource recovery: guarded removal, live stock and cancelled-site continuation | 89 | 0 | 354 ms |
| Resource recovery: timed construction, native salvage and saved physical delivery | 89 | 0 | 517 ms |

**178 checks / two suites**, zero failures/aborts. Same two names reran during this slice; no third
name, broad family, default runner or full gate ran. PointRecoveryTests is invoked within these
two suites; it does not create a separate runner gate. Prior suites/API/identity callers compiled only.

## Implementation and evidence

- ResourceRecoveryDef/IResourceRecoveryDefinition selects actual catalog space/container aliases.
  Scenario settings.sim.construction.recovery explicitly names a profile or null. Invalid/missing
  selection, unknown space/container, stocked creation, missing inventory and moving point container
  refuse before Initialize. No fabricated default, construction_settings reader or generated recovery.
- ConstructionService/IResourceRecovery/IConstructionProjects remains the sole owner. Its partial
  ConstructionRecovery.cs packs through native receipts or explicit authored lifecycle yield; no
  separate stock/claim/packing ledger, scheduler, codec or compatibility LeavePile API.
- ISpatialRecoveryPlacement extends the existing selected JSON/capture/check/configure/identity
  contract. Geometry remains in tile/point providers; generic recovery/demolition has no tile lookup,
  coordinate conversion or engine-imposed raster/size limit. Actual created container identity and
  empty state verify before transfer; each packed amount is bounded by the real remaining quantity.
- Pure private ConstructionView blockers/container ID, current bounds, foreign geometry/company/
  session, fractional counts and resource filters refuse without hash/ID/stock/claim mutation.
  Explicit null returns typed MissingRecovery for stocked sources, but empty removal needs no profile.
- Existing tile cases cover owned removal guards, supplier/native claims, external stock splitting,
  fractional fluids, cancelled-site retention/cleanup, line disposition, real construction consumption,
  structural salvage accounting and later actual delivery. The initial run exposed an old test that
  equated assigned Working state with positive physical progress; its bounded wait now requires actual
  progress after travel before asserting input spending. The final run passes, not a weakened assertion.
- Point timed recovery is an ordinary master-listed activity/effect. A player commands it; machine
  work precedes five capacity-four local containers holding twenty real units. Source balances empty
  through owned receipts and flow facts are StockTransfer, not manufactured salvage. Later native
  loading/hauling/delivery retires verified-empty containers without quantity loss.
- Point cancellation uses player work policy (haul-only) and a normal plan. Eight actual units travel
  to the unbuilt site. A foreign inbound promise blocks removal; the ended native order/site/stock/
  token save exactly. Releasing that actual promise lets the same cleanup owner move eight units to
  two local containers; later real hauling returns them. Total stock stays twenty with no refund/yield.
- Point construction uses authored all requirements: consume eight and hold four actual supplied
  tools. Native delivery/work spends only eight; the four real tools remain, then release at completion
  and transfer to a local container before empty site retirement. No injected surplus/free material.
  Saved working construction, surplus recovery and later physical tool return match exactly.
- Player point demolition shares pure admission and ordinary authored graph/time. It creates six
  explicitly authored salvage units split four/two at the real structure location. Native Produced
  facts carry demolish:point.tower; total surviving quantity increases from twelve to eighteen, not
  a refund of consumed inputs. Real later delivery and saved build/demolition/haul stages match.
- Every physical wait has a bounded update count and diagnostic snapshot. Interleaved saved twins
  prove these actual scoped chains, not universal deterministic/purity/replay coverage.

## Remaining required work

This completes this geometry/authoring slice only. Full objective stays active/partial. Broader
completion-admission retry policy is still needed: current WorkSystem.EndOrder treats a refused
completion effect as failure and retained site cleanup, not retryable completed work. Provider fault
matrices, all geometry/mod combinations, whole multi-leg transfer batches, templates/bulk/repeat/
history, fleet/areas/congestion/replanning/replay/fuzz/soak/performance/full acceptance and P8/P10/
P11/P12 remain required. Focused results do not authorize closing those tracker gates.

Stage/master/P3/P4/P7/P9/README/AGENTS are updated together. No OilFieldDays/Tycoon/host/demo/platform/
Draw work, commits, publication, deletion of user data or memory writes. Lifecycle skill guided
staged ownership/docs/focused proof; repository/user restrictions overrode full-regression advice.
