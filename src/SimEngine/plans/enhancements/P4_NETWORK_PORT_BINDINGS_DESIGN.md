# P4-G50: Contract-owned network port authoring — Design Document

## 0. Problem Statement

INetworkPortsDefinition already existed, but all normal connection authoring required NetworkPortsDef<TPorts>. Native Ports still cast concrete PortsDef and historical custom providers inherited the same native base. Runtime flow checked only Entity.Version around Connections; private persisted sibling changes could evade that check. Diagnostics and gate views also read connections directly.

## 1. Goals

1. Remove the required native authoring base; native/direct definitions share one NetworkPortsDeclaration.
2. Bind the original Ports runtime to INetworkPortsDefinition; preserve original empty codec1.
3. Admit all loaded matching metadata/runtime/catalog connection declarations before Initialize, unused included.
4. Guard actual runtime connection reads through existing entity/provider codecs and clock observations.
5. Prove normal native/reusable/custom physical parity and positive timed push/pull without copied gameplay.

## 2. Design

Extend the existing contract path in place: no second port interface, registry, topology or transfer model. NetworkPortsDeclaration.Read/Resolve/ValidateOwner owns strict list authoring, raw identity validation, actual INetworkDefinition references, carried push/pull resources and immutable PortSpec lists. PortsDef is a normal ComponentDef<Ports>/INetworkPortsDefinition JSON registration, not an extension base. Ports now binds the metadata contract directly. NetworkPortsDef<TPorts> is deleted, including the obsolete Specs accessor; historical ProofNetworkPortsDef delegates the same declaration.

NetworkPortsContracts.ValidateDefinition verifies actual singular owner/component/runtime aliases and slot, unique actual network Id/Module/catalog identities, valid unique carried native resources, inlet presence, tile placement and same-space line association. Every exporting connection also requires IOutletPort; the global outlet/inlet invariant is unchanged. Explicit empty connection lists and omitted directional filters retain existing authored no-flow semantics, not invented endpoints.

ResolvedContent validates metadata/runtime parity and all resolved connection definitions after component validation, before freeze. InfrastructureModule and original LineService composition revalidate frozen registered definitions, including unused ones, under original observations before Initialize. Original Ports bindings must retain registered codec1; custom INetworkPorts providers use their actual registered codecs.

ReadConnections verifies the actual live singular provider, frozen definition and catalog aliases. It captures provider and whole selected entity Sync plus game-clock facts. Runtime Connections is read once and must alias the actual immutable authored list. Copied/foreign runtime lists refuse rather than silently substitute metadata. Flow, quantity diagnostics and InfrastructureView gate reads now use that shared path. Original metadata-only NetworkConnectionContracts remains shared by signal/gate declarations, not a runtime port state owner.

LineService, topology/pool inventories, flow cursors, demand/ranking/solver, gates, resource transactions, work/hauling and sole GameClock remain authoritative. No new transfer algorithm, mutable connection cache, scheduler, stock or clock.

## 3. API surface

Native ports JSON remains list entries with network, push and pull. Independent modules register ComponentDef<Ports>/INetworkPortsDefinition or custom matching runtime metadata, and list authored entity files in the master manifest. Unknown/missing/empty network/resource IDs and duplicate connections/resources fail. Normal player commands, Update, clock controls, Work/Operations/Infrastructure views, saves and replay stay the host boundary. No Draw/platform/adapter API.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| New | Standard/NetworkPortsBindings.cs | medium |
| Modify | Standard/Networks.cs, InfrastructureModule.cs | medium |
| Modify | Content/ResolvedContent.cs | medium |
| Modify | Standard/NetworkFlow.cs, NetworkDiagnostics.cs | medium |
| Modify | Views/InfrastructureView.cs | medium |
| New | tests/ExtensionProof/NetworkPortsBindingsProof.cs | low |
| New | tests/ExtensionProof/content/network-ports-bindings.json | low |
| New | tests/Beep.SimEngine.Tests/NetworkPortsBindingTests.cs | medium |
| Modify | historical proof/ownership references, registration/protocol fixture | low |

Core paths are relative to src/Beep.SimEngine. Historical custom/ownership cases compile only. One master-listed file owns the independent selected entities; the shared fixture removes duplicate source entity declarations. No duplicate private-probe component registration survives.

## 5. Error handling matrix

| Metadata/read | Result |
|---|---|
| Missing/empty/unknown network/resource, duplicate list entry, extra instant field | Typed ContentException, no fallback |
| Null/default/foreign/copied authored network or incompatible resource | Refusal before Initialize, unused included |
| Missing inlet/presence/wrong space or metadata/runtime mismatch | Actual owner admission refuses |
| Runtime getter throw/pause/Touch/private sibling mutation | Existing scoped fault; save/hash refuse |
| Runtime copied connection list | Actual authored alias refusal; no substitution |
| Accepted normal transfer/haul | Original physical transaction/time/state owners unchanged |

## 6. Backward compatibility

Development only; no compatibility base/accessor, alternate ports state or legacy reader. Original reusable empty codec1 and stock/network/work payloads remain save154/replay1. Build/protocol fixture become2.393.0; exact build/content identity checks remain authoritative.

## 7. Verification

ONE initial compile failure4.21s (missing existing Maps import), followed by FOUR clean builds12.69s/5.86s/4.47s/4.35s, zero warnings/errors. ONE NEW exact bounded case, FOUR attempts:4 passed1 failed54ms;192 passed13 failed1339ms;190 passed15 failed1177ms; FINAL205/0/0 in1169ms. Final PID39096 Exited0, observed1314ms. [Actual evidence](../../docs/migration/P4_NETWORK_PORT_BINDINGS_VERIFICATION.md).

Only this new exact case ran. Old/family/full/Oil/Tycoon/platform runtime remains unrun; full regression stays unauthorized and OPEN.

## 8. Risks

Selected codec/clock observation is not a CLR sandbox/global rollback guarantee. Exact authored list alias is intentional for immutable connections, not a dynamic runtime connection-policy API. All ownership/public-neutral/topology/congestion, populated promises, provider recursion, nonparticipating private state and performance matrices remain OPEN. Composition's bad original codec registration branch is implemented but not covered by this proof.

## 9. Out of scope

Dynamic connection policies, non-tile networks, line-effect authoring-contract migration, all provider/metadata callbacks, utilities/areas/topology/receipt matrices, allocation/lockstep/fuzz/soak and full original acceptance. G19 authority unanswered. Protected Tycoon/GameClock, Oil, hosts, Draw/platforms untouched. Prior line/network proofs are historical, not rerun on2.393.0.

## 10. Sub-task execution order

1. Extract sole connection declaration; remove native generic base and Specs path.
2. Bind reusable Ports to existing metadata contract and centralize actual owner/alias validation.
3. Apply actual provider/entity codec observations in runtime flow/diagnostic/gate reads.
4. Master-list independent authoring/custom provider proof and one thin native/direct physical case.
5. Build/run the same exact case; repair proof registration/startup timing, preserving all failed evidence.
6. Record post-green hashes and update current tracker/audit without changing original compound gates.

## 11. Delivery status

Scoped verified, not whole-plan completion. Native/reusable/custom flow chronology matches: source30/pool0 becomes26/4 then22/8; original gate blocks further flow without catch-up credit. Ordinary truck command physically moves five depot units to local watcher stock over game time; depot45/pool8, not duplicated network stock. Original gates/private facts/pool-flow views, saved twins/pause/replay/session isolation succeed.

Positive native/reusable pull waits one step for pushed stock: first pool4/watcher0; next pool4/watcher4; third source18/pool4/watcher8, total30. Original elapsed cursors save/restore/replay.27 unused malformed authoring/bindings refuse before Initialize. Four metadata getter and five custom runtime faults cover throw/pause/Touch/private sibling and copied list; final healthy first step and refused save/hash are proven.

Selected382 executable inputs hashed AFTER final green at2026-10-09 19:17:02 UTC, not pre-run/full transitive closure. No executable edits afterward. All221 original compound gate text/checkboxes remain unchanged. Full goal active; nothing staged/committed/pushed until the whole original goal and plan are complete.
