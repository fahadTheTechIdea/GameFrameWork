# P4-G71 — Sole execution ownership verification

Status: VERIFIED-SOURCE for unchanged engine2.445.0/save154/replay1. This is the
original structural P4.acceptance.015 requirement, not new gameplay/runtime proof.

## Actual current-source evidence

[Read-only verifier](VerifyExecutionOwners.ps1) reads all395production C# files and
records one declaration each for ResolvedContent, DependencyGraph, ActivityGraph,
ResourceInventoryState, ResourceReservationBook, Order, ActivityNodeExecution and
Job. WorkBoard has23partial declarations in its one canonical namespace/type.
Eleven concrete alias/loader/promise witnesses pass; obsolete loader/executor/
catalog/ledger declaration inventory is empty. [Manifest](P4_EXECUTION_OWNERSHIP_SOURCE_MANIFEST.json)
records current production hashes and each declaration/witness path and line.

### Reviewed ownership distinctions

ResolvedContent owns the typed frozen catalog and IResourceCatalog aliases its
actual Resources. ContentSet raw documents are input, not another typed authority.
Unknown content sections fail ResolvedContent.Build; no patches loader or public
generated-definition/prototype factory remains. Current source review confirms
the P2-F9 deletion remains present; that historical runtime was not rerun.

DependencyGraph is immutable topology compilation. ActivityGraph wraps its same
node topology; derived physical-work reachability is not another graph owner.
Order.BindNodes creates one live node record and inserts the same instance into
list and named lookup. WorkBoard owns actual orders/jobs/plans/history under its
one codec. WorkSystem executes those same jobs through IWorkPhaseExecutor. No
old step interpreter survives; PlanPlaybooks.Step is authored planning topology
and PlaybookStepSnapshot is detached diagnostics, not live physical execution.

ResourceInventoryState owns one stock list and one ResourceReservationBook,
deriving inbound/outbound quantities from that book. Spatial pool and optional
supplier books represent separate actual endpoints/custody, not a second ledger
for the same inventory. Tool/dock/machine claims are distinct physical dimensions;
transaction overlays, immutable receipts and views are facts, not shadow stock.
This does not close broad resource/provider transaction semantics or hidden limits.

## Verification boundary

No production, protocol, module, test, demo or harness edits. No compile or
gameplay runtime rerun: unchanged game code already has P2-F11's112-check exact
physical contract run on2.445.0. That result remains its original scoped evidence,
not a new run or blanket all-system acceptance. The new source verifier was run
after its final source edit and found zero errors. Source manifest395hashes must
match actual current production before closure. Regex counts alone are not proof;
the ownership/alias relationships above were reviewed separately.

## Original gate and limits

Only unchanged-text P4.acceptance.015 is checked: original221gates become
180checked/41open, additional170. P4.010 cancellation/loss/effects and P4.014
private shared evaluator breadth remain open, as do P7.020 balances/hidden limits/
old fields, arbitrary provider purity, global/stress/full P1–P12 and publication.
No protected G19/clock, Oil or platform changes. No staging/commit/push before
the whole goal AND plan have finished and been verified.

[Stage](../../plans/enhancements/P4_EXECUTION_OWNERSHIP_DESIGN.md)

## Current read-only integrity result

1460source anchors/738selected inputs/13quickstart anchors, zero drift;395source
manifest production hashes current. All previous737selected executable inputs
unchanged; only one new read-only script added. All221literal requirement text/
line anchors retained, only P4.acceptance.015 checkbox changed.18master rows valid;
2702workspace paths/948Markdown/8544local links, zero broken. Existing positive
driver boundary matches0; git diff --check exit0 with34existing line-ending
warnings. Four protected clock/G19 hashes unchanged; dirty2690/staged0. No game
or test executable mutation, compile, gameplay runtime rerun or early publication.
