# P11-A8 — complete progression startup binding validation

## Outcome and original requirement

Engine2.416.0/save154/replay1. Original **P11.acceptance.002** is now checked with unchanged text: Missing/cyclic tech, invalid effect bounds and objective references fail before Initialize. Original221 records:157 checked/64 unchecked. This is not an aggregate completion claim; the full P1–P12 goal remains active and incomplete.

The original [ObjectiveSystem](../../src/Beep.SimEngine/Standard/Objectives.cs) now invokes every loaded predicate/effect binding validator, including unused content and explicit empty enrollment. Actual aliases require frozen owned content, matching identity/module and no persistent service. Definition metadata and binding callbacks use the existing observation boundary. The former reachable-only startup filter is deleted; runtime Reachable evaluation and enrollment/deadline/settlement remain original. Existing [contracts](../../src/Beep.SimEngine/Standard/ObjectiveContracts.cs) explicitly document complete startup admission. No new engine owner, state, command, schema, clock or compatibility path.

## Exact verification

ONE solution compile:

```text
dotnet build Beep.SimEngine.slnx
Build succeeded. 0 warnings, 0 errors. Exit0.16.43 seconds.
```

ONE NEW exact runtime attempt:

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Progression startup: all declared bindings bounds and references fail before initialization including unused content' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
212 passed,0 failed,0 aborted; suite1288ms.
Exited, child PID21372, exit0, observed1455ms.
stdout-truncated=False; stderr-truncated=False; observer error empty.
```

All attempts are recorded above: no failed compile/runtime attempt, repair or earlier/family/full runtime. No executable edits after FINAL.

## Requirement-matched proof

The [thin driver](../../tests/Beep.SimEngine.Tests/ProgressionBindingTests.cs) reuses the original progression fixture and ordinary player views/commands/one clock/saves. Its explicit fourteen-module [master](../../tests/ExtensionProof/content/progression-bindings-master.json) adds the [binding JSON](../../tests/ExtensionProof/content/progression-bindings.json) and [negative-only contract probe](../../tests/ExtensionProof/ProgressionBindingProof.cs). No test provider executes positive gameplay: unused Observe/Check/Apply callbacks throw if accidentally invoked. Original native/independent technology, objectives, graph, hauling and stock owners execute the healthy game.

79 negative authoring/composition variants prove rejection before the initialization probe runs:

- Native and independent technology missing prerequisite, dependency cycle, missing final effect and wrong effect contract.
- Native and independent objectives, enrolled or with empty enrollment: missing/wrong predicate, missing prerequisite, cycles and missing completion/failure effects.
- Unused logical predicate missing reference or self-cycle.
- Unused native physical inventory effect: zero, negative or fractional count-unit amounts fail the existing bill/unit admission.
- Independent predicate/effect declarations, unused/selected/empty enrollment: negative minimum, maximum above its explicitly declared domain, inverted interval, missing bound, missing provider, thrown validator and clock-mutating validator. Actual validators run exactly once where parsing succeeds; failed startup never initializes.

Valid complete binding admission occurs once even with empty enrollment and never enrolls/evaluates content. Healthy ordinary elapsed research physically supplies/holds the original tool, retains partial work through save/load, earns independent then native prerequisites, settles the original objective dependency chain and restores/replays the exact authoritative chronology. Interleaved sessions retain independent earned state and no probe callback runs as gameplay. Bound semantics remain provider-owned, not guessed by the engine.

## Source freeze and boundaries

After FINAL, UTC2026-10-10 01:52:39:564 selected executable inputs;11 changed/new versus560 previous inputs;4 new executable selections (one driver, one negative probe, one master and one add-on JSON). All selected hashes are recorded in the [machine audit](../../plans/enhancements/COMPLETION_AUDIT.json), including historical inputs refreshed without executing historical suites. Original gate wording remains unchanged; only P11.acceptance.002 changes its checkbox.

The sole clock and the three protected G19 Tycoon files remain unchanged. No Oil, Tycoon behavior/content, platform, Draw, renderer, adapter, legacy compatibility or parallel gameplay subsystem changes. Protected G19 authority is still pending. No staging, commit or push: the human condition requires full goal AND plan completion first.

Broader progression utility/provider/private purity/lifecycle/incident/controller/stress/lockstep/full regression requirements remain open. A validator can only enforce a provider's declared contract; this proof does not certify undeclared rules, malicious arbitrary CLR state, historical suites or complete P1–P12 acceptance.

## Read-only metadata and post-pass validation

UTC2026-10-10 01:55:02 UTC:1219 source anchors and13 quickstart anchors match current SHA256;2494 selected paths/858 Markdown documents/7151 local links,0 broken links.18 master rows each retain5 columns; git diff check exits0 with34 pre-existing diagnostic output lines. New thin-driver forbidden-boundary scan has0 matches.564 frozen executable inputs have0 post-FINAL drift. Actual `git status --porcelain=v1 --untracked-files=all` reports2484 dirty files; staged0. The four protected source hashes exactly match the previous checkpoint. This metadata check executes no test suite and does not convert historical evidence into current runtime proof.
