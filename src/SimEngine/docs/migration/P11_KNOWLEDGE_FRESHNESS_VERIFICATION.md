# P11-D12 authored knowledge freshness and exact survey visibility — verification

## Current M86: self-observed spatial coverage strategies — 2026-10-22

Native **2.727.0/save161/replay2**, dirty master base HEAD
`cb61483ab81404c11944931aedb4703ba694edb5`. Only LayerFreshKnowledgeVisibility,
LayerRegionKnowledgeVisibility and EngineInfo.Build change among450 native sources. Both original
strategies now delegate their actual coverage callback to EntityObservation.ReadProvider with the
actual selected space. Their local CaptureProvider/callback/Verify sequences are deleted. No new
observer, world hash cache, ledger, map, visibility owner, state field, clock, executor or schema.

### Reproduced defect, not a speculative repair

The built-in strategies previously relied on their caller's outer observation to latch callback
exceptions and observe selected-space changes. Normal native view calls were already protected.
A module using their public IVisibilitySampleTarget contract directly could catch an original
callback exception and continue with a healthy game, or receive true after its callback changed the
actual selected space. It could then save/hash and, where not paused, advance the game.

The new exact negative case reproduces **304passed/38failed** on actual2.726. All38 failures are
in direct fresh/regional callback contexts, not a failed positive workflow or weakened assertion.
Both original strategies now use the same existing read boundary for direct and wrapped use:
callback errors are latched/rethrown unchanged; actual selected-space and clock mutations cannot
return a healthy answer. Normal nested view observation remains intact. This is scoped purity of
the selected provider/space/clock, not rollback or a sandbox against unrelated hidden CLR state.

### Actual contract proof

The same ReadingSpatialAdmissionTests owner gains one negative partial. Existing public-only
PointReadingDef and PointIntervalRegionDef gain explicit transient coverage/containment hooks only;
they are not authored gameplay, saved fields or additional provider implementations. The existing
master-listed finite profile declares a second ordinary regional policy and exact point region.
Each test selects either native fresh or native regional policy, never both, so one cannot mask the
other's coverage behavior. No duplicate subject is loaded and no scheduler/provider fallback exists.

Each policy's baseline is earned through the original real18-minute/eight-material survey. Twenty
saved-baseline contexts cover fresh/regional × direct strategy/native view × clock/throw/clock-throw/
space/space-throw. The actual original point provider changes AccessOpen for spatial injections;
the mutation remains real and is **not** pretended to roll back after a terminal observation fault.
Hooks are always disarmed before the independent saved session is observed/advanced.

Final **398/0** proves one actual callback, foreign unearned refusal before callback, exact original
exception identity, no bool/page/value prefix, first-fault retention, missing/private cached-view
cutoff, healthy save/hash refusal, no further game-step progress, last healthy private snapshot
retention and independent saved-session isolation. Negative direct strategy/hook calls are explicit
engine-contract instrumentation, not a public host discovery path. Fault injection is not replayed.

### Every build, runtime and current replay

Three targeted Release build attempts, never a solution/full test executor:

1. 14.35s/exit1/one CS0103 from the new test's missing Hosting import; no successful test image claimed.
2. 10.59s/exit0/0warnings/0errors, old native2.726 reproduction image.
3. 8.39s/exit0/0warnings/0errors, corrected native2.727 final image.

| Exact bounded scope | Passed/failed | Suite/observer ms | PID/exit | Image |
| --- | --- | --- | --- | --- |
| Spatial coverage callbacks: direct strategies and native views preserve original faults and selected space, before correction |304/38 |549/685 |3812/1 |build2 |
| Same coverage case, corrected |398/0 |604/753 |22728/0 |build3 |
| Spatial reading freshness: independent point surveys expire share forget and physically refresh through native policy |264/0 |897/1048 |19068/0 |build3 |
| Knowledge freshness: physical survey expiry and exact saved visibility |122/0 |1366/1517 |7952/0 |build3 |
| Regional discovery: physical learning, sample privacy and saved continuation |183/0 |851/1003 |17392/0 |build3 |

Latest distinct final scopes **967/0**, all on the captured final build3 image. Every invocation uses
its literal `--bounded-exact`,60000ms timeout/5000ms termination/30000-character output. No default,
family/profile/full/soak executor or unrelated old cases ran. Positive physical work/durations,
material costs, saved twins, private scope and normal sharing/forgetting remain unchanged.

The finite point workflows retain8commands each,0→37/37checkpoints and0→67/67checkpoints; full live
and replay hashes respectively `797A0CC15DF3D807` and `9ACA49160547F313`. The same regional exchange
suffix42→50 retains8commands/8checkpoints/hash `2C7F17484C7CD4B2`; earlier physical/setup steps are
its initial checkpoint, not replayed inputs. Three actual complete-final-hash verifier runs, not
replay of negative hooks or all optional modules. Earlier M85 hashes retain their actual2.726 image.

Artifacts: [all attempts/commands](../../artifacts/optional-module-durability/6c5d0151dec4414a8dd91ff8c0566f2a/attempts.json),
[before inputs](../../artifacts/optional-module-durability/6c5d0151dec4414a8dd91ff8c0566f2a/build-2-inputs.json),
[final tested inputs](../../artifacts/optional-module-durability/6c5d0151dec4414a8dd91ff8c0566f2a/build-3-inputs.json),
[native/proof identities](../../artifacts/optional-module-durability/6c5d0151dec4414a8dd91ff8c0566f2a/native-scope-identities.json),
[compiled audit](../../artifacts/optional-module-durability/6c5d0151dec4414a8dd91ff8c0566f2a/compiled-caller-audit.log),
[original gates](../../artifacts/optional-module-durability/6c5d0151dec4414a8dd91ff8c0566f2a/original-gates-current.json),
[frozen inputs](../../artifacts/optional-module-durability/6c5d0151dec4414a8dd91ff8c0566f2a/final-inputs.json),
[structural checks](../../artifacts/optional-module-durability/6c5d0151dec4414a8dd91ff8c0566f2a/structural-checks.json).

The matched native audit checks450 source/PDB checksums,2941types/12931IL methods/345023instructions,
137typed resource/spatial calls,14resource primitive/eight selected layer field writes, and0unknown/
unmapped/indirect sites. This is structural caller coverage, not execution of all gameplay callers.
Native DLL SHA256 `B2FEEA1684A0BD22B1234B45E4DB35D39240194D363109DED795F55ED5C9A08B`.

Self-review only. Original World first-fault/results/notifications and the
[operational assurance owner](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain the owning path; no swallowed callback, alternate logger or failure-as-success fallback.
No allocation/performance/CI/independent review/consumer/Oil/marine/Tycoon business/clock/platform/
Draw/publication work. Original221/210source-recorded checked/11open/full goal active/no original
closure. Wider P11 provider/fault/remote/whole-region/protected-cell strategies, C3/P8/P10 and every-
optional-module requirements retain their original scope. Continue that finite coverage through
the same existing owners; do not turn these selected contract guards into universal mod-sandbox claims.

<a id="current-m85-independent-finite-spatial-readings--2026-10-22"></a>

## Earlier M85: independent finite spatial readings — 2026-10-22

Native **unchanged2.726.0/save161/replay2**, dirty master base HEAD
`cb61483ab81404c11944931aedb4703ba694edb5`. All450 native sources/native DLL and the independent
PointReadingDef implementation remain unchanged. The same ReadingSpatialAdmissionTests owner gains
one finite-profile partial and exact registration; no new engine executor, provider, ledger, fog map,
cursor store or clock. This qualifies an independently implemented non-tile subject against the native
LayerFreshKnowledgeVisibility policy, rather than treating the native tile proof as universal.

### Authored composition and original responsibilities

One explicit [finite point document](../../tests/ExtensionProof/content/reading-spatial-freshness.json)
declares the existing point subject's150-second lifetime, native fresh-visibility policy and normal
sharing/forgetting policies. The original factory chooses **one** subject declaration: its existing
permanent document still supplies unchanged work/effects/material requirements, but omits that subject
section when the selected finite document provides it. Both are master-listed before the original
loader runs. There is no duplicate definition, runtime overlay, default or compatibility reader.
The permanent/default factory path is retained semantically; it compiled but was not rerun here.

The existing public-only PointReadingDef implements ISpatialKnowledgeSubjectDefinition directly and
continues to delegate SpatialReadingDeclaration. Native CompanyKnowledgeService owns private history,
freshness, sharing and forgetting; native KnowledgeVisibility evaluates the same exact CoversSample
contract; the original point provider owns geometry, actual samples, scoped versions and views.
The new profile needs no changes in any of these owners or in scheduling, movement, work or saves.

### Actual physical and durability proof

ONE new literal case covers minute and half-minute quanta, not a broad family executor. In each:

- An independent point machine travels from4 to0, spends eight of20 real supplies and performs the
  unchanged18-minute sample activity. Partial saved work grants no knowledge. Completion observes
  actual0 with age1, never a fabricated zero-age timestamp.
- 150seconds rounds to3 or5 actual game-clock intervals. Only the exact observed point becomes
  visible; a surrounding interval and an ordinary write preview at another point refuse. Foreign
  ledger reads stay private.
- Ordinary sharing copies the original observed time and age2, rather than renewing lifetime.
  Saved twins and three paused Updates preserve the same age under the original single clock.
- An ordinary player point-write command changes actual live value0→7. Historical data remains0.
  Sender and recipient both expire at the same exact original deadline; expired metadata, values
  and retained page/count/cursor remain unavailable, including after restore.
- Ordinary selective forgetting removes sender history only. The unchanged six-minute physical
  refresh must finish before history/visibility returns; partial saved work grants nothing. It
  captures actual7 and a later observed time. Recipient history stays stale until an explicit share,
  which preserves recipient's original LearnedTick.
- A third refresh is cancelled after real partial work. It cannot renew observed time. Materials
  remain12 because the authored refresh has no inputs; no cost refund/repeated first cost or grant.
  Retained immutable sender/recipient facts, final save and an isolated idle session remain correct.

Every state-changing gameplay input is an ordinary engine command; there are **no** raw layer writes,
ledger grants, stock mutations, instant spawns or progress edits in the new player driver. Clock
pause/resume uses the public single-clock API. Explicit observer reads inspect real data but do not
authorize a player or implement gameplay.

Both original primary games retain their recorders through final state. All eight physical/order,
share/write/forget/refresh/cancel commands and every advancing checkpoint are required:

| Session quantum | Initial→final | Commands/checkpoints | Live and replay final hash |
| --- | --- | --- | --- |
| Minute |0→37 |8/37 |F4575890E57D1087 |
| Half-minute |0→67 |8/67 |838980C8892F5893 |

These are complete new workflows from their actual initial sessions, not only an exchange suffix.
No primary game is replaced after recording starts; separately restored twins never own its recorder.

### Exact attempts, inputs and remaining scope

ONE targeted Release build13.15s, exit0/0warnings/0errors. ONE FIRST/FINAL exact invocation:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spatial reading freshness: independent point surveys expire share forget and physically refresh through native policy" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
```

**264passed/0failed**,788ms suite/922ms observer,PID11504/exit0; no truncation/timeout. No old default
admission case, M83/M84 case or unrelated runtime reran. This is not full regression or proof of
every independent geometry/coverage/freshness provider.

Artifacts: [actual command/attempt](../../artifacts/optional-module-durability/79302e8263ef4258a4141745c98fc1b2/attempts.json),
[tested inputs](../../artifacts/optional-module-durability/79302e8263ef4258a4141745c98fc1b2/build-1-inputs.json),
[native/proof identities](../../artifacts/optional-module-durability/79302e8263ef4258a4141745c98fc1b2/native-scope-identities.json),
[original gates](../../artifacts/optional-module-durability/79302e8263ef4258a4141745c98fc1b2/original-gates-current.json),
[frozen inputs](../../artifacts/optional-module-durability/79302e8263ef4258a4141745c98fc1b2/final-inputs.json),
[structural checks](../../artifacts/optional-module-durability/79302e8263ef4258a4141745c98fc1b2/structural-checks.json).
Native DLL remains `31B1EC6A5C53E98E6B3425590830625EC9D69433A62BF8F72FA957ECED8461F4`.
The949 proof source/project/content inputs differ only in the project and new explicit JSON; proof
DLL changes as expected. No new compiled native caller audit was needed or run for unchanged native.

Self-review only. Existing first-fault/results/notifications and the
[operational assurance owner](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain responsible; no new operational services, swallowed exceptions or dependencies. No allocation/
performance/CI/independent review/full/default/profile/family/soak/consumer/Oil/marine/Tycoon business/
platform/Draw/Git publication. Original221/210source-recorded checked/11open/full goal active; no
original gate closes. Remaining P11.026 provider/fault/remote/whole-region/protected-cell combinations,
original C3/P8/P10/P11 and every-optional-module requirements retain their full original scope.
Next inspect the actual independent coverage callback refusal/fault paths through these same owners,
not another subject registry or a blanket arbitrary-CLR sandbox claim.

<a id="current-m83-protected-bulk-changes-and-earned-freshness--2026-10-22"></a>

## Earlier M83: protected bulk changes and earned freshness — 2026-10-22

Native **unchanged2.726.0/save161/replay2**, master HEAD
`cb61483ab81404c11944931aedb4703ba694edb5` plus preserved existing work. All450 native sources,
947 proof-source/project/content inputs, original JSON, native/proof DLLs and clock remain unchanged
from M82. Only two existing test owners change; no new engine subsystem or gameplay in a host.

### Actual improvement

The existing KnowledgeFreshnessTests physical survey now checks bulk Set/Fill privacy together with
earned finite knowledge, rather than separately assuming those two mechanisms compose. The original
machine still travels, consumes eight of20 actual supplies, works for the authored duration and
records31 only on completion. The new native Fill calls are explicitly **provider protocol
instrumentation**, not player resource generation, command/replay inputs or a second workflow.

While the earned one-cell sample is fresh, real adjacent/same-chunk and distant bulk edits change
the actual layer/map/exact-observer versions once but disclose no company version or changed chunk.
The retained visible page remains usable. A page over a mixed rectangle authorizes only its selected
known slice; continuation into the hidden neighbour returns no value/count/cursor. A mixed known and
hidden write invalidates only the actual viewer once, reports only its known chunk and refuses the
hidden slice before stale-cursor disclosure. Historical31 stays immutable while the live value32 and
all hidden changed values save/load exactly. No resource accounting claim is made for raw protocol
edits. This case is not replayable as a public command stream.

Paused saved sessions retain freshness under the sole clock. The same original age3 boundary expires
permission. Identical actual bulk writes in the saved twins then change neither company's version nor
delta, expose no expired page or layer metadata, and cannot disclose invalid geometry/page envelopes.
Both authoritative hashes agree; history remains31. Ordinary physical re-observation and cancellation
still follow, retaining original material/time/history behavior. No second fog map, expiry table,
cursor store, renderer, provider registry, default or compatibility branch.

The existing independent point-space access case also ran. Its original native shared admission
guards six callback stages (layer/unit/resource/vertical metadata, sample, observation keys) against
throw/state/clock/space failures plus malformed-key and foreign-viewer contexts. It preserves original
first faults, clears failed output and isolates independent sessions. This is a selected scoped
contract proof, not a sandbox for arbitrary unrelated hidden CLR state or all mod providers.

Its two existing positive physical scenarios now require all seven actual commands, a checkpoint for
every committed update, and replay equality with the final live hash. No primary game is replaced
after recording starts. Minute:0→27,7commands/27checkpoints, hash `784D54394484085E`;
half-minute:0→43,7commands/43checkpoints, hash `9AE5284B08831446`. Both actual replay hashes match.

### Exact checks, images and limitations

Two targeted Release builds:12.34s and9.97s, both0warnings/0errors. Four literal bounded invocations:

| Exact case | Checks passed/failed | Suite/observer ms | PID/exit | Image |
| --- | --- | --- | --- | --- |
| Knowledge freshness: physical survey expiry and exact saved visibility | 122/0 | 1281/1418 | 25872/0 | build1 |
| Bulk layers: exact sample-private generation and durable cursors | 133/0 | 533/671 | 11680/0 | build1 |
| Spatial layer access: guarded extension visibility and atomic observation keys, before stronger replay assertions | 819/0 | 1651/1786 | 27036/0 | build1 |
| Same extension case, final complete-stream/final-hash assertions | 823/0 | 1893/2059 | 27972/0 | build2 |

Latest distinct scopes **1078/0**, not the sum of both extension attempts. This spans captured test
images1/2 with identical native/proof images; no full final-image regression. Freshness/bulk were not
rerun after the extension-test-only edit. All runs use `--bounded-exact` with60000ms timeout,
5000ms termination and30000-character output, not a family/profile/default executor.

Artifacts: [all attempts and commands](../../artifacts/optional-module-durability/84a00da8c3cc48e392df693a36fa56f7/attempts.json),
[build1 inputs](../../artifacts/optional-module-durability/84a00da8c3cc48e392df693a36fa56f7/build-1-inputs.json),
[build2 inputs](../../artifacts/optional-module-durability/84a00da8c3cc48e392df693a36fa56f7/build-2-inputs.json),
[native/proof identities](../../artifacts/optional-module-durability/84a00da8c3cc48e392df693a36fa56f7/native-scope-identities.json),
[original gates](../../artifacts/optional-module-durability/84a00da8c3cc48e392df693a36fa56f7/original-gates-current.json),
[final inputs](../../artifacts/optional-module-durability/84a00da8c3cc48e392df693a36fa56f7/final-inputs.json),
[structural checks](../../artifacts/optional-module-durability/84a00da8c3cc48e392df693a36fa56f7/structural-checks.json).
The [M82 lookup correction](P11_KNOWLEDGE_LEDGER_DURABILITY_VERIFICATION.md#current-m82-observed-knowledge-provider-lookup--2026-10-22)
and its compiled137-call audit retain their actual original evidence; no new compiled audit was needed
or run for unchanged native sources. Native DLL remains
`31B1EC6A5C53E98E6B3425590830625EC9D69433A62BF8F72FA957ECED8461F4`.

Self-review only. Reuse existing World first-fault/results/notifications and the
[operational assurance owner](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage);
no operational service or exception handling changes. No allocation/performance, CI, independent
review, full/default/profile/family/soak, consumer/Oil/marine, Tycoon business, platform/Draw or Git
publication. Original221/210source-recorded checked/11open remain, no original gate closes/full goal
active. P11.026 still needs broader regional/freshness-provider and protected-cell strategies;
C3/P8/P10/P11 and every-optional-module gates retain their full original scope.

## Earlier D12 implementation — 2.261.0

Date2026-10-08. Canonical Beep.SimEngine only. Full P1–P12 goal ACTIVE; commit/push only after FULL
goal AND plan finish and verified. This new focused case/inventory is not whole-engine acceptance.

## Ownership and identities

Build2.261.0/save140/native authored knowledge provider3/KnowledgeView3. Original knowledge codec2
and record payload unchanged. [CompanyKnowledgeService](../../src/Beep.SimEngine/Standard/CompanyKnowledge.cs)
owns both the original ICompanyKnowledge and new IKnowledgeFreshness alias. Native provider implements
optional IKnowledgeFreshnessProvider. Required per-subject fresh_for is explicit null/permanent or
positive rational duration; finite lifetime requires latest retention. Conversion through selected
actual session time validates before first Update. A provider without the capability is unsupported,
not fabricated permanent. That external/unsupported path has NO runtime acceptance in this gate.

[Module registration](../../src/Beep.SimEngine/Standard/CompanyKnowledgeModule.cs) aliases the same
owner, not another ledger. [Private view](../../src/Beep.SimEngine/Views/KnowledgeView.cs) exposes
immutable original reading, authored lifetime, converted lifetime steps, exact age duration/steps,
fresh status. Age is sole GameClock.StepIndex minus original ObservedTick. Existing interval-start
timestamp includes the interval that completed physical work; expiry is age >= converted lifetime.
Positive durations round upward to the session quantum. No guessed timestamp shift or expiry timer.
Finite subquantum intent can already be stale at the first post-Update snapshot; this is the declared
clock coordinate, not a guaranteed one-frame fresh-display promise. Historical APIs preserve stale
detached records; no live resource sample is read by a freshness query.

[Spatial subject contract](../../src/Beep.SimEngine/Standard/SpatialReadings.cs) declares actual layer
and exact CoversSample. Native TileReadingDef requires its exact one-cell footprint, not any larger
containing rectangle. [Fresh visibility definition](../../src/Beep.SimEngine/Standard/LayerFreshKnowledgeVisibility.cs)
resolves stable spatial subjects against the exact target layer, and joins the same private freshness
service to pure sample coverage. [Existing module](../../src/Beep.SimEngine/Standard/KnowledgeVisibilityModule.cs)
registers one new section in the original selected visibility policy. Ownership/policy checks remain
ANDed. Freshness authorizes current native sample access; historical payload remains historical.

Original map scope/cursor authority is unchanged. A finite fresh scope includes actual clock coordinate
as a transient dependency, so the advancing expiry Update invalidates even though refresh precedes
clock completion. This is conservative: each advancing finite-fresh update can invalidate, and the
next refresh removes an expired scope. Actual permission always reads the current sole clock.
No completion move means failed visibility refresh cannot advance a failed Update. Restored scopes
derive from actual clock/ledger; session-local cursors are not persisted or promised twin-identical.
Scoped provider guards/reentry checks are not a universal hostile-mod rollback sandbox.

## Every build/run

Working directory C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine:

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Knowledge freshness: physical survey expiry and exact saved visibility"
```

- ONE build exit0,13.44s,0 warnings/errors. Build yielded live session17959, polled to completion;
  not restarted. ONE exact NEW bounded case exit0,84 passed0 failed0 aborted suites460ms,FIRST attempt.
- No repairs, extra cases, old/family/full/platform/OilFieldDays/stale binary run.125 older strict
  build identity files/lines migrated compile-only. Seven JSON files add explicit null lifetime,
  three inline declarations migrate likewise; public point predicate is compile-only. Native authored
  provider semantic version advances3, original ledger codec2 unchanged. No compatibility reader.
- Gameplay source/public proof/tests/JSON frozen after green; remaining edits documentation/evidence
  only. Prior fleet/navigation/occlusion/logistics/clock runtime gates NOT rerun at2.261.0.

## Native proof scope

[New case](../../tests/Beep.SimEngine.Tests/KnowledgeFreshnessTests.cs) assembles native master-selected
files, authors150 seconds in the original reading document, and selects only
[new deposit rule](../../tests/ExtensionProof/content/native-fresh-survey-visibility.json). Other public
layers are explicitly unlisted; moving live sensor scope cannot account for this expiry cursor proof.
The native machine actually travels, consumes8 of20 materials, performs timed survey work and records
actual31 only on completion. Partial travel/work checkpoint continues identically. No test gameplay
module, stock mutation, spawn, forged progress or instant knowledge recording.

150 seconds rounds to3 actual minute intervals. Post-completion observation age1 is fresh; paused
saved twins stay age1; advancing age2 remains fresh and invalidates the prior page; age3 becomes stale
and that same Update increases existing presenter cursor. Exact cell/private/whole mixed rectangle
refusals clear output and return no foreign metadata. Read/previews leave hashes unchanged; payload
and held freshness snapshot stay immutable/detached. Stale history and original times remain readable;
restoring an expired checkpoint does not resurrect permission.

Explicit native layer Set protocol instrumentation changes actual deposit31→7 without touching history.
Ordinary physical survey.reobserve preserves stale data during partial work, completes12 native
intervals, retains LearnedTick, refreshes ObservedTick and captures actual7. Partial refresh saved twins
agree. Another native protocol Set7→9 demonstrates authorized current sample9 while remembered sample7
remains unchanged. These layer changes are NOT player weather/terrain command or extraction acceptance.
Normal cancellation grants no refreshed time. One original ledger row persists; interleaved fresh
session borrows none. Seven invalid lifetime cases (missing/zero/negative/denominator/unit/first/
conversion overflow), five invalid rule references (missing/empty/duplicate/wrong-layer/non-spatial)
refuse. Explicit-null content composes but does not grant unearned permanent knowledge. This is not a
runtime proof of earned permanent or unsupported-provider freshness.

Per physical stage500-Update failure bound with clock/position/order-state/blocker diagnostics.
460ms is observed focused runtime, not an allocation/CPU guarantee or game-speed cap.

## Scoped source checks and remaining acceptance

Scoped642 paths/131 Markdown/2068 local links/0 broken/0 duplicates, actual git diff --check exit0
with existing LF/CRLF warnings. [Manifest](P11_KNOWLEDGE_FRESHNESS_SOURCE_MANIFEST.json) records exact file
hashes/invocations. Historical manifests/reports remain unchanged; inventory is accumulated selected
source, not all dirty files or full-repository runtime acceptance. Own manifest excluded from its hash
set. Clock source remains unchanged; no clock/draw/platform edit.

Automatic exploration collection/sharing/forgetting, broader freshness strategies/non-tile providers,
provider fault/global purity, large-scope invalidation/allocations/CPU, replay/lockstep/fuzz/soak,
rival knowledge and full P11/P1–P12 acceptance remain OPEN. Oil domain migration and future platform
integration are not performed here. No Tycoon business source, hosts, demos, Draw/Godot/Unity change.

[Stage](../../plans/enhancements/P11_KNOWLEDGE_FRESHNESS_DESIGN.md),
[P11 parent](../../plans/enhancements/P11_PROGRESSION_KNOWLEDGE_CONTROLLERS_DESIGN.md),
[master tracker](../../plans/enhancements/MASTER_TRACKER.md).
