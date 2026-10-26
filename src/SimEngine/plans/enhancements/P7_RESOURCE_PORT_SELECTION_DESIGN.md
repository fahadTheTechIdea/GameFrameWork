# P7-PS1: engine-owned resource-port selection — Design Document

Current contract2.711/save161/replay2: InitialEndpoint and RetainsOriginalAccess are required metadata beside Choices. InitialEndpoint comes from explicit JSON intent and must be a declared choice before initialization, independent of choice order. New replacement capabilities use it; retained capabilities keep their actual saved selection. Actual prepared selection is validated before registration. [Current evidence and limits](../../docs/migration/P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m50-authored-initial-port-bindings--2026-10-22). Earlier PS1 measurements below retain their historical identities.

## 0. Problem Statement

tests/ExtensionProof/OutputPortProof.cs owns SelectPoolPortCommand/Handler and its company/choice/endpoint decision rules. That is normal routing gameplay living outside the framework and casting to PoolPort. It prevents other resource-port providers from reusing the same player command and preview.

## 1. Goals

1. SimEngine owns one public SelectResourcePortCommand with identical preview/command refusal rules through contracts only.
2. Providers declare JSON-resolved immutable choices and own their selected reference; no command/permission/resource-transfer rules remain in the proof module.
3. Selection changes no stock, receipt, work progress, cargo, clock or endpoint identity. Existing logistics handles original-binding loss and physical recovery.
4. All-loaded selection metadata is admitted before initialization; invalid, duplicate or foreign choices refuse without defaults.
5. One new bounded public-player acceptance proves pure previews/private choices, pending command save/load/replay, actual carrier wait/restoration/residual delivery and session isolation.

## 2. Design

IResourcePortSelectionDefinition extends IResourcePortDefinition with Choices, InitialEndpoint and RetainsOriginalAccess; IResourcePortSelection extends IResourcePort with SelectedEndpoint, ResolveChoice and Select. Existing Standard/ResourcePortSelection.cs owns metadata admission, detached sorted choice facts, candidate observation, replacement selection validation and actual selection verification. The existing resource-port validator remains the actual store/packing authority; the existing resource transaction handles pure endpoint-access probing without acquiring promises.

Commands/SelectResourcePortCommand.cs owns player/site/declared-choice/actual endpoint/packing/permission validation and fresh commit. InventoryModule registers sim.select-resource-port; its existing InventoryView exposes immutable private choice diagnostics. ResourceTransactions validates all loaded selection metadata using existing composition lifecycle.

PoolPortDef derives choice metadata from its actual JSON pool/alternatives. PoolPort only resolves declared aliases and mutates its existing selected field via the contract. Remove the proof command/handler/registration; migrate existing proof callers to the engine command, without compatibility aliases or duplicate paths.

## 3. API surface

SelectResourcePortCommand: Entity, Port, Endpoint. IResourcePortSelectionDefinition.Choices/InitialEndpoint/RetainsOriginalAccess; IResourcePortSelection.SelectedEndpoint/ResolveChoice(World,endpoint)/Select(World,endpoint). InventoryView.ReadResourcePortChoices returns detached endpoint/selected/blocker facts, never live stores or book handles. PoolPortDef's required JSON pool reference supplies InitialEndpoint; alternatives only add choices and never override initialization by position.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---:|---|
| New | Standard/ResourcePortSelection.cs | 150 | medium |
| New | Commands/SelectResourcePortCommand.cs | 55 | medium |
| Modify | InventoryModule, InventoryView, ResourcePorts and ResourceTransactions | 45 | medium |
| Modify | OutputPortProof and existing proof callers | 100 | medium |
| New | one public source-selection acceptance driver | 180 | medium |
| Modify | engine and independent protocol identity, Program and stage tracker | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Missing/foreign site or missing capability | Existing not-found/not-owned/invalid-argument, no provider commit |
| Invalid/undeclared endpoint | Invalid-argument |
| Missing or inaccessible declared endpoint | Typed refusal, no stock or promises fabricated |
| Different actual packing/identity | Original terminal provider fault |
| Selection callback mutates stock or receipts | Original observation/commit safeguards; no healthy success claim |
| Original active physical port loses access | Existing logistics recovery, never teleport/rebind old cargo |
| Invalid all-loaded choice metadata | ContentException before Initialize |

## 6. Backward compatibility

Development-only. Delete old proof command/handler and ID, no compatibility shim or legacy routing path. Existing selected-state codec is unchanged; new command is self-described by the normal manifest.

## 7. Verification

One solution build, necessary targeted repairs only, one NEW exact bounded public-player case and retained attempts. No full/family/historical runtime. Existing callers compile only. No original broad gate closes from a narrow physical proof.

## 8. Risks

Choices are endpoint references, not balances or credentials. Providers must not silently substitute selected/original endpoints. Preview/read callbacks remain pure. Actual active claims belong to their original books across routing changes and must be recovered by existing coordinators.

## 9. Out of scope

Dynamic network capacity/ports, general unavailable-port result redesign, arbitrary private CLR/fault/stress matrices, Oil business/platform/Draw and publication.

## 10. Sub-task execution order

1. Add contract-based selection and metadata validation through existing owners.
2. Move normal command and choice views into InventoryModule; remove proof gameplay command.
3. Migrate proof provider/callers in place, without parallel models.
4. Verify actual physical original-binding wait/recovery using public commands; update evidence and whole-goal tracker.

## Current scoped result

Core2.470.0/save154/replay1. FINAL229/0/0 in5734ms: four native/point minute/half command/private choices/pending inbox/live cargo/original receipts/restoration/partial delivery/saved twins/replay/isolation flows. Duplicate/scope metadata refusals; three illegal reserve/clock/partial-throw callbacks fault and cannot checkpoint healthy. Original selected-field codec retained. Proof command/handler removed; sixteen existing caller files/twenty command sites use the engine-owned command.

One solution compile failed missing namespace import7.22s; three targeted compiles include negative clock-type failure3.69s and clean7.95s/5.15s. FIRST49/1, FINAL229/0/0; all attempts retained. No solution rerun, older/family/full runtime or executable edit after FINAL. [Evidence](../../docs/migration/P7_RESOURCE_PORT_SELECTION_VERIFICATION.md). Original221/185checked/36open unchanged; additional196. Unavailable-port/network-capacity/general CLR/provider/stress/performance/full and publication gates remain OPEN.

Metadata closure at 2026-10-12 01:41:06 UTC:1652 source anchors/13 quickstart anchors/908 selected executable inputs, zero SHA drift;2840 paths/1000 Markdown/8628 local links, zero broken;18 master rows/zero malformed; thin driver boundary matches0;git diff --check exit0 (34 warning lines),dirty2828/staged0. Original221/185checked/36open unchanged; additional196. Protected GameClock/Tycoon hashes unchanged, goal_complete false. Ten prior selected inputs changed;21 newly frozen inputs comprise five new executable files and16 previously existing migrated/helper files. Five existing production files changed/two new production files; seven source anchors added, no prior source anchor omitted. Old proof selection command/handler/registration paths0, engine command1/handler1, migrated callers16/sites20. No executable edit after FINAL or original gate text/checkbox/line-anchor change. Both attempts retained; no older/family/full runtime or terminal-solution success claim.

