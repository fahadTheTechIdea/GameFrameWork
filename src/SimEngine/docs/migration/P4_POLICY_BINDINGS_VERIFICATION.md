# P4-G26 safety policy metadata/read bindings — scoped evidence

2026-10-09, core2.361.0/save154/replay1; persisted engine/provider/view payload schemas unchanged.
Full P1–P12 and active goal OPEN; no staging/commit/push.

## Original metadata and gameplay owners

One OperationPolicyDeclaration now reads/resolves native and independent JSON metadata; removed
duplicated native/proof policy readers. Original OperationPolicy state/Sync runtime consumes
IOperationPolicyDefinition rather than OperationPolicyDef. Independent metadata creates the
same original runtime, not a wrapper, replacement policy engine or legacy path.

Original OperationPolicyContracts guards full metadata/state/bound condition/control reads and
pure policy admission using original EntityObservation and actual provider Sync. Condition metadata
validation now runs inside its actual condition-provider scope BEFORE getters execute. This repairs
the hidden-state gap found during this new case, not merely its expectations.

Shutdown selection and acknowledgment use named contracts, not part.Def type lookups. Explicit
acknowledgment observes state before clearing a temporarily latched/enabled pair, then validates the
result. Original thresholds, chronology, restart hysteresis, command refusals, exact mutations and
recurring priority rules remain authoritative. Component-wide authoring checks use actual named
metadata/HasCapability, not concrete condition/operation parts. Policy ID view stages detached IDs.

Existing ConditionContracts.Read now guards metadata and callbacks through the original actual
codec, replacing limited manual read checks; its condition math/change owner remains unchanged.
Original ReadProvider rejects a null World before fault-latching, avoiding a null dereference.
No added policy/condition state, clock, registry, scheduler, copied rule evaluator or compatibility.

## Public extension / thin player

IndependentPolicyDef uses sole declaration and original native OperationPolicy runtime. Existing
ProofOperationPolicyDef and ProofConditionDef now have transient callback probes; existing runtime
providers expose probes on existing saved ChangedTick fields, not extra state or payload changes.
No gameplay simulation moved into driver/harness/demo. Probe faults are deliberately armed only
after actual content resolution and Initialize.

Existing shared recipe factory explicitly selects equipment/policy modules/master document.
Derived authoring retains native units/effects/requirements/activities, explicitly supplies the
permit section and stopped state required by its existing extension, maps physical materials to
the existing supply resource, assigns compatible inspect workers and authors a three-hour batch.
Repair still uses the original thirty-minute activity and physical automatic supply; no instant
condition change, work timer, hauling, repair or restart in the driver.

Actual public-command workflow: real four-unit batch hauling; travel and twelve-minute stress
work; unsafe shutdown; native maintenance priority70+criticality30; spare/tool hauling; thirty-minute
repair; next-update restart; preserved three-hour batch progress; one entity result. Conservation
accounts for three production inputs plus three repair spares, with tools retained normally.
Saved shutdown/twins/one-clock pause, replay and settled save identity are proven. A saved branch
explicitly disables/acknowledges only its own latch and still refuses unsafe enabling.

## Actual compilation and runtime

Build: dotnet build Beep.SimEngine.slnx --no-restore.
Five clean compilation builds:8.86s,4.70s,3.81s,5.45s,5.66s; zero warnings/errors.

Only ONE NEW exact bounded case was executed, five times:

```text
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- --bounded-exact "Policy bindings: authored contracts govern physical shutdown repair and restart" --timeout-ms 30000 --termination-timeout-ms 2000 --output-chars 24000
```

1.1 passed/1 failed in156ms; pid15576, exit1, observed313ms: missing explicitly selected permit
  section declaration in the new fixture. Reused existing PermitRequirementDef.
2.1 passed/1 failed in175ms; pid23008, exit1, observed320ms: existing permit extension required
  authored stopped state absent in the point fixture. Added that explicit content state.
3.624 passed/13 failed in3590ms; pid20108, exit1, observed3732ms: physical workflow passed, but
  policy ValidState read condition.Unit before condition codec observation. Hidden mutation was
  unnoticed. Added actual condition-provider scope around state and configuration metadata validation.
4.637 passed/0 failed in3355ms; pid8720, exit0, observed3492ms.
5.Final646 passed/0 failed in3657ms; pid29520, exit0, observed3820ms: strengthened missing-session
  and explicit latch acknowledgment/save-branch safety checks.

Zero aborted cases; all output untruncated, no observer errors. Final81 source/fixture inputs
frozen2026-10-09 09:25:38 UTC and verified unchanged afterward; no executable edits/runtime after final.

64 combinations exercise public view, policy preview and ID discovery across policy/condition
identity/unit/operation/state callbacks × exception, clock pause, identity allocation and hidden
saved-state mutation. They reject partial facts and preserve the first observation.provider fault;
no healthy Update/hash/save/read retry or further callbacks. Seven malformed early variants
(operation/condition/unit/bands/duplicate owner/unknown key/unused bad metadata) reject before
Initialize; null-session reads fail explicitly. Normal policy/private/missing/refusal/priority/
retained metadata semantics and actual saved physical continuation are also exercised.

## Remaining acceptance

G25/G24 and older runtime evidence is historical, not rerun on2.361.0. No historical/family/full,
OilFieldDays, Tycoon or platform runtime. Arbitrary CLR/content/codec sandbox and cross-provider
mutation outside declared scopes, complete condition/operation/capability decomposition, all
mutating transaction semantics, spatial intent deep immutability, every external codec, multi-lane/
network/continuous/technology/removal/concurrency/lockstep/fuzz/soak/performance and full P1–P12
acceptance remain unproved. Excluded Tycoon catalog type-binding authority still pending.
No business/default/domain/platform/Draw changes; no publishing.
