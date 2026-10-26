# P4-G40 — Authored knowledge content contracts

## 0. Problem statement

CompanyKnowledgeService previews and composition still select native policy base classes.
AuthoredKnowledgeProvider, KnowledgeEffectDef, contact exploration and fresh-layer visibility
select KnowledgeSubjectDef despite public subject contracts. Independent definitions cannot use
the existing knowledge owner without inheriting native bases.

## 1. Goals

1. Select actual frozen subject/sharing/forgetting contract aliases, including unused definitions.
2. Delete obsolete subject/policy dependency bases without a compatibility path.
3. Share strict sharing/forgetting JSON declarations with direct independent definitions.
4. Preserve the sole knowledge ledger, freshness clock, work graph and command/view execution.
5. Verify physical learning, private sharing, aging, forgetting/relearning and saved continuation.

## 2. Design

CompanyKnowledge.cs uses IKnowledgeSubjectDefinition and IKnowledgeSubjectPolicy indexes.
Native fact/reading definitions implement the contract without KnowledgeSubjectDef.
KnowledgeSharing.cs and KnowledgeForgetting.cs expose sole immutable declaration readers and
binding/admission rules; native and independent Def implementations delegate to those declarations.
Knowledge policy declarations contain intent only, not observations or a second codec.
All loaded definitions validate actual ownership/frozen aliases, retention/freshness and policy
provider references before Initialize. Exploration and fresh visibility resolve the same contracts.

## 3. API surface

Existing IKnowledgeSubjectDefinition, IKnowledgeSubjectPolicy, IKnowledgeSharingPolicy,
IKnowledgeForgettingPolicy remain canonical. Public declaration readers support registered JSON
extensions. KnowledgeView, normal sharing/forgetting commands and persisted payloads remain unchanged.

## 4. Files to change

CompanyKnowledge.cs, KnowledgeSharing.cs, KnowledgeForgetting.cs, SpatialReadings.cs,
Exploration.cs, LayerFreshKnowledgeVisibility.cs; public proof and one new player driver/content.
One old compile-only test lookup changes to its contract; no runtime rerun of historical cases.

## 5. Error handling

Missing/ambiguous references, mutable aliases, invalid retention/freshness, unknown policy capability,
invalid seat/direction/subject refuse before Initialize. Normal private commands retain typed blockers.
No invented subject, fallback observation or hidden research prerequisites.

## 6. Compatibility

Development-only breaking migration. Delete dependency bases; no alias/shim or old reader.
Save154/replay1 unchanged when persistent layouts stay unchanged.

## 7. Verification

ONE NEW exact bounded case: Knowledge content contracts: physical private learning and policies.
Direct/native definitions, earned positive-time learning, partial saves, sole-clock pause, sharing,
freshness expiry, forgetting and relearning, replay and independent sessions; strict malformed content.
No historical/family/full/Oil/Tycoon/platform runtime. Freeze selected input identities before execution.

## 8. Risks

Focused proof does not establish arbitrary provider mutation safety, spatial survey breadth,
all technology sharing/forgetting dependencies, concurrency/scale or full original acceptance.

## 9. Non-goals

Tycoon/G19 authority, Oil, host/demo/platform/Draw, another knowledge/graph/clock/store,
compatibility, full runtime gate or publication before entire goal and plan complete.

## 10. Sequence

Read actual owners; migrate contract selectors and delete bases; share strict declaration readers;
author independent definitions and thin normal-player case; compile/freeze/run that exact case;
update evidence and tracker without closing broader compound gates.

## 11. Status

Implemented and scoped-verified on2.378.0/save154/replay1. FIRST/FINAL156/0/0 in645ms.
240 selected inputs frozen2026-10-09 14:42:22 UTC; no post-pass executable edits.
[Evidence](../../docs/migration/P4_KNOWLEDGE_CONTENT_CONTRACTS_VERIFICATION.md).
Full P1-P12 and goal remain OPEN; original compound gates unchanged.
