# P4-G31 — Contract-owned reusable operating inputs

## 0. Problem

Standard/OperatingInputs.cs exposes IOperatingInputsDefinition but the reusable OperatingInputs
inherits Component<OperatingInputsDef>. Independent JSON metadata cannot bind that original
exposure component without native definition inheritance. Its public ReadDemand observes entity
versions, but not the actual exposure codec. This violates the existing P2/P4 contract boundary.

## 1. Goals

1. Native and independent JSON definitions bind the same operating exposure component and codec1.
2. One declaration reads named identity, explicit phase, native resource rates and disabled mode.
3. Complete loaded metadata/runtime pairing and owned state references validate before Initialize.
4. Existing phase admission/transactions consume fuel and idle/standby inputs over game time.
5. Actual provider observations detect hidden exposure mutation before publishing healthy facts.

## 2. Design

Extend the existing definition family with IStatefulOperatingInputsDefinition.States and pair it with
IStatefulOperatingInputs : IOperatingInputs, IEntityStateSource. This describes the reusable provider's
blocked mode; external exposure providers remain IOperatingInputs and need not invent native states.

OperatingInputsDeclaration owns the original JSON read/resolve/physical binding validation. Native
OperatingInputsDef delegates; original OperatingInputs consumes actual contract metadata as Component.
Its two exposure fields and Sync remain sole exposure authority. ResourceInventoryState retains stock
and promises; OperatingInputsContracts retains cumulative-rate evaluation and exact whole inventory
transaction/accepted interval admission. No new timer, draw scheduler, inventory, loader or ledger.

Original composition checks all loaded runtime/definition stateful pairs and the actual owned disabled
state, including unused entities. Actual rates/phase target/inventory/module checks remain authoritative.
ReadDemand uses original EntityObservation.ReadProvider for the actual exposure codec and live alias,
including named identity reads. Existing private MotionView/WorkView/OperationsView project the facts.

## 3. API

IStatefulOperatingInputsDefinition.States; IStatefulOperatingInputs marker/state source;
OperatingInputsDeclaration.Read(JsonReader), Resolve(ContentResolver), ValidateBinding(EntityDef).
OperatingInputs.Def returns the actual IStatefulOperatingInputsDefinition. Original command/view APIs
and JSON fields unchanged. No native definition accessor or alternate compatibility path.

## 4. Files

Modify Standard/OperatingInputs.cs, Kernel/SaveGame.cs and the protocol build identity;
register one new bounded case in tests/Beep.SimEngine.Tests/Program.cs.
New tests/ExtensionProof/OperatingInputsBindingProof.cs and
tests/Beep.SimEngine.Tests/OperatingInputsBindingTests.cs. Reuse existing master authoring/player helpers.
Update master tracker and actual evidence after the new case completes.

## 5. Errors

Malformed identity/phase/rate/state/filter or missing execution target: ContentException before
Initialize. Stateful runtime/metadata mismatch and null/foreign state references: same early refusal.
Actual pure callback mutation/exception: original scoped observation first fault, unpublished facts,
no healthy retry/hash/save/clock advance. Ordinary input shortage blocks physical work/motion with no
partial burn or exposure credit; no instant recovery, compensation or fabricated supply.

## 6. Compatibility

Development only. No old reader, native-definition wrapper, duplicate declaration or compatibility API.
Build identity changes; existing exposure codec1/save154/replay1 remain unchanged.

## 7. Verification

ONE NEW exact bounded case: Operating inputs bindings: authored exposure contracts and physical phase consumption.
Native/independent bindings, real timed inspection travel, phase-specific cumulative stock consumption,
shortage, policy-selected idle/standby, saved in-travel twins/pause/replay/private immutable facts,
interleaved isolation, invalid loaded/unused declarations and actual codec faults.
Compilation only for historical fixtures; no historical/family/full/platform/Oil/Tycoon runtime.

Scoped result: two clean builds8.68s/5.73s, ONE new exact case FIRST/FINAL233/0/0 in1404ms.
34 early malformed declarations and20 actual observation fault combinations passed.
[Actual evidence](../../docs/migration/P4_OPERATING_INPUTS_BINDINGS_VERIFICATION.md).

## 8. Risks

Scoped provider observation is not an arbitrary CLR/whole-world/cross-provider sandbox. Full simultaneous
providers, remote/service ports, rates/quantum matrices, operating resupply/cancel/removal/recovery/network
accounting, provider commit faults, concurrency/lockstep/fuzz/soak/allocation/performance/global acceptance
remain open until actually proven. No broad completion claim from this case.

## 9. Scope

Engine only. Pending excluded Tycoon/catalog return-type migration remains deferred. No business/default,
platform/Draw, new game clock, legacy path or staging/commit/push before full verified completion.

## 10. Order

Review original exposure/contracts/ports/views → sole declaration and paired metadata → original runtime
and composition/observation migration → thin new proof → focused build and ONE new exact case → actual
evidence and tracker. Full P1-P12 and active goal remain open.
