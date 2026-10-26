# P7-ER3: Provider-owned plan references

## 0. Problem statement

WorkBoard owns persistent JSON plan intent. Before ER3, replacement changed worker homes only and pending targets/selected machines retained retired identities. The existing provider-owned schema migration now resolves those references. Generic JSON-number rewriting remains invalid because it would corrupt coordinates, quantities and provider-owned schemas.

Current M69, unchanged core2.723/save161/replay2: both existing plan literals pass1616/0 with five actual replay verifications, no executable edits or rebuild. Pending native and independent-schema plans survive physical replacement and execute the material-supplied sequential/parallel DAG; active execution/acquired claims still refuse another replacement. Terminal history remains factual. [Current finite-owner evidence](../../docs/migration/P7_ENTITY_REFERENCE_REBINDING_VERIFICATION.md#current-m69-finite-native-reference-owner-qualification--2026-10-22). The earlier [M53 implementation record](../../docs/migration/P7_PLAN_REFERENCE_REBINDING_VERIFICATION.md#current-er3b-m53-supplied-pending-plan-lifecycle) retains its own actual image and limits.

## 1. Goals

1. Every IPlanDefinition explicitly declares its live entity references and owns admission/rewrite of its argument schema.
2. Compatible pending plans survive physical target or selected-worker replacement without recreating plans, losing dependencies/priority/pause/age or granting work.
3. Active executions and recurring provenance that cannot yet be safely migrated refuse replacement before mutation. Historical terminal facts remain factual.
4. Public commands/views, saved pending twins and ordinary timed plan execution prove continuation. Only focused cases run.

## 2. Design

Extend IPlanDefinition with ReadEntityReferences, CheckEntityReplacement and ReplaceEntityReference. Native/direct activity/removal providers delegate target/worker parsing and formatting to the existing ActivityWorkerSelection; construction/line declarations explicitly expose no entity references in their placement/path schema. No optional compatibility fallback.

WorkBoard's existing reference-owner callback validates pending plans, refuses acquired/active execution and source-owned recurring intent, validates projected identities against duplicate live intent, stages provider-produced frozen arguments, and commits them to the original PlanEntry objects. Existing graph/work/resource/clock owners execute plans afterwards. Preserve terminal arguments/targets and journals.

Provider reads/checks/rewrites run under existing observation. The returned reference set must exactly replace the requested identity, retain all others, and remain valid/unique. Actual destination submission is checked before changing plan records. No reflection, integer scanning, second board, new reference store or copied worker state.

## 3. API surface

IPlanDefinition requires three pure schema operations. Existing entity replacement command and public plan APIs remain. Initial implementation was build2.560.0/save156/replay1. ER3b uses unchanged current core2.713.0/save161/replay2; it requires no additional engine API or saved state.

## 4. Files to change

PlanContracts.cs, ActivityWorkerSelection.cs, Work.cs and its reference-owner partial; existing construction/line declarations and their native/direct registrations; focused public plan reference proof; protocol/tracker/evidence.

## 5. Error handling

Incompatible target/worker metadata, active executions, recurring source provenance and projected duplicate intent return typed refusals. Malformed/mutating provider callbacks or incorrect rewrites fault the original observation/replacement boundary. No healthy partial save or implicit unbound-worker substitution.

## 6. Compatibility

Development-only contract migration. No old API bridge, legacy JSON patcher or alternate scheduler.

## 7. Verification

New exact plan-reference case and affected logistics/reference case as necessary; solution build. No full regression. Test target and selected-worker replacement, refusals, unchanged JSON numeric fields, dependencies, pause/priority/age, saved twins, private facts and actual timed completion.

## 8. Risks

Provider schemas differ: each declaration is responsible for its references. Active work receipts and recurring cycle schedules are not rewritten here and must refuse rather than silently corrupt. Dedicated broad provider/fault/replay/performance gates remain required.

## 9. Out of scope

Platform/rendering/host gameplay, Oil/domain/business rules, full tests and Git writes. Full goal and all original plan requirements remain unchanged.

## 10. Execution order

1. Migrate plan contracts and existing schema owners.
2. Implement original WorkBoard admission/staged rebinding.
3. Exercise pending physical workflows and incompatible/active cases through public APIs.
4. Record focused evidence and update master audit; leave broader gates open.

Status: implemented and scoped verified. New plan187 plus affected logistics185/reference96/construction admission174:642/0 final focused checks. Solution2.17s and Oil consumer4.38s clean; no Oil runtime/source changes. Private projection now observes each provider's declared argument references before publishing. The older removal fixture now explicitly binds resource-only startup/navigation/placement/conditions to its actual recovery space and drops an unused seller; physical work requires no Tycoon. Original221/194recorded checked/27open unchanged. See [evidence](../../docs/migration/P7_PLAN_REFERENCE_REBINDING_VERIFICATION.md).

The preceding counts/builds are historical ER3 evidence, not current qualification. Current ER3b stages:

1. Author finite donor20 and empty original/replacement sites in the existing JSON factory. Keep haul/inspect roles separate and author prepare → parallel check/calibrate → finish, with check consuming two physically supplied units.
2. Save pending paused intent through a real staffed replacement; preserve provider-owned numeric non-reference data and all original plan identity/policy fields.
3. Activate through the normal plan command. The existing WorkBoard creates one demand keyed by the actual active order ID, targeting the new site. Save material wait and verify original active-execution refusal without changing demand age.
4. Save a physically loaded haul and partial staffed work. Preserve exact job/resource/progress bindings, prove original inbound-claim refusal, and finish with cumulative consumed2 despite eviction from the original two-record journal window.
5. Separately cancel a waiting active plan through the public command. Verify withdrawal of owned demand/jobs with zero consumption, then permit a later staffed replacement. Both terminal outcomes retain factual historical arguments.
6. Current two literal cases1616/0, five actual replay verifications; profile metadata enrolled but not executed. No engine/proof-provider change, widened limit, original221 gate closure or full-goal completion. Broader processor/WIP/incident/disposition and recurring provenance remain separate existing-owner work; acquired workflows must not be silently hot-swapped.
