# P8-D2b native input ports — focused verification

Date: 2026-10-08. Canonical Beep.SimEngine only. Full goal/P1–P12 are NOT finished.

## Outcome and sole owners

Native transformation content requires exact input_ports as well as output_ports. Shared immutable
TransformationResourceRoute/TransformationPortSnapshot and one guarded binding resolver serve both
directions. Actual named IResourcePort stores own stock; no implicit local feed or parallel inventory.

Batch startup preflights selected feed, local tools and first operating draw together. The existing
complete movement transaction moves feed into the existing protected WIP. Transformer.SyncWip stores
only canonical original resource/port/endpoint references. Cancellation commits the complete return
to those original endpoints, not currently selected ports; full destinations retain WIP/tools/progress.

Continuous feed uses selected source native packing and whole outbound claims/conversion. Shortage
and output backpressure consume no prefix or earned progress. Flow facts expose detached private
InputPorts/OutputPorts; the obsolete singleton InputStore API is removed, not retained as a shim.

Core2.307.0/save145/replay1/transformer codec7/transformation view11; proof PoolPort codec2 stores
only the selected authored store reference. Original stock/claims/rates/clock/queue owners remain.
Sole GameClock SHA256:35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.

## Authoring and proof boundary

The renamed/revised [AuthorTransformationPorts.ps1](AuthorTransformationPorts.ps1) ran once as a
mechanical authoring migration:20 existing JSON files,22 input maps,19 native inventory aliases.
The same script subsequently migrated tools, operating ports and completion disposition; these remain historical input-stage counts.
It is never an engine loader. Missing/extra/unknown/incompatible routes fail before Update.

Explicit master-listed input-ports.json and input-ports-master.json reuse resource-port-common.json.
That common document also serves the output fixture, avoiding duplicated resources/modes/units.
Existing engine-side PoolPort aliases actual EndpointPools stock and changes only an authored
selected reference through an ordinary player command. The driver owns no progress/material rules.

## Commands and disclosed attempts

Three invocations of dotnet build Beep.SimEngine.slnx --no-restore in this slice:
initial implementation build clean6.58s; new-test compile output was truncated by context and its
full summary/timing is unavailable (the newly registered executable case ran successfully);
final explicit verification build clean1.85s, zero warnings/errors. No compile failure was observed.

ONE distinct NEW exact bounded case, ONE runtime attempt, FIRST green:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "P8 input ports: authored mixed feed, original-source returns and saved batch/flow execution" --timeout-ms 15000 --termination-timeout-ms 2000 --output-chars 20000
```

178 passed,0 failed/aborted in452ms. ChildPID33108 exited0, observed600ms, untruncated stdout/stderr,
no observer error or timeout. No failed/repeated runtime or older/family/full/Oil/platform suite.
Executable inputs retained unchanged through this runtime and final build; frozen17:32:05 UTC.
Subsequent work changes documents/evidence only.

## Scope actually checked and still open

Mixed solid/liquid feed, nonlocal selection despite spare local stock, whole shortage/startup,
three-second batch timing, local tool protection, immutable source/input facts, full original return
destinations, changed port while active, saved returning convergence, exact committed return history,
sequential batch queue, full output WIP cancellation, private company filtering, twin hashes and
recorded replay. Continuous per-port cumulative feed/products, empty/partial feed waits, backpressure,
stop without refunding burned inputs, saved twins and five malformed authored-route refusals.

Current code contains typed original missing/access return waits, but this case does not establish
dynamic endpoint disappearance/provider recovery. Scoped automatic feed is explicitly unavailable
where current local supply cannot reach the selected port; physical scoped supply/export is still
required, not replaced by a local inventory delivery or teleport. Tool and operating input contracts
remain local. Named tool/utility routes, catalysts/entity tools, provider/global purity, accounting,
stress/soak/lockstep and full P8/P1–P12 acceptance remain OPEN. Earlier tests compile only on2.307.0;
their historical runtime results are not current behavioral proof. No full phase checkbox promoted.

No staging, commit or push; publication requires full goal AND plan completion and verification.
