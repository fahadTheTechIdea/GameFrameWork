# P7-AR2 committed resource source verification

## Engine change

Core2.481.0/save154/replay1/work-view100. The existing WorkBoard resource evaluator skips non-original endpoints before stock and CheckSource queries when an occurrence is committed; initial alternatives still participate in stable candidate metadata validation and uncommitted source selection. Only the original candidate can affect live work. Every matching original candidate's result retains its actual status/blocker, including held and running policy refusal and typed missing-source.

An omitted original candidate is now missing source access, not an invalid receipt. It pauses the original committed intent and valid reservation instead of triggering lost-claim reopening and discarding identity. ResourceRefusal(InvalidReceipt) remains the actual lost-claim path; no fabricated receipt validation or new recovery/selection state.

WorkBoard.EvaluateRequirement now wraps its canonical algorithm in the existing EntityObservation.ReadProvider boundary over the actual target/worker codecs and selected space. Malformed result/candidate validation, provider exceptions and observed pure-query mutation latch the first engine fault before facts can be published. Public views and live callers share this entry. The private core method holds the original algorithm, not a second scheduler/evaluator/book. No save payload or clock changes.

The independent engine-side provider uses only public contracts and actual JSON: ResourceWorkRequirementDef/ResourceRequirementDeclaration, IResourcePort.ResolveStore, existing real PoolPort and existing AutomationProbe operation control state. It has no live codec or additional gameplay state. A deliberate exception on post-commit non-original checks verifies the production loop never invokes them.

## Builds and all runtime attempts

One solution compile, then focused project compiles only:

| Compile | Seconds | Result |
| --- | --- | --- |
| dotnet build Beep.SimEngine.slnx --no-restore | 4.63 | 0warnings/1error: new non-friend proof referenced internal ResourcePortContracts |
| Focused provider repair | 8.10 | Clean; public IResourcePort.ResolveStore used, no friend grant |
| Focused completed proof compile | 5.06 | Clean; spent-input proof added before any runtime |
| Focused embedded JSON packaging repair | 7.80 | Clean |
| Focused omitted-source semantics repair | 11.82 | Clean |
| FINAL focused canonical fault-boundary repair | 13.31 | Clean |

Focused command: dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore. All clean compiles have0warnings/errors. No solution rerun.

| Attempt | Checks passed/failed | Case ms | PID / process ms | Evidence |
| --- | --- | --- | --- | --- |
| 1 | 2/1 | 73 | 44464 /229 | New JSON omitted from explicit embedded-resource list; suite threw missing authored document |
| 2 | 1024/4 | 10392 | 17336 /10604 | Original omission falsely triggered lost-claim resource release; live criteria then displayed alternate; suite threw missing original binding |
| 3 | 1359/18 | 15818 | 45892 /16071 | Source preview exceptions escaped without first-fault latch; no healthy save/hash rejection; original-error assertion hit null fault |
| FINAL4 | 1455/0 | 16594 | 19292 /16848 | Exited0, no suite abort, stream truncation or observer error |

Every attempt used the same ONE NEW exact case; failed assertions/throws remain recorded. No older/family/full runtime. Clean final compile preceded FINAL; no executable edit afterward.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Committed activity sources: original policy refusal isolated from alternatives and malformed providers" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
```

Outer60000ms, termination5000ms, output24000chars; each physical stage bounded1000Updates with actual clock/node/progress/blocker/jobs diagnostics.

## Public-player proof

Thirty-two actual point workflows cover four source policies (Pending, Blocked, explicit MissingSource, omitted original), held/continuous resource uses, minute/half-minute quanta and completion/cancellation. Two additional spent-input workflows cover both quanta. The actual point master explicitly lists the new committed-work-sources.json through normal module composition; a project resource entry makes that file loadable. Content/modules own every rule.

Ordinary physical hauling supplies eight real units separately to each original and alternative endpoint; previews show both real uncommitted candidates and select the stable lower-cost source. Posting the authored twenty-minute activity commits the original source; live criteria contain only that original candidate. A normal operation-control command disables its authored source policy. Exact Pending/Blocked status and original cause remain visible, earned work and continuous consumption stop, original held token and actual bank promise survive, and the stocked alternative remains untouched. Omission also preserves the original receipt despite no offered original candidate.

Saved twins, private foreign refusal and the single paused clock retain the same state. Re-enable resumes and completes on the original endpoint, consuming exactly four continuous units or preserving the reusable held stock; cancellation releases the original claim without refunding prior consumption or creating a completion. Already-consumed startup input remains accounted to its original endpoint and needs no renewed source admission; disabling later cannot double-consume it or probe the alternate. Final saves/replay and untouched interleaved sessions match.

Nine independent malformed cases cover unavailable-plus-candidates, duplicate identity, negative cost, invalid endpoint, original enumeration throw, target revision mutation, clock mutation, malformed source check and original CheckSource throw. Each faults the actual session before healthy save/hash/early cached query; subsequent queries retain the first fault and another session stays unchanged. Original callback error messages are preserved.

The driver contains only normal seat previews/commands/results/views, Game.Update, saves/replay and clock controls. No World/TestKit/direct stock/job/progress/geometry writes. Negative mutations are declared engine-side providers, not host gameplay.

## Qualification

Original221/186checked/35open unchanged; additional208. No original checkbox/text/line changes or gate closure. This proves the affected source-policy evaluator path, not all worker-local/elapsed/network/graph/actual-invalid-receipt/provider foreign-state mutations, CLR sandboxing, concurrency, allocation/performance, fuzz, stress or full P7/P1-P12 regression. Guarding the broader entry does not prove all its other callers; earlier proofs compile only, not rerun.

Canonical SimEngine only; protected sole-clock/G19/Tycoon unchanged. No Oil, Draw/platform/adapters, staging, commit or push. Full goal AND plan remain incomplete.

## Metadata closure

Fresh read-only metadata closure:1707 source anchors,950 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2895 paths/1024 Markdown files/8663 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2883/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional208. Seven existing selected executable inputs changed; three newly created executable inputs/five new source files, four existing production sources changed/zero new. No previously existing input newly admitted to freeze. Work view100 records original-source/blocker/fault semantics. No runtime rerun for documentation validation.
