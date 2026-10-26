# P4-G37 — Immutable work-effect contracts and scoped refused completion

## 0. Problem statement

WorkBoard.RunEffect currently checks only target Alive/reference/Version on refusal. Private target
codec, clock and identity mutation can escape that check. ValidateEffect inspects only used plan
validators; unused mutable definition-owned effects are not rejected. TechnologyCompletionDef
duplicates the earned-completion delegation expected of independent effects.

## 1. Goals

1. Validate all loaded actual frozen IWorkEffect aliases before Initialize, including unused content.
2. Reject IPersistentService definition-owned state; mutable state remains in existing session owners.
3. Observe metadata/plan validation/preview through the existing provider and optional map scopes.
4. Refused completion must preserve actual target Sync, clock/identity and optional map facts.
5. Native and direct independent technology completion reuse one earned-knowledge delegation.
6. Prove physical research, a utility-controlled pending final effect, saved retry and exactly-once
   learned completion through public player commands/views, plus actual refusal-mutation faults.

## 2. Design

Extend the existing WorkEffectContracts; no new registry, completion queue, callback state or codec.
WorkBoard composition validates all effects. Original plan/preview/run entry points delegate to that
same contract boundary. Refusal observes actual Entity.Sync through original CaptureProvider plus
existing entity scalar scope. Applied commits are allowed to change target/identity; Pending/Failed
must verify the captured scope. Exceptions latch work.effect and withhold successful completion.
This is scoped guarding, not a full-world rollback/sandbox or arbitrary service/cross-entity proof.

TechnologyCompletionContracts owns ValidatePlan, Check and Apply delegation to actual
ITechnologyKnowledge. Native TechnologyCompletionDef directly implements existing effect contracts.
An independent direct Def/ITechnologyCompletionEffect adds explicit nullable named utility gating,
reads actual UtilityContracts, and delegates earned completion. It stores no learned knowledge,
progress, retry countdown or decision loop. Existing WorkBoard retries and CompanyKnowledge own state.

Reuse the authored utility-research fixture via explicit additional module/document and JSON edit
hooks, not duplicated factory/gameplay code. Research has the same staffed thirty-minute/eight-input
graph, while the independent final effect can wait for a processor the player starts normally.

## 3. API surface

WorkEffectContracts.ValidateDefinition/ValidatePlan/Preview/Apply over actual IWorkEffect.
WorkView.PreviewOrderEffect exposes the same guarded private final-effect boundary.
TechnologyCompletionContracts.ValidatePlan/Check/Apply; native completion is direct Def.
No native JSON grammar change, legacy reader, compatibility base or scheduler type branch.

## 4. Files

Standard/WorkEffects.cs, Work.cs, Technology.cs; shared authored fixture hooks; new public-only
completion proof/module/JSON and one new player case; Program/build/protocol; stage/evidence/master/audit.

## 5. Errors

Invalid used/unused aliases or definition-owned persistence fail composition. Missing/premature
research remains the original typed refusal. Pending utility is authorized by an explicitly
selected retry profile. Refusal after private target/clock/identity/map mutation faults; no refund,
rollback fiction or healthy save/hash. Applied effect uses original once-only work/knowledge state.

## 6. Compatibility

Development only; direct native effect contract and original shared owners. Save154/replay1 and
native layouts unchanged; explicit build advances. No second catalog, clock or completion codec.

## 7. Verification

ONE NEW exact bounded case: Work effect contracts: earned research and scoped refused completion.
Compile as needed; freeze selected executable/content inputs before final runtime attempt.
No historical/family/full/Oil/Tycoon/platform runtime. Optional TileMap guard source-only unless
actually exercised. Original acceptance030 and broader provider/global gates remain open.

## 8. Risks

Scoped target codec hashing costs work per actual effect, not a full-world hash per Update.
Arbitrary unrelated service/entity changes and successful-effect clock misuse are not proved here.
Providers still owe their atomicity contract; no universal CLR sandbox is claimed.

## 9. Non-goals

Tycoon/G19 pending authority, Oil/hosts/platforms/Draw, shared definition runtime state, another
completion scheduler/ledger. No staging/commit/push before the full original goal and plan complete.

## 10. Sequence

Inspect actual completion owners; add all-loaded immutable boundary/refusal guard/shared technology
delegation; author independent provider and fixture hooks; compile and run only the new exact case;
update scoped evidence, original unchanged gates, source identity and tracker.

## 11. Status

Implemented and scoped-verified on2.375.0/save154/replay1. FINAL117/0/0 in1385ms;
209 selected executable/content inputs frozen2026-10-09 13:47:06 UTC before final attempt.
[Evidence](../../docs/migration/P4_WORK_EFFECT_CONTRACTS_VERIFICATION.md).
Original221 gate text/checkboxes and full P1-P12 remain OPEN; no publication.
