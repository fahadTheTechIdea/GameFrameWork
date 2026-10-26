# P4-G41 — Authored visibility content contracts

## 0. Problem statement

KnowledgeVisibilityModule selects concrete KnowledgeVisibilityPolicyDef; policy rule references
resolve VisibilityTargetDef. Knowledge-backed definition/regional rules resolve KnowledgeFactDef.
Public rule/subject contracts therefore cannot independently author the same engine visibility path.

## 1. Goals

1. Use actual frozen policy/rule/subject contract aliases, with no legacy visibility base dependency.
2. Share strict policy and definition-target JSON readers and exact knowledge matching.
3. Keep the original visibility owner, ledger, command gates, spatial invalidation and game clock.
4. Observe actual policy/rule callbacks; reject recursive/swallowed recursion and scoped mutation.
5. Verify earned knowledge reveals content only to entitled companies through public engine commands.

## 2. Design

KnowledgeVisibility.cs declares IKnowledgeVisibilityDefinition and makes IVisibilityTarget a content
contract. KnowledgeVisibilityDeclaration and KnowledgeVisibilityTargetDeclaration<T> are sole
strict metadata readers/resolvers; native and independent definitions delegate there.
Native definition/regional rules resolve IKnowledgeSubjectDefinition, preserving all/any semantics
for remembered history. Fresh visibility remains the explicit freshness restriction.
KnowledgeVisibility remains the sole runtime owner. Actual immutable aliases validate for all loaded
rules/policies; selected rules alone validate their current-world capabilities/bindings.
Guard policy/Matches/Allows/entity/sample/key callbacks with the existing provider observation and
actual queried entity codec where applicable. A transient evaluation flag prevents recursive entry;
no second visibility authority, cache, knowledge store, clock or rollback mechanism.

## 3. API surface

IVisibilityTarget : IContentDefinition. IKnowledgeVisibilityDefinition exposes explicit own/unlisted
flags and canonical rule list. Public declaration helpers support registered JSON implementations.
IVisibility/ISpatialSampleVisibility, existing command/refusal/view and saved payloads stay unchanged.

## 4. Files

KnowledgeVisibility.cs/module; native fresh/regional/sensor definitions; public StockVisibilityTargetDef
migration; independent proof module/content and one new thin player case. Build identity advances only.

## 5. Error handling

Invalid/ambiguous references, duplicate IDs, bad all/any, empty facts, mutable definition aliases and
missing selected binding refuse before Initialize. Hidden commands use existing UnknownDef/NotFound.
Callback mutation/exception/reentry latches the original fault; no healthy continuation or fake rollback.

## 6. Compatibility

Development-only breaking migration. Delete VisibilityTargetDef, no compatibility reader or wrapper.
Save154/replay1 retained when persistent layouts are unchanged.

## 7. Verification

ONE NEW exact bounded case: Visibility content contracts: earned private discovery and guarded rules.
Native/direct independent definitions/policy, real travel/eight-minute work/material consumption,
private catalogue/snapshot/command parity, sharing/forgetting/relearning, partial saved twins/pause/replay,
strict authoring and actual callbacks. Compile-only historical callers; no family/full/Oil/Tycoon/platform.

## 8. Risks

Scoped observation does not sandbox unrelated private entities/services or arbitrary CLR behavior.
Sensor/regional/fresh-layer migrated runtime matrices, all visibility integration, concurrency/scale/
accounting and full original compound gates remain OPEN.

## 9. Non-goals

Tycoon/G19 authority, Oil, host/demo/platform/Draw, another policy/knowledge/clock/cache/state owner,
full test gate or publication before whole goal and plan genuinely finish.

## 10. Sequence

Read original owner; migrate contracts/shared declarations and rule types; guard actual callbacks;
author independent content/thin case; compile/freeze/run only that case; update accurate evidence
and tracker without closing unrelated compound gates.

## 11. Status

Implemented and scoped-verified on2.379.0/save154/replay1. FIRST/FINAL168/0/0 in793ms.
248 selected inputs frozen2026-10-09 14:59:36 UTC before the sole runtime invocation.
[Evidence](../../docs/migration/P4_VISIBILITY_CONTENT_CONTRACTS_VERIFICATION.md).
No post-pass executable edits. Full P1-P12/goal remain OPEN; original compound gates unchanged.
