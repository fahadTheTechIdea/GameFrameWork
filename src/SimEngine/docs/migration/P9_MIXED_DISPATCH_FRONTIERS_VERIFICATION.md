# P9-B4e — Priority-preserving mixed dispatch frontiers: focused evidence

Date: 2026-10-08. Canonical repository: Beep.SimEngine.
[Stage](../../plans/enhancements/P9_MIXED_DISPATCH_FRONTIERS_DESIGN.md).
[Master](../../plans/enhancements/MASTER_TRACKER.md).
[Self-excluded source manifest](P9_MIXED_DISPATCH_FRONTIERS_SOURCE_MANIFEST.json).

## Outcome and actual defect

The original WorkSystem.Assign already ranks offers correctly, but its native member collection
previously scanned ALL later same-company native offers. A native priority1000 job could therefore
batch a native priority1 job across a ready priority100 public/native shipment. The low job reserved
the shipment's finite source materials before the higher-ranked shipment was attempted.

The SAME existing Assign loop now skips exhausted/blocked/unmatched-worker offers and stops at the
first runnable foreign-provider/company admission, or standalone native worker admission. A native
whole-set cannot cross that frontier. On actual acquisition the original pass refreshes physical
supplies/candidates and reconstructs the next ranked frontier. No second dispatcher, temporary saved
allocation, claim authority, rollback/compensation or compatibility branch was added.

Build2.298.0/save143/replay1/work-view84; persistent layouts unchanged. 165 older C# identity paths
migrate compile-only. One explicit master-listed provider document owns the authored sink, inspector,
courier, timed activity and shipment. Existing pure Factory accepts its explicit document/path; no
new test/harness owns assignment, stock, loading, productive progress, transport or recovery rules.

## All executable verification attempts

ONE build only:
`dotnet build Beep.SimEngine.slnx --no-restore`
Clean, **0 warnings / 0 errors**, elapsed **5.45 seconds**.

ONE attempt of ONE distinct NEW bounded runtime case:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Mixed dispatch frontiers: priority barriers, physical stock and saved recovery"`
**194 passed, 0 failed, 0 aborted**, **1614 ms**, first attempt.
No runtime repair/rerun, prior suite, family/full filter, Oil, Tycoon or platform runtime.

Executable source/proof/tests/JSON frozen immediately after green at **2026-10-08 12:40:59 UTC**.
Only documentation/manifest edits and read-only checks afterward.

## Real physical stages covered by that single case

Four actual engine combinations: staffed/timed low work, each competing with independent-public/native
shipment intent. A fifth blocked-carrier control checks that unavailable higher offers do not stall
an otherwise feasible same-company native frontier.

All gameplay uses normal player commands, previews, immutable views and engine Update. Public snapshot
identities locate authored actors; no test allocates jobs, claims workers/materials, updates stocks,
moves machines, advances node progress, completes orders or implements policy.

- Begin with actual source8, highest native target20, sink0 and separate physical inspectors/courier.
  Work consumes8 per authored activity; loading/unloading take actual authored elapsed game time.
- The engine admits actual work/courier policies in one Update; timed low admission enters that SAME
  Update so it genuinely competes rather than starting before the carrier becomes eligible.
- High work owns its machine; low work owns NO input/worker/progress across the shipment frontier.
  Actual shipment owns its distinct carrier and eight-unit finite source/cargo promise.
  Sink remains zero at assignment: no instant delivery.
- Save during actual competing claims, then physically finish high work and shipment in bounded stages.
  Source0, native target12, sink8; total20 equals initial28 minus8 true productive consumption.
  Unfunded low work remains typed-blocked and unassigned; it never borrows spent/held inputs.
- The player cancels the blocked order, configures ordinary stock demand, waits for physical replenishment,
  disables that demand, then posts a fresh normal activity. No instant stock grant or fabricated fallback.
  The real supplied low work consumes8; total12 equals28 minus16 true productive consumption.
- Private assignment facts are invisible to the foreign company. Normal command previews are pure.
- Pending/claimed/continuing saved twins remain identical, a separate interleaved session remains untouched,
  and canonical command replay reproduces final physical state in all five stages.
- Each physical wait is bounded by1200 normal updates with authoritative clock/job/state/blocker/loaded
  diagnostics on timeout. Replay bounds8MiB/5000 are test-resource limits, not engine/world-size limits.

## Review and remaining gates

The lifecycle/SOLID review kept ranking and native batch execution in their original responsible
WorkSystem and reservations/execution in the original WorkBoard/Logistics owners. Public-only provider
participation uses its existing contract; no source-specific scheduler exception exists.

This is not distributed atomic acquisition across unrelated providers. It does not prove the exhaustive
mixed timed/staffed scarcity matrix, provider observation/purity under hostile callbacks, sustained
fairness/CPU/stress/soak/allocation, lockstep/shuffled admission, global host-boundary coverage, all spatial
utilities, independent game acceptance, Oil/domain migration or the explicitly authorized P6/full plan.
Earlier results are historical, not rerun on2.298.0. The full P1-P12 goal remains ACTIVE.

No staging/commit/push: publication is authorized only AFTER the FULL goal AND plan finish.

## Source audit

Manifest uses stable normalized unique SHA256 paths and excludes itself. It includes the prior B4d
manifest so that earlier evidence remains traceable. Actual git diff --check exited0 with31 existing
line-ending warnings. All18 existing phase rows retain five columns. Audit figures and final hash/link
checks are recorded in the manifest; no executable edits followed the freeze.

1815 self-excluded paths,594 Markdown and3679 local links. Of359 prior runtime paths, only original
Work.cs and the build identity changed. Prior330 authored extension JSON remain unchanged; one new
explicit frontier JSON added. Sole GameClock SHA256 remains
35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.
