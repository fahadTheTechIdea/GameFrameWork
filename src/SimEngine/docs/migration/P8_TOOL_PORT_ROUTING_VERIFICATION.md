# P8-D2c reusable tool ports — focused verification

Date: 2026-10-08. Canonical Beep.SimEngine only; full P8/P1–P12 and goal remain OPEN.

Historical2.308.0 proof; not rerun on2.309.0. The same authoring script is now revised for operating
ports in [D2d](P8_OPERATING_PORT_ROUTING_VERIFICATION.md), then completion disposition in
[D3a](P8_CATALYST_DISPOSITION_VERIFICATION.md); the counts below remain its original tool-stage run.

## Outcome and authority

ITransformationDefinition/TransformationDef now require exact tool_ports alongside input/output maps.
The same named binding resolver, resource matching and selected packing metadata validate tools.
Complete reusable bills must fit each authored port, including multi-resource aggregate capacity.

The existing transient endpoint vector groups feed, reusable tool and first local operating demand
before startup acquires any prefix. Native batch feed spends only its required portion of a shared
actual receipt, leaving the original endpoint's exact reusable remainder. Transformer.SyncWip stores
original resource/port/endpoint tool references for active batch and continuous executions.

ITransformerTools.ToolStore and singleton tool snapshot endpoint/version are removed. ToolSources
and detached Sources/Receipts/Held facts follow the original bindings, not current port selections.
IResourceTransaction.ReadClaims uses the original guarded ClaimAccess/checkpoint/book to observe
actual records. No duplicate resolver, saved tool balance, reservation ledger or execution loop.

Completion conversion releases the actual original receipts. Cancellation retains all reusable holds
while any original input return destination is full, then returns the complete WIP and releases tools.
Read/queue/save validation checks original bindings, exact receipt resource/owner/token/direction/
amount, complete actual protection and orphan claims at WIP/original tool endpoints.

Core2.308.0/save146/replay1/transformer codec8/transformation-view12. Existing endpoint stock/service
codecs and sole GameClock unchanged. No old API/save/parser compatibility path. Native operating
inputs still require their original local inventory until their separate planned migration.

## Authored migration and thin proof

The revised [AuthorTransformationPorts.ps1](AuthorTransformationPorts.ps1) ran once:21 existing JSON
files,24 required tool maps and20 explicit native inventory aliases. It is authoring tooling only,
never an engine loader. Current source documents need explicit tool_ports, including {} for no tools.

The existing input-ports.json gains two explicitly authored recipes and two actual reusable pools;
the same master lists it and resource-port-common.json. No duplicated common resource/mode definitions.
Original proof PoolPort changes only an authored selection reference; actual EndpointPools books own
stock. Native commands own acquisition/movement/conversion and time. New test reuses public player
helpers rather than creating another simulation driver. Older tool consumers source-migrate and
compile only; their historical behavior was not freshly revalidated.

## Observed commands and attempts

Three dotnet build Beep.SimEngine.slnx --no-restore invocations:

1.7.06s:0 warnings,2 new-test errors (missing Kernel namespace import for EngineInfo).
2.4.67s:clean,0 warnings/errors after import correction.
3.5.34s:clean,0 warnings/errors after authored-capacity review correction/test extension.

ONE distinct NEW exact bounded case, two runtime attempts, both green:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "P8 tool ports: whole startup, original reusable holds and saved batch/flow release" --timeout-ms 15000 --termination-timeout-ms 2000 --output-chars 20000
```

- Initial workflow:154 passed,0 failed/aborted432ms; childPID28748 exited0, observed576ms.
- Same case extended for impossible individual/aggregate authored capacity:156 passed,0 failed/
  aborted389ms; childPID33784 exited0, observed522ms.
- Both stdout/stderr untruncated, no observer error, failed runtime or timeout.
- Executable inputs frozen2026-10-08 17:51:29 UTC before the final runtime; subsequent work docs only.
- No old/family/broad/full/Oil/platform suite. Build output is compile evidence, not regression proof.

## Scope and remaining requirements

New scoped proof: private/pure tool port views, shortage despite spare local stock, shared-resource
feed/tool whole admission without prefix, timed three-second production, exact reusable remainder,
changed current tool selection without replacing original holds, refused unrelated tool spending,
full returning retention, saved supplying/working/returning/completion/continuous-stop twins,
immutable old facts, no jig consumption, no local feed/tool fallback, replay, and seven invalid maps/
capacity variants before Update. Both reusable resources retain actual stock until ordinary completion
or stop, never become fabricated disposable materials.

Named multi-endpoint operating inputs, generic utility consumer migration, catalyst wear/disposition,
entity tools, scoped physical automatic supply/export, dynamic endpoint disappearance/recovery,
malicious-provider/global purity, full accounting/lockstep/stress and P8/P1–P12 acceptance remain OPEN.
This proof does not certify actual dynamic tool disappearance or the broader corrupt-provider matrix.
No primary acceptance checkbox or full phase was promoted. No staging/commit/push: publication awaits
the full goal AND plan being genuinely finished and verified.
