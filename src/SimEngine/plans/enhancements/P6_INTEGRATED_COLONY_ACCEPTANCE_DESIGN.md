# P6 — Integrated resource-only colony acceptance

## 0. Problem statement
The existing GenericColonyCompositionTests proves physical manufacturing, construction and recovery.
IncidentPhaseTests installs its target through internal live-world access and tests monetary work.
These isolated proofs do not establish that an authored resource-only colony can manufacture,
construct, survive engine-generated pressure and settle an objective chain through public inputs.

## 1. Goals
One authored master and scenario reuse existing colony prototypes, startup and resource-only mode.
Native pressure creates a finite emergency; native phased DAG consumes materials/holds tools and
waits for a player-controlled cleanup permit. Native objectives alone decide completion/victory.
The player driver uses commands, detached views, GameClock and Update only, with no World access.
Save/load/replay/isolation and exact terminal resource balances cover the integrated workflow.

## 2. Design
Reuse Colony.Factory with an optional explicitly authored master document, not another composer.
The new master lists all actual code modules and shared documents; only conditions/pressure are
new selected modules. Add acceptance definitions under the existing colony content owner.
JSON authors initial emergency assets, phased response, permit/material/tool requirements, pressure
budget, signals/conditions and objective dependencies/outcome. No mandatory business or market.
Extend the existing GenericColonyCompositionTests owner via a partial file and one new exact case.
Reuse its stock/production/travel observation helpers, not its older case. Each stage is bounded
by 2000 Updates and a30-second observation watchdog, with public work/objective/incident diagnostics.

## 3. API surface
No runtime API/schema changes are planned. Pure test-assembly Colony.Factory accepts masterDocument.
All gameplay uses existing engine contracts, native modules, JSON and ordinary commands/views.

## 4. Files to change
Existing ColonyGame.cs factory seam, GenericColonyCompositionTests.cs registration/partial modifier,
ExtensionProof.csproj explicit embedded assets; new authored master/scenario and partial test file.
No host/demo/platform/Oil/Tycoon behavior changes; no new runtime scheduler, state ledger or clock.

## 5. Error handling
Missing JSON dependencies/providers refuse composition. Shortages and blocked cleanup remain typed
engine blockers. Unmet bounded stages throw diagnostic failures, never mutate progress or grant stock.

## 6. Compatibility
Development only; shared current contracts/content, no shim, copied scheduler or legacy reader.

## 7. Verification
Build once after implementation. Run ONE distinct NEW exact bounded case; repair/rerun only observed
failures of that case. Record all attempts and freeze executable files after green. No broad runtime.

## 8. Risks
Pressure/replay, objective settling and cargo/resource owners must compose without privileged repair.
Standalone acceptance is not Oil migration, rival strategy, provider fault/soak or full regression.

## 9. Out of scope
Rendering/platforms, Oil/Tycoon business changes, full suites, premature publication and another game.

## 10. Execution order
Author master/scenario, extend original factory/test owner, compile, run exact case, record evidence
and update existing master rows. Full goal remains ACTIVE until every remaining requirement is proven.

## 11. Observed repair and verified outcome
First run:61 passed2 failed, no abort,4108ms. The entire physical workflow/replay succeeded,
but policy preview/command returned work.not-policy: shared crane authoring had no IWorkPolicy.
Repair adds the existing native work_policy component to the original crane prototype and one
explicit profile in the original colony-work.json. No copied worker, scheduler, money or fallback.
Default enabled/role/priority/interruptibility/emergency behavior is explicitly authored.
This shared content changes content identity automatically; no runtime build/schema bump.
Second run of the SAME exact case:63 passed0 failed/aborted3517ms.
Two clean builds4.24s and3.74s, zero warnings/errors. Source/proof/tests/JSON frozen after green at
2026-10-08 14:24:49 UTC. All subsequent work is documentation/read-only inspection.
