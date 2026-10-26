# P12-D6 explicit machine bindings in ordinary plans — focused verification

Date: 2026-10-06. Runtime: Beep.SimEngine only.
Build **beep-simengine-2.155.0**, save **82**, WorkBoard **26**, WorkSystem **17**, WorkView **46**.
Existing condition/provider, resource, dock, recurring and clock payloads are unchanged.
[Stage](../../plans/enhancements/P12_SELECTED_SERVICE_WORKERS_DESIGN.md).

## Sole responsibility and current public contract

ActivityWorkerSelection implements IActivityWorkerSelection, exposing detached read-only
ActivityWorkerBinding(node,worker) values. It owns strict argument parsing, ordinal canonical selection,
graph validation, actual machine admission, identity and one embedded Sync. Order retains that immutable
selection. No new component/module/service/system, selection registry, worker reservation, receipt history
or fleet scheduler exists. PlanEntry.Arguments retains frozen player intent as before; the existing board
checks native execution identity against it. Binding array order does not change selection identity or
its codec. Original argument arrays remain authored input and may yield different whole-session hashes.

Ordinary activity arguments can specify a chosen facility and machine for one or more positive-time nodes:

```json
{"target":1,"workers":[{"node":"service","worker":4}]}
```

Omitted bindings deliberately select any compatible worker for unbound nodes. Explicit null/empty arrays,
unknown fields/nodes, duplicates, nonpositive identities, timers and zero-time selections reject; no
compatibility translator or absent-worker substitution exists. Submission requires actual live owned
compatible workers. Busy, held or policy-disabled workers are intent, not acquisition; ordinary gates
still determine readiness. No selection consumes resources, teleports a worker or grants repair.

WorkSystem.CopyCandidates restricts actual worker identity before existing role/policy/route/dock/resource
evaluation. Missing/foreign subjects produce typed facts without route/role details or alternative offers.
WorkBoard.Assign rechecks the binding before native resource/worker acquisition. CheckJob and physical
completion enforce the same constraint. Job.Worker/IWorker.Job remain actual claims; D5 CompletedBy stays
the earned final executor after positive work and physical departure. Checkpoint validation rejects
unallocated selection IDs and assigned/completing actors that differ from requested intent. Removed or
ownership-changed unassigned selections remain honest persistent waits, not fabricated replacements.

ActivityNodeView.RequestedWorker is private/immutable and distinct from current Worker and CompletedBy.
SubmitPlanCommand, private PreviewPlan, ordinary plan policy/cancellation and candidate/node views are
unchanged player entry points. Direct engine-side Post can explicitly supply the same selection object;
unbound generated/system orders still use the deliberate automatic policy. New master-listed
selected-workers.json authors service, parallel tool-use and zero-time refusal examples. Public-only
ContractWorker and ProofConditionDef are reused, not duplicated or inspected by concrete scheduler cases.

Exactly one GameClock is unchanged. Selection has no current time, deadline, update loop or timer.
World.Tick remains Clock.StepIndex, a read-only update index. Local physical work derives only from
that clock. No platform/game-size limit, rendering, OilFieldDays, Tycoon, host or demo logic was added.

## Final commands and results

```
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Selected workers: intent contracts, admission and checkpoint ownership"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Selected workers: physical service, parallel nodes and saved continuation"
```

- Final clean build: **0 warnings, 0 errors**, MSBuild **2.51 s**.
- Intent contracts/admission/checkpoint ownership: **65 passed, 0 failed**, **342 ms**.
- Physical service/parallel nodes/saved continuation: **98 passed, 0 failed**, **464 ms**.
- **163 passed, 0 failed, 0 aborted suites** in the final gate. Only these same two exact names ran.
- 71 previous test source files mechanically migrate65 build,62 save,27 board and2 work-system
  assertion lines. Those suites compile only, not rerun; historical counts are not current full evidence.
- WorkerServiceTests.Factory forwards an explicitly declared optional activity document; its earlier
  D5 suites are not rerun. No scanner, host gameplay or new condition proof owner was introduced.
- Source remains unchanged after the final build/gates; later edits update documentation only.
- Scoped whitespace/local selected-worker links and previous identity-assertion migration checks pass;
  Git reports normal line-ending notices only. Final source fingerprints match the gate above.

Contract evidence includes input capture/read-only contracts, canonical identity/codec, rejected duplicate/
default structs, malformed/unknown/timer/zero/negative/foreign/nonworker/wrong-role selection, pure preview/
command refusal parity, private frozen pending intents, no premature order/resource/worker reservation,
pending command saves, wrong direct/candidate acquisition guards, live job mismatch, invalid checkpoint
identities/assignments and exact restored selection. The immutable selection decoder rejects truncated,
duplicate, reversed and malformed records without mutating its prior value. This does not prove atomic
whole-board restore, arbitrary historical tampering or global provider mutation rollback.

Physical evidence selects a farther native machine and an independent public worker despite nearer
compatible damaged alternatives. The selected actor physically reaches the bay, earns12 game minutes,
consumes3 spares, holds/releases2 tools, pauses through ordinary policy, departs and receives exactly
one correct condition repair. Unselected machines and the facility stay damaged. Requested/assigned/
earned identities remain distinct in frozen private views; an alternate completion receipt rejects.
Partial work/departure/completion saves, interleaved twins/session isolation and sole-clock pause pass.

Disabled intent waits without consuming stock or offering replacements; re-enabling resumes the same
actor. Ordinary cancellation retains spent inputs and cannot repair. Explicit entity-loss/ownership-loss
instrumentation returns NotFound/NotOwned with no role/travel disclosure, no alternate execution and exact
saved waits. Parallel DAG nodes select native/public distinct actors through real dock/job claims; selecting
one actor for both serializes its claims and completes without deadlock or duplicated tool consumption.
All waits are bounded at240 normal game updates with authoritative node/selection/blocker diagnostics.

## Same-name gate repairs

The first test used a nonexistent IsFrozen property; it now proves actual immutable mutation refusal.
The first contract run expected an open job while the selected worker was policy-disabled. The engine
correctly kept requirements waiting before creating a job. The test now enables that worker through its
normal policy before instrumenting the actual acquired job. No fabricated/open shadow job or special
disabled-worker execution was added. Final source inspection and expanded codec/ownership tests preceded
the final clean build and same two names above.

## Source fingerprints (SHA-256, framework-relative)

```
1AEA4B188CD613B7BD2DEEEAA2484BED4BCE94A993CFC43387EEDF8D8A90748F src/Beep.SimEngine/Standard/ActivityWorkerSelection.cs
FB89D27657FDED956DE745BD44F78BFC2DF88465FBA220E8C98CFB70FC067FB0 src/Beep.SimEngine/Standard/Work.cs
94ED024E2CDC7E9B4328857EE825C77717B54FE195E74DC5166584E4B5FEBA1C src/Beep.SimEngine/Views/WorkView.cs
B917F058D015433824032F1311BB0663C13831FCD9E66177F7A577692424C96D tests/Beep.SimEngine.Tests/SelectedWorkerTests.cs
E120424C367FDEFCEC067D43E2D0BEF9E19924343B9D06BBB252D468373DC715 tests/ExtensionProof/content/selected-workers.json
339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 src/Beep.SimEngine/Hosting/GameClock.cs
```

## Open acceptance

Manual selected-machine/facility service now works through ordinary plans. Automatic fleet subject/
facility search, home/cadence dispatch and provenance, repair teams, preparedness, inspection science,
traffic/fuel/congestion/preferred routes and broader providers/global purity/replay/stress/full P9/P12/
framework acceptance remain open. D5/B9/D3b/D4 and older gates were not rerun; no broad/full regression
was performed. The full implementation objective stays active, not redefined as this prerequisite.
