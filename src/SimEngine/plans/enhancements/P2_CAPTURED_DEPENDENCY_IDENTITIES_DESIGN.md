# P2 / P4 — Captured dependency identities

## 0. Problem

The sole [DependencyGraph](../../src/Beep.SimEngine/Kernel/DependencyGraph.cs) captures dictionary keys
and edges at construction but formerly reread live `IDependencyNode.Id` during critical-path analysis.
An external provider changing its identity could invalidate an already validated graph. The compiler
also read identity/dependency getters multiple times during capture.

## 1. Goals

Capture each node identity and dependency declaration once; retain canonical validated identities for
all topology calculations; preserve provider aliases without allowing live metadata to redefine keys.
Prove ordinal order, exact unbounded weights, immutable results and refusal semantics in one new case.

## 2. Design and API

Extend the existing compiler with `IReadOnlyList<string> NodeIds`, in the same topological order as
`Nodes`. This is captured graph metadata, not a second catalog, graph or execution state. `Find` still
returns the original provider. CriticalPath uses captured keys only. Consumers wanting live provider
behavior still use Nodes; they must not infer captured topology from a provider's later mutable Id.
No scheduler, clock, entity, save layout, activity executor or JSON schema changes.

## 3. Files

Change Kernel/DependencyGraph.cs and the engine build identity; update the independently authored
current-engine-protocol test baseline. Add one public external mutable-node proof, one exact test
registration and focused evidence. No game content or business changes.

## 4. Errors and compatibility

Existing empty/duplicate/missing/self/cycle validation remains. Original keys remain required in
weight maps. No aliases for replacement keys, compatibility resolver or substituted defaults.
Build changes to2.304.0; save144/replay1 and module codecs unchanged. Prior-build saves refuse normally.

## 5. Verification

Build once after the slice, then one distinct new bounded exact case:
`Dependency contracts: captured identity, canonical topology and external provider mutation`.
No older, family, full, Oil, Tycoon or platform runtime gate. Freeze executable inputs after green.

## 6. Scope and sequence

1. Correct capture and critical-path calculation in the existing compiler.
2. Compile an external provider without friendship and verify the single focused case.
3. Record actual evidence and update the master. Whole P2/P4/P6/P1–P12 acceptance stays open.

The lifecycle skill informs staged evidence, not permission for a broad test run. Provider behavior
outside captured topology, distributed acquisition, privacy, physical work and full completion are
not proved by this compiler-only case.

## 7. Recorded status

- [x] Existing compiler corrected; canonical captured identities own analysis.
- [x] External provider compiles without friendship/reflection/copied source.
- [x] ONE new bounded exact case:35 passed,0 failed/aborted on first attempt.
- [x] ONE clean12.02s build; executable inputs frozen2026-10-08 15:54:09 UTC.
- [x] Master, parent designs, specification and scoped evidence reconciled.
- [ ] Full P2/P4/P6 and P1–P12 requirement-by-requirement completion audit and acceptance.

[Actual verification](../../docs/migration/P2_CAPTURED_DEPENDENCY_IDENTITIES_VERIFICATION.md).
