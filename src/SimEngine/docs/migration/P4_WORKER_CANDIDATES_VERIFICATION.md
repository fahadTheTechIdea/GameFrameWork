# P4-G2b1 — Worker candidates and committed alternatives

Date: 2026-10-05. Working-tree evidence, not committed-HEAD or full-regression completion.
Engine **2.47.0 / save22**, WorkBoard schema8, WorkView feature schema8.
No old singular activity-node role, flat requirement array or old save reader.
Only standalone SimEngine source/content/tests/docs changed. No oil, Tycoon, host/demo, Draw or platform work.

## Final verification

`dotnet build Beep.SimEngine.slnx --no-restore`: zero warnings/errors, final build1.96s.
Runtime, public non-friend ExtensionProof, testing library and test project compile together.

Exactly two directly affected suites ran, including corrections:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Worker candidates: authored roles, scoped offers and pure private selection'
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Worker candidates: committed alternatives, physical reselection and saved ownership'
```

| Exact suite | Passed | Failed | Final runtime |
|---|---:|---:|---:|
| Authored roles, scoped offers and pure private selection | 34 | 0 | 183ms |
| Committed alternatives, physical reselection and saved ownership | 158 | 0 | 358ms |
| Total final-source checks | 192 | 0 | 541ms |

Neither final suite aborted. Counts are the final run, not sums of correction reruns. The physical gate
directly invokes the affected G1 physical method (parallel work, contention, supply, saved named jobs),
not every older graph/work suite. Other migrated test callers compile but were not broadly rerun.
No full regression, replay, stress, soak, performance, oil or platform gate ran.

## Sole ownership and breaking migration

- ActivityNodeDef/IActivityNodeDefinition.Roles replaces singular Role. Master-listed node JSON uses
  `roles` arrays; empty means timed work. ActivityGraph captures a canonical immutable set and rejects
  invalid/duplicate identities. Generated Make callers explicitly pass a set, never an implicit role.
  Line/demolition retain their own authored role intent for generated plans; those values enter the same
  node/selector path, not a second activity assignment algorithm. Broader template migration is G4.
- IWorkerWorkRequirement is an optional worker-scoped leaf contract in the existing WorkRequirementDef
  family. Context.Worker is the actual candidate. With no candidate the engine reports pending, never
  calls a scoped provider with a fabricated worker or treats it as ready. Worker-scoped timed plans refuse.
  The public ExtensionProof introduces a rate predicate and a loader role/worker through ordinary JSON/
  module declarations, without modifying dispatch to recognize that content type or role.
- WorkSystem.CopyCandidates is the sole pure worker offer/assignment selector. CheckWorker centralizes
  owner/role/busy/held/operation eligibility for ordinary assignment, continuation and hauling eligibility.
  Real spatial contracts supply nullable route cost and reach checks. Offers disclose only owner machines,
  actual roles, costs, typed results and immutable branch paths. Ranking uses readiness, cost, ordinal
  role and stable entity identity. Offers are observations, never retained mutation authorization.
- WorkBoard's one iterative evaluator now derives WorkRequirementChoice occurrence/entry facts. All
  combines satisfied child choices; any chooses a stable entry. With committed choices it evaluates the
  recorded branch, not a newly preferred alternative. Invalid unselected branches still dominate.
  Missing/busy/unreachable candidate aggregate facts and per-entry diagnostics use the same evaluation.
- Actual assignment sets Job.Worker/Role and IWorker.Job together with the existing node's Choices.
  No worker ID is mirrored onto the node. Job.Role is actual selected role, distinct from authored allowed
  Roles. Cancellation/reopen releases choice/claim while retaining Progress and already spent inputs.
  Timed work commits its branch facts when starting and saves them without creating a machine job.
- WorkBoard.Sync embeds choices in each named node record. Restore validates exact reachable group paths/
  entries, no duplicate/extraneous commitments, role membership and surviving worker ownership/capability.
  Missing participants are handled by normal subsequent Update, never silently replaced during load.
- WorkView publishes detached actual worker/role from the existing job, committed node choices, and pure
  proposed/current offers separately. Immutable old views retain old choice facts after reselection;
  private refusal clears destination buffers. No client reproduces worker selection or branch rules.

## Focused runtime proof

Admission covers alternate role references, canonical frozen sets, external scoped predicates, actual
rate quantities/costs, privacy, held/busy/unavailable facts, pure hash invariance and deterministic equal
cost ties. Old singular roles, duplicates, unknown roles, null generated role sets and scoped timed
fallbacks refuse explicitly. Proposed offers never claim a worker or spend resources.

Physical proof starts a real loader job with the same offer selector, commits a nested any occurrence,
spends actual materials/payment once, travels and works over engine time. Disabling its preferred branch
pauses despite another satisfied branch; active diagnostics report the same blocker. Saved twins retain
the actual claim/role/path. Player MoveVehicle interruption explicitly releases the selection and
reselects a physical machine/branch without resetting progress or charging/consuming again. Loss of the
selected participant survives save/load and reselects deterministically on Update. Cancellation leaves
no orphan job/choice. Unknown saved entries refuse rather than selecting a different branch.

Timed proof commits its originally satisfied branch with no job, retains it when a newly preferred
alternative becomes ready, pauses when the committed branch blocks, and resumes saved twins identically.
Every waiting stage is bounded to 500 ticks with order/node/job/progress/blocker diagnostics on failure;
that is a test bound, not a framework world/time/content limit.

An initial compile corrected the test's use of entity ID instead of Entity for Despawn. The first physical
run aborted because the migrated JSON formatter removed line breaks used by the older declaration-order
fixture probe. The authored DAG fixture now preserves those node lines; no engine workaround or skipped
assertion was introduced. Final-source counts above follow the corrections.

## Remaining scope and review

This closes G2b1 worker offers/claims and durable branch occurrence facts, not all G2b or G2c. Typed
tool/source/target/area offers and multi-provider ownership, all-or-none stock/tool acquisition,
continuous inputs, generic executors, templates, prior facts, oil integration and broad-stage gates remain
required. Existing node Needs/Cash still follow their current supply semantics; this proof does not claim
those are the future atomic multi-provider transaction. No new reservation ledger or selector authority.

The lifecycle skill guided contract-first authoring/API migration and shared selection/codec ownership;
repository/user exact-suite policy overrides its generic full-regression suggestion. SRP/DIP review keeps
topology in DependencyGraph, expressions in WorkBoard, dispatch in WorkSystem and state with current
node/job/providers. External scoped requirements extend through contracts/content, not scheduler edits.
