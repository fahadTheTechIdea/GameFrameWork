# P2-F3b — Pure provider-codec entity state settlement

## 0. Problem

Entity.Update already owns deterministic source priority and issued once-per-update permission,
but guards GetState with scalar entity/clock facts only. A source can change its own saved payload
without Touch and publish a healthy state. Existing public lifecycle proof counts are incorrectly
serialized even though they are transient callback instrumentation.

## 1. Goals

1. Every existing IEntityStateSource is observed through its actual original component codec.
2. Retain original entity-specific fault phases and first-fault cutoff, context authority,
   deterministic authored priority/tie-breaking and single-clock state timestamps.
3. Remove diagnostic read counters from saved lifecycle proof payloads; explicit new codec versions,
   no legacy readers. Reuse the existing boundary proof component, not a second state provider.
4. One new thin-player case proves real travel/work/repair state transitions, retained snapshots,
   saved twins/pause/replay/isolation, native metadata callback faults and direct source codec faults.

## 2. Design

Extend the original EntityObservation.ProviderScope with an owner-supplied internal fault phase.
Default observations retain observation.provider. Entity.Update captures/verifies the same scope
around each actual state source, preserving entity.state:id:key[slot]. No whole-world hash, new
state resolver, timer, state store, loop, compatibility path or host gameplay rules.

The original boundary probe gains explicit JSON active state and its sole Boolean codec2, plus
transient fault injection. Existing lifecycle A/B probes keep their existing authoritative Booleans;
callback counts become transient with schema2. Native equipment state metadata gains a transient
state-getter probe only. All healthy gameplay remains in existing engine work/resource/policy owners.

## 3. API

Public IEntity/IEntityStateSource/context/game commands remain unchanged. Internal observation
accepts its actual owner fault phase. Existing test-only codec versions change explicitly.

## 4. Files

EntityObservation.cs, Entities.cs; existing EntityBoundaryProof/ConditionBindingProof;
existing lifecycle proof declarations/JSON; reused policy/condition authoring helpers;
new EntityStateObservationProof.cs diagnostics/module and thin EntityStateObservationTests.cs;
Program/current build/protocol; stage/evidence/master/audit.

## 5. Errors

Hidden saved mutations, clock/identity/version changes, exceptions and foreign state references
fault before selected state or time commits. Swallowed recursive update/game faults keep their
original first diagnostic. Invalid external contexts refuse without executing callbacks.
No partial save/hash or healthy retry, no fabricated state or substitute reference.

## 6. Compatibility

Development only; one current proof codec. No migration readers or compatibility instrumentation.

## 7. Verification

Build when needed for compilation. ONE NEW exact bounded case only:
Entity state observation: authored physical transitions and codec-pure update sources.
Only this new case and repairs, no old/full/family/Oil/Tycoon/platform runtime.

## 8. Risks

Actual-source codec observation is not a CLR/whole-world sandbox. Cross-provider hidden mutation,
all-codec/genre/native-state breadth and allocation/performance/global acceptance remain open.

## 9. Out of scope

Excluded Tycoon/catalog authority, business/defaults, platform/rendering/Draw, publication.

## 10. Sequence

Original observation/source boundary; in-place proof codec authoring; thin public proof;
focused gate; actual evidence/tracker/audit. Full P1-P12 and goal remain OPEN.

Status: narrowly verified, core2.363.0/save154/replay1; final NEW exact bounded case235/0/0 in1742ms.
[Actual evidence](../../docs/migration/P2_ENTITY_STATE_OBSERVATION_VERIFICATION.md).
Three executions of only this new case:13/1 bounded abort for wrong authored travel-state predicate;
223/12 for wrong native source order assertions; final235/0/0 after proof fixes. No full/family/old run.
Existing A/B diagnostic-counter and boundary probe schema2 migrations compiled only; old suites
not rerun. Full P1-P12/goal OPEN; pending excluded Tycoon scope and delayed publication unchanged.
