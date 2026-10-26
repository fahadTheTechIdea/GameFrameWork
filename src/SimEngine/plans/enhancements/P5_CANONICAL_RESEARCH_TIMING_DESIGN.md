# P5-RT1 — Canonical physical research timing

Current M150/native2.772 revalidates the SAME actual full study:1418/0, two276/1
placement-fixture failures retained. Radius8 engine suggestions replace the
over-restricted radius4 request; original work/rates/material/power rules and
stage bounds stay unchanged. [Current evidence](../../docs/migration/P5_PHYSICAL_NATIVE_RESEARCH_VERIFICATION.md#current-m150-physical-canonical-research-continuation--2026-10-23).
No original gate closure or replay/full/profile/family validation. The checkpoint
below retains its actual historical2.639 identity.

Completed focused slice2026-10-16, unchanged2.639.0/save160/replay1: existing exact
physical research proof now retains canonical120-day nominal work/ten productive game
days. Final1412/0; two earlier new-assertion failures preserved in the
[verification report](../../docs/migration/P5_CANONICAL_RESEARCH_TIMING_VERIFICATION.md).
Original P5.005 remains open;221/210 recorded checked/11 open, full objective active.

## 0. Problem and specification

Original P5.acceptance.005 includes preserved timed research. The existing actual-consumer
PhysicalResearchTests proves supplied canonical lab/grid construction and saved shutdown,
but its Files.ReadText replaces seismic_3d's 120-day nominal work with one day. That
cannot prove the original ten productive game days at the authored lab capacity of 12.
Remove the shortcut in that existing proof, not another research owner or test scenario.

## 1. Goals

1. Preserve actual tech.json bytes and original 172800-minute study goal.
2. Keep original 36h lab/20h grid construction, actual hauled materials and absent/unpowered/
   disabled facility gates, with money still optional.
3. Earn research through normal engine updates at unchanged rate 12; verify 14399 productive
   minutes remain incomplete and minute 14400 earns knowledge exactly once.
4. Preserve saved partial/shutdown/last-minute/completed identity and immutable retained facts.
5. Run only this exact existing case. Retain the wider P5 timed-scenario gate as open unless
   its complete scope is independently supported.

## 2. Design

Edit only actual-consumer OilCo.PublicProof/PhysicalResearchTests.cs. Delete the tech.json
duration projection. Keep its existing finite starting fleet/stock and 24x24 reservoir-free
resource-only fixture explicit; this is not an unmodified canonical map/profile proof.

Reuse the original factory, founding, construction, stock queries, Study, Submit and Update.
Add a read-only StudyNode helper over WorkView.TryCopyNodes for actual productive-time facts.
Extend Update with optional exact native RunTicks steps for boundary assertions; both sessions
still execute the same Game.Update pipeline and clock. No direct progress/clock/state writes.

After ordinary restart, drive native batches up to 60 minutes, check actual rate/progress,
no premature knowledge and complete twin hashes per batch. Stop one productive minute before
the declared goal; save/load there, advance one native minute, inspect original completion
history/knowledge and completed durability. Terminal executions are pruned by WorkBoard;
do not retain them artificially to satisfy an obsolete active-node lookup. Batch arithmetic
chooses test observation cadence only, never performs work or publishes knowledge. Preserve the existing 1000-update/420s construction
wait limits. The newly full-duration study has an explicit 14400-minute/180s stage bound and
the single child has a 240s external deadline; none is extended after a failed run.

## 3. API surface

No production API, content, codec, clock, engine version or financial rule changes planned.

## 4. Files

| Action | File | Risk |
|---|---|---|
| Modify | Actual Oil consumer OilCo.PublicProof/PhysicalResearchTests.cs | Test-only; retain all original assertions |
| Add | This design and docs/migration/P5_CANONICAL_RESEARCH_TIMING_VERIFICATION.md | Scoped evidence |
| Modify | MASTER_TRACKER.md, P5_OILFIELD_EXTENSION_DESIGN.md, AGENTS.md, README.md | Record limits accurately |

## 5. Error handling

Throw with native work/entity diagnostics on stalled physical work, fault or incomplete batch.
Do not replace a power/capacity failure with invented uptime or a forced completion. A test
failure must be diagnosed in its original owner; retain every attempt and bound.

## 6. Compatibility

Development architecture only. No old short-duration companion, fallback, duplicate test
driver, compatibility path or second state owner.

## 7. Verification

One targeted Release build of OilCo.PublicProof, then existing literal case:
`Oil physical native research: supplied canonical lab construction power waits and saved facility shutdown`.
Use --bounded-exact, timeout-ms 240000, termination-timeout-ms 5000, output-chars 16000.
No full/profile/family filter. Check core/content identity and diff/link hygiene separately.

## 8. Risks

Longer real work can expose actual safety/capacity defects or local performance limits. Neither
justifies shortened durations, disabled guards or longer watchdogs after failure. A reduced map
and authored supplies still do not prove all profiles/scenarios or broader lockstep/replay.

## 9. Out of scope

Financial amounts/rules, new workflows, platform/Draw/adapters, client gameplay, broad regression
or staging/commit/push before the full goal and plan finish.

## 10. Execution order

1. [x] Remove the duration override and extend existing assertions/observations in place.
2. [x] Compile the target as needed; run only the strengthened exact case and observe its live handle.
3. [x] Diagnose demonstrated failures through original owners, preserve every result.
   Two test assumptions corrected: completed-order pruning and interval timestamps.
   No engine/gameplay repair or changed authoring required.
4. [x] Record actual evidence and remaining original gates; keep the full objective active.
