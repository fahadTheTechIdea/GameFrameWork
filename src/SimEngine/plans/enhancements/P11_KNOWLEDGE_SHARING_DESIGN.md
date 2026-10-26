# P11-D14 — Contract-based authored knowledge sharing

2026-10-08. Scoped implementation verified:104 checks/ONE exact NEW bounded case0 failures/aborts,
622ms,FIRST attempt; ONE clean --no-restore build10.16s,0 warnings/errors. Full P1–P12 goal remains
active. [Evidence](../../docs/migration/P11_KNOWLEDGE_SHARING_VERIFICATION.md).

## 0. Problem statement

CompanyKnowledgeService is the sole learned ledger. Contact exploration and physical survey earn
private observations, but no ordinary engine command can intentionally share them. Hosts must not
copy facts or make policy decisions. Sharing must not turn stale observations into fresh readings.

## 1. Goals

1. Master JSON authors stable sharing profiles with explicit subject/provider keys and directional
   seated-company pairs. No implicit alliance, global sharing, purchaser or economic prerequisite.
2. Optional actual-provider sharing capability validates subjects and outgoing data. Providers without
   it cannot be shared; in particular research is not accidentally granted by copying arbitrary keys.
3. The existing ledger preflights the complete profile and commits a canonical whole candidate once.
   No copied ledger, relationship table, pending transfer service, clock or shared-knowledge cache.
4. Private previews expose only sender observations/blockers, never recipient holdings or acceptance
   details. A missing outgoing subject refuses the whole share without changing a recipient prefix.
5. Preserve source observation time/data/schema. New recipient acquisition time is the actual current
   clock; latest retention only replaces older readings, first retention never overwrites knowledge.
6. One new bounded acceptance case proves actual travel/material work, sharing, expiry, private reads,
   pending saves/twins, conservation, omission and invalid JSON. Older cases compile only.

## 2. Design

Extend CompanyKnowledgeModule in place. IKnowledgeSharing aliases its same CompanyKnowledgeService.
KnowledgeSharingPolicyDef implements IKnowledgeSharingPolicy; native AuthoredKnowledgeSharingDef owns
immutable canonical subject keys and explicit directional pairs. Actual IKnowledgeSharingProvider
provides pure subject validation and share admission; native AuthoredKnowledgeProvider opts in for its
validated authored subjects. Technology and other providers do not implicitly opt in.

Policy permissions and sender-visible content are checked before reading any facts. Read all outgoing
subjects from the actual sender's original ledger. Detached source-only snapshots are public immutable
values. Preview and command use the same admission; commit rebuilds current admission. No retained
preview is authority. All recipients in this slice are existing seated companies, not diplomacy groups.

Sharing is information transmission through a normal command, not a physical cargo activity. It never
observes the live world, invokes a survey effect, refreshes a source, manufactures resources or unlocks
technology outside opted-in providers. Recipient first-learned time is current receipt time; original
ObservedTick can precede LearnedTick. Revise the existing ledger chronology invariant accordingly:
both times must independently be nonnegative and no later than the sole clock. Existing capture rules
are unchanged; native first retention keeps its original reading. Existing payload fields suffice.

Latest recipient observations win against older or equal incoming timestamps. For newer incoming
readings, preserve the recipient's original LearnedTick and replace observation data/time/schema.
First recipient observations are never overwritten. Repeat commands are accepted idempotently; result
and preview do not disclose whether recipient holdings changed. Existing visibility/freshness derives
from the same committed ledger and clock. No automatic refresh, sharing subscription or secret sight.

## 3. API surface

IKnowledgeSharing.PreviewShare(sender, recipient, policyId, out KnowledgeShareSnapshot) and
Share(sender, recipient, policyId); ordinary ShareKnowledgeCommand carries recipient/profile only.
KnowledgeView.PreviewShare binds sender to its actual viewer. No tooling impersonation or arbitrary
payload command. IKnowledgeSharingProvider.CheckSharingSubject / CheckShare opt actual providers in.
IKnowledgeSharingPolicy.Subjects / Allows / ValidateBinding support extension-defined policy content
without ledger branches. All provider checks use the existing scoped observation guard; not a hostile
mod sandbox or world-global purity guarantee.

## 4. Files to change

- Standard/KnowledgeSharing.cs: contracts, immutable facts, authored policy and ordinary handler.
- Standard/CompanyKnowledge.cs: same ledger admission/merge and native provider capability.
- Standard/CompanyKnowledgeModule.cs; Views/KnowledgeView.cs; Commands/KnowledgeCommands.cs.
- Kernel/SaveGame.cs: simulation identity bump; ledger semantic schema3, view4, wire layout unchanged.
- One new test and master-listed native JSON; strict older build identities migrate compile-only.

## 5. Error handling

Invalid/missing profile, malformed keys, missing/unsupported provider, duplicate directions/subjects,
unknown subjects and non-seated policy pairs fail composition or return typed admission refusal.
Wrong direction/self-share refuses before source lookup. Missing outgoing facts refuse without mutation.
Provider violations/exceptions are deterministic faults through the original fault boundary; never
compensation or fabricated successful sharing. Recipient holdings never appear in refusal details.

## 6. Compatibility

Development only; no legacy reader, compatibility shim or alternative timestamp interpretation.

## 7. Verification

One build, then only `Knowledge sharing: authored private exchange preserves physical observation age`.
Repair/rerun only observed failures; freeze gameplay/public proof/tests/JSON after final green.
Record every attempt and historical compile-only migrations. Full suite remains unrun/unproven.

## 8. Risks

Provider permission is explicit; research and provider-specific dependencies cannot be bypassed by
universal sharing. First/observed chronology now represents acquired old information accurately.
Directional JSON authorization is not negotiated diplomacy. Immediate information sharing is not
physical communication-delay simulation. Equal timestamp conflicts preserve existing recipient data;
normal deterministic command ordering remains authoritative. Scoped guards are not mod isolation.

## 9. Out of scope

Forgetting, remote exploration, alliances, automatic subscriptions, communication networks, monetary
trading, Oil/Tycoon source, hosts/demos/Draw/platforms, broad replay/lockstep/stress/fault acceptance.

## 10. Execution order

1. Define policy/provider capabilities and original-ledger whole admission/merge.
2. Connect normal command/private view; author native profiles through the master JSON.
3. Implement one bounded player-driven physical survey/share scenario and run its exact gate.
4. Freeze sources; finalize stage/parent/master/AGENTS/README and evidence/source inventory.

## Current outcome

Steps1–4 completed within this native sharing slice. Build2.263.0/save140/knowledge codec3/view4;
payload field layout unchanged.127 older strict identity files/lines and three ledger schema assertions
migrated compile-only. Shared existing native factory extends document assembly only; no gameplay.
No older/family/full/platform/stale suite. Source/public proof/tests/JSON frozen after first green.
Actual native discovery/survey/material use, partial physical and pending-command saved twins, private
pure frozen facts, no partial transfer, first/latest merge, paused clock/expiry/idempotence/actual
positive refresh/stock conservation/isolation/omitted profiles and13 invalid JSON cases proved.
The research opt-out assertion checks actual provider type capability, NOT runtime research composition
or a full mod-provider proof. External policy/provider/fault/corruption/global purity/large allocations/
CPU/replay/lockstep/stress/full P11/P1–P12 remain open. No publication before full verified completion.
