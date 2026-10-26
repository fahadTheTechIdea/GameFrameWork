# P8-EY1: Complete transformation yield previews

## 0. Problem statement

`TransformationExecutionPreview` reports resource yields but omits the recipe's authored entity output. `Current.EntityOutput` describes the active head, not necessarily the requested recipe. Empty queues therefore lose machine/building yield information, requiring another query and host interpretation.

## 1. Goals

1. Expose an immutable authored entity output ID in the existing execution preview; never expose a live definition or entity.
2. Keep disclosure, provider observation, productive estimates and gameplay authority in the existing engine owners.
3. Verify complete previews against physical batch/continuous execution, private discovery, return states, retained projections, saved twins, replay and guarded callbacks using one new bounded exact case.

## 2. Design

Add `string EntityOutput` to `Views/TransformationExecutionPreview.cs`, copied from the already-authorized recipe inside `TransformationManagementView.ReadRecipe`'s existing observation. No output creates an explicit null; resource yields remain the original immutable bills/rates. The ID denotes one entity per successful batch conversion, not a placed or completed result. Existing `PreviewEntityOutput` remains responsible for placement eligibility, not a duplicate yield calculator.

Increment the existing transformation view schema to22 and build identity to2.501.0. Save154/replay1 remain unchanged because no authoritative state is added.

## 3. API surface

`TransformationExecutionPreview.EntityOutput`: detached nullable authored content ID. All existing ownership, reference visibility and provider-fault refusals precede publication. No host, rendering or platform API.

## 4. Files to change

- Existing preview DTO, transformation module view schema and engine protocol fixture.
- One new public-player preview acceptance driver and its explicit profile/registration.
- Reuse existing physical helpers without invoking older registered cases; helper visibility changes only where necessary.
- Tracker and this stage's evidence.

## 5. Error handling

Hidden/missing/unsupported references retain existing refusal codes and publish no facts. Throwing or state/RNG-mutating providers retain the original first observation fault and no partial out value. Range-limited productive work reports existing OutOfRange rather than inventing completion time.

## 6. Compatibility

Development only; no compatibility or alternate preview model.

## 7. Verification

One solution build, then one new bounded exact case. Tests drive normal commands, Update, sole clock and immutable views; no World escape, resource/progress mutation or test-side recipe execution. Metadata profile-info executes no suites. Authorized final full regression remains deferred until implementation is finished.

## 8. Risks

Yield identity is not placement feasibility or delivered throughput. Keep those meanings explicit. Reused helper evidence covers only actual executed flows, not historical passing counts.

## 9. Out of scope

No second queue, result calculator, scheduler, timer, Tycoon rules, Oil files, Draw/platform code, legacy fallback, staging or publication. Cancellation/accounting/global acceptance are not inferred from preview evidence.

## 10. Execution order

1. Extend original DTO/schema/build.
2. Add focused public-only yield/estimate/privacy/observation proof.
3. Build and run the new exact case; retain any failures.
4. Update tracker and evidence with actual outcomes and unclosed scope.

## Delivery outcome

Original P8.acceptance.015 alone closed after current NEW FINAL1223/0/0 in13465ms. FIRST1176/12 unrelated-port fixture failures and intermediate1200/0/0 retained; resource-yield/rate coefficient/period assertions added before final proof. One solution10.43s/focused repairs4.94s and8.46s clean. Original221/190checked/31open,additional232; no full regression/goal completion/publication inferred. Metadata-only source/input/quick anchors1800/1009/13 match, all221 original gates validated and8763 local links resolve. No executable edits after FINAL.
