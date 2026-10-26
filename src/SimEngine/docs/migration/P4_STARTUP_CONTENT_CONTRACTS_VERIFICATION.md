# P4-N5 — Startup content contract verification

Date:2026-10-09. [Stage](../../plans/enhancements/P4_STARTUP_CONTENT_CONTRACTS_DESIGN.md).

## Outcome

The sole ResolvedContent type/identity indexes now alias implemented IContentDefinition contracts as well as native Def base classes. All/Count/Find/Get and ContentResolver.Def/OptionalDef retain their existing methods but accept content contracts. Actual objects, frozen metadata, concrete Index and concrete collision rules are unchanged; no parallel catalog, resolver, loader or runtime state.

Broad interface identities reused by distinct content kinds are explicitly ambiguous in the existing index. Find/Get refuse rather than returning the first implementation. More specific native/contract references remain usable. This is selected lookup admission, not a new blanket global ID prohibition.

IWorldSetupProfileDefinition exposes resolved Entries/Includes/dependency identity and DependencyGraph<IWorldSetupDefinition>. Native and external profiles call the existing closure compiler, now WorldSetupProfileDef.Compile; the sole DependencyGraph remains topology authority. The closure validates actual include/dependency agreement and conflicting aliases. WorldSetupModule selects the profile contract; WorldSetupStage iterates entry contracts.

Existing external PointLayerSetupDef and StartupAdmissionProbeDef now implement IWorldSetupDefinition directly with ordinary Def metadata, not WorldSetupDef inheritance. Their own provider-owned parsing/check/generation remains in place. New ContractStartupProfileDef explicitly authors one included profile and calls the same compiler, without a startup scheduler/type switch. It is in the existing master-listed startup-composition-extra.json. No file scanning or compatibility reader.

Core identity:beep-simengine-2.331.0/save154/replay1. No saved state layout or native/provider schema changes. Sole GameClock unchanged.

## Build and runtime evidence

Three build attempts of the framework/test/extension/quickstart graph: first failed in2.12s with12 compiler errors, zero warnings. Contract inheritance exposed competing Id declarations from IContentDefinition and IDependencyNode; startup contracts now explicitly redeclare their identity. No legacy shim. Next two builds clean6.16s/5.13s, zero warnings/errors.

Only ONE NEW bounded exact case was executed, one attempt:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Startup content contracts: independent nodes and profiles share aliases, physical work and saved replay" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

FINAL:67 passed/0 failed/0 aborted,570ms. PID12720 exited0, observed724ms. Stdout/stderr untruncated; no observer error, timeout or forced termination.

## Proven scope

- Contract index aliases five actual startup entries/four profiles, exact native/external object identities and owned frozen metadata.
- Independent selected root includes native profiles; the direct contract node executes once. Unselected company99 intent executes nothing.
- Native concrete and contract lookup identity agree. Missing/wrong contracts and broad ambiguous Find/Get refuse; table reads are detached and profile references immutable.
- Startup creates actual stock20 and real machines once. Restore validates but never generates/regrants.
- Public pure preview and ordinary commands produce timed physical work/travel, then physical delivery4:source16/destination4. Saved twins match while advancing.
- Replay and another interleaved session have identical opening state/command results.
- Nine missing/wrong entry/profile, contract cycle, ambiguous entry/profile, missing after and invalid requested company branches refuse before Initialize or any generation.
- Public player driver has no World/resource/progress mutation. Actual catalog observation is detached instrumentation in the extension module.

Existing startup factory authors inputs in one shared place. No duplicate test-side startup policy or runtime gameplay.

Executable freeze:2026-10-09 01:37:23 UTC. Subsequent edits documentation/audit only.

## Limits

Older startup/point/Planet-X/tile/optional-money runtimes were compiled only, NOT rerun on2.331.0. No full/family/Tycoon/Oil/platform runtime. No business expansion, mandatory money, Draw/adapter, host startup logic or second clock.

This proof is not a global contract-metadata sandbox, mod corruption/fault/performance guarantee, full catalog migration of every existing concrete caller, or full P1–P12 closure. Those gates remain OPEN. No original acceptance checkbox is promoted from this narrow proof; no staging/commit/push until FULL goal AND plan completion and verification.

