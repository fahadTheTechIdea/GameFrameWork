# P10-A7 — authored utility activity requirements verification

2026-10-06. Framework-only focused evidence, not full P10/P1–P12 completion.

## Source and ownership

- Build **beep-simengine-2.143.0**, save **76**. Existing work/utility/network codecs and views
  are unchanged: WorkView39, infrastructure view5, operations view9, LineService4, utility signal1.
- Explicit target_utility_requirements/worker_utility_requirements register normal derived
  WorkRequirementDef providers. One UtilityWorkRequirementDef validates required named consumer
  identity and implements shared readiness; subclasses select only target or candidate worker.
  WorkerUtilityRequirementDef uses existing IWorkerWorkRequirement; no scheduler type case exists.
- UtilityContracts validates the actual live named provider, company, stock owner, native resource
  quantities, promises and pure detached demand. Missing consumer is pending utility.missing-consumer;
  inactive execution, shortage and representation blockers retain actual typed consumer results.
  Foreign ownership blocks. Heterogeneous quantities are never fabricated into one measured unit.
- The existing iterative all/any evaluator, stable occurrence choices, actual candidate preview,
  committed worker selection, node state/progress/blocker, physical travel, cancellation and Sync
  remain authoritative. Guard evaluation creates neither utility demand nor reservation/consumption.
  Native Transformer remains the sole actual running-input consumption owner. Public-only proof
  consumers observe real inventory; their worker/target readiness is not claimed to burn resources.
- Explicit utility-work.json is embedded and master-listed alongside existing actual consumer/network
  definitions. It authors stable requirements/groups/activities and a real mobile worker. Hosts/tests
  do not implement allocation, consumption, requirement selection or a second update/time loop.
- Exactly one GameClock is still constructed by World; StepIndex/Tick and local elapsed work derive
  from that owner. GameClock source SHA256 is unchanged from A6. Paused game time changes no hash.
- Invalid identity/requirement content fails before execution. Forged named demand faults pure views
  and active sim.work execution; faulted games cannot save. Guards are trusted provider validation,
  not unrestricted-world rollback or a mod security sandbox.

## Completed verification

Final completed `dotnet build Beep.SimEngine.slnx --no-restore`: **0 warnings,0 errors**, MSBuild1.84s.
This build completed before both final suites. Failed-build/stale-binary repair attempts are discarded
and provide no verification evidence. Final gates used the current compiled sources below.

| Exact complete suite | Passed | Failed | Reported time |
| --- | ---: | ---: | ---: |
| Requirement expressions: strict named composition, topology and pure typed outcomes | 129 | 0 | 487ms |
| Requirement expressions: physical guarded execution, saved alternatives and cancellation | 162 | 0 | 1287ms |

**291 checks, two exact suites, zero final failures/aborts.** Includes their existing directly nested
ActivityGraph checks and new utility checks, not an additive count. Only these two names were run;
no separately registered third suite, broad family or full regression. A6 network182/A5 running146/
B8 resource447/B7e automation1069 are not rerun; other suites compile only.

Proof covers strict named JSON/native registry alias/scoped worker contract, missing/invalid identities,
missing requirement references, immutable old outcomes, pure/private previews, actual worker offers,
explicit workerless evaluation, canonical any/all outcomes, outbound promises, actual network supply,
typed missing/inactive/shortage readiness, malformed provider facts, timed and physically assigned
machine pause/resume, actual native consumption, saved interleaved disabled/resumed execution,
normal cancellation, machine release, retained work and sole-clock pause. Scenarios use bounded loops
with diagnostic node/state/progress/blocker snapshots on failure; no unbounded acceptance hang.

Directly exercised older tests had stale fixture assumptions after previous engine refactors:
expression paths now include the authored resource/cost wrapper, malformed content insertion must
match actual JSON, contention must check the first actually working branch rather than assume one
whose inputs arrive later, and its actual blocker is worker-busy. Declaration-order proof now reverses
parsed JSON nodes rather than slicing obsolete whitespace. These assertions still prove strict graph
and physical single-machine semantics; no gameplay rule was changed to satisfy historical assumptions.
The new shortage fixture protects real queued pool stock as well as closing its source: a closed
source alone correctly cannot prevent delivery of already queued physical supply.

## SHA256 source fingerprints

Relative paths identify verified source, not a Git commit.

```text
src/Beep.SimEngine/Standard/UtilityWorkRequirements.cs 95AE044DDCD80D05AD265C9D862E1C06F6BF8FBCAD27C95CBCAD0A42A02DD9D5
src/Beep.SimEngine/Standard/WorkModule.cs 4251E398533E50059AC41C22987817A66FE2493E81F6A3B788EDF01EE1FA9012
tests/Beep.SimEngine.Tests/UtilityWorkTests.cs A01A115A3C9FA6FC37CE42681E9595943B6160A5FFD32D4274278366B64AB24D
tests/Beep.SimEngine.Tests/RequirementExpressionTests.cs DD26919F8DA9F5209AFE1AF33A0F3E61CC8828139EC446BBB97EEE450FA539BC
tests/Beep.SimEngine.Tests/ActivityGraphTests.cs FA62AA84A7DAC2D1974976A91752FCE2A1DF7545C00CCFAC7CF3C8C5DCE1DC44
tests/ExtensionProof/content/utility-work.json 66B1A63EE2AF9AF045CB23E9EF7A852FE9A886CE33D065F2571AA3A2B1DFD7CA
src/Beep.SimEngine/Hosting/GameClock.cs 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31
```

## Remaining scope

This is current-readiness admission, not a future supply guarantee or utility reservation/consumption
budget for work. Existing resource requirements own held/consumed work inputs. Electrical/thermal/
pressure physics, aggregate bandwidth, global priorities, graph-independent providers, broader native
utility consumers, rollback/replay/fuzz/soak/allocation budgets and full framework acceptance stay open.
No Oil/Tycoon/host/demo/platform/Draw changes, separate clock, legacy shim or duplicate owner was added.
