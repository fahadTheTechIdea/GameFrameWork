# P5-A4 — Oil physical work-effect migration evidence

Date2026-10-13. Framework remains2.531.0/save154/replay1; no core/clock source change.

## Authoritative source changes

Survey/work/autopilot/research references directly use actual IAuthoredActivityDefinition catalog aliases rather than removed OrderDef. Existing drilling/survey classes are Def plus IPhysicalWorkEffect/IWorkEffectIntentAdmission; Check/Apply delegate the original PhysicalWorkEffectContracts owner. Whole-order validation rejects node use. Post-freeze capability validation only reads service presence, never mutates requirements.

Actual effects JSON declares two pad prototypes, three result prototypes, one stake and three survey bindings. References resolve into immutable actual content dependencies, including survey activity aliases after resolved validation. Duplicate bindings/prototypes and absent native tile/well capabilities refuse. No fabricated empty dependency list, compatibility effect base or new scheduler exists.

Drilling start admission checks actual owned pad identity, not surveyed/logged knowledge. Final admission reads oil geology and delegates dry-hole retirement to CheckDespawn or result admission to Upgrades.Check. Commit uses the sole Upgrades.TryReplace coordinator, preserving original geometry/state/stock/receipts instead of deleting the pad before an attempted SpawnAt. Only successful replacement binds reservoir identity, learns the actual field and publishes oil completion notification. Dry holes use kernel guarded removal; no unconditional clear/refund/instant fallback.

Survey binds only a declared actual activity. Unknown/ambiguous binding refuses; the old all-survey scan and unmatched silent completion are removed. Actual removal admission precedes commit. Commit retires the actual stake through kernel lifecycle and then performs engine-owned discovery/notification. Exceptions still fault the existing session rather than publish healthy partial success. These source paths require runtime proof after remaining integration.

## Compile observations

P5-A3 left12 errors. Three current scoped OilCo.Game Release --no-restore observations:

1. Physical effects and typed activity aliases: exit1, zero warnings,7 errors,1.36s.
2. Resolved result/survey validation: exit1, zero warnings,7 errors,1.32s.
3. FINAL pure post-freeze capability validation: exit1, zero warnings,7 errors,0.98s.

```text
dotnet build OilCo.Game/OilCo.Game.csproj -c Release --no-restore
```

All outputs retained untruncated; final seven unique reported errors concern remaining incident work/response types, queue facts, signal input and content visibility. Later compiler passes can expose body/composition/spatial/storage/caller errors. No successful build or gameplay result is inferred.

Read-only source inspection found every authored pad/result/stake in structures.json and every declared survey in work.json, with no missing reference. This is not engine composition or DAG/runtime validation. Old WorkEffectDef/StepDef are absent from migrated code; old OrderDef/IWorkPlan remain in Wear and are explicitly pending. Founding and survey command creation paths also remain unmigrated and are not claimed guarded by this completion-effect change.

Oil source/effects diff-check exit0 with two line-ending warnings, staged0. No test executor, family/full suite, profile runtime, Oil host/demo/harness/test source, platform/Draw or Tycoon mutation.

## Remaining goal scope

Migrate incident response through the authoritative contracts, signal/visibility/queue/controller APIs, authored work/content/time/game-mode/module composition, physical resource/accounting and optional single-owner business/progression. Then prove unsurveyed drilling, timed work/travel/repair, refusal purity, private knowledge/rival parity, save/load/replay/lockstep and original complete acceptance.

Original221/193checked/28open criterion/status/line anchors unchanged; no gate closed. No staging/commit/push. One final full regression remains deferred until implementation finishes.

Metadata validation: 1,972 source anchors, 13 quickstart anchors and 1,132 selected inputs match the recorded hashes. All 221 original criterion text, checkbox and line anchors validate unchanged. All 18 master rows retain their table shape; 8,862 local Markdown links resolve. Framework diff-check exit 0 with 34 existing line-ending warning lines; staged files 0. The clock and protected Tycoon sources are unchanged. These are metadata checks, not gameplay tests.
