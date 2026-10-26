# P10-TL1: Saved topology lifecycle and original preservation gate

## 0. Problem statement

Original P10.acceptance.001 remains unchecked: split/merge/removal must preserve or explicitly account for stored resources and reservations. Recent P7 fixes verify commit boundaries, but the original native/independent topology scenarios and repeated saved receipt lifecycle have not been assembled into current evidence for this gate.

## 1. Goals

1. Repeated native and independently supplied tile graph split/merge cycles preserve every original stock vector under both authored time quanta.
2. Real inbound/outbound reservations on either resource/network/changed component prevent destructive split/merge without changing state; saved tokens retain exact authority.
3. Endpoint identities do not recycle across cycles; whole-resource withdrawal uses the existing conversion contract before final empty removal.
4. Combine this protocol proof with current physical and journal-backed topology acceptance; close only original P10.acceptance.001 if that evidence is sufficient.

## 2. Design

Extend NetworkTopologyTests, not a new graph runner. Reuse Factory/Setup/Stamp/stock helpers and existing IResourceTransaction. Explicitly label raw owned-pool initialization as protocol instrumentation, not player acceptance. Add half-quantum authoring to the existing factory. No shadow graph, stock store, scheduler, clock or compatibility reader.

Eight deterministic split/merge cycles per native/independent and minute/half session verify actual complete stock totals, every selected receipt's pure refusal and saved authority, endpoint retirement/nonreuse, sole-clock pause and saved continuation. Final whole-Bill resource consumption uses original owned receipts and CommitConversion, then empty topology may disappear. The finite workload is a test bound, never an engine limit.

Inspect original BuildTopology/Redistribute/ApplyTopology and resource movement/receipt owners. Repair production only for demonstrated defects. Preserve all existing topology corruption/capacity/ownership checks.

## 3. API surface

No planned public/codec/build change: current2.622.0/save159/replay1. Test factory gains explicit half-quantum authoring only.

## 4. Files

NetworkTopologyTests; original P10 gate/checkpoint; master tracker; this stage/evidence. Original frozen completion audit remains historical and is not rewritten as current proof.

## 5. Error handling

Active promises, occupied disappearance, capacity shrink and foreign ownership remain pure typed refusals. Missing/disappearing stock cannot be silently dropped. Commit faults remain terminal; no fabricated rollback. Empty removal succeeds only after original explicit resource disposition and receipt retirement.

## 6. Compatibility

Development only; no parallel/legacy path.

## 7. Verification

Build affected tests. Run the new exact lifecycle literal, four existing native/independent topology literals, the journal-backed durable transfer literal and the physical erase/reconnect literal. These are seven scoped selections, not a profile/family/full suite. Each process has60s/5s watchdog. Record every attempt, source identity and limits; do not close broader P7/P10 provider/stress gates.

## 8. Risks

Protocol instrumentation must not be mislabeled player acceptance. Repeated samples are not arbitrary-provider proof or performance budgets. Receipt preservation means unchanged components retain tokens and changed components refuse until release; it does not mean tokens are rebound to invented endpoints.

## 9. Out of scope

Oil/business, hosts/demo/platforms/Draw, generic utility physics, every graph provider, global stress/performance and full plan completion.

## 10. Execution order

1. Inspect current original gate and preservation algorithms, add repeated saved lifecycle proof.
2. Build/run the seven exact scoped selections; repair only demonstrated production defects.
3. Inspect current checkbox mapping; update only the proven original gate and scoped evidence.
4. Preserve unverified original requirements and do not stage/commit/push.
