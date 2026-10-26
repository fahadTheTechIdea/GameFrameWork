# P12-C2 environmental content contracts — scoped verification

## Current M94: retained environmental queries stop at the original terminal fault — 2026-10-22

Native **2.732.0/save161/replay2**, dirty master base
`cb61483ab81404c11944931aedb4703ba694edb5`. A retained EnvironmentalMetricsView could return
NotOwned before invoking its source, and the original EnvironmentalMetrics.Read could return
NotSeated/UnknownDef before entering provider observation. These paths looked like ordinary
healthy query results after a terminal fault. The public view and original source now call
the existing World.RequireHealthyUpdate before their respective shortcuts. The view retains
its actual World and resolves the same registered source; no new policy/observation mechanism,
metric history, journal, resource state, clock, scheduler, provider or codec is introduced.

Only EnvironmentalMetricsView, EnvironmentalMetrics and EngineInfo change among450native
sources. All949proof source/content inputs are unchanged; dependent binaries were rebuilt.
Protocol expectation advances2.731→2.732; save161/replay2 and payload schemas are unchanged.
Healthy foreign/invalid/missing/null metric meanings remain NotOwned/NotSeated/UnknownDef;
complete empty history is still measured zero and zero age still has NoElapsedTime.

### One existing test owner and physical proof

One new literal reuses the original Physical method and original six authored provider faults
in EnvironmentContentContractTests. The same Faults helper optionally checks retained views
and restored sessions, not a copied environment simulator. The original old entry's stale
save154 assertion now uses the existing current-protocol oracle; that older literal itself
was not run. No malformed metadata gate is claimed current merely because it compiles.

Actual null/throw/pause/entity-touch/reentry/wrong-identity callbacks fault through the existing
selected provider. Twelve fresh/restored contexts capture healthy facts before the failure.
Five cached viewers (own/foreign/exact observer/two invalid) × five companies × native/direct/
faulting/missing/null metrics, plus direct original source-contract queries, all reject the
first terminal fault. Direct source access is explicitly read-only framework diagnostics,
not a public mutable host escape. Retained detached observations stay unchanged; the first
fault/time, save/hash refusal, stopped Update and independent live session remain intact.
No rollback/global CLR isolation or invocation-count measurement is claimed.

The reused physical scenario uses only actual player commands, views and Update: machine
travel4→20, eight supplied repair materials, twelve-minute repair, once-only two-unit loss,
three-minute cleanup and three-unit disposal. Holding/cancelling work pauses the same actual
response rather than granting progress. Final stock20−8−2−3=7; native/direct metrics agree,
private objective completes only after earned cleanup/publication, exact rates/expired history/
quiet windows, paused clock and saved twins remain correct. No new host gameplay or stock write.

The primary session remains alive throughout recording. Complete initial-to-final replay
**0→49/5external player commands/49checkpoints**, expected/actual hash
**7AD5770588F40966**. New footer asserts final live hash and every advancing update, rather
than only verifier.Success. Both actual attempts run this verifier before the negative matrix;
two successful physical verifiers do not turn the failing baseline case into a pass. No
cross-build save/replay compatibility or saved replay-file artifact is claimed.

### Every actual build and runtime

Two targeted Release builds, both0warnings/0errors/exit0:5.99s and7.33s reported by build
(outer6301/7639ms). Same command:
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.

One distinct literal, exactly two invocations:
`Environmental views: retained terminal faults reject private unknown metrics and preserve physical accounting`.

| Attempt | Passed/failed | Suite/observer ms | PID/exit | Native image |
| --- | --- | --- | --- | --- |
| Before original entry preflights |533/1512 |790/929 |29048/1 |build1/2.731 |
| Final original view/source correction |2045/0 |803/940 |14764/0 |build2/2.732 |

Literal `--bounded-exact`,60000ms timeout/5000ms termination/250000output characters; both
processes Exited, no aborted suites/truncation/observer error. Final **2045passed/0failed**,
one final complete physical verifier. No executable edits after final green, no unrelated
old case, full/default/profile/family/soak executor or consumer execution.

One explicit-path metadata-only profile-info call exits0:1062registered/1042included/20excluded/
runtimeSuitesExecuted0. One new case is added, all1061prior assignments/exclusions/limits
preserved. This is profile admission, not a whole-profile pass.

Read-only native compiled audit:450matched source/PDB,2944types/12937methods/345495IL
instructions,137typed resource/spatial calls,14primitive/eight layer writes and0unknown/
unmapped/indirect. Script success=True/enclosing shell exit0; last native-exit variable is
null, not an invented independently reported script exit. Native SHA256:
**6D94CA4B3064699E05C39BB48BE4DD34856B4D217FF5C84922179569FF4B8080**.
This structural audit is not execution of every caller or whole-resource qualification.

[Before](../../artifacts/optional-module-durability/f9b3f1f9463e4c40a334b10e80cc8614/before.json),
[all attempts](../../artifacts/optional-module-durability/f9b3f1f9463e4c40a334b10e80cc8614/attempts.json),
[tested inputs](../../artifacts/optional-module-durability/f9b3f1f9463e4c40a334b10e80cc8614/build-2-inputs.json),
[source identities](../../artifacts/optional-module-durability/f9b3f1f9463e4c40a334b10e80cc8614/native-scope-identities.json),
[compiled audit](../../artifacts/optional-module-durability/f9b3f1f9463e4c40a334b10e80cc8614/compiled-caller-audit.log),
[original gates](../../artifacts/optional-module-durability/f9b3f1f9463e4c40a334b10e80cc8614/original-gates-current.json),
[frozen inputs](../../artifacts/optional-module-durability/f9b3f1f9463e4c40a334b10e80cc8614/final-inputs.json),
[structural checks](../../artifacts/optional-module-durability/f9b3f1f9463e4c40a334b10e80cc8614/structural-checks.json).

Self-review only; original World fault/results/notifications and the existing
[operational assurance](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain responsible, without a second logger, silent fallback or swallowed callback. No new
dependency, business/Tycoon/consumer/Oil/marine/platform/Draw/adapter changes or execution.
CI, independent review and allocation/performance were not performed. Original221/210source-
recorded checked/11open/no original closure/full goal ACTIVE. Wider accounting, arbitrary
provider/private mutation, pressure/corruption/lockstep/stress/soak and every-optional-module
gates stay open. No staging/commit/push before full goal/plan completion.

## Earlier C2 implementation and execution — 2026-10-09

2026-10-09; core2.382.0/save154/replay1. Full goal and P1-P12 remain OPEN.
[Stage](../../plans/enhancements/P12_ENVIRONMENT_CONTENT_CONTRACTS_DESIGN.md).

## Implementation

Original environmental facade, views and objective binding resolve actual immutable
IEnvironmentalMetricDefinition aliases. Unit metadata uses actual IUnitContentDefinition.
EnvironmentalMetricDef is deleted; native and existing independent providers derive directly
from Def. EnvironmentalMetricDeclaration is the sole strict unit/rate/window reader/resolver;
ResourceEnvironmentalMetricDeclaration owns the original explicit native resource/kind/cause
classification and journal algorithm. The new independently compiled definition delegates it.

All loaded definitions validate actual frozen aliases/units, explicit rates and positive
quantum-exact windows before Initialize. Original EntityObservation guards actual callbacks,
clock/identity and existing entity facts. A transient reentry flag rejects recursion and resets
in finally; it is not persistence. Existing resource journal6, physical work/stock, objective
system1 and sole GameClock retain authority. No native-only selector, default metric, extra
balance/history/compiler/scheduler/state machine/clock or game-size limit was introduced.

The original pure control fixture authors actual extension requirements into its master JSON.
The new driver only submits normal commands, advances Update and reads public immutable facts.
Fault probes are independently registered provider callbacks, not gameplay in the driver.

## Actual build and runtime

Three `dotnet build Beep.SimEngine.slnx --no-restore` invocations:

- First exit1,7.45s, zero warnings/two errors: proof used Pause on readonly IGameClock.
- Clean exit0,9.08s, zero warnings/errors: explicit actual GameClock fault-probe casts.
- Final exit0,4.82s, zero warnings/errors: migrated historical fault-owner expectation; old case NOT run.

The ONLY new runtime CASE (two invocations, no other cases):

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Environment content contracts: physical incident accounting and private objectives" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

Attempt1:126 passed/0 failed/0 aborted in1111ms; PID5996/exit0/observed1288ms.
Final review migrated one old assertion from objective-owned to actual observation.provider first
fault. The old case was NOT executed; the SAME new case was rerun against the resulting assembly.
Attempt2 FINAL:126 passed/0 failed/0 aborted in817ms; PID33000/exit0/observed971ms.
Both children observed Exited, stdout/stderr not truncated, no timeout or observer error.
Final selected283 executable/proof/test/JSON inputs hashed AFTER final green at16:06:03UTC.
Earlier282-input inventory16:02:03UTC belongs to attempt1 only, not the final assembly.
These are POST-RUN inventories, not pre-run freezes or complete transitive closures.
No executable edits between final green and final snapshot or afterward; audit compares hashes.
Earlier G43/P3-S13 and all historical results were NOT rerun on2.382.0.

## Narrow proven scope

An ordinary raised incident grants no materials or immediate loss. The assigned machine travels
4→20, spends eight actual repair inputs, earns twelve minutes of repair, commits two units of
actual loss, then earns separate three-minute cleanup and disposes three actual units.
Final stock20−8−2−3=7; each classified physical event appears once. Native/direct definitions
observe identical actual quantities through the original journal. Held worker and ordinary
cancellation preserve the same episode/order and block cleanup without fabricated fallback.

Private cumulative rates use exact native quantities/elapsed minute-clock time; a complete
short window measures the real spill, a later quiet window reports real zero, and overlapping
evicted history refuses HistoryExpired. Cumulative amounts survive short retained history.
The direct metric drives the original objective only after earned physical cleanup/event
publication, not recursively in the publishing Update. Foreign observations withhold units/
amounts/time, foreign objective activity stays absent and foreign company loss remains zero.
Zero elapsed time has NoElapsedTime, not a fabricated rate. Held earlier facts remain detached.

Raised/travel/material/held checkpoints and saved continuation through cancellation, cleanup
and resolution share hashes;
one paused clock freezes actual work/history/objectives. Ordinary command/control replay and
untouched interleaved-session isolation pass. Eighteen metadata/validation/authoring refusals
precede Initialize, including unused direct definitions. Six actual live callbacks cover
null/throw/pause/entity-touch/reentry/wrong identity; all fault observation.provider and refuse
healthy Save/Hash. These scoped faults do not prove arbitrary CLR/global mutation isolation.

## Remaining gates and publication

Native historical accounting runtime, arbitrary private codec/metadata mutation, broad provider/
geometry/utility/network/propagation/contamination/scientific formulas, whole-session resource
accounting, shuffled lockstep, stress/fuzz/soak and compound/full acceptance remain OPEN.
Full regression requires explicit human authority. G19's excluded-file decision is unanswered.
Tycoon/Oil/platforms/Draw/hosts/demos/sole-clock untouched. No staging/commit/push before the
entire goal AND plan are genuinely complete and verified.
