# P4-G2b entity-state requirements — scoped verification

2026-10-07. Canonical Beep.SimEngine only. Full goal/plan ACTIVE. No commit/push.

## Actual invocation ledger

1. `dotnet build Beep.SimEngine.slnx --no-restore`: exit0,0 warnings/errors,5.02s.
2. `dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Entity state requirements: authored guards, physical pauses and saved work"`:
   exit0,74 checks passed/0 failed/0 aborted,375ms, first runtime attempt.

ONE build, ONE runtime invocation, ONE distinct exact NEW bounded case.102 older files/102 build
identity lines migrate/compile ONLY. Shared LiveSensorTests assembly helper is reused without
runtime rerun or source modification beyond its build identity. No older/family/full/platform/
stale/OilFieldDays gate. Source/test/content/public proof frozen after green; docs only.
Historical reports/manifests remain historical, not rerun.

## Single authoritative ownership

EntityStateWorkRequirementDef owns strict state predicate authoring/evaluation. Target and worker
specializations choose the actual existing actor; worker implements IWorkerWorkRequirement.
Explicit nonempty states resolve actual EntityStateDef references, sorted by stable ID and
published immutable. Duplicate IDs or duplicate published state codes refuse; absent/unknown/
empty references, invalid blockers and extra fallback fields refuse. No alias/compatibility
reader. Existing WorkModule registers target_state_requirements/worker_state_requirements in the
same catalog and requirement family. No registry, second predicate evaluator or graph compiler.

Actual live session identity and ownership are checked; the value comes from IEntity.State,
not component-type inference, a state copy or proposed state transition. Missing actor remains
pending (workerless evaluation uses the existing marker rule); mismatch blocks with authored
Code. Existing all/any traversal, candidate dispatch and private requirement views evaluate the
same guard. No command, system, state setter, save payload or additional clock added.

Entity.Update remains the only transition owner, after systems/events. Guards read the last
committed state, including authored priority and precedence, and never issue recursive updates
or predict a same-update transition. Capability controls settle state at that normal point;
subsequent work evaluations observe it. This is workflow readiness, NOT an immediate safety
shutdown mechanism. Actual operation/network safety still uses current capability gates.
State/work feedback can be unsatisfiable if authored against its own paused state; no fabricated
idle substitution is supplied. Wider feedback analyses remain open.

Build2.236.0/save136/all existing layouts/views unchanged. One unchanged GameClock SHA256:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.

## Actual native player workflow

Actual master-listed native-state-work/startup files reuse native sensor/survey modules, world,
navigation, work, knowledge and explicit startup. Pure file assembly selects documents/profile
only. No test-owned provider/worldgen, raw spawn/stock/progress/state mutation or economy provider.
Engine startup creates one site with20 steel and one distant machine. Native sensor operation
controls provide an existing state source; they are not a new test state machine. Authored site
begins disabled, target ready guard accepts idle, shutdown guard accepts stopped. Worker guard
accepts authored idle/moving/working alternatives, not a concrete machine class.

Normal ConfigureOperation commands and Game.Update settle actual worker/site states. Existing
candidate offers reject the stopped machine with its authored blocker, then accept its actual
idle/moving state after normal re-enable. Target all diagnostics report blocked ready state;
any diagnostics select the actual shutdown branch. These reads do not mutate hash or allocate
jobs/stock. The requirement exposes immutable same-content state references. Private foreign
criteria clear output and refuse disclosure.

Posting the physical sample while stopped produces valid player intent but no acquired job or
spent input. Saved twins preserve that blocked order. Normal site re-enable settles idle;
existing work acquires a machine and performs real travel before productive work. Readiness
alone grants no progress or survey. Actual service consumes20→12 materials; partial work earns
no completion effect. Normal site shutdown and following Update pause the same node/job.
Subsequent blocked Updates preserve progress, StateSince and stock; twins remain identical.
Paused save/load preserves actual state and spent input. Normal re-enable resumes real work
and earns native survey completion with actual CompletedBy identity, without another startup
consumption. A second physical order spends12→4, then state-driven pause and ordinary cancel
do not refund materials. Cancelled save and interleaved startup retain independent state/stock/
knowledge. Unknown/duplicate/empty/absent states, same-code aliases, bad blockers/extra fields
and workerless authored nodes refuse before Initialize.

Each wait is bounded at900 Updates with actual time/site/worker/order diagnostics. Case world
coordinates and failure timeout are proof configuration, not engine or platform size limits.

## Review and remaining gates

Lifecycle ownership review reused canonical entity state and requirement evaluation rather than
creating a site-status component, state predictor or host-side decision. The native scoped proof
does NOT establish all providers/geometry/cross-entity policies, universal state/work feedback
analysis, immediate safety isolation, prior-activity compact facts, generic activity executors,
atomic remote tools/teams, arbitrary provider mutation safety, replay/lockstep/fault/stress/soak/
CPU/allocation/full regression or full P1–P12 acceptance. Those stay open. No OilFieldDays/host/
demo/Draw/platform/business gameplay changes. Publication only after FULL completion/verification.

The [scoped source manifest](P4_ENTITY_STATE_REQUIREMENTS_SOURCE_MANIFEST.json) hashes inherited
relevant source plus actual new paths and checks Markdown local links/unchanged clock. It is not
a whole-repository audit or rerun of historical runtime gates. Unrelated dirty work is preserved.

Final scoped inventory:415 unique paths,76 Markdown documents,1469 local links,0 broken links
and0 current manifest hash mismatches. `git diff --check` exit0; existing LF/CRLF warnings remain.
