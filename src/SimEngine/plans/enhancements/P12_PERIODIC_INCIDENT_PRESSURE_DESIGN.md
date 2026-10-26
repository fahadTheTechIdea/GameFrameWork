# P12-E7 — Persistent interval-based incident pressure

## 0. Problem

Delayed propagation correctly raises a target once and may deposit a finite pulse. Persistent incidents also need effects while isolation/repair remains incomplete. Reusing propagation's expected-target episode queue as a repeated local effect would mix responsibilities or raise duplicate episodes. Extend the original incident Update pass instead.

## 1. Goals

1. An optional named incident-process capability binds an actual phased incident, positive interval, allowed incomplete phases and an existing exposure-effect contract.
2. Profile/binding metadata is authored through master-listed JSON and public contracts. No concrete incident, spatial provider or material-kind dispatch.
3. One reusable component state owns only bound-episode provenance and local interval progress. The original incident retains the episode; the original DAG retains phase completion; the one clock supplies elapsed updates.
4. The existing incident response system visits processes after response posting and before ordinary work. No second game loop/system scheduler.
5. Existing prepared effect/material transactions own publication. Blocked due work retains one pending pulse, never an unbounded catch-up backlog or fabricated material.
6. Immutable engine diagnostics, save/load/hash, pause, episode changes, phase stop, finite-stock/capacity waits and native/independent storage are focused acceptance gates.

## 2. Design

IncidentProcessModule registers profile and component metadata but no new system. IIncidentProcessProfile declares exact source/workflow, until_phases_complete IDs, interval, ineligible-progress reset/retain policy and IIncidentExposureEffect. A process remains phase-eligible until all selected original phase/node mappings complete; this is not a fabricated linear current-phase state and supports the original DAG. IIncidentProcessDefinition binds profile and named capability identity. Common validation rejects missing aliases, wrong workflow/phase, invalid interval/quantum and unsupported effect capabilities for every loaded prototype.

IncidentProcessState is the sole reusable interval owner, embedded by a component. It retains episode provenance and elapsed game updates, not current game time, stock or incident active state. New episodes reset partial interval progress. Source inactivity resets it; an ineligible active phase follows its explicit reset/retain policy. Each due visit prepares and applies at most one effect through original guarded callbacks. A blocked due pulse remains due and retries normally; missed pulses are not accumulated into a burst.

IncidentResponseSystem keeps its original order390. It first ensures response work, then visits process capabilities in stable entity/component order. It validates restored process state after all entity payloads load. IncidentsView projects current episode, required/elapsed interval, due/phase eligibility and typed blockers without host policy.

Effects reuse IIncidentExposureEffect and its original single-use preparation with a local source/target context. They do not raise an episode. A spatial deposit effect therefore debits the actual local tank and credits its original cell; no second leak/material ledger. Arbitrary repeated effects remain contract-authored. Interval pulses are game-quantized operations, not claimed continuous scientific fluid dynamics.

## 3. API/state

New optional profile/binding/process/state/snapshot contracts and named component codec1. Engine2.599.0/save159/replay1, incident view9. Existing propagation/incident/DAG/resource payloads unchanged. Transient duplicate-visit/reentry guards are not another clock or saved gameplay store.

## 4. Files

New Standard incident-process contracts/profile/state/module; original incident Update/load validator and view; original binding validator helper; authored profile/source setup and focused native/independent proof; protocol/tracker/evidence.

## 5. Errors

Invalid metadata refuses before Initialize. Missing source, wrong episode/workflow, inactive/completed phase, no stock/full cell/unavailable effect remain explicit typed states. Prepared commit disagreement faults the original session, never a synthetic rollback, resource grant or second episode. Corrupt interval/episode state refuses load.

## 6. Compatibility

Optional content addition, no legacy reader, duplicate process scheduler or compatibility facade.

## 7. Verification

Targeted compile and five exact bounded cases only: metadata/full cell, native pressure, independent half-minute pressure, reset/retain phase-stop policies, existing environmental admission. Physical/policy cases use60-second process watchdogs, other cases30 seconds, all5-second termination deadlines. Verify positive interval, partial save/pause, repeated real material pulses, saturation without catch-up, physical phase-stop, episode reset and interleaved live sessions. No full/profile/family tests.

## 8. Risks

Interval pressure is not scientific continuous flow. Fractional native rates, arbitrary custom effects/reentry, weather/chemical transport/multi-cell interactions and broad durability/performance need separate gates.

## 9. Exclusions

Oil/Tycoon/platform/host/Draw code, another clock/incident/material ledger, full regression or Git publication.

## 10. Status

- [x] Optional JSON profile/binding/process contracts and direct native/external component definitions.
- [x] One embedded interval owner, visited only by original incident Update; original episode/DAG/effect/material/clock owners retained.
- [x] Guarded immutable diagnostics and restored validation, new component codec1, epoch reset and single pending pulse semantics.
- [x] Finite material, capacity/stock waits, physical recovery, phase stop with material remaining, new episode, reset/retain policies, saves/pause/replay and interleaved live sessions within scoped cases.
- [x] Five final literal cases1785 successful checks; all attempts in [verification](../../docs/migration/P12_PERIODIC_INCIDENT_PRESSURE_VERIFICATION.md).
- [ ] Fractional scientific rates, weather/chemical/multi-cell pressure, arbitrary source/effect/private/reentry/upgrade/fault matrices and global durability/performance; whole goal remains active.
