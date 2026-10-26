# P3-S39 — Pure resource-cell guard verification

Engine beep-simengine-2.589.0/save157/replay1; payloads unchanged. No Oil/Tycoon/platform/host rules change, full/profile/family run or Git publication.

## Implementation

ISpatialResourceCellGuard requires immutable World metadata. The original SpatialResourceEndpoint exposes its existing readonly session. SpatialResourceCellBindings validates matching actual selected space and one bound session, retaining that reference rather than inferring it from concrete provider types.

SpatialMutation uses one original EntityObservation facts capture for publication and pure guard checks. A successful guard verifies world/entity and existing cell quantity/version/owner facts. Silent scoped mutation faults spatial.guard. A thrown guard callback remains the same exception; if facts stayed unchanged, it is an ordinary healthy refusal, but mutation-plus-error latches that first exception as the world fault. No extra fault/book/store/clock or full-world hash is introduced.

## All builds and exact runs

1. Targeted contract/helper compile: exit0, zero warnings/errors,7.02s.
2. Added focused guard modes; targeted compile clean4.71s.
3. FINAL pure guard refusal/mutation case:130/0,775ms; PID31916 exit0/983ms.
4. FINAL publication notification modes:96/0,687ms; PID38724 exit0/907ms.
5. FINAL atomic refusal/receipt case:56/0,722ms; PID31856 exit0/940ms.
6. FINAL independent physical extraction/optional survey:89/0,2630ms; PID21492 exit0/2835ms.
7. FINAL native visibility-enabled extraction/optional survey:92/0,4331ms; PID18256 exit0/4531ms.
8. FINAL actual native/point receipt ownership/transfer provenance:109/0,766ms; PID32752 exit0/906ms.

Six distinct exact cases572 successful checks. All outputs untruncated and processes terminal. No failed run, repair build or executable edit after these finals.

## Proven scope

On both providers, unchanged preflight exceptions preserve the exact exception instance, healthy state/hash and save/load continuation. Silent clock-pause/entity-touch guard callbacks fault before requested material publication. Mutation followed by an exception preserves the first thrown instance and terminal world fault. No requested material debit, endpoint-version prefix or publication notification occurs; later mutation cannot retry the bad guard. Faulted save/hash/update continuation refuses and unrelated sessions remain healthy.

Publication safeguards and no-prefix sample/receipt refusal still pass. Native, independent tile and point extraction retain original receipt guards, exact stock/book versions, transfer provenance and saved physical continuation. Machines still physically extract over game time; optional surveying, private readings and deterministic ordinary replay remain valid.

## Open boundaries

The guard captures scalar world/entity facts and original cell facts, not arbitrary private component/service memory or malicious metadata getter effects before binding. Every owner-change/codec/resource-provider callback, per-entity observation cost, physical stocked dismantling/recovery/utility/lifecycle and global lockstep/fuzz/soak/allocation/full acceptance remain open. Protocol probes are negative engine tests, not ordinary player stock writers. No original compound checkbox closes.
