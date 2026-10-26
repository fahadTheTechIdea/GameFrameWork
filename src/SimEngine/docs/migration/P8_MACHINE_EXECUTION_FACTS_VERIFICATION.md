# P8-D4a — Actual machine execution facts verification

Date: 2026-10-08. Canonical framework only. Full goal/plan remain incomplete; no publication.

## Authoritative implementation

The original WorkBoard implements IWorkAssignments. Existing WorkExecutionFacts now includes
actual order/node/plan, itinerary index, authored phase and composed purpose/target, blocker and
dock references. ReadAssignment checks actual company visibility and reciprocal live job/worker
identity. Idle is explicit null. Worker.GetState no longer retrieves mutable concrete jobs;
WorkView exposes the same private contract facts. Existing JSON roles/activities/executors supply
the projected identities; no new assignment cache, reservation owner, codec or scheduler.

## Verification performed

Two compile builds, both dotnet build Beep.SimEngine.slnx --no-restore:

- First failed11.32s, zero warnings, one new-test compile error: CancelOrderCommand uses Order,
  not OrderId. Native engine/proof projects compiled. Corrected test call only.
- Final clean5.32s, zero warnings/errors. No restore/new packages.

Executable source/test/protocol inputs frozen2026-10-08 19:27:43 UTC before the green run.
Only ONE NEW distinct exact case, FIRST runtime64passed/0failed/0aborted in644ms:

~~~powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Work assignment facts: actual authored phases, private detached holds and saved player continuation' --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
~~~

ChildPID21584 exited0; bounded observer789ms, no stdout/stderr truncation or observer failure.
The thin driver uses ordinary activity/cancellation commands, immutable views and Update.
It reuses existing engine-authored physical phase content. There are no World/job/progress writes.

Scoped checks cover idle/non-worker/absent/private reads, read purity, immutable fact shape,
actual approach/role/order/plan/node and phase target, the same job through authored pause and
funded productive work, detached past facts, saved phase wait/resume, physical completion release,
ordinary command replay and saved pending cancellation. Update faults abort with diagnostics.

## Identity and unchanged boundaries

Core2.313.0/save148/replay1/work-view87. Transformer8/transformation-view15/operations-view11,
WorkBoard44/WorkSystem41 and all native state byte layouts remain unchanged.
GameClock SHA256:35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.

## Limits and next work

Readonly handoff audit:134 source anchors plus13 quickstart anchors matched current files;
all221 original acceptance texts/checkboxes preserved with current source lines.1924 repository
paths,637 Markdown files and4125 local links checked, zero broken targets. All18 master rows
retain five columns. git diff --check exited0 with34 existing line-ending warning lines.
New player driver boundary scan had zero World/resource/progress writes; clock hash unchanged.
The machine-readable ledger records ten additional scoped deliverables, not full completion.

This is actual assignment observation, not transformer team acquisition, atomic machine/resource
admission, process-bound lifetime, output-wait retention, incident interruption recovery or dock
integration acceptance. Malicious/corrupt provider breadth, hauling phase matrix and all prior
consumers were compiled, not runtime re-proven. No older/family/full/Oil/platform test was run.
Production must freshly revalidate and acquire through the existing work owner; copied facts are
never authorization. Original compound P8 and full P1-P12 acceptance remain open.

[Stage](../../plans/enhancements/P8_MACHINE_EXECUTION_FACTS_DESIGN.md),
[tracker](../../plans/enhancements/MASTER_TRACKER.md),
[completion audit](../../plans/enhancements/COMPLETION_AUDIT.md).
