# P11-D15 — Engine-owned authored knowledge forgetting

2026-10-08. Scoped implementation verified:94 checks/ONE exact NEW bounded case0 final failures/aborts,
663ms. FOUR builds/TWO runs of this SAME case, observed repairs documented in
[evidence](../../docs/migration/P11_KNOWLEDGE_FORGETTING_VERIFICATION.md). Full P1–P12 goal remains active.

## 0. Problem statement

The P11 parent keeps forgetting open. CompanyKnowledgeService owns all learned observations, but a
host cannot intentionally discard selected information without fabricating another knowledge store.
Expiration is not forgetting: finite freshness retains historical data. Add explicit optional policy
and ordinary command, not automatic deletion, entity/resource destruction or a second expiry clock.

## 1. Goals

1. Master-listed JSON profiles declare canonical subject/provider keys and explicit allowed companies.
2. Actual providers opt in through IKnowledgeForgettingProvider. Pure whole removed/retained sets let
   extensions enforce dependency rules before commit. No implicit research deletion or unlock reset.
3. Existing ledger owns admission/atomic whole candidate/removal/Sync. Missing observations are harmless
   idempotent no-ops; a provider refusal changes no prefix. Foreign profiles expose no learned facts.
4. Reuse common subject selection parsing/validation and immutable entry facts with sharing; no copied
   parser/store/clock. Existing selected visibility and map cursor revoke access from committed removal.
5. Relearning still requires actual sensor contact or physical survey time/resources. Other companies'
   deliberately shared copies are independent; forgetting is not global revocation of exported data.

## 2. Design

Promote IKnowledgeSubjectPolicy/KnowledgeSubjectPolicyDef as the common contract/content base. Sharing
retains directional permission and merges; forgetting has owned-company permission and removal. One
KnowledgeSubjectKeys reader/validator owns explicit nonempty canonical unique key sets. One immutable
KnowledgeSubjectSnapshot represents policy entry key/observation/blocker; no compatibility alias for
old sharing entry naming. Both native definitions use the existing registry and master documents.

CompanyKnowledgeService aliases IKnowledgeForgetting alongside its original interfaces. Whole preview
captures only caller-company observations, groups removals by actual provider, supplies immutable
removed/retained same-provider/same-company lists and checks pure provider admission. Native authored
provider opts in; TechnologyService does not. No arbitrary payload, company override or full-clear
command. Commit rebuilds preview, constructs one transient original-ledger candidate, validates it
through the existing provider validators and replaces original state once. No deletion tombstone,
forgotten flag, blacklist, learned-history archive or new codec.

Forget absent subjects succeeds idempotently. Positive work already in progress remains with WorkBoard;
completed incident/objective/technology state is not rewritten by a knowledge operation. Native contact
exploration sees the original ledger absence on its next actual Update and must earn its complete
authored dwell again. A currently eligible source can begin relearning in the same ordinary command
Update; disable the actual sensor if the player wants it to remain undiscovered. Physical survey again
needs the original graph's machine/requirements/time; no remembered material refund.

## 3. API surface

IKnowledgeForgetting.PreviewForget(company, policyId, out KnowledgeForgetSnapshot) / Forget(company, policyId).
ForgetKnowledgeCommand carries only policy; original inbox stamps actual company. KnowledgeView binds
its actual viewer. Provider CheckForgettingSubject and CheckForget(company, removed, retained) are pure
and typed. Existing scoped observation guard protects actual provider codec/clock facts, not hostile
world-global mutations. Common subject facts replace the old sharing-specific entry API directly.

## 4. Files

Standard/KnowledgeSharing.cs common contract/parser/facts; Standard/KnowledgeForgetting.cs new optional
contracts/content/handler; existing CompanyKnowledge.cs/module/view/KnowledgeCommands.cs; one new
native JSON and exact bounded test. Existing sharing fixture helper gains optional document assembly,
not gameplay. Strict older identities and directly affected API names migrate compile-only.

## 5. Refusals

Malformed/missing policy, nonseated company, disallowed caller, unknown provider/subject, provider
without opt-in, duplicate keys/companies and unsupported content refuse explicitly. Whole provider
refusal changes nothing. Provider violation faults original session; no rollback/compensation.

## 6. Compatibility

Development-only direct migration; no old entry type/property aliases or alternative codecs.

## 7. Verification

One build and only new exact bounded case `Knowledge forgetting: private whole removal and physical relearning`.
Repair/rerun only observed failures; freeze gameplay/public proof/tests/JSON after final green. Record
all attempts; no older/family/full/platform/stale suites. Prior runtime claims stay historical.

## 8. Risks

Forgetting does not revoke other companies' exported data, erase materials/finished projects, roll back
objectives, automatically delete stale readings or prevent a live eligible source from learning again.
Provider-specific whole-set dependency protection is explicit, not inferred by ledger string keys.

## 9. Out of scope

Automatic aging deletion, diplomatic global revocation, remote/cell-wide exploration, all-provider fault/
dependency proof, Oil/Tycoon source/hosts/demos/Draw/platforms/full P11/P1–P12 acceptance.

## 10. Order

1. Consolidate subject policy parsing/facts; extend original ledger and provider capability.
2. Register normal command/private view and master JSON profile; write new physical acceptance case.
3. Build/exact gate, freeze sources; finalize stage/parent/master/evidence/manifest.

## Current outcome

Completed this native/independent-provider slice on Build2.264.0/save140/ledger semantic schema3/
KnowledgeView5; persistent fields unchanged. One common policy base/parser/snapshot replaces sharing-
specific entry naming directly;128 older strict build identities and6 old sharing-test API lines
compile-only. Existing contact fixture gains explicit document/module assembly only. FOUR builds:
failed4.90s(new proof internal Freeze call), failed3.25s(new fixture nonexistent Items accessor), clean
6.85s, clean7.10s; all0 warnings. SAME case first92 passed2 failed532ms due to restored session not
receiving the same local pause control; explicit twin Pause repairs fixture only. Final94/0/0,663ms.
Source/public proof/tests/JSON frozen after green; no older/family/full/platform/stale gate.
Independent engine-side provider earns dependent facts through original positive work, uses original
ledger and whole removed/retained contract to protect prerequisites, without a new scheduler/store.
Actual native technology provider selected in invalid-profile proof refuses opt-in; actual independent
forgetting provider refuses implicit sharing. Broad adversarial/dependency/cycle/global purity/provider
codec/corruption/replay/lockstep/stress/performance/no-work/non-tile/full P11/P1–P12 remain open.
