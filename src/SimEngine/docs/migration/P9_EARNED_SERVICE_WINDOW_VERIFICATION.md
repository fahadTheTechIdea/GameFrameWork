# P9-B11.3c2b3 earned service windows — verification

Date: 2026-10-07. Dirty working-tree checkpoint, not clean HEAD attribution or full regression.
Build2.195.0/save104/board36/work35/logistics13/work view69/logistics view24/native recurring8/
public recurring5,5,6. Existing owner payloads remain unchanged.
[Design](../../plans/enhancements/P9_EARNED_SERVICE_WINDOW_DESIGN.md).

## Reproduced regression and correction

An exact native/public regression first produced two failures: cancelled first service returned
none for prospective movement instead of work.recurring-movement-window. The source correctly
had no completed cycle, but CheckMovementWindow used its future retry deadline as service credit.
This was an engine error, not a provider difference or a timing assertion relaxed to fit output.

Existing ResolveCadence now derives completed-service timing independently of retry admission in
one canonical calculation. The second output is local read scratch, not stored state, another
clock, a copied deadline or a service-success ledger. Native source references and actual board
whole-cycle success remain the owners. Failed/cancelled attempts still delay retries, but leave
earned service timing overdue or initially zero. Whole actual completion alone renews it.

RecurringPlanView.ServiceDueTick/ServiceDueGameTime expose detached completed-service timing;
DueTick/DueGameTime retain next-attempt admission semantics. Both are projections on one game
clock, not independent timers. Generic submission cadence has no completed-service deadline.
Trip service references must select worker-bound scheduled successful_completion sources.
Submission remains a valid explicit generic recurring mode, but cannot impersonate performed
maintenance. Unknown/outstanding/pending/disabled/overflow/emergency semantics still use the
existing physical and protocol gates. Retained cargo behavior remains unchanged.

## Native/public physical proof

Tests use ordinary commands, engine views, bounded Game.Update, saves and native board history.
The failed workflow is explicit master-listed completion-failed-service.json. It reuses the existing
public CompletionEffectModule/CompletionEffectDef; no new scheduler, failure rule or gameplay logic
was implemented in the test host. The normal engine effect protocol terminates the real workflow.

Initial cancellation leaves no completed service despite future retry admission. Native/public
private timing and saved snapshots preserve zero earned timing and movement refusal. Actual
spares, tools, machine time, restoration, exit and whole DAG completion then earn a real window.
Retaining the cancelled view does not mutate it into later success. Saved twins agree.

After a prior success, the next due attempt is cancelled with ordinary commands. Successful
history and its exhausted deadline remain unchanged while retry admission moves forward. Fresh
movement is refused before and after save/load. A later physically completed retry renews it.

The failure fixture performs real repair and later stages, then returns the existing typed public
completion rejection. The native plan becomes Failed and retains that blocker even after its order
is pruned. Condition restoration to10 cannot turn whole-workflow failure into successful service.
Repeated real retries also fail without refreshing service age; saved twin hashes remain identical.

## Focused final gate

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Completion service: authored cadence, private admission and retained success"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Completion service: timed work, materials, restoration and saved continuation"
```

Final build: zero warnings/errors. Admission274 + physical276 =550 passed, zero final failures
or aborted suites. Only these same two exact service suites ran. Other suites compile only;
no full/family/third gate ran. Physical waits are bounded at400 updates, existing grouped proofs480.

During test authoring, a nullable-duration comparison did not compile and an assertion dereferenced
an already-pruned failed order. Those fixture errors were corrected using duration facts and the
authoritative retained plan blocker. No-build output after the failed build was from the older
binary and is excluded from current verification; all final counts follow a successful fresh build.

The build identity migrates directly across81 literal source/test paths. Six logistics schema
assertions migrate to13; these separate suites compile only. New work/logistics view semantic
identities describe current projection/admission behavior; source and owner payloads do not change.
No old reader, compatibility facade, forced repair, stock grant or alternate state path is added.

## Boundary and remaining work

GameClock.cs remains unchanged, SHA256:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
World constructs it once; SimGame reuses it; World.Tick only projects Clock.StepIndex. New service
and retry facts both derive from this one clock and the same actual history.

The lifecycle skill kept the reproduced correction, staged implementation, scoped evidence and
master tracker aligned. Literal fingerprints appear in [the manifest](P9_EARNED_SERVICE_WINDOW_SOURCE_MANIFEST.json).
Older reports/manifests remain historical checkpoints. The dirty worktree is preserved.

B11.3c2b3 is focused verified, not full B11.3c2b/B11.4 or roadmap completion. Wider fleet/multi-source
service fairness, queue/congestion, new early-condition combinations, arbitrary failures/provider
stress, propulsion rescue, global purity, CPU/allocations, replay and full acceptance remain open.
Movement windows still do not forecast uncertain handling/queue delay or guarantee arrival.
No Oil/Tycoon/host/demo/platform/Draw work or full-regression claim.
