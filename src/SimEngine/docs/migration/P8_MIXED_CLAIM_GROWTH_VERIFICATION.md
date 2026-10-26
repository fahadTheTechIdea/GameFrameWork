# P8-D3b1 mixed fresh/increase acquisition — focused verification

Date: 2026-10-08. Canonical Beep.SimEngine only. Full P8/P1–P12 and implementation goal remain OPEN.

## Outcome and original owners

The original ResourceClaimIntent explicitly selects Fresh or Increase. Increase supplies one exact
full actual outbound receipt and a single-resource additional Bill. Invalid original metadata never
falls back to fresh acquisition; fresh demands never silently merge into existing same-owner holds.

ResourceTransactions.PrepareClaims captures all endpoints before admission callbacks, validates all
original full records through PrepareClaimReceipts, and admits fresh and increased amounts through
the same aggregate native stock/capacity vectors. AcquireClaims applies canonically ordered native
ReserveOut/ReserveIn/IncreaseReservedOut, verifies every captured participant after each operation,
and returns the exact updated full receipt with the same original token. Endpoint books remain sole
authority. ExtendClaim now delegates to AcquireClaims; its separate growth algorithm is removed.

This is the required acquisition prerequisite for continuous catalyst replenishment. The engine can
now acquire fresh feed/utilities and increase original catalyst protection as one admitted set, then
consume only the added amount through D3a's exact disposition while retaining the original charge.
Continuous rate authoring/execution is NOT yet connected and remains the next source change.

Independent ClaimProbeCommand saves explicit kind, original full receipt and nullable additional Bill
through its existing engine-side command owner. There is no second handler, stock ledger or growth
coordinator. No content migration was needed: the new public-player proof reuses the existing
input-port master, authored resource units/capacity and native entity endpoint books.

Core2.311.0/save148/replay1/transformer8/transformation-view14/operations-view11. Save148 identifies
the revised pending claim command payload. Native stock/process/service layouts are unchanged.
Independent current-engine-protocol.json migrates build/save identity directly; no old payload reader.
GameClock SHA256 remains35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.

## Observed build and runtime

Two compilation invocations:

1. dotnet build Beep.SimEngine.slnx --no-restore: failed6.83s with two missing Standard namespace
   interface references in the NEW test. Native engine/proof/module projects compiled.
2. After importing Standard in that test, same build: clean3.81s, zero warnings/errors.

ONE distinct NEW exact bounded case, FIRST runtime attempt:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "P8 mixed claim growth: whole fresh/increase admission, original tokens and saved replenishment" --timeout-ms 15000 --termination-timeout-ms 2000 --output-chars 20000
```

231 passed,0 failed/aborted295ms. ChildPID7436 exited0, observed436ms. Stdout/stderr untruncated,
no observer error or timeout. Executable source/test/protocol inputs frozen2026-10-08 18:57:20 UTC
before runtime; later edits documentation/audit only. No older/family/full/Oil/platform runtime.

Readonly metadata audit:124 source anchors plus13 quickstart anchors, zero mismatches;221 original
acceptance records unchanged (149 historically checked/72 unchecked, not a completion percentage).
1,917 rg paths/633 Markdown documents/4,097 local links, zero broken links.18 master rows retain
five columns. git diff --check exited0 (34 existing line-ending warnings). New thin player/shared
helper boundary scan has no direct World/resource/progress writes. Clock hash above unchanged.

## Proven scope

The thin player submits ordinary commands and observes immutable views; all checks/mutations are
engine-owned. Actual full original receipts, later liquid shortage, shared stock contention from
fresh/increased claims, later inlet capacity, duplicate/fresh-overlap, stale/wrong-owner/resource/
direction/token/missing endpoint, empty/null/multi-resource growth, fractional discrete amounts and
representation overflow refuse without changed claim prefixes. Copied receipts never grant another
company access. Successful previews are pure; earlier copied receipt facts remain immutable.

Saved pending mixed commands preserve original tokens and full updated quantities. Partial actual
consumption leaves the original charge in the same token; saved continuation replenishes it with
fresh feed. Other-owner protection remains unchanged. Final release changes no physical stock or
consumption accounting. Native history distinguishes liquid and solid quantities. Canonical player
replay verifies the actual refused/successful acquisition, consumption, replenishment and release.

Build evidence is compilation only. This proof is not direct runtime evidence for the convenience
ExtendClaim caller or arbitrary provider faults/stress. Continuous authored rate execution, entity
tools, scoped physical supply/export, dynamic providers, global accounting/lockstep/stress/full
P8/P1–P12 acceptance remain OPEN. No primary checkbox or whole phase is promoted by this proof.
No staging/commit/push: the full goal AND plan must be genuinely finished and verified first.
