# P7-D1 disposition composition and physical continuation verification

## Outcome and scope

Engine remains **2.615.0 / save159 / replay1**. No production source, protocol, Tycoon business rule, host application, rendering or platform change.

Two existing literal cases have final results **93 + 111 = 204 checks passed, zero failures**. This is not a full regression or completion of original P7.017. Original bookkeeping remains 221 requirements / 200 recorded checked / 21 tracked open; the checked count is not a fresh global audit.

The 442 core C# path/content SHA256 remains `222fa20283f1b8954caac0cc73e78283d90c4318d918c447ac2d9f43daa5af72`.

## Composition ownership

- New `resource-disposition-master.json` declares every installed module, including inactive/documentless ones. Declaring installed Tycoon does **not** activate it.
- The physical scenario explicitly omits the unused recovery `market` prototype. No general loader suppression, business activation, fixture auto-detection or compatibility path was added. Other consumers of `resource-recovery.json` retain their original content and are not claimed migrated.
- New master-listed startup/navigation documents use `recovery.tiles`; startup is performed by native `WorldSetupModule`, not test-owned spawning logic. The fixture starts the two depots and cranes needed by these cases, without unrelated markets/fleets/factories.
- Existing test authoring supplies native worker-execution components and unit metadata. Runtime rate/availability, motion, work and dispatch remain engine responsibilities.
- Cargo depot privacy now edits the parsed JSON property. The obsolete whitespace-sensitive replacement did not reliably select private stock.
- The test consumes the actual ready `WorkerDispatchOffer` from `CopyOffers`. It no longer reconstructs a stale private key format. A later full destination claim blocks that captured offer without changing jobs, cargo, history or unrelated receipts; releasing the actual claim admits the same identity, including after load.

## Source and behavioral audit

`InventoryWorkEffectContracts.Check` and `Commit` delegate to the original `IResourceTransaction.EvaluateInventory` / `CommitInventory` with the actual target, authored whole bill, lifecycle kind, cause and owner. No disposition ledger/coordinator is introduced.

`WorkBoard` productive commitment execution consumes repair inputs only on productive progress. Posting/renewing an episode before a machine starts work consumes nothing. The cancelled first episode therefore cannot consume a third repair input. Two earned repair inputs plus two destroyed steel bills leave 34 of the original 40 steel. The private journal independently proves consumed2 / destroyed4 steel and lost1 water; actual water stock ends4.

Private effect/cargo diagnostics currently return `NotFound` through the original disclosure rules, not `NotOwned`, preventing foreign identity disclosure. Assertions were migrated to those actual semantics; engine privacy was not relaxed.

Native cargo handling transfers one water unit per authored minute, then the exact half-unit remainder. The history contains six physical transfer facts, not a fictional single instant unload. Each fact now verifies resource, actual source/destination, exact quantum, before/after stock chain and cumulative native raw total5.5. Travel and interruption publish zero transfer facts. Steel3 and its unrelated owner's exact receipt survive every stage.

The cargo case additionally checkpoints after the first productive unloading quantum: cargo4.5, receiver1, original residual outbound/inbound receipts4.5. A fresh load preserves exact original tokens and productive timing, then finishes identically without duplication. Previous pre-acquisition, moving and interrupted checkpoints remain.

Named independent incident providers retain separate board-owned responses/episodes, stale-episode refusal, real travel and stages, idempotent posting, native effects, saved continuation, invalid authored bill/reference refusals and a partial-removal provider fault that prevents healthy hash/save. The existing tests directly instrument world/worker/claim/provider state in places: these are **protocol/provider tests, not thin public-player acceptance proof**.

Every physical wait in these two cases is bounded by its existing update ceiling (600 or300) and a20-second elapsed deadline. Timeout diagnostics contain simulation update, elapsed time and actual worker/state/job/motion facts. The bounded child process remains60seconds with5second termination wait.

## All observed attempts

Commands use the affected test project build with `--no-restore -v minimal`. Every runtime is `--bounded-exact` with timeout60000, termination5000, output4000. All outputs below were untruncated.

| Attempt | Resource response literal | Cargo literal | Evidence / next action |
|---|---|---|---|
| Original baseline before this continuation |0/1,146ms, pid33772, exit1, outer290ms|0/1,109ms, pid17572, exit1, outer259ms|Inactive Tycoon supplier in resource-only composition; no play began.|
| First explicit master, build6.34s clean |0/1,53ms, pid33876, exit1, outer191ms|0/1,53ms, pid22960, exit1, outer194ms|Master omitted installed/documentless modules. Added explicit declarations; no validator relaxation.|
| Complete installed declarations, build7.23s clean |0/1,202ms, pid34688, exit1, outer342ms|0/1,204ms, pid21312, exit1, outer351ms|Fixture selected a line-placement policy as a construction-placement profile. Removed unnecessary construction-rule activation; these cases perform no placement work.|
| Physical composition loads, build5.07s clean |88/4,1089ms, pid34768, exit1, outer1232ms|14/4,420ms, pid31416, exit1, outer559ms|Actual productive material timing, private NotFound and changed dispatch key exposed stale assertions. Cargo aborted before later checks on missing job.|
| Productive/offer assertions corrected, build4.93s clean |**93/0**,1382ms, pid15860, exit0, outer1554ms|51/2,719ms, pid31568, exit1, outer901ms|Response final; cargo history correctly contains six timed unloads instead of one.|
| Partial-unload assertions added |Not rerun|No runtime|Build failed4.10s: test used old CheckReservedIn signature and nullable access on nonnullable destination facts. Three compile errors, no production failure.|
| Test API usage corrected, build4.72s clean |Unchanged response code; not redundantly rerun|**111/0**,551ms, pid23588, exit0, outer688ms|Cargo final: partial-save/original tokens and every actual unload fact verified.|

No executable changes after the final cargo result. Response factory/helper/body were unchanged between its final run and that result; only Cargo assertions were subsequently expanded. No family/profile/full runtime occurred.

## Remaining work

P7.017 remains open: complete conversion/spill/topology accounting and broader provider-fault/stress evidence through the existing owners, and reconcile remaining incident/cargo consumers. Other old recovery-content callers have not been repaired or run here. Optional quality/lot proof remains separately unselected. No staging, commit or push before the full goal and plan are complete.
