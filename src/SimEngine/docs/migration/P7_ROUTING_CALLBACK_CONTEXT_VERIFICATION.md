# P7-NG2 — Value-context routing callback allocation

2026-10-18. Engine2.663.0/save160/replay1.
[Design](../../plans/enhancements/P7_ROUTING_CALLBACK_CONTEXT_DESIGN.md).
Extends original P7-NG1 without rerunning its broad fleet driver.
Original221 requirements/209 recorded checked/12 open; full goal ACTIVE.

## Existing owner and actual change

Only two existing hot callback callers change runtime behavior:

- TileConditionContracts.Matches passes its actual definition/world/map/tile value
  context and one static stateless reader to existing EntityObservation.ReadProvider.
- NavGrid.EntryCost passes its actual grid/map/actor/tile/structural context and
  one static stateless reader to that SAME original observer. Original callback
  logic moves into ReadEntryCore, not another admission implementation.

Both previously created captured callback objects per sampled tile. No callback
object is now created by either caller per invocation. The static readers bind no
World/entity/provider from earlier sessions; contexts are per-call value tuples.

Original outside-bounds/origin checks keep their order. The same selected provider,
actor and space are observed. Metadata/context/minimum-cost/occupancy/clearance
validation, callback invocation, full persisted space verification, first errors
and fault latching stay inside the same boundary. No EntityObservation, hasher,
map codec, layer lookup, pathfinder, route/dock/fuel rule or gameplay default changes.
No world-derived cache, extra persistent owner, pooled hasher or native bypass.

Three production sources change: these two callers and EngineInfo identity.
Build2.663/save160/replay1; no codec/layout/public API changes or compatibility.

## Exact measured scope

Existing NavigationObservationAllocationTests gains a separate partial exact case;
the original all-tier Run method is NOT invoked. Existing LayeredNavigationTests
and IndependentTileWorkTests factories provide actual master-listed native and
independent spatial layers/conditions/routes. A new embedded test profile explicitly
authors512 iterations,64 warmup calls,3 rounds, sample IDs/coordinates/expected values.
No copied factory/provider, fake layer or driver-owned game rule.

Six operations use the actual existing engine methods: condition match, structural
entry cost and live occupancy-aware entry cost on both fixtures. Current-thread
managed allocation and Stopwatch timestamps surround ONLY the repeated calls;
setup, callback delegate construction in the measuring driver, assertions, hash,
output, saves and session setup are excluded. Each sample also checks correct
authored values and healthy observation; whole-context hash/time/save/load/idle
isolation are verified outside the timing window.

All three rounds reported exactly the same allocation for each operation:

| Fixture / operation | Baseline bytes/call | Final bytes/call | Fewer bytes/call |
|---|---:|---:|---:|
|Native condition|3752|3640|112|
|Native structural entry|14056|13488|568|
|Native live entry|14440|13872|568|
|Independent condition|968|856|112|
|Independent structural entry|1760|1528|232|
|Independent live entry|2720|2488|232|

Across the SAME9216 measured calls, allocation falls57901056 ->55099392 bytes:
2801664 fewer bytes (4.84%). This is **not allocation-free navigation**: substantial
original observer/codec allocation remains, e.g.13872 bytes/native live read in
this fixture. Only the two per-call captured callback allocations are eliminated.

All elapsed sample observations (each512 calls, milliseconds):

| Fixture / operation | Baseline rounds1/2/3 | Final rounds1/2/3 |
|---|---|---|
|Native condition|45.428 /48.891 /44.905|45.306 /50.284 /44.337|
|Native structural|211.395 /236.630 /82.453|196.309 /197.645 /34.724|
|Native live|48.180 /50.304 /53.174|18.116 /20.724 /18.780|
|Independent condition|10.351 /5.691 /4.640|2.629 /2.968 /2.787|
|Independent structural|9.376 /7.634 /13.456|5.610 /5.502 /6.025|
|Independent live|23.475 /19.022 /16.824|11.347 /11.926 /10.679|

JIT/tiering/runtime variation is visible, including a slower native-condition
round. No fixed CPU speedup, FPS improvement, all-thread/process budget or whole
scenario performance victory is inferred. The allocation delta is the scoped
measured result. No flaky elapsed-time assertion was introduced.

## All builds and selected runtime attempts

Working directory: C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.
Two target builds used:

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v minimal
```

Baseline-proof build yielded live session49870. The SAME handle was awaited to
terminal success BEFORE the baseline ran: MSBuild9930ms/0 warnings/0 errors.
Final runtime build completed MSBuild8930ms/0 warnings/0 errors before final tests.
No live handle was restarted because an observation expired; no incomplete-build
test results are used.

Every row below is a separately selected exact-literal process, timeout30000ms,
termination5000ms. Benchmark output cap14000, other outputs12000; all untruncated.

| Attempt | Passed/failed | Worker ms | Outer ms | Outcome |
|---|---:|---:|---:|---|
|2.662 pre-edit measurement|60/0|1671|1951|exit0; actual baseline, not a fabricated failing behavior test|
|2.663 same measurement|60/0|1072|1231|exit0|
|Existing condition callback fault stage|48/0|521|689|exit0|
|Existing independent physical routing|82/0|338|513|exit0|

Exact literals:

```text
Navigation entry contexts: measured native and independent guarded callback allocation
Tile condition contracts: callback fault stage
Independent tile world: native layered routing, physical movement and public commands
```

Final190 assertions across THREE focused cases; baseline60 not counted twice.
Fault case retains actual callback mutation/exception/clock refusal, no result,
terminal hash/save refusal and stopped Update. Physical case retains elapsed
layered movement, saved twins, public commands and occupancy-provider faults.
No full/profile/family/default/stress driver, harbour day or automatic rerun of
other passing families. Previous nested-query proof is2.662 evidence, not silently
counted as rerun on2.663.

## Final identities — SHA256

Core448 C# sorted relative path/hash LF digest:
b944ddff070e9ab844c01f7712d91f16cb952bd2aa37d26f3121bcea869e2c75.

| Artifact | SHA256 |
|---|---|
|Core DLL and Tests copied core|1E55AEE5E00E4515FC08FE70B9AD81CB3577AAB73942B7B057E272D84D67B4F9|
|Tests DLL|6B9D0F8D46B75CE553836296E5EE500DE765972160AF9F4480EB5278434DE0CF|
|Recompiled ExtensionProof DLL|54662D67BFA2D83DE017B726C45EC981B9EB92D4373EDCA103786DF067FF9194|
|Navigation.cs|543734D2DB9C4A302EB0023EA6548EEF924C3C7A22A694E018D430D0DFF7242E|
|TileLayerConditions.cs|D07CD8CCDE03A7ABBE2F9725048F7F64A37031222D28E8910203561F955CEE08|
|New same-owner measurement partial|60A1330CE7502A25883E921423EEB8326965C993660D3EF766B55A248E722974|
|Explicit measurement profile|9BEA97D7636AA42B34B30276137489D461F9B63EE562C6AC0D4F22794BB1A581|

ExtensionProof sources were not edited in this stage; the dependent DLL was rebuilt
against the current core. No executable edits followed final runtime observations.

## Remaining scope and hygiene

Original requirement text/checkbox audit221 total/209 recorded checked/12 open,
unchanged. No original broad gate closes from this read microbenchmark. Native
Update/harbour/fleet throughput, broad provider/fault/accounting/control matrices,
all-tier current allocation and soak/full acceptance remain OPEN. EarlierP7-NG1
full declared fleet result is historical2.498 evidence, not re-certified here.

No Oil/business/Tycoon/platform/Draw/host gameplay edits or consumer rebuild.
Consumer PublicProof core remains prior2.659 SHA
E59763A3457711D9C1905C96700886241083A699BEC700BB15B400249F2B35A8.
No compatible2.663 native checkpoint is claimed. Preserve historical incompatible
saves, never rewrite headers or raise original scenario limits.

Hygiene: framework diff --check exit0/35 existing LF warnings/no other findings;
both repositories staged0, no live bounded/worker processes. No staging/commit/push
until the entire goal AND plan are complete.
