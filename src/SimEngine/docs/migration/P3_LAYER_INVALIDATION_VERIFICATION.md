# P3-S5e — Private ownership and atomic invalidation evidence

Date:2026-10-06. Current build `beep-simengine-2.108.0`, save65, native map4.
Native payload/view schemas unchanged. No compatibility reader or legacy path.

## Scope and ownership

The existing TileMap remains the sole viewer invalidation owner; MapLayer<T> remains the sole native
sparse value/checkpoint owner. There is no second spatial layer catalog, store, codec, query cache or
game loop. JSON definitions, metadata/resources/vertical semantics and provider-owned geometry are
unchanged. No platform, renderer/Draw, host/demo, OilFieldDays or Tycoon changes.

Private ownership now preflights the actual layer/observer/old-owner/new-owner counters, then updates
only those existing records. Public/third-company versions do not reveal a transfer. Native writes
preflight the actual affected counters after same-value exit and before changing existing/new chunks.
Fill rechecks after generation callbacks and before replacing values. Touch operations preflight
themselves too. This is scoped counter-refusal atomicity, not allocation-failure/global rollback.

## Exact gates and final results

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Spatial layers: canonical sparse storage and generation"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Spatial layers: private ownership, detached views and saved sparse values"
```

Final build: zero warnings/errors,3.26s. Exactly two full suite names executed:

| Suite | Passed | Failed | Aborted | Final duration |
|---|---:|---:|---:|---:|
| Canonical sparse storage and generation |182|0|0|193ms|
| Private ownership, detached views and saved sparse values |59|0|0|131ms|

Total241 checks. No third/family/full gate. Same-name development/final reruns only.
41 preceding identity assertion files migrate/compile only; SpatialLayerTests is the42nd identity file
and the only behavioral test file changed. No preceding behavioral suites are claimed green.

## Proven cases

- Existing byte/int/fixed canonical sparse storage, strict reads, edge padding, failed staged decoding,
  generation refusal, same-layer reentrancy, private generation, wide sparse geometry/save/twins remain
  proved by the selected canonical suite.
- Layer/observer/public/existing-private-record counter exhaustion refuses overwrites, new chunks and
  generation before value/cursor changes; same-value writes stay no-ops. Native bytes, authoritative
  hash and whole resumable saves remain unchanged after each refused operation.
- Generation calls an actual other-layer Set that exhausts the observer counter. Final preflight
  refuses generated replacement, preserves original layer/version, and deliberately retains the real
  other-layer write. No fictional global compensation.
- An exhausted unrelated public counter does not block a valid private write. Exhausting the actual
  private owner does block private overwrite/generation without changed values/cursors.
- Three-company JSON scenario, including a third company already owning another private layer:
  assignment/transfer invalidates only affected owners/observer. Third-company visible version, chunk
  delta and versioned public-page continuation remain unchanged; old owner loses access and new owner
  immediately reads existing exact values. No value copying/reset.
- Ownership layer/observer/old-owner/existing-new-owner/new-owner-inherited-public counter faults
  retain actual owner, all tested cursors, hash and resumable save. Same-owner no-op at exhausted
  observer cursor succeeds. Saved twins continue through actual Game.Update.

## Development repair

After adding a third company that already owned a private layer, two fixture assertions wrongly
compared another company's cursor to that third company's local cursor. The expected baselines were
corrected to each viewer's own prior cursor; no engine fallback/change was used to satisfy the test.
Both same named gates were rebuilt and rerun successfully against final source. Earlier passing
240-check evidence is superseded by the final241-check result above.

## Limits and remaining work

Counters remain existing transient signed integer cursors; representational overflow explicitly
refuses rather than wraps. This adds no authored map-area/platform limit, but does not extend the
cursor range. Native fault cases use framework-only reflection; extensions never receive backing
store access. No proof of arbitrary custom-provider mutation, global side-effect/allocator rollback,
whole-map corrupt restore atomicity, extraction/hydrology/3D semantics, replay/lockstep, broad stress/
allocations or full P3/P1–P12 completion. No entire dirty worktree regression claim.

## Final source fingerprints (SHA-256)

| File | SHA-256 |
|---|---|
| src/Beep.SimEngine/Map/TileMap.cs |60b732e5ac0b163ab81da9e85377543633e5ac40ebfa6c7fec8d7c708e6ffb41|
| src/Beep.SimEngine/Kernel/SaveGame.cs |b8e490ec9a8582f4fc20243b30509eb8f6a2482f30b93024ee892f49a3e0369a|
| tests/Beep.SimEngine.Tests/SpatialLayerTests.cs |27372a50f6c3a6398b0ecb3f52bc97793fb3f96ecdce2c323ba271f206b1f116|
