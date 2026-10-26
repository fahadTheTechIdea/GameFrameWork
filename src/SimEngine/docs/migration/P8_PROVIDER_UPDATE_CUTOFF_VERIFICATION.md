# P8-PC1 — Production provider dispatch cutoff verification

Current M87/native2.728/save161/replay2: the original mixed-provider fixture now also reproduces
pure snapshot/admission silently mutating a same-entity native processor while actual WIP is held.
Whole-target observation fixes those existing contract boundaries; the original dispatch-cutoff
case remains unchanged and passes482/0 again, alongside new880/0/default spatial398/0/physical
manufacturing1012/0. [All actual attempts, source responsibility and remaining requirements](P4_TRANSFORMATION_QUEUE_BOUNDARY_VERIFICATION.md#current-m87-whole-entity-pure-transformation-reads--2026-10-22).
No dispatcher, resource model or clock rewrite; no broad/full regression. Earlier PC1 image below
remains historical, not a claim of current whole-engine qualification.

## Outcome and scope

Engine **2.631.0 / save159 / replay1**. Sole TransformerManager requires healthy
world state before work, after each ITransformationStaffingLifecycle callback and
after each ITransformer.Advance, before another callback or final validation.
Native caught faults retain their original first phase/cause; no wrapper, rollback,
new scheduler, state owner, timer, resource ledger or codec was added.

Original **P8.acceptance.017 remains open**: broader WIP/disposition/ports/continuous/
results/provider/full accounting is not proven by this slice. Original bookkeeping
remains221/208 recorded checked/13 open; full goal and other original gates remain open.

## Actual failure

Before repair, World.Step checked health only after the whole transformation system
returned. A custom staffing callback catching a failed native creation could return
and enter processor.Advance. A processor catching that failure after real work could
return and allow a later processor's staffing/Advance callbacks before downstream
guards eventually stopped the system. The corrected pre-repair proof reproduced
four failures across fresh/loaded contexts, not merely a hypothetical source concern.

The repair adds existing World.RequireHealthyUpdate at original dispatch boundaries.
It does not catch/translate exceptions or undo already performed work. Direct throws
retain sim.transformations ownership; failed native creation retains entity.create.
Build identity and independent expected protocol JSON advance2.630.0→2.631.0 for
changed simulation behavior. Save159/replay1/provider3 and all view/content schemas
remain unchanged; there is no compatibility path.

## Contract-based proof and real accounting

The existing independent non-friend PulseTransformer gained only transient before/
after Advance instrumentation and optional ITransformationStaffingLifecycle with a
no-op callback when no test hook is attached. Its actual execution body moved once
to AdvanceCurrent; there is no copied production algorithm or diagnostic completion
counter. No host/demo/Oil or business source was changed.

One new exact case reuses TransformerTests.Factory/master-selected JSON and the
existing sibling-part authoring pattern. The native assembler has its ordinary
processor plus an independent pulse part; another entity has the independent pulse.
The initial entity setup explicitly authors emitter-start after start, proving actual
source EntityId precedes the later entity rather than guessing list/creation order.

Ordinary queue commands start a6-minute native conversion plus two2-minute external
entity-result recipes. After one actual minute, raw supply17 plus protected WIP3
still totals20; all three processes have earned one unit of progress, no result is
in the published entity collection. Saved continuation is captured at that real stage.

Two healthy fresh/loaded contexts prove deterministic twins and all3 timed results:
native WIP empties and supply becomes18, accounting for consume3/produce1 exactly.
18 negative contexts cover staffing before native/independent processing and after
independent Advance, each with caught-native/propagated-native/direct-throw failures,
fresh and loaded. Assertions prove callback cutoff, unchanged later provider codec,
native first cause, original material/WIP ownership, stopped time/healthy snapshot,
cleared pending events, refused healthy save/hash and an independent session actually
continuing to its next two timed external results.

A successfully created result before failure remains alive in the original pending
EntityStore (verified using the allocator's actual identity and Get), without being
flushed into All or published snapshots. This is real retained diagnostic state,
not rollback, lost output, a fabricated completed entity or healthy continuation.

The existing queue-boundary case verifies healthy external/native append/timed output,
saved continuation and malformed callback/codec cutoffs. The selected physical blocker
case verifies real hauling/staffing, tool/raw/fuel shortages, disabled/held workers,
partial saved work, limited fuel, both full product/waste vessels, whole output commit,
tool release, exact24-unit fuel use, private facts and command replay. Neither case
invokes the full transformation test family.

## All build and runtime attempts

Targeted build command:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
```

First build6.25s failed with one test API error: IResourceOutlet exposes Amount,
not Stock. The proof was corrected to use that existing inlet/outlet contract;
no engine alias or concrete inventory cast was introduced. Subsequent builds5.04s,
4.27s,4.51s,6.56s were clean, exit0, zero warnings/errors.

An exact invocation after the failed compile saw the previous executable with no
new registration: PID30796, child Exited2,129ms, matched=0. It is not counted as an
executed test. Later build/run orchestration explicitly gates runs on compile exit0.

All actual cases used literal --bounded-exact, timeout60000ms,
termination-timeout5000ms, output8000 for baselines/6500 for finals. Outputs are
untruncated, no observer errors/timeouts/aborted suite/process restart. Physical
case exec session14774 was polled to terminal on the same handle.

| Attempt/case | Pass/fail | Suite/process ms | PID/exit |
|---|---:|---:|---|
| First executable fixture |418/44 |705/858 |32876/1 |
| Explicit setup dependency |472/10 |428/567 |17436/1 |
| Correct pending-entity diagnostic, actual pre-repair baseline |478/4 |471/605 |20340/1 |
| Final provider cutoff |482/0 |435/577 |33972/0 |
| Final existing queue boundary |55/0 |268/411 |32648/0 |
| Final physical blockers |77/0 |29938/30081 |31908/0 |

Initial failures included two proof assumptions, not engine defects: setup dependency
IDs, not host entry listing, determine initialization order; a result born during a
failed step remains pending rather than in All. Correcting those facts did not waive
the core callback-cutoff assertions. The478/4 baseline isolates the actual defect.
Final selected total **614 passed / 0 failed**; no executable edits after finals.

Exact cases:

```text
Production provider cutoff: staffing and advance failures stop later dispatch with saved WIP intact
Transformation queue boundary: external append parity, hidden mutation and fault cutoff
Production blockers: physical supply workers fuel and full vessels preserve saved work and whole WIP
```

## Identities and remaining work

442 core C# sorted path/content SHA256:
`2f1c458f82ccf9b37de97ac9f6294808947a6339396d3e60dc700d0206f07dc0`.
Debug framework:
`94F6D2828C24AB058FD92F7440BF4E74E5B2499FC87412CF589BA24551536920`.
Framework test assembly:
`8504D780DCCE3D6A8B97C8C2A861256535096BF51506685A5DF7BFE1CAABE84B`.
New cutoff proof:
`1FE861A6DC0EF77B88E6ABF57A88ABCF4EEE0AED58B3C57071E68F9945307E1C`.
Independent provider:
`273687F72592446566C6E2021CC92C2F93E1D5B2105160B14AB237A049713800`.

This is callback-boundary enforcement, not a sandbox within custom code or global
atomic rollback. Resource settlement admission, broader provider/ref/rebind/WIP/
continuous/disposition/soak matrices and full P8/P7/P6 remain open. No full/family/
profile or broad bundle ran; prior Oil/automation evidence is historical, not rerun
on2.631. No Oil/Tycoon/platform/Draw/adapter or Git staging/commit/push changes.
