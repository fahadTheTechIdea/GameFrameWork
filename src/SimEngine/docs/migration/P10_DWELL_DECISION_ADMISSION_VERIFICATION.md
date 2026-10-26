# P10-DA1 — Dwell decision admission verification

## Outcome

Engine **2.629.0 / save159 / replay1**. Original AutomationSystem prepares evaluation
state, dwell candidate and next-step intent inside its existing
EntityObservation.ReadParticipants interval, then publishes to its original Record
only after verification. No other scheduler/state/clock/codec owner was added.

The demonstrated failure is fixed; original **P10.acceptance.022 remains open**:
broader control-provider/fault/replay/stress matrices and full P10 acceptance are
not proven. Bookkeeping remains221 original/208 recorded checked/13 open after the
preceding P5-CF1 closure. Full goal remains open.

## Actual bug and repair

Before repair, Update observed Evaluate/ValidateEvaluation, then wrote State,
Candidate and Since before calling rule.Dwell outside observation. Silent getter
mutation of an actual bound entity's private persisted payload completed the step
as healthy. Throwing getters left a rule-state prefix and faulted only at the outer
sim.automation system instead of the provider boundary.

The original decision logic moved into the same observed callback. It returns
transient result/candidate/since/pending/due values and commits only after observation.
Dwell conversion and checked next-step deadline admission now precede all decision
writes. Existing short circuits still skip Dwell during cooldown, pending action,
typed blocker or an unchanged applied branch/value. Blockers use original Suspend;
newly evaluated commands still wait for the next simulation step.

This is not rollback: hostile provider payload/clock effects may remain in the
terminal world. Healthy rule publication, completed time, snapshots and save/hash
certification are withheld. Existing first-fault/exception ownership is retained.
EngineInfo.Build and independent current-engine-protocol.json expected build change
2.628.0→2.629.0 for changed simulation behavior. Save159/replay1/automation codec5 and
all content/view/payload schemas are unchanged; no compatibility path was added.

## Existing contracts and test owners

One directly registered dwell literal extends RuleProviderObservationTests using its
Rules/Bound helpers and existing non-friend BandControlRuleDef.TimingMetadataProbe.
No new provider, extension hook, JSON catalog, scheduler or client gameplay was needed.

Six healthy contexts cover fresh/loaded sensor, target and shared bindings. An ordinary
SetAutomationProbeCommand changes the signal after an earlier false action settles.
The real evaluator records true intent; the normal action owner applies next step;
saved twins remain identical.

Eighteen negative contexts cover those bindings with silent private payload mutation,
payload+throw and clock+throw during Dwell. They prove actual payload change without
Touch, one callback, observation.provider first fault, original throw cause, unchanged
complete rule codec, no completed step/healthy snapshot/success event, refused save/
hash and an independent saved session actually continuing after hook removal.
The ordinary preceding input command is legitimate engine mutation; its rollback
is neither implemented nor asserted.

## Every build and execution attempt

Targeted command:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
```

Before-fix4.55s and after-fix9.03s builds both exit0, zero warnings/errors. Every
runtime selected one exact literal, timeout60000ms, termination-timeout5000ms,
output10000 characters for baseline/6000 for finals. All handles terminal, no
truncation/observer error/timeout/aborted suite/duplicate process restart.

| Attempt/case | Pass/fail | Suite/process ms | Terminal child |
|---|---:|---:|---|
| Pre-repair dwell |210/48 |414/558 | PID31892, Exited1 |
| Final dwell |270/0 |452/617 | PID23264, Exited0 |
| Existing rule-provider interval/composition |348/0 |564/720 | PID31564, Exited0 |
| Existing deferred-action commit/cooldown |130/0 |379/531 | PID31496, Exited0 |
| Existing independent normal action/timing/save |13/0 |273/429 | PID35136, Exited0 |

Final **761 passed / 0 failed**. Assertions were not weakened after baseline;
the total rises because six formerly silent/healthy failures now execute required
faulted save/hash refusal checks. Exact names:

```text
Rule dwell admission: bound metadata faults cannot publish candidate state or deferred intent
Rule providers: original evaluation participants and composition errors remain inside one observation owner
Deferred action commits: metadata admission and swallowed native faults cannot publish applied rule state
Control actions: independent normal commands deferred timing and saved continuation
```

## Identities and limits

442 core C# sorted path/content SHA256:
`769dd9f3ba5c443de7dcc682190dcc99487ab7d5e7a10929c897c0c6269a6241`.
Debug framework:
`B5054D6C82C5ACF6FA16090322A28C41FA2C28CB7119502965F532DECC297F7C`.
Framework test assembly:
`388480CDF0F69F8379E2B8AFE7692C571F188EC313EA13CEDFB80DD8E07B406B`.
RuleProviderObservationTests source:
`5B5A8F501E132055CBB26384D70EA3284B39F80258EB9CF355C8228A4E3F1526`.

Earlier P5-CF1 evidence was on2.628.0 before this production change; it is not an Oil
rebuild/retest on2.629. Historical checked gates were not globally revalidated.
Arbitrary reentrant rule/handler changes, all metadata entry points, shared provider
corruption, replay/stress/performance and full P10 remain open. No full/family/profile
or broad automation bundle ran. No Oil/Tycoon/platform/Draw/adapter, duplicate owner
or Git publication changes. One initial multi-document apply_patch was rejected for
an incorrect documentation heading; no partial files were written, and it was
reapplied with the inspected actual heading. No additional compile/runtime was needed.
