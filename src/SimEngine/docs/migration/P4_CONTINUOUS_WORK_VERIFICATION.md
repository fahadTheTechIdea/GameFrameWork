# P4-G2c3 — Continuous native work-input verification

Date: 2026-10-05. Current working-tree evidence, not committed HEAD or full-regression completion.
Engine **2.51.0/save26**, work/view11, resource-flow4. No old use synonym or saved-state reader.
Only standalone runtime/proof/tests/docs changed; no oil/Tycoon/host/demo/platform/Draw work.

## Final-source gate

`dotnet build Beep.SimEngine.slnx --no-restore`: zero warnings/errors, final build1.50s. Runtime,
public non-friend ExtensionProof, testing library and test project compile together.

Exactly these two directly affected suites ran throughout this slice; existing methods include directly
affected resource/startup proofs, extended with continuous methods. No broad gate or third suite ran.

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Activity resources: authored scoped offers, native sets and private selection'
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Activity resources: physical consumption, held tools, interruption and saved ownership'
```

| Exact suite | Passed | Failed | Final runtime |
|---|---:|---:|---:|
| Authored scoped offers, native sets and private selection | 100 | 0 | 305ms |
| Physical consumption, held tools, interruption and saved ownership | 264 | 0 | 1919ms |
| Total final-source checks | 364 | 0 | 2224ms |

Zero aborted suites. Counts are final-source results, not sums of development reruns. No broad/full
regression, oil, replay, lockstep, stress, soak, allocation or whole-framework gate ran. Old activity/
worker schema assertions and zero-bill claim assertions were source-migrated and compile, but their own
suites did not run. Historical reports do not establish current broad-regression correctness.

Targeted tracked-file `git diff --check` passed. Explicit trailing-whitespace scans covered all edited
runtime/proof/test/tracker/stage/evidence files, including untracked files, with no matches. Source
checks found no StartWork or separate node-progress Advance path remaining. These are source checks,
not additional suites or broad-regression evidence.

## Sole owners and implemented behavior

- Existing target_resource_requirements, worker_resource_requirements and unrelated connected provider
  leaves author `use: "continuous"` through IResourceWorkRequirement. Native quantities are the exact
  total budget for the node, not a per-wall-clock-second rate. RequirementMultiplier scales that total;
  required work determines consumption per actual progress fraction.
- WorkRequirementContext aliases the immutable node and captures actual/prospective progress once.
  IWorkerDispatch.EffectiveWorkRate delegates to WorkSystem's sole rate formula. WorkBoard owns one
  cumulative/delta/remaining calculator: signed64 work plus Q32 fraction, BigInteger intermediates,
  upward cumulative native quantization bounded by total. Each increment pays only the difference.
  There is no independently rounded tick charge, lost tail, saved remainder, stock balance or work cap.
- Assignment retains continuous source identity with no persistent receipt. AdvanceWork replaces
  StartWork and WorkSystem's separate advancement. It preflights startup receipts, the whole next
  continuous set and ordinary money, acquires real next-input promises through ResourceTransactions,
  consumes the combined exact receipt set, then commits actual node progress. Normal shortage spends/
  advances nothing. A violating provider faults Update, without fake history/compensation/checkpoint.
- EvaluateClaims permits empty native bills only as pure endpoint access probes through the existing
  resolver/ownership/checkpoint guards; AcquireClaims still refuses them. Already funded discrete
  inputs allow later zero-demand increments without trusting missing/foreign endpoints.
- Existing LogisticsService supplies the actual next demand even for assigned paused nodes, through
  actual stock/inbound receipts/truck travel. Public player stock targets require explicitly public
  content; private ports are not made public by engine fallback. Automatic inputs retain their policy.
- Stock shortage retains the selected recipe/source. Interruption releases/reselects through existing
  jobs and resumes from previous progress, never repeats the full budget. A depleted second source now
  reports its own shortage, not the first unselected candidate's lost-source result. Positive actual
  progress retains committed branches even when the first discrete quantum spans many increments.
- The sole node codec stores source-only continuous bindings and actual progress beside ordinary
  startup/tool/cost facts. Restore rejects fabricated continuous spent flags/persistent receipts and
  invalid authored/native/branch/lifetime/ownership. No consuming restore or new remainder codec.
- Private ActivityNodeView.ContinuousInputs publishes immutable derived Total/Consumed/Remaining.
  Endpoint identifies the current binding, not the origin of all previous consumption after reselection;
  existing optional native flow history records actual historical endpoints. Detached facts never mutate.
- continuous-work.json is explicitly master-listed through existing proof configuration. Slow machines,
  target/worker/scoped inputs and parallel/scaled plans are ordinary content. Harness code submits engine
  commands/reads outcomes or instruments setup/faults, never implements work/supply gameplay.

## Focused scenarios and development findings

Checks cover strict/native/instant refusal, fractional offers, whole-set and parallel contention,
pure/foreign/empty endpoint probes, tiny/wide progress, travel, zero rate, half-rate work, cumulative
discrete/fluid usage, startup parity, shortage/replenishment, committed recipes, actual player/automatic
hauling, demand withdrawal, private immutable budgets, interrupted second-source exhaustion/refill,
worker-local zero-demand steps, mapless/unpriced timing, cancellation, scaled/overshooting work,
corrupt restore and short-consuming providers. Saved twins span ordinary stages. Each physical wait
is bounded to 500 ticks and reports node/job/progress/blocker diagnostics.

Development fixture assumptions were corrected: public stock commands require public ports; separate
player supply may legitimately retain its own receipt after work completes; initialization events need
Update before save; and the long repeated-quarter-haul route outlasted its timeout while still advancing.
The proof now explicitly authors its public target, checks only work-owned receipts and uses a shorter
real route. No engine authorization, receipt ownership, travel timing or stock rule was weakened.
The selected-second-source blocker defect was fixed in the sole evaluator and exercised under repeated
exhaustion/replenishment.

## Remaining gates

Work-normalized inputs do not prove per-elapsed-time utility/idle/travel fuel rates, all source-generation/
rebinding cases, arbitrary mod faults or broader stress/performance. Area/target policies, optional money
reservations, wider economic guards, generic executors/templates, prior completion facts/ETA, full replay/
lockstep/soak/allocation, oil integration and full P1-P12 acceptance remain open. Lifecycle guidance
shaped this staged single-owner migration; user/repository exact-suite restrictions govern verification.
