# P11-D14 — Authored knowledge sharing: current scoped evidence

2026-10-08, canonical Beep.SimEngine only. [Stage](../../plans/enhancements/P11_KNOWLEDGE_SHARING_DESIGN.md).
Full goal and P1–P12 are still active/incomplete. Commit/push is deferred until FULL verified completion.

## Implementation and responsibility

Same CompanyKnowledgeService aliases IKnowledgeSharing and retains its sole ledger/Sync. JSON content
owns explicit canonical subject/provider sets and directional company pairs. Actual providers opt in
through IKnowledgeSharingProvider; no universal sharing of research/unknown providers. Existing native
authored provider opts in for its validated subjects. IKnowledgeSharingPolicy/KnowledgeSharingPolicyDef
supports contract-authored extension content; no native coordinate/provider-ID special case in ledger.

Ordinary ShareKnowledgeCommand contains profile/recipient only; sender is stamped by existing inbox.
Private KnowledgeView and ordinary command use the same whole-set admission. Source-only snapshots
copy/read-only wrap entries and retain already-frozen data. Recipient holdings never enter native
preview/result facts. Commit rebuilds admission, makes one transient candidate from original ledger,
applies first/latest retention, validates whole candidate through original providers, then commits once.
No source removal, arbitrary input data, physical material creation or knowledge/cursor/clock copy.

Recipient LearnedTick records actual receipt. ObservedTick/data/schema remain source capture facts.
The old ObservedTick >= LearnedTick/first equality restriction is replaced: both timestamps independently
must lie within the actual game clock. This correctly permits newly acquired old information. Latest
only replaces strictly older observations and preserves recipient first acquisition; first and equal
timestamps preserve existing recipient data. Repeat acceptance conceals whether a recipient changed.
Native visibility/freshness reads the same ledger. Sharing cannot bypass private layer ownership.

Build2.263.0, Save140, CompanyKnowledgeService semantic schema3, KnowledgeView4. Existing native provider3
and payload fields remain unchanged. One new normal command; no additional persistent owner/module/time
or relationship table. No compatibility reader or older semantics mode.

## Every build and runtime attempt

1. `dotnet build Beep.SimEngine.slnx --no-restore`: exit0,10.16s,0 warnings/errors. Initial call yielded
   live session27552; the SAME live session was polled to successful completion, not restarted.
2. `dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Knowledge sharing: authored private exchange preserves physical observation age"`:
   FIRST attempt exit0,104 passed,0 failed,0 aborted,622ms; exactly ONE distinct NEW bounded case.

No repairs; gameplay/public proof/tests/JSON frozen immediately after green. Remaining edits docs-only.
127 older strict build-identity files/lines migrated compile-only; three ledger semantic-schema assertions
also migrated compile-only. Existing ContactExplorationTests.Factory adds optional JSON document assembly
and internal fixture access, not gameplay. Previous cases and all old/family/full/platform/stale suites
were NOT run. Observed622ms is NOT a CPU/allocation budget or full acceptance result.

## Proven native acceptance scope

- Actual master-listed profile file/canonical subjects/directional permission; typed unknown/malformed/
  self/unseated/unearned/forbidden refusal without data, money/account requirement or fake observation.
- Native machine actually moves into a region and earns contact discovery through positive observation
  time; travelling twin restore agrees. No raw spawning/stock/knowledge/clock/progress/layer mutations.
- One outgoing region known while survey still missing: ordinary whole command refuses with no recipient
  prefix. Preview is pure, private and frozen; it reveals only source knowledge. Authorized sharing
  remains distinct from remote inspection of source or recipient companies.
- Pending share is snapshotted by original inbox, caller fields subsequently changed, original pending
  intent saves/restores and succeeds identically. First recipient learning is actual receipt time while
  original older observation time/data persist. Original public regional visibility changes accordingly;
  mixed unknown/known samples refuse atomically. First repeat neither relearns nor reobserves.
- Normal player release/post commands assign real machine survey. Partial physical survey consumes actual
  eight materials20→12 and grants no reading; saved continuation completes normally. Whole share now has
  actual observations. Recipient age2/fresh derives from original age, not receipt/reset/default.
- Paused saved twins do not age. One resumed original interval reaches exact age3/stale. Re-sharing stale
  information does not refresh it. Actual positive-time re-survey alone creates newer observation time.
  Reverse older transmission cannot replace newer source; forward newer merge preserves recipient's
  first learning. Detached older snapshots retain their original data/time/status.
- Private native deposit samples remain inaccessible to other companies even with deliberately shared
  historical readings. Sharing neither changes private layer owner nor grants blanket sight/subscription.
- Completed first/latest chronology round-trips. Recipient has only two explicitly transmitted subjects;
  actual stock remains12. Interleaved session has no shared history. No JSON profiles implies no implicit
  sharing permission.13 malformed policy forms fail initialization. Research opt-out assertion is a
  native type capability check, NOT an exercised research/provider integration gate.
- Each physical stage uses at most1000 Update calls with clock/position/order/blocker failure diagnostics.

## Open limits and publication

External policy/provider adoption, declined shares, provider-specific dependencies, hostile provider/
world-global purity/fault/corrupt save matrix, equal-timestamp conflicting data, first-retention differing
payloads, non-tile/no-work/no-space composition, communication delay/diplomacy/automatic subscriptions,
forgetting/remote exploration/rival strategy, large allocations/CPU/replay/lockstep/fuzz/soak and FULL
P11/P1–P12 acceptance remain OPEN. Sharing is immediate information transmission through ordinary
command Update, not instant completion of physical work or cargo transport. Previous runtime gates NOT
rerun at2.263.0. Lifecycle review kept immutable policy, original learned ledger and sender-only views
as separate responsibilities. No Oil/Tycoon business source/host/demo/Draw/platform work.

## Source-only inventory

[Manifest](P11_KNOWLEDGE_SHARING_SOURCE_MANIFEST.json) inventories selected accumulated source/docs,
not all repository state or runtime behavior. Historical manifests stay historical; own manifest is
excluded from its hashes. Final hashes/link checks and actual diff-check are recorded there.
Scope660 paths/135 Markdown/2099 local links/0 broken/0 duplicate paths. Actual `git diff --check`
exit0 with existing LF/CRLF warnings. Sole GameClock source hash is unchanged:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.
