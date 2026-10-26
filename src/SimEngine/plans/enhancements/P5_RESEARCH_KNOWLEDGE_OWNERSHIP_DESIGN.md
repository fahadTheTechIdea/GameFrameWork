# P5-A30: Oil research facts use the framework knowledge owner

## 0. Problem statement

Oil's `Research.Seat.Done` duplicated the framework's company-knowledge ledger. Its modifier cache
also relied on that second list. This prevented the generic knowledge view from reporting Oil research.

## 1. Goals

1. One saved, hashed owner for learned facts: existing `ICompanyKnowledge`.
2. Research access, routes and domain multipliers read that owner, never a copied learned list.
3. Preserve authored starting grants, physical earned points, pause/resume and optional Tycoon fees.
4. Verify only affected public-engine behavior; leave the complete activity migration open.

## 2. Design

Existing Oil `Research` implements `IKnowledgeProvider`, with stable ID `oil.research`, observation
schema1 and explicit `FirstLearned` retention. Empty object observations name actual `TechDef` content.
The original ledger handles company privacy, captured payloads, timestamps, uniqueness, save/load and
hashing. `Grant` delegates to `Record`; `Has` delegates to `TryRead`. Domain multipliers derive from
the ledger's stable subject ordering, preserving the former ordinal multiplication order without a cache.

Remove `Seat.Done`, its codec and the modifier cache. Oil physical-study codec changes1→2; there is no
legacy reader or compatibility shim. Core remains2.548.0/save156/replay1; framework owners and their
schemas do not change. Explicit starting grants retain their existing semantics, including an authored
known descendant without prior technologies; dependency checks still govern actual study admission.

## 3. Contract surface

Reuse `IKnowledgeProvider` and `ICompanyKnowledge`. Existing additive provider discovery finds
the already-registered Research instance: no second service, catalog, provider registration or ledger.
`KnowledgeView` now exposes actual Oil learned facts. Existing research command/view contracts stay.

## 4. Files

- Oil `OilCo.Game/Research.cs`: remove duplicate facts/cache; delegate learned ownership.
- Oil `OilCo.Game/OilExecution.cs`: correct ownership documentation.
- Oil `OilCo.PublicProof/ResearchKnowledgeTests.cs` and `Program.cs`: public focused proof.
- Framework master tracker/audit: scoped evidence, original gates unchanged.

## 5. Refusals

Unknown subjects and nonempty/nonobject payloads refuse through the knowledge contract. Foreign facts
remain private. Duplicate grants are idempotent; repeated completed research refuses as already done.
An invalid physical-study seat count refuses rather than indexing beyond the session's seats.

## 6. Compatibility

Development migration only; no old research codec, copied learned list or compatibility path retained.

## 7. Verification

Focused public-proof Release build:0 warnings/errors,1.76s after fixture namespace correction.
New exact research-knowledge case:72 passed/0 failed,13,517ms; affected exact technology-route case:
1,513 passed/0 failed,7,505ms. No full/family run. [Evidence](../../docs/migration/P5_RESEARCH_KNOWLEDGE_OWNERSHIP_VERIFICATION.md).

## 8. Risks

This removes learned-state duplication, not all Oil generic research machinery. Domain multiplier
performance, arbitrary providers/faults and complete physical facility construction are not proven here.

## 9. Non-goals

No financial rules/prices, Tycoon implementation, platform, Draw, adapters or host gameplay changes.
No claim of complete native research execution, industrial acceptance or full regression.

## 10. Stages

- [x] Remove Oil learned-state and cache ownership; use original knowledge contracts.
- [x] Prove starting/earned facts, privacy, paused saves, optional money and affected routes.
- [x] Author Oil research as native technology activity graphs; migrate active/paused progress into
  existing WorkBoard and remove Oil point scheduler/Lab/TechDef duplication.
- [ ] Consolidate domain technology modifiers through contracts and prove physical research parity,
  optional authored fees, rival/private behavior and full selected durability requirements.

Original221 gates retain194 recorded checked/27 open; no original gate closes. Full goal remains active.

P5-A32 replaces the temporary Oil learned provider with native TechnologyService/CompanyKnowledge
and removes the point/lab/paused-progress owners. TechDef now reads domain metadata over the sole
native technology declaration. [Current migration](P5_NATIVE_OIL_RESEARCH_MIGRATION_DESIGN.md).
