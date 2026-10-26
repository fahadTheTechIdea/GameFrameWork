# P6 Planet X finite resource-chain verification

## Outcome and authority

Scoped complete on build 2.303.0/save144/replay1. Full P1–P12/goal remains ACTIVE.
No core engine source changes, old behavioral tests, family/full regression, Oil/Tycoon or platform runtime.
Commit and push are explicitly deferred until FULL goal AND plan completion and verification.
The lifecycle skill kept startup/contracts, staged public proof and full-plan closure distinct.

## Implementation ownership

- New explicit master/scenario: planet-x-master.json and planet-x.json in the independent proof assembly.
- PlanetXModule registers existing PlanetOreDef and one new public PointLayerSetupDef.
- Native WorldSetupStage is the only initialization DAG; PointLayer remains the only deposit quantity owner.
- Existing PointModule, PointSpatialResourcesModule, ContractWorkerDef, point_stock and PointDock are reused.
- Native WorkBoard/WorkSystem, LogisticsService, ResourceTransactions, TransformerManager,
  ConstructionService, ResourceFlowJournal and GameClock retain all authoritative gameplay.
- PlanetX.Factory only supplies explicit module/content sources; no dynamic synthetic catalog.
- PlanetXAcceptanceTests uses normal commands, detached public views, Update, clock, saves and replay.
  No Game.World access, private reflection, stock mutation, spawn grant or manual-haul workaround.

Native canonical unit.work-rate is explicitly authored, not renamed or silently substituted.
Service berths are authored at offset -1; worker homes at facility origin do not occupy them.
Refinery public export port is explicitly enabled for automatic owned-stock matching. Port export
permission is not private-layer visibility: foreign layer/order/history facts remain denied.

## Physical terminal accounting

Initial actual underground layer:12 ore, private owner0, depth -5 metres. Every inventory starts empty.
Six extraction orders each earn one minute before native transfer of2 ore to stationary mine.
Carrier performs physical loading/movement/unloading; six two-minute native refinements yield6 alloy.
Three-minute assembly consumes4 alloy, produces exactly one custom-contract builder through native rollout.
A pre-existing blocked beacon plan becomes real prepare/build work only after machine/material availability;
its build consumes2 alloy. Final actual layer/inventories ore0/alloy0; exactly one rover and one beacon.

Native journal records12 ore as spatial StockTransfer,6 produced alloy,18 combined native count units
consumed (12 ore +6 alloy). No Initialized/AuthoredCreation/StockPurchase facts occur.
These are expected assertions over native facts, not a copied balance or host conversion rule.

## Exact gate and all attempts

Each build command: `dotnet build Beep.SimEngine.slnx --no-restore`.
Each runtime command (same ONE distinct NEW bounded registered case only):

`dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Planet X acceptance: finite extraction, timed refinery, manufactured worker and physical deployment"`

| Attempt | Clean build | Runtime | Observed repair |
| --- | --- | --- | --- |
| 1 | 5.62s;0 warnings/errors | 0 passed,1 failed/aborted;102ms | New authoring used noncanonical transformer target unit; authored actual unit.work-rate |
| 2 | 4.22s;0 warnings/errors | 68 passed,1 failed/aborted;5442ms | Native typed dock occupied; mine service coincided with returning worker home; authored distinct service offset |
| 3 | 3.89s;0 warnings/errors | 70 passed,1 failed/aborted;5288ms | Suspected second berth/home conflict; separated refinery service, but next run still stalled |
| 4 | 3.59s;0 warnings/errors | 70 passed,1 failed/aborted;5935ms | Remaining delivery stall disproved berth-only explanation; inspect actual automatic source selection |
| 5 | 3.32s;0 warnings/errors | 70 passed,1 failed/aborted;5440ms | Added public demand/explicit haul diagnostics; ready explicit haul versus disabled auto-export identified |
| 6 | 3.65s;0 warnings/errors | 86 passed,0 failed/aborted;2567ms | Enabled actual refinery export in new authored JSON; no engine shortcut |

No earlier behavior, related-family or broad suite was run. Builds compile existing tests; compilation
does not revalidate their behavioral assertions. All failed attempts are retained as evidence.
Executable source/proof/tests/JSON frozen 2026-10-08 14:47:58 UTC; subsequent changes are docs/read-only.

## Durability and public boundary

Pending recipes/project/extraction, partial process, manufactured machine and final state roundtrip.
Saved physical twins advance identically; sole GameClock pause freezes actual updates.
Retained entity snapshot stays detached. Untouched interleaved session hash remains unchanged.
Canonical ReplayVerifier accepts actual player stream and matches exact final source hash.
Foreign layer/order/resource-flow facts refuse. No business feature or tile-navigation selected.

Every reach has maximum2,000 updates and a30-second observational watchdog with public diagnostics.
This cannot interrupt a blocked single synchronous Update; external-process watchdog remains OPEN.
Coverage does not prove arbitrary content/provider cycles, malicious hooks, global rollback, optimality,
lockstep permutations, larger throughput/stress/fuzz/soak, allocation budgets or broad industrial baseline.
Expanded Planet X technology/automation signals/incident response still needs explicit authored slices;
the extraction/manufacture/deployment chain alone does not close the broader mod proof.

## Source audit

Scoped source audit:1853 self-excluded paths,608 Markdown,3894 local links. All359 prior core
runtime paths and337 existing authored JSON unchanged; two new Planet X JSON documents.
Sole GameClock SHA unchanged. Actual git diff --check exit0 (32 line-ending messages);
18 five-column phase rows valid. New player driver live-World/mutation scan has no matches.
Final manifest/hash/link audit is evidence metadata only, never full behavioral acceptance.

[Source manifest](P6_PLANET_X_ACCEPTANCE_SOURCE_MANIFEST.json) records current hashes and previous-checkpoint
comparison. Self-excluded manifest is metadata, not execution input. Final read-only audit appended there.
[Stage](../../plans/enhancements/P6_PLANET_X_ACCEPTANCE_DESIGN.md) ·
[Master tracker](../../plans/enhancements/MASTER_TRACKER.md)

