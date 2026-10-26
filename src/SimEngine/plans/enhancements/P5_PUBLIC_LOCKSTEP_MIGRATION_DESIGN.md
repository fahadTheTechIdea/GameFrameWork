# P5-A42: Public lockstep test migration

## 0. Problem

The original NetTests referenced removed RunFrame/static-clock APIs, narrowed long tick coordinates to int and corrupted World.Random directly. The external Oil test assembly no longer has friendship.

## 1. Goals

Move original lockstep suites once to the public runner, preserve their two-day/guard intent, bound potentially stalled waits, and exercise transport faults without engine-state writers.

## 2. Design

NetTests moves from OilCo.Tests to OilCo.PublicProof with original case names preserved and one registration. It reuses ClientTests.Factory's canonical authored Frontier, not reduced geometry. Normal Update replaces the old lifecycle call. GameDuration and actual session ExactTicks replace the static time helper; coordinates remain long.

The two-day scenario remains two days. Its progress and synchronization waits now have explicit observer budgets and public diagnostic facts; a missed deadline fails, rather than hanging. It compiles but is not executed in this stage.

Fault injection clones an actual engine-produced current checksum packet and changes its hash byte in transport. Receive owns decoding and divergence detection. The test never changes world random, stock, progress or a provider. This tests peer-checksum disagreement, not arbitrary internal-state corruption.

A separate short case requests ordinary founding on local seats, exchanges real frames, checks synchronization/hash identity and sends a changed checksum. It is not a substitute for the original two-day acceptance.

## 3. API/state

No production APIs, game rules, wire format or persistence changes. Core2.557.0/save156/replay1 unchanged. Current packet-format recognition is test-only fault injection at the byte transport boundary.

## 4. Files

Move existing NetTests; remove old and add public runner registration. Stage/evidence/master/audit. No second transport implementation or gameplay owner.

## 5. Errors

Missing founding sites or engine faults must fail rather than indexing absent suggestions or waiting forever. Bounded child processes provide a wall-clock cutoff independently of frame budgets. Existing engine guards reject duplicate/foreign/local frames and direct clock changes.

## 6. Compatibility

No old RunFrame, static clock, World access or external friendship. One source and registration; no excluded duplicate legacy suite.

## 7. Verification

Targeted public build, compile-only remaining old project diagnostic, exact migrated guards and short public exchange/checksum case. No original two-day, family or full runtime.

## 8. Risks

Canonical peer creation/update is comparatively expensive. Short transport proof is not a two-day gameplay/lockstep/save/replay/soak gate. Other old Oil suites remain non-green.

## 9. Out of scope

Production net/game/financial changes, adapters/platforms/Draw, full tests and Git writes.

## 10. Status

Source migration implemented; focused finals14/0 across guard6/0 and short exchange8/0. The original two-day case remains compile-only. Remaining old project131 unique errors across four files. Original221/194 recorded checked/27open unchanged. [Evidence](../../docs/migration/P5_PUBLIC_LOCKSTEP_MIGRATION_VERIFICATION.md).

