# P11-D1 — One company-private knowledge owner

Status: focused D1 slice verified,2026-10-05. Full P11/P1–P12 remains partial.
146 checks/two exact technology suites, clean build; [evidence](../../docs/migration/P11_COMPANY_KNOWLEDGE_VERIFICATION.md).

Historical D1 runtime NOT rerun. Follow-up [D7 observation refresh](P11_OBSERVATION_REFRESH_DESIGN.md)
extends SAME provider/ledger with explicit per-subject first/latest retention and last-observed time.
Technology/fixed facts remain permanent; dynamic provider readings may refresh through real work.
Build2.230.0/save134/knowledge2/view2,84 checks/ONE new bounded case; no second state owner.
Spatial remembered samples/sensors/sharing/expiry and broad gates remain open.

## 0. Problem

TechnologyService alone persists learned research, but there is no reusable knowledge contract for
discovered facts. Adding another learned-fact table would duplicate responsibility. Visibility is a
different concern: IVisibility controls live entities, not what a company has actually learned.

## 1. Goals

1. Move the existing learned table into one optional CompanyKnowledgeService/Sync.
2. TechnologyService keeps research/access rules, delegates facts and validates technology chronology.
3. Open IKnowledgeProvider contracts validate typed immutable observations; aliases discover once via
   the existing ServiceContainer. No second provider registry, implicit knowledge or host grant command.
4. Master-listed facts/effects earn company knowledge through the existing timed machine work DAG.
5. Prove private reads, physical learning/research, cancellation, save/hash and public provider extension.

## 2. Design

KnowledgeObservation contains actual company/provider/subject/schema/learned tick and frozen JSON
payload. Providers validate subject and typed data; one owner checks session identity, unique providers,
company, canonical keys and timestamps. Unknown or foreign private reads disclose no fact or payload.
Known facts are first-learned observations; a changed payload cannot silently overwrite one. Exact
idempotent recording changes nothing. This is a knowledge observation, never a stock or world authority.

Optional sim.knowledge composes that one ledger and immutable KnowledgeView. No work/space/resources
are compulsory. TechnologyModule explicitly requires it; TechnologyService loses its learned table,
IPersistentService and codec, implementing IKnowledgeProvider over the existing technology definitions.
The one generic ledger invokes provider chronology validation on complete record candidates.

KnowledgeFactDef authors explicit value JSON in knowledge_facts. KnowledgeEffectDef resolves one fact
and extends existing PhysicalWorkEffectDef; positive-time machine ancestry, ownership and actual native
completion gates learning. No new scheduler, research progress or discovery job store. New typed
providers implement IKnowledgeProvider on declared existing services and use the same ledger.

## 3. API

IKnowledgeProvider, ICompanyKnowledge, KnowledgeObservation, KnowledgeView; CompanyKnowledgeModule;
knowledge_facts and knowledge_effects. Technology contracts/views remain rules over the same facts.
Build2.92.0/save61/knowledge-state1/view1; technology codec removed, other native codecs unchanged.

## 4. Files

Standard/CompanyKnowledge.cs/module; Views/KnowledgeView.cs; migrate Technology.cs/module;
public proof/JSON and existing TechnologyTests; current build/save assertions compile-only;
stage/master/evidence/parents/README/help/AGENTS.

## 5. Errors

Missing provider/fact, duplicate/foreign providers, invalid data/company/subject/schema and future
timestamps refuse. Checks and provider validation cannot re-enter ledger mutation. Changed known
payload refuses; identical recording is idempotent. Premature/cancelled work never learns.

## 6. Compatibility

Breaking learned-state ownership and explicit module dependency; no old technology codec or shim.

## 7. Verification

Standalone build; exactly the existing two suites extended for the ledger/facts/public provider:

- Technology: authored contracts, access identities and private knowledge
- Technology: physical research, unlocks and saved continuation

Other suites compile only; no third/family/full gate. Bounded physical waits include native diagnostics.

## 8. Risks

Generic payload validation is provider-owned. Broader callback purity/CPU/corruption matrices remain
open. Technology validation must evaluate the entire candidate, not load-order prefixes. Historical
observations are not live resource stock and do not authorize resource mutation.

## 9. Out of scope

Fog/live entity/layer/menu/delta information-flow migration, dynamic survey geometry, observation
refresh/sharing/forgetting, pressure/recovery strategy, Oil/Tycoon/platform/Draw and full acceptance.

## 10. Execution

1. [x] Implement the sole ledger, typed provider validation and private projection.
2. [x] Move actual technology knowledge/codec responsibility into that owner in place.
3. [x] Add authored physical fact effects and public provider proof.
4. [x] Run two exact gates, record evidence and update master/parents/instructions/help.

Final gates:80 contracts/66 physical, zero failures/aborts. The public stock provider records native
remaining stock after real work; authored fact completion and research share the same learned owner.
Failed future/schema/duplicate/provider/key/data/truncated candidate decode preserves prior knowledge.
No third/family/full run. Live fog/metadata, refresh/sharing, pressure/recovery and broader provider,
replay/stress/full roadmap gates remain open.
