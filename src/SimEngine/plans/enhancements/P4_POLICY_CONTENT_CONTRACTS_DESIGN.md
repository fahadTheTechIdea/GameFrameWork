# P4-G9 — Open authored work, retry and dispatch policies

Status: scoped verified 2026-10-09, 99 focused checks; full P1–P12 remains open.

[Verification](../../docs/migration/P4_POLICY_CONTENT_CONTRACTS_VERIFICATION.md). Executable inputs frozen 2026-10-09 04:08:29 UTC.

## Specification and goals

The existing policy contracts control physical workers, but composition still requires WorkPolicyDef, WorkRetryProfileDef and DispatchProfileDef. Master-listed independent JSON definitions must work through the same contracts without inheriting those native classes.

1. Add IWorkPolicyDefinition and make retry/dispatch contracts actual content identities.
2. Extract the original JSON parsing and deterministic decisions into shared declarations; native and independent definitions use those same owners.
3. Resolve all profile consumers through contracts. Validate ownership, role/area/state references, retry timing and dispatch metadata before Initialize.
4. Retain WorkBoard, WorkSystem, policy commands, immutable views, physical execution and all existing state codecs. No parallel scheduling, stock, graph or time authority.
5. Prove direct JSON profiles through one bounded thin-player scenario: disabled work, physical travel, retry/recovery, material/tool accounting, saves, replay and early authoring refusals.

## Design and API

- Standard/WorkPolicies.cs: IWorkPolicyDefinition exposes immutable Initial and resolved States. WorkPolicyDeclaration owns the existing reader/resolver. WorkPolicyComponentDef owns its resolved content context, not the authored profile.
- Standard/WorkRetries.cs: IWorkRetryPolicy extends IContentDefinition. WorkRetryDeclaration owns explicit positive rational durations, multiplier, blocker set and capped delay.
- Standard/DispatchPolicies.cs: IWorkerDispatchPolicy extends IContentDefinition. DispatchPolicyDeclaration owns explicit ordering, scopes, threshold and emergency preemption willingness. Actual ranking and safety remain in WorkSystem. WorkView.TryReadDispatchPolicy exposes access-guarded detached selected profile metadata, never fabricated offers or provider handles.
- Standard/WorkModule.cs, Work.cs, WorkScheduling.cs, DispatchPolicyModule.cs and OperatingAreas.cs select/check actual contract aliases.
- ExtensionProof registers direct definitions from an explicitly master-listed file; the test driver submits commands and reads views only.

## Errors and compatibility

Missing fields/references, wrong contract kinds, duplicate roles/scopes/blockers, invalid ordering/durations and foreign state aliases refuse as content errors before Initialize. No fallback values, compatibility loader or legacy profile alias. Save layouts and the sole GameClock stay unchanged; build identity advances for changed composition semantics.

## Verification and risks

Build the canonical solution. Run only the single new exact case, bounded by the runner and per-workflow Update limits. Repairs may rerun that case only. Historical fixtures compile but are not revalidated; full regression remains open under the user's focused-test instruction. Check actual source boundaries and update tracker/evidence without changing original acceptance requirements.

## Out of scope

Oil consumer, Tycoon, income/budget, platforms, adapters, Draw, fleet/facility policy migrations, broad/runtime regression and publishing before full completion.

## Execution order

1. Shared declarations and contract consumers.
2. Independent JSON definitions and explicit module selection.
3. Bounded player-only proof and clean build.
4. Source review, evidence and master tracker reconciliation.
