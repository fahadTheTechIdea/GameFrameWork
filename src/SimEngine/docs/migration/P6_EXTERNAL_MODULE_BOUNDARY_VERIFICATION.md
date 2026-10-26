# P6-I1 external module independence — focused evidence

Date2026-10-15. Engine2.604.0/native save159/replay1. No persistent payload change.

## Changes and original owners

The compiled ExtensionProof assembly contained LineCursorCorruption, which used
Type.GetField/FieldInfo.SetValue to alter private LineWork fields. This contradicted
the original external-mod independence requirement, despite having no friendship.
The sole helper is now in Tests/Probes, namespace Beep.SimEngine.Tests; its old
definition/import is removed, not retained as a shim. Existing corruption consumers
compile against this one test-only implementation.

Legitimate IndependentLineWorkDef/AuthoredLineCursorModule and IndependentLineEffectDef
remain in the separate non-friend assembly. The existing line-effect module receives
an explicit Action<LineWork> only for its declared live-cursor negative probe. The
callback contains test corruption, not gameplay. A missing selected dependency
refuses before Initialize; a normal parameterless module needs no callback. No
static hook, public engine corruption API or copied state/scheduler is introduced.

The binary audit inspects all method/constructor bodies in ExtensionProof and the
public quickstart module. It checks runtime/framework-only dependencies for these
specific artifacts, no engine friendship, no nonpublic engine member access,
reflective binding/invocation or native/indirect calls. Unresolved tokens fail.
Embedded JSON resource access and public/protected extension APIs are legitimate.
This is an audit of these binaries, not a sandbox or policy forbidding arbitrary
future mods from using third-party libraries.

## Initialization defect found by the affected physical case

The physical line workflow exposed a real initialization regression: an empty
initial inventory called the whole-entity transaction observer while the new line
cursor was still awaiting its creation binding. Its original codec correctly
rejected that incomplete cursor.

ResourceTransactions.InitializeStock now first requires a healthy registered entity,
explicit initial bill and actual empty inventory/book. An empty authored bill then
returns without a resource mutation, fact or unrelated capability observation.
Nonempty initial stock still follows the original fully validated transaction path.
No invalid cursor, ignored observer error, invented material or alternate initializer
is accepted. The line proof now completes native/direct supplied construction and
saved continuation; the generic nonempty mapless startup remains green.

The affected older test also migrated save154 to the independent current protocol
expectation and foreign readiness from NotOwned to the existing privacy-preserving
NotFound. Production privacy rules were not weakened to satisfy old assertions.

## Commands and every attempt

Each build was only the test project, `--no-restore -v minimal`, completed before runtime.
Each run was one literal with external60000ms/termination5000ms/output7500 chars:

1. Framework independence: compiled external modules use public engine contracts only
2. Native startup: nonspatial work, process and resource contracts with saved continuation
3. Independent tile construction: physical material supply and native project completion
4. Line effects: direct master JSON earns physical segments and whole-order completion

| Attempt | Actual result |
|---|---|
| New audit build | Clean13.82s; session74592 terminal |
| Audit RED | 9/1,202ms; PID31868 exit1/460ms.5997 bodies/30098 member references in ExtensionProof, four private reflection binding calls; sample5/15 with zero violations. |
| Move helper / explicit injection build | Clean44.79s; session18392 terminal |
| Retain normal parameterless module construction | Clean32.45s; session43381 terminal |
| First audit GREEN | 10/0,450ms; PID20748 exit0/1124ms |
| First mapless startup | 72/0,2789ms; PID29268 exit0/3594ms |
| First independent tile construction | 121/0,5149ms; PID8360 exit0/5792ms |
| Affected line workflow | 10/1,1608ms; PID24456 exit1/2135ms. Empty initial inventory observed an unbound line cursor; original observation fault retained. |
| Empty-bill no-op / build identity | Clean20.26s; session9901 terminal |
| Audit FINAL | **10/0**,176ms; PID31672 exit0/401ms. ExtensionProof5999 bodies/30102 references, sample5/15, zero violations. |
| Mapless startup FINAL | **72/0**,793ms; PID25928 exit0/985ms |
| Independent tile construction FINAL | **121/0**,1327ms; PID22168 exit0/1557ms |
| Affected line workflow, privacy assertions | 134/2,4139ms; PID16192 exit1/4370ms. Existing foreign readiness returns NotFound rather than obsolete NotOwned. |
| Privacy assertion / missing dependency proof build | Clean7.17s |
| Line workflow FINAL | **138/0**,3588ms; PID12040 exit0/3775ms |

Five clean builds, zero warnings/errors. No external timeout, output truncation or
process restart; every handle terminal. No executable edits after final line run.
Only the affected line test changed in the last build; other final tested artifacts/
algorithms were unchanged, so those literal cases were not redundantly rerun.
Four distinct passing literals341 checks, not a full regression count.

## Scope and remaining requirements

Current evidence covers actual compiled independence of two non-friend artifacts,
mapless resource/work/process startup, independent tile supplied construction and
native/direct line work, saved continuations/replay in those selected cases, explicit
negative callback refusal and actual private cursor fault detection.

Original P6.acceptance.008 remains open: its complete nonspatial technology/objective
and policy scenario, broader spatial congestion/alternative-role/replanning and all
third-party worker/project/endpoint/signal/response combinations are not established
by these four cases. General nonempty-inventory initialization while arbitrary other
capabilities are unbound is not proven by the empty-bill correction. Other historical
line-corruption callers compile but were not all executed. No arbitrary code sandbox,
cross-platform execution or full performance/robustness claim.

Original26 recorded open gates/full goal remain open; no original checkbox/frozen
audit rewrite, full/profile/family runtime, Oil/Tycoon/host/platform/renderer work,
staging, commit or push. Godot/Unity draw remains outside this implementation.
