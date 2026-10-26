# P8-D2d operating ports — focused verification

Date: 2026-10-08. Canonical Beep.SimEngine only; full P8/P1–P12 and goal remain OPEN.

Historical2.309.0 proof; not rerun on2.310.0. The same authoring script is now revised for completion
disposition in [D3a](P8_CATALYST_DISPOSITION_VERIFICATION.md); operating counts below remain its original run.

## Outcome and shared authority

ITransformationDefinition/TransformationDef require exact operating_ports JSON alongside existing
input/output/tool maps. The generic IUtilityConsumer.ResolveUtilityPort(World,ResourceId) replaces
UtilityStore; UtilityDemandSnapshot.Ports replaces singleton endpoint/version facts. Resources is
derived from canonical detached per-resource port facts, not a utility stock model or saved balance.

ResourcePortRoute/Binding/Snapshot generalize the former transformation-specific primitive names.
ResourcePortContracts is the sole extracted guarded actual binding/fact owner for both consumers and
transformations. It checks real named capability, prototype packing, actual endpoint/company/version,
compatibility and immutable actual stock facts. UtilityContracts verifies each selected resource's
native quantity and unpromised availability. NetworkFlow matches the resource-specific destination;
generic signals/work requirements continue to consume the same utility demand contract.

Existing worker/motion OperatingInputs providers expose their current named operating capability as
an actual local IResourcePort over the same inventory. Their original phase, exposure and resource
consumption owners remain unchanged. Independent proof consumers migrate to the same contract.

Native processor startup groups actual feed, reusable tools and first operating draw before admitting
any prefix. Batch operating draws acquire/consume complete actual claim sets under a derived owner,
distinct from reusable holds; no new counter or reservation book. Continuous feed/operating draw and
output remain in the original complete conversion. Native per-port ceiling rates derive from the
same sole running-time counter. A processor with explicit external ports needs no entity inventory.

Shortage, disable and full outputs earn no exposure and burn no partial draw. Cancellation returns
raw WIP to its original sources and releases original tools, never refunds already consumed utility.
Remote automatic supply remains explicitly unavailable until the existing physical logistics path
supports scoped destinations; no fake local delivery, teleport or compatibility fallback was added.

Core2.309.0/save146/replay1/transformer codec8/transformation-view13/operations-view11. Save and native
state codecs remain unchanged because this slice changes content/contracts/projections, not durable
layout. The sole GameClock is unchanged. No platform Draw, required money/Tycoon or second loop.

## Authoring and player-only proof

[AuthorTransformationPorts.ps1](AuthorTransformationPorts.ps1) was revised for operating authoring
and ran once:21 existing JSON files,26 operating maps and20 explicit inventory aliases. It is a
one-time mechanical authoring migration, never a runtime loader. Explicit operating_ports:{} is
required when no operating resources are authored. Older migration counts are historical stages.

The same master-listed input-ports.json gains an actual water-utility pool, two operating recipes and
an external-port processor with no storage/inventory component. Shared feed/tool/utility ore uses one
actual endpoint; feed water and operating water use separate actual native vessels. No duplicated
resource definitions or stock authority. The new test uses existing player command/view/Update
helpers; it never writes resources, progress, promises, lifecycle or live World state.

## Commands and observed attempts

Two dotnet build Beep.SimEngine.slnx --no-restore invocations:

1. 11.58s:0 warnings,4 new-test errors (Commands namespace and numeric JsonValue authoring API).
2. 4.68s:clean,0 warnings/errors after the test-only corrections.

ONE distinct NEW exact bounded case, FIRST runtime attempt:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "P8 operating ports: mixed timed draw, shared startup and inventory-free saved batch/flow" --timeout-ms 15000 --termination-timeout-ms 2000 --output-chars 20000
```

- 197 passed,0 failed/aborted578ms. ChildPID35544 exited0, observed718ms.
- Stdout/stderr untruncated; no observer error, failed runtime, repair rerun or timeout.
- Executable inputs frozen2026-10-08 18:20:14 UTC before that runtime; subsequent edits documents only.
- No older/family/broad/full/Oil/platform suite. Worker/network proofs compile only, not current runtime evidence.

## Proven scope and remaining acceptance

Scoped proof covers inventory-free processors, private/pure immutable operating and generic utility
facts, same-source feed/tool/utility whole admission, timed native solid/fluid draws, partial supply
without prefix, pause, output backpressure, distinct feed/operating projections, actual consumption
history, reusable release, no-refund cancellation, saved supplying/working/disabled/continuous twins,
canonical player replay, and six malformed map/capacity variants refused before Update.

Continuous accounting retains its existing aggregate conversion cause; snapshots derive the separate
feed/operating amounts from actual earned time. This is not a new per-purpose saved accounting ledger.

Catalyst wear/disposition, entity tools, scoped physical automatic supply/export, dynamic endpoint
recovery, provider/global purity, worker/network breadth, accounting/lockstep/stress and full P8/P1–P12
acceptance remain OPEN. No primary checkbox or full phase is promoted. No staging/commit/push; the
human publication condition requires the full goal AND plan to be genuinely finished and verified.
