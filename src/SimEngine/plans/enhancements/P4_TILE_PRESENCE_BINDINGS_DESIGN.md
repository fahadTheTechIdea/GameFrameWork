# P4-G63 — contract-bound tile presence

## 0. Problem

TilePresence still inherits Component<TilePresenceDef>, despite ITilePresenceDefinition already being used by placement, navigation, construction, rollout, recovery and ports. An independent component definition cannot reuse the existing coordinate/interpolation/occupancy codec. TilePresenceDef also owns JSON parsing privately, encouraging duplicate readers. TileMapView.TryGetFootprint reads extensible metadata without the original healthy observation guard.

## 1. Goals

Allow independent file-authored ITilePresenceDefinition to create the exact existing TilePresence runtime and codec1. Keep one reader/validator and one authoritative coordinate owner. Validate actual owned metadata, positive geometry, mobile geometry and selected providers before Initialize. Guard detached footprint publication and preserve first callback errors. No compatibility or copied geometry state.

## 2. Design

Extract the original strict parser into TilePresenceDeclaration, shared by native and direct definitions. Shared resolved binding reads the actual component alias and original tile-space catalog; native generated line markers use explicit validated authoring through the same declaration. TilePresence inherits Component and exposes ITilePresenceDefinition from its actual base.Def; existing runtime methods and Sync layout stay unchanged. Existing P3-S22 presence admission calls the shared composition check. TileMapView stages footprint under EntityObservation against the actual selected map and refuses faulted queries before early returns.

## 3. API surface

Public TilePresenceDeclaration.Read/Resolve/ValidateBinding and shared ValidateComposition for component metadata. Native TilePresenceDef remains ordinary registration/generated authoring, not mandatory mod base. TilePresence.Def becomes the existing contract. Engine2.423.0/save154/replay1, codec1 unchanged. No new commands, clock, renderer or adapter.

## 4. Files

Map/TilePresence.cs owns metadata declaration and actual runtime. Map/TileMapView.cs owns detached footprint query. SaveGame/current-engine-protocol carry behavior identity. New non-friend TilePresenceBindingProof.cs registers direct metadata creating the SAME TilePresence. ONE NEW thin TilePresenceBindingTests.cs uses existing master-listed Colony JSON and public commands/views; no gameplay in drivers.

## 5. Errors

Missing/unknown fields, references, invalid dimension/mobile footprint, wrong actual owner/alias and missing selected tile provider refuse before Initialize. Runtime metadata observation faults through the existing World first-fault owner and publishes no footprint; mutation-then-throw retains original exception. No repaired geometry/default map.

## 6. Compatibility

Breaking development-only change. Delete native-only runtime binding; no shim, duplicate reader or parallel model.

## 7. Verification

One completed solution compile; ONE NEW exact bounded case and same-case repairs only. Native/direct presence across real colony startup, physical hauling, manufactured vehicle rollout and supplied timed construction; no instant creation, actual stock conservation, staged saves/twins/pause/private retained facts/replay/isolation. Malformed direct metadata and unused definitions reject early. Pure footprint and fault/clock callback publication guards tested. Physical stages bounded768 Updates, outer60000ms/owned termination5000ms/output30000 chars.

## 8. Risks

Definition authoring callbacks are extension code, not a hostile CLR sandbox. Scoped guards do not certify arbitrary callback purity or all concurrent/scale/private/protocol/Oil/global/full behavior. Original compound P2/P3/P4 acceptance gates remain OPEN unless full wording is proven.

## 9. Out of scope

Oil/domain/Tycoon/platform/Draw, extra geometry/state/reservation/material/clock owners, historical/family/full runtime gates and protected G19 migration. No staging/commit/push before full goal AND plan completion.

## 10. Order

1. Extract strict declaration and actual binding; remove mandatory native runtime metadata.
2. Guard original footprint view, author direct metadata against existing fixtures.
3. Compile and run only the new exact bounded case, retain all attempts.
4. Update stage evidence/master/audit with actual hashes and unresolved full scope.
