# P10-A21: connected network resource ports — Design Document

Current framework2.712/save161/replay2; connected contracts remain those implemented at M48. Connected replacement490/0/two replays is selected again at M51 after sharing its original prediction path with evacuation. Full connected366/0 remains M48/core2.709 evidence, not a current-image rerun. [Current recovery/binding evidence and limits](../../docs/migration/P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m51-independent-evacuation-replacement--2026-10-22), [M48 contact implementation](../../docs/migration/P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m48-connected-policy-replacement--2026-10-22). No connected-evacuation runtime or full/profile/family qualification; full goal remains OPEN.

## 0. Original problem

Before A21, named ports required reference-equal static capacity metadata while the network's private wrapper scaled per-member capacity. Shared instanced capacity and explicit connected-port contracts now permit manufacturing/work/hauling from original pools, without requiring local inventory for empty push/pull declarations.

## 1. Goals

1. One explicit instanced capacity contract replaces the private network wrapper and delegates arithmetic to the authored policy.
2. JSON network_resource_port binds to the original NetworkService pool, with explicit minimum_members, native quantities and no new stock/claim/clock owner.
3. Connection-only participants need actual spatial presence and declared network, not fabricated local inventory; real push/pull retains inlet/outlet requirements.
4. Missing/undersized/pending/original-lost connections expose typed unavailable facts; no alternate pool or stock default.
5. Original manufacturing, activity work and physical hauling use shared pool receipts through existing coordinators, with saved twins/replay/private views.

## 2. Design

Core/InstancedCapacityPolicy.cs owns IInstancedCapacityPolicy and immutable InstancedCapacityPolicy over the actual authored ICapacityPolicyDefinition. The old private wrapper is removed. ResourcePortContracts accepts exact policies or the same actual basis with enough declared instances, retaining exact native quantum.

NetworkResourcePortDeclaration owns strict id/network/minimum_members JSON, actual resolution, and owner/space/network metadata validation. Native NetworkResourcePortDef/part delegates it and INetworkResourceAccess. The same NetworkService uses ObserveConnection to return its actual inventory. Explicit footprint metadata supports ordinary reads (entity.Def) and replacement projection (destination definition) at an actual registered anchor. Original graph geometry owns contact; topology owns member/owner/endpoint selection with settled-version guards. No temporary entity, projected stock or duplicate selection. Diagnostics remain immutable; mutable handles stay inside engine modules, not hosts.

InfrastructureModule registers the new component, binds INetworkResourceAccess to the existing topology/simulation/diagnostics owner, and validates loaded metadata. Connection-only NetworkPorts declarations may have empty push/pull without a local inlet. Actual pull requires inlet, push requires outlet (which also includes inlet). Saves keep current original topology/stock/receipt codecs.

## 3. API surface

IInstancedCapacityPolicy.Definition/Instances; NetworkResourcePortDeclaration shared read/resolve/validation; INetworkResourceAccess.ResolveStore(connection,footprint,network,company,minimumMembers,original). Footprint is an actual catalog EntityDef; no old overload/default prediction. NetworkService.ComponentOf(network,anchor,footprint) delegates INetworkGraphGeometry.Touches(world,line,anchor,footprint,identity). JSON component remains id, network, minimum_members explicitly positive; no authoring/payload change. ResourcePortResolution retains actual ready/unavailable facts and typed disconnected/pending/undersized/original-unavailable blockers.

## 4. Original migration file scope (historical)

| Action | File | Approximate lines | Risk |
|---|---|---:|---|
| New | Core/InstancedCapacityPolicy.cs | 65 | medium |
| Modify | Standard/Networks.cs private capacity wrapper/actual construction | 30 | medium |
| New | Standard/NetworkResourcePorts.cs | 130 | medium |
| Modify | InfrastructureModule, NetworkPortsBindings, ResourcePorts validation | 35 | high |
| New | authored network-port content and public physical proof | scoped | high |
| Modify | engine/protocol identity, stage/evidence/master | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Missing/foreign network, invalid minimum count | Content refusal before Initialize |
| Wrong selected space/provider or undeclared connection | Composition refusal |
| No physical connection or insufficient members | Typed unavailable facts, no stock/claims fabricated |
| Original endpoint differs | OriginalUnavailable, never substitute current component |
| Actual policy basis/quantum/count inconsistent | Original provider fault |
| Active receipts affected by construction/removal | Original topology admission and physical waits |
| Source mutation/throw | Existing terminal observer/commit boundary |

## 6. One current path

Development only. Delete the old private capacity wrapper, no compatibility reader, map proxy, local fallback, second inventory or network owner.

## 7. Verification

Current targeted Release build and five exact cases5576/0, including original connected366/0 and new replacement490/0. Physical construction/flow/manufacturing/work/hauling, private views, claims, saves/replay/isolation remain engine-owned. New replacement keeps settled pool/version and policy/route history, saves loaded and delivers3; insufficient members and native footprint disconnection refuse. No full/profile/family runtime. Broad P10/P7/P8/provider/claim/prepared-fault gates remain open. All attempts/identities and scope distinctions are in current linked evidence; original results below are historical.

## 8. Risks

Authored minimum membership is a gameplay requirement, not an engine/map size limit. Capacity instances must describe actual connected membership; they do not invent members. Policy quantum remains the same native resource step. Endpoint changes must not migrate protected receipts implicitly.

## 9. Out of scope

New transport media, rendering/platform adapters, Oil/business/Tycoon changes, arbitrary malformed/CLR/provider/performance/stress/full acceptance and publication.

## 10. Original migration sequence (historical)

1. Extract existing instanced capacity contract/implementation and strengthen named-port basis validation.
2. Add the JSON/network binding through the existing pool owner and admit connection-only entities.
3. Verify real unbuilt/undersized/shared/claimed/manufacturing/physical-transfer states using public player intent.
4. Refresh evidence/master/audit, without narrowing original requirements.

## 11. Scoped result — 2026-10-12

Core2.472.0/save154/replay1. One explicit instanced capacity contract and JSON declaration bind original NetworkService stock; no new ledger, clock or scheduler. Connection-only participants need no fabricated local storage; real flow keeps inlet/outlet requirements. Operating/haul validation now matches typed unavailable ports and existing unowned-public transaction access.

ONE NEW exact FINAL366/0/0 in24556ms, process24738ms, PID43880 exit0. Eight native/independent metadata, minute/half, owned-private/public-unowned point workflows physically construct/supply, manufacture/cancel, hold work tools and haul; saves/private facts/replay/isolation agree. Nine unused-metadata refusals. All four runtime attempts and five compilations retained in [evidence](../../docs/migration/P10_CONNECTED_RESOURCE_PORTS_VERIFICATION.md). No broad/full runtime or executable edit after FINAL.

Original221/185checked/36open unchanged; additional198. Native tile/all-provider/active-topology/lost-original/inbound/continuous/fault/stress/global/full gates remain OPEN. No staging/commit/push until full goal AND plan finish.

## 12. Historical A21 metadata closure

Fresh read-only metadata validation:1664 source anchors,921 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2852 paths/1004 Markdown files/8633 local links, broken0. All18 master rows parse. All221 original literal/checkbox/line anchors validated with0errors;185checked/36open. Driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2840/staged0. Protected sole-clock/G19 files remain exact; goal_complete remainsfalse/additional198. Ten prior selected inputs changed, six new executable inputs/two new production sources, eight new source anchors; original requirements unchanged. No runtime rerun was needed for documentation checks.

