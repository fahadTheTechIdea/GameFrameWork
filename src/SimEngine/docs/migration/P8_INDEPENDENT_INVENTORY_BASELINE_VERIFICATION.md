# P8-IB1 — Independent inventory transformation baseline

> Later P7-IS1 on unchanged2.661 directly resolves the separate ExternalSalvage fixture gap mentioned below: [all salvage attempts and scoped proof](P7_INDEPENDENT_SALVAGE_BASELINE_VERIFICATION.md). This document's2.660 transformation observations are preserved historical; they are not silently rerun or broadened.

2026-10-18. Unchanged2.660.0/save160/replay1. [Design](../../plans/enhancements/P8_INDEPENDENT_INVENTORY_BASELINE_DESIGN.md).
Original221/209 recorded checked/12 open; full goal ACTIVE, broad P8.017 OPEN.

## Actual cause and correction

Original combined case reproduces75/16 on the current completed build. The
CapabilityTests.IndependentFactory string replacement patterns no longer match
formatted colony.json. Thus it actually selects native Storage/initial1 and no
resource output, not the asserted independent inventory/initial30/output bill.
The cascading materials/output/stock/vehicle/cancellation failures are not proof
of a runtime provider regression. No old-build A/B causality is claimed.

Only CapabilityTests.cs changes executable source. Its existing helper now parses
actual Colony.Defs, selects unique factory/build_truck IDs, requires the original
storage owner, removes it, and explicitly authors the EXISTING independently
compiled SingleResourcePort (steel/count40/initial30/private/non-evacuating).
It authors the requested bill and one material.output route per resource. Named
input/output aliases resolve to the SAME actual inventory, never a second buffer.
All other factory/recipe fields, actual3h duration/input10, current rollout and
producer-home policy are retained. No scheduler/runtime/content loader change.

First corrected-input execution41/1 correctly refuses absent explicit output
ports. The actual engine already requires every output resource to name exactly
one port; that strict validation is preserved. Likewise permanently incompatible
water on a steel-only output port is correctly rejected BEFORE initialization,
not accepted into obsolete late-blocked gameplay. The existing incompatible
case now proves that actual refusal, repeated against the same factory, using
explicitly master-listed ProbeModule/InitializeProbe. A valid control initializes
the witness once; invalid content initializes0 times, advances no time, changes
no actual stock/vehicles in another valid session and leaves it hash/save durable.
No invalid world/partial compatible result is published.

Cancellation remains runtime-tested rather than removed: a saved clone of the
VALID capacity-blocked execution receives ordinary CancelTransformationCommand.
Existing TestKit.RunUntil fails explicitly on an unmet bound (source inspected),
so its queue really drains within one hour. Exactly original10 raw inputs return
to the same inventory (20->30), no resource output/extra truck appears, checkpoint
roundtrips, and the original blocked world's hash is unchanged. The original
then releases its actual inbound reservation, delivers all12 output units and
ONE truck together (stock32), and never repeats delivery on later updates.

The same original case retains native sequential/automatic production, independent
timed/repeat provider behavior, original gates, private immutable facts, active
saved twins, malformed checkpoint refusal and provider faults. No assertion is
changed merely to tolerate a failed valid output; static invalid content now
exercises its actual required earlier boundary.

## All builds and attempts

Working directory C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.
Each selection used SAME existing literal, in a separate process:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Transformations: timed native and independent providers with saved continuation' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 14000
```

| Attempt | Passed/failed | Worker ms | Outer ms | Outcome |
|---|---:|---:|---:|---|
|Current pre-edit baseline|75/16|3028|3188|exit1, all failures retained|
|Structural input, missing current port bindings|41/1|414|574|exit1, correct early authored refusal|
|Explicit ports/static refusal/runtime cancellation|98/0|2651|2856|exit0|

All output untruncated. Two target builds used
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v minimal`.
First14160ms MSBuild,0 warnings/errors, yielded live handle13167 which was awaited
to terminal BEFORE runtime. Second8320ms MSBuild/9141 command,0 warnings/errors.
No restart of a yielded process, no tests against an incomplete build. Exactly
ONE original selected case, no full/profile/family or separate capability suite.
Both affected standalone capability methods are invoked by that existing case;
they are not counted again as separately executed tests.

## Final identities — SHA256

Unchanged core448 sorted C# path/hash LF digest:
c040844f0330971456642a30853e775e29734dbcb085310761f422ad3737f622.

| Artifact | SHA256 |
|---|---|
|Core DLL and Tests copied core|E5BB145B65A3215986A8A1D7648E58A02A933FEDC72C87440391454C1947D742|
|Final Tests DLL|87B68C36FBEB9244671BBE70A8254AAE9C90BB379FD801D1CF5361BE2ADCB8E1|
|Unchanged ExtensionProof DLL|176AC1FABA59969AC768F7CE59E0E7644C59D181979C3E9601BF71F20CF01CFE|
|CapabilityTests.cs|71F2291EC39C26D9073ADA4A2A0B021DC18C70B3B561A2382FE808FE2DC3FB8C|

## Scope, remaining work and hygiene

No production engine, shared gameplay JSON, Oil, business, platform/Draw or
consumer build changes. Consumer still2.659; NO compatible2.660 consumer/native
checkpoint or harbour performance improvement is claimed. Earlier artifacts are
preserved historical/incompatible, never header-rewritten. No repeat long day.

ExternalSalvage also uses the corrected shared helper but retains unrelated old
literal demolition edits; it was NOT executed and demolition acceptance is not
inferred. Other capability fixtures remain out of scope. Dynamic runtime provider
compatibility/fault breadth, conversion/disposition/accounting stress and original
P8.017/full goal remain OPEN. Other original dock/motion proofs are prior2.660
evidence, not rerun here or silently added to98 assertions.

Fresh original221 text/checkbox audit209 recorded checked/12 open, unchanged.
Final hygiene: framework diff --check exit0,35 existing LF warnings, no other
findings; both repositories staged0, no live bounded-exact/worker-exact processes.
No executable edits followed final observation. No staging/commit/push until
whole goal AND plan finish.
