# P4-G5a — Exact activity work diagnostics

## 0. Problem and specification

WorkView exposes node counts and one current node, but a host cannot distinguish parallel work
from sequential work without recreating dependency analysis. DependencyGraph already owns captured
topology; WorkProgress and the actual Order nodes already own earned work. Use those authorities.

Success: company-private immutable views expose exact total and remaining nominal work and a
deterministic critical unfinished work path, survive ordinary saved continuation, and never advance
the game. No new scheduler, progress store, clock, graph compiler, JSON schema or host gameplay.

## 1. Design and API

DependencyGraph<T>.CriticalPath accepts an exact weight for every captured node. Nonnegative
BigInteger weights avoid narrowing aggregate work to Fix or long. Iterative topological analysis
uses captured edges; equal predecessor/terminal choices use ordinal identity. A path retains zero
weight connectors between unfinished nodes but trims zero-work prefixes/suffixes. All-zero returns
an empty path. Missing, extra or negative weights refuse; no fabricated defaults.

WorkProgress.RemainingRaw(requiredTicks) computes exact remaining Q32 work and refuses overshoot.
IActivityGraph.AnalyzeWork(time, progress) requires the exact node set and returns detached totals
and the same compiler's path. WorkView.TryReadProgress(orderId, out ActivityProgressView) authorizes
before reading actual Order progress; missing/foreign orders disclose nothing. The immutable view
also includes actual order state/blocker, settled node count, unfinished node count and earned-but-
unsettled node count. Zero remaining work does NOT mean physical departure/effects have settled.

Units are Q32 work per current simulation quantum, NOT wall-time ETA. Rate, travel, resource waits,
contention and effect retries are not inferred. Host reads no live provider/runtime record.
The sole game clock and existing save/Sync bytes remain unchanged. Strict build2.244.0/View80.

## 2. Files, errors and compatibility

Modify Kernel/DependencyGraph.cs, Core/WorkProgress.cs, Standard/ActivityGraph.cs,
Views/WorkView.cs, Standard/WorkModule.cs, Kernel/SaveGame.cs, test registration and existing
build-identity assertions (compile-only). One new bounded player case reuses existing explicitly
master-listed authored segment content. No compatibility reader, default weight or old API shim.

Null/mismatched weights or progress, negative weights, negative duration and excess earned work
refuse explicitly. Numeric diagnostics never reserve stock, dispatch machines or evaluate effects.
No Oil/host/demo/platform/Draw/business changes.

## 3. Verification and execution order

1. Implement exact numeric projection and same-compiler critical path.
2. Publish private immutable actual-order view.
3. ONE distinct NEW bounded case: parallel authored physical work, holding machines, actual earned
   segment prefix, save/load twins, read purity/privacy and detached values; exact fractional/wide
   numeric boundaries and deterministic ties. Every build/run recorded; repair observed failures only.
4. After final green freeze source/public proof/tests/JSON; docs-only tracker/evidence inventory.

No older/family/full/platform/stale gates. Earlier metadata/fixtures compile-only. Broad replay,
provider/fault, ETA, performance, all generated workflows and full G5/P1-P12 acceptance remain open.
Full goal remains active; commit/push only after FULL completion and verification.

## 4. Verified scoped checkpoint,2026-10-08

Implemented build2.244.0/save139/View80; no persistent bytes changed. TWO clean builds13.32s/4.87s.
TWO invocations of SAME exact NEW case:58 checks/1 suite aborted498ms because no-dock native work
can finish and retire in the last productive update. Correct only that lifecycle assertion:
completed actual records pass through same graph analysis and the live view refuses retired IDs.
Final72 checks/0 failures/aborts484ms.110 older assertion files/lines source/compile ONLY.
Source/public proof/tests/JSON frozen after green; docs-only afterward.
[Evidence](../../docs/migration/P4_ACTIVITY_WORK_DIAGNOSTICS_VERIFICATION.md).

EarnedUnsettledNodes classification is implemented, but a live dock-departure/effect-retry wait
with zero remaining work is NOT runtime-proved by this no-dock case. Keep that acceptance open
alongside ETA/provider/fault/replay/stress/CPU/all generated workflows/full G5/P1-P12.
