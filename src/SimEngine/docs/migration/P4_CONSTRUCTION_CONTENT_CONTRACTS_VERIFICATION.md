# P4-G56 — Construction content contracts: scoped verification

Verified 2026-10-10. Core `beep-simengine-2.403.0`, save154/replay1/construction-view4. Original component and work codecs are unchanged.

## Engine ownership

ConstructionDeclaration is the sole strict construction JSON reader and plan/template/removal declaration. Native and independent registrations delegate it. It binds the actual registered construction definition and authored activity graph; it never copies or recompiles the DAG. Its removal plan selects IConstructionDefinition, not native ConstructibleDef.

ConstructionSiteDeclaration is the sole target reader. Existing Site selects IConstructionSiteDefinition and its authored ConstructionState, not native SiteDef. Original order/Sync, cancellation, retention, inspection and entity Update state settlement remain. Constructible is the same empty capability without a native generic cast.

ConstructionBindingContracts validates every loaded definition, unused included, through the existing composition owner before Initialize: actual frozen target/site/workflow/state aliases, reusable runtime/codec1, graph identity and supply/salvage bills. Selected site state and inspection reads observe the original whole entity codec, provider and clock; recursion latches the first fault. This is not proof of arbitrary provider or all getter purity.

The original construction service is bound only through IConstructionProjects/IResourceRecovery. There is no concrete service binding, compatibility alias, duplicate project coordinator, state store, scheduler or clock.

## Exact runtime

Only this ONE NEW case was run, with two attempts of that same case:

```text
Construction content contracts: one physical owner and independent authored sites
```

| Attempt | Passed | Failed | Aborted | Suite ms | Child PID | Exit | Observed ms |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | 51 | 2 | 1 | 800 | 19168 | 1 | 955 |
| FINAL 2 | 158 | 0 | 0 | 907 | 35068 | 0 | 1056 |

Both children authoritatively Exited, with untruncated stdout/stderr and no observer errors. Limits: outer30000ms, owned termination5000ms, output30000 characters. Each physical stage has a180-Update limit and actual order/blocker/stock diagnostics. Aborted1 records the first suite exception, not an observer timeout.

The first attempt incorrectly expected the preparation node to consume the later build node's eight-unit material bill, and the proof-only fault selector used a required JSON getter despite being absent from normal independent content. The driver now waits for actual material-consuming build work; proof instrumentation reads its optional negative selector without changing gameplay authoring/defaults. Production material rules and refusal semantics were not weakened.

## Builds

`dotnet build Beep.SimEngine.slnx --no-restore`:

| Build | Exit | Elapsed seconds | Warnings | Errors | Note |
|---|---:|---:|---:|---:|---|
| 1 | 1 | 7.86 | 0 | 1 | negative proof used non-capability IInspectable in Entity.Get |
| 2 | 0 | 8.79 | 0 | 0 | compile correction; ordinary demolition uses Target |
| FINAL 3 | 0 | 5.52 | 0 | 0 | clean, completed slice |

Six historical test files received mechanical service-lookup changes only: native-owner inspection uses provider inventory; optional absence checks IConstructionProjects. Their cases were not executed.

## What is proven

Native/direct JSON registrations share one actual construction/recovery owner, graph and site/target/state aliases. Ordinary player commands post a paused plan without a site/order/spend, activate the authored site, observe the entity's settled state and two-node DAG, physically supply eight units, earn machine work, pause/save/resume the same project, and finish one structure. Source20 becomes12 with no duplicate cargo. Pending-plan and empty direct-site cancellation create no completed structure or fabricated stock. Original timed demolition earns removal with explicitly empty salvage.

Native/direct completion coordinates match. Partial and completed saves, retained detached node facts, company-private plans, sole-clock pause, ordinary command replay and interleaved stock/project isolation pass narrowly. Twelve malformed unused bindings refuse before Initialize, including required references/states/removal/salvage, unknown references, null metadata, mismatched codec, missing contract, orphan site and unknown JSON fields. Five armed negative site callbacks throw, pause, corrupt the saved order, touch identity or recurse; original faults latch and healthy save/hash refuse. First callback text remains original.

Selected executable inventory:456 inputs, observed2026-10-09 22:03:12 UTC after FINAL green;16 changed/new compared with previous446. Exact hashes are in the completion audit. No executable changes after this pass.

## Open scope and publication

This is not complete P4/P9 or P1-P12 acceptance. All provider purity, foreign/neutral sites, supplied cancellation/recovery, template groups/repeat, areas/congestion, other spatial providers, global/performance/fuzz/soak/lockstep and full regression remain open. Historical infrastructure/incident/construction families compile only. Protected resource-catalog/Tycoon authority remains pending.

No protected clock/Tycoon file, Oil behavior, budget/default-money behavior, rendering/platform, staging, commit or push changes. Publication remains deferred until the FULL goal AND plan genuinely finish and are verified.
