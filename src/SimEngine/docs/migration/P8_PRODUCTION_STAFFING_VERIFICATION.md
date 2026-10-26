# P8-D4b — Actual production staffing verification

## Current M159 contract-authored staffing — 2026-10-25

Observed dirty master based on cb61483ab81404c11944931aedb4703ba694edb5;
engine2.776.0/save161/replay2. Only Beep.SimEngine source, proof authoring/tests and
existing documentation were edited. Quality/lot remains deferred by human instruction.
Artifact owner: `artifacts/optional-module-durability/m159-staffing-contracts/`.

The native staffing definition is now a thin wrapper over StaffedTransformationGateDeclaration.
Independent ProofStaffingGateDef implements IStaffedTransformationGateDefinition directly,
delegating to that same declaration without a native-definition subclass or validation override.
The original StaffedTransformationGate is non-generic and consumes that metadata contract.
ITransformationStaffingRequirement includes its actual authored activity alias. No runtime
fields, authoritative payload, codec2, work/queue lifetime, material amounts or durations changed.
One binding owner validates loaded metadata, including automatic team graph constraints formerly
located in the native definition. TransformerManager calls it before its spatial exclusion;
WorkBoard/worker dispatch/material transactions remain their original owners.

Exact invocation for each row:
`dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact '<case>' --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 25000`.

| Exact case | Passed/failed | Body/process ms | PID | Log |
|---|---|---|---|---|
| Production staffing: independent JSON metadata shares physical facility teams and replay | 100/0 | 2264/2457 | 12680 | independent-facility-1.log |
| Process staffing: independent JSON metadata shares queue teams lifetime and replay | 104/0 | 5489/5664 | 19276 | independent-process-1.log |
| Production staffing: authored distinct physical machines, retained process holds and saved player control | 100/0 | 2224/2402 | 15544 | native-facility-1.log |
| Process staffing: automatic exact queue teams, retained duty, sibling contention and saved departure | 104/0 | 4885/5063 | 1624 | native-process-1.log |

408/0 aggregate, four exact affected cases, all exit0/zero aborts/truncation/observer error.
Each case runs one actual ReplayVerifier and now also compares ActualHash with the actual
final live game hash; four verifications total. Hash values/checkpoint counts are not printed
and are not invented. All four ran on the same image after one targeted Release --no-restore
build of tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj:8.80s/zero warnings/errors,
build-1.log. No intermediate failures, additional builds, full/profile/family/soak execution
or unrelated green reruns. Metadata-only profile inspection1110 registered/1089 included/
21 excluded/zero runtime bodies, profile-metadata.log.

Native DLL SHA256:196702CA0A851FAB3055B469867E0380A9B2F851B0C80AF90DC6F0829CB11244.
before-inputs.json preserves eight selected pre-edit source/config hashes and dirty baseline;
final-inputs.json records those eight plus five binary/PDB identities. This is a scoped input
inventory, not a whole-repository freeze or a fresh resource mutation census.

Self-review: both existing physical drivers were parameterized in place, not copied. Only the
authoring kind and explicitly master-listed proof module/document ownership differ; native
content and both driver semantics remain the same. All input is ordinary engine commands,
updates, views, saves and replay; metadata type checks are read-only observations. Facility
proof includes real arrival, distinct alternative roles, holds, resource preservation, pause/
cancel and refusal. Process proof includes exact queue execution lifetime, retained minimum
work, sibling contention, real replacement/departure, continuous duty/material consumption,
incomplete teams and explicit malformed JSON. Independent definitions omit local validation,
so successful content admission and refusal exercise engine composition, not a mod-side
fallback. All-loaded iteration is source-reviewed; no claim of a separate unused-prototype
runtime case or universal hostile-provider purity is made.

No added state service/clock, duplication of assignment or matching logic, compatibility path,
instant machine/material fallback, business behavior, Oil/host/demo/platform change or staging/
commit/push. World first-fault, existing private views, events/journal/replay retain diagnostic
ownership; broader operational sink/audit/notification work remains recorded in the master.
P8.017 and other full feature obligations remain open; this closes the concrete staffing
definition coupling, not a complete framework or all-provider qualification.

## Historical D4b evidence

Date2026-10-08. Canonical framework only; full goal/plan/publication remain open.

## Authoritative changes

StaffedTransformationGateDef resolves strict stable requirement IDs, actual activity/node references,
positive counts and alternative roles from master-listed JSON. Only a selected staffing capability
requires actual IWorkAssignments/IWorkerDispatch services. No implicit staffing is added to other
recipes. Native ITransformationGate dispatch now reads real productive actor teams through the same
WorkBoard. CheckProductiveAssignment freshly checks native ownership/job/phase/earned work, current
policy/rate/requirements and physical reach. The existing dock algorithm has one pure ReadDockReach
core and its original mutating CheckDockReach wrapper; diagnostics do not change job blockers.

DistinctWorkerMatching is reused; no scheduler, job store, actor claim, stock copy, team codec or
clock was added. The stateless staffing component derives private immutable diagnostics from the
actual existing assignments. Native batch/continuous execution already checks gates before acquiring,
burning or completing resources. Staffing loss retains WIP/tools/progress; material cancellation
still runs the existing return/release path without pretending to staff a machine.

The new engine-owned proof master/file uses native work/physical travel, two authored machine roles,
finite stock20, a two-node staffed activity and illustrative batch/continuous recipes. Those yields
are game examples, not engineering data. The helper only composes files/modules; the player driver
uses ordinary commands, immutable views and Update, never World/job/resource/progress mutations.

## Builds and exact runtimes

Four dotnet build Beep.SimEngine.slnx --no-restore builds:

1. Failed3.34s, zero warnings, two proof fixture errors: missing Kernel namespace for GameDefinition/
   GameModule. Native engine compiled; added the import.
2. Clean4.50s0 warnings/errors.
3. Clean8.49s0 warnings/errors after correcting new authored rates and forcing the alternative-only
   technician role/policy in the fixture.
4. Final clean3.49s0 warnings/errors after adding contested-actor/large-count/missing-work checks.

Only ONE NEW distinct exact bounded case, three attempts:

- First2passed/1failed with suite abort at content load: new rate JSON incorrectly included a unit
  field. The existing strict ResourceRateSpec properly rejected it; removed that fixture field.
  PID20216 exited1; observer263ms, case103ms, no truncation/observer failure.
- Second85passed/0failed/0aborted in2003ms; PID11196 exited0, observer2334ms, no truncation/failure.
- Final98passed/0failed/0aborted in785ms; PID29944 exited0, observer919ms, no truncation/failure.

Executable code/content/test/protocol inputs frozen2026-10-08 19:48:49 UTC before final green.

~~~powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Production staffing: authored distinct physical machines, retained process holds and saved player control' --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
~~~

Scoped coverage: idle/posted/travelling actors are not productive staff; actual two-machine role
alternatives and node identities; company-private/pure facts; no startup prefix; staffing loss and
saved resumption; same real resource tool holds; native batch completion; continuous waits/stop;
staffing order cancellation; saved pending material cancellation and command replay. A contested
primary actor shared by two needs counts once and spends nothing. int.MaxValue requested staff
remains an authored shortage, with no artificial platform bound or allocated ghost worker slots.
Malformed IDs/counts/activities/nodes/role alternatives and an unselected work module refuse before
Update. The missing-work case establishes refusal, not isolated coverage of every service validator.

## Identity and limits

Readonly handoff audit:142 source anchors plus13 quickstart anchors match current files. All221
original acceptance texts/checkboxes remain unchanged with refreshed source lines.1931 repository
paths,639 Markdown files and4140 local links checked, zero broken targets. All18 master rows have
five columns. git diff --check exited0 with34 existing line-ending warning lines. New driver/proof
composition boundary scan found zero World/resource/progress writes. Clock hash unchanged; eleven
additional scoped deliverables are recorded without inferring full completion.

Core2.314.0/save148/replay1/new zero-state staffing component1/transformation-view16/work-view87.
Existing transformer8/WorkBoard44/WorkSystem41/operations-view11 codecs/layouts remain unchanged.
GameClock SHA256:35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.

Ordinary authored work owns the staffing interval; the processor does not automatically create,
retire or synchronise that activity to its queue. Queue-bound joint team/resource startup, lifetime
through output waits, automatic release/departure/replacement, custom phase/provider/fault/dock
breadth, scoped supply/export, lockstep/accounting/stress and full P8/P1-P12 gates remain open.
This is facility staffing, not a per-processor exclusive entity-tool lease. Named sibling processors
can observe the same facility work; their process-bound exclusivity still needs authoritative work
binding and evidence. This case has one named processor and does not prove sibling exclusivity.
Prior cases compile only and were not rerun; no family/full/Oil/platform runtime was executed.
No full compound acceptance checkbox is promoted by98 scoped checks.

[Stage](../../plans/enhancements/P8_PRODUCTION_STAFFING_DESIGN.md),
[tracker](../../plans/enhancements/MASTER_TRACKER.md),
[completion audit](../../plans/enhancements/COMPLETION_AUDIT.md).
