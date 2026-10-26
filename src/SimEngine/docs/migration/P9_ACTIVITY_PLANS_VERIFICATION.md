# P9-A2a persistent activity plans — focused working-tree evidence

Date: 2026-10-05. This is scoped proof, not a full regression or framework-completion claim.

Historical 2.69.0 evidence. [A2b](P9_CONSTRUCTION_PLANS_VERIFICATION.md) replaces the narrow
activity-plan API directly; its combined focused gate retains these tests under the generic contracts.

## Identity and commands actually run

Current build **2.69.0 / save41 / work19 / work-view24 / logistics4 / logistics-view9**.
The sole work codec adds intent records; native jobs, node/resource progress, transporter, logistics,
flow and alert layouts remain with their existing owners. There is no old-save reader.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Persistent plans: contracts, private intent and queue controls"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Persistent plans: physical execution and saved continuation"
```

Final build: **0 warnings, 0 errors, 3.75 seconds**. Final exact-suite results:

| Exact suite | Passed | Failed | Runtime |
| --- | ---: | ---: | ---: |
| Persistent plans: contracts, private intent and queue controls | 83 | 0 | 428 ms |
| Persistent plans: physical execution and saved continuation | 128 | 0 | 609 ms |

**211 checks / two exact suites, zero failures or aborted suites.** Physical waits are bounded to
240 updates with plan identity/state/blocker diagnostics. Only these two names ran, including their
repeated development gates; no third/family/full run. Initial fixture-only composition/seat mistakes
were corrected before this final gate. Eight older suites' literal current identity assertions were
migrated and compiled, not run. Their earlier evidence remains historical.

## What this proves

- IPlanBoard aliases the actual WorkBoard. Its list and lookup aliases own only intent, ordering,
  settings, terminal facts and one native execution link; no duplicate scheduler, jobs or inventory.
- Required immutable activity/target/group/priority/pause intent, pure current admission, owner-private
  detached views, duplicate rejection and identical plan/direct command refusal semantics.
- JSON in the explicit master manifest registers a public-only independent ActivityDef provider
  implementing IPlayerActivityDefinition. Its authored branching/joining DAG and provider-specific
  admission execute without scheduler edits or copied recipes.
- Pending intent creates no physical order/job/stock claim. Configure/reorder/self-move/cancel/repeat
  preserve stable IDs. Actual simultaneous promotion follows priority then explicit board order.
- Current direct native execution can block promotion; the same pending intent waits and re-admits
  after native cancellation. Active reordering refuses; active priority derives only native Order.
- Active pause immediately stops native motion and timed node progress, retains the actual worker,
  creation age, spent inputs and tool receipts. Paused open jobs cannot acquire enabled workers.
- Native physical work, travel and authored parallel/timed DAGs complete through actual engine work.
  Exactly authored startup consumption occurs; unpause never pays it twice, and completion frees tools.
- Pausing withdraws unacquired workflow supply intent without cancelling acquired hauls. Actual loaded
  cargo retains its original destination receipt, delivers once over time, and waits for work unpause.
  Only native work then consumes actual delivered inputs.
- Fresh repeat identities/native executions, active and direct cancellation, terminal history before
  native pruning, invalid identity checkpoint refusal, pending/active/paused/ended saves, pending
  command-inbox saves, deterministic interleaved twins and isolated sessions.

## What remains open

This stage is persistent **activity** intent admission, not structure/network placement or template
completion. Resources/machine shortages remain the existing physical order's responsibility, including
automatic supply. Construction-project promotion gating, structure/corridor/survey/upgrade/removal
intents, relative-position templates, bulk groups, automatic repeats and history lifecycle controls
remain later slices. Broad provider fault/purity/corrupt-restore matrices, replay/lockstep/shuffled
arrival/fuzz/soak/allocation and full acceptance remain unverified. No OilFieldDays/Tycoon/host/demo/
platform/Draw behavior was implemented or changed.

[Design](../../plans/enhancements/P9_ACTIVITY_PLANS_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).
