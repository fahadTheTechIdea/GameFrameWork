# P9-C3g2 — Physical recovery-output cost proof

Status: focused scope verified,2026-10-07; current2.218.0/save122 source.
Extends [C3g](P9_HAUL_CANDIDATE_COST_DESIGN.md) only for physical cargo recovery.

## 1. Scope and ownership

SimEngine owns physical resources, cargo transfer, reservations, movement, jobs and elapsed time.
This physical-only proof does not implement economics. Default values/budgets/income and optional
Tycoon rules remain the shared module's responsibility in P4-H1/H2/H3, not excluded engine features.
Reuse LogisticsService.SelectedCost/PreviewDestination, existing dock/cargo contracts and authored
fixtures. No second route/cost model, scheduler, inventory, clock or host gameplay logic.

## 2. Acceptance

- A docked endpoint at10 with service bay15 competes with a non-docked endpoint at12.
  Recovery must select the admitted trip cost, not endpoint-center proximity.
- Native cargo and public separate-inventory cargo use the same selection authority.
- A continuing recovery job freshly selects valid room while retaining the same actual cargo,
  worker, job identity, creation time and reservation provenance.
- Physical movement, timed unloading, conservation, claim release and save/load remain engine-owned.
- Pure previews must not mutate state or advance the single GameClock.

## 3. Implementation and state

Use the existing RecoveryDispatchTests fixture and its new exact named RecoveryCost scenario.
Explicit protocol instrumentation must be distinguished from normal player commands. Replace inline
endpoint cloning and direct removal of forty stored units with explicit master-listed
recovery-output-cost.json. It authors service geometry, output stores, stationary compatible worker
and a physical material-consuming activity. Native PostOrder and existing Update consume forty units
and open real capacity; only the replanning failure signal remains labeled direct protocol probing.
Existing PointGame additionalDocuments lists the file in the master; no new loader/module/fixture.
No runtime API, payload or schema change is planned. Current2.218.0/save122/logistics view28/work
view76 source, not historical H1's build, is the gate baseline. No compatibility or parallel models.

## 4. Validation and failure cases

Compare actual admitted service-bay cost; reject missing/invalid costs using the existing validator.
Recheck room on retarget, reserve new room before releasing old room, and preserve cargo on refusal.
Keep waits bounded with diagnostic snapshots; require saved twins and material conservation.
These fixtures do not prove arbitrary geometry, congestion, prospective-origin multi-dock cost,
fuel/service budgets, global purity, replay, stress or full framework acceptance.

## 5. Verification and tasks

- [x] Review the pending physical-only RecoveryCost fixture and assertions; author endpoints/material
  work in JSON and replace direct stock removal with native player-ordered work.
- [x] At the completed slice, one clean build and its ONE exact previously unexecuted named scenario.
- [x] Record actual results and update the master tracker; no historical passing-count reuse.
- [x] No older source/recovery, family/full, platform, Draw or business test runs.

One clean build0 warnings/errors3.23s; ONE exact scenario30 checks0 failures/aborts405ms, first
attempt. No engine runtime/schema/metadata changes, repair/rerun or post-gate test/gameplay edits.
Current2.218.0/save122/logistics view28/work view76. Native and public separate-inventory cargo
retain job/worker/age/cargo receipt while replacing destination room, then travel/unload/clear actual
claims identically after save/load. Forty pre-existing units are consumed by authored native activity,
not test code. Replanning signal is still a labeled direct protocol probe, NOT normal Update
endpoint-loss/recovery proof. [Evidence](../../docs/migration/P9_RECOVERY_OUTPUT_COST_VERIFICATION.md).
Only this stage's narrow proof is complete; full framework goal/plan and publication remain open.

The ad-hoc paid-supplier fixture/module/scenario was removed. Any later money/supplier proof must
exercise P4-H's actual shared module and accounts, not implement a test-owned parallel economy.
Existing IEconomy hooks are retained; blanket business removal is not the approved plan.
