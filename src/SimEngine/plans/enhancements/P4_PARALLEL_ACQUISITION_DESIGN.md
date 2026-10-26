# P4-G65: parallel all/any acquisition acceptance

## 0. Problem statement

Original P4.acceptance.007 is now covered by matching scoped evidence: isolated resource alternatives and individual held-tool workflows were not proof that simultaneous role/tool/material obligations choose and acquire a jointly feasible set. The actual focused probe exposed sequential individually preferred branches. Extend the original WorkBoard.CheckWorkSet admission and evaluator without a new scheduler, ledger or test-side reservation algorithm.

## 1. Goals

1. Two concurrent staffed nodes choose a feasible pair of all/any branches over one scarce target buffer, actual worker tools and compatible roles.
2. Joint selection prefers productive parallel coverage over individually feasible expensive branches that cannot coexist.
3. A third contending task acquires no worker, material or tool prefix while stock is fully promised.
4. Cancel/save/pause preserve original held commitments; after release a later task physically executes with no resource duplication.
5. Native/direct metadata and minute/half-minute clocks agree through public commands/views/save/replay and private detached facts.

## 2. Design

Author requirement-acquisition.json as one explicitly master-listed module document. Reuse existing IndependentWorkerRoleDef, IndependentStaffedExecutorDef, IndependentRequirementGroupDef, IndependentTargetResourceDef and IndependentWorkerGuardDef; a metadata-only proof module registers them, not another executor, state store or decision algorithm.

Each branch contains the same selected-role guard and actual worker-held wrench. Preferred branch consumes two supply and holds two target supply; fallback consumes one and holds one. Two simultaneous nodes share four real supply, making two fallback sets jointly feasible while either preferred branch excludes the other. Native definitions use the same declaration algorithms. A third ordinary task is denied actual available stock while both target tools are held; cancellation must not alter either working commitment.

All physical requirements, roles, compatibility, selection, claims, consumption, progress and completion are engine-owned. Three machines start with explicit authored reusable tools. Two service bays at49/51 around a source at50, departure slots and left/right operating areas make noncrossing travel explicit in the single-lane proof geometry. Machines move, perform elapsed work and depart; no overlap bypass. The driver uses only player commands/results and immutable views, one Update/clock, saves/replay; no TestKit or direct engine mutation.

The same WorkBoard now lazily enumerates authored ANY occurrence choices for unacquired nodes and reuses EvaluateRequirement plus original prefix dock/resource/budget admission. Distinct workers and role alternatives still belong to existing matching; selected resource sources still belong to CheckResourceSet. Already-bound choices/receipts/spending/completion are not reselected. Working alone is not acquisition: native jobs enter that state before a machine acquires them. Search is iterative with transient proposals, no persisted cursor, arbitrary cap or speculative claims. Actual assignment freshly rechecks the selected branch before the existing atomic acquisition.

## 3. API surface

Engine2.437.0; save154/replay1 and public API/payloads unchanged. Only build identity changes. New proof module registers metadata contracts only; WorkBoard remains the sole admission/evaluation/acquisition owner.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| New | tests/ExtensionProof/RequirementAcquisitionProof.cs | low |
| New | tests/ExtensionProof/Content/requirement-acquisition.json | low |
| New | tests/Beep.SimEngine.Tests/ParallelAcquisitionTests.cs | low |
| New partial of original owner | src/Beep.SimEngine/Standard/WorkRequirementAlternatives.cs | moderate |
| Modify original owner | src/Beep.SimEngine/Standard/WorkStart.cs | moderate |
| Modify | original test registration/resource listing/tracker/audit | low |

## 5. Error handling

Content ownership and dependency validation precede Initialize. A blocked actual node carries the existing typed blocker and no committed resources/choices/worker; previews cannot reserve. Invalid or missing references never manufacture compatibility or readiness.

## 6. Compatibility

Development-only; no legacy/compatibility path or copied acquisition model.

## 7. Verification

One solution compile then ONE NEW exact case, same-case repairs only. Runtime outer60000ms/termination5000ms/output30000characters; each physical stage bounded512 Updates with public node diagnostics. No old/family/full runtime. Freeze inputs after final passing attempt; documentation-only closure.

## 8. Risks

Assert real committed branches/receipts, not projected individually feasible offers. Keep source/worker native units explicit. Global performance, arbitrary callback faults and lifecycle matrices are not proven by this finite contention scenario.

## 9. Out of scope

Protected G19/Tycoon, Oil, Draw/platform/adapters, population/combat, new transport media and full regression. No staging/commit/push before full goal and plan finish.

## 10. Execution order

1. Author file/master/module registrations and ordinary public-input proof.
2. Compile and execute one exact case; repair actual defects in their sole responsible owners if found.
3. Update original acceptance status only if complete matching evidence exists; record all attempts and remaining broader scope.

## 11. Scoped result

FINAL249passed/0failed/0aborted in3019ms; PID45064 Exited0/process3157ms, untruncated/no observer error. Initial solution compile failed a new fixture CS0819; all nine targeted repair compiles clean, last3.87s. Nine same-new-case attempts retained, including the actual engine defect and explicit single-lane fixture corrections. No executable edits/runtime after FINAL. P4.007 covered; original221/177checked/44open, additional159. Full goal/plan/publication and broad provider-fault/lifecycle/performance gates remain OPEN. [Evidence](../../docs/migration/P4_PARALLEL_ACQUISITION_VERIFICATION.md).

## 12. Metadata closure

Read-only closure:1400 source/13 quickstart anchors and697 selected executable inputs frozen at2026-10-11 13:19:47 UTC all match, zero drift. All221 original literal requirements verified; only P4.acceptance.007 checkbox closes,177checked/44open,204 line anchors moved six lines.2652 paths/926 Markdown/8304 local links, broken0/thin-driver boundary0;18 valid master rows. git diff --check exit0 with34 existing line-ending warning lines; dirty2640/staged0. Four protected clock/Tycoon hashes unchanged. Five existing executable inputs changed and four new inputs added. No executable edits/runtime/publication after FINAL; full goal/plan remain incomplete.
