# P7-IS1 — Independent inventory physical salvage baseline

2026-10-18. Unchanged engine2.661.0/save160/replay1.
[Design](../../plans/enhancements/P7_INDEPENDENT_SALVAGE_BASELINE_DESIGN.md).
Original221 requirements/209 recorded checked/12 open; full goal ACTIVE.

## Current failure and actual correction

The original exact CapabilityTests.ExternalSalvage case fails0/1 BEFORE game
initialization: site:factory references a factory missing its construction
definition. Its old whitespace-dependent replacements do not match the canonical
JSON produced by IndependentFactory, so neither construction binding nor required
assembly input is actually authored. This is invalid test content, not demonstrated
failure of the runtime independent inventory or recovery implementation.

Only tests/Beep.SimEngine.Tests/CapabilityTests.cs changes executable source.
ExternalSalvage now structurally selects the actual unique factory, rejects an
already present site/construction binding, authors the original construction
binding/build/removal activity IDs and6-steel input requirement, and clones the
actual tile-presence definition into its explicit site. Existing30 stored steel,
3 structural yield and1h crane work are unchanged. The existing independent
SingleResourcePort remains the sole factory inventory; native Storage is absent.
No new inventory provider, resource owner, packing implementation or scheduler.

An initial strengthened execution reaches184 checks and completes real removal,
then correctly exposes a missing test module selection: ResourceFlowView is absent
because the optional journal was not selected by this base scenario. The fixture
now explicitly selects its already master-listed ResourceFlowsModule and authors
retention128 for this test's actual movement/yield receipts. No production defaults,
shared JSON document or journal implementation changes.

That version then reaches real demolition but waits a full original day for cargo
pickup with stock83 conserved: the scenario has NO selected automatic-evacuation
policy. The old test also assumed obsolete implicit evacuation. The final test
uses the existing ordinary ConfigureStockPolicyCommand to request depot min/
desired/max83, priority45/protected reserve. With initial depot50, only33 units are
needed. The engine performs source matching, carrier assignment, physical loading,
travel and delivery. Both saved sessions receive the same command. No test-owned
resource movement, artificial source spawning, timer shortening or free fallback.

Original ConstructionService.Evacuate and ReleaseYield intentionally call their
sole Pack implementation separately. This creates30 units of MOVED original stock
and3 units of explicit PRODUCED structural yield in two physical containers, not
the old asserted single33-unit pile. The test asserts both exact quantities, native
flow kinds/causes and actual source location. Production packing was not changed
to fit a historical container-count assumption.

## What the final same case proves

- Actual independent sole inventory and physical construction/removal contract.
- Pure preview/command admission, private source inventory, no instant removal.
- Actual crane travel and partial work, full authored hour, saved partial execution.
- Pause stops the original game clock/work; saved twins agree on pause and resume.
- Per-Update stock conservation:80 initially,83 only after earned structural yield.
- Removed source emptied once, no dangling source reservations,30 transferred and
  exactly3 authored yield; containers are at the source, depot still50 immediately
  after removal, so there is no instant remote delivery.
- Ordinary player stock policy drives real loaded-cargo recovery. Saving during
  loaded hauling preserves actual endpoint receipts/cargo; twin hashes continue.
- Final original depot83, empty-container cleanup, no repeated yield or delivery.
- Final save/load, private company flow-history refusal, interleaved session
  isolation, and replay of actual commands/pause/physical recovery.

The test's local Step/Until helpers only invoke normal Update and observe/assert
engine facts. Each wait retains the original one-day simulation bound plus20s
diagnostic bound; unmet predicates throw with clock, work state, blockers and stock.
No silent successful wait, host gameplay or duplicate authoritative state.

## Every attempt and build

Working directory: C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.
Each runtime selected the SAME original case in a separate process:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Capabilities: independent inventory survives physical demolition as salvage' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 14000
```

| Attempt | Passed/failed | Worker ms | Outer ms | Outcome |
|---|---:|---:|---:|---|
|Original current baseline|0/1|173|356|exit1; missing construction binding before initialization|
|Structural binding and physical assertions|184/1|2516|2729|exit1; unselected journal/view at accounting observation|
|Explicit journal selection|3067/1|10975|11208|exit1; actual1440-update pickup wait fails, stock83 preserved|
|Explicit player recovery demand|375/0|3836|4007|exit0|

All outputs untruncated. The third attempt yielded live process observation
session70751; that SAME session was awaited to terminal failure. It was not
restarted because observation initially expired. Its inner wait reports minute1541,
1440 updates,7696ms, orderDone/stock83. This is a failed predicate bound, not an
outer timeout or passed physical-recovery stage. No timeout was increased.

Three targeted builds used:

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v minimal
```

MSBuild7360ms,6530ms,6780ms respectively; each terminal exit0/0 warnings/0 errors
BEFORE its subsequent runtime. ONE exact case was exercised, not a capability
family/profile/full suite. Final375 are assertions, not375 independent tests.
No already passing unrelated cases were rerun.

## Final source and binary identities — SHA256

Unchanged core448 C# sorted relative path/hash LF digest:
63d352f2096c2d1db0d26bbdb49023ad72753070c6b91710efa2656ebdff9923.

| Artifact | SHA256 |
|---|---|
|Core DLL and Tests copied core|62FB93B1B0E3640B5A71B14B7B442641EA83C2188C30BACC252987175E43FC0F|
|Final Tests DLL|8A3C6E8D3E98D08F6A84E1765AE6BFB5372641D4FD2385F72D4A8B7DD91725FC|
|Unchanged ExtensionProof DLL|857A766DBACC503AF33AC24FCE8C20CA56837C3F5BF11C9DBF5D050DBE4ACA08|
|CapabilityTests.cs|7C070C1C243D2C7BFE41DD750452EDFC97A8B69DC9720E4B68A73C81795A6320|

## Scope and remaining work

No production engine or shared content edits, no Oil/business/Tycoon/platform/Draw
changes and no consumer rebuild. No new current consumer/native checkpoint or
harbour/fleet/performance acceptance is claimed. Old consumer2.659 saves remain
historical/incompatible with2.661 and are not header-rewritten. Previous P7-SS1
spill and P8-IB1 transformation results remain prior scoped evidence on the same
core; they were not silently counted again or claimed rerun with this Tests DLL.

No broad P7.017/P8.017/P9.051 gate is closed by this one proof. Arbitrary providers,
remaining cargo disposition/reference replacement, full spill/conversion/topology
fault/stress matrices, optional lots and complete scenario acceptance remain open.
Fresh original text/checkbox audit:221 total/209 recorded checked/12 open, unchanged.
Framework diff --check exit0/35 existing LF warnings/no other findings; both
repositories staged0, no live bounded/worker processes. No executable changes
followed the final observation. No staging/commit/push before whole goal AND plan.
