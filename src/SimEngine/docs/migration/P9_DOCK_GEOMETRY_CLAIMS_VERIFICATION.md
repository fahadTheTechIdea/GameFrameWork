# P9-C8g — Physical dock geometry claim verification

Date: 2026-10-06. Scope: Beep.SimEngine only. Parent:
[stage](../../plans/enhancements/P9_DOCK_GEOMETRY_CLAIMS_DESIGN.md).

## Current source and gate

Build **beep-simengine-2.122.0**. Save67/WorkBoard23/work system13/work view39/native map6/public
point space4 and all existing persistent owner schemas/payload layouts unchanged. Exact build
identity refuses preceding saves/peers; no migration reader or compatibility path is introduced.

Commands, from the framework repository:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Dock geometry claims: cross-facility admission and checkpoint ownership"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Dock geometry claims: physical mixed queues and saved continuation"
```

Final build: **0 warnings / 0 errors**, MSBuild elapsed **4.17 seconds**.
Final admission suite: **41 passed / 0 failed**, 1,577 ms.
Final physical suite: **30 passed / 0 failed**, 1,945 ms.
Total: **71 checks / exactly two complete suites**, zero failures or aborted suites.

Only those names ran, including same-name repairs.55 preceding test files/55 build assertion lines
migrated mechanically and compiled only; none of their suites ran. Shared ServiceDock/RouteOperatingArea/
ResourceDisposition fixture helpers were reused, not their test gates. Scoped tracked-source whitespace
checks passed; Git reported line-ending conversion warnings only. The pre-existing dirty tree remains.

Fixture repairs used normal Update to finish native pending events, selected a valid native spawn
tile and authored a second point departure slot. An idle worker legitimately remained at the first
exit: the test did not teleport it or relax occupancy checks. No full/family/third suite ran.

## One authority per responsibility

- Kernel adds optional pure `ISpatialOverlap` to the selected spatial authority. Native TileMap
  interprets half-open rectangular footprints using signed64 end arithmetic. Public-only PointSpace
  interprets current point positions, excluding Previous presentation history. Neither owns claims.
- WorkBoard.ClaimBlocker replaces identity-only Claimed in the existing owner. It reads the same live
  source/destination slot fields for all companies, including paused and future commitments. There
  is no second queue, reservation index, serialized geometry, stock, timer or scheduler.
- Candidate/self and live competing slot geometry are captured and compared through actual provider
  contracts. Unsupported shape/capability gives work.dock-unsupported-geometry; undefined/self-disjoint
  results and participating-entity mutations fail. Guards include entity identity/version/owner,
  world tick and allocator; they are not arbitrary global rollback or a universal spatial mutation log.
- Existing SelectDock shares the claim check for retained/new alternatives; ServiceAvailable shares
  it for promotion; initial exit uses the same selector. Existing replanning/actual work and job Sync
  retain their authority, and checkpoint validation rejects unsupported/overlapping commitments.
- Existing job fields, native receipts, cargo, earned work/handling and actual movement remain intact.
  Definitions are authored canonical JSON loaded through the existing explicit master composition.
  Test code authors inputs/commands, advances time and observes engine behavior only.

## Direct scoped proof

An empty point service place is reserved by hauling before arrival. A different facility, with a
different service slot ID and a foreign-company worker, selects its actual waiting place instead.
Once that wait is committed, the original source must skip its differently named overlapping wait
and propose its next authored alternative. Pure preview leaves hash, job/entity allocators and claims
unchanged; the foreign company sees no private job rows. Ordinary player assignment uses the same
check. Saved cross-company commitments round-trip identically. Corrupting an activity claim to the
other facility's reserved service geometry makes both Save and Hash refuse.

Point shape mismatch is Unsupported, not guessed disjoint. Provider Unsupported returns the typed
dock refusal and no proposed slot. Undefined/self-disjoint results fail. Mutating callbacks cannot
publish usable preview admission; through normal Update they fault the session, whose Save refuses.
Native source claims protect differently named overlapping facility geometry before actual arrival.
Whole rectangles overlap beyond their origins; edge-only contact is disjoint. End arithmetic beyond
signed32 coordinates does not overflow or introduce a platform-size cap. Empty/foreign shape is refused.

Native and independent public point simulations run a real haul and two authored activity nodes at
facilities sharing a bay and waiting geometry. Queued work earns no service during loading. Resource
receipt identities survive; partial handling saves into isolated twins that Update interleaved.
Every sampled continuation hash matches and the shared service bay never has two machine occupants.
Actual timed movement/handling/work/departure finish; both DAG nodes complete, source ends24 and
destination8 from authored32/0, without duplicate transfer. Completed checkpoints round-trip.

## Not established

This gate is not full framework acceptance. Missing-interface branch is implemented but not directly
exercised by a separate spatial provider; no arbitrary unsupported mixed-shape provider, moving dock,
same-facility overlapping hull, direct cross-facility exit contention, forged conflicting save bytes,
cross-facility fairness, traffic trajectory reservation, fleet/deadlock recovery, global provider
rollback, replay/lockstep, stress/soak or allocation budget was tested. Payload ownership and physical
claims do not guarantee future traffic clearance. No Oil/Tycoon/host/demo/Unity/Godot/Draw changes.

## SHA-256 of final compiled/gated source

| File | SHA-256 |
| --- | --- |
| src/Beep.SimEngine/Kernel/SaveGame.cs | B81358CE82B6C23DB680A4E9B611737E301C30C79AFFD5396DEAB13041E559DE |
| src/Beep.SimEngine/Kernel/SpatialContracts.cs | 5D0CE843A63F30D4181EA6C2A48E37613EA80379CF6DB13CA4605EAD1517D807 |
| src/Beep.SimEngine/Map/TileMap.cs | C7E985CB27F19F8CBDFCCD7BFF5747A139D2FA69FB8E889FBDF0D7CCA887C797 |
| src/Beep.SimEngine/Standard/DockContracts.cs | 221228B5063415DCCC3CA4BD378F41418DA89275760B212AE4FEB8AEE578BC19 |
| src/Beep.SimEngine/Standard/WorkDocks.cs | CA4BC076ACD56EE7C2193105C16BE669299C9632FC096DA124D72B2CAB8D1F16 |
| tests/ExtensionProof/PointSpaceProof.cs | 9DE9D7A40025E969DEEEC02A0D046F095C49DB5805784D726811C0757F3C747D |
| tests/Beep.SimEngine.Tests/ServiceDockTests.cs | 1A096A1030B1A5D75719F21F1993E11D32D9495EE562060B24AA98CABB479749 |
| tests/Beep.SimEngine.Tests/DockGeometryClaimTests.cs | 75C4DB339EFF70B4D19CAA014BB2091EEEB40EA90C174D8BE587F7DC4B1127B9 |
| tests/Beep.SimEngine.Tests/Program.cs | 2AE3A240F708C91E61FF6037BE5EDBEEC13C4FA2EFD0E92AB48401EAC75AC84A |
