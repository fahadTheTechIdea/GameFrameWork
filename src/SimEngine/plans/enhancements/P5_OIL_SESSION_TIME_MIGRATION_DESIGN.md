# P5-A2: Oil session-time contract migration — Design Document

## 0. Problem Statement

Oil still overrides removed Schedule properties and uses fixed GameTime tick constants. Its timestamps and fog invalidation cursors are int despite the framework's long clock. These prevent correct integration with minute/half-minute session time. P5-A1 left 35 compile errors; physical/runtime acceptance remains incomplete.

## 1. Goals

1. All oil schedules use GameSystem.GetSchedule(ISimulationTime) and the sole session converter.
2. Rates, quiet periods, decision periods and daily settlement derive from declared duration units/session spans, not old fixed tick constants.
3. Persist/project actual game-clock timestamps as long with explicit oil codec/view versions.
4. Preserve the existing framework clock/converter/random/state owners and introduce no legacy time wrapper.

## 2. Design

Use existing Schedule.Every(time, duration, converted phase) for hourly/daily systems. Extraction and wear now use periodic whole-component hourly visits instead of old fixed-count bucket arrays; the framework owns when those visits occur. Extraction/research per-day rates use ISimulationTime.RateFor with actual TickInfo.Span. Wear retains authored mean-time days as GameDuration and derives the whole hourly draw interval through the actual session converter and random stream, without an int-size clamp. Autopilot period/seat offsets, commerce day settlement and storyteller quiet interval use the same converter.

Company outcome, objective-met, incident-raised and storyteller-last-incident timestamps and fog revision timestamps become long. Persisted oil books/campaign/storyteller and wear component codecs advance to version2; company view timestamp projection advances to2. No compatibility loader is introduced. Campaign/storyteller age is derived from the sole clock and exact day duration; an optional presentation calendar is not an artificial gameplay prerequisite.

This migrates existing cadence semantics; authored cadence selection/content composition and replacement of duplicated generic research/campaign/business execution remain open integration tasks. A signature migration is not proof of elapsed extraction/physical work/provider acceptance.

## 3. API surface

Current GetSchedule contract replaces obsolete property overrides. Existing ObjectiveView.MetTick becomes long. Native framework build/protocol/clock are unchanged. No draw/platform API.

## 4. Files to change

OilCo.Game Operations, Research, Campaign, Storyteller, Autopilot, Company, Fog, Views and component/view registration in OilModule; canonical stage/evidence/master/audit.

## 5. Error handling matrix

Unrepresentable exact schedule duration/phase refuses through the current converter; no rounded fake period. Elapsed age does not substitute a default or wrap with presentation calendar fields. Invalid mean-time duration refuses authoring. Persisted timestamp versions must match actual layouts. Remaining compile failures preclude oil runtime claims.

## 6. Backward compatibility

Development only; no old time constants, truncated timestamp aliases or compatibility codec.

## 7. Verification

Capture current source, migrate completed slice, focused OilCo.Game Release compile and old-time-reference search. No executor while other contracts prevent assembly compilation; original time/physical/save gates remain open. Full regression deferred until implementation finishes.

## 8. Risks

Periodic whole-component hourly visits change old spread visitation chronology; this is explicit, not legacy parity. First-visit/rate earning and complete domain replacement require later physical verification. Calendar naming and scenario time content must be authored before runtime. No reduced compiler count implies success.

## 9. Out of scope

Generic engine clock/range changes, full authored cadence/content/domain migration, oil gameplay acceptance, hosts/tests/platforms/Tycoon changes and Git writes.

## 10. Sub-task execution order

1. Migrate all current oil schedule/time references and timestamps/codecs together.
2. Compile and preserve remaining errors; run no unavailable runtime.
3. Update tracker/evidence/source freeze, keeping original gates open.

## Partial implementation outcome

Nine oil sources migrated, including all seven obsolete schedule properties, rate/period conversions and long timestamps/codecs. Remaining compiler errors:35 ->28; observed Release builds exit1/zero warnings in1.09s/5.08s/1.55s. Final elapsed-age projection does not require a presentation calendar. No test executor or oil runtime; framework source/time/clock unchanged. Source search finds no old GameTime static calls, Schedule-property overrides or int timestamp fields. Cadence/content composition, first-visit earning and full physical/durability acceptance remain open. See [verification evidence](../../docs/migration/P5_OIL_SESSION_TIME_MIGRATION_VERIFICATION.md).
