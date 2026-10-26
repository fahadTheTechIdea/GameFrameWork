# P5-A45: Public game-policy suite migration

## 0. Problem

OilCo.Tests/GameTests.cs still uses removed static time, tile projection, resource, route, construction, financial and private constant APIs. Its waits assume obsolete work-list removal and false-predicate RunUntil behavior.

## 1. Goals

Migrate the original eight cases once to the existing non-friend public runner. Preserve original authored scenarios and long durations. Verify short retirement and charge-once research selection through public commands, facts and saves. Keep all gameplay in engine assemblies.

## 2. Design

Use existing ClientTests.Factory for canonical disk content, not another loader/cache. Replace static time with session SimulationTime, EntityView.Tile with TileSpatialSnapshot, map reads with TileMapView, construction with TilePlacement intent, and routes with ResourceId/named ports/LogisticsRouteSettings.

Use actual BusinessView accounts/sales and explicit refusal on unavailable monetary facts. Use Oil exploration SurveyConfidence instead of the removed knowledge map facade; actual company-private ledger remains authoritative. No unknown coverage is fabricated.

Order-state waits consider active versus retained terminal orders. Explicit duration advancement uses RunTicks, not a false completion predicate. Missing suggestions/metadata/results fail explicitly; long scenarios retain native waits and process-isolation requirements.

Move the already-public BudgetTests source alongside GameTests so it reuses the same canonical factory. Its long performance case is compile-only, not executed. Mechanically change the two remaining offshore fixture calls to the existing OilCoGame factory/content entry point after removing their dependency on GameTests; no compatibility facade is retained. Remaining OffshoreTests migration remains open.

## 3. API/state

No production API, schema, financial amount, clock or controller changes. Core2.557.0/save156/replay1 unchanged. Public typed refusal codes replace references to private classes, not restored compatibility aliases.

## 4. Files

Move GameTests.cs and BudgetTests.cs to OilCo.PublicProof; old/public registration changes. Current stage/evidence/master/audit update.

## 5. Errors

Unavailable owned accounts/sales, missing founding/build suggestions, missing physical outputs and unexpected waits fail rather than invent defaults. Canonical long workflows remain bounded in simulation time and require external process bounds when executed.

## 6. Compatibility

No private friendship, cash ledger fallback, static clock, duplicate engine state or legacy World escape.

## 7. Verification

Build public dependency graph; exact retirement and research-switch cases only. Compile diagnostic of remaining old project. No full, performance, all-scenarios, physical survey/drill/sales, privacy or long rival runs.

## 8. Risks

Long authored campaigns, retained-order behavior across all workflows, offshore migration, physical survey privacy and performance remain unverified by these short cases.

## 9. Out of scope

Production gameplay/finance changes, platforms/Draw, broad tests and Git writes.

## 10. Status

Implemented; canonical focused20/0, public build2.05s clean; remaining old compile54 errors/OffshoreTests only. Original long/performance cases compile-only, full goal open; original221/194 recorded checked/27open unchanged. [Evidence](../../docs/migration/P5_PUBLIC_GAME_POLICY_MIGRATION_VERIFICATION.md).

