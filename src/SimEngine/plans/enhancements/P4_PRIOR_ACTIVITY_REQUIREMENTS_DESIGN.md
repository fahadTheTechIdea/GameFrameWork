# P4-G2b prior-activity requirements and compact completion facts

## Problem and stages

WorkBoard.Prune removes ordinary terminal orders. Authored workflows therefore cannot require
earned completion after pruning. Preserve compact evidence in SAME WorkBoard, not a second
activity manager/knowledge/plan ledger. Existing full orders remain authoritative while retained;
pruning transfers completed evidence into one latest fact per company/target/plan key.

1. Add immutable ActivityCompletionFact and IActivityCompletionHistory alias to SAME WorkBoard.
   Record actual order creation/completion game time; latest is completion time then order ID.
   Target IDs are exact generations, never prototype or placement aliases. No count/stock copy.
2. Stamp CompletedTick only when actual full order/effects reach Done. Failed, cancelled,
   partial and completing orders earn no fact. Compact only during normal Prune, merge latest
   canonically and preserve evidence independently of later target removal/history retirement.
3. Save/hash compact facts in the board's existing codec, sorted company/target/plan. Validate
   exact identity, timestamps, known composed plan, no duplicate keys or retained order overlap.
   No legacy decoder/default timestamps. WorkBoard42/save137; unchanged WorkSystem41.
4. Register strict prior_activity_requirements with explicit plan key/scope/blocker. Scope is
   target or company; resolution uses actual existing plan resolvers, not an activity-only catalog.
   Optional generic binding-validation contract checks composed requirements before Initialize.
5. Add private immutable WorkView completion reads. Existing all/any diagnostics/commands/jobs/
   material receipts and physical timing act on the SAME requirement without host rules.
6. ONE distinct exact NEW bounded native JSON player case proves partial/cancelled absence,
   physical completion, pruning, same/other target scope, repeated latest evidence, saved twins,
   private/pure views, interleaved sessions and invalid content. Then freeze source/test/JSON.

## Semantics and exclusions

JSON references existing plan keys (e.g. activity:survey.sample, or a selected provider's key),
not runtime order IDs. An explicit target scope means any earned execution on this actual target
generation; company means the latest earned company execution on any target. Returned facts
retain exact execution ID and both timestamps. Exact predecessor-instance constraints remain
existing plan dependencies, not an invented implicit sequence. This does not make a cyclic
cross-activity prerequisite graph executable or provide implicit starter completion. Broader
cross-plan feedback validation was subsequently scoped-verified in the
[prerequisite validation stage](P4_ACTIVITY_PREREQUISITE_VALIDATION_DESIGN.md) at2.266.0.
Dynamic generation/replacement and physical feedback feasibility remain open.

- [x] Compact facts through existing completion/pruning/Sync owner.
- [x] Strict contract/JSON prerequisites and private completion reads.
- [x] Physical scoped saved proof and focused evidence/tracker update.

Verified2026-10-07: build2.237.0/save137/board42/view78/WorkSystem41. ONE clean build10.83s,
ONE exact NEW bounded case68 checks0 failures/aborts430ms first attempt.109 older files/182
build/save assertion lines compile ONLY. Source/test/JSON frozen after green.
[Evidence](../../docs/migration/P4_PRIOR_ACTIVITY_REQUIREMENTS_VERIFICATION.md).

No new system/service/store owner, scheduler, clock, instant completion or compatibility path.
Older identity/save/layout assertions migrate/compile ONLY. No older/family/full/platform/Tycoon
gate or per-slice commit/push. Entire P1–P12 goal remains ACTIVE until all requirements are proven.
