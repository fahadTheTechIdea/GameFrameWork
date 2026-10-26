# P4-G35 — Contract-owned authored content access

## 0. Problem statement

Standard/ContentAccess.cs selects the abstract ContentAccessDefinition hierarchy and creates an
unregistered runtime basis. ContentAccessPolicy invokes basis/rule checks and rule metadata without
the original provider observation guard. Independent JSON policies need no inheritance requirement,
detached stateful factory, second access registry or host admission logic.

## 1. Goals

1. Select exactly one immutable IContentAccessDefinition through existing content aliases.
2. Remove ContentAccessDefinition/Create entirely; native and independent definitions evaluate directly.
3. Preserve ContentAccessPolicy as the sole ordering/visibility/admission owner and technology rules.
4. Guard actual definition and service-rule observations using original codec/clock/identity/map scopes.
5. Prove JSON-driven restrictions and physical work with ordinary player commands, saved twins and replay.

## 2. Design

IContentAccessDefinition extends IContentDefinition and IContentAccess. Authored definitions are frozen
actual Def aliases, not stateful services or factories. Stateful extensions remain declared
IContentAccessRule services, whose existing IPersistentService registration owns their codec once.
ContentAccessModule validates the selected actual frozen alias. ContentAccessPolicy captures rule IDs
under EntityObservation.ReadProvider, sorts those immutable binding IDs, checks unique current-session
ownership, and verifies each identity again around its guarded Check. The existing optional actual
TileMap guard observes map invalidation. No clock, learned-state, access cache or parallel admission path.

## 3. API surface

New IContentAccessDefinition : IContentDefinition, IContentAccess. ContentAccessPolicy constructor
accepts that definition. OpenContentAccessDef is a direct Def implementing it. Existing
IContentAccess/IContentAccessRule retain their public signatures. Delete the abstract factory base.

## 4. Files to change

Standard/ContentAccess.cs; existing ExtensionProof/ProcessProof.cs declaration; new public-contract
engine proof and thin player test; Program/protocol/build identity; stage/evidence/master/audit.

## 5. Error handling

Absent/multiple/foreign/unfrozen/stateful definition aliases and duplicate/foreign rule metadata fail
composition before Initialize. Ordinary refusals keep typed codes and allocate no accepted work.
Observed callback exceptions/state/map/clock/identity mutation fault the session before publishing a
result. No implicit open policy, fallback, substituted identity or compensation.

## 6. Compatibility

Development only; no compatibility hierarchy/factory/old reader. Existing save154/replay1 owners remain;
advance build identity for changed simulation semantics.

## 7. Verification

ONE NEW exact bounded case: Content access contracts: authored policy governs physical work.
Native and independent JSON selection; preview/command parity; physical hauling/construction and
research gates where authored; interrupted save/twins/pause/replay/isolation; malformed composition;
scoped basis/rule fault injection. Build only for compilation, no historical/full/family/Oil/Tycoon run.

## 8. Risks

Scoped observation is not a whole-world/CLR sandbox. Immutable definitions cannot own per-session
mutable gameplay; that belongs to declared rule services with existing state ownership. Unrelated
providers, stress/performance/full-accounting and full P1-P12 gates remain unproved.

## 9. Out of scope

Tycoon and pending G19 authority, Draw/platforms/demos, new work or research schedulers/state stores.
Commit and push remain deferred until the complete original goal/plan is proved.

## 10. Sub-task order

Inspect owners; document stage; replace selector/factory; guard rule binding/actual observations;
migrate independent proof; new ordinary player case; compile and bounded exact execution; record
precise evidence and refresh tracker without claiming historical runtime coverage.

## 11. Status

Implemented direct content policy selection, deleted the detached factory path and guarded original
access owner/rule metadata/checks, including recursive reads. Core2.372.0/save154/replay1.
ONE NEW exact case FINAL275/0/0 in5782ms;191 selected inputs frozen12:40:53UTC.
[Scoped evidence](../../docs/migration/P4_CONTENT_ACCESS_CONTRACTS_VERIFICATION.md).
Full P1-P12, other providers and authorized broad gates remain OPEN; no publication.
