# P3-S1 focused spatial privacy evidence

## Current checkpoint: S1.4–S1.5

Original P3.acceptance.008 is now checked after the requirement-specific coverage
audit below. Seven selected current literals1305/0 complement the unchanged first
slice's direct/layer1637/0; these are adjacent scoped checkpoints, not a single
2942-check full regression. The first-slice history below remains preserved.
Engine2.605.0/save159/replay1 and all441core C# source hashes remain unchanged.
Only one original acceptance checkbox changes:221/198recorded checked/23tracked
open. This bookkeeping is not a fresh audit of all198previously checked items.

### Original requirement coverage

Requirement: **Snapshots and spatial queries disclose only viewer-authorized
immutable data.** This concerns the engine's declared projection/query contracts,
not a hostile CLR-module sandbox or every optional gameplay menu's authorization.

| Required dimension | Actual owners and inspected assertions | Current evidence |
|---|---|---|
| Viewer-scoped entity/full snapshot facts | `PlayerView.CanSee`, staged `CopyEntities`, `GameSnapshot.Capture`; hidden entity/owner filter/inspection refusals, explicit observer separate | Core observation201; deltas72 |
| Area, occupant, authored footprint queries | `TileMapView`; same sight filter, hidden geometry withheld although observer proves4x2authored shape exists, invalid/empty regions clear output, late foreign callback exposes no earlier matched prefix, occupant/footprint failures clear out values | Core observation201 |
| Retained immutable entity/space facts | `EntityView` value copy; snapshot read-only collections; tile/point payloads detached after real movement; live space and publication share scoped identity/revision | Core201; deltas72; headers286; unchanged direct818 |
| Authorized immutable layer metadata | `SpatialLayerAccess`, `SpatialLayerSnapshot`; actual layer/content/unit/resource authorization, copied resource/unit/capacity facts, frozen nested JSON, retained owner not rewritten by real ownership transfer | Native layers65; unchanged non-tile access819 |
| Native/sample privacy and cursors | Original TileMap visibility/generation revisions and TileMapView sample preflight; hidden same-chunk writes/erasures expose no revision/change region, invalidation only affected observers; refused pages expose no count/cursor/prefix | Bulk132; paging85; non-tile access819 |
| Visible deltas without hidden-state leakage | Original retained snapshot is sole baseline; unseen changes absent, physical sight reveals Spawned, visible changes Changed, lost sight Removed contains only known ID, reacquisition Spawned; native/point/nonspatial production births | Deltas72 |
| Provider errors cannot publish partial or wrong-scope facts | Original EntityObservation/World first-fault owners; malformed headers, null/foreign/negative results, mutation/exception callbacks and asymmetric/unstable comparison refuse; each viewer retains OWN previous healthy payload and epoch, empty deltas, no callback retry/healthy save/hash | Core201; headers286; comparison464; direct818; layer819 |
| Persistence/session independence | Actual saved twins and replay retain native state/knowledge/values; publication baselines and page cursors are transient, foreign-session cursors refuse; other sessions unchanged | Deltas72; headers286; comparison464; bulk132; layers65; paging85; unchanged direct/layer |

This gate is supported by both inspected execution paths and current runtime
assertions, not by case names or a search result alone. `ViewFeatureRegistration`
observes creation and rejects direct mutable-owner aliases; it explicitly does not
provide reflection isolation for custom modules. Mod providers must honor their
detached immutable snapshot contract; this closure does not claim arbitrary CLR
object-graph safety. Later all-gameplay-menu/knowledge/optional-module/stress gates
remain separate and OPEN.

### Changes after first slice

`CoreViewObservationTests` extends its EXISTING healthy JSON fixture locally with
hidden4x2geometry (no new content owner), confirms observer/player distinction,
returned-struct detachment, retained entity/removal collection immutability, and
wide invalid/empty area behavior. Existing explicit negative probes now also test
late spatial collection visibility failure, occupant output clearing and footprint
output clearing. Its physical construction wait now has2000updates/20s bounds.

Existing header/comparison tests replace historical save154 pins with independently
authored protocol expectations; their1000-update loops add20sdeadlines. The delta
case reuses its existing Reach helper for saved point completion and adds the same
deadline. Existing failure cutoffs/physical checks are preserved.

Native `SpatialLayerTests.Persistence` had an obsolete `ReferenceEquals` expectation
requiring live ResourceDef exposure. It now verifies stable actual catalog identity,
exact unit/category/phase/traits/capacity values, NO live content aliases (including
capacity-unit links), and immutable copied collections. No engine behavior changed.

### All additional attempts

All runtime invocations use the same bounded-exact/60s/5s termination options as
below, with bounded captured output. Baseline core process ran the previously
compiled artifact; code was edited while it ran, then rebuilt only after its
terminal exit was observed. No duplicate or abandoned process.

- Core baseline179/0,11497ms, PID33136 exit0/11636ms (session34606 polled terminal).
- Edited core build exit0/4.47s, zero warnings/errors.
- FINAL core201/0,11660ms, PID2540 exit0/11797ms (session96111 polled terminal).
- Header baseline285/1,562ms, PID1768 exit1/700ms: only historical154vs159 assertion.
- Comparison baseline463/1,685ms, PID12612 exit1/825ms: same stale protocol assertion.
- Edited header/comparison/delta build exit0/4.96s, zero warnings/errors.
- FINAL headers286/0,568ms, PID31944 exit0/714ms.
- FINAL comparison464/0,908ms, PID17436 exit0/1058ms.
- FINAL deltas72/0,2959ms, PID17740 exit0/3107ms.
- FINAL bulk132/0,1228ms, PID16604 exit0/1374ms.
- Native layers baseline58/1,160ms, PID20980 exit1/314ms: obsolete live-definition reference assertion.
- First edited layer build exit1/3.90s, CS0411: missing System.Linq import for new SequenceEqual assertion. No runtime launched from failed build.
- Corrected layer build exit0/4.23s, zero warnings/errors.
- FINAL native layers65/0,124ms, PID4444 exit0/257ms.
- FINAL paging85/0,310ms, PID24520 exit0/444ms.

Additional exact selectors (no directory/family/profile selector):

1. `Core view observations: guarded callbacks, private facts and atomic fault publication`
2. `Spatial snapshot headers: actual space identity and retained private publications reject malformed captures`
3. `Spatial snapshot comparison: observed symmetric stable delta matching and first-fault retention`
4. `Snapshot deltas: visible facts, physical mod movement and saved publication baselines`
5. `Bulk layers: exact sample-private generation and durable cursors`
6. `Spatial layers: private ownership, detached views and saved sparse values`
7. `Spatial layers: paged tile and non-tile reads without area caps`

All children terminal; no abort/timeout. Final source hash rechecked identical to
the first slice. Only tests/documents changed. No full/profile/family run, Oil,
Tycoon, platform, host gameplay or Git publication. Full objective remains active.

## Historical first-slice checkpoint (before S1.4–S1.5)

2026-10-15. Engine2.605.0/save159/replay1 unchanged. The stage extends existing
tests only; no engine implementation, content, protocol or ownership changes.

## Inspected owners

`PlayerView.CanSee` and `ViewOf` observe the selected visibility/presence provider
and target state. `GameSnapshot.Capture` retains scoped visible facts and derives
deltas from each viewer's previous publication. `EntityObservation.ReadSpace`
validates the actual selected spatial identity and nonnegative revision.

`TileMapView` validates every requested sample before emitting native values;
entity collections stage their results before publication. `SpatialLayerAccess`
checks actual registered layer identity, linked content visibility, ownership and
sample authorization through the original observation guards.
`SpatialLayerSnapshot` copies unit/resource facts and clones/freezes metadata.
These inspected paths did not justify a replacement engine subsystem.

## Changes

Both existing cases now compare save format with the independently authored
`current-engine-protocol.json` through `StateCodecTests.ExpectedSaveFormat`, not a
readback expectation copied from `EngineInfo`. Both physical1000-update loops also
stop at20seconds and report clock/fault/published entity states.

The existing layer-access case additionally retains actual point-layer snapshots,
rejects nested metadata writes, confirms no live content definitions escape via
unit/resource interfaces, transfers ownership with the public command, checks
former-owner metadata/sample refusal and empty destination, verifies historical
retained facts, and has the NEW owner transfer it back. Existing saves, physical
work, exact30/60-second clock compositions, replay and callback fault proofs remain.
No player rule or transfer fallback was added to the test driver.

## All build/runtime attempts

Build command: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`.

- Baseline build exit0, zero warnings/errors,1.23s reported.
- Baseline direct case817/1,618ms, PID19128 exit1/763ms: only stale154vs159 assertion.
- Baseline layer case786/1,1910ms, PID31928 exit1/2047ms: only stale154vs159 assertion.
- First edited build exit1/3.70s: two CS1061 diagnostics, test timeout diagnostic
  incorrectly assumed `WorkView.Orders`. Corrected to actual published entity states.
  No runtime launched from failed compilation.
- Corrected build exit0/5.36s, zero warnings/errors.
- Direct case818/0,565ms, PID28544 exit0/703ms.
- Layer case62/4,426ms, PID22476 exit1/563ms: test tried returning ownership as
  former owner, correctly refused `core.not-owned`; later private-read assertions
  failed because that transfer did not happen. Corrected player input to seat1;
  no engine authority relaxed and no assertion removed.
- Final build exit0/4.32s, zero warnings/errors.
- FINAL layer case819/0,2062ms, PID33392 exit0/2201ms.
- FINAL direct case818/0,553ms, PID32432 exit0/690ms.

The only literal runtime selectors were:

1. `Direct spatial views: shared capture admission and retained viewer faults`
2. `Spatial layer access: guarded extension visibility and atomic observation keys`

Each used `--bounded-exact`, `--timeout-ms 60000`,
`--termination-timeout-ms 5000`, bounded output. All children terminated normally;
no aborted/timeout result. Final sum1637 passed/0failed. Earlier failed attempts
are NOT added to final passing totals.

## Scope and remaining work

No full/profile/family run. No Oil/Tycoon/platform/host implementation or Git
staging/commit/push. Production441 C# files still have combined SHA256
`aaa21d3297f20d16a0386876eeaf76df498490b18bddf8517018c7ba5bb0ad82`, matching
the prior checkpoint. No new save reader, data owner or compatibility mechanism.

Original P3.acceptance.008 remains OPEN: these two cases are scoped current proof,
not complete all-projection/provider/knowledge/menu/delta coverage. Original
bookkeeping stays221requirements/197recorded checked/24tracked open, not a fresh
global audit. No original acceptance checkbox changed. Full goal remains active.
