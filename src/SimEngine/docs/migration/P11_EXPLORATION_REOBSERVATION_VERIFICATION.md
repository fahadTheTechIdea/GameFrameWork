# P11-RE2 — Timed exploration reobservation verification

Date: 2026-10-15. Scoped gameplay implementation; full goal/P11 not complete.

## Implemented behavior and ownership

Original AuthoredExploration now admits permanent first-learned subjects or latest
subjects with an explicitly finite authored freshness lifetime. One helper in its
existing contracts owns this automatic-subject policy. Latest with null freshness
remains valid for explicit/manual knowledge operations, not automatic exploration;
there is no default polling interval or compatibility path.

The same component reads the existing IKnowledgeFreshness service. Its immutable
ExplorationEntrySnapshot now carries the original KnowledgeFreshnessSnapshot:
Known derives from historical membership; NeedsObservation derives from absent or
stale history. Those facts are not another persistent table. Current, original
AdvanceExploration and original entity GetState all use that same distinction.

Fresh observations earn no additional dwell or new timestamp. Expired observations
remain visible as historical data while the original requirement/target checks and
full authored dwell execute again. Original reset/retain interruption still applies.
Only successful original subject observation plus original ledger.Record publishes
the replacement. LearnedTick remains original, ObservedTick is earned at completion.
Sharing and forgetting still use the original ledger/policies/timestamps.

No CompanyKnowledgeService implementation, saved knowledge/progress layout, work
scheduler, resource store, business rule or clock was changed. ExplorationModule
requires its original freshness capability and publishes richer ExplorationView
feature schema2. Engine build2.634.0, save159/replay1. The old bool constructor is
removed; no compatibility overload or parallel source exists.

## Authored proof

`exploration-reobservation.json` is an explicit master-listed document containing
two existing point-reading contracts with latest retention and ten-minute freshness.
It replaces the fixture's permanent fact definitions, not the engine's data model.
Existing independent/native profile aliases,12/15-minute entry durations, contact
requirement, sharing/forgetting policies, physical workers/routes and point layers
are reused. No new positive gameplay implementation lives in the test driver.

The existing independent PointReadingDef has a transient negative Observe hook. It
is null in positive cases, targeted to only its designated world and cleared in a
finally block; it owns no samples, history, progress or clock. Shared frozen content
does not acquire positive per-session mutable state.

## Verification — every observed build/run

Exactly one targeted build, zero warnings/errors,6.77s:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
```

Each runtime command used one literal with `--bounded-exact`, timeout60000ms,
termination timeout5000ms and output6500 characters. All returned terminal Exited,
exit0, no truncation, abort, timeout or lost observation. No repair/rebuild/rerun was
needed and no executable source changed after these runs.

| Exact case | Passed / failed | Child PID | Suite / process ms |
|---|---:|---:|---:|
| Exploration reobservation: expired history earns replacement dwell without premature timestamps |434 /0|25688|5691 /5828|
| Contact exploration: physical regional discovery and saved sensor dwell |117 /0|19192|914 /1053|
| Region exploration: authored requirements earn whole remote shapes with private saved dwell |239 /0|28636|3279 /3416|
| Knowledge freshness: physical survey expiry and exact saved visibility |84 /0|17132|1307 /1446|

Final focused total: **874 checks passed, zero failed in four exact cases**. These
are affected behavior checks, not a full family/profile/solution regression.

### New case scope

Four native/independent metadata × minute/half-minute physical workflows verify:

- Real single-unit sensor supply and physical travel to the observed field point.
- Initial full contact dwell, original saved twins and no money/Tycoon dependency.
- Fresh history waits; repeated facts/views/preview do not mutate age or timestamps.
- An actual authorized engine-side layer-write command changes terrain; history
  remains the old value until full replacement work completes.
- Expiry leaves Known=true, NeedsObservation=true and original stale data; disabled
  coverage blocks work without losing history or earning a progress prefix.
- Sole-clock pause freezes knowledge age and replacement progress.
- Partial replacement, reset/retain shutdown and save during the last incomplete
  interval preserve original timestamp and unfinished work.
- Completed replacement samples the changed value9 and retains original LearnedTick.
- Ordinary sharing retains observation time (not receipt time); forgetting affects
  only the issuing company; re-earning after forgetting establishes new learned time.
- Actual source/sensor quantities remain19/1; old immutable snapshots stay detached;
  isolated sessions and complete public command replay agree.

Four negative contexts (fresh/restored × throwing/silent clock-mutation callbacks)
reach the real final subject Observe call during replacement. They verify original
observation fault phase/first error, stopped clock/publication, poisoned save/hash
rejection, unchanged last-safe historical snapshot and healthy completion in the
independent saved session. The faulted ledger's public queries correctly remain
unavailable: this is **not** a claim to have read its private live record after fault
or to roll back the invalid clock mutation. Source ordering keeps Record after the
failed callback; no fallback observation or compensating ledger is added.

The selected existing cases prove permanent contact/remote discovery still stops
after learning, native geometry/full-region coverage still works, non-finite latest
automatic profiles still refuse, and explicit physical survey/fresh visibility/manual
refresh behavior remains available through the unchanged knowledge service.

## Current identities and remaining work

Sorted path/content SHA256 over443 core C# files:
`918a67ab00ad43651ddd6610a3a5ee70bdc6ab388153745321dbc319b24221c3`.
Debug framework DLL:
`16144172D0A3D70F7AAAF601772EF644B3EFB40CE1DEDBFAE876DE4B959678C8`.
Debug test DLL:
`07605ED5551FB38C34859817AC86C3F603701A5C1378ED14F9270E3FBCF27C6C`.
New test source:
`63810AEEED17881F2E7809299DBAA1102D54959E981C6996F06E35E725F7FBF0`.
New authored reading document:
`A4E6B600B029963AB3864B28A813255311840AFFA6FE77AF32B6FA4DDEBFE1CD`.

Current original-ID/literal-requirement mapping remains221 total/208 recorded
checked/13 open. No original broad gate closes here; historical checked gates are
not globally revalidated. P11.026/.029/.035/full P11/full goal remain open, including
arbitrary/custom freshness and requirement providers, remote/cell-wide bulk,
occlusion/utility combinations, wider fault/corruption/lockstep/stress/performance
and every optional module's isolation matrix.

No Oil/Tycoon/platform/Draw/demo gameplay changes, full/family/profile tests, memory
writes, staging, commit or push. Publication still waits for the actual full goal
and plan completion.

Final hygiene: explicit stage-source/document whitespace check clean; `git diff
--check` exit0 with35 existing line-ending warnings/no other diagnostics. Exactly one
new direct test registration and one authored embedded-resource link; staged paths0.
Final read-only process inspection found no live engine test/build process.
