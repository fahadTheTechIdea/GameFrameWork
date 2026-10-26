# P6-I1: Compiled external module independence

Status: implemented/focused-verified on2.604.0/save159; four distinct literals341
checks. [All attempts and limits](../../docs/migration/P6_EXTERNAL_MODULE_BOUNDARY_VERIFICATION.md).
Original broader P6 acceptance/full goal remain open.

## 0. Problem

Original P6.acceptance.008 requires external proofs without oil/internal access.
AuthoredLineCursorProof currently puts private LineWork field corruption through
reflection in the non-friend ExtensionProof assembly. LineEffectContractsProof calls
that helper for an explicitly negative test. Legitimate metadata and negative
privileged corruption must not share that dependency.

## 1. Goals

1. Keep legitimate independent cursor/effect metadata in its separate non-friend assembly.
2. Move private-field corruption to test-only instrumentation, with one definition.
3. Inject only the explicit negative mutation callback; no normal gameplay in drivers.
4. Audit compiled proof/sample assemblies for engine/runtime-only references,
   no engine friendship, reflective binding, private engine calls or native entry points.
5. Run selected real nonspatial/spatial external scenarios against current code.

## 2. Design

LineCursorCorruption moves to the test assembly namespace. The public-only effect
proof accepts a constructor-injected Action<LineWork> used only for its declared
live-cursor negative probe; require it explicitly when that probe is selected.
No static hook, public engine corruption API, compatibility alias or copied helper.
Unfaulted modules need no injected callback. All work/stock/timing remain in engine.

One test-side compiled IL audit walks all methods and constructors of ExtensionProof
and QuickGame.Module. It resolves member tokens and rejects nonpublic engine access,
reflective field/method/property/constructor binding/invocation, native calls and
non-framework/runtime references in these two particular proof artifacts. Public/
protected extension APIs and embedded JSON resource access remain legitimate.
This is not a runtime sandbox or a restriction on all future mods' libraries.

## 3. API

No production engine API change. An optional negative-probe callback on the existing
test module; explicit refusal if a selected corruption probe lacks its dependency.

## 4. Files

- ExtensionProof/AuthoredLineCursorProof.cs: retain independent metadata, remove private writer.
- ExtensionProof/LineEffectContractsProof.cs: explicit per-module negative callback.
- Tests/Probes/LineCursorCorruption.cs: sole privileged corruption implementation.
- LineEffectContractTests.cs: supply callback from test instrumentation.
- ExternalModuleBoundaryTests.cs / Program.cs: compiled independence audit.
- Tracker/evidence: actual current scope, not closure of all original A/B/C requirements.
- Standard/ResourceTransactions.cs: empty authored initial bill is a healthy validated
  no-op, not a whole-entity observation of capabilities still receiving creation bindings.
- SaveGame.cs / current protocol: build identity only, no saved payload change.

## 5. Errors

Missing negative callback refuses explicitly. Audit identifies exact offending methods
and member references; unresolved IL tokens fail, never silently skip. Retained fault
tests stay privileged and clearly separate from portable game module guarantees.

## 6. Compatibility

Development-only; no old helper alias or private-access fallback.

## 7. Verification

New audit RED before cleanup; targeted build; audit plus selected exact nonspatial,
spatial and affected negative-probe cases. External watchdog per literal only; no
full/profile/family run. Record every attempt and any unverified scenario gaps.

## 8. Risks

An IL audit is evidence for these compiled managed artifacts, not arbitrary malicious
code isolation. Embedded JSON resource loading is permitted, not private reflection.

## 9. Out of scope

Oil/Tycoon/platforms/hosts, new execution/resource models, whole P6 gate closure or publication.

## 10. Order

1. Capture compiled private-binding violation.
2. Separate sole privileged helper and explicit test injection.
3. Verify selected runtime cases and update authoritative tracking honestly.
