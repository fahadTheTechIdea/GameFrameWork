# P11-D3 — Exact spatial observer identity

## 0. Problem statement

`SpatialLayerAccess.CanRead` grants observer access for every negative player ID. Native TileMap
version/chunk cursors and the public point provider repeat that shortcut. Existing `World.ViewFor`
and knowledge visibility correctly recognize only `PlayerId.None`; direct spatial contracts must
not expose private values or invalidation metadata to malformed negative identities.

## 1. Goals

1. Exactly PlayerId.None retains observer access; every other negative viewer receives no layer data.
2. Live and detached layer contracts agree for public/private layers and actual owner/foreign seats.
3. Native map and public point cursors expose no private changes to invalid negative viewers.
4. Refused region/page reads clear output and disclose no count/version; saved continuations preserve
   actual authorized values. Invalid module-visible views remain rejected by the existing World guard.

## 2. Design

Correct the existing shared `SpatialLayerAccess.CanRead` identity rule. Native `TileMap.VersionFor`,
`ChunkVersionFor` and `ChangedChunkKeys` distinguish exact observer, nonnegative viewer, and invalid
negative viewer. Invalid identities receive cursor0/empty changes, never another company's cursor.
The public point provider uses the same distinction for its cursor and existing CanRead for values.
No new visibility service, provider registry, learned table, stock owner or clock is introduced.

Use one new exact protocol scenario in existing SpatialLayerTests. An explicit embedded/master-listed
JSON file authors native layer units/resources/metadata/space. Existing point JSON/provider factories
are reused. Native setup uses labeled engine-layer fixture hooks; point setup uses normal player
commands. Assertions inspect authorization/metadata/cursors/clear-on-refusal and saved twin updates;
they do not implement discovery, surveys or fog rules in tests.

## 3. API surface

Signatures/layouts unchanged; corrected authorization semantics. Identity2.220.0/save124; actual native
codecs unchanged. Direct assertion migration only; no old-save reader, compatibility alias or fallback.
SpatialLayerAccess is an identity/ownership rule, not a session-seat membership service; World.ViewFor
continues to reject nonseated positive IDs. Cell discovery and typed layer knowledge remain open.

## 4. Files

Kernel/SpatialContracts.cs; Map/TileMap.cs; Kernel/SaveGame.cs; public point provider; existing
SpatialLayerTests/new authored JSON embedding; current identity assertions (compile only); tracker,
README/AGENTS and scoped report/inventory.

## 5. Errors

Invalid negative viewer: CanRead=false; region/page Unavailable with cleared outputs and no metadata;
cursor0/empty chunks. Existing invalid-view construction rejects through World.ViewFor. Exact
observer/owner permissions and typed value/geometry checks remain unchanged for authorized reads.

## 6. Compatibility

Direct semantic correction, no legacy reader or alternative authorization path.

## 7. Verification

One completed-slice build and only this new named bounded scenario:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Spatial visibility: exact observer identity and private cursor isolation"
```

Repair reruns only for actual failures. No older/family/full/platform gate. Source/hash/link/clock
checks do not substitute for broader information-flow or gameplay acceptance.

## 8. Risks

Cursor0/empty change facts for an invalid identity are refusal metadata, not a fabricated game value.
Authorization must precede region/page/type details. Public extension proof is engine-side capability
code, not host/demo gameplay. Broader malicious providers/positive nonseat direct contracts remain open.

## 9. Out of scope

Cell-level discovery, typed layer knowledge, fog refresh/sharing, hidden public-layer activity and
relationship/event/ID side-channel audits, pressure/strategy, oil/platform/Draw/hosts/demos, full gates.

## 10. Execution

- [x] Correct shared layer and existing native/public cursor identities.
- [x] Add authored fixture and the single new bounded protocol scenario.
- [x] ONE clean build0 warnings/errors3.83s; ONE exact new scenario89 checks0 failures/aborts313ms,
  first attempt. No repair/rerun/stale binary, older/family/full/platform run or post-gate source edit.
- [x] Record [scoped evidence](../../docs/migration/P11_SPATIAL_OBSERVER_IDENTITY_VERIFICATION.md)/tracker.
  Full plan/goal, cell discovery/knowledge and final publication remain open.
