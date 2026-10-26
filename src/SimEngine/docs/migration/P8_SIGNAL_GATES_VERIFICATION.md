# P8-B1 — Authored signal-backed transformation gates

## Current M158 contract-authored transformation gates — 2026-10-25

Observed engine2.775.0/save161/replay2, dirty master based on
cb61483ab81404c11944931aedb4703ba694edb5. Only Beep.SimEngine was edited.
Raw logs and current source/binary hashes:
`artifacts/optional-module-durability/m158-transformation-gate-contracts/`.

One original gate runtime now reads ISignalTransformationGateDefinition through the shared
SignalTransformationGateDeclaration and TransformationGateBindingContracts. Native and independent
metadata do not require a native definition subclass. Composition validates all loaded bindings;
live reads use original observation and SignalGateCondition evaluation. No authoritative payload,
gate codec, resource quantity, duration, provider state, clock or scheduler was duplicated.

Exact invocation for each named case:
`dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact '<case>' --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 25000`.
No broad filter/profile/family/full/soak test was executed.

| Exact case | Final observed result | Raw evidence |
|---|---|---|
| Transformation gates: independent JSON metadata shares native timed execution and replay | 26/0; PID10916; body356ms/process512ms; exit0 | independent-4.log (build4) |
| Transformation gates: authored signals, named contracts and private previews | 64/0; PID2676; body287ms/process442ms; exit0 | native-contracts.log (build4) |
| Transformation gates: timed waits, cancellation and saved provider faults | 99/0; PID30292; body436ms/process589ms; exit0 | native-execution-2.log (build5) |

Final scoped aggregate189/0, zero aborts/truncations/observer errors. Not a whole-framework
regression or a claim that every test ran on build5. Native/proof source unchanged between
build4 and build5; only the existing negative test history inspection was repaired. The two
green cases were not rerun after that test-only correction. One actual ReplayVerifier verifies
the new independent-metadata run and requires ActualHash equal the actual final live hash;
the hash/checkpoint count is not printed and is not invented here.

Retained attempts: independent-1.log0/1 (old fixture added two tools but no explicit tool route);
independent-2.log0/1 (new assertion used singular lookup for named metadata); independent-3.log25/0
(then strengthened unused-content checks, not promoted as final); native-execution.log46/1
(old negative helper attempted a prohibited public view read after first fault). No failure hidden.
Final negative helper hashes the actual original journal codec before/after fault, retaining the
no-success-prefix guarantee, and explicitly tests public flow-read refusal plus no pending event,
no save/hash and no fabricated result/claims. This is negative protocol inspection, not gameplay
or a valid whole-world checkpoint after a fault.

Five necessary targeted builds of tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj,
Release/--no-restore, all clean: 9.06s/6.52s/6.25s/5.43s/5.60s. Subsequent builds were test-fixture
repairs, not recurring regression runs. build-1.log through build-5.log retain full output.

Metadata-only profile inspection:1108 registered/1087 included/21 explicitly excluded,
zero runtime suite bodies. Current native DLL SHA256
`EBA58516F8E64B5FBDAD07022F3C77B8CB1350B906567D77C1834CAEA17C937D`.
final-inputs.json captures seven selected source/config and five current binary/PDB
identities, not a whole-repository freeze. Original COMPLETION_AUDIT.json SHA256 remains
`022C593B63C9F77DBAC7A137B9FD89BE15A79FA752BAC9B2DE1A10804EA5AA14`;
it is historical and unchanged, with the two human-authorized current amendments in P6/P7.

Self-review: public-only proof metadata lives in ExtensionProof and only delegates JSON authoring;
all gates, timing, material holds, production and decisions remain engine-owned. The new test's
positive path issues normal commands/updates and reads facts/saves/replay; unused malformed
prototypes are checked against specific owner/reason, with an unused valid control. Native existing
negative tests retain explicit fault instrumentation. Frozen identities and condition aliases are
validated before tick one even when a mod omits its own validation override. No blanket catch,
silent success, compatibility shim or second model. Existing World first-fault/event/journal/replay
owners retain diagnostic responsibility. Broader operational sink/audit/notification foundations
remain as recorded in the master tracker; this gate refactor does not claim to implement them.

P8.017/P10.022 and other unrelated feature requirements remain open. Human acceptance amendments
are in P6; quality/lot is deferred, not implemented. Previous M153 resource mutation census is
historical; no new global census, every-provider purity, stress or allocation qualification is claimed.
No Oil/host/demo/platform/business edit, staging, commit or push occurred.

## Historical B1 evidence

Date: 2026-10-05. Standalone framework and public engine-side proof only.
Focused slice verified; full P8/P1–P12 implementation and regression remain partial.

## Actual implementation and ownership

Build **beep-simengine-2.81.0/save51**; transformation management view5. Native transformer4,
TransformerManager2, resource-flow5 and existing inventory/source layouts are unchanged.
SignalTransformationGate uses component schema1 and empty component Sync: authored gates own
no utility balance, progress, promise, timer, policy state or extra service/system codec.

ITransformationGate now extends INamedEntityCapability and returns immutable ReadGate facts.
The former Code-only callback is removed, with no old overload/default/compatibility path.
TransformationContracts.ReadGates is the sole shared evaluator for execution and private views.
It reads every gate in stable capability order, even behind an earlier ordinary refusal, validates
identity/applicability/condition/source/schema facts, and guards actual producer version/company,
live reference and existing entity identity sequence around pure callbacks. This is scoped purity
validation, not global detection or rollback of arbitrary hostile module side effects.

Explicit master-listed JSON authors gate identity, supported transformations and named conditions.
Each condition selects an actual local named signal provider and catalog signal, plus an authored
blocker. Boolean expected values and inclusive numeric min/max/exact unit are explicit. Actual
AutomationSignalDef.ReadFrom retains sole signal validation ownership. Unavailable signals keep
their typed source blocker; no fabricated zero, inferred source/unit or always-ready utility.
Same-entity signal projection is not automatically inferred to be a forbidden dependency cycle.

Native Transformer gates startup, running work and completion through the existing context. A
refusal preserves actual queue, WIP, reusable tools and earned progress. Returning/cancellation
continues through actual resource capacity/disposition independently of production readiness.
Management ReadGates exposes detached company-private facts using that same evaluator; ordinary
commands change actual source state. Saves restore sources and pending commands, not gate caches.
Existing automation owns hysteresis, cooldown and next-step controls; gates add no second controller.

The public engine-side PulseTransformer migrates directly to named gate snapshots and composes
with the native authored gate. TransformationGateProof reuses the actual AutomationProbe provider,
its existing signal/control contract and Sync; its module adds no Update loop or copied signal store.
Tests only submit player controls, bound tick stages, inspect facts and inject provider faults.

## Executed final gates

Working directory: `C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine`.

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Transformation gates: authored signals, named contracts and private previews"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Transformation gates: timed waits, cancellation and saved provider faults"
```

- Final build: zero warnings, zero errors, 2.50s.
- Authored contracts/private previews: 64 passed, 0 failed, 199ms.
- Timed waits/cancellation/saved faults: 91 passed, 0 failed, 269ms.
- Total: **155 checks in exactly two suite names**, zero failures/aborted suites.

Repair/extension repeats used only these names. All other suites migrate build/save expectations
and compile only; no third, family, full, platform or host gameplay gate ran. Historical counts
in predecessor documents were not rerun and are not current regression claims. Lifecycle skill
guidance aligned design, single owners, scoped verification and tracker evidence.

## Demonstrated behavior

Nineteen invalid authoring cases refuse missing/duplicate identities, unsupported/unknown
transformations, missing/wrong signal sources, incompatible schemas, malformed boolean/range
combinations and wrong units. Named native/independent gates compose in stable order. Private,
pure, detached views expose complete typed facts, exact inclusive boundaries and unavailability.
Unrelated supported transformations remain outside an explicitly scoped gate.

Blocked startup allocates no material/claims/progress/history; valid blocked intent remains queued.
Saved startup waits and pending ordinary operation controls continue identically. Mid-batch loss
preserves real WIP/tools/work; source recovery completes once. Completed-output waits preserve
earned work; reopening commits one result, and repeats reacquire requirements. Unavailable source
does not prevent physical cancellation. Full storage retains Returning and actual tool holds across
save/load; capacity release permits return even while utility readiness remains unavailable.
Independent timed providers use the same authored gating. Interleaved saved twins remain isolated.

Eight normal-Update faults cover malformed-unit and pure-mutating signal providers during work
and behind earlier ordinary refusal, plus mutating/null/wrong-identity/inapplicable-blocked custom
gate snapshots. No queued success prefix, extra result or new claim publishes; faulted sessions
cannot save/hash. There is no claim of rollback or recoverable arbitrary-code atomicity.

## Remaining gates

This is local signal-backed process admission, **not a physical utility solver**. The proof utility
level is an actual independent signal provider, not production/draw/elapsed-rate consumption.
Physical utility supply, remote bindings, worker/service reservations, broader expressions,
ports/catalysts/disposition/continuous processes and arbitrary result providers remain open.
Priced utility-gate combinations, global hostile-provider side effects, broad replay/fuzz/stress,
performance and full P8/P1–P12 acceptance were not behaviorally verified. Goal remains active/partial.
