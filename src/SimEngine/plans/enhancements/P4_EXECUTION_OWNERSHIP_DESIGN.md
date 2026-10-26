# P4-G71: Sole execution ownership — Design Document

## 0. Problem Statement

Original P4.acceptance.015 remains open after the old catalog-generation APIs and
native alternative constructors were removed. Its structural criterion is absence
of patches loaders, old step executors and duplicate graph/reservation/catalog
owners, not blanket proof of all runtime workflows.

## 1. Goals

1. Inspect all current production C# sources and uniquely identify actual catalog,
   topology, order/node/job and reservation-book owners.
2. Distinguish immutable authored topology, live execution, indexes, detached views
   and physically distinct inventory custody from duplicate state authority.
3. Record current source hashes and concrete alias witnesses; close only the
   original structural gate if proved. No fabricated implementation work.

## 2. Design

Keep original owners unchanged. `DependencyGraph<T>` compiles topology;
`ActivityGraph` wraps its captured metadata and node list. `Order` allocates one
`ActivityNodeExecution` per bound node; list and named index share those exact
records. `WorkBoard` partials are one type/codec, owning actual orders/jobs/plans.
`WorkSystem` executes contract-declared physical job phases; it does not own a
second graph or job store. Completion history is retained outcome evidence, not
another live progress model.

`ResourceReservationBook` is one embedded endpoint-owned primitive. Inventory,
spatial pools and optional supplier promise endpoints instantiate it for distinct
custody. Tool/dock/machine claims are their own physical dimensions, not resource
balance copies. Transfer admission vectors, snapshots and indexes are derived
facts, never another ledger. `ResolvedContent` is the only resolved/resource
catalog; raw JSON input and immutable authoring definitions are not live catalogs.

Read-only `VerifyExecutionOwners.ps1` inventories declarations, partials, current
source hashes, literal ownership aliases and obsolete declarations. Review those
actual relationships, not just zero-match regex output. Source manifest stores
current evidence. Game code and protocol2.445.0/save154/replay1 remain unchanged.

## 3. API surface

No gameplay/public API, model or serializer changes. One read-only audit command.

## 4. Files to change

New stage/evidence/verifier/source manifest. Update original gate, master tracker
and current metadata headers/hashes only. No tests, harness, demo or module edits.

## 5. Error handling matrix

Missing owner, noncanonical partial, missing actual alias witness or obsolete
declaration fails verification. Hash drift fails current-source closure. Owner
names/regex alone do not prove semantic ownership; reviewed witnesses are required.

## 6. Backward compatibility

No compatibility path; production source remains unchanged.

## 7. Verification

Execute only the new read-only ownership verifier and metadata/literal/hash/link
checks. No compile or gameplay runtime is needed for unchanged production/test
code. P2-F11's112checks remain existing scoped evidence on unchanged2.445.0, not
rerun or broadened by this stage. Structural verification is reported separately.

## 8. Risks

Independent runtime custody is not a duplicate balance, authoring is not live
progress, and indexes are not state copies. This gate does not prove all resource
accounting, atomicity, arbitrary providers, lifecycle, stress or full regression.

## 9. Out of scope

P4.010/P4.014 runtime breadth, P7.020 hidden limits/balances/old fields, other
provider/global/full gates, Oil, platforms/Draw, advanced Tycoon/G19 and publication.

## 10. Sub-task execution order

1. Inspect original ownership types, live alias/custody relationships and loader.
2. Execute reproducible source ownership audit; preserve actual manifest.
3. Close only unchanged-text P4.acceptance.015 with structural evidence if valid.
4. Reconcile current tracker and integrity anchors. Status: VERIFIED-SOURCE on
   unchanged2.445.0/save154/replay1. Actual395production sources, eight single
   model declarations plus23canonical WorkBoard partials, eleven alias/loader/
   promise witnesses; no obsolete owner/loader/executor declaration. Only original
   P4.acceptance.015 checked,221/180checked/41open. No production/test edit,
   compile or gameplay runtime rerun; broader/full goal and publication remain open.

[Evidence](../../docs/migration/P4_EXECUTION_OWNERSHIP_VERIFICATION.md)

## Actual integrity closure

1460source anchors/738selected inputs/13quickstart anchors, zero drift. All395
production hashes and all prior737selected executable inputs unchanged. One new
read-only audit script added; no game/test executable edits or runtime rerun.
221literal requirements retain text/line anchors with only P4.acceptance.015
checkbox changed.18master rows valid;2702paths/948Markdown/8544local links,
none broken. Existing positive driver boundary matches0; diff --check exit0 with
34existing line-ending warnings. Four protected clock/G19 hashes unchanged;
dirty2690/staged0. Original180checked/41open; goal/plan/publication still incomplete.
