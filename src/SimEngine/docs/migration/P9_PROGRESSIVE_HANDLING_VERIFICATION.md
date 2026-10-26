# P9-C4c — Progressive native cargo handling verification

Date: 2026-10-05. Working source **2.64.0/save36/work17/work-view19**.
Transporter2/logistics4/logistics-view8/flow4/company-alert1 and native inventory layouts unchanged.
[Stage](../../plans/enhancements/P9_PROGRESSIVE_HANDLING_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Exact focused gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Progressive handling: native holds, prefix timing and partial checkpoints"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Progressive handling: physical chunks, interruptions and saved continuation"
```

Final build: **2.12 seconds**, zero warnings/errors. Native/prefix/checkpoint suite: **83 passed / 531 ms**;
physical suite: **108 passed / 742 ms**. **191 checks / two exact suites / zero failures / zero aborted suites**.
Summed reported suite time: 1,273 ms. Both complete names selected exactly one suite against the same
already-built binaries. Only these two distinct names were run/repeated; no family, third or full suite.
Other suites compiled only. Four directly dependent test files received current identity assertions,
not claims of fresh execution. The earlier timed-handling tests were migrated in place, not duplicated.

The first gate exposed a whole-batch assumption in post-restore orphan validation. WorkBoard now has
one phase-aware HaulReceiptAmount calculation shared by ordinary and orphan checks. An intermediate
physical gate also caught a proof-history retention mismatch: the selected JSON retained two facts.
The proof now explicitly selects the existing master-listed 64-record flow definition; no runtime
retention limit/default, second loader or fabricated gameplay rule was introduced. Final direct-call
review added the existing dispatch eligibility contract and shared whole-vector hold admission.

Source-only checks covered 31 explicit stage paths, including untracked documents/source: scoped
git diff --check and trailing-whitespace search reported no findings. Git emitted ordinary LF/CRLF
policy notices only. Subsequent edits were documentation only; no runtime changes followed the gate.

## Proven ownership and behavior

- The existing authored handling definition and pure positive monotone CheckHandling contract
  determine completed native quantity. HaulHandling searches cumulative native prefixes using the
  same exact session time conversion, including stepped independent profiles and different phase
  packing increments. No timer, percentage balance, implicit chunk rate or game-size cap was added.
- ResourceReservationBook.Increase grows one existing owned outbound row under the same token.
  ResourceTransactions.ExtendClaim verifies existing receipts, full-vector capacity, available
  native stock, quantum, identity/version, unchanged stock and unrelated promises through its
  existing checkpoint. Growth creates no stock or flow history. Stale records and protected-stock
  conflicts refuse without mutation. The same book/Sync remains authoritative.
- Load/Unload return actual ResourceTransferResult. Verified chunks move real source/cargo/destination
  stock; current cargo protection grows and then reduces under its original token. Job.Amount remains
  original intent; Job.Loaded is actual held job cargo. WorkSystem alone advances HandlingProgress.
  Native source/cargo inlet receipts retain Amount-Loaded; destination protection is full before
  delivery and actual pending remainder during unloading. No copied cargo or receipt ledger exists.
- Physical reach, actual service slot, current toil/registered participant, assignment, policy and
  positive effective work rate gate direct calls. Repeating already-spent service cannot transfer
  twice. Dock waiting and policy pauses earn no time, cannot preempt paused service, and save exactly.
- Payment shortage yields a typed wait with already paid cargo and remaining promises intact.
  Retries earn no additional service while blocked. Funding the remainder continues physical work,
  charging only actual chunks. Missing participants end execution without refund or cargo deletion.
- Loading/unloading interruption releases only outstanding job claims. Already received/delivered
  stock remains physical; the existing timed return path conserves it and continues after save/load.
  Actual chunk history uses existing ResourceFlowFact/journal/private views, never original-shipment
  success facts for partial transfers.
- WorkBoard validates phase receipts, native loaded quantity, causal handled <= earned and sole saved
  progress both on writes and after complete restore. Corrupt partial stock/progress/claims refuse.
  Immutable private WorkJobView exposes resource, original amount, held, handled and remaining facts.
  Interleaved sessions, saved twins, fractional worker rates and different endpoint quanta pass.
- Three independent public-only provider faults exercise normal Update: wrong source take, wrong
  inlet receive and incorrect native hold growth. They fault with no resumable save/hash. Faults do
  not trigger compensation, instant transfer or a replacement endpoint. This is not the complete
  adversarial-provider matrix.

## Scope and open gates

Only standalone framework runtime, public-only proof/tests and linked framework documentation changed.
No OilFieldDays/Tycoon, host/demo gameplay, adapter, renderer, platform or Draw implementation.
Development semantics replace whole-batch hauling; no old signature/reader or compatibility branch.

The lifecycle skill kept implementation and bounded proof in the same tracked stage. Full framework
completion is not claimed. General congestion/collision/replanning, broader dock services, fleet/plans/
editable areas, starvation/preemption, provider matrix, replay/lockstep/fuzz/soak/performance and full
acceptance remain open. Earlier stage reports remain historical evidence, not current regression proof.
