# P2-B3b — Single-owner content authoring and publication

Status: focused-verified, 2026-10-06; full P2/framework remains partial.
Parent [P2](P2_EXTENSION_CONTRACTS_DESIGN.md); uses [P4](P4_REUSABLE_MODULES_DESIGN.md).
[Evidence](../../docs/migration/P2_CONTENT_LIFECYCLE_VERIFICATION.md).

## 0. Problem

Defs are shared between sessions. Def/ComponentDef expose overridable public Read/Resolve, with
uneven per-class booleans. Some definitions can be reauthored after publication; others individually
guard only one phase. Frozen JSON and read-only lists do not protect those executable authoring paths.

## 1. Goals

1. Def and ComponentDef own non-overridable Read/Resolve; providers implement protected ReadCore/
   ResolveCore. One authoring lifecycle enforces single use, failure poison and publication freeze.
2. ResolvedContent publishes only successfully resolved/frozen definitions and components, including
   generated definitions. Engine-owned identity/layout setters and component attachment reject mutation
   after freeze. No successful second read/resolve or failed callback retry.
3. Remove obsolete per-definition _read/_resolved guard state, not retain two permission owners.
4. Runtime operating-area shapes use the same authoring lifecycle and freeze before observation;
   no manually forged resolved booleans. Generated content remains the declared catalog pipeline.
5. Prove public extension content, generated components, interleaved physical sessions, save/load and
   unchanged single-clock behavior through exactly two suites.

## 2. Design

An internal ContentAuthoring helper owns phase transitions for both base types: created, reading,
read, resolving, resolved, validating, validated, frozen, failed. Callbacks cannot recursively re-enter;
exceptions permanently reject reuse, even when a provider swallows a recursive-call refusal. Strict
JsonReader.End validation runs inside the shared Read permission, so unknown keys poison authoring too.
Constructors/generators may supply already-authored fields before first resolution;
this is explicit generation, not a failed-JSON fallback. IsFrozen is a read-only definition fact.

Base metadata setters call the same mutable-phase guard. EntityDef.SetComponents preflights fresh
component ownership before attachment and cannot replace frozen layouts. ResolvedContent resolves
units/resources/capacity/other/entity definitions in existing order, performs existing linked/sibling
validation, freezes all parts/defs, then publishes the catalog. No new registry, cache or content copy.
The obsolete optional IResolvedContentValidator path is deleted; every Def now has one mandatory
non-overridable validation entry, with protected ValidateResolvedCore. ComponentDef likewise owns
Validate with protected ValidateCore, including actual-owner validation inside its failure boundary.

SpatialRegionDef.BuildGeometry authors a fresh shape with its explicit prototype space through the
same Read/Resolve and freezes it. Containment remains the existing provider's shape logic. Existing
catalog/manifests/JSON/fingerprints and gameplay state owners remain authoritative.

## 3. API

Def and ComponentDef: public nonvirtual Read(JsonReader), Resolve(ContentResolver), bool IsFrozen;
protected abstract ReadCore(JsonReader), protected virtual ResolveCore(ContentResolver).
Existing derived core/public proof classes migrate directly, including base.ReadCore/ResolveCore calls.
Linked validation: public nonvirtual ValidateResolved(ContentResolver) and protected virtual
ValidateResolvedCore(ContentResolver). Sibling validation: public nonvirtual Validate(EntityDef) and
protected virtual ValidateCore(EntityDef). No optional validator interface survives.
No old virtual hook, alias, overload or compatibility loader.

## 4. Files

Content/Defs, Content/ResolvedContent, Content/SpatialRegionDef, shared authoring lifecycle, directly
migrated core/public/test definitions and existing authoring guards; current build/save identity;
two focused suites plus stage/evidence/master/README/AGENTS/P2/P4/offline help.

## 5. Errors

Reentry, repeat, post-resolve read, post-publication mutation and component reuse refuse before
callbacks or layout edits. Failed callbacks cannot become a publishable definition on retry. Invalid
linked/sibling content still fails through existing validators. A malicious provider that exposes
unrelated mutable state or bypasses its contract is not sandboxed by this lifecycle; global purity
and arbitrary provider reflection/immutability audits remain open.

## 6. Compatibility

Development only, breaking hooks/current build/save. No old reader or retained duplicate guards.

## 7. Verification

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Content lifecycle: single authoring owner, publication and generated metadata"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Content lifecycle: shared prototypes, physical sessions and saved continuation"
```

Only these two exact names run, including repair reruns. Old suites migrate/compile only; no full run.

## 8. Risks

Protected hooks require ordinary extension migration; this is intentional direct replacement. Freeze
must happen after cross-reference validation but before any session initialization. Generated and
runtime area geometry must not skip it. Protected methods are a cooperation contract, not a sandbox.

## 9. Out of scope

Oil/Tycoon/host/demo/platform/Draw; new game clock; global mutable-provider isolation, all protocol/
replay/fuzz/soak/allocation gates and full framework completion.

## 10. Execution

- [x] Audit actual shared authoring/publication, generators and runtime geometry.
- [x] Replace hooks/guard ownership and freeze before publication.
- [x] Build and verify two exact contracts/physical suites: 1120 + 224 assertions, zero final failures.
- [x] Record evidence, update sole tracker and keep broader gates open.

Build2.167.0/save94; all existing runtime service/system/view schemas unchanged. Clean build, no
warnings/errors. Only these two suite names ran, including repairs. 123 hook-migration files and
30 sibling-validation migration files overlap; they are not 153 independent classes. 81 older test
metadata files migrate current build/save literals and compile only. Native rectangular/painted and
public point geometry, generated component metadata, failed/reentrant authoring, eight native/public
response compositions, physical inputs, saved twins, shared-catalog session isolation and sole-clock
pause pass. This does not prove arbitrary provider mutators, global purity or full regression.
