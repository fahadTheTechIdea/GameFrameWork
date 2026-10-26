# P8-F6: executing and queued recipe disclosure

## 0. Problem statement

TransformationManagementView.TryGetTransformation and CopyQueue publish current queue/head facts without the metadata closure already used by recipe menus and admission. ReadExecutionPreview also includes the entire current processor snapshot, so asking about a visible recipe can disclose another hidden queued recipe.

## 1. Goals

1. Current processor facts are published only when every actual queued recipe and its referenced metadata can be disclosed through the original visibility owner.
2. A denied projection returns false/default, false/empty queue, or UnknownDef/null preview; no visible prefix of a mixed queue escapes.
3. Forgetting knowledge changes visibility only, never authoritative queue identities, WIP, held tools, progress or physical execution.
4. Native/independent recipes, minute/half-minute clocks, hidden head/later entry, saved pending forgetting, physical re-discovery and replay use one path.
5. All original broad P8/P11/full-plan gates remain open unless their whole scope is independently proven.

## 2. Design

Extend the existing internal TransformationContracts partial with CanDiscloseExecution(World, PlayerId, TransformationSnapshot), walking actual detached queue entries through the existing CanDisclose closure. The original transformation provider observation remains responsible for snapshot validity and physical state; no new catalog, queue, knowledge or scheduler owner.

TryGetTransformation captures and checks the entire snapshot inside one provider/entity observation before assigning out facts. CopyQueue reuses that method and clears its caller destination before selection. ProjectExecution refuses a hidden current queue; ReadExecutionPreview translates that withheld projection to UnknownDef, preserving independent recipe metadata queries and private ownership semantics.

The new ordinary driver reuses the existing pure JSON composition fixture, adding authored forgetting policy and a harmless independent visible recipe. It uses public Preview/Submit/results, views, Update, clock, saves and replay only; no engine-internal stock/progress/knowledge edits or TestKit execution helper.

## 3. API surface

Existing signatures retained; stricter disclosure semantics only. Build identity advances, save154/replay1 unchanged. A default refused snapshot is absent, not a fabricated idle processor; callers must respect false.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | src/Beep.SimEngine/Standard/TransformationDisclosure.cs | medium |
| Modify | src/Beep.SimEngine/Views/TransformationManagementView.cs | medium |
| Modify | src/Beep.SimEngine/Kernel/SaveGame.cs and current protocol fixture | low |
| Modify | existing RecipeDisclosureTests pure fixture, registration/resource list | low |
| New | tests/Beep.SimEngine.Tests/ExecutionDisclosureTests.cs | low |
| New | tests/ExtensionProof/Content/execution-disclosure.json | low |
| Modify | tracker, quickstart metadata and completion audit | low |

## 5. Error handling

Original throwing/mutating provider and visibility observations retain first-fault behavior. Out facts are assigned only after observation succeeds. Missing/private processor keeps NotFound/false. Hidden queue keeps UnknownDef/false, never cancellation or a synthetic replacement.

## 6. Compatibility

Development-only; no compatibility shim or duplicated view/state model.

## 7. Verification

One solution compile followed by ONE NEW bounded exact case; only same-case repairs. Outer60000ms/termination5000ms/output30000 characters; physical waits bounded512 Updates. No old/family/full runtime. Freeze selected executable inputs after final pass and close documentation only.

## 8. Risks

Withholding whole mixed queues is intentional to avoid revealing hidden identities or ordering through a known prefix. Previously retained immutable facts stay detached; forgetting does not rewrite player history. A queued hidden entry must not affect access to unrelated standalone visible recipe metadata.

## 9. Out of scope

All-work/job/fog/snapshot-delta/provider-fault/performance matrices, protected G19/Tycoon changes, Oil consumer, Draw/platforms/adapters. No staging/commit/push until the full goal and plan are complete.

## 10. Execution order

1. Share the existing recipe disclosure closure across live snapshots, queue copies and execution projections.
2. Author ordinary forgetting/re-learning and mixed queue proof, compile, run one new exact case.
3. Record actual attempts and update authoritative tracker/audit without broader completion claims.

## 11. Scoped result

Implemented on2.436.0/save154/replay1. FINAL485/0/0 in11207ms; original221/176checked/45open unchanged, additional158. Eight native/direct minute/half-minute flows cross hidden head/later entry. Original access rule pauses a hidden result at the head while retaining WIP/tools/progress; visible head continues despite hidden later recipe. Earned rediscovery resumes productive time. No new execution rule, queue/knowledge owner or clock. All three attempts and three clean compiles retained; no executable edits/runtime after FINAL. Full P8/P11/P1-P12/publication remain OPEN. [Evidence](../../docs/migration/P8_EXECUTION_DISCLOSURE_VERIFICATION.md).

Read-only closure:1394 source/13 quickstart anchors and693 selected executable inputs frozen at2026-10-11 12:54:12 UTC all match with zero drift. All221 original literal requirements/checkboxes unchanged;204 line anchors moved six lines.2646 paths/924 Markdown/8272 local links, no broken targets or thin-driver boundary matches;18 valid master rows. git diff --check exit0 with34 existing line-ending warning lines; dirty2634/staged0. Four protected clock/Tycoon hashes unchanged. No publication or global completion claim.
