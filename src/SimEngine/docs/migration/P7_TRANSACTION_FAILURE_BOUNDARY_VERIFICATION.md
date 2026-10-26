# P7-C10 original transaction failure ownership — focused evidence

Date2026-10-15. Engine2.603.0/native save159/replay1. Native persistent payloads,
module schemas, resource books and the sole clock are unchanged.

## Implemented

All23 IResourceTransaction methods now own unexpected execution failures, rather
than depending on the caller eventually reaching World.Update's exception boundary.
Twenty-two original bodies receive ordinary try/catch after their existing terminal
guard. CommitInventory retains its prior specific boundary. One private method on
the same ResourceTransactions class supplies fallback latching; World.Fault remains
the only fault state. Existing observation faults and specific inventory phase retain
precedence. Exceptions rethrow unchanged; no healthy partial continuation or save.

The three partial files keep original method names and algorithms. This adds no
delegates/closures to hot entry calls, wrapper service, legacy core method, second
transaction implementation, balance, receipt store, scheduler, clock or compensation.
Normal typed refusals remain healthy; invariant/provider failures become terminal.

## Reused proof and actual prefixes

The new literal reuses EndpointGame's master-listed content, ResourceClaimTests.Seed
and original ClaimFaultOutlet over ResourceInventoryState. The seed and previous
cutoff's argument helper are shared internally, not copied into a new fixture.

Six real direct operations run in both new and genuinely restored worlds:

- Query: a pure outbound check creates an unexpected actual reservation.
- Acquisition: the first endpoint acquires its claim; the second creates an extra
  inbound reservation in addition to the requested outbound promise.
- Consumption: actual stock/receipts are consumed, but the second provider reports
  a short amount.
- Release: the first receipt is released; the second provider ignores release.
- Movement: actual source decreases and destination increases, but receipt return
  is short.
- Conversion: actual input is consumed/output inserted, but output reports short.

The final proof explicitly verifies each actual stock/book prefix, rather than
assuming a refusal exercised mutation. No fabricated rollback is attempted. The
first error is latched, no unverified success fact is pending, provider resolution
does not repeat, Save/Hash refuse, Update earns no time, and another session is
unchanged. Provider faults are armed only after healthy setup/load.

Structural reflection confirms exception boundaries on all23 current interface
implementation targets. Separately, all23 methods receive ordinary invalid input
in a healthy world: typed InvalidRequest/InvalidReceipt, no fault, no hash/fact change.
These are bounded contract checks, not exhaustive provider-shape verification.

## Commands

Build only the test project and its references:

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
```

Each runtime uses one `--bounded-exact` literal through the test executable, with
`--timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 7500`:

1. Resource transaction failures: direct provider violations are terminal at the original owner
2. Resource transaction cutoff: every contract entry stops before caller or provider work
3. Resource lifecycle commit: terminal provider violations and verified publication
4. Atomic hauling: common quantities, all-participant refusal and exclusive carrier acquisition

No full/profile/family or paid/Tycoon runtime.

## All attempts

| Attempt | Actual result |
|---|---|
| New proof against old engine | Clean19.81s build; session80459 terminal exit0 |
| New literal RED | 51passed/92failed,512ms; PID22116 exit1/749ms. All12 real direct failures lacked owner latching, providers could repeat, partial state could save/hash and time advanced. Missing exception boundaries also reported. Stdout tail truncated at7500; aggregate totals retained. |
| Original-method failure boundaries/build identity | Clean14.23s build; session81659 terminal exit0 |
| New literal first GREEN | 143/0,894ms; PID23508 exit0/1316ms |
| Existing cutoff FINAL | **326/0**,2256ms; PID28832 exit0/2907ms |
| Existing direct commit FINAL | **306/0**,2171ms; PID32076 exit0/2966ms |
| Existing physical atomic-haul FINAL | **587/0**,2713ms; PID11880 exit0/3068ms |
| Healthy all-method refusal assertions | Clean11.83s build; session87312 terminal exit0 |
| New literal healthy-refusal addition | 212/0,1488ms; PID16668 exit0/2038ms |
| Explicit actual-prefix assertions | Clean15.51s build; session75416 terminal exit0 |
| New literal FINAL | **224/0**,638ms; PID32496 exit0/874ms |

Four builds, all zero warnings/errors and terminal before runtime. No watchdog
timeout, process restart or executable edit after FINAL. Only the affected new
proof reran after final proof-only assertions; the other three literals' production
and tested algorithms were unchanged. Four passing literal cases1443 checks at
this checkpoint (224+326+306+587), not a full regression total.

## Remaining scope

Structural exception coverage is not proof that every possible silent provider
corruption is detected in every healthy path. Arbitrary CLR behavior, all content/
financial settlements/creation results, all optional modules, mixed stress, shuffled
lockstep/replay, soak and allocation/performance remain separate gates. No healthy
gameplay rules or Tycoon amount/formula was changed. Other-session hash isolation
here is supplemented only by the specific live interleaving in the rerun cutoff case,
not a claim for every optional module.

Original26 recorded open gates/full objective remain open, including compound P7
tool/dock/graph/inbound/physical-batch and broader spill/conversion/topology/fault
gates. No original checkbox or frozen-audit rewrite, full/profile/family run,
Oil/Tycoon/host/platform/renderer changes, staging, commit or push.
