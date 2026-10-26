# P11-C8 private conditional plan sites — focused evidence

Verified2026-10-08. Build2.294.0 / Save143 / Replay1 / playbook-state5 / controller-view7 / work-view83.
[Design](../../plans/enhancements/P11_CONDITIONAL_PLAN_SITES_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md),
[source manifest](P11_CONDITIONAL_PLAN_SITES_SOURCE_MANIFEST.json).
Full goal ACTIVE; no staging/commit/push before full goal AND plan finish and verify.

## Engine ownership

Original AuthoredPlanSitesDef reads explicit candidate conditions, validates canonical distinct IDs,
resolves registered IGameCondition providers and validates bindings under original purity guards.
Existing IPlanSiteSource signature now receives same-view guarded condition observation; external
source wrappers forward it. PlanSiteContracts alone supplies GameConditionContracts.Read with
actual world/content/visibility/provider guards. No host decides whether a candidate is safe.

PlanSiteCandidate exposes detached immutable geometry, stable priority/ID and typed Blocker only,
not condition providers. PlaybookSiteSnapshot combines policy and actual PlanGroupCheck blockers.
Original playbook chooses only policy-ready AND physically-ready candidates; blocked alternatives
remain visible in private diagnostics, with deterministic priority/ID ordering and no default site.
All conditions per candidate are required; reference order canonicalized by stable identity.
Empty arrays are deliberate unconditional policy, not an implicit fallback.

Policy chooses new intent; it does not revoke queued normal player commands or cancel active work.
Normal board preview/admission remains the authority for placement/resources. No new condition
scheduler/store, clock, reservation, budget requirement or mutable execution model.
Persistent schemas unchanged; existing build/view identities reject old peers/content as appropriate.
Five previous site documents gain explicit empty conditions;159 old identity paths, wrapper and
constructor callers migrate compile-only. One new explicit master-selected conditional proof JSON.
Existing non-friend signal/clock providers are engine modules, not fixture-side progression.

## Every build/runtime attempt

Build command: `dotnet build Beep.SimEngine.slnx`.

| Build | Result | Observation |
| --- | --- | --- |
| 1 | Clean13.03s,0 warnings/errors,exit0 | Live session60959,one completion poll; no restart |

ONE runtime invocation of ONE exact NEW bounded case:

```powershell
dotnet run --project tests\Beep.SimEngine.Tests --no-build -- "Player controllers: private conditional sites and physical strategic selection"
```

| Runtime | Result | Observation |
| --- | --- | --- |
| 1 | 64 passed0 failed0 aborted1658ms,exit0 | First green; no compile/runtime repair or rerun |

No older/family/full/platform/stale runtime. Executable inputs frozen at
2026-10-08 11:32:20 UTC; documentation/read-only audit only thereafter.

## Scoped coverage

- High-priority site policy-blocked despite normal physically-ready admission.
- Equal-priority alternative chooses stable a-ID despite reverse JSON row order.
- Detached immutable retained site lists/anchors/blockers; reads leave hash unchanged.
- Same strategy profile sees each company's own operational signal; foreign readiness cannot unlock.
- Actual master-selected file identity and denial of another company's playbook diagnostics.
- One normal next-update intent, actual active project, physical workers/hauling/building.
- Player alternative24 and rival primary30 each consume8 of actual52 supply; two structures only.
- Save during queued command and physical work; twins and canonical replay match actual state/resources.
- All alternatives gated: retained typed wait, no plans/work/grants, saved native wait.
- Normal own signal command unlocks fresh engine selection, still physically supplied construction.
- Existing native inventory reserve threshold gates every site without economy/Tycoon.
- Missing/null/wrong-type/unknown/wrong-provider/duplicate/empty conditions fail before gameplay.
- Invalid public candidate blocker refuses; no extra policy state in save layout.

Physical waits <=2000 updates with actual clock/plan/site blockers. Driver submits ordinary commands
and reads engine evidence; it does not implement strategy, signal updates, placement or completion.

## Still open

Complete industrial risk/expansion/service/survey/reserve profiles, active-project recovery,
hidden-provider/fault/global-purity/CPU breadth, larger interruption/corruption/lockstep/group matrices,
performance/full accounting/Oil and authorized P6/full P1-P12 acceptance remain open.
This is resource-only strategic selection, not optional-money proof or full regression.
C7 and earlier runtime evidence are historical and were not rerun on2.294.0.

## Read-only handoff audit

1788 repository-visible/prior-referenced paths;584 Markdown documents;3536 local links.
The linked self-excluded manifest resolves the only pending link;18 five-column phase rows valid.
Actual git diff --check exit0 with31 existing line-ending warnings.358 prior engine hashes compared:
four changed, no new engine file.326 existing authored src/tests JSON compared:five explicit-array
migrations, one new conditional workflow JSON. Sole GameClock unchanged; final all-file hashes audited.
No executable input/build/runtime after freeze, staging/commit/push.
