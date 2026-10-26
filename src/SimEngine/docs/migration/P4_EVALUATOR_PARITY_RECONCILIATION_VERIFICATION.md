# P4-R5 shared evaluator/private projection acceptance

2026-10-15. Production unchanged2.609.0/save159/replay1/work-view108.
Three current exact finals1919/0 plus unchanged adjacent R4 evidence1560/0.
These are separate requirement-specific checkpoints, NOT a3479-check full run.

## Original requirement and decision

**P4.acceptance.014: Previews/commands/blockers share one evaluator and private projection.**

Now checked from the source/runtime mapping below. The requirement does not mean
every valid currently blocked order is rejected, nor that a new order has the same
receipt/worker context as already committed work. It requires engine-owned common
admission/readiness decisions and authorized, immutable diagnostics. No renderer,
host or test supplies a substitute requirement decision.

| Required dimension | Inspected authoritative path | Evidence |
|---|---|---|
| Public command preview and execution share admission | World.Check and input application BOTH call the same Handler.Check; PostOrderHandler uses WorkBoard.CheckPlayerActivity plus PlanObservation.Activation before Apply | Current timed1353/parallel433 preview hashes and accepted inputs; PlanetX133 locked command previews/results create no work; R4 work1025 fault/admission probes |
| Persistent plans use the same activity admission | ActivityDefinitionContracts.CheckSubmission → WorkBoard.CheckPlayerActivity; activation/start rules remain engine-owned and revalidated | Current timed/parallel work; PlanetX133 research/build/control; R4 graph246/staff102/native92/infrastructure95 |
| Proposed/active readiness and actual blockers share evaluation | WorkView.CopyRequirements → WorkBoard.EvaluateRequirements; WorkSystem advancement/frontier/supply/productive work, WorkAcquisition timed admission, WorkStart productive revalidation, WorkAssignments and incident readiness all use that owner or its shared candidate evaluator | Current exact root identity/path/status/blocker comparisons for unclaimed blocked nodes, before/after admission and after save/load; composed incident/research and adjacent staffed-service execution |
| Worker candidate policy shares provider evaluation | WorkBoard candidate selection and WorkSystem.CopyCandidates use the same WorkBoard.EvaluateRequirement; same acquired worker/choice/receipt context feeds runtime | Native and independent parallel433; timed1353 across timed/staffed/all-any/30s/60s/cancel compositions; R4 work1025 candidate observations |
| One typed requirement traversal and resource/cost aggregation | EvaluateRequirementCore iteratively traverses actual requirement/group contracts, validates results, evaluates resource/cost leaves/sets, and projects those SAME results; worker/resource/choice context is explicit | Current timed1353/parallel433 complete contended-set acquisition, refusal without prefix, held tools, material conservation, saved claims/choices, later feasible alternative |
| Private proposed and existing diagnostics | WorkView requires activity disclosure, ownership, target sight and staged publication; Discloses(order/job) applies the same selected sight/catalog authority to existing facts | Current private acquisition facts; R4 work1025 hidden OWN target and saved refusal/no prefix/explicit observer; graph246 whole nested requirement/node/final metadata disclosure, forgetting and rediscovery |
| Pure contract callbacks and fault cutoff | Sole PlanObservation owns intent/identity/submission/activation/reference/binding/identity invocation, using original EntityObservation/World scopes and native order codec | R4 work1025 mutations/throws, expected malformed input, no healthy prefix/save/hash, own retained snapshot and stopped subsequent time; actual dynamic graphs in infrastructure95 |
| Physical technology and operation blockers, not client decisions | Original technology, work, incident, control and inventory owners operate on the same game clock | Current PlanetX133: TechnologyCodes.Locked stops real repair and manufacture/build; actual staffed research consumes input and earns private knowledge, physical response consumes spares and defers restart, saved pending due times retained |
| Durable/read-only projection | Exact root facts and original reservations survive blocked/paused pending-command saves; retained facts do not track later completion; canonical replay verifies engine decisions | Current timed1353/parallel433 saved blocked parity and claims, no hash/time changes; PlanetX133 composed saved work/control/replay; R4 physical saved graph/service/native/corridor cases |

This closure is based on actual source and assertions, not test names or searching
alone. The final all-source consumer check was followed by inspection of each
additional caller: WorkIncidentPreparedness derives actual episode/frontier
criteria (or shared response candidates), WorkAssignments returns the common
requirement blocker before publishing productive assignment, WorkAcquisition
evaluates before timed commitments, and WorkStart revalidates before productive
progress. Other callers are Work.cs advancement/frontier/supply/productive work
and WorkView.CopyRequirements. Candidate/evaluation routes in WorkScheduling/
WorkRequirements/WorkRequirementAlternatives also delegate to the same owner.
Their contexts differ legitimately; there is no parallel preview evaluator or
host-side requirement formula.

## Changes

One assertion-only `Testing.TestKit.AssertUnclaimedRequirementParity` reads public
proposed requirement rows, active rows and actual node facts. It requires no worker/
resources/choices/progress prefix, then compares authored root ID, path, typed
status and exact blocker. It verifies unchanged hash and sole clock. It does NOT
choose branches, calculate requirements, dispatch workers or fabricate readiness.
One shared helper is reused by both existing acquisition cases and their restored
blocked checkpoints; no duplicate fixture/model or production behavior added.

Timed acquisition additionally compares the actual waiting blocker with the
PRE-command shortage. Its existing512-update waits now also stop after20seconds
with the original stock/claim/trail diagnostics, without increasing the update
budget. Already committed work is not incorrectly compared against new-intent
readiness: owned receipts/selected alternatives are a legitimate different context.

## All attempts

Baseline targeted literals on the previously clean built2.609.0 artifact:

- Timed953/0,2507ms, PID26404 exit0/2654ms.
- Parallel337/0,6592ms, PID7372 exit0/6733ms.

Then `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`
exited0, zero warnings/errors,4.56s reported. No runtime before terminal success.

Final exact invocations used `--bounded-exact`,60second timeout,5second termination
deadline and bounded output:

1. `Timed acquisition: authored all-any timed and staffed frontiers share atomic branch commitments`
   —1353/0,2438ms, PID30228 exit0/2577ms.
2. `Parallel acquisition: authored all-any roles tools and materials reserve whole contended sets`
   —433/0,6433ms, PID2004 exit0/6570ms.
3. `Planet X control acceptance: authored research, deferred safety control and physical incident recovery`
   —133/0,15472ms, PID31228 exit0/15613ms; session53876 polled to terminal.

Final sum1919/0. No failed attempt, process abort or timeout in R5. Baselines are
not added to final totals. No full/profile/family selector. The adjacent R4 five
cases1560/0 were NOT rerun here or represented as a fresh whole regression.

Core442 C# SHA256 rechecked:
`8620234b8075f6de18233ad95880511ed828494f13f2173831d4198087d69d7a`, identical to R4.
No production or content/protocol edits in R5. Existing TestKit methods remain
unchanged; the new assertion helper is called only by the two named cases.

## Remaining scope

Only original P4.acceptance.014 changes. Bookkeeping221/199recorded checked/
22tracked open is a delta, NOT a fresh audit of all199items. Full objective remains
active. This does not close later all-gameplay-menu/knowledge disclosure, arbitrary
CLR/provider purity, all-optional-module interleaving, stress/fuzz/allocation/soak
or full-profile gates. Expected state changes between preview and application
still cause normal revalidation, not a guaranteed future reservation.

No Oil/Tycoon/host/platform/Draw changes or Git staging/commit/push.
