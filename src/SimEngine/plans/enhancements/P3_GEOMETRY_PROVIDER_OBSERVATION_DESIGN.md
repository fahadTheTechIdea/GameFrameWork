# P3-S25: observed construction, recovery and result geometry — Design Document

## 0. Problem Statement

The original construction/recovery admission invokes selected geometry callbacks without observing that spatial owner's existing Sync. EntityRollout uses a separate identity/version-only CheckPure that misses clock and persisted spatial changes. These read-only decisions must not publish success after a provider changes authoritative state.

## 1. Goals

1. Use the existing EntityObservation boundary and actual ISpatialSpace.Sync for selected construction/recovery definition checks before Initialize.
2. Replace rollout's partial CheckPure with the same provider/clock observation and actual producer.Sync.
3. Publish result placement only after the complete read verifies; preserve original mutation-then-throw errors.
4. Prove missing/incompatible geometry refusals and healthy non-tile, resource-only physical construction/manufacturing, saves, pause, replay and session isolation.

## 2. Design

ConstructionService retains one placement/work/recovery owner. Its existing composition checks wrap only the selected provider callback in EntityObservation.ReadProvider. EntityRollout retains sole result-placement boundary; CheckDefinition reads actual selected geometry under that guard, and Find stages refusal/placement locally while observing the actual producer codec. Remove the obsolete CheckPure, never keep a compatibility path. Faulted sessions refuse even missing rollout targets before early returns.

Existing non-friend PointSpace is the real independent provider, not a substitute. Negative-only callbacks on its actual construction/recovery/result/placement methods can alter access/clock or throw. Existing ConstructionContentContractsProofModule arms them; one new test authors JSON through existing master-listed point documents and exercises public commands/views/time/save only. No positive gameplay or geometry implementation enters the driver.

## 3. API surface

No new engine contract, persistence owner or payload schema. Behavioral identity advances2.434.0; save154/replay1 and existing views unchanged. Test instrumentation is not a normal gameplay API.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| Modify | ../../src/Beep.SimEngine/Kernel/SpatialPlacement.cs | 50 | medium |
| Modify | ../../src/Beep.SimEngine/Standard/Construction.cs | 3 | medium |
| Modify | ../../src/Beep.SimEngine/Standard/ConstructionRecovery.cs | 2 | medium |
| Modify | Existing point/construction negative probes, pure factory, protocol and registration | 45 | low |
| New | ../../tests/Beep.SimEngine.Tests/GeometryProviderObservationTests.cs | 170 | medium |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Required selected space absent/site or output incompatible | ContentException before Initialize, no fallback |
| Silent selected provider Sync/clock mutation | Existing observation fault, no geometry publication |
| Provider mutates then throws | Original callback error preserved; no finally verifier masks it |
| Runtime rollout error | No placement prefix or resumable save; prior detached facts retained |
| Explicit null recovery | No inferred container or recovery state |

## 6. Backward compatibility

None. Replace partial purity validation, reuse actual state and existing guard. No shim or parallel geometry model.

## 7. Verification

ONE NEW exact case: Geometry providers: construction recovery and physical rollout observe actual state before publication. FINAL404/0/0 in8181ms, PID18760 Exited0/8325ms observed, untruncated/no observer error. Solution compile clean7.63s; same-case targeted compile clean4.44s. Initial fixture attempt23/25/0 in160ms refused missing explicit recipe ports; corrected only authored fixture. Outer60000ms/termination5000ms/output30000 characters; each physical wait at most600 Updates. [Evidence](../../docs/migration/P3_GEOMETRY_PROVIDER_OBSERVATION_VERIFICATION.md). No historical/family/full/Oil/Tycoon/platform runtime or executable edits after FINAL.

## 8. Risks

Actual populated spatial/producer codec cost is not a broad allocation budget. This is not a hostile-CLR sandbox: providers must include authoritative state in Sync. Other construction command/template/removal callbacks and utility/control spatial consumers remain separately auditable.

## 9. Out of scope

Full P3.004 closure without all higher-level consumer evidence; platforms/Draw; protected clock/Tycoon; Oil consumer; legacy compatibility; full P1-P12/publication before complete goal AND plan.

## 10. Sub-task execution order

1. [x] Inspect actual admission and rollout providers.
2. [x] Replace partial callback observations in existing owners.
3. [x] Author one bounded public physical/negative proof.
4. [x] Observe compile and exact runtime, retaining all attempts.
5. [x] All221 original literal requirements/checkboxes validated unchanged;204 anchors shift seven lines,173checked/48open, additional153.1375 source anchors/13 quickstart headers/682 executable inputs frozen at2026-10-11 11:39:49 UTC match with zero post-FINAL drift. Existing PlanProof.cs newly added to previously incomplete inventories, not a newly created file or historical comparison. Links/master columns/thin driver/protected files verified; P3.004/full publication condition remain unsatisfied.
