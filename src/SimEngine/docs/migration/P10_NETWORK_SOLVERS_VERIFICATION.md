# P10-A4 — authored resource-network solver verification

2026-10-06, framework only. Focused source evidence, not full P10/P1-P12 completion.
No Oil/Tycoon/host/demo/platform/Draw changes. Lifecycle guidance preserved existing single owners,
explicit staged scope and two directly affected gates rather than historical green claims.

## Source and ownership

- Build **beep-simengine-2.128.0**, save **70**, LineService **4**, infrastructure view **4**.
  Existing state layouts remain unchanged. No old content/save reader or default solver exists.
- NetworkDef requires explicit JSON solver resolving registered open NetworkSolverDef/INetworkSolver.
  Native resource_transfer_network_solvers and independent proof content use the same engine seam.
  Missing, wrong, unknown and obsolete references fail. ValidateTime rejects authored intervals
  incompatible with the actual session quantum before Update.
- LineService retains graph, eligibility, ranks, pools, receipts, history and Sync ownership.
  All requests derive from existing captured due legs; selected solvers cannot bank unused allowance.
  Actual ExecuteFlow rechecks stock/capacity and commits the existing verified native receipts.
- Context exposes immutable ranked requests, endpoint identities, earned allowance/quantum and
  CompletedAt/Time. CompletedAt reads the one game clock; Time is the same immutable converter,
  not an advancing clock. Requests execute once, require positive bounded exact-quantum amounts,
  and cannot cross context/network/session boundaries. Context expires in finally, including faults.
- Known stock/capacity/receipt refusal returns typed zero without rotating successful history.
  Recursive/repeated/foreign/invalid requests and captured participant mutation fault execution.
  Only exact owners changed by successful verified transfers may refresh guarded baselines;
  refused attempts cannot conceal unrelated mutations. Earlier committed transfers are not rolled
  back by a later provider fault; faulted games cannot save. This is not a security sandbox.
- Five existing authored network fixtures now name explicit native solvers; earlier build assertions
  migrated and compiled only. New provider JSON is explicitly master-listed. Public-only proof
  modules implement actual extension contracts; tests drive Update/commands/observe, not copied flow.

## Completed verification

Clean completed `dotnet build Beep.SimEngine.slnx --no-restore`: **0 warnings,0 errors**, MSBuild3.55s.
The completed build preceded both final focused suites; no stale binaries or concurrent build/test.

| Exact complete suite | Passed | Failed | Reported time |
| --- | ---: | ---: | ---: |
| Network solvers: explicit contracts, lifetime and guarded execution | 63 | 0 | 444ms |
| Network solvers: physical selection, refusal and saved continuation | 51 | 0 | 401ms |

**114 checks, two suites, zero failures/aborts.** Repair reruns used only these same two names.
No earlier, broad-family or full regression suite was run. Earlier gates are historical, compile-only.

Contracts cover explicit selection and invalid content, actual session time validation, detached
read-only metadata, expired/invalid/duplicate requests, before/after-transfer mutation, cross-network
and cross-session request rejection, expiry on solver faults, recursive transfer and mutation during
a refused inlet check. Instrumentation uses the existing real public port/provider contracts.

Physical proof covers native and independent JSON-selected capped/pulsed execution, unbanked skipped
allowance, actual native/independent receipts and stock conservation, capacity and inlet refusal,
paused sole clock, operation-gated intervals, saved interleaved continuation, multiple native resource
dimensions and two networks sharing a physical source. Scenarios use bounded Update counts.

During repairs, the probe was corrected to use actual ReleaseIn rather than an invented method.
An authored-time conversion error was changed to explicit ContentException at provider validation;
no default interval or exception-tolerant runtime fallback was introduced.

## SHA256 source fingerprints

Paths are relative to the framework root; hashes identify the verified source, not a Git commit.

```text
src/Beep.SimEngine/Standard/NetworkSolvers.cs E82D44F86AAB4804712F5A809C440A129DF3D05A3D272687A13B18B99928CFC3
src/Beep.SimEngine/Standard/NetworkFlow.cs 47133569FFF68D4068E06E8B9840611F54872A287827B4CEA3CA81A47BD4E46D
src/Beep.SimEngine/Standard/Networks.cs 285DE9C4A6DE1A79A3047EC8C33ABCFA3551F637952B0BA8577BB9C8ABA002A5
src/Beep.SimEngine/Kernel/SaveGame.cs 1F52CCA2C71843F3934A1C45E2643B47DB681CC9DC0E7EA163D24BF0E43707D3
tests/Beep.SimEngine.Tests/NetworkSolverTests.cs F53ABA40134B3E9FA249BBAFCB774B12ECFC8DC013AC1B2FAEF7A1BFEBD75856
tests/ExtensionProof/NetworkSolverProof.cs 27DF4C862153E09733B94511F45E741E98DE1FC06F557AB15A983C2B8F4372B2
tests/ExtensionProof/content/network-solvers.json 2D624BC3ED1225A76112A707584984CE27F239B265023C252AF32052CCC0D3B3
tests/ExtensionProof/ContractCapabilities.cs F1F45F4E5CC7C247E2509FECC1681BF04FC8BD3B9330DEBB52F849738432F151
```

## Remaining scope

IUtilityConsumer, pressure/power/thermal allocation, provider-neutral graph construction, cross-resource/
network fairness, runtime player policy editing, global rollback, replay, third-party determinism,
stress/soak/allocation budgets and full P10/P1-P12 acceptance remain open. Solver callbacks are trusted
engine extensions guarded against captured participant violations, not unrestricted-world rollback.
Native candidate eligibility remains the existing resource-flow phase, not a general utility budget.
No extra current-time owner, inventory, graph, scheduler, persistent request queue or codec was added.
