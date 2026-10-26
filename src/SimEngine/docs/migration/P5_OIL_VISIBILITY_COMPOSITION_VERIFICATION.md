# P5-A6 — Native visibility and modular Oil composition evidence

Date: 2026-10-13. Framework2.532.0/save154/replay1.

## Implemented source

Explicit EntityPublicVisibilityDef resolves one actual prototype through existing content and implements IVisibilityTarget. The existing KnowledgeVisibility evaluates all rules; no separate sight, knowledge or entity owner. KnowledgeVisibilityModule registers the new JSON section.

Deleted OilCo.Game/Fog.cs and content/fog/fog.json, both tracked and recoverable from Git. Removed StandardModule/FogModule references. Oil registers thirteen native modules plus Oil and storyteller, and declares seven direct dependencies. Master manifest has fifteen modules and twenty-one existing/new document paths, with no missing path or declared dependency found in source inspection.

Forty-two existing prototypes have explicit presence and sensor metadata; one public-town rule and forty-one sensor rules use the same native owner. Six scenarios explicitly select native visibility. Native rectangular coverage and blocking/movable presence are authored rules, not equivalence to old circular fog. No Tycoon source/business behavior, game clock, renderer/adapter or host source changed.

## Observed framework verification

Release test-project build exit0, zero warnings/errors, 16.71s.

One new bounded exact case:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Public visibility: exact authored prototypes without unrelated disclosure" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 100000
```

50 passed / 0 failed / 0 aborted. Case277ms; bounded outer797ms, PID7192, exit0, no truncated output. Proves exact public prototype scope, unrelated entity hiding, own-entity visibility, scoped snapshots, native save/load/twin continuation and missing prototype refusal. Test setup is authored JSON and public GameFactory, updates, saves and views; no World mutation or test-owned gameplay.

This is not full privacy/provider/fault/physical/industrial acceptance. Historical framework tests were not rerun.

## Observed Oil integration

One scoped OilCo.Game Release --no-restore build exit1, zero warnings, 85 unique reported errors, 6.90s. Removing the obsolete declaration exposed previously masked body/API failures; it is not a successful Oil build.

Compiler groups: CS1061 67; CS0246 7; CS0122 2; CS0103 2; CS1503 1; CS0117 4; CS0104 1; CS1501 1. Failures cover old spatial/entity access, resource APIs, production/research aliases, operation hooks and command/view contracts. Complete output retained untruncated.

Source JSON inspection finds42 prototypes/presences/sensors, one public rule,41 sensor rules,15 modules,21 document paths and no missing declared file/dependency. Engine Oil content loading has NOT run.

## Unresolved geometry and gameplay

Native oil.tiles geometry remains unresolved. Original scenario sizes remain: frontier96x96, adventure128x128, continent224x224, sandbox64x64, showcase112x112, gulf96x96. No single-size fallback or duplicated prototype catalog is introduced. The next spatial slice must resolve explicit per-scenario geometry through the same immutable definition/session composition and prove isolation and durability.

Access/technology, resources, activities, navigation, module settings, game modes, optional single-owner business and remaining body APIs are still incomplete. Full physical drilling/hauling/repair/rival/private/save/replay acceptance remains open.

Changed framework paths diff-check exit0 with two line-ending warnings; changed Oil paths diff-check exit0. No broad test filter, full regression, staging, commit or push. Original221/193recorded checked/28open criteria unchanged; no gate closed.

Metadata validation: 1,984 active source anchors,13 quickstart anchors and1,140 selected inputs match current hashes. The retired Fog source is recorded separately with its prior hash. All221 original criterion text/checkbox/line anchors validate unchanged. Eighteen master rows retain their table shape;8,859 local Markdown links resolve. The current new public-visibility driver has no World mutation/test-owned gameplay boundary matches. All12 frozen Tycoon sources and the game clock are unchanged. Whole framework diff-check exit0 with34 existing line-ending warning lines; staged0. These metadata checks do not extend the scope of the50-check case or verify Oil gameplay.
