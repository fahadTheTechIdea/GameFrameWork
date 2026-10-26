# P5-ST1 — Canonical supplied survey timing

Completed focused slice2026-10-16 on unchanged2.639.0/save160/replay1. Existing case
now retains full36h work; first/final4903/0 in71758ms. No engine/gameplay repair.
[Results and limits](../../docs/migration/P5_CANONICAL_SURVEY_TIMING_VERIFICATION.md).
Original221/210 recorded checked/11 open; P5.005/full goal stay open.

## 0. Problem and specification

Original P5.acceptance.005 requires preserved physical workflows. Existing actual-consumer
ActivityContractTests.Survey replaces the survey_2d_work shoot node's 36 hours with six
minutes and caps the whole workflow at 240 minutes. Remove this test-only shortcut and
verify the same actual engine path, not another survey executor or fixture scenario.

## 1. Goals

1. Keep work.json unchanged: actual 36h /2160-minute goal and ten survey charges.
2. Observe loaded carrier movement and the real seismic worker's work; no injected stock,
   movement, progress, knowledge or completion.
3. Save posted, partial and final-incomplete work; 2159 productive minutes must not complete.
   The next original minute must complete the same order/stake through native history.
4. Verify finite stock, retired stake, private completion, saved continuation, stable history
   and isolation of an untouched sibling session.
5. Reuse the existing literal case and shared stock query; no duplicate runtime or driver.

## 2. Design

Edit actual Oil consumer OilCo.PublicProof/ActivityContractTests.cs only. Remove its
oil/work.json projection; retain the original 24x24 two-seat Sandbox, seed17, real starting
seismic/haul trucks, native speeds/rates, stocks, terrain and one-minute frame authoring.
The resource-only profile remains independent of money. The original generic WorkBoard,
hauling, motion, clock, company knowledge and Oil survey effect own all gameplay.

Reuse Step/Send, public WorkView and existing HeadquartersRecoveryTests.Total stock
aggregation; do not add another inventory implementation. The driver chooses a legal
click through ordinary Preview, advances Update and asserts detached facts. Keep actual
2160-minute node diagnostics, productive time and selected seismic worker visible.

Replace the short whole-workflow limit with 240 pre-work/handling minutes plus the
unchanged 2160-minute goal: 2400 native updates total. This reflects newly unshortened
work, not an extended failed deadline. Declare a120s inner watchdog and180s external
child deadline before the first attempt; retain native diagnostic snapshots on failure.
Keep periodic12-update twin checks; reduce repetitive progress output during long
productive work without removing checks. Save at progress2159, then check the next
minute's original completion history instead of querying a pruned execution. Fact
timestamps identify the executing interval; the one clock publishes its end afterward.

## 3. API surface

No production contract, content, codec, clock, business rule or version change planned.

## 4. Files

| Action | File | Risk |
|---|---|---|
| Modify | Actual consumer OilCo.PublicProof/ActivityContractTests.cs | Focused test only |
| Add | This design and docs/migration/P5_CANONICAL_SURVEY_TIMING_VERIFICATION.md | Scoped evidence |
| Modify | MASTER_TRACKER.md, P5_OILFIELD_EXTENSION_DESIGN.md, AGENTS.md, README.md | Accurate status |

## 5. Error handling

On stall/fault print actual work/jobs/blockers/cargo. Do not shorten work, disable wear,
relax resource gates, teleport machines, grant stock or enlarge failed deadlines. Repair
only demonstrated defects at their original responsible owner, preserving every attempt.

## 6. Compatibility

Development architecture only; no legacy short-work companion or compatibility path.

## 7. Verification

Targeted Release build of OilCo.PublicProof and existing literal case:
`Oil activity contracts: detached survey order physical supplies saved progress and completion`.
--bounded-exact; timeout-ms180000; termination-timeout-ms5000; output-chars16000.
No profile/family/full test run. Check source/artifact/content identity and document hygiene.

## 8. Risks

Real36h work may expose an actual incident, route or accounting defect. Treat that as
evidence, not a reason to replace physical behavior. This reduced map/two-machine proof
does not establish marine/large-map/all-profile or full-stage replay/stress acceptance.

## 9. Out of scope

New workflows/business rules, canonical JSON changes, platform/Draw/adapters, gameplay in
clients, broad tests or publication before the full goal and plan finish.

## 10. Execution order

1. [x] Remove duration projection and strengthen existing public assertions/bounds in place.
2. [x] Compile the target; run only the single case and poll its actual handle to terminal.
3. [x] Diagnose any observed defect without weakening timing/physical rules; retain all attempts.
   One clean build/one successful run; no runtime defect or repair required.
4. [x] Record results and original remaining scope; full goal stays active.
