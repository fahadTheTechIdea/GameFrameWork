# P10-A11: file-authored corridor placement policies — Design Document

## 0. Problem Statement

Standard/LineDefinitions.cs hard-codes land/water/any while Networks.cs owns a mutable TileRules callback list. Neither is an actual catalog policy. Preview, routing and earned segment checks need one typed, file-authored rule owner.

## 1. Goals

1. Replace Medium and TileRules, without a legacy alias or implicit default.
2. Require one actual ILinePlacementPolicy per line; admission validates spatial/content aliases before Initialize.
3. Reuse the same policy in actual path search, activation preview and earned physical segment checks.
4. Native/direct policies reuse existing tile footprint conditions; preserve original jobs, material, topology and clock owners.

## 2. Design

LinePlacementPolicies.cs defines pure ILinePlacementPolicy over SpatialPlacement, not a closed terrain enum. TileLinePlacementDeclaration delegates the existing TileFootprintConditionDeclaration. Native registration and independent definitions share it. LineDeclaration links explicit placement_policy; LineService invokes LinePlacementContracts for previews, router cells and segment effects. Original observation scopes preserve first exceptions and actual map facts. Line policy is included in existing disclosure metadata. Existing literal JSON lines migrate to explicit unconditional policies; no silent fallback.

## 3. API surface

ILineDefinition.PlacementPolicy replaces Medium. TileRules removed. JSON tile_line_placement_policies has explicit space/blocker/footprint; line placement_policy required. Serialized state unchanged; Core2.458.0/save154/replay1.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---|---|
| New | Standard/LinePlacementPolicies.cs | 90 | medium |
| Modify | LineDefinitions/Networks/InfrastructureModule | 45 | medium |
| Modify | existing independent line proof and eight active JSON authoring files | 25 | medium |
| New | independent policy proof/JSON and public player driver | 200 | medium |
| Modify | protocol/registrations/tracker | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Missing/unknown/foreign policy or condition | Content refusal before Initialize |
| Valid but unmet layer predicate | Authored blocker; pending intent owns no resources |
| Layer changes before earned stamp | Same current-state predicate refuses segment effect |
| Pure callback changes clock/map or throws | Original terminal fault, first exception retained |

## 6. Backward compatibility

Development-only; no legacy Medium, callback registry, implicit policy or compatibility shim.

## 7. Verification

ONE NEW exact bounded case: native/direct policies × minute/half physical prerequisite underlay then three-segment corridor, material conservation, pending/partial save twins, private detached views, pause/replay/isolation; unused malformed references and actual callback faults. One solution compile, then only necessary focused repairs; retain all attempts. No old/family/full run.

## 8. Risks

| Risk | Mitigation |
|---|---|
| A policy becomes a second construction owner | Pure typed checks only; WorkBoard/LineService retain execution |
| Mutable callbacks or hidden defaults survive | Remove old API/content; require actual declared policy |
| Broad portability overclaimed | SpatialPlacement policy is generic; native corridor path/geometry remains tile-specific |

## 9. Out of scope

Non-tile corridor compiler/topology, arbitrary private policy dependency disclosure, full P10.003/P3.004/P4.014/provider/global/stress acceptance, Oil/platform/Draw/Tycoon/G19, commit/push until whole goal and plan finish.

## 10. Sub-task execution order

Inspect existing owners; add policy contract/shared declaration; replace old authoring/callback paths in place; add one actual public-player proof; compile/exact bounded verification; record attempts and freeze tracker metadata without broad checkbox closure.

Status: scoped verified2.458.0/save154/replay1; [evidence](../../docs/migration/P10_LINE_PLACEMENT_POLICIES_VERIFICATION.md). Lifecycle skill supplies staged ownership/evidence; user no-full-suite rule takes precedence. Whole goal and plan OPEN.

## Verification result

ONE NEW exact FINAL224/0/0 in4117ms; PID18816 Exited0/process4274ms, no output truncation/observer error. Four native/direct policy minute/half physical underlay/corridor flows: blocked intent has no marker/material use, router shares predicate, ordinary underlay work unlocks original pending execution, six physical segments consume twelve of40 opening units (28 retained), tools remain, private detached facts, pending/partial/completed saved twins, pause/cancel/replay/session isolation. Six malformed loaded/unused policy-reference probes refuse before Initialize. Four actual throw/clock/clock-throw/map policy callbacks preserve first failures and reject cached own/foreign/missing routes, Save/Hash/Update.

Clean solution9.59s/necessary targeted5.61s; FIRST164passed/5failed3816ms retained (four intent-preview expectations and later view acquisition after fault); corrected driver follows existing pending-intent semantics and captures both views before fault. No production repair after initial compile; no executable edit after FINAL, old/family/full runtime, Oil/platform/Draw/Tycoon/G19 changes or publication. Original221/184checked/37open unchanged; provider-neutral policy contract is delivered, native physical corridor compiler/topology remains tile-specific, broad P10.003/P3.004/P4.014/full/provider/global/stress OPEN.

## Metadata closure

Current metadata closure:1586 source anchors,13 quickstart anchors,851 selected executable inputs; zero hash mismatches/post-pass executable drift.2778 paths/976 Markdown files/8601 local links, zero broken links;18 valid master rows; all221 original literal gate/checkbox anchors exact and unchanged (184checked/37open), additional184. Thin driver boundary scan empty, executable Medium/TileRules/medium-invalid scan empty. git diff --check exit0 (34 existing line-ending-warning output lines),2766 dirty paths/zero staged. Sole clock/protected G19 hashes unchanged. Seventeen previously frozen executable inputs changed (four existing production); four new executable files including one core declaration and two stage/evidence docs. Six new source files admitted; consuming dependencies already frozen. No executable edit after FINAL. Metadata integrity is not broad gameplay/provider acceptance.
