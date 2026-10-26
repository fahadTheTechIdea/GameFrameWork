# P5-A2 — Oil session-time contract migration evidence

Date: 2026-10-13. Framework core remains2.531.0/save154/replay1; no framework production source changed.

## Source implementation

All seven old Schedule-property overrides now implement GameSystem.GetSchedule(ISimulationTime), using the framework's existing exact-duration Schedule.Every conversion. Hourly/daily phases are converted from duration units rather than reused as fixed tick indexes. Extraction/wear now visit the complete component list once per session hour instead of allocating old fixed-period spread buckets. This explicitly changes spread visitation chronology; old chronology is not claimed compatible.

Extraction/research per-day quantities derive through the sole ISimulationTime.RateFor with actual TickInfo.Span. Autopilot decision hours and seat offsets, daily settlement and storyteller quiet duration use the same converter. Invalid decision hours refuse authoring instead of being silently clamped. Authored mean-time days retain their exact numeric literal as GameDuration; wear derives its whole-hour random draw interval through the session converter and existing NextULong stream, with no int-range clamp or new random/time owner.

Actual company-outcome/objective-met/incident-raised/storyteller-last-incident timestamps are long, with matching Long codecs. Company/campaign/storyteller service schemas, wear component registration and company view registration advance to2. ObjectiveView.MetTick and transient fog invalidation cursors are long. No timestamp alias, old codec fallback or new clock exists.

Campaign/storyteller elapsed day is derived from the sole clock divided by the converter's exact day duration, not a wrapping presentation-calendar value. No display calendar is required or synthesized. Existing cadence values still need authored configuration integration alongside the remaining scenario/time/content migration. First-visit earning and replacement of generic business/research/campaign execution are not established by this source work.

## Compile evidence

P5-A1's35-error current oil build was the starting evidence. Three completed scoped OilCo.Game Release --no-restore compile observations after time migration:

1. Exit1, zero warnings,28 errors,1.09s.
2. Decision-period validation compile: exit1, zero warnings,28 errors,5.08s. Its running process was polled through the same session handle to terminal completion, not restarted.
3. FINAL elapsed-age/calendar-independence compile: exit1, zero warnings,28 errors,1.55s.

```text
dotnet build OilCo.Game/OilCo.Game.csproj -c Release --no-restore
```

All three current outputs were retained untruncated. Final output has28 unique errors; no obsolete Schedule override remains. Other resource/work/visibility/automation/incident contracts still prevent assembly compilation, and later compiler passes may reveal further composition/caller issues. There is no positive oil build or runtime result.

Search across actual OilCo.Game sources finds no GameTime static member use, old Schedule property, MtbfHours field, int MetTick/OutcomeTick/FailedTick/LastIncidentTick, or Int codec for those timestamps. Oil diff-check exit0 (one existing line-ending warning), staged0. No test executor, full regression or profile runtime ran.

## Remaining goal scope

Complete resource/work contracts, author selected time/calendar/cadence/game-mode content, use authoritative shared production/business/progression owners, and migrate public oil clients. Then prove minute/half-minute physical drilling/hauling/production/repair, optional exploration, private knowledge, ordinary rivals and save/load/replay/lockstep. None of these original gates is closed by this migration.

Original221/193checked/28open criterion/status/line anchors unchanged. No Tycoon business, older sibling engine, platform/Draw, framework clock or oil host/demo/harness/test source change. No staging/commit/push. The single authorized final full regression is deferred until implementation finishes.

## Metadata/source-only checkpoint

1964 source anchors,1128 selected inputs and13 quickstart anchors match current bytes, with zero input drift after the final observed failing build. Nine previously frozen oil sources changed; all1113 framework inputs,12 Tycoon sources and sole clock remain unchanged. Two new documents only: no new executable class/file. 3097 canonical paths/1137 Markdown documents/8858 local links, no broken links; eighteen well-formed master rows. All221 current criterion/status/line checks pass unchanged, additional265.

Canonical diff-check exit0 with34 existing LF/CRLF warnings, dirty3085/staged0. Oil diff-check exit0/one line-ending warning/staged0; old time reference search zero. The metadata driver-boundary search is the prior framework driver, not an oil thin-client proof. One hash-refresh patch used header rather than anchor order; it was refused without edits and corrected. No positive build/runtime/full acceptance result is inferred.
