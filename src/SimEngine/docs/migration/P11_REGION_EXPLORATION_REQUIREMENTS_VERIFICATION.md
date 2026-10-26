# P11-RE1 — Authored region exploration verification

Date: 2026-10-15. Implemented gameplay slice, not full P11 or overall completion.

## What changed

The one existing exploration component is now `AuthoredExploration`, configured by
`IAuthoredExplorationDefinition` / `IAuthoredExplorationProfileDefinition`. Its
declaration/profile/validation helpers were renamed in place. The old reusable
ContactExploration class/contracts/component key/profile section are gone; this is
not a parallel component, migration shim or second exploration system.

Each entry now requires a resolved, actual frozen
`IExplorationRequirementDefinition`. Its selected requirement ID appears in the
immutable entry snapshot. The engine invokes the requirement inside its original
provider/whole-entity/space observation boundary and checks original world health
before admitting progress. Contact eligibility moved to the native contact
requirement. A native sensor-region requirement validates complete region geometry
and requires actual current sensor admission for **every** footprint. It does not
guess that a region center or a pair of endpoint samples proves complete coverage.

Only the original component owns unfinished progress and reset/retain interruption;
only the original company knowledge ledger owns completed facts. Existing
ExplorationSystem/order420, engine-issued update permission, entity state resolver,
subject admission/observation, share/forget commands, saves and sole clock remain.

Optional spatial contract `ISpatialObservationRegion` supplies complete detached
footprints. Native rectangles provide their full rectangle; painted regions provide
their exact cells, including disconnected shapes. `TileRegionSnapshot` contains
only region geometry, not fabricated entity movement/blocking facts. The existing
tile sensor and occlusion reader now accept the shared footprint capability. The
independent point extension provides one whole interval snapshot, understood by its
existing sensor; exploration has no point-coordinate logic.

Current JSON uses `authored_exploration`, `exploration_profiles` and explicit
`requirement` references. Both existing authored profile documents and their native/
independent callers were migrated directly. The independent proof binding still
selects the **same** reusable runtime, not a forked implementation. The new native
sensor requirement is authored in the separately master-listed
`region-exploration.json` file. No fabricated default requirement is supplied.

Build `beep-simengine-2.633.0`, save159/replay1. Saved unfinished progress layout is
unchanged; definition/content identities still identify the authored dependencies.
No second clock/progress/knowledge/resource owner, finance, Oil, platform, adapter,
renderer or Draw change. Seismic/logging/drilling prerequisites are untouched.

## Focused verification and all attempts

Targeted command, both observed builds succeeded with zero warnings/errors:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
```

First build6.75s; final build6.44s. No full suite, family filter, profile or broad
bundle. Every runtime call selected **one exact case** with60000ms timeout,
5000ms termination timeout and6500-character bounded output. Every child exited
normally; no lost observation, timeout, restart, truncated output or aborted suite.

| Exact case / attempt | Passed / failed | Child PID | Suite / process ms |
|---|---:|---:|---:|
| Region exploration: authored requirements earn whole remote shapes with private saved dwell — first fixture |222 /4|260|3236 /3374|
| Same new case — final |239 /0|34420|3456 /3614|
| Contact exploration: physical regional discovery and saved sensor dwell |117 /0|25420|918 /1055|
| Exploration bindings: physical point discovery and saved interruption |193 /0|34984|1965 /2103|

**Final focused total549 passed, zero failed across three exact cases.** No
executable source changed after these final runs. Earlier-stage test results were
not rerun or represented as current broad regression evidence.

The first new-case failure was a fixture state expectation: the sensor's worker had
been held through ordinary player input to prevent hauling allocation. Its authored
held/blocked state correctly outranked the sampling state. After its supplier
departs, the fixture now releases that hold through the original player command.
No state priority, production rule, forced state or fake progress was changed.
The selected existing binding test's obsolete literal save154 assertion was changed
to canonical `StateCodecTests.ExpectedSaveFormat`; actual save159/layout unchanged.

### New case scope

Four native/independent metadata × minute/half-minute non-tile sessions verify:

- Actual physical one-unit delivery before sensor readiness; no Tycoon or cash.
- Explorer remains at position5, **outside** the target interval9..11.
- Radius5 sees the center but cannot see the whole interval: no progress/reveal.
- Radius6 permits parallel12/15-minute dwell through the original update owner.
- Ordinary shutdown resets the reset entry and retains unfinished retain progress.
- Sole-clock pause, saved disabled state, saved final interval, deterministic twins.
- No premature discovery, completed membership only in the original private ledger.
- Explicit sharing, company-local forgetting, physical rediscovery and replay.
- Detached original views, exact source/sensor stock, interleaved-session isolation.

Native region probes verify full rectangle/exact painted cells, immutable footprints,
center-only versus full rectangle coverage and disconnected painted geometry without
a bounding-box substitute. These are **geometry/coverage probes**, not a new full
native-tile remote exploration/occlusion campaign.

Six native/independent invalid-content contexts verify missing requirement, unknown
requirement and a region lacking complete observation geometry refuse before play.
The two existing exact contact cases additionally cover actual physical regional
entry, dwell, outside interruption, saves, supply, sharing/forgetting/replay and
existing malformed metadata/composition-validation callbacks.

## Identities and audit limits

Sorted path/content SHA256 over443 current core C# files:
`b947b97c58790c0d66fcbbfa3890dda2475d9b1656a777ce32a9620528c0a590`.
Final Debug framework DLL:
`623D1B76973EEB27AFCBF367AE33F0F27A6D4248280EB028394BCBC107DD85FB`.
Final Debug test DLL:
`A497A5734F0A06D30EC007FDBD5FE5DD55089CDDCD12E51140216CCA5E85F503`.
New test source:
`4275C11E29D77504202DCA63B5C705287C62CAB18E378C6808FC47E94A0F7028`.

Original frozen audit IDs/literal requirements remapped to current stage documents:
221 total,208 recorded checked,13 open. No original broad requirement closes here.
P11.026 still includes arbitrary mod/protected-cell bulk, remote/cell-wide breadth,
utility/condition/occlusion/freshness providers and their fault/stress matrices.
P11.029/.035 and all other previously open gates remain open. This does not prove
every possible mod supplies a truthful complete region union or pure callbacks;
those remain explicit provider contracts, not a sandbox or inferred geometry.

`git diff --check` exit0 with35 existing line-ending warnings and no other diagnostics.
Staged path count0. No matching engine build/test process remained at final read-only
inspection. No full tests, Git staging/commit/push, memory changes or publishing.
