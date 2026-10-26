# P8-RP1 return execution preview — Design Document

## 0. Problem Statement

Views/TransformationExecutionPreview.cs CurrentBlocker prioritizes disabled production and production gates over Current.Blocker even when Current.Phase is Returning. Native cancellation returns WIP before those production gates in Standard/Transformations.cs AdvanceTransformation. ProjectExecution similarly classifies return work through production/continuous status, obscuring the actual cleanup phase.

## 1. Goals

1. Returning work has an explicit ProductiveEstimateStatus.Returning and no productive completion/time estimate.
2. Returning CurrentBlocker is its actual recorded return blocker, not a substituted disabled/production-gate cause.
3. Retain ordinary working/disabled/not-head/continuous preview semantics, immutable private facts and one actual execution owner.
4. Prove actual native/point minute/half work, disabled original return loss, saved/pause/restoration and replay without preview mutation.

## 2. Design

Extend the existing ProductiveEstimateStatus enum with Returning. ProjectExecution classifies an actual head in Returning before production mode/control/gates. CurrentBlocker uses Current.Blocker for Returning before production control/gates. Keep Gates as the detached production-gate observation and ClockPaused as the actual clock fact; neither invents a cleanup ETA. Nominal authored cycle remains distinct from actual productive completion.

No new lifecycle state, clock, scheduler or stored preview. Existing canonical Transformer.Phase/Blocker and WIP/tool/return protocol remain authoritative.

## 3. API surface

Same ITransformationExecutionView and immutable TransformationExecutionPreview. New explicit status; transformation view19 advances to20. Save154/replay1 state unchanged.

## 4. Files to change

TransformationExecutionPreview.cs, TransformationManagementView.cs, TransformationModule.cs, Kernel/SaveGame.cs and protocol fixture; one new public-driver case/registration and tracker/evidence/hash anchors. Reuse actual master-listed restoration/manufacturing JSON; no duplicate authored fixture.

## 5. Error handling matrix

| Phase | Preview |
| --- | --- |
| Actual working head with current positive rate/gates | Existing conditional productive estimate |
| Disabled production before cancellation | Existing disabled blocker/no productive ETA |
| Returning original inaccessible input | Returning / exact original blocker / no productive ETA |
| Returning with paused clock | Returning plus actual ClockPaused; no fabricated return duration |
| No head after original return | NotHead; no current blocker or completion estimate |
| Foreign company | Existing refusal, no private facts |

## 6. Backward compatibility

Development only; change the canonical projection, no second preview or compatibility branch.

## 7. Verification

One solution compile and necessary focused repairs only; ONE NEW exact bounded public-player case. Four native/point minute/half actual physical feed/work/disabled cancellation/return workflows, repeated pure/private previews, saved twins, clock pause, restoration, frozen facts, actual stock/output accounting and replay/isolation. No older/family/full runtime.

## 8. Risks

Returning is not productive manufacturing and has no universal cleanup ETA. Continuous-return transients, independent providers, hostile gate/descriptor reads, all monetary/tool/resource matrices and full preview gate remain open unless individually proved.

## 9. Out of scope

Oil, Draw/platform/adapters, Tycoon/protected G19/sole-clock implementation, broad regression and publication.

## 10. Sub-task execution order

1. Extend existing preview status and actual blocker precedence.
2. Prove the real disabled return path through public engine contracts.
3. Record scoped evidence without changing original gate text or status.

## 11. Status

Core2.483.0/save154/replay1/transformation-view20. Existing return projection reports explicit Returning/no productive ETA and actual recorded blocker before disabled production/gates. No duplicated state, scheduler or clock. ONE NEW FIRST/FINAL346/0/0 in11537ms;4 native/point minute/half actual physical working/disabled/original return loss/saved/pause/restored/private/frozen/replay/isolation workflows. Solution3.47s Windows resource error before compilation; focused reduced-concurrency final75.33s clean. No executable edit after FINAL. Original221/186checked/35open unchanged, additional210. Continuous/independent/gate-fault/all-phase/optional-money/full preview/full P8/P1-P12/publication remain OPEN. [Evidence](../../docs/migration/P8_RETURN_EXECUTION_PREVIEW_VERIFICATION.md).

Fresh read-only metadata closure:1715 source anchors,954 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2903 paths/1028 Markdown files/8669 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2891/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional210. Six existing selected executable inputs changed; one newly created executable driver/three new source files, four existing production sources changed/zero new. No previously existing input newly admitted. Transformation view20 records return status/blocker semantics. An initially slow read-only hash command outlasted its observer; authoritative process inspection found no live hash command, then the inventory collector was updated to resume yielded command handles and the full fresh freeze/closure completed. No runtime rerun for documentation validation.
