# P3-S32 — Settled spatial withdrawal preparation

## 0. Problem

The affected extraction admission case exposed PointSpace.Withdraw entering public WriteLayer/visibility observation after SpatialResourceEndpoint marked an in-progress reserved debit. The unchanged layer codec correctly refuses unsettled endpoint state. Native tile value invalidation also needs visibility preflight before such a debit in visibility-enabled scenarios.

## 1. Goals

1. Keep public observation and visibility preparation outside unsettled resource commits.
2. Preserve original source receipts, exact material guards, single sparse stores and settled-save refusal.
3. Reject reused or stale preparations before any write, then verify saved physical extraction with focused cases.

## 2. Design

ISpatialResourceQuantity declares pure PrepareWithdrawal. SpatialResourceWithdrawalCommit is a single-use unsaved immediate preparation, not authorization, stock or a reservation owner. The existing endpoint prepares through EntityObservation while its book is settled, then uses the prepared commit under its original `_committing` allowance and consumes the original receipt. StageSync still rejects unsettled commits.

Native MapLayer prepares the original TileMap.ValueWrite. Normal Set and prepared depletion share CommitSample; TileMap validates original layer/owner/map/viewer revisions before sparse publication. PointSpace public writes and internal withdrawal share PrepareWrites/CommitWrites. Visibility and revision deltas are prepared once, not queried again after material changes. The independent sample provider captures its original owner/revision and retains SetSample as the sole writer.

## 3. API/state

One required quantity contract method and one short-lived preparation. No legacy default method or concrete fallback. Build2.583.0/save157/replay1; existing saved payloads unchanged. The historical point-space schema expectation is updated from4 to its actual existing5.

## 4. Files

Kernel/SpatialResourceValues.cs; Standard/SpatialResources.cs; Map/TileMap.cs; existing point and independent tile quantity providers; SpatialExtractionTests.cs; engine/protocol identity.

## 5. Errors

Illegal material, receipt protection, preparation reuse, changed actual layer/map/viewer revisions, overflow and unsettled checkpoint reads continue to refuse. Preparation observes all provider callbacks before authoritative commit and does not publish partial stock on a refused preparation.

## 6. Compatibility

Development-only required contract migration; no shim, old commit path or additional material/coordinator owner.

## 7. Verification

Two targeted builds succeeded with zero warnings/errors. Five exact focused cases pass409 checks: affected native/point admission109, preparation protocol33, native visibility-enabled physical extraction/survey92, independent physical extraction/survey89 and existing timed/blocking/competing/cancellation continuation86. [Evidence and attempts](../../docs/migration/P3_SPATIAL_WITHDRAWAL_PREPARATION_VERIFICATION.md). No full/profile/family tests.

## 8. Risks

Native visibility-enabled reserved extraction now has the stated physical proof. Broader hostile provider, mixed batches, failure during commit, utility, lifecycle, global save/replay/lockstep/stress/allocation and full-engine acceptance remain open.

## 9. Exclusions

Oil/Tycoon/platform/host behavior, relaxed save guards, copied transactions, instant extraction, full regressions and Git publication.

## 10. Status

Required quantity-contract migration and scoped verification complete. The P3-S31 point admission regression is corrected; no original broad acceptance checkbox closes and the full goal remains active.
