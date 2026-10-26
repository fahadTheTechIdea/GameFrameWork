# P5-A11: Single engine knowledge ledger and wide survey geometry

## 0. Problem statement

Geology owned a separate per-company confidence array and imperative private survey layers alongside the framework CompanyKnowledgeService. Estimates read live hidden reservoir remaining quantities. Survey circles used overflow-prone integer products and fixed-point square roots of large squared distances.

## 1. Goals

Use the original ICompanyKnowledge authority for learned field/tile observations; remove old knowledge arrays/layers/export APIs; keep geology truth only; author policy values in existing JSON; use remembered quantities for player estimates; support exact wide distance arithmetic in the original geometry owner. Keep source and focused arithmetic proof separate from unverified Oil gameplay.

## 2. Design

Geology implements IKnowledgeFreshnessProvider and aliases itself through IKnowledgeProvider. It remains the hidden reservoir truth owner and semantic validator, not a learned-data store. Confidence and coverage are read from ICompanyKnowledge using stable canonical field.<id>/tile.<x>.<y> subjects. Learn/Survey record frozen confidence observations through that service. Field remaining quantity is included only at known confidence, and type only at identified confidence; low-confidence raw ledger facts cannot reveal those fields.

Player estimates read the last remembered remaining quantity, not live hidden rival depletion. Own successful extraction and drilling refresh the owner's observation. Confidence never decreases. Observer field queries may explicitly read truth; ordinary players use their own ledger. Survey coverage stays independent per company in the original ledger.

The old _knowledge arrays, their codec payload and private ByteLayer creation are deleted. Geology truth codec becomes2; provider observation schema1. Exploration view schema2 exposes SurveyConfidence(TileCoord), not a legacy layer name. No compatibility payload or fallback is retained.

Existing oil_rules JSON authors provider identity, known/identified/maximum confidence, estimate error percent, latest-observation retention and explicit permanent freshness. Exactly one rules entry is required, not arbitrary first-entry selection. Survey confidence validates against those authored bounds rather than a byte-width cap. Oil directly declares sim.knowledge in code and master manifest.

TileCoord.WithinDistance(TileCoord,long) and DistanceFloor(TileCoord) extend the original geometry class. BigInteger squared arithmetic and bounded integer square-root search avoid floating point/overflow across represented coordinates. Results may exceed Int32; there is no platform-size clamp. Geology uses these helpers and clips survey iteration to actual map bounds.

Core2.537.0/save154/replay1 unchanged format; Oil geology codec2/exploration view2.

## 3. API surface

Engine TileCoord.WithinDistance and DistanceFloor. Domain Geology implements existing IKnowledgeProvider/freshness contracts and uses original ICompanyKnowledge. ExplorationView.SurveyConfidence replaces KnowledgeLayer directly; no host simulation or rendering API.

## 4. Files

Framework Math/Vectors.cs, Kernel/SaveGame.cs. Oil Reservoirs.cs, Settings.cs, Commands.cs, Operations.cs, Views.cs, OilModule.cs, rules.json and manifest.json. New WideTileDistanceTests.cs; Program/protocol/tracker/evidence.

## 5. Errors

Unknown/noncanonical/out-of-bounds subjects and malformed or unauthorized payload shapes refuse. Hidden remaining/type fields are disallowed below thresholds. Nonpositive/out-of-range authored confidence refuses, not normalized. Corrupt reservoir state refuses codec validation. Invalid coordinates and negative radii refuse; missing knowledge provider never fabricates facts.

## 6. Compatibility

Development replacement only. Separate confidence arrays/private layers and old layer-name API removed, without shim or old-save migration.

## 7. Verification

Framework focused compile26.39s clean. ONE NEW exact arithmetic case110/0/0 in19ms/outer244ms. Scoped Oil compiles fail15 errors (3.50s/6.37s/1.89s), reduced from16; changed knowledge/policy/view/operation files have no compiler diagnostics. Structural inspection confirms required sim.knowledge and explicit policy plus no retired paths. This is NOT native Oil content/runtime/privacy/save acceptance.

## 8. Risks

Oil still cannot compile/load; founding8/composition7 errors remain. Provider restore ordering, field/tile privacy, sharing/forgetting/freshness interaction, interrupted survey/extraction records and saved continuation must be proven after integration. Per-tile sequential records can be expensive for large surveys; batch/region performance remains open. Domain terrain generation and resource endpoint/proportional pumping migration remain incomplete.

## 9. Out of scope

Platform/Draw/Unity/Godot/host gameplay, Tycoon business-rule changes, old sibling engine, full regression, final publication or commit/push. Core arithmetic proof is not a broader private-knowledge gate closure.

## 10. Execution outcome

One learned-data authority now selected in source; field estimates use recorded observations and JSON policy. Exact engine distance helpers passed110 focused assertions. Oil errors16 to15; original221 texts/status/line anchors unchanged (193 recorded checked,28 open), no original gate closure.

[Evidence](../../docs/migration/P5_OIL_KNOWLEDGE_LEDGER_VERIFICATION.md).
