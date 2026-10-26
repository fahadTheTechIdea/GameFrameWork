# P4-G2a — Named requirement expressions

Date: 2026-10-05. Working-tree evidence, not committed-HEAD or full-regression completion.
Engine **2.46.0 / save21**, WorkBoard schema7 unchanged, WorkView feature schema7.
No old flat-list parser, implicit AND wrapper, params-array authoring or compatibility reader.
No oil, Tycoon, host/demo, Draw, renderer or platform project changes.

## Final verification

`dotnet build Beep.SimEngine.slnx --no-restore`: zero warnings/errors, final build1.83s.
Runtime, public non-friend ExtensionProof, testing library and test project compile together.

Exactly two directly affected named suites ran, including correction reruns:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Requirement expressions: strict named composition, topology and pure typed outcomes'
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Requirement expressions: physical guarded execution, saved alternatives and cancellation'
```

| Exact suite | Passed | Failed | Final runtime |
|---|---:|---:|---:|
| Strict named composition, topology and pure typed outcomes | 92 | 0 | 267ms |
| Physical guarded execution, saved alternatives and cancellation | 121 | 0 | 325ms |
| Total final-source checks | 213 | 0 | 592ms |

Neither final suite aborted. Counts are the final run, not sums of repeated assertions. Each suite
invokes the directly affected G1 admission or physical method because those paths use the extracted
topology and migrated node requirement API. This is not all older activity/work test families.
Older WorkRequirementTests and ContentOwnershipTests were source-migrated and compiled, not rerun
wholesale. No broad/full regression, stress, soak, benchmark or oil gate was run.

## Contracts and responsibility

- ActivityNodeDef/IActivityNodeDefinition exposes one optional Requirement alias. JSON references
  its stable ID through `requirement`; absent means no extra guard. The former array path is deleted.
- WorkRequirementGroupDef extends the same WorkRequirementDef family in the ordinary owned content
  registry. Master-listed JSON declares all/any mode and nonempty named entries. Resolved children
  alias the same selected definitions, in ordinal entry order; no second registry or inline parser.
- Kernel.DependencyGraph over IDependencyNode is the sole topology/cycle algorithm. ActivityGraph
  retains immutable typed node data and physical ancestry while delegating topology. Requirement
  references use the same compiler before Initialize. It captures immutable edges but aliases supplied
  definition values; it does not promise arbitrary external mutable values become immutable.
- WorkBoard owns one iterative expression evaluator. It checks every entry, even an unselected
  alternative, and validates leaf results. Invalid dominates both modes. All ranks blocked before
  pending before satisfied; any ranks satisfied before pending before blocked. Equal outcomes use
  ordinal entry identity. No recursion/depth cap or default-ready fallback.
- Leaf measurements retain their actual quantities/units. Group outcomes do not sum quantities or
  invent a combined measure. SelectedEntry identifies a satisfied any branch as pure readiness only.
  It is not a machine/tool/provider reservation, saved selection or permission to spend resources.
- WorkView uses the same traversal for detached immutable entry paths, leaf facts and group outcomes.
  Owner-private refusal clears the result buffer. Reading neither reserves nor changes state.
- WorkSystem, physical jobs/motion, ResourceTransactions, WorkBoard.Sync and existing providers remain
  the execution/state owners. Expression frames/results are transient, not another saved choice ledger.

## What the focused gates prove

Admission/outcome checks cover forward nested references, exact base aliases, frozen entries,
canonical ordering, immutable paths/results and private/pure reads. Unknown mode/fields, empty groups
or branch IDs, duplicate identities/references, missing/self references and named cycles refuse.
Old flat arrays refuse rather than being implicitly translated. The shared compiler handles the
4,096-node activity proof; a 512-group requirement chain evaluates iteratively without a content depth
cap. Missing runtime capability remains invalid even when another branch is satisfied. Malformed
unselected leaf results fault execution before material/cash spending.

Physical checks cover real crane travel/work, pending guards, exact one-time input/payment, pause at
actual position/progress, resume of the same job, immutable active diagnostics and saved twin hashes.
Timed alternatives save/pause/cancel through the same node codec. The directly affected G1 physical
method also covers parallel jobs, one-machine contention, supply, effects and saved named linkage.
Each waiting stage is bounded to 500 ticks and fails with order/node/progress/blocker diagnostics;
the bound belongs to the test, not engine time or world size.

An initial pure-read assertion ran before newly spawned stock's creation facts had drained and the
save/hash guard correctly refused. The fixture now performs the normal engine Update before its
read-only checkpoint; no guard bypass or engine fallback was added. A misplaced using block was
corrected before the clean final build. Neither correction broadens verification scope.

## Remaining approved work

G2b must introduce actual typed candidate offers, scope and durable selection on the existing named
node/job. G2c must acquire all-or-none real provider/tool/resource commitments via existing P7 tokens,
consume physical/continuous inputs, release/reselect correctly, and save/hash that ownership. Current
SelectedEntry readiness is not proof of these capabilities. Generic authored executors (G3), workflow
templates/migrations, prior-activity facts, ETA, oil integration, replay/stress and full-stage acceptance
remain required. No whole P4 or overall roadmap completion is claimed.

The lifecycle skill guided the contract-first staged migration and single-owner topology/evaluation
boundaries. Repository/user exact-suite policy overrides its generic full-regression suggestion.
