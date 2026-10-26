# P4-G2b1 — Actual worker candidates and durable alternatives

Status: focused-verified on 2026-10-05, **192 checks / two exact suites**, zero failures/aborts,
build2.47.0/save22/work8/view8. See [evidence](../../docs/migration/P4_WORKER_CANDIDATES_VERIFICATION.md).
Parent: P4_REQUIREMENT_EXPRESSIONS_DESIGN. This is the worker part of G2b,
not closure of source/tool/area candidates or G2c resource acquisition.

## Problem, goals and boundaries

The former single node Role and transient-only selection have been replaced by one immutable
authored Roles set and committed choices on the existing node. Evaluate worker-scoped leaf
contracts against actual company machines. The existing WorkSystem must own one pure candidate query
used by preview and assignment. WorkBoard's existing node owns committed branch identities; Job.Worker
remains the sole actual machine identity and IWorker.Job remains its exclusive claim. No second selector,
provider store, job loop or reservation book. No platform, oil, Tycoon or host gameplay changes.

Acceptance: master-listed JSON role alternatives; strict role references/duplicates; external scoped
requirements without scheduler edits; typed actual candidate cost/refusals; deterministic cost/role/entity
ties; stable committed alternatives through pause/save/load; explicit release/reselection on interrupt,
loss or invalidated worker eligibility; no double material/payment/progress. Private detached views.

## Contracts and runtime design

- IActivityNodeDefinition.Roles replaces Role, including ActivityNodeDef.Make. JSON nodes use roles arrays;
  no singular role reader/shim. Empty Roles means timed work. Generated line/demolition plans bind
  explicit immutable role sets through the same graph; their broader authored-template migration is G4.
- WorkRequirementContext gains the actual candidate Worker and committed Choices. IWorkerWorkRequirement
  marks worker-scoped leaves. Without a candidate they return an explicit engine pending blocker, not
  fabricated readiness. Timer plans with a worker-scoped leaf are invalid before Initialize.
- WorkRequirementChoice contains a group path and entry identity. WorkBoard's sole iterative evaluator
  collects satisfied all branches or one canonical any branch. With committed choices it checks the
  chosen branch, never silently swaps it; malformed/invalid unselected branches still fault.
- IWorkerDispatch.CopyCandidates produces immutable WorkWorkerCandidate facts. WorkSystem uses its
  existing CanAssign/CanWork and spatial contracts; facts include actual role, entity, nullable cost,
  typed eligibility/result and selected branch paths. Only the owning company's machines are included.
  The same method serves node previews and actual job assignment, including existing standalone jobs.
- ActivityNodeExecution stores only committed choice facts in its existing codec. The existing Job
  stores chosen role/worker; do not add a mirrored worker field on the node. Assignment commits choices
  with IWorker.Job/Job.Worker together; reopen/abort releases those choices but preserves real Progress.
  Timed work commits its choices when starting and retains them while paused.
- Saved choices validate exact group paths/entries and node/job role relationships. Runtime eligibility
  is rechecked after load by normal Update, not changed during reads or silently reassigned on restore.
  Completed nodes may retain branch facts; cancelled/failed nodes release live commitments.
- WorkView adds proposed/current candidate facts and immutable committed choices on ActivityNodeView.
  Reads use the same evaluator/selector; no actual worker/provider object leaves the view.

## Migration, failure and verification

Migrate current standalone node JSON and generated Make consumers once. Break build/save/work/view
identities together; old singular node roles and old saves refuse. Worker component roles, standalone
Job.Role and line/demolition's own role intent are not duplicate activity assignment algorithms.

No eligible worker remains a typed pending/blocked result; missing target/reference is invalid. Busy,
held, unusable, unreachable and scoped-leaf refusals remain distinguishable. Candidate reads cannot
reserve or spend. Actual all-or-none material/tool spending remains G2c; existing supply semantics are
not claimed to acquire the future multi-provider requirements.

Only two exact directly affected suites: scoped candidate/admission and committed physical execution.
Bound stages and emit node/job/progress/blocker on failure. Build all projects, no broad/full tests.
Update parent/master/README/AGENTS/evidence in the same slice. G2b tools/source/area offers, G2c,
G3–G5, oil integration, replay/stress and full-stage acceptance remain required.
