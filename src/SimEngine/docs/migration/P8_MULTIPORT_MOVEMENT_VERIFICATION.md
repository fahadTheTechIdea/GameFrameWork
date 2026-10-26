# P8-D2a complete multi-port movement — focused verification

Date: 2026-10-08. Canonical repository: `Beep.SimEngine`. Goal and full P1–P12 remain open.

## Outcome and authoritative owners

The existing public `IResourceTransaction` now admits/commits a complete movement array. Singular
calls delegate to the same implementation. `ResourceTransactions.PrepareMovements` validates the
complete request, normalizes actual endpoint/owner vectors, checks actual source references and
aggregate stock/inlet capacity, then acquires all generated claims before calling `CommitTransfers`.
Converging inputs and split returns can share actual receipt tokens within their held quantity.
Preparation rejects duplicate receipt pairs, conflicting token metadata/direction, aggregate overuse
and overflow. Exact deltas/checkpoints/deferred facts remain in the original transfer owner.

No stock/promise service, scheduler, loop, synthetic compensation or mutable copy was introduced.
No JSON process route was fabricated. Existing `TransferProbeCommand` carries the whole array and
delegates to this owner through normal player Check/Apply; its old singular field was removed and
four existing movement call sites migrated. Native source/tool/utility JSON and cancellation routing
are the next migration, not implemented by this prerequisite. This is a low-level stock protocol,
not authority to skip machine travel/handling: actual gameplay callers still own physical timing.

Core2.306.0/save144/replay1/transformation-view10. Persistent stock/queue/service codecs, authored
game JSON and sole GameClock are unchanged. Breaking engine build rejects old saves/peers/replays;
there is no old command reader or compatibility path. The command-array proof uses selected JSON
definitions and explicit authored variants of the existing vessel game, not test-owned rules.

## Commands and observed attempts

Two builds, both `dotnet build Beep.SimEngine.slnx --no-restore`, clean:

1. Initial implementation/proof compile:12.04s, zero warnings/errors.
2. Completed proof coverage compile:5.75s, zero warnings/errors.

ONE distinct NEW exact bounded case, two runtime attempts, both green:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "P8 movement envelope: mixed ports, aggregate refusals and saved convergence/split" --timeout-ms 15000 --termination-timeout-ms 2000 --output-chars 20000
```

- Initial proof:97 passed,0 failed/aborted296ms; childPID29848 exited0, observed446ms.
- Extended same case after adding replay/mixed-bill/overflow/absent-array checks:116 passed,
  0 failed/aborted378ms; childPID37108 exited0, observed549ms.
- Both stdout/stderr untruncated, no observer error or timeout. No failed runtime attempt.
- Executable inputs frozen **2026-10-08 17:00:57 UTC** before the final attempt. Later edits only
  maintain documents and their evidence inventory; no subsequent executable changes or runtime.

## Covered behavior and boundaries

Public player commands/views prove mixed solid/liquid movement; no-prefix full-port refusal;
same-resource convergence/split with one aggregate receipt; complete inlet capacity refusal;
actual protected source overspend/stale-reference refusal and exact surviving token/amount;
saved pending commands, partial-receipt continuation, caller-order-independent hashes/history;
canonical recorded replay; two-resource native bill; malformed/duplicate/missing/aggregate-stock/
overflow refusal; absent command-array preservation through save; private flow history.

The driver uses no live World, direct stock/reservation writes, substitute Update or progress rule.
All prior cases only compile: none were run here, including the D1 output case (its last behavioral
proof remains2.305.0). The full baseline, malicious/global provider purity matrix, optional purchases,
lockstep, stress/soak/allocation and all-module accounting are NOT certified by this case.

P8 input/tool/utility authoring and execution, catalysts, dynamic endpoint recovery and physical
scoped supply/export remain OPEN. No full phase checkbox was promoted to Done. No staging,
commit or push: publication requires the full goal AND plan genuinely finished and verified.
