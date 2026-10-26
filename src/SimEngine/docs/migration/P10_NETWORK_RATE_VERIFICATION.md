# P10-A2c — exact elapsed-time native network verification

## Current M136 — native observation and settled-boundary qualification

Native2.767/save161/replay2/network service6 unchanged payload. The original codec
observes the actually earned in-flight interval through flow, events and entity
updates from the one GameClock; no checkpoint can use that observation allowance.
The native contention11/1 and post-flow event1/1 defects are corrected. Five raw
corruption branches still fail terminally; existing rate Contracts was mechanically
migrated from old schema4/reusable-faulted-session/outer-phase assumptions to exact
current schema6, independent terminal branches and original scoped first-fault cause.
[All attempts, final case table and build4/build6 distinction](P7_TOPOLOGY_ACCOUNTING_REVALIDATION_VERIFICATION.md#current-m136-native-flow-observation-and-physical-contention--2026-10-23).
New read-only event2904/0, rate Contracts68/0 and unchanged slow physical45/0 are
scoped evidence, not the whole P10/full gate. Normal rate/eligibility/fuel/material/
time budgets are unchanged; no extra clock, flow store or scheduler.

## Historical A2c evidence — 2026-10-06

2026-10-06, framework checkout only. Scoped current-source evidence, not full P10/P1-P12 completion.
No Oil/Tycoon/host/demo/platform/Draw changes. Development lifecycle guidance kept one owner per
responsibility, explicit staged scope and focused verification rather than promoting historical counts.

## Clean revision and ownership

- Build **beep-simengine-2.126.0**, save **69**, **LineService schema3**, **infrastructure view4**.
  Other owner payloads unchanged. Remove scalar port_rate/resources content and ten-minute flow
  scheduling; no previous save/content reader or compatibility facade.
- NetworkDef uses existing ResourceRateSpec/IResourceRate with native quantities and explicit
  rational periods. Carried resources derive from resolved rates. Common native port content
  rejects duplicate network/resource legs; independent INetworkPorts still executes unchanged.
- Existing LineSystem runs flow each Update. Capture eligibility for every leg before any stock
  moves. Newly pushed stock cannot earn a receiving leg's past-time service during that Update.
- Existing LineService owns sparse entity/direction/resource cursors bound to actual endpoints,
  combined quantum and last tick. A readiness gap or rebinding resets eligibility. Gate/stock/
  capacity/disconnection waits accrue nothing; unused whole allowance is not banked.
- Existing QuantizedGameRates derives cumulative whole before/after deltas with wide integer math,
  including rates smaller than receiver batch quantum. Only a single-step allowance narrows to Fix.
  Unrepresentable allowance produces sim.network.flow-range, never substitute quantity or stock.
- Actual ResourceTransactions receipts move stock and journal real transfers. No extra inventory,
  promise/credit ledger, scheduler, time service, solver or codec. The partial source file is the
  same LineService owner, not another subsystem.
- Existing Sync validates order/uniqueness, authored resources, endpoint shape, positive quantum
  and bounded past-time cursors. Next Update validates current bindings/readiness; saved cursors
  are past-time facts, not current stock/capacity guarantees. CopyFlow is detached and company-private
  even on explicitly public topology. No mutable engine escape is added.

## Final gates

`dotnet build Beep.SimEngine.slnx --no-restore`

Success, **0 warnings/0 errors**, MSBuild **2.33s**. Both gates below used this completed build.

| Exact suite | Passed | Failed | Final reported time |
|---|---:|---:|---:|
| Network rates: authored native periods, contracts and private progress | 41 | 0 | 622ms |
| Network rates: slow physical legs, blocked intervals and saved continuation | 45 | 0 | 1430ms |

**86 checks, two suites, zero failures/aborts.** Repair reruns retained these exact names; no third,
family or full suite ran. Bounded Update loops and engine fault diagnostics avoid indefinite waits.
Tests author JSON/input, use actual engine contracts/commands, drive Update and observe real owners;
cursor corruption and provider mutation probes instrument actual internals, not copied game rules.

Contracts cover explicit rates/units/periods and malformed/obsolete content refusal; duplicate legs;
native and independent providers; private/public-neutral detached timing; save/hash/view purity;
cursor shape/future/order corruption; mutation faults; typed overflow; larger batch quantum; native
mixed five-minute quantum execution and saved continuation. An explicit ninety-second network schedule
is accepted without exact ten-minute conversion.

Physical execution covers one count/hour and half fluid-unit/hour; independent receiving elapsed
time/no same-step feed-through; two-unit receiver batches; rational half-hour periods; normal gate
commands, pause, stock gaps, capacity promises, topology rebinding, real disconnection/reconnection;
no blocked-time catch-up; actual journal/conservation and saved/interleaved twin identity.

The attempted ninety-second mixed scenario refused during composition at the unrelated
LogisticsMaintenanceSystem exact five-minute schedule. That runtime rule was not weakened. The
ninety-second test now proves only network scheduling; actual mixed execution proves a five-minute
quantum. This is not arbitrary-quantum composition proof for every installed module.

59 preceding build assertion files and57 save assertions, three existing JSON network fixtures and
old network test timing/content assumptions were revised and compile only. Their historical gates,
including corridor construction and process/capacity tests, were not rerun. A compile success is not
current behavioral evidence for those suites.

## Limits and next work

Native elapsed flow only. Utility solver/allocation/pressure science, provider-neutral spatial
corridors, contention priority/fairness, global rollback/replay/fault matrices, soak/allocations and
full acceptance remain open. Rotating consumer order is deterministic, but transfer due times can
alias rotation under scarce intermittent supply; starvation freedom needs a separate authored
arbitration design and focused proof, not an assertion from these tests. Endpoint/flow guards here
are not global transactional rollback proof. No size cap, default gameplay, legacy or instant fallback.

## SHA-256 source fingerprints

| File | SHA-256 |
|---|---|
| src/Beep.SimEngine/Standard/NetworkFlow.cs | 558F0CA3B86F2F2908B046B18251E31F42A2C14F00C470D64E915D9BACE25173 |
| src/Beep.SimEngine/Standard/Networks.cs | 160521753EFEC087BDE0DC6EA9DF3347AA9F435681E8CCF2C890A8FACB8E9790 |
| src/Beep.SimEngine/Core/QuantizedGameRates.cs | BEEDACEF814EEF6379502C7ECFDE869C342AD5786928764BF10F77185DFC8C2C |
| src/Beep.SimEngine/Kernel/SaveGame.cs | 5E257CA6B5F1AE3E57BF5F2B27F6AF7B58E7B6F999FB9CB5E762A3279C19AD93 |
| tests/Beep.SimEngine.Tests/NetworkRateTests.cs | 49E4E406AA97213FC65455E601F9949C0051D895B6D15B65E0859583FDEC2C62 |
| tests/ExtensionProof/content/network-rates.json | 125CC0DB1239B71BED6AAE454F75F5B1942792268E628BB1938EB4C86760296F |
