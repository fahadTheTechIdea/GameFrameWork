# P11-UV1 referenced-unit metadata visibility

## 0. Problem

PlayerView.CanSeeContent checks only the requested definition. CopyResources/CopyCapacityPolicies and resource lookup/name therefore expose nested units even if the selected visibility owner hides those unit definitions. SpatialLayerAccess checks direct unit/resource/vertical links but not resource quantity/capacity units. TransformationContracts.CanDisclose separately implements a resource-unit closure, duplicating a rule missing elsewhere.

## 1. Outcomes

One existing PlayerView visibility path requires actual resource quantity/capacity-unit and capacity-policy limit-unit references to be visible before publishing parent facts. Resource menus/lookups/names, policy menus, layers/samples and recipes agree. Observer remains exactly PlayerId.None; no-fog sessions retain visible content. Invalid referenced content faults explicitly, hidden content refuses without partial output. Execution and queued physical work remain original owners; hosts implement no disclosure rules.

## 2. Implementation

Extend PlayerView.CanSeeContent in place with a transient visited set of actual content instances. Same IVisibility decides each definition, under EntityObservation with actual definition/selected space guards. Traverse only existing IResourceDefinition quantity/capacity-unit links and ICapacityPolicyDefinition limit units; no new permission model, dependency registry, content store or persistent state. Seen actual identities terminate repeated/cyclic typed links; invalid references are not silently replaced.

SpatialLayerAccess routes definition/direct links through this same player view while retaining actual provider/runtime-layer admission. Remove recipe's duplicate resource-unit loop; its recipe-specific output/input/tool/rate closure continues using the shared content gate. Unit-policy defaults, business rules, physical resource amounts and scheduling remain unchanged.

## 3. Contracts/authoring

No new production contract/model/schema; core advances2.496.0, save154/replay1 and existing view versions retained. Existing independent LayerAccessProof JSON gains explicit hidden_unit and hidden_unit_viewer, with actual content resolution. Existing capture-fault metre counter distinguishes direct/vertical metre links from newly traversed resource units. One new public case authors unit hiding and a parent capacity policy into the existing master/scenario flow.

## 4. Files

Hosting/PlayerView.cs shared typed closure; Kernel/SpatialContracts.cs layer routing; Standard/TransformationDisclosure.cs remove duplicate closure; Kernel/SaveGame.cs identity. Existing LayerAccessProof.cs/content JSON extend independent visibility authoring. Current protocol/Program/complete profile/new driver, tracker and evidence update. No Tycoon/Oil/platform/Draw/clock implementation changes.

## 5. Error/compatibility

Hidden referenced definition: parent metadata/lookup/sample unavailable using existing refusal types. Missing/unowned reference in admitted parent: first observed provider fault. No-fog/exact observer keep their original contracts. No compatibility shim, legacy branch, fabricated default unit or alternate metadata permission system.

## 6. Verification

One solution build and one new exact bounded case; necessary same-case repairs only. Physical minute/half saved-twin/replayed inspection flows for quantity/capacity/spatial unit hiding: unit/resource/policy menus, lookup/name, layer ownership/sample mask, authorized company/observer consistency, pure reads and continued state/isolation. Metadata-only complete profile/source/gate closure; no older/family/full suite. Retain every attempt.

## 7. Limits

This is the canonical resource/policy unit-reference closure, not all arbitrary mod dependency graphs or every private view/menu/knowledge/incident/delta. Allocation/global sandbox/full P3.008/P11.033/profile gates remain open. Caller output failures and unrelated provider writes are not rollback guarantees. Final full regression only after implementation finishes; no staging/commit/push.

## Status

VERIFIED-SCOPED: NEW FINAL3018/0/0 in4615ms; six physical unit-mask saved-twin/replay flows and two native/direct timed discovery/manufacturing workflows with resource/recipe metadata parity. FIRST2814/204 retained for unrelated-unit fixture; solution7.60s/focused5.99s/4.73s clean. No executable edits after FINAL. Profile-info695 included/0 excluded/0 game suites, full executor not run. Original221/188checked/33open unchanged; additional227. [Evidence](../../docs/migration/P11_REFERENCED_UNIT_VISIBILITY_VERIFICATION.md).

## Source identity

Freeze2026-10-12 14:23:09 UTC:1,780 source anchors/999 selected executable inputs/13 quickstart anchors match actual dirty source with0 drift. Inventory2,965 files/1,061 Markdown documents/8,747 local links/0 broken links;18 valid master rows;221 original literal gate/line anchors validated with0 errors. Original188 checked/33 open unchanged, additional227. Ten previously selected executable inputs change; one new exact driver and two docs, four existing production sources change/no new production state/classes. All prior view/state schemas unchanged, including native tile7/point layer4. Sole clock/all12 Tycoon/Oil/platform/Draw implementations unchanged. git diff --check exit0 with34 pre-existing warning lines;2,953 dirty entries/0 staged. No executable edits after FINAL, no old/family/full registry run, complete profile executor or staging/commit/push.
