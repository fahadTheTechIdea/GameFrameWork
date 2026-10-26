# P4-N4 — Startup composition verification

Historical P4-N4 runtime evidence; not rerun on2.331.0. P4-N5 now resolves startup definitions/profiles through content contracts; composition admission and actual generation remain with the same owners. [Current evidence](P4_STARTUP_CONTENT_CONTRACTS_VERIFICATION.md).

Date:2026-10-09. [Stage](../../plans/enhancements/P4_STARTUP_COMPOSITION_DESIGN.md).

## Outcome and ownership

The existing engine composition pipeline now visits services, systems and registered world-generation stages with reference alias deduplication. All preparations run before validations; GameDefinition.Build obtains one ordered stage list for validation and generation. No second validator registry, startup executor, graph compiler, spawner or clock.

IWorldSetupDefinition/WorldSetupDef now require ValidateComposition(World). WorldSetupStage delegates selected actual nodes through the same contract and scoped observation guard. EntitySetupDef checks actual requested company seats, same-company authored homes, selected provider/presence identity and strict provider-owned placement decoding before Initialize. Native and external layer definitions explicitly implement their existing pure declaration checks. JSON after must be explicitly an array.

Actual generated terrain, placement bounds, collisions and actual dependency/home results remain authoritative during generation. This is NOT global observation purity, complete generation atomicity, future geometry prediction or rollback.

Core identity:beep-simengine-2.330.0, save154, replay1. No saved layout/state schema change; no compatibility reader. External PointLayerSetupDef implements the new mandatory contract. Old native/composed startup assertions were source-corrected and compiled only, not runtime rerun.

## Executed evidence

Four clean builds of the framework/test/extension/quickstart project graph:6.47s,5.48s,5.01s,4.69s; zero warnings/errors.

Only ONE NEW exact case was executed:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Startup composition: selected declarations refuse before Initialize and preserve physical saved work" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

First attempt:0 passed/1 failed,179ms; PID30368 exit1, observed360ms, no truncation/observer error. Supplemental authored profile omitted its source dependency entry. Corrected explicit profile membership by including the same source ID; existing graph deduplication preserved. No engine dependency bypass or inferred profile selection.

Second attempt:92 passed/0 failed/0 aborted,597ms; PID40888 exit0, observed744ms. Source boundary inspection then found decoder fault instrumentation in the player driver. Moved that module-composition instrumentation into StartupStageProbe.Compose; no engine/gameplay change.

FINAL, same new exact case only:92 passed/0 failed/0 aborted,537ms; PID16744 exit0, observed686ms. Stdout/stderr untruncated, no observer error, no timeout or forced termination.

## What the case proves

- Aliased stage/service preparation and validation run once before Initialize; generation runs only on new games.
- An external native node participates through contracts/content without scheduler modification. Unselected company99 intent executes nothing.
- Actual startup stock20, placement4 and same-company home resolve through immutable/public views.
- Normal player preview is pure; physical work moves from4 to18 and consumes simulation time, not instant results.
- Hauling4 performs physical pickup, travel and handling; source16/destination4, no duplication or startup regrant.
- Saved twins remain hash-identical during work and hauling; restored composition validates without generating/granting. Replay and independent new-game opening match.
- Requested-seat, foreign-home, missing/null/nonarray after, wrong space/presence, absent space, missing position, extra placement field, stage refusal, external-node refusal and decoder refusal all reject before any Initialize or generation.
- Out-of-bounds actual geometry refuses during generation after Initialize; later stage generation is not reached. This explicitly retains the static/actual boundary.

The public player driver contains no World escape or raw resource/progress mutation. Extension proof code owns only lifecycle counters/fault instrumentation; actual engine startup and gameplay remain in their authoritative owners.

Executable freeze:2026-10-09 01:27:49 UTC. Subsequent edits are documentation/audit only.

## Remaining limits and publication

All historical/native/point/tile/Planet-X/optional-money suites are source/compile only on this build. No family/full/Tycoon/Oil/platform runtime was run. No business expansion, mandatory money, Draw/platform adapter, second clock or host-side startup logic.

Broad provider/mod/fault/scale/global-purity/atomic-generation and full P1–P12 remain OPEN. This narrow result does not close any complete original acceptance requirement. No staging/commit/push until the FULL goal AND plan finish and are verified.

