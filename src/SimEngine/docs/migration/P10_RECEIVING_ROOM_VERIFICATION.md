# P10-B8 — actual receiving-room backpressure evidence

2026-10-06; local dirty worktree, not a clean-HEAD or full-regression claim.

## Current identities and commands

Build **beep-simengine-2.140.0**, save **76**, inventory signal schema **3**, network signal schema **2**.
Component persistent layouts, LineService4, automation4, operations view8/logistics view15 unchanged.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Resource signals: inventory and network contracts and private facts"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Resource signals: inventory and network controls and saved continuation"
```

Final build:1.91s MSBuild, zero warnings/errors. Exactly two complete suites:

| Suite | Passed | Failed | Time |
|---|---:|---:|---:|
| Resource signals: inventory and network contracts and private facts |264|0|594ms|
| Resource signals: inventory and network controls and saved continuation |183|0|632ms|
| Total |447|0| |

No aborts. Initial gate260/169 also passed before strengthening mixed-resource/deferred-rule proof;
it is superseded by the final447, not additive. Includes preceding inventory/network-signal cases,
not all automation/stock-band suites. Other suites compile only; preceding B7e1069 was not rerun.
No third/family/full suite run. Scoped git diff --check passed with only an existing CRLF notice;
explicit source/docs whitespace scan also passed for untracked files rather than relying on Git alone.

## What the evidence proves

- ResourceQuantitySnapshot requires explicit CapacityOffer facts; no old constructor/default loader.
  Inventory/network JSON schema references and exact build assertions migrate in place.
- Native mixed inventory and unrelated public-only SingleResourcePort derive actual receiving room.
  Existing reservations reduce room; outbound promises do not remove held stock. A fluid commitment
  reduces supply room through the original shared mass policy, not a per-resource fake capacity.
- Positive room must satisfy native quantum/unit and actual CheckIncoming; malformed negative,
  fractional, over-capacity, mutating Offer/check or unknown status faults. Known unavailable capacity
  withholds room with resource.signal-capacity-unavailable, while valid stored quantity stays readable.
- Known full room is available zero; exact pair conversions work and odd discrete pairs refuse.
  Private room reveals no value; pending/disconnected/unsupported network facts retain existing blockers.
- Actual connected pools include stock and promises from the same LineService inventory. Native and
  independent network ports observe the same pool; detached facts/save twins remain isolated and pure.
- Authored numeric signal_network_gate stops flow with two units of unusable remaining room, waits
  through updates, then resumes after a real transaction drains stock. An inbound commitment remains
  reserved. Resume earns one normal four-unit allowance, never accumulated blocked time. Source/pool/
  delivered material conserves30. Saved interleaved twins hash identically; pause freezes game time/state.
- Authored below-threshold rules use receiving-room hysteresis, not stock-level assumptions. Ordinary
  ConfigureAutomationCommand binds an independent source; normal deferred valve commands shut/reopen
  flow one update later. Pending saved decisions/physical withdrawals continue identically and conserve30.
- GameClock source remains unchanged in this slice. It is still the single time owner; no controller,
  timer, graph, inventory, registry, queue or codec was introduced.

## Fingerprints at final source verification (SHA256)

| File | SHA256 |
|---|---|
| Standard/ResourceQuantitySignals.cs |1E4619E4D65C758970D0CB1E946A79C8869DE33ADE2127DE6D42BA47973E6BCE|
| Standard/InventorySignals.cs |04AC517FB88749BCC1B1FD7DE5F087AAD2D7470106B0A1AD034E08268D689CB9|
| Standard/NetworkSignals.cs |53732AE64BEE4F36B4465BB2080CD8F3C4DBF8B565C3CC7E955647C9C7E25CCF|
| tests/InventorySignalTests.cs |21D07157B807E6F4BE1EEAB38BCAB22B4E3ECA11569682A3458179B9DFB4B904|
| tests/NetworkSignalTests.cs |4EDB8461E9AABCFDFF5193739E2561584BF4D523B87D1AFE6552387D42144919|
| Hosting/GameClock.cs (unchanged) |339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31|

## Remaining limits

Resource room is not electrical/thermal/pressure allocation or IUtilityConsumer satisfaction.
Graph-independent utility providers, provider/global rollback/replay/lockstep/fuzz/soak/allocations
and full P10/P1-P12 acceptance remain open. No Oil/Tycoon/host/demo/platform/Draw changes.
Provider guards are correctness contracts for trusted modules, not a security sandbox or global rollback.
