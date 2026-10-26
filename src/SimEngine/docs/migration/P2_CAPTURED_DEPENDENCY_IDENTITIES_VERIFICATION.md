# Captured dependency identities — focused verification

Date:2026-10-08. Canonical repository:Beep.SimEngine. Full goal remains active.
Stage:[design](../../plans/enhancements/P2_CAPTURED_DEPENDENCY_IDENTITIES_DESIGN.md).

## Implementation and cause

Kernel/DependencyGraph.cs is the sole compiler used by activities, setup, requirement, condition,
technology, objective and plan validation. It previously captured dictionary keys/edges but analyzed
critical paths using live provider Id getters. It now reads each declaration once, stores canonical
NodeIds in its existing graph and calculates using those keys only. Find/Nodes retain provider aliases;
aliases do not redefine topology. No behavior provider, scheduler or runtime progress model duplicated.

Engine build2.304.0, save144, replay1; no persistent schema/layout change. The existing engine identity
and independently authored current-engine-protocol test baseline migrate. Prior-build saves refuse
through the normal exact-build rule; no shim. No gameplay JSON, business, Oil or platform edits.

## Actual commands and evidence

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- --bounded-exact "Dependency contracts: captured identity, canonical topology and external provider mutation" --timeout-ms 10000 --termination-timeout-ms 3000 --output-chars 16384
```

ONE build:exit0,12.02s,0 warnings/errors. The initially yielded build handle99031 was resumed, not
restarted. ONE distinct NEW exact case, FIRST attempt:35 passed,0 failed/aborted,21ms suite runtime.
Bounded childPID37608 observedExited/exit0/178ms; stdout/stderr not truncated; observer failure empty.
Executable source/proof/tests/baseline frozen2026-10-08 15:54:09 UTC. No executable edits after green.

External assembly proof reads public contracts only. It mutates original IDs/dependency arrays,
then throws if getters are reread. Canonical keys/edges, original provider aliases, exact BigInteger
weights, ordinal ties, detached immutable paths, new weight recalculation and original-key refusal
pass. Empty/duplicate/missing/cyclic graph behavior is covered in this same new case.

## Source comparison and limits

Read-only comparison against the previous wait-boundary manifest:358 prior core C# paths checked;
only DependencyGraph.cs and SaveGame.cs differ.343 prior authored game JSON paths unchanged.
Sole GameClock SHA256 unchanged:
`35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1`.
The extension project references only the framework; no proof friendship, reflection or copied code.

Old suites compile-only; none rerun. This mathematical contract case does NOT prove physical work,
private views, durable activity execution, provider purity/atomicity, every graph consumer, Windows
versus other-host behavior, replay/lockstep/fuzz/soak/allocations, Oil parity or full baseline success.
Current broad P2/P4/P6 and all remaining P1–P12 gates stay open. Full regression still needs explicit
user authorization. Commit/push remains deferred until the full goal and plan are complete.

Actual read-only git diff --check exits0 with34 existing line-ending messages.18 master phase rows
retain five columns. New proof/test scans find no reflection, live World or friendship access.
Repository path hashes and local Markdown links are recorded in the self-excluded
[source manifest](P2_CAPTURED_DEPENDENCY_IDENTITIES_SOURCE_MANIFEST.json); metadata is not runtime proof.

The lifecycle skill guided staged design and evidence separation; user focused-testing restrictions
override its generic full-regression step.
