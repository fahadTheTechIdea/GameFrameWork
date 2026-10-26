# P4-G2b dock admission requirements

## Ownership and scope

Add a master-listed `dock_work_requirements` leaf implementing `IWorkerWorkRequirement`.
It requires an actual target `IWorkDock`; ordinary target interaction is not a dock fallback.
The candidate role comes from the existing worker selector, not a second role lookup.
`IWorkDockAdmission` aliases the existing WorkBoard, which delegates its sole PreviewDock
selector. Existing jobs retain slot intents; resource receipts stay with actual inventory owners.
This adds no system, queue, reservation table, save payload, clock or platform logic.

## Stages

1. Extend the ephemeral requirement context with the selected worker role. Propagate the
   actual candidate role and live job role through existing evaluator paths.
2. Bind the dock admission contract to the same WorkBoard. Require actual owned live target/
   worker, validate current node/job identity and retain its slot to prevent self-contention.
3. Register the JSON leaf and compose it through existing all/any requirements. Existing
   roles validation, private views, job assignment and stock acquisition remain authoritative.
4. Prove native master/startup content, physical service/waiting travel, competing nodes,
   pure diagnostics, stock consumption, cancellation and saved continuation with ONE distinct
   exact NEW bounded player case. No economy, raw test-spawn or test-owned scheduler.
5. Freeze source/test/content after green; record builds/runs and scoped source inventory.

## Acceptance and exclusions

- [x] Actual target dock required; missing/full behavior and actual compatible role proof.
- [x] Same worker-role/slot selection in offers, retained execution and diagnostics.
- [x] Physical service queue and native stock ownership survive saved continuation.
- [x] Focused build/runtime evidence and tracker update.

Verified2026-10-07: build2.235.0/save136/existing layouts unchanged. FOUR builds (first1 CS0534,
then three clean); THREE SAME exact NEW case runs (42pass/3 failures including1 abort,
61pass/1 failure, final62pass/0 failures/aborts499ms). 101 older identity lines compile ONLY.
Source/test/JSON frozen after green. [Evidence](../../docs/migration/P4_AUTHORED_DOCK_REQUIREMENTS_VERIFICATION.md).

No claim that remote/entity tools, multi-worker crews, inbound batch acquisition, arbitrary
provider mutation safety, all contention matrices or full P4/P7/P9 acceptance are complete.
Build identity must change; unchanged persistent layouts retain save136. Older identity
assertions migrate/compile only. No older/family/full suites or publication for this slice.
