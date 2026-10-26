# P9-C10 — Logistics view durability verification

Date: 2026-10-10. Scope: canonical Beep.SimEngine only. Full goal and P1-P12 remain open; no staging, commit or push.

## Implemented boundary

InventoryView and every LogisticsView method check the original World.RequireHealthyUpdate before missing/private/no-policy/empty results. Scalar logistics callbacks recheck health before publishing output. Named store facts use the existing actual provider observation. Demand/cargo/routes/jobs/queue/assignment projections gather detached local rows and only publish after successful, healthy observation. WorkView optional dispatch-policy absence is also gated. This is not a new fault state, scheduler, inventory, work graph or game clock.

Engine2.408.0, save154 and replay1. Inventory-view6, logistics-view41 and work-view98; no saved payload codec changes. Original native work, resource claims, physical docks/navigation, handling and clock owners unchanged. Sole clock and three protected G19 Tycoon files are not edited.

## Focused runtime

ONE NEW exact case only:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Logistics view durability: retained stock cargo jobs and empty queues reject first update fault' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

All attempts are preserved below. Exited children, stdout/stderr untruncated, observer errors empty; 30000ms runtime /5000ms termination /30000 output characters. Physical wait stage bounded to256Updates.

| Attempt | Passed | Failed | Aborted suites | Suite ms | PID | Exit | Observed ms |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1 |38|105|0|676|7288|1|829|
| 2 |143|0|0|646|41160|0|788|
| 3 |0|1|1|356|18816|1|604|
| 4 |150|1|0|665|7260|1|819|
| 5 FINAL |197|0|0|666|28100|0|812|

1. The negative update probe was registered/master-listed but omitted from the selected scenario. No failure was injected. Fixed explicit content selection, not engine selection logic.
2. Retained update-fault matrix passed. Extended the same case with an actual failing dispatch-source callback.
3. Negative provider wrongly bound the already-owned IWorkerDispatchSource primary service. Composition correctly refused. Bind its distinct concrete service; original Providers<IWorkerDispatchSource> discovers it, no replacement owner.
4. Callback fault was correctly latched/prefix withheld; assertion wrongly omitted the public exception-type prefix. Fixed expected text and required observation.provider phase.
5. Final also covers actual source, worker and missing identities for own/private retained features.

## Builds

Command: `dotnet build Beep.SimEngine.slnx --no-restore`.
All SIX builds exit0 /0warnings /0errors:6.44s,4.68s,4.93s,6.20s,4.66s,4.48s FINAL.
These compile historical registrations but do not execute them.

## Proven behavior

Two ordinary pending hauling commands acquire distinct trucks/actual reservations. Native movement and authored amount-scaled handling earn partial four-unit cargo. A saved twin remains identical through every advanced step; repeated healthy projection reads do not alter hashes. The partial checkpoint and normal command replay verify. The sole clock pause freezes advancement.

After an actual system update error, retained stock/resource-port/capacity/reservation/content-acceptance facts, operational/throughput/outlet/stock/load/trip/service/budget/disposition diagnostics and haul/route previews refuse before normal missing/private/no-policy fallbacks. Demands/routes/logistics queues/jobs/work queues/assignments clear outputs and throw even when empty/private; optional dispatch-policy absence cannot bypass the gate. First fault and previously detached cargo/claim/job facts stay unchanged. Faulted save/hash reject. An interleaved healthy session and saved physical twin continue independently.

A negative public-contract dispatch provider adds a private prefix then throws from CopyOffers during CopyAssignments. The original observation guard records the exact first error, caller rows remain empty, later missing/absent reads/save/hash refuse without overwriting it, and another session remains healthy. No driver implements assignment, stock, handling, travel, demand or policy decisions.

## Source identity and scope limits

487 selected executable inputs frozen 2026-10-09 23:31:41 UTC;15 changed/new paths versus prior483, including four new paths. The authoritative audit stores exact SHA256s. No executable edits after FINAL.

Original221 requirements remain153checked/68open. No original text/checkbox changed or compound gate closed. This is additional scoped durability evidence, not full P6/P9 completion, arbitrary provider rollback/purity, stress/allocation proof, all WorkView policy/area/planning coverage, full saved hauling lifecycle or all optional modules. G19 permission remains pending. No historical/family/full/Oil/Tycoon/platform runtime.

Metadata review:1135 source anchors plus13 quickstart anchors, zero hash mismatches, zero broken local Markdown links, all18 master rows retain five columns; git diff --check exits0 (34 existing warning/output lines). Final selected487 executable hashes have zero post-pass drift. Sole clock and all three protected G19 hashes remain identical. Dirty inventory2418 paths using --untracked-files=all; staged0. No original requirement text/checkbox changed.

The lifecycle skill influenced stage separation, original-owner review and the explicit attempt/evidence record. User/repository focused-test restrictions override its full-regression recommendation.
