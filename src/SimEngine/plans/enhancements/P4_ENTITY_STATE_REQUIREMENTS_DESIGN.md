# P4-G2b authored entity-state requirements

## Problem and goals

The activity plan explicitly requires site-state and worker prerequisites. Existing entity state
is committed once by IEntity.Update after systems/events, but no standard requirement leaf
can reference that state. Add one common state guard, target/worker specializations and strict
master-listed JSON, using the same WorkRequirementDef graph/evaluator/private diagnostics.

## Stages and ownership

1. `EntityStateWorkRequirementDef` resolves explicit nonempty `states` references to existing
   EntityStateDef instances and one authored `blocker`. Reject unknown/duplicate/ambiguous
   published states and extra fields. Publish immutable state references, not runtime copies.
2. Target/worker subclasses choose the actual entity only. The worker class implements existing
   IWorkerWorkRequirement. Check actual session/live identity/company, then read IEntity.State.
   A matching state satisfies; mismatch blocks with authored code; absent actor remains pending.
3. WorkModule registers target_state_requirements/worker_state_requirements. No scheduler,
   state setter/predictor, command, registry, service, codec, clock or view implementation added.
4. ONE distinct exact NEW bounded native JSON/player case proves state transitions, all/any
   diagnostics, worker offers, pre-acquisition waits, physical travel/material/work, pause/
   resume/cancel and saved twins. Run a completed-slice build and only that new case.
5. Freeze source/test/JSON after green, then update tracker/evidence/scoped source inventory.

## Temporal semantics and limitations

Entity.State is the canonical last committed state. Work observes it at the existing system
order; command-controlled capability changes settle in Entity.Update and affect subsequent
work evaluations. No same-update recursive entity refresh or alternate state machine. An
author must not require a state that the guarded work itself prevents (e.g. paused worker
must be idle to resume); this is an authored feedback dependency, not an automatic fallback.
State guards are workflow predicates, not immediate safety isolation; operation/network safety
continues using actual current capability gates. State precedence remains existing authored
EntityStateDef priority/tie rules.

- [x] Strict contract/JSON state guards through the existing evaluator.
- [x] Native physical pause/resume/stock/saved workflow and pure/private diagnostics.
- [x] Focused verification and docs/source freeze.

Verified2026-10-07: build2.236.0/save136/existing layouts unchanged; ONE clean build5.02s,
ONE exact NEW case74 checks0 failures/aborts375ms, first attempt.102 older identity lines
compile ONLY. [Evidence](../../docs/migration/P4_ENTITY_STATE_REQUIREMENTS_VERIFICATION.md).

No compatibility reader or hidden state default. Build changes; unchanged owner layouts keep
save136. Older identities compile only. No older/family/full/platform/Tycoon gate. Entire
P1–P12 plan remains active; this does not complete prior-activity facts, generic executors,
tool teams or universal feedback/safety/other-provider acceptance. No per-slice commit/push.
