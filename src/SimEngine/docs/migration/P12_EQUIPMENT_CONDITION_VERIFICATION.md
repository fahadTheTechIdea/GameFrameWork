# P12-D1 equipment condition — scoped verification

Date: 2026-10-06. Runtime: Beep.SimEngine only. Build **beep-simengine-2.99.0**, save **63**.
[Stage](../../plans/enhancements/P12_EQUIPMENT_CONDITION_DESIGN.md).

## Implemented ownership

Optional sim.equipment-condition declares native codec1/view1/signal schema1 and one stateless validation
system at370 with an empty payload. It requires no compulsory work, map, stock, operation, economy or
population module. Each actual named provider owns exactly one condition value and change timestamp
through its own Sync. ConditionContracts alone validates live identity/catalog units/bounds/native quantum,
pure admission and exact commit. CheckQuantity is reused by native and independent providers; no health
registry, damage ledger, repair queue, work progress mirror, resource balance or host calculation.

Native equipment_condition requires explicit id/unit.condition/maximum/initial/damaged and failed states.
There is no100-point cap or clamped repair/damage. Exact positive changes refuse missing/read-only providers,
wrong unit/quantum and out-of-bounds requests without mutation. Public contracts admit independent owners
with other authored native units. Immutable private views enumerate/read only actual named capabilities;
foreign reads clear facts. Existing entity.Update alone settles authored states.

condition_effects/condition_requirements are master-listed definitions extending existing effect and
requirement families. PhysicalWorkEffectDef migrates its existing protected hooks to CheckEffect/CommitEffect
in native resource/knowledge effects and public proof directly; no old hook or alternate completion path.
The existing WorkBoard owns machine travel, native resource/tool claims, positive-time progress, cancellation
and once-only completion. Native condition.value/maximum signals use existing OperationsView/typed gates
and automation contracts, not another enabled/hold value. No instant player condition-edit command.

Native component decode stages bounds, native quantity and structural chronology before assignment.
Future change ticks validate at actual session reads/checkpoint/OnLoaded, after complete restoration.
Five dishonest public-provider modes fault normal Update and prevent checkpoints. Pure guards cover the
actual entity version/identity/owner and world tick/allocator, not every possible cross-service mutation.
Unrelated existing native layouts remain unchanged; saves/peers still require exact current build.

## Final commands and evidence

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Equipment condition: authored ownership, providers and checkpoint admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Equipment condition: physical service, shutdown and saved continuation"
```

- Build: zero warnings/errors, **2.13 s**.
- Contracts/provider/checkpoint: **94 passed, 0 failed**, 728 ms, one exact suite.
- Physical service: **34 passed, 0 failed**, 779 ms, one exact suite.
- **128 total passed, 0 failed, 0 aborted suites**. Repair attempts reran only these two names.
  Two early attempts after a failed compile selected zero suites; they are not verification evidence.
- 33 other current build assertions migrated/compiled, **not run**. Public knowledge-effect proof hooks
  migrate/compile only; its earlier suite was not rerun. No third/family/full run.

## Proven behavior

- Strict units/bounds/initial values/states/signal schema/effect direction/source/quantities/requirement
  blocker and positive-machine ancestry. Timer-only or zero-time repair/damage fails content admission.
- Private metadata clearing, explicit observer access, immutable captured facts, cross-session rejection
  and pure ordinary refusal without allocation, stock changes or claims.
- Full damage/repair, exact native quanta, maximal native representable points without a genre cap;
  two separately named native conditions retain independent values/codecs and reject duplicate ownership.
- Staged malformed/negative/out-of-bounds/structurally invalid component decode preserves prior state.
  Future condition chronology refuses reads/hash and post-restore semantics; instrumentation restores the
  original payload exactly. This is not a claim of global transactional rollback across failed session loads.
- Native condition-only no-map/no-work/no-inventory session updates/saves/continues identically.
- Independent public-only condition owner uses the same native requirement/effect and real machine work,
  consumes actual spares, saves its one codec, and also works with an alternative authored native unit.
- Mutating read/admission, missing commit, future commit and incorrect unit facts fault normal Update;
  faulted partial effects cannot produce a healthy checkpoint. No invented compensation.
- Normal player commands start a real batch and positive-time damage/service. A crane travels before
  damage; the existing condition signal gate stops in-flight WIP and preserves real process progress.
- Disabled readiness earns no service progress, spends no spares and cannot heal. Active service consumes
  exactly3 spares, protects2 real tools and cannot repair early. Cancel retains damage/spent inputs and
  releases only unspent tool holds. Fresh service performs actual work and spends a fresh3 spares.
- Once-only repair clears the existing safety gate; the same WIP resumes. Final native conservation is
  30 initial -2 batch -6 two service startups +1 output =23, foreign stock/condition unchanged.
- Ages above2^40, saves during damaged/gated work, partial material/tool service, cancellation,
  repaired/resumed WIP and final completion preserve interleaved twin hashes.

## Remaining roadmap obligations

Automatic elapsed/operating/travel wear; inspection and service-interval policies; reserved preparedness
capacity/materials; automatic maintenance posting through existing work; condition-triggered incident/severity
policies; arbitrary non-process gates and network-specific condition policy; replacement/multi-provider
binding/fault breadth; contamination layers/native scientific environmental formulas; replay/fuzz/soak/
allocations/full regression and full P12/P1–P12 acceptance remain open. Existing system validation enumerates
actual entities; it is not a CPU budget or platform/world-size limit. No Oil/Tycoon/host/demo/platform/Draw edits.
