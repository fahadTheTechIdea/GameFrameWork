# P3-PB1 — Public callback selected-space integrity

## 0. Problem statement

The existing public projection owners guard the callback provider but `PlayerView.CanSee`, `PlayerView.Inspect`, and `ViewFeatureRegistration.Create` do not include the selected spatial authority in that scope. A declared read-only mod can write a real map layer while answering a query, including a write restored to its old value.

## 1. Goals

1. Guard these callbacks against the actual selected space codec and invalidation revision before any result escapes.
2. Keep authorization, target entity state, staged output, first-fault handling and retained private publications in their current owners.
3. Prove healthy saved/replayed updates and actual physical hauling/manufacturing/construction using public input and facts.
4. Prove malformed callbacks through owner, foreign and authorized observer query paths without host-side state writes.

## 2. Design

Use the original `EntityObservation.ReadProvider` with `space: world.Space`. Visibility and inspectors also observe the existing target entity codec. No global default, duplicated ledger, blanket world hash, replacement visibility policy or additional clock is introduced. Mapless sessions retain a null spatial scope. Native map geometry and mod composition are unchanged.

The existing proof module arms its own transient bad callbacks. A new driver authors a scalar layer and a second time resolution through the existing JSON loader. It submits no privileged game-state mutations. Write, write/restore and throw-after-write run separately for eleven public query paths and two quanta.

## 3. API surface

No new production API. Engine identity becomes 2.506.0; save154/replay1 and view schemas remain unchanged. The existing test module gains negative instrumentation, not gameplay or persistence.

## 4. Files to change

- Modify `src/Beep.SimEngine/Hosting/PlayerView.cs`, `Kernel/ViewFeatureRegistration.cs`, and engine/protocol identity.
- Extend `tests/ExtensionProof/ViewObservationProof.cs` without changing existing authored defaults.
- Add `tests/Beep.SimEngine.Tests/PublicCallbackSpaceTests.cs` and register its one bounded case.
- Update the current tracker and evidence; retain original gate wording and historical results.

## 5. Error handling

Provider throws preserve the original exception. Space codec or revision mutation faults before returning facts, even when values are restored. Staged lists stay empty, out values stay default, first fault wins, live reads/save/hash stop, and publications retain each viewer's last safe facts. No rollback or instant fallback is claimed.

## 6. Compatibility

Development-only breaking engine identity; no shim or legacy load path.

## 7. Verification

One solution build and one new exact case with a 90-second process budget; repair only that case if required. The authorized final full regression remains deferred. This slice does not by itself close whole P3.008 or the full goal.

## 8. Risks

Selected-space hashing adds cost to these public callback paths. The physical continuation proves functional operation, not a whole-engine allocation/stress budget. Arbitrary unserialized CLR state remains a module contract responsibility.

## 9. Out of scope

Tycoon business rules, Oil content, platform rendering/adapters, new world dimensions, a new state/scheduler owner, and broad/full test execution.

## 10. Execution order

1. Extend the three current observation boundaries.
2. Add engine-side negative instrumentation and one public player driver.
3. Build, run the new bounded case, retain any repair evidence.
4. Update authoritative evidence and current tracker without overstating gate completion.

## Post-pass metadata verification

After FINAL, metadata-only checks validated1818 source anchors,1017 executable inputs and13 quickstart anchors with zero hash mismatches or executable drift. Repository inventory:3001 paths,1081Markdown files,8773 local links/0broken,18 primary/historical master rows/0malformed. All221 original gate literals and line anchors validate unchanged. Diff check exits0 (34existing LF/CRLF warnings);2989dirty paths/0staged. All12Tycoon files and the sole clock implementation match the prior checkpoint. Three new files were created; the previously unanchored existing ViewFeatureRegistration source was explicitly added to source/input inventories. No full regression, staging, commit or push occurred.
