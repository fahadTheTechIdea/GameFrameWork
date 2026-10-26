# P8-B1 — Authored signal-backed transformation gates

## Current M158 — contract-authored gates (2026-10-25)

Implemented on engine2.775/save161/replay2. ISignalTransformationGateDefinition exposes
authored transformations and conditions. SignalTransformationGateDeclaration is the sole
strict reader/resolver for native and independently registered JSON kinds. The native
definition delegates to it; SignalTransformationGate no longer inherits a concrete-definition
component base. There is no compatibility path, alternate runtime, condition evaluator,
scheduler, stock book or clock. Existing JSON fields and empty gate codec1 are unchanged.

TransformationGateBindingContracts admits every loaded binding during module composition,
including unused entity prototypes and independent definitions without ValidateCore overrides.
It requires frozen actual owner/codec/catalog aliases, unique ordered transformations/conditions,
declared producer support and actual named compatible signal metadata. Live reads use the
same binding admission and original provider/entity observation; malformed providers cannot
silently become an open gate. SignalGateCondition remains the only condition interpreter.

Focused acceptance: independent metadata physical batch/saved interruption/restart/private facts/
complete replay26 checks; native contracts64; affected native execution/cancellation/fault cutoff99.
All final outcomes pass, across the recorded test-only build4/build5 boundary; no unrelated green
reruns. A missing explicit tool port and obsolete post-fault public-history read in the old fixture
were corrected without changing resource amounts/timing or relaxing assertions. Negative checks
now fingerprint the actual journal codec and separately require public first-fault refusal.
[All attempts, scope and self-review](../../docs/migration/P8_SIGNAL_GATES_VERIFICATION.md#current-m158-contract-authored-transformation-gates--2026-10-25).

This finishes this concrete contract gap within P8.017/P10.022; it does not close either whole
compound feature gate. Next continue the finite declared processing/control owner matrix in P6,
not arbitrary future-provider expansion. Acceptance is focused-only; quality/lot is explicitly
deferred by the human. Only SimEngine changed; no consumer/host/demo/platform gameplay.

## Historical B1 implementation

Status: focused-verified on2.81.0/save51/view5:155 checks/two exact suites (64 contracts;91 execution),
zero failures/aborts; clean build, zero warnings/errors,2.50s.
[Evidence](../../docs/migration/P8_SIGNAL_GATES_VERIFICATION.md). Full P8/P1–P12 remain partial.
No compatibility path. Physical utility production/consumption is not implemented by this slice.

Current follow-on [P10-B2 inventory readiness](P10_INVENTORY_SIGNALS_DESIGN.md) feeds the same
gate condition/typed signal reader with actual whole-bill stock, unpromised stock and incoming
capacity, including real reservations. Native timed process startup/running waits and saved restart
are included in115 checks/two exact suites on2.124.0/save67; [evidence](../../docs/migration/P10_INVENTORY_SIGNALS_VERIFICATION.md).
No new gate/store/controller/codec owner. Original B1 suites compile only; utility solvers remain open.

## Problem and goals

Before B1, TransformationExecutionContext.CheckGates called Code-only custom ITransformationGate
components. A module could implement a utility gate, but native JSON could not declare typed availability,
boolean readiness or safe numeric operating ranges, and management had no per-gate diagnostic facts.
Add reusable authored gates, not a power ledger/utility simulation/second process loop.

1. ITransformationGate becomes named and returns one immutable TransformationGateSnapshot with
   per-condition facts. All current providers migrate directly. The shared existing contracts owner
   evaluates all gates in stable capability order for execution and private views, guarding pure reads.
2. SignalTransformationGateDef reads explicit id, transformations and nonempty named conditions from
   master-listed JSON. Each condition names an actual local signal capability, selected signal and
   authored blocker. Boolean conditions author expected; numeric ranges author minimum, maximum and
   exact unit. No source lookup/default threshold/implicit resource or time conversion.
3. Content validates actual supported transformation/signal aliases, native signal kind/unit, duplicate
   IDs and actual source metadata before Initialize. Conditions are sorted
   by stable identity; the sole AutomationSignalDef.ReadFrom owns provider schema/value validation.
4. Pure gates own no progress, inventory, promise, signal state or codec fields. Existing utility/provider
   components own physical facts. Native Transformer checks before startup/progress/completion; ordinary
   gate refusal preserves actual WIP/tools/progress/costs/queue. Return/cancel uses existing capacity and
   disposition independently of production readiness. No forced operation flag/host decision.
5. Management exposes private detached live gate facts through the same reader. Existing commands still
   queue valid blocked intent and configure real signal providers; a gate is immutable authored content,
   not another editable policy store. Saved blocked continuation re-reads actual restored providers.

## API and single owners

TransformationContracts.cs: ITransformationGate : INamedEntityCapability,
ReadGate(World, TransformationDef) -> TransformationGateSnapshot. Snapshot records named capability,
applicability, aggregate blocker and immutable named condition facts (source identity, signal, typed
value/unit/availability, readiness/blocker). TransformationContracts.ReadGates is shared pure evaluator;
context.CheckGates returns its aggregate refusal. Public-only providers return the same facts without
native manager branches. No optional old Code callback/overload.

Standard/TransformationSignalGates.cs contains the native component/definition; TransformationGates.cs
contains detached facts and the existing partial contracts owner's shared evaluator.
TransformationModule registers signal_transformation_gate schema1 and existing management view5.
Transformer4/manager2/flow5 and other owners retain their codecs; build2.81.0/save51 identifies changed
contracts/behavior. Native gate empty Sync is the component's sole codec, not an extra service/system.
TransformationManagementView.ReadGates validates owner/named transformer/supported catalog identity and
returns aggregate plus per-gate facts. No World/provider handles or host-side rule reproduction.

Numeric range readiness is inclusive native validity, not hysteresis/control-action logic. Existing
automation owns threshold latches, delayed controls and cooldown. Gates only decide whether the current
authored process may advance. This is intentionally not another control-rule expression compiler.

## JSON shape

```json
{
  "signal_transformation_gate": {
    "id":"safe-operation",
    "transformations":["parts.assembly"],
    "conditions":[
      {"id":"power","source":"utility-feed","signal":"utility.ready","expected":true,"blocker":"process.power-unavailable"},
      {"id":"pressure","source":"process-sensor","signal":"process.pressure","minimum":1,"maximum":4,"unit":"unit.pressure","blocker":"process.pressure-unsafe"}
    ]
  }
}
```

Referenced signal sources must be actual declared named components on the same producer. A domain
module may publish network/worker/service facts through its existing signal contract; this stage does
not invent remote entity references or utility balances. No gate means no authored constraint, not a
fabricated always-ready utility. Missing listed sources fail before gameplay.

## Errors and verification

Missing/duplicate IDs, malformed types/ranges/units, unknown or foreign signal/transformation aliases,
wrong actual provider schema -> composition/content refusal. Same-entity projection is not inferred
to be a forbidden dependency cycle; signal providers own their semantics. Available
false/range miss -> authored blocker; unavailable signal -> its typed blocker, never substituted zero.
Unknown/private host entity -> typed refusal without facts. Pure source/gate mutation or malformed
provider snapshot faults normal Update and cannot save/hash; no rollback masking.

Only these suite names plus standalone build may execute this stage (repair repeats allowed):

- `Transformation gates: authored signals, named contracts and private previews`
- `Transformation gates: timed waits, cancellation and saved provider faults`

Prove JSON source/kind/unit/alias validation, named multiple-provider participation, pure/private
immutable views, exact boundary readiness/unavailability, startup with no claims/costs, mid-batch
pause/resume, completed-capacity waits, repeat/cancel/return, saved twins/pending ordinary controls,
independent provider participation and malformed/pure-mutation fault boundaries. Bounded explicit
tick stages only; other suites migrate/compile, no third/family/full gate or platform work.

## Sequence and remaining scope

Design/master -> contract/facts/shared reader -> native JSON gate -> existing view and public proof ->
build/two named suites -> evidence/parents/master/README/AGENTS/help. No demo/harness gameplay.
Remaining: physical utility supply/draw/elapsed consumption, remote gate bindings, worker reservations,
gate expression/hysteresis policies, distinct ports/catalysts/disposition/continuous processes, arbitrary
result providers and full replay/fuzz/stress/performance/acceptance. Full goal remains active/partial.
