# P4-I5: Resource-only generic colony composition — Design Document

Status: focused implementation verified,2026-10-08. Full P1-P12 goal active.
[Evidence](../../docs/migration/P4_GENERIC_COLONY_COMPOSITION_VERIFICATION.md).

## 0. Problem statement

Existing generic Colony.Factory selects colony-work.json with native Tycoon-owned monetary
requirements but no Tycoon declaration. Current strict loader correctly refuses initialization.
ColonyModule.Compose also unconditionally binds TestLedger with10000 cash and ColonyStart
creates a priced native market. Generic machine/work games must not require those economics.
This is an authoritative generic engine-extension proof migration, not loosening the loader.

## 1. Goals

1. Default Colony.Factory initializes with no Tycoon selection/account/business view/market.
2. Authored tower requirements retain30 real steel and positive staged machine work without cash.
3. Normal commands physically haul, sequentially manufacture, construct, wait on resource shortage,
   demolish/recover and manufacture again using recovered resources, entirely through engine owners.
4. Public-player save/load twins continue during physical/shortage/recovery stages identically.
5. No fabricated stock/money, host workflow, duplicate scheduler, default free economy or compatibility.

## 2. Design

Original ColonyModule/ColonyStart remain existing engine extension assembly owners; delete their
default cash binding and native market startup. Delete native market prototype from colony.json.
Delete work_cost_requirements and cost member from original colony-work.json; existing all-group
retains actual physical material entry. Native strict module ownership/refusal remains unchanged.
Standalone TestLedger remains only explicit older monetary fault instrumentation; it is NOT
enrolled by the default colony, not a native monetary provider or survival fallback.

Existing core WorkBoard/LogisticsService/ResourceTransactions/TransformerManager/ConstructionService
own all assignment, receipts, movement, progress, manufacturing, shortage, demolition/salvage/recovery.
No new core gameplay handler/provider needed. One NEW test drives normal commands/public detached
views/snapshots/clock/save/hash only; module/document assembly is original Factory.
Two initial manufacturing outputs consume20steel, tower consumes30; a third waits without suppliers.
Normal demolition creates explicit15steel salvage via existing physical owner; third consumes10,
leaving5. Expected assertions describe authored content, never mutate gameplay.

## 3. API surface

No new public gameplay API. Default public proof Colony.Factory now genuinely resource-only.
Build identity advances; persistent layouts/save140/GameClock unchanged. Explicit Tycoon games
continue selecting existing sim.tycoon owner and own files; no changes to Tycoon implementations.

## 4. Files to change

Existing tests/ExtensionProof/ColonyGame.cs, content/colony.json and colony-work.json.
New tests/Beep.SimEngine.Tests/GenericColonyCompositionTests.cs and Program registration.
EngineInfo build identity/older strict identity fixtures mechanical compile-only migration.
Stage/evidence/source manifest/README/AGENTS/master/related P4 modes and P6 docs.

## 5. Error handling matrix

Undeclared monetary section/native supplier → original strict ContentException, never zero prices.
Real missing materials → original demand/production wait; no buyer/seller/free resource fallback.
Native foreign commands → original ownership refusal.
Physical stage timeout → explicit public clock/entity/order/supply diagnostics; bounded runtime.

## 6. Backward compatibility

Development only; direct removal of generic economic defaults. No old save/content compatibility.
Older monetary probes must explicitly author/select appropriate module/account in their own later
migration; generic colony does not keep a hidden ledger or silently activate Tycoon for them.

## 7. Verification

ONE distinct exact NEW bounded case. Completed-slice --no-restore solution build, record EVERY
attempt; same-case repair/rerun ONLY after observed failure. No old/full/family/platform/stale run.
Player-only scenarios include sequential manufacture, physical construction/material consumption,
shortage with no fabrication, demolition/recovered supply, private refusals, actual elapsed time,
positive work/movement, partial/shortage/recovery saved twins and interleaved sessions/pause.
Max2000 Update per stage with diagnostics. No CPU/allocation/full industrial-profile claim.
Freeze gameplay/public proof/tests/JSON immediately after FINAL green, documents-only afterward.
Actual ONE clean build10.86s0 warnings/errors; ONE exact new bounded case FIRST runtime attempt
45 checks0 failures/aborts2419ms. Source frozen. Older identities138 compile-only; no older suite.

## 8. Risks

Older generic paid/provider tests relied on hidden cash and may require explicit authored economics;
not runtime-verified here. Public proof default migration changes old save content identity directly.
Standalone older fault TestLedger is explicit test instrumentation, not default game support.
Original hardcoded colony terrain/navigation/world-generation sample remains a later JSON authoring
migration; this stage does not claim the entire example is fully file-authored.

## 9. Out of scope

Tycoon business source/advanced behavior, Oil/sibling/hosts/demos/Draw/platform edits.
Global old-fixture migration, mandatory game-mode profiles, world setup/navigation sample authoring,
all baseline regressions/atomic teams/provider/fault/replay/lockstep/performance/stress/full P1-P12.
Commit/push only after FULL goal and plan verified complete.

## 10. Sub-task execution order

1. Delete generic cash/market/monetary work defaults using existing authored content owners.
2. New normal public-player sequential physical build/shortage/recovery/saved execution proof.
3. Build/exact case, repair only observed same-case failures, freeze after green.
4. Evidence/current source inventory/tracker and all attempts, retain full goal active.

