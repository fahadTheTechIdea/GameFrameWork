# P5-CB1 — Compiled Oil contract boundary

Status: verified for original P5.acceptance.002; implementation and all focused attempts
are recorded in [the verification report](../../docs/migration/P5_COMPILED_CONTRACT_BOUNDARY_VERIFICATION.md).
Full P5 and the overall plan remain open.

## 0. Problem Statement

P5.acceptance.002 remains open. The existing compiled scanner covers independent
proof modules, but not the actual Oil assembly, signature/generic/local type
references or host-facing Oil signatures. Visibility alone also cannot detect
public implementation-owner coupling such as WorkBoard/Storage/Transformer.

## 1. Goals

1. Extend the existing binary inspector, not a second auditor, to inspect all
   actual Oil methods, signatures, generic arguments, fields, locals and catches.
2. Detect nonpublic access, reflective/native bypasses and direct shared
   implementation-owner dependencies in the Oil extension.
3. Verify no host-facing Oil fact/command/factory signature exposes mutable live
   engine owners and the game facade has no public World escape.
4. Prove negative fixtures are rejected for actual reasons, without executing them.
5. Keep selected public SDK classes (module/definition/value types and explicitly
   selected TileMap provider) distinct from private/shared implementation internals.

## 2. Design

Refactor ExternalModuleBoundaryTests' current scanner to return one inspection
result used by its existing positive audit and a new Oil exact case. Load the
actual freshly compiled sibling Release OilCo.Game.dll as test-only binary data;
do not add an Oil runtime project reference to framework/proof modules.

Use actual runtime metadata/IL, not source grep or a compilation-only claim.
Type inspection recursively checks closed generic/array/by-ref/constraint shapes.
The Oil policy additionally rejects actual shared work/resource/network/production/
knowledge/plan/account implementation owners while allowing their public contracts.
Public TileMap is an explicit selected spatial SDK provider, not Godot or a claim
the Oil game supports arbitrary map types.

Generate in-memory negative metadata/IL fixtures for private field access,
reflective binding, native entrypoint, private engine generic argument and shared
implementation-owner signature; inspect only, never execute them. Audit
the actual host-facing Oil namespaces/factory for live-owner escapes, including
generic/delegate shapes and transitively returned Oil fact types.

## 3. API surface

No production API changes. The audit is test-only; portable modules do not inspect
assemblies or use reflection at runtime.

## 4. Files to change

| Action | File | Risk |
|--------|------|------|
| Modify | ExternalModuleBoundaryTests.cs | Medium: stricter sole inspector |
| New | stage verification report | Low: evidence/limits |
| Modify | P5 design/master/README/AGENTS | Low: requirement status |

## 5. Error handling matrix

| Input | Required result |
|-------|-----------------|
| Missing actual Oil assembly | Explicit failure requiring targeted build, no stub |
| Inaccessible member/type or reflective/native bypass | Inspector violation |
| Direct shared implementation-owner dependency | Oil inspector violation |
| Host fact/command exposes mutable owner | Explicit surface violation |
| Valid public SDK/content/spatial provider | Allowed; no invented dependency on a platform |

## 6. Backward compatibility

Development-only. No fake old assembly, compatibility alias or privileged friend.

## 7. Verification

Build changed test project. Reuse current actual Oil Release output only after
confirming current core/source identity, or build that target if needed. Run the
new exact Oil boundary/negative case and the existing independent module literal.
Use the existing actual Oil composition literal if runtime assembly topology needs
confirmation. No full/family/profile or long gameplay run.

## 8. Risks

Binary conformance is not gameplay/privacy/timing equivalence or a CLR security
sandbox. The explicit Oil implementation-owner policy must not prohibit public
SDK registration/definition/DTO or deliberate tile-provider use merely because
they are classes. Do not silently waive violations to make the check green.

## 9. Out of scope

Oil source edits, domain rules, platform/Draw/adapters, broad scenario/P5/P6/full
regression, Git staging/commit/push.

## 10. Sub-task execution order

1. Inspect existing checker, actual project/reference/source boundary and gate.
2. Extend the sole scanner and its negative fixtures; run selected exact checks.
3. Investigate any violation before claiming original requirement completion.
4. Record full coverage/limits and update tracker, leaving unrelated gates open.
