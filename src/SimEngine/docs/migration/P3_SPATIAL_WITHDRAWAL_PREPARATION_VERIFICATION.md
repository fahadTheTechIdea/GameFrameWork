# P3-S32 — Settled spatial withdrawal verification

Engine beep-simengine-2.583.0/save157/replay1. Existing native, point and independent space payloads unchanged. No Oil/Tycoon/platform/host logic change, full/profile/family run or Git publication.

## Cause and correction

P3-S31's affected admission failed because PointSpace.Withdraw called public WriteLayer while the original endpoint held a transient reserved-debit allowance. Public authorization captured the actual space codec, whose resource book correctly refuses unsettled commits. The old point schema assertion also expected4 although the current provider already declares5.

ISpatialResourceQuantity now requires pure PrepareWithdrawal. The existing endpoint calls it through EntityObservation while books are settled, then immediately commits under its original allowance and consumes the original receipt. SpatialResourceWithdrawalCommit is single-use and unsaved; it grants no receipt authority and holds no material/book. StageSync's unsettled-commit refusal is unchanged.

Native MapLayer normal Set and prepared depletion share CommitSample and the original TileMap.ValueWrite revisions/cursors. Point public batch writes and internal depletion share PrepareWrites/CommitWrites; original visibility deltas are computed before mutation, never through public observation after a debit begins. The independent sparse provider retains its original SetSample owner and validates prepared owner/revision. No duplicate stock, transaction, visibility, work or clock owner was created.

## All attempts

1. Targeted test-project build: terminal exit0, zero warnings/errors, MSBuild9.79s. Initial handle92211 was awaited to completion before testing.
2. Exact preparation protocol:33/0 in544ms; bounded PID30056 terminal exit0,700ms.
3. Exact affected native/point admission:109/0 in789ms; bounded PID31452 terminal exit0,945ms.
4. Exact independent physical extraction/survey (before adding native selection and an explicit visibility assertion):88/0 in2305ms; bounded PID32356 terminal exit0,2461ms.
5. Added a native geometry selection to the same JSON composition/physical driver, with no copied gameplay. Targeted build terminal exit0, zero warnings/errors, MSBuild4.91s (observed5.40s).
6. FINAL new exact native visibility-enabled extraction/survey:92/0 in3772ms; bounded PID39068 terminal exit0,3911ms.
7. FINAL exact independent physical extraction/survey:89/0 in2250ms; bounded PID28692 terminal exit0,2391ms.
8. FINAL exact existing timed work/blockers/saved continuation:86/0 in2880ms; bounded PID22948 terminal exit0,3021ms.

All outputs untruncated. Final distinct focused successes409 checks across five exact cases; preliminary88 is not double-counted. Final test-driver addition does not change the admission/preparation cases or engine behavior. All owned processes reported terminal completion. No executable edits after these final cases.

## What this proves

- Preparation leaves actual stock, receipts, endpoint versions and provider cursors unchanged.
- Preparing a whole deposit does not grant permission to erase reserved material; actual commit refuses without a write prefix.
- Failed/successful preparations cannot be reused, and changed provider revisions invalidate retained preparations.
- Real receipt-bounded transfer retains unspent promises, versions, exact journal provenance and saved twin continuation on native and point providers. Schema expectation now names the actual existing5 rather than inventing a migration.
- Native and independent geometry execute the same authored physical extraction and optional survey. Machines travel, work for exact game minutes, pause and resume, save partial progress, and use native work/resource/knowledge owners. Surveying is not an extraction prerequisite.
- With native visibility active, exact finite material debit succeeds without entering unsettled observation. Owner queries read the actual stored remainder8; the other company receives no private sample prefix or private learned reading. Ordinary command replay reaches the same final hash.
- Existing capacity-blocked completion, retry after real capacity release, competing finite-stock orders and cancellation conserve material and retain saved state.

## Still open

Arbitrary hostile preparations/commits and failure recovery, mixed-lot batches, all provider visibility topologies, lifecycle/replacement/recovery/utility breadth, global lockstep/fuzz/soak/allocation and original compound gates. Native/point protocol probes are engine-internal tests, not normal player-driver writes. Positive physical scenarios remain thin command/update/view/save drivers. Full engine completion and Git publication are not claimed.
