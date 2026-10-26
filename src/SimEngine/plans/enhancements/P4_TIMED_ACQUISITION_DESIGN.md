# P4-G66: timed and mixed all/any acquisition

## 0. Problem statement

WorkAcquisition.PrepareTimedStarts currently puts freshly evaluated proposal choices into WorkRequirementContext. RequirementAlternatives correctly treats context choices as binding, so timed nodes cannot reconsider individually feasible ANY branches during complete frontier admission. Staffed/timed frontiers must share the original WorkBoard owner, not have a timed special scheduler.

## 1. Goals

1. Two timed nodes or one staffed plus one timed node select two jointly feasible fallback ALL branches instead of sequential expensive branches.
2. Target material and held tools acquire as one original native set, and all acquired branch facts stay fixed.
3. A contending timed task commits no branch/claim/progress prefix; pending cancellation survives save/load without altering the active set.
4. Actual completion releases held tools; acquired cancellation never refunds earned material or invents completion. Saved twins, sole-clock pause, private detached views, replay and idle-session isolation agree.
5. Native/direct metadata, minute/half clocks and completion/cancellation cross sixteen pure public-player flows.

## 2. Design

Change the original PrepareTimedStarts context to carry node.Choices, which is actual authority, instead of result.Choices, which is a fresh scalar proposal. Original RequirementAlternatives/EvaluateRequirement/CheckWorkSet performs pure branch/source/role/dock search; original PrepareNativeAdmission and AcquireResources retain fresh whole-set validation and atomic resource ownership. BindTimedStarts saves selected actual results. No new evaluator, scheduler, reservation store, clock or codec.

One master-listed timed-acquisition.json declares expensive consume2/hold2 and fallback consume1/hold1 ALL branches over target supply4. Authored graphs have parallel alpha/beta and dependent timed join. Native or independent group/resource/staffed/timed executor definitions use existing declaration algorithms. Existing metadata-only RequirementAcquisitionModule additionally registers the existing IndependentTimedExecutorDef. Drivers only author JSON or submit ordinary public inputs/observe immutable views/advance Update/clock/save/replay. One actual native machine atpoint4 travels to source0 in mixed flows; only one worker avoids overlap shortcuts.

## 3. API surface

No public API/payload change. Engine build2.438.0, save154/replay1 unchanged. Actual timed branch choices remain original graph/node codec fields.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | src/Beep.SimEngine/Standard/WorkAcquisition.cs | moderate |
| Modify | tests/ExtensionProof/RequirementAcquisitionProof.cs | low |
| New | tests/ExtensionProof/Content/timed-acquisition.json | low |
| New | tests/Beep.SimEngine.Tests/TimedAcquisitionTests.cs | low |
| Modify | build identity/test registration/resource listing/tracker/audit | low |

## 5. Error handling

Existing typed shortage/refusal and first-fault observation remain. A proposal never creates commitments. Actual node choices/receipts/spending prevent branch reselection; no absent source, worker or tool is fabricated.

## 6. Compatibility

Development-only; no legacy/compatibility path, migration shim or duplicate owner.

## 7. Verification

ONE NEW exact case, one solution compile then relevant same-case repair compiles only. Runtime60000ms/termination5000ms/output30000characters; physical stages512 Updates with bounded public diagnostics. No historical/family/full runtime. Freeze selected executable inputs after FINAL; documentation-only closure. Existing221 requirement words/checkboxes unchanged unless additional matching original scope actually proven.

## 8. Risks

Proposal and commitment must not be conflated. Timed progress may begin before a travelling worker arrives, but actual overlap and dependency completion must be earned. Arbitrary providers/search allocation/paid costs and broader lifecycle/fault/stress matrices remain open.

## 9. Out of scope

Oil, Draw/platforms/adapters, protected G19/Tycoon/clock, new transport media and full regression. Commit/push only after full goal AND plan finish.

## 10. Execution order

1. Correct the sole timed proposal context; add explicit authored graph and public player proof.
2. Compile then execute one exact case, repairing only that completed slice if necessary.
3. Record all attempts and remaining scope; update tracker and integrity anchors without claiming full completion.


## 11. Scoped result

Engine2.438.0/save154/replay1; FINAL953passed/0failed/0aborted in2183ms, PID32716 Exited0/process2322ms, untruncated/no observer error. Initial solution compile failed four new-fixture CS7036 calls missing explicit mixed flag; two targeted repair compiles clean4.12s/3.72s. First new-case32/1/0 in458ms exposed a driver-loaded twin not explicitly paused; actual public Pause corrected, no clock/save alteration. FINAL passes sixteen authored public-player flows. Both attempts retained; no executable edits/runtime after FINAL. Original221/177checked/44open unchanged, additional160. [Evidence](../../docs/migration/P4_TIMED_ACQUISITION_VERIFICATION.md). Full plan/goal/publication remain OPEN.

## 12. Metadata closure

Read-only closure:1404 source/13 quickstart anchors and699 selected executable inputs frozen at2026-10-11 13:28:57 UTC all match, zero drift. All221 original literal requirements/checkboxes unchanged,177checked/44open;204 line anchors moved six lines.2656 paths/928 Markdown/8336 local links, broken0/thin-driver boundary0;18 valid master rows. git diff --check exit0 with34 existing line-ending warning lines; dirty2644/staged0. Four protected clock/Tycoon hashes unchanged. Six existing executable inputs changed and two new inputs added. Documentation-only closure, no executable edits/runtime/publication after FINAL; full goal/plan incomplete.
