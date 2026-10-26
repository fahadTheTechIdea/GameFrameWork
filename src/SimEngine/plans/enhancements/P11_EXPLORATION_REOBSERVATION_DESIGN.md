# P11-RE2 — Timed reobservation of expired exploration knowledge

Status: implemented, focused verification complete. Full original P11/goal open.
[Every build/run, identities, coverage and limits](../../docs/migration/P11_EXPLORATION_REOBSERVATION_VERIFICATION.md).

## 0. Problem Statement

The original ledger already owns first/latest retention and authored freshness.
AuthoredExploration still rejects every latest subject and stops permanently when
any history exists. A changing field can therefore become stale but cannot be
reobserved through its existing exploration source.

## 1. Goals

1. Admit first-learned subjects and explicitly finite-freshness latest subjects.
2. Use original IKnowledgeFreshness facts to decide whether observation is needed;
   never add an expiry table, deadline, second clock or copied knowledge record.
3. Preserve historical Known independently from NeedsObservation. Expose the original
   immutable freshness reading in the same exploration snapshot.
4. Require the entire original dwell/coverage/target checks again after expiry;
   shutdown/interruption/save/load never grants an early timestamp or replacement.
5. Prove actual changed layer readings, reset/retain, privacy, sharing, forgetting,
   saved twins, replay and first-fault behavior using existing engine contracts.

## 2. Design

Original AuthoredExplorationContracts owns the additional automatic-subject policy:
FirstLearned is permanent; LatestObservation must have a positive authored finite
FreshFor (already validated by KnowledgeContentContracts). Latest with null lifetime
remains valid for manual/activity observation but not an automatic exploration
profile: there is no invented polling interval.

Original Current reads IKnowledgeFreshness from the sole knowledge owner. Unknown
or stale entries evaluate their actual selected requirement and subject target;
fresh entries wait. ExplorationEntrySnapshot exposes KnowledgeFreshnessSnapshot,
Known (history exists) and NeedsObservation (history absent or stale), derived from
that one reading. AdvanceExploration/GetState use NeedsObservation instead of Known.
Completion observes and records through the same ledger, preserving LearnedTick and
updating ObservedTick only after the full new dwell completes.

No new saved fields. Existing ordered entry/progress codec remains authoritative.
Build2.633→2.634, save159/replay1; ExplorationView feature schema1→2 for richer facts.
Existing first-learned content/semantics stay unchanged without fallback or old API.

## 3. API surface

ExplorationEntrySnapshot constructor accepts the actual immutable freshness result
instead of an independent bool known. Known and NeedsObservation are derived getters.
Latest subjects author `retention:"latest"` and positive `fresh_for` using existing
knowledge subject contracts. No new profile scheduling settings or host decisions.

Example on an existing point-reading subject in a master-listed document:

```json
{
  "point_readings": [{
    "id": "field.observation", "layer": "field.terrain", "position": 10,
    "retention": "latest",
    "fresh_for": { "numerator": 10, "denominator": 1, "unit": "minute" }
  }]
}
```

The existing exploration entry references that subject and keeps its own authored
requirement, dwell duration and reset/retain interruption. Field/sensor/subject
contracts still decide observation eligibility; freshness does not bypass them.

## 4. Files to change

- Standard/Exploration.cs: original validation, facts and progress/state decisions.
- Standard/ExplorationModule.cs: require original freshness capability, view identity.
- Kernel/SaveGame.cs and independently authored protocol expectation: build identity.
- Existing PointReadingProof: transient negative Observe hook only, no positive state.
- New master-listed latest-reading document/exact public-player proof/registration.
- Stage evidence, master/P11 tracker, README and AGENTS handoff.

## 5. Error handling matrix

| Condition | Behavior |
|---|---|
| Unknown reading | Earn initial full dwell |
| Fresh existing reading | No work or timestamp refresh |
| Stale reading | Keep history, earn full replacement dwell |
| Interrupted replacement | Authored reset/retain; no ledger mutation |
| First-learned fact | Existing once-only permanent semantics |
| Latest without finite lifetime | Content refusal for automatic exploration |
| Failed subject observation | Original first-fault boundary, old reading retained |

## 6. Backward compatibility

Development-only API migration, no old constructor overload or alternate runtime.
Current content and saved entry/progress layout do not need a compatibility reader.

## 7. Verification

Targeted build, exact new reobservation case and selected existing permanent/contact
and freshness cases after source inspection. No full/family/profile run. Keep all
attempts and label the scope accurately; full original P11.026/.029/.035 remain open.

## 8. Risks

Known must mean historical membership, not freshness; stale data must not disappear.
Changing the world or reading a view must not refresh history. Shared facts retain
original observation time, rather than becoming fresh on receipt. Same-store ledger
identity must survive save/load and partial replacement failure. Negative hooks in
shared frozen proof metadata must target only their designated world and be cleared.

## 9. Out of scope

New resource/cash ledgers, manual survey rules, free geological knowledge, mandatory
seismic/logging, Oil/business/platform/Draw, broad provider/stress/full gates or Git
publication. This extends the existing framework; no game logic enters the harness.

## 10. Sub-task execution order

1. Extend the original automatic policy and derived snapshot/progress decisions.
2. Author latest point readings in an explicitly master-listed proof document.
3. Verify changed actual readings and physical dwell, saved interruption and faults.
4. Record focused evidence/limits; retain all original broad gate statuses.

Completed: original automatic policy, one derived history/current snapshot and
progress/state path; master-listed latest readings; four physical metadata/quantum
variants and four original callback failures. One clean targeted build; four exact
cases874/0, no repairs/reruns. Original221/208 recorded checked/13 open unchanged.
