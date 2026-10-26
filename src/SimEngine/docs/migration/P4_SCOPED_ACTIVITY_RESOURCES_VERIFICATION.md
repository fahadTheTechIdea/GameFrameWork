# P4-G2i scoped activity resources and actual-endpoint supply verification

Date: 2026-10-08. Status: native slice focused-verified; full P4/P8/P9/P1-P12 OPEN.

## Scope and ownership

The existing activity DAG now resolves explicitly authored target and worker ports through one
native EntityResourceRequirementDef source algorithm. The existing requirement context carries
the original committed endpoint into intrinsic evaluation, offers and source checks. It adds no
new inventory, claim, clock, scheduler or demand owner.

The existing work-to-logistics envelope uses LogisticsSupplyRequest. Aliases sharing one actual
store aggregate their quantities; distinct actual stores remain independent. Named requirements
do not fabricate local supply-buffer capacity. JSON must explicitly author port:null or a port name;
there is no legacy default reader. Startup, continuous and held requirements continue through the
original graph, claims, physical hauling, worker dispatch and completion/cancellation paths.

49 older JSON files containing80 native entity-resource rows and six inline C# declarations were
migrated. The external role-aware provider reuses the native entity algorithm. Those older cases
were compiled only, not executed. Section names and ownership remain unchanged.

Normal master-listed scoped-activity JSON authors the new warehouse, actual stores, named ports,
worker and DAG. The thin test driver uses public commands, immutable views, Update, saves and replay;
it does not implement gameplay rules or write inventory directly.

## Current protocol

Core2.321.0; save153; replay1; WorkBoard48; WorkSystem42; work-view91;
logistics-service19; logistics-policy3; logistics-view34. Transformer9,
transformation-view17, operations-view11 and inventory-view5 unchanged.

The sole GameClock SHA256 is
35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.
Old saves are rejected by native build identity; no compatibility reader was introduced.

## Verification actually run

Six compilation builds: dotnet build Beep.SimEngine.slnx --no-restore.

- Clean17.57s, zero warnings/errors.
- Failed6.43s: two new test references used WorkRequirementView.Resources instead of Result.Resources.
- Clean5.09s after correcting those references.
- Clean3.87s after adding native notification diagnostics.
- Clean4.11s after correcting the test's completion-history plan key.
- FINAL clean6.09s after passing committed source facts to intrinsic leaf evaluation and adding physical consumption accounting.

ONE NEW exact case, with only same-case repairs/reruns:

Scoped activity resources: parallel actual supply, running sources and physical tool lifetime

| Attempt | Passed | Failed | Aborted | Case elapsed | Process observation |
| --- | ---: | ---: | ---: | ---: | --- |
| 1 |47|1|1|2927ms|PID20580, exit1, observed3082ms|
| 2 |47|1|1|2734ms|PID33572, exit1, observed2887ms|
| 3 |90|0|0|2985ms|PID37252, exit0, observed3145ms|
| FINAL |101|0|0|3147ms|PID20108, exit0, observed3319ms|

The first two attempts exhausted the native1000-Update stage bound while looking up completion by
the plain content ID. Native notification388/note.order-done showed the workflow had completed.
The test lookup was corrected to activity:dag.scoped; no engine fallback was added.
All process outputs were untruncated and had no observer error. No OS timeout or forced termination.
Each physical Until stage is bounded to1000Updates with graph, demand, job and notification diagnostics.

Exact command:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Scoped activity resources: parallel actual supply, running sources and physical tool lifetime" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

## Focused proof

The case physically supplies parallel startup consume and continuous-flow nodes, then dispatches
the ordinary machine to a dependent assembly stage. Initial demands aggregate actual First aliases
to5units while Second requests the next flow quantum, not a fabricated total.

The running flow source remains committed to original Second after the current port changes to First.
Target and worker tool holds remain independently reserved against original Third while their current
selections change. Saved twins continue identically during physical supply, running work and held work.
Conflicting tool export is refused while held.

Completion withdraws original work demands and releases the two held tools. Native stock accounting
shows20 remaining units from30 after5startup+3flow+2assembly consumption; held tools are not consumed.
An empty newly selected port gives NoSupply, never implicit fallback to the released original tools.
A second order proves saved cancellation releases original holds and withdraws original demands.
Immutable occurrence facts, company privacy, fault-free continuation and normal replay are checked.

Post-pass executable freeze: 2026-10-08 22:29:26UTC. Only tracker/audit/help documentation changes follow.

## Limits and next work

Named elapsed-resource authoring shares the native algorithm but has no named-elapsed runtime proof.
Invalid-port binding validation is source/compile evidence, not a new malformed-content runtime case.
Scoped recovery and same-site automatic matching remain next source gaps. External providers,
removed stores, purity faults, corruption, optional journals, docks, areas, emergencies, joint claims,
fuel/route/congestion stress, allocation budgets, global accounting, lockstep, shuffled input and
full durability remain OPEN. No older, family, full, Oil, platform or historical runtime suites ran.

No phase acceptance checkbox is promoted by this focused slice. Full goal remains incomplete.
No staging, commit or push until the FULL goal AND plan are genuinely finished and verified.

[Stage](../../plans/enhancements/P4_SCOPED_ACTIVITY_RESOURCES_DESIGN.md) ·
[Master tracker](../../plans/enhancements/MASTER_TRACKER.md) ·
[Completion audit](../../plans/enhancements/COMPLETION_AUDIT.md)

