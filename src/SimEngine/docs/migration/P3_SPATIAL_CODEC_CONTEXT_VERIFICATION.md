# P3-SC1 — Spatial checkpoint callback contexts

> Subsequent source-unchanged [current2.664 native workload revalidation](P5_CURRENT_NATIVE_WORKLOAD_VERIFICATION.md) rebuilds the consumer and earns a compatible checkpoint at1453. The prior no-current-consumer statement below is preserved historical. One bounded day passes, but active updates remain581–656ms; allocation reduction is not a general CPU/harbour completion proof.

2026-10-18. Engine2.664.0/save160/replay1.
[Design](../../plans/enhancements/P3_SPATIAL_CODEC_CONTEXT_DESIGN.md).
Original221 requirements/209 recorded checked/12 open; full goal ACTIVE.

## Original owners and direct API migration

SpatialResourceCellBindings remains the sole per-layer cell binding/index/codec
owner. Its StageSync now takes a generic value context and a static-compatible
quantity sampler. The old capturing Func<long,Fix> signature is removed, not kept
as a legacy overload. ISpatialResourceCellGuard.StageSync, actual cell books,
endpoint identities, material quantities and stored layouts are unchanged.

Every mode FIRST encodes or decodes and validates the actual binding count. An
actual zero-binding set has no cell quantity/book callbacks to run. Its read path
returns a stateless empty publication, its write/hash path returns null, exactly
the original action shape. This is the validated empty operation, not a guessed
default. Bad saved counts still refuse before the empty branch.

One shared nonempty core retains stable geometry order, exact key validation,
original endpoint StageSync/native candidate quantity, unchanged version check,
required read publication and forbidden writer publication. Captured publication
is created in a separate action factory ONLY when reading real books. Hash/write
calls no longer allocate a closure for a restore action they cannot use.

Native MapLayer<T>.Sync supplies its actual (layer,candidate chunks) to one static
sampler. Independent SensorSampleLayer and PointLayer migrate the SAME way using
their actual candidate/current stores. Point-layer live-read validation versus
staged-candidate interpretation stays distinct as in the original code. Native
OrderedKeys uses Array.Empty<long>() only for an actually empty store; nonempty
sorting/validation/hash bytes remain unchanged.

No new stock/book, dictionary copy, state codec, router, provider cache, pooled
hasher, clock or world-size limit. Every sample/chunk is still serialized/hashed
and validated. No EntityObservation or first-fault boundary is changed. Runtime
identity2.664 requires matching saves/peers; save160/replay1 and bytes/layouts stay.

All four known producer calls migrated: native tile, independent tile, point
candidate restore and point live write. Read-only Oil subtree search found no
SpatialResourceCellBindings caller requiring migration; no Oil source was edited.

## Focused codec, books and physical flow

Existing SpatialCellContentTests gains a separate partial empty-codec unit, using
an actual authored native layer and an isolated binding codec NEVER installed as
an alternate world index. It proves exact zero Int32 bytes, identical scalar count
hash, null writer/hash action, non-null completed read action, no nonexistent
quantity callback, malformed counts1/-1 refused, unchanged actual world hash and
save/load of the original native zero-binding layers.

Existing bounded incoming-commitment case runs native tile and independent point
cells. Actual finite room, inbound/outbound ownership, protected quantities/owners,
malformed over-capacity staged books, saved exact receipts, partial receive,
remaining promises and release/twin identity remain intact.

Existing independent physical spill case retains timed finite material deposition,
recovery, loaded hauling, treatment, conservation and original saved/replay paths.
The unchanged routing-read measurement also proves native/independent correct
values, healthy full observations, no elapsed game time, save/load and isolation.

## Reused verified baseline, unchanged measuring inputs

The immediately preceding P7-NG2 final2.663 measurement is the actual pre-edit
baseline. Before implementation, on-disk Tests/core hashes were verified to match
that exact recorded observation:

- Tests6B9D0F8D46B75CE553836296E5EE500DE765972160AF9F4480EB5278434DE0CF.
- Copied core1E55AEE5E00E4515FC08FE70B9AD81CB3577AAB73942B7B057E272D84D67B4F9.

It was NOT needlessly rerun or counted as a new current test. The same measurement
method/profile are unchanged (hashes below),512 iterations/64 warmup/3 rounds per
operation. Current-thread managed allocation surrounds only the actual reads;
setup/assertions/hashes/output/saves are outside the measured window.

Every round reports identical allocation for its operation:

| Fixture / operation | 2.663 bytes/call | 2.664 bytes/call | Fewer bytes/call |
|---|---:|---:|---:|
|Native condition|3640|928|2712|
|Native structural entry|13488|3544|9944|
|Native live entry|13872|3928|9944|
|Independent condition|856|328|528|
|Independent structural entry|1528|648|880|
|Independent live entry|2488|1608|880|

SAME9216 measured calls:55099392 ->16871424 allocated bytes,38227968 fewer bytes
(69.38%). Substantial original hashing/guard allocation remains; no zero-allocation
or all-thread/process/update budget claim.

All elapsed observations, each512 calls, milliseconds:

| Fixture / operation | 2.663 rounds1/2/3 | 2.664 rounds1/2/3 |
|---|---|---|
|Native condition|45.306 /50.284 /44.337|57.218 /49.885 /45.487|
|Native structural|196.309 /197.645 /34.724|158.929 /217.590 /187.908|
|Native live|18.116 /20.724 /18.780|42.001 /39.547 /39.379|
|Independent condition|2.629 /2.968 /2.787|3.505 /3.775 /3.685|
|Independent structural|5.610 /5.502 /6.025|7.262 /7.080 /7.246|
|Independent live|11.347 /11.926 /10.679|12.891 /12.728 /13.524|

Several timing samples are slower despite the allocation reduction. JIT/tiering/
runtime variation is not isolated by these microbenchmarks. They do NOT establish
a CPU speedup or whole-scenario performance fix. Current native Update/harbour/
fleet throughput still requires direct bounded measurement; do not replace that
gate with the lower allocation number or increase its limits.

## Completed build and every runtime attempt

Working directory C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.
ONE target build completed BEFORE all current runtime:

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v minimal
```

MSBuild9070ms, terminal exit0/0 warnings/0 errors. No incomplete build, unobserved
test outcome or process restart. Each row below is a separate exact-literal
process, timeout30000ms/termination5000ms; measurement output cap14000, other caps
12000 chars. All output untruncated.

| Current2.664 case | Passed/failed | Worker ms | Outer ms | Outcome |
|---|---:|---:|---:|---|
|Empty binding codec|11/0|396|581|exit0|
|Unchanged routing-read measurement|60/0|1321|1486|exit0|
|Bounded incoming cell commitments|52/0|626|779|exit0|
|Independent physical spill accounting|322/0|4905|5310|exit0|

Exact literals:

```text
Spatial cell codec: explicit empty bindings retain exact counts and staged publication
Navigation entry contexts: measured native and independent guarded callback allocation
Spatial resources: bounded incoming receipts preserve actual layers and saved commitments
Spill accounting: independent physical deposition recovery hauling and treatment
```

Final445 assertions across FOUR focused cases, not445 suites. No all-tier fleet
driver, broad family/profile/full suite, native physical-spill variant, consumer
rebuild or long-day run. No assertions were weakened to fit lower allocation.

## Final identities — SHA256

Core448 C# sorted relative path/hash LF digest:
7ab60feff685fb948b0e216855a4beb53d1b9c5560e654e183e930d36fbff453.

| Artifact | SHA256 |
|---|---|
|Core DLL and Tests copied core|0B0F09D651442E4A9FAE695E84CC6FA80342F07DA975525F50B30EF09DCCB177|
|Tests DLL|2960064F07043D2FE1F2DEE421471F90EEE72456829F30594F178DDE54215B0E|
|ExtensionProof DLL|7CA90AFC746088F344687D5CD12A04D1F299D544A66C97303D6EF69ED732D44B|
|SpatialResourceCells.cs|52716D0A1EB7F8633EF29C85261F6C02EF09FDE457CC62C3E4D395C5D4C760BA|
|TileMap.cs|6573E6F0D3C9FAFBF56468EEA3A90DD65B483CF6A72EAD9A26F296DD31856094|
|UNCHANGED measurement partial|60A1330CE7502A25883E921423EEB8326965C993660D3EF766B55A248E722974|
|UNCHANGED measurement profile|9BEA97D7636AA42B34B30276137489D461F9B63EE562C6AC0D4F22794BB1A581|

No executable changes followed current final observations.

## Original audit and remaining scope

Fresh original literal text/checkbox audit221 total/209 recorded checked/12 open,
unchanged. Full goal remains ACTIVE; no original broad gate closes from this codec
slice. Arbitrary provider faults/geometry/rebinding/conversion/accounting/stress,
all-tier current budgets, native harbour/fleet and full acceptance remain open.

No business/Tycoon/Oil/platform/Draw/host gameplay edits. Consumer PublicProof core
still prior2.659 SHA E59763A3457711D9C1905C96700886241083A699BEC700BB15B400249F2B35A8.
No compatible2.664 consumer checkpoint exists from this stage; older saves remain
preserved historical/incompatible, never header-rewritten. Next performance work
must measure actual current workload instead of claiming a speedup from allocation.

Hygiene: framework diff --check exit0/35 existing LF warnings/no other findings,
both repositories staged0, no live bounded/worker processes. No staging/commit/
push until entire goal AND plan finish.
