# P8-PC1 — Stop production dispatch after provider faults

Current M87/native2.728/save161/replay2 also observes whole-entity pure transformation reads and
admission through existing contracts/observer. Original dispatch cutoff passes482/0, unchanged;
new same-entity negative880/0, physical manufacturing1012/0 and observer default398/0 are scoped.
[All attempts, correction and remaining gates](../../docs/migration/P4_TRANSFORMATION_QUEUE_BOUNDARY_VERIFICATION.md#current-m87-whole-entity-pure-transformation-reads--2026-10-22).
No new dispatcher, state owner, original gate closure or full-regression claim.

Status: implemented and focused-verified on2.631.0. [All attempts, actual failure
baseline, final checks and limits](../../docs/migration/P8_PROVIDER_UPDATE_CUTOFF_VERIFICATION.md).
Original P8.acceptance.017 and full goal remain open.

## 0. Problem Statement

TransformerManager.Update visits staffing lifecycle callbacks and processor.Advance
without checking world health between callbacks. If a provider catches a native
failure and returns, later callbacks can run before World.Step checks the system.
Production/WIP accounting must not continue dispatch after the first terminal fault.

## 1. Goals

1. Demonstrate the gap through existing public provider/lifecycle contracts and an
   actual caught native creation failure, not a test-owned fault flag.
2. Require healthy world state before manager work and after each staffing/advance
   callback, before another callback or final validation occurs.
3. Prove later providers retain original queue/progress/codecs, while legitimate
   work already done before the failure is not falsely rolled back.
4. Preserve first cause/phase, stopped clock, last healthy snapshots and save/hash
   refusal, including loaded partial WIP and independent saved continuation.

## 2. Design

Keep the sole TransformerManager and World fault owner. Add health gates at its
existing callback boundaries; do not catch/replace the original failure.

Instrument the existing non-friend PulseTransformer with transient before/after
Advance callbacks and an optional no-op ITransformationStaffingLifecycle callback.
Its existing timed execution body remains one method behind the wrapper. Hooks are
diagnostic-only, not authored gameplay or persisted state.

Reuse TransformerTests.Factory and its original master-selected process/transformer
JSON. The existing sibling-component fixture pattern adds the same authored pulse
part to the native assembler. Ordinary queue commands start the real6-minute native
input/WIP recipe plus two2-minute independent entity-output recipes. Save after one
Update with actual raw material protected in WIP and both external queues in progress.

Test staffing before native/independent execution and after independent Advance,
with caught native, propagated native and direct throw, fresh and loaded. Test
before-entry/after-work markers separately: instrumentation counts callbacks only;
all progress/results/resource changes come from the original providers.

## 3. API surface

No production public API or content/payload changes. Engine build2.631.0 and its
independent expected build change; save159/replay1/provider schemas remain unchanged.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | Standard/Transformations.cs | Medium: original callback cutoffs |
| Modify | Kernel/SaveGame.cs + expected protocol JSON | Low: behavior identity |
| Modify | ExtensionProof/TransformerProof.cs | Low: transient actual-provider probes |
| New | TransformationProviderCutoffTests.cs | Medium: partial physical/fault proof |
| Modify | Program.cs | Low: one exact registration |
| New/modify | verification/P8/master/README/AGENTS | Low: scoped evidence |

## 5. Error handling matrix

| Callback | Required result |
|---|---|
| Healthy lifecycle/Advance | Original physical timing/results and saved twin identity |
| Caught native fault | Retain native first cause; stop next dispatch |
| Propagated native fault | Same native first cause, no masking |
| Direct callback throw | Existing outer transformation system phase |
| Faulted world | No healthy time/publication/save/hash; retain actual diagnostic facts |

## 6. Backward compatibility

Development-only; no legacy/compatibility or instant fallback.

## 7. Verification

Build changed test target and run only the new exact case for baseline. After repair,
targeted build and new case plus existing queue-boundary and a selected physical
production case, after inspecting their scope. Bounded60s/5s; no full/family/profile
or broad transformation bundle.

## 8. Risks

This enforces dispatch cutoff, not rollback or a sandbox inside custom callback code.
Already executed earlier providers may have genuine partial state. Never claim all
WIP/continuous/output/disposition/fault matrices from this one boundary proof.

## 9. Out of scope

Optional financial settlement rules, full P8.017/P7/P6 acceptance, Oil/Tycoon/platform/
Draw/adapters, new production media and Git publication.

## 10. Sub-task execution order

1. Add actual-provider instrumentation and physical failure baseline.
2. Add original manager health gates; bump build identity without codec changes.
3. Verify selected exact cases, record all attempts and retain broad gates open.
