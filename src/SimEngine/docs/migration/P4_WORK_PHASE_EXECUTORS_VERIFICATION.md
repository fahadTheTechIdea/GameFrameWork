# P4-G3b physical phase executor foundation — scoped verification

2026-10-07. Canonical Beep.SimEngine only. Entire plan/goal ACTIVE. No commit/push.

## Actual invocation ledger

1. `dotnet build Beep.SimEngine.slnx --no-restore`: exit1,0 warnings,11 errors,1.96s.
   Mechanical migration incorrectly renamed dock-slot Kind fields and missed two Toil consumers.
   Repaired those observed errors; dock-slot semantics remain unchanged.
2. Same build: exit0,0 warnings/errors,4.93s.
3. `dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Work phase executors: stable composition, physical work and saved identity"`:
   exit0,42 checks/0 failures/0 aborted,388ms, first runtime attempt.

TWO builds, ONE runtime invocation, ONE distinct NEW bounded case.110 older files/182 build/save
assertion lines compile-migrate ONLY:104 build identities,101 save assertions,23 combined lines.
Three older phase-API consumers and the previous checkpoint's board-schema assertion migrate/compile
ONLY. Existing older raw byte fixtures/layout matrices and unrelated stale expectations remain
runtime-unverified. No older/family/full/platform/stale gate. Source/tests/content FROZEN after green;
only docs/checkpoint inventory afterward. Historical reports are not updated into current evidence.

## Engine ownership

Toil.Executor is a stable Code saved as a string by SAME WorkBoard.Sync. Unknown phase identity
refuses on both write/read through SAME composed catalog. No old enum reader or fallback remains.
Native factories explicitly select travel/productive/pickup/delivery/departure identities.
WorkSystem's RunJob invokes IWorkPhaseExecutor instead of a switch. Its unchanged native algorithms
now have named methods; existing node progress, native transactions, haul handling, dock queues,
policies/retries and worker relation remain sole owners. There is no new scheduler, game clock,
stock/receipt/progress ledger or persistent phase service.

WorkModule declares five native provider services. ServiceContainer.Providers alias-deduplicates
actual composed instances. WorkSystem captures/validates IDs before Initialize, rejecting invalid/
duplicate IDs, and aliases IWorkPhaseCatalog. Sorted immutable IDs and lookup are derived composition,
not state. Native algorithms are engine capabilities, not a new JSON gameplay catalog or hidden
content default. Authored node executor selection is still absent and explicitly open.

WorkPhaseContext is engine-issued for an actual job/worker/target/current Toil. Source guards permit
one delegated physical operation, verify current identity/index/state and expire in finally. This
source review is NOT runtime proof of all malicious/retained context/provider behaviors or a global
mod sandbox. Those fault matrices remain open. Module providers with state must use declared normal
owners/Sync; the new catalog itself owns none.

## Focused actual proof

Native master/startup/navigation/work/knowledge files create actual machine/site/20 steel. Player
PostOrder and CancelOrder plus Game.Update drive all gameplay; no test-spawn, stock/progress/state
mutation, test timer or test economy/provider rules.

- Five explicit native IDs; catalog aliases actual dispatch WorkSystem, is sorted/immutable, reads
  preserve hash; no unknown fallback or business dependency.
- Actual travel job has saved stable travel ID, no progress/material spending and starts away from site.
- Actual board payload is read through SAME codec with one deliberately corrupted phase ID; it refuses
  specifically as an unknown physical executor, not through an old-byte conversion or checksum shortcut.
- Saved travel continues identically to physical arrival and productive work. Materials20->12 at first
  real work; positive duration remains incomplete. Partial productive save continues identically,
  completion never recharges input.
- Second ordinary physical execution12->4, normal cancellation retains spent stock and saves canonically.
  New interleaved session starts with its own20.
- An explicitly selected external composition probe adds one unused stable identity without enum edits;
  two aliases appear once. Duplicate native identity and empty identity refuse before Initialize.
  This proves extension discovery/admission ONLY, not execution of an authored custom workflow.

Each Reach is bounded900 Updates and reports time/actual job/state/index/blocker on a stall.
Travel/productive stages tested; progressive pickup/delivery/departure code compiles but was not run
in this gate. No arbitrary custom-itinerary, dock/haul, replay/stress/CPU/full regression claim.

## Identity and source checks

Build2.238.0/save138/WorkBoard43. WorkSystem41/WorkView78 and all other existing payload layouts
unchanged. No GameClock edit; sole construction remains World.cs:96. Clock SHA256
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.
Scoped inventory429 paths/80 Markdown/1481 local links/0 broken accompanies this report through the
[generated source manifest](P4_WORK_PHASE_EXECUTORS_SOURCE_MANIFEST.json); it excludes its own hash.
It is NOT a whole repository/runtime acceptance gate. Diff check exit0, existing line-ending warnings.

## Remaining acceptance

G3 node IActivityExecutor/JSON executor and typed duration; replace fixed node itinerary constructor/
validator contractually; authored actual custom phase execution, cancellation/recovery/state semantics,
provider invocation/purity/fault matrices, all native haul/dock phase saves, G4 all-workflow migration,
G5 views/ETA/replay and all wider P1–P12 requirements remain open. Native roles/hours authoring remains
current, not falsely accepted executor/duration JSON. No OilFieldDays/host/demo/platform/Draw/business
changes. Publication ONLY after FULL goal/plan completion and verification.
