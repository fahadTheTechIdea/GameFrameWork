# P4-G57 — Contract-authored resource disposition: scoped verification

Verified 2026-10-10. Core `beep-simengine-2.405.0`, save154/replay1/construction-view4. Original site/work/inventory/transaction/recovery/logistics/clock state codecs are unchanged.

## Engine implementation

`IResourceRecoveryDefinition` now inherits the existing stable `IContentDefinition` identity. Construction selects its actual frozen catalog alias through that contract instead of requiring `ResourceRecoveryDef`. Native authoring delegates sole `ResourceRecoveryDeclaration`; independent master-listed registration derives only from Def and the contract, using the same required space/container schema, reference linking and resolved metadata validation. No native inheritance, compatibility accessor or alternate recovery path.

All references resolve before validation checks actual catalog space, container, matching spatial metadata, inventory capability, empty authored stock and compatible empty capacity. Original composition owner additionally validates every loaded native/direct contract profile, unused included, through observed catalog admission. The selected profile still requires the original runtime recovery-placement provider and exact selected geometry. Unselected valid profiles are not forced into the selected space. Explicit null selection never fabricates a container.

Original ConstructionService remains the sole IConstructionProjects/IResourceRecovery owner. Existing whole-stock preflight, exact quantum packing, owned resource receipts, local entity placement, construction/removal DAG, cleanup and logistics dispatch earn all physical behavior. No new stock ledger, work scheduler, project coordinator, clock, saved state or normal gameplay in drivers. Protected G19 resource-catalog/Tycoon migration remains untouched.

## Exact runtime

Only this ONE NEW exact case was run, twice:

```text
Recovery container contracts: supplied cancellation and earned salvage share physical disposition
```

| Attempt | Passed | Failed | Aborted | Suite ms | Child PID | Exit | Observed ms |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | 16 | 1 | 1 | 478 | 17392 | 1 | 626 |
| FINAL 2 | 125 | 0 | 0 | 1696 | 18008 | 0 | 1844 |

Both children authoritatively Exited; stdout/stderr untruncated; observer errors empty. Outer30000ms, owned termination5000ms, output30000 characters. Each physical stage is bounded256 Updates with actual stock/cargo/container/job diagnostics. Aborted records the first suite exception, not a process timeout.

Attempt1 correctly stalled the haul-only worker at the original preparation node, which needs inspection before downstream material work is active. The proof now explicitly authors one material-requiring first build node with a one-hour duration; inspection remains disabled during supply/cancellation, then ordinary policy enables it for construction. This is selected JSON intent, not instant delivery or an engine default change. Existing logistics recovery and flow modules are explicitly selected with master-listed policy documents; drivers contain no recovery decisions or direct state writes.

## Build evidence

`dotnet build Beep.SimEngine.slnx --no-restore`:

| Build | Exit | Elapsed seconds | Warnings | Errors |
|---|---:|---:|---:|---:|
| 1 | 1 | 4.57 | 0 | 1 |
| 2 | 1 | 6.19 | 0 | 1 |
| 3 | 0 | 5.76 | 0 | 0 |
| FINAL 4 | 0 | 4.70 | 0 | 0 |

Build1 found the missing Kernel namespace for ISpatialPresenceDefinition. Build2 found the proof's incorrect EntitySnapshot name; the actual detached snapshot model is EntityView. Both corrected without changing runtime owners. Historical tests compile only, not run. No old/family/full/Oil/Tycoon/platform runtime.

## Narrow acceptance evidence

Native and independent selected profiles earn identical completion coordinates through ordinary construction, policy and demolition commands. Haul-only machines physically deliver eight source units to an unbuilt site; no construction or material consumption occurs. Cancellation moves those eight units into two actual capacity-four recovery containers at position12, never instantly refunds the source or manufactures salvage. Original transfer facts account for exactly eight moved units. Actual later hauling empties the containers and preserves total20.

Ordinary inspection-enabled construction consumes exactly eight units and earns one structure after authored work. Ordinary demolition takes actual machine work, removes the structure and packs explicit six-unit salvage into four-plus-two local containers. Only this authored structural yield is Produced; subsequent physical hauling preserves total18 without duplicate output. Foreign company recovery queries refuse private diagnostics. Views/previews remain pure; sole clock pause, stocked cancellation save/load, physical return twins, demolition twins, completed save/load, command replay and interleaved session isolation pass narrowly.

Eighteen invalid loaded native/direct variants refuse before Initialize, including missing/unknown space/container, stocked or inventory-less container, extra field, unused bad profile and direct getter metadata disagreeing with its declaration. Explicit null recovery retains typed missing-recovery for stock and accepts empty disposition without a fabricated default.

Selected executable inventory:466 inputs, observed2026-10-09 22:33:40 UTC after FINAL green;12 changed/new compared with previous461. Exact hashes attached to completion audit. No executable edits after the final pass.

## Remaining scope

This is not complete P4/P7/P9 or P1-P12 acceptance. Arbitrary custom geometry/provider purity, active foreign claims, fractional/mixed lots, competing carrier/dock reservations, interruption across every lifecycle, all cancellation/reselection/resource bands, allocation/fuzz/soak/global/lockstep and full regression remain open. Selected geometry validation is not proof that every unselected profile supports every future runtime world.

No protected clock/Tycoon source, Oil behavior, default money/budget, renderer/platform, staging, commit or push changed. Publication waits for the FULL goal AND original plan to genuinely finish and be verified.
