# P11-D1 — Company knowledge evidence

Date:2026-10-05. Current dirty working-source evidence, not committed HEAD or a full regression.
Only standalone Beep.SimEngine; no Oil/Tycoon/host gameplay/platform/Draw changes.

## Identity and ownership

Build2.92.0/save61/knowledge-state1/knowledge-view1. Old technology learned state/codec removed;
technology view and all other native codecs unchanged. No old reader, duplicate table or compatibility
path. sim.knowledge is an optional master-listed module with no compulsory work/space/resource module.
TechnologyModule explicitly requires it. One CompanyKnowledgeService/Sync owns learned observations.
TechnologyService retains existing research admission/access/completion/prerequisite chronology and
implements IKnowledgeProvider; active progress still derives from WorkBoard, never copied knowledge.

KnowledgeObservation captures company/provider/subject/schema/actual learned tick/frozen JSON payload.
Alias-deduplicated declared providers come from the existing ServiceContainer, not another registry.
Generic validation checks canonical keys, company, schema, session, sorted uniqueness, data and time;
providers validate their typed observations and complete-candidate chronology. Exact repeated recording
is idempotent; changed known data refuses. Only explicit PlayerId.None is an observer. Company views
return no foreign/unknown observations and expose recursively immutable data.

knowledge_facts contains explicit value objects; knowledge_effects resolves those existing definitions.
KnowledgeEffectDef extends PhysicalWorkEffectDef, retaining actual positive-time machine ancestry,
target ownership and native completion. ExtensionProof's StockKnowledgeProvider/StockKnowledgeEffectDef
uses public contracts only, captures actual native remaining supply, validates resource identity/unit
quantity and uses the same ledger. It has no store/codec/scheduler. Observations are historical facts,
not a second inventory balance. Discovery, research and construction retain existing execution owners.

## Exact final gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Technology: authored contracts, access identities and private knowledge"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Technology: physical research, unlocks and saved continuation"
```

Focused final result:80 contracts/66 physical =146 checks/exactly two suites, zero failures/aborts.
Final rerun:294ms contracts,304ms physical; clean build1.86s, zero warnings/errors. Compile repairs fixed
test API assumptions and moved corruption instrumentation to the actual sole owner. Other suites'
current build/save assertions migrate mechanically and compile only. No third/family/full gate runs.
Existing bounded900-update waits report tick, technology/native active order/blockers on failure;
this is a test timeout, not an engine session/world size limit.

## Proven

- Existing native research/access identities, prerequisites, physical materials/tools/travel/time,
  cancellation/pause/unlocks and saved twins continue through the new sole learned owner.
- Three distinct declared knowledge providers discover once despite technology service aliases;
  one learned codec, no old technology codec. Master document provenance and unknown initial state.
- Missing provider/subject/company/data, noncanonical key and malformed typed stock payload refuse;
  private reads/readiness are pure. Invalid negative viewer is not an observer.
- Missing authored value/effect reference and timer-only discovery refuse before Initialize.
- Cancelled physical sampling spends eight real materials but learns nothing. Completed sample/final
  effects learn actual12 remaining supply and a structured authored fact privately. Later research
  spends eight more inputs, leaving4; all three facts use the same ledger and save/hash owner.
- Held views remain detached; recursively frozen payloads reject mutation. Exact idempotence and
  changed-payload refusal preserve hashes/first-learned history. Foreign private reads disclose nothing.
- Partial native discovery saves and twin continuations agree; mixed-provider/technology saves agree;
  a second session starts independently unknown.
- Staged future/schema/duplicate/missing-provider/noncanonical-key/invalid-data/truncated decode rejects
  without replacing the prior ledger. Future learned time refuses save/hash at the actual owner.

## Remaining scope

This is not live fog or complete world information-flow privacy. Entity counts, spatial layer metadata,
content menus and snapshot deltas require their own scoped migration. Dynamic survey geometry,
observation refresh/sharing/forgetting, pressure/risk/recovery strategy, broad provider identity/purity/
CPU/reentrancy/corruption matrices, replay/lockstep/fuzz/stress and full P11/P1–P12 acceptance remain open.
Failed knowledge candidate decode does not prove rollback of every arbitrary provider/global callback.
No broad regression, oil extraction migration or platform rendering proof is claimed.
