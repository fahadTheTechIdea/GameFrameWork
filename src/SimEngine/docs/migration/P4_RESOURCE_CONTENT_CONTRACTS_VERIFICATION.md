# P4-G19c — Resource content contract migration evidence

Status: VERIFIED-SCOPED G19c; FIRST/FINAL146/0/0. Full goal/plan remains open.

[Existing stage](../../plans/enhancements/P4_RESOURCE_CONTENT_CONTRACTS_DESIGN.md).

User authorized mechanical resource/unit type bindings in Suppliers.cs, BusinessValues.cs and BusinessAccountDefinitions.cs only, with business rules unchanged. Source is preserved before migration; mechanical type-token normalization will verify that those are the only changes. One final full regression is authorized only after implementation is finished, not during this stage.

One NEW bounded exact independent-resource catalog/hauling/manufacturing case ran, as recorded below. Existing source assertions/fixtures remain, with only direct mechanical consumer type migration; no historical/family/full runtime. Original221/187checked/34open remain unchanged. No Oil/platform/Draw/clock implementation edits or staging/commit/push.


## G19c scoped delivery —2026-10-12

Core2.488.0/save154/replay1; state/view schemas and compact ResourceId encoding unchanged. ResolvedContent now registers, resolves, validates and returns actual IResourceContentDefinition providers through its existing numeric table and stable-ID map. ResourceDeclaration binds that original numeric identity during its single resolution and validates every loaded resource's catalog aliases, quantity-unit ownership, traits and dimensional capacity before consumers or Compose. Native ResourceDef remains a legitimate built-in authoring helper; no native-only catalog accessor, public ID setter, wrapper, fallback or legacy load path was added.

Original inventory/handling/claims/movement/conversion/flows/recovery/work/upgrades/operation/rate/layer/environment/control/view callers now use resource content contracts. Public extension endpoint/knowledge/visibility/signal consumers migrated directly. Native class-specific fixture assertions are retained; the old readonly collection cast is mechanically updated to the actual returned contract type.

The three approved Tycoon files contain only ResourceDef→IResourceContentDefinition and UnitDef→IUnitContentDefinition token substitutions. Comparison against preserved full source is exact after only those substitutions. Prices, defaults, business modes, valuation/payment/purchase/accounting formulas, resource receipts, content JSON and timing are unchanged. Other Tycoon files and sole clock implementation are unchanged; no Tycoon runtime was run.

### Exact compile/runtime evidence

- Solution command: dotnet build Beep.SimEngine.slnx --no-restore;9.04s,0 warnings/1 new driver error (JsonValue has no RemoveAt). Engine and independent modules compiled. New authoring driver now replaces its actual JSON array through the supported API; no assertion removed.
- Focused repair command: dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore;6.73s, clean0 warnings/0 errors.
- ONE NEW FIRST/FINAL exact case:146 passed/0 failed/0 aborted in1306ms; PID18340 Exited0/process1470ms, no truncation/observer error. No executable edits after FINAL.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Resource content contracts: independent JSON catalog, physical hauling and manufacturing" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
```

Four minute/half-minute public workflows (completion/cancellation) load master-listed resources-contracts.json, with corresponding native supply metadata absent, plus a valid unused independent resource with fractional count-capacity coefficient. Actual aliases and single resolution, compact public IDs, detached readonly metadata, actual eight-unit physical hauling, two-unit WIP, reusable tool, partial/save/pause/private process facts, correct single completion/return, replay and interleaved session isolation pass. Fourteen unused malformed identity/category/unit/phase/trait/measure and duplicate/cross-kind/wrong-kind definitions refuse before Compose. Driver uses public seats/views/commands/Game.Update/saves only. Stage512Update and60000ms process bounds with diagnostics; no host stock/work/time decisions.

No old/family/full case ran; previously existing cases compile-only. This proves the direct catalog/consumer migration and named workflows, not every optional business/resource/utility/network/quality/lot/provider/fault/lockstep/shuffle/stress/allocation path. Original221/187checked/34open unchanged; additional216. Full implementation/goal remains open. The user-authorized one final full regression stays deferred until implementation is finished. No Oil/platform/Draw edits or staging/commit/push.


## Post-FINAL source/metadata closure

Freeze: 2026-10-12 09:51:43 UTC;1736 source anchors/970 selected executable inputs/13 quickstart anchors.27 previously selected inputs changed;6 existing inputs newly admitted plus3 new executable files.33 existing executable files change (22 production/11 proof-test-registration inputs);4 new files total (probe/JSON/driver/evidence). Existing Upgrades.cs also joins source anchors. Existing G19 plan reused. No new production source or state codec/schema, and no executable edits after FINAL.

Read-only closure:0 source/quickstart mismatch/0 executable drift;2922 paths/1039 Markdown/8692 local links/0 broken;18 valid master rows;221 original literal requirement/checkbox/line anchors validated with0 errors. Driver boundary0. Direct current master record identity/solution/focused/exact runtime/timings/limits checks all pass. git diff --check exits0 with34 existing warning output lines; dirty2910/staged0. Original221/187checked/34open unchanged; additional216; goal_complete=false.

Tycoon approval audit: all3 changed files match their preserved full source after ONLY ResourceDef→IResourceContentDefinition and UnitDef→IUnitContentDefinition token substitution;9 other Tycoon source anchors remain exact. Sole-clock implementation hash unchanged. Prices/defaults/formulas/business modes/behavior JSON unchanged; no Tycoon runtime claim. Production resource concrete-consumer scan0 (only legitimate native authoring declarations remain).
