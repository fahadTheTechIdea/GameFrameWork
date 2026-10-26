# P9-B11.3c2b2 authored service movement windows — verification

Date: 2026-10-07. Dirty working-tree checkpoint, not clean HEAD attribution or full regression.
Build2.194.0/save104/board36/work35/logistics12/work view68/logistics view23/native recurring8.
Public recurring schemas5,5,6 and all owner payload layouts remain unchanged.
[Design](../../plans/enhancements/P9_SERVICE_MOVEMENT_WINDOW_DESIGN.md).

## Ownership and implementation

IHaulTripProfile exposes immutable ServiceSources. Every native profile explicitly authors a JSON
service_sources array, including empty arrays for no prospective service requirement. A referenced
capability must be this carrier's actual worker-bound scheduled recurring source. Missing arrays,
duplicates, unknown sources, templates, unbound activities and condition-only sources are refused
by existing composition/configuration validation; no fallback, auto-discovery or default source.
Existing HaulTripSettings persists only its already selected profile/enabled/reserve settings.
No source list, service deadline or elapsed service age is copied into another saved owner.

RecurringPlanContracts.ResolveCadence is the single actual chronology calculation extracted from
the existing recurring view. Both Read and CheckMovementWindow read real source timestamps, native
board cycle members, actual successful completion and pending/retry/outstanding semantics through
that calculation. They never invoke a second scheduler or store success chronology elsewhere.
The movement window reads the sole GameClock's current update coordinate. Unknown completion is
not posting time; pending acknowledgement stays a typed refusal. Oversized movement compares
against remaining time without adding it to current age or narrowing into fixed point.

Existing HaulTripPolicyContracts.CheckDispatch governs fresh selected movement only. The ordinary
LogisticsService haul/return selector calls it after existing physical movement funding and before
claims/interruption. It selects another eligible carrier where possible. Disabled future service
schedules impose no future window; admitted service-first work still uses its existing independent
dispatch restriction. Eligible emergency admission bypasses only this prospective window, never
the earlier physical fuel, worker policy, safety, area, stock or emergency-eligibility checks.

Retained cargo reads its existing physical trip budget, not fresh service-window admission. A new
service deadline cannot deadlock cargo behind maintenance needing the same worker. Actual jobs,
claims, unloading, departure and later service continue through their existing physical owners.
LogisticsView.ReadHaulTripServiceSources returns detached immutable private references; changing
the actual selected profile cannot mutate an earlier view. Foreign reads expose no references.

LogisticsService schema12 and LogisticsView schema23 capture current admission/view semantics;
saved payload layouts are unchanged. Board36/work35/work view68/native recurring8 remain unchanged.
Current identity migrates directly across81 literal runtime/test build paths, including functional
overlap. Six logistics schema assertions migrate to the actual current semantic version12;
those separate suites compile only. There is no old reader, compatibility facade or new clock/
state/reservation authority.

## Focused final gate

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Completion service: authored cadence, private admission and retained success"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Completion service: timed work, materials, restoration and saved continuation"
```

Final build: zero warnings/errors. Admission248 + physical236 =484 passed, zero failures or aborted
suites. Only these same two exact suites ran. Existing completion/trigger/dispatch callbacks remain
inside their focused service suites. Other suites compile only; no full/family/third gate ran.
Physical waits are bounded at400 normal updates; retained grouped proofs at480.

Native and independent public source/worker providers cover missing/duplicate/unknown references,
real selected-machine binding, outstanding service's unknown deadline, actual completed-cycle
window, zero/short/equal/long movement, huge duration, emergency-only exception, unknown route,
negative duration, company-private settings/references, detached views, unchanged hashes and
saved source/whole-cycle chronology. Ordinary profile changes explicitly remove and restore
service linkage while the source stays enabled; disabling its schedule removes future demand.

Fresh integration explicitly proves an idle machine, no next admitted service and the exact
work.recurring-movement-window refusal. Preview and real acquisition leave hashes and claims
unchanged. A normal work-policy command enables another actual machine; the same selector chooses
it rather than granting supplies, ignoring a deadline or manufacturing a carrier.

The loaded case begins after physically earned service, previews a valid real movement window,
and requests stock with a normal player command. The selected machine earns real cargo. A normal
service-interval command then makes maintenance due without cancelling the shipment. Save/load
preserves the actual loaded state; delivery reaches the real destination (four units), followed
by whole timed service. Saved twin hashes agree. No gameplay decision/rule lives in the test host.

## Limits and remaining acceptance

This is a movement-only window, not predicted trip completion. Uncertain queue/handling delays,
future condition wear, road changes and traffic are not fabricated. Actual physical gates remain
authoritative. Multi-source service fairness, new early-condition/window combinations, complete
fleet readiness, failures/queue/provider stress, stranded propulsion rescue, global purity,
CPU/allocations, replay and full acceptance remain open. Source reference validation has native
code coverage beyond the focused invalid-fixture cases; do not claim every invalid shape was run.

GameClock.cs remains unchanged, SHA256:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
World constructs it once; SimGame reuses it; World.Tick only projects Clock.StepIndex. No new time
owner was introduced. All movement/service chronology remains in that one game-time coordinate.

The lifecycle skill kept staged implementation, scoped evidence and master tracker aligned.
Literal fingerprints appear in [the manifest](P9_SERVICE_MOVEMENT_WINDOW_SOURCE_MANIFEST.json).
Older reports/manifests remain historical checkpoints. Existing dirty work is preserved.

B11.3c2b2 is focused verified; B11.3c2b/B11.4 and the full roadmap are not complete. No Oil/Tycoon/
host/demo/platform/Draw changes or full-regression claim.
