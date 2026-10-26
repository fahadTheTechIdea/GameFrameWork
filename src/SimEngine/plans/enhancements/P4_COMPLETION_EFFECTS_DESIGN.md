# P4-G3a — Typed native completion and retained final-step waits

Status: implemented and focused-verified on 2026-10-05; full P4/framework completion remains open.
Build2.72.0/save44/work21/work-view26; 214 checks in the two exact suites below,
zero warnings/errors/failures/aborts. [Evidence](../../docs/migration/P4_COMPLETION_EFFECTS_VERIFICATION.md).

Current ownership: [P12-A4/P4-G6](P12_INCIDENT_CONTENT_CONTRACTS_DESIGN.md) opens actual effect/workflow content aliases using one shared parser/completion owner and composition-time admission. Original state/progress ownership unchanged; historical runtime below not rerun on2.338.0.

## Specification and ownership

Replace Code-only IWorkEffect.Apply with one immutable WorkEffectResult: Applied,
Pending(blocker), Failed(blocker). Refusals must not mutate their provider. WorkBoard
remains the sole order/node/job codec, WorkSystem the only advancement/scheduler.
No compatibility overload, second completion queue, copied progress or host timer.

## Execution

- Completed physical work releases its actual worker and unspent tools, then enters
  NodeState.Completing. Progress, spent inputs/costs and selected branches stay earned.
- Only Applied makes the node Done; dependent nodes wait. Failed ends the order normally.
- Pending requires the existing selected IWorkRetryPolicy to authorize its blocker;
  otherwise it is a terminal refusal. Consecutive attempts and checked deadlines live
  on the actual node/order, using that same authored session delay/backoff.
- After every node is Done, OrderState.Completing retains the final effect/replacement
  stage. Record whether the order effect was applied, so a blocked replacement cannot
  repeat an irreversible effect. No effect runs before its native work is complete.
- Pause stops retry execution without resetting deadlines/progress; cancellation clears
  pending retries through existing native cleanup. Target/episode loss remains terminal.
- Construction, recovery, line, incident and external effects migrate directly to the
  typed contract; permanent identity/binding errors are not retryable native results.

## Durability and diagnostics

Embed retry facts and the order effect-applied fact in the sole WorkBoard.Sync. Validate
states, completed progress, dependency ancestry, no live completed-node job, positive
authored retry deadlines and terminal cleanup at checkpoint/load. Break work/view/global
build/save identities together; no old reader. Detached company-private node/order views
report actual failure count, deadline and remaining ticks; pure reads do not evaluate effects.

## Files and risks

Standard/WorkEffects.cs, Work.cs, Incidents.cs, WorkModule.cs, Core/WorkStates.cs,
Views/WorkView.cs, kernel identity/schema declarations, public-contract proof providers,
focused tests/content and master/P3/P4/P7/P9/README/AGENTS/evidence. Provider commits must
remain atomic on refusal; a violation faults Update, not fictitious compensation.

## Focused verification gate

Build Beep.SimEngine.slnx. Run only these two exact suites (repeats allowed):

1. `Completion effects: typed contracts and private retry state`
2. `Completion effects: physical blockers and saved continuation`

Prove terminal/unlisted refusal, typed provider outcomes, complete progress without
workers/claims, authored cadence/backoff, dependency blocking, private/pure diagnostics,
pause/cancel, real point construction/recovery blockers, save/load/isolated twins and
exactly-once effects. Reuse directly affected recovery checks inside these suites.
No third/family/full suite. Generic executor breadth, templates, incidents, fleets,
provider fault matrices, replay/stress and P8/P10/P11/P12/full gates remain open.

## Landed review

The typed interface directly replaces the old Code-only callback. Native profiles classify
refusals through the selected immutable retry content; external providers return typed
results explicitly, and the board still requires authorization before deferring. Physical
completion releases the real worker/tools once; no requirement/spending phase reruns.
Node/order retry records and the order effect-applied fact are embedded in WorkBoard.Sync.
Temporary construction/recovery promises retain the actual site/stock. Applied effects
are skipped during replacement waits, including after restore. Pause retains original
blocker/deadline; cancellation clears only native retry/work commitments. Native response
completion uses Completing, not a prematurely terminal Done state.

Target mutation on a refused effect faults ordinary Update and prevents checkpointing.
This is a target guard, not proof against every possible cross-entity/service provider
mutation; broader fault matrices remain open. Checkpoint guards cover completed progress,
dependency ancestry/job lifetime, graph completion, state/effect facts and authored retry
bounds. Existing resource/logistics codecs and physical transaction owners are unchanged.

The lifecycle skill guided stage-first ownership/design, narrow implementation and evidence
updates. Repo/user instructions keep full regression pending; only the two names ran.
