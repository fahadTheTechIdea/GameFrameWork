# P4-G24 — Actual named signal capability bindings

## 0. Problem

AutomationSignalContracts, AutomationSystem, SignalGateCondition and ExecutionGateContracts
derive signal schemas through runtime Component/ComponentDef casts. Named entity/definition
lookups also repeatedly call live identity getters instead of using stable authored identities.
This contradicts contract-based extension and permits unrelated getter execution during lookup.

## 1. Goals

1. Existing EntityDef owns one immutable authored named-slot index, shared by metadata/runtime
   queries; no gameplay registry or separate runtime index.
2. Existing IAutomationSignalDefinition exposes its already-resolved actual SignalSchema.
3. The sole AutomationSignalContracts binding/read owner validates actual named source and
   schema through contracts; gates and automation reuse it without concrete provider casts.
4. Malformed schema aliases and changed authored identities refuse before Initialize.
5. Verify one new focused case with real physical production, native signal gates and automation.

## 2. Design

Build a Code-to-existing-slot dictionary during EntityDef.SetComponents. Named metadata,
capability checks and Entity.Get use that same index. Existing final component validation
ensures resolved named identity still matches its authored slot. No added saved IDs, clock,
entity limits or game/platform dimensions.

IAutomationSignalDefinition.SignalSchema is fulfilled by the existing ComponentDef property.
The existing ComponentDef final validator checks reference identity against its registered schema,
including unused entities. No copied schema or compatibility accessor.

AutomationSignalContracts owns schema selection by actual entity definition/identity and pure
binding acceptance. ReadFrom guards source/descriptor selection and callbacks with existing
EntityObservation; typed missing/incompatible unavailability remains. Foreign/forged sources
are not substituted. Gate authoring/evaluation and rule binding delegate to this owner.

## 3. API

Add readonly SignalSchema to existing metadata contract. Public signal snapshots/math/commands,
entity query signatures and persistence schemas remain unchanged.

## 4. Files

Defs, Entities, Operations, AutomationSignalContent, AutomationSystem, OperationsView,
SignalGateCondition, ExecutionGates; public extension proof and thin player driver; protocol
build identity; stage/evidence/master/completion audit.

## 5. Errors

Duplicate/missing/changed named metadata and non-actual schema aliases: ContentException before
Initialize. Missing/incompatible source: existing typed SignalUnavailable. Getter/callback
exception or scoped state mutation: original observation/session fault, no partial facts.

## 6. Compatibility

Development-only contract enhancement; no old selector, native fallback or parallel schema store.

## 7. Verification

Build for compilation; ONE NEW exact bounded case:
`Signal bindings: authored named metadata drives gates automation and physical production`.
Prove metadata getter usage, stable named selection, pure/private/retained views, schema/identity
refusals, metadata faults, native gates/automation, one-clock physical saved continuation/replay.
No historical/family/full runtime; all global gates remain open.

## 8. Risks

Framework components still own actual state/Sync. Removing consumer casts is not a claim that
arbitrary non-Component parts can bypass entity composition. Metadata index is not a CLR sandbox.

## 9. Out of scope

Tycoon, resource catalog migration pending authority, business/defaults, platforms and publishing.

## 10. Sequence

Canonical named index; expose/validate actual schema; migrate sole binding/read and all identified
consumers; authored extension proof/thin driver; bounded gate; actual evidence/tracker update.

Status: narrowly verified, core2.359.0/save154/replay1; one NEW bounded case152/0/0 in1448ms.
Compilation repaired a public proof's internal API reference; no runtime failure.
[Actual evidence](../../docs/migration/P4_SIGNAL_BINDINGS_VERIFICATION.md).
Full P1–P12 and goal remain OPEN; operation-schema concrete selector, arbitrary CLR/provider
sandbox, full capability decomposition, spatial intent deep immutability, all codecs and global
stress/soak/full acceptance remain unproved. Historical cases were not rerun; no publication.
