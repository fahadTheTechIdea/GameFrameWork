# P11-C6 pending-plan recovery — focused evidence

Verified2026-10-08. Build2.292.0 / Save142 / Replay1 / playbook-state4 / controller-view5.
[Design](../../plans/enhancements/P11_PENDING_PLAN_RECOVERY_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md),
[source manifest](P11_PENDING_PLAN_RECOVERY_SOURCE_MANIFEST.json).
Full goal ACTIVE; no staging, commit or push before full goal AND plan finish and verify.

## Engine ownership

IPlanRecoveryPolicy/PlanRecoveryPolicyDef uses the existing content catalog and original provider
purity boundary. PendingPlanRecoveryDef explicitly authors delay, blocker IDs and maximum recoveries.
Every existing playbook step now explicitly selects recovery:null or a registered profile ID.
The existing Playbook Execution/Sync alone owns observed blocked timestamp, attempts, recovery flag
and actual cancellation ticket/plan link; actual plans, projects, materials and workers remain native.

The policy is sampled at authored decision cadence. It does not invent a second current clock or
record every intermediate change between decisions. Cancellation uses the revised existing
CancelPlanCommand, shared board preview/commit and pending-only state guard. It cannot cancel active
work after a stale preview. Real cancellation answers precede link release/reselection; the existing
site source and activation preview decide the next valid site. New deterministic attempt group
identities preserve prior native history. Refused/no-site/attempt-limit/player-paused cases wait;
no instant relocation, free resources, compensation or fabricated completion.

Observed first runtime failure was in cross-provider composition, not hidden by a fixture reorder:
controller site validation executed before WorkBoard captured its immutable activity catalog.
ServiceContainer's original alias-deduplicated pass now prepares all providers after registration
freeze and before any cross-provider validation. Public ICompositionPreparation is the open contract;
WorkBoard moves its sole catalog capture into it. No lazy lookup, second catalog, controller-specific
type switch or extra runtime initialization loop.

165 existing source paths mechanically migrated current build/save identities or cancellation
signatures; older cases compiled only. Three prior explicit playbook JSON documents migrate with
recovery:null; one new explicit recovery document and one new test driver. Public non-friend proof
adds a JSON relay policy implementing the same contract without scheduler edits.

## Every build/runtime attempt

Build command: `dotnet build Beep.SimEngine.slnx`.

| Build | Result | Observation/repair |
| --- | --- | --- |
| 1 | Failed5.66s,0 warnings1 error | New context treated actual PlanView value type as nullable; use identity/Intent validity |
| 2 | Failed10.67s,0 warnings2 errors | New test passed TimeSpan to actual long-microseconds Update API; fix driver argument |
| 3 | Clean5.23s,0 warnings/errors | First focused runtime exposed actual composition ordering |
| 4 | Clean5.65s,0 warnings/errors | Move existing catalog capture to declaration preparation before validators |

Second build returned live session1565 and was polled once to completion, not restarted.

All runtime invocations used SAME ONE exact NEW bounded case:

```powershell
dotnet run --project tests\Beep.SimEngine.Tests --no-build -- "Player controllers: authored pending-plan recovery, physical rivals and durable cancellation"
```

| Runtime | Result | Observation |
| --- | --- | --- |
| 1 | 2 passed1 failed/aborted165ms,exit1 | Activity declarations not captured during controller site validation |
| 2 | 185 passed0 failed0 aborted1709ms,exit0 | Final native/public recovery and physical proof green |

No old/family/full/platform/stale runtime was run. Executable inputs frozen at
2026-10-08 11:09:42 UTC; only docs/read-only audit follows.

## Scoped coverage

- Native and public-policy real player/rival site race; accepted intent remains unstarted and stock20.
- Authored delay and sole-clock pause, immutable retained facts and foreign-company diagnostic denial.
- Save during observed delay, queued cancellation and replacement; physical saved twins agree.
- Real normal cancellation ends original intent, alternative at42 becomes ordinary project with distinct group.
- Player and rival each physically haul/build, consuming8 of their own20, no grant/refund/resource copying.
- Canonical replay reproduces both native/public policies and final physical state.
- Explicit attempt cap retains second blocked intent; no viable alternate stays a typed wait.
- Player pause resets sampled delay; pending-only command/view/actual refusal protects activated work.
- Eight malformed JSON/reference cases refuse; explicit null never installs a recovery fallback.
- Independent policy throw faults original Update; Save/Hash refuse the faulted state.
- Independent interleaved sessions begin with no borrowed recovery state.

Every physical wait <=2000 updates, with time/actual-plan/blocker/current-recovery diagnostics.
Tests simulate normal players and read authoritative evidence; they own no gameplay progression.

## Still open

Broader active-project/material/route recovery, multi-entry cancellation races, counter/timestamp
corruption and grouped history stress, industrial risk/expansion/maintenance/survey profile breadth,
trusted provider/global purity/CPU, concurrency/lockstep/performance/full accounting/Oil and explicit
authorized P6/full P1-P12 acceptance remain open. This resource-only proof does not prove optional-money
recovery. Earlier runtime evidence is historical and was not rerun on2.292.0.

## Read-only handoff audit

1778 repository-visible/prior-referenced paths;580 Markdown documents;3479 local links.
The self-excluded linked manifest resolves the final pending link.18 five-column phase rows valid;
both new help targets exist. Actual git diff --check exit0 with31 existing LF/CRLF warnings.
357 prior engine files compared by SHA256:14 changed in this slice, one new recovery policy file.
324 prior src/tests JSON:three deliberate explicit-null playbook migrations, one new recovery JSON.
Sole GameClock SHA256 unchanged; final manifest hash consistency checked after docs-only refresh.
