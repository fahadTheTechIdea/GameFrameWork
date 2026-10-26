# P5-A39 public test boundary verification

Core2.555.0/save156/replay1. Framework external OilCo.Tests friendship and OilCo.Game's test friendship removed. Native power/fuel behavior and schema unchanged. Canonical master/definition register the existing optional WorldSetup owner; normal scenarios do not select it by default.

## Builds/attempts retained

Before migration, targeted old OilCo.Tests build failed with131 reported errors,0 warnings,4.00s. Its tool output was truncated; no full diagnostic or runtime success is inferred.

Current public proof migration compiles:3.42s failed (proof used nonexistent Fix.Decimal);1.92s failed (local JSON variable shadowing);8.92s clean after actual API/single reader correction. No stale executable ran after compile failures.

First migrated exact runtime: TimedOut at120079ms,PID40520,exit-1,untruncated. No completed check count was returned; this attempt is retained as aborted, not a pass. The terminal handle was observed before retry.

After removing the remaining game friendship and adding diagnostics, final public dependency build clean,0 warnings/errors,3.62s. FINAL migrated exact `Oil operations: actual generator facts are not fuel-stock guesses`:144 passed/0 failed in126342ms,observer126539ms,PID8032,exit0,untruncated.420000ms process bound,5000ms termination,30000characters. Checkpoints show native time6 productive startup, then7/37/67/97/127 during original scheduled depletion.

No executable edits after FINAL. One migrated exact case only; no old/family/full runtime. One earlier aborted attempt does NOT satisfy a zero-abort full profile.

Current old test project diagnostic build failed:133 unique source-location errors in5.25s;0 runtime suites. Budget4,Client3,Demo1,Game37,Input3,Net15,Offshore40,OilResponse13,OptionalModule17. Remaining obsolete clock/spatial/command APIs and private/world callers must migrate; the canonical full solution/test profile is NOT green.

## Preserved/migrated suite

The existing OilOperationTests.cs and case name moved from OilCo.Tests to OilCo.PublicProof. Old registration removed; exactly one current registration. No duplicate suite/source or excluded failing case substitutes for migration.

Fixture gas_generator is created by native WorldSetup from actual master-listed JSON, including explicit placement and0.8 initial natural_gas. No raw spawn, entity flush, live component, inventory Add/Take or mutable Game.World in the driver. It reads public operation, signal and named stock facts.

Native hourly power scheduling consumes its unchanged0.4 bill. Actual generation/output is observed after the scheduled update; fuel naturally exhausts, and a later scheduled refusal reports zero output. Earlier detached zero readings remain unchanged. Public operation/signal reads are pure and foreign facts stay unavailable. Saved interleaved sessions agree at every observed update. Reflection verifies no Oil test friendship on framework/game and no public World property.

The framework's own Beep.SimEngine.Tests white-box instrumentation remains permitted; no external Oil friend is restored. Power rates, finances, generic stock/init/state/clock owners and host logic are unchanged.

## Remaining scope

No old sibling OilCoEngine/sim/adapter mutation. SimEngine.slnx now includes the actual public proof project and the existing external runtime. Its old test project still fails, so P5.001/P5.002/P6 profiles/global client migration and all broad gates remain open.

No full legacy suite migration, complete user-driver audit, all incident/offshore/network/optional-module scenarios, full save/lockstep/replay/soak/performance or full industrial acceptance is claimed.

## Tracker

Original221 /194 recorded checked /27 open unchanged; additional304. No original gate closed or reworded. Full goal/plan incomplete; no staging/commit/push. [Stage](../../plans/enhancements/P5_PUBLIC_TEST_BOUNDARY_DESIGN.md).

