# P7-IC1 — Contract-safe shared inventory admission

Status: VERIFIED-SCOPED; FINAL768/0/0 in3766ms,4 compiles/two same-case attempts retained. Original221/187checked/34open unchanged; additional220. No executable edits after FINAL.

## Problem and goals

Standard/ResourceInventoryState.cs passes its private mutable stock and mutable request/projected vectors into external capacity callbacks and takes Offer.Amount without validating status, bounds, exact quantum or matching Check. Malformed policies can alter stock/intents or mint/overdraw through Add/Remove. Correct the original stock/book owner centrally for storage, cargo, WIP and network endpoints; no second inventory or compatibility path.

## Design

Existing stateless CapacityPolicyContracts owns Check/Quantum/Offer conformance, not a new policy evaluator or stock owner. Check freezes callback vectors and rejects unknown status; Offer validates bounds/native exactness/refusal and the same policy's whole Check. Original inventory checks/receipts/conversion/load, storage filters/metadata/inspection and topology all share these gateways. Original stock/book/graph mutations and production schemas remain unchanged. Three public stateless helpers, no new public gameplay API/clock/state owner/platform change.

## Proof and staged execution

Use actual JSON independent capacity aliases in native storage on public point-space players. Reuse existing public physical haul/batch/tools/cancel/save/pause/replay workflow with explicit factory input, retaining the old registration/assertions/default behavior. New engine-side query instrumentation calls original endpoint contracts inside guarded inspection only; malicious callback probe activates within that query after actual delivery, not driver internal writes. Negative factories are single-session only; healthy shared content remains unbound.

Implement all production/proof first, compile once solution (focused same-case repairs only if necessary), run ONE NEW exact bounded case. Retain failures; final compile before FINAL, no executable edits afterward. No older/family/full runtime. Refresh staged evidence/master/source audit only after proof.

## Boundaries and risks

No arbitrary mod sandbox claim, global side-effect isolation, rollback/refund or allocation-budget proof. Defensive argument copies are transient facts, not another ledger. Preserve valid capacity refusals and measured admission, including zero/closed capacity. Original221/187checked/34open unchanged unless a complete original gate is proved. No Tycoon/Oil/platform/Draw/sole-clock implementation changes; authorized final full regression waits until implementation finishes. No staging/commit/push.

## Source freeze and metadata closure

Freeze2026-10-12 11:23:50 UTC:1,752 source anchors/983 selected executable inputs/13 quickstart anchors match actual files with0 drift. Inventory2,938 files/1,047 Markdown documents/8,712 local links/0 broken links;18 valid master rows;221 original literal gate/line anchors validated with0 errors. Original187 checked/34 open unchanged, additional220. Eleven existing selected executable inputs changed and three new executable inputs; five existing production sources changed, no new production source or state schema. Sole clock and all12 optional Tycoon sources unchanged. git diff --check exit0 with34 pre-existing warning lines;2,926 dirty entries/0 staged. No executable edits after FINAL; no old/family/full runtime or staging/commit/push.
