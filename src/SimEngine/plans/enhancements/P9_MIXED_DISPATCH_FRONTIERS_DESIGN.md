# P9-B4e — Priority-preserving mixed dispatch frontiers

Follow-on [B4f native prefixes](P9_NATIVE_ADMISSION_PREFIXES_DESIGN.md) replaces all-queue native
feasibility with the longest feasible first native frontier, retaining the provider/company boundaries.
This B4e runtime report remains historical, not rerun on2.299.0.

## 0. Problem

WorkSystem.Assign in Standard/Work.cs batches every feasible same-company native offer after a
selected offer, even across an intervening ready matched external/logistics offer. Native activity
input reservations can therefore overtake a higher-ranked shipment. The existing rank is correct;
the batch execution frontier violates it.

## 1. Goals

1. Never commit a lower-ranked native admission across an earlier runnable provider/company boundary.
2. Preserve original native whole-set acquisition for each uninterrupted ranked native frontier.
3. Keep ordinary blocked/unmatched/exhausted intents non-blocking; no provider gets a hidden priority.
4. Prove staffed and timed low-priority work versus native and public shipments using real eight-unit
   stock, three physical machines, public player commands, saved twins and canonical replay.

## 2. Design

Change only the native member collection loop in WorkSystem.Assign. Skip exhausted, blocked or
unmatched-worker offers; stop at the first runnable offer outside this native company frontier,
or a native worker offer without an activity admission selection. Never collect a later native
prefix across that boundary. Existing changed-success break/rebuild, attempt bounds, matching,
whole-set admission, endpoint/cargo/dock reservations and physical execution remain authoritative.
No new scheduler, acquisition contract, persistent matching state, claim book, timer or compensation.

## 3. API

No runtime API or codec-layout change. Build identity2.298.0, save143/replay1/work-view84 unchanged.
Thin verification fixture accepts an explicit authored provider document; no test owns dispatch rules.

## 4. Files

Standard/Work.cs (original Assign); Kernel/SaveGame.cs build identity; existing DispatchAgingTests
pure factory seam; new master-listed dispatch-frontier.json and embedded-resource declaration;
new MixedDispatchFrontierTests and one registration. Older identity assertions compile-only.

## 5. Errors

Actual ordinary refusal remains pending. Whole native-set refusal cannot fall through to prefix
mutation. Higher runnable external claims invalidate lower candidates through the original refresh.
No retained diagnostic fact authorizes acquisition, no instant refill/recovery or fabricated default.

## 6. Compatibility

Development only. Replace the flawed collection behavior directly; no old dispatcher/shim.

## 7. Verification

Only ONE distinct NEW bounded runtime case:
Mixed dispatch frontiers: priority barriers, physical stock and saved recovery.
Up to1200 updates per physical stage with clock/job diagnostics; explicit replay limits.
Build as needed, repair/rerun only observed failures of this case, record every attempt.
Freeze executable source/proof/tests/JSON immediately after final green; docs/read-only afterward.

## 8. Risks

This repairs cross-frontier priority, not distributed atomic commit across unrelated providers.
Distinct matching/material/dock breadth, hostile providers, sustained fairness and performance
remain open. Strict priorities can deliberately starve lower-priority work.

## 9. Exclusions

No business/Tycoon/Oil/platform/Draw/demo logic, second clock, world-size limits or full test suite.
Full goal/P1-P12 completion and commit/push remain conditional on all required gates.

## 10. Execution

Inspect original owner → replace member selection → authored physical proof fixture/thin driver →
build and ONE new case → freeze → evidence/manifest/master updates.

## 11. Focused result

Implemented in the original Assign loop; no new acquisition authority. Build2.298.0/save143/view84.
ONE clean build5.45s and ONE attempt of the ONE new case194/0/0 1614ms. No repairs/runtime reruns.
Staffed/timed versus public/native shipments and unmatched-provider control use real physical stock,
loading/delivery/replenishment/consumption, private facts, saved twins, isolated sessions and replay.
165 older identity paths compile-only. Frozen2026-10-08 12:40:59 UTC; docs/read-only only afterward.
Broader gates and full plan remain open. [Evidence](../../docs/migration/P9_MIXED_DISPATCH_FRONTIERS_VERIFICATION.md).
