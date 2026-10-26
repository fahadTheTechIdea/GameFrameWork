# P7-D1: Explicit physical disposition composition

## 0. Problem statement

The two `ResourceDispositionTests` literals cannot load: their shared recovery fixture contains an unused Tycoon supplier, but selects a resource-only mode. Its default Colony master also lists navigation/startup documents for `colony.tiles`, not the selected `recovery.tiles`. These are fixture composition failures, not permission to activate business or weaken engine validation.

## 1. Goals

1. Select an explicit JSON master for physical recovery/disposition, with actual recovery-map navigation and engine-owned startup.
2. Reuse the existing Colony installed-module composition, WorkBoard, native transactions, incident response, logistics, stock, receipts and clock. No parallel gameplay owner.
3. Preserve the two existing literals' named episodes, exact native destruction/loss/consumption, cargo interruption, private facts and saved continuation.
4. Bound physical waits by both update count and elapsed deadline with diagnostic context.

## 2. Design

`ResourceDispositionTests.Factory` explicitly authors a physical subset of the existing recovery content: omit its unused `market` prototype, select world setup, and select the declared recovery startup profile. This is test content selection, not an engine loader fallback. Other recovery-content consumers are not silently migrated or claimed verified.

New master-listed recovery navigation/startup documents declare native geometry and machines. The existing `WorkerExecutionTestContent` helper authors only native JSON controls; no scheduler or availability logic is implemented by tests. Cargo privacy uses parsed JSON, not whitespace-dependent string replacement. Cargo recovery remains an explicitly selected module/profile.

## 3. API surface

No public engine API or save/replay schema changes. Existing `Colony.Factory(..., masterDocument:, moduleDocuments:)` supplies the master-selected files.

## 4. Files

- New: this design, `resource-disposition-master.json`, `resource-disposition-startup.json`, `resource-disposition-navigation.json`, verification report.
- Modify: proof project resource declarations, `ResourceDispositionTests.cs`, active tracker and checkpoint documentation.
- Production code changes only if focused evidence exposes an actual engine defect; document separately before changing ownership or semantics.

## 5. Error handling

Missing/invalid providers, references and quantities still refuse at composition. Faulted sessions retain the first failure and cannot expose healthy live views, hashes or saves. Timed-out physical waits fail with current work/motion/clock diagnostics, not instant completion or extended limits.

## 6. Compatibility

Development only; no compatibility path or auto-detection. Default Colony and unrelated old recovery callers are not substituted implicitly.

## 7. Verification

Build the affected test project once changes require compilation, then run only these literal cases with the bounded runner:

- `Resource disposition: named episodes, authored native effects and saved physical response`
- `Cargo disposition: mixed stock, private diagnostics and interrupted saved return`

Record every attempt, including the existing composition failures. No full/profile/family runtime. These cases are low-level protocol/provider diagnostics, not proof of the complete public-player acceptance scenario.

## 8. Risks

Old assertions may assume obsolete automatic dispatch or lifecycle publication behavior. Inspect actual owners and exact stock/claims before changing expectations. Do not suppress provider faults, relax physical collision or enable money to obtain green tests.

## 9. Out of scope

Tycoon behavior, other recovery fixture consumers, rendering/platform integrations, optional quality/lot selection, full accounting/stress closure, Git publication.

## 10. Execution

1. Author explicit master/navigation/startup and native worker controls.
2. Build; run the two exact cases and repair only evidence-backed defects/obsolete assertions.
3. Record source/runtime scope and update the existing master. P7.017 and the full goal remain open pending their broader evidence.

## Completed scoped result

- [x] Explicit composition and native physical execution selected without activating business.
- [x] Both literals repaired and extended: response93 / cargo111, zero final failures.
- [x] Partial-unload saved original receipts, exact six-fact stock chain and elapsed-bounded diagnostics.
- [x] [Every attempt, source mapping and limitations](../../docs/migration/P7_DISPOSITION_COMPOSITION_VERIFICATION.md) recorded.
- [ ] Original broader P7.017 accounting/provider/stress gate (not closed by this slice).
