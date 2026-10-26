# P4-G4c — Captured contract-based activity plan discovery

## 0. Problem

WorkBoard's mutable delegate resolver list and construction/line key-prefix switches return the
first match. Component activity sources validate independently from lookup. A valid external plan
contract cannot enter normal commands/saves without editing resolver code; duplicate keys can be
hidden. This violates declared contract/content ownership and the sole authoritative board.

## 1. Goals

1. Delete RegisterPlanResolver/delegate list/native prefix lookup switches. Reuse IActivitySource
   for declared service providers alongside selected ActivityDef and compatible entity components.
2. WorkBoard owns ONE derived immutable key-to-definition alias index, captured before its other
   composition validators and Initialize. IActivityCatalog aliases SAME board, not a new service/store.
3. Canonical ordinal keys, duplicate different-definition identity refusal, null/invalid collection/
   definition refusal, alias-deduplicated source collection. Resolve never invokes runtime providers.
4. Validate every captured plan through existing work admission. Post requires SAME catalog definition
   or SAME provider-bound actual target execution; no unregistered plan fallback.
5. Native construction/removal/line and custom JSON/content-only plan declarations use same public
   plans/commands/views/saves. No second scheduler, graph, resource owner or game clock.

## 2. Design

WorkPlanCatalog.cs is a partial of WorkBoard. Capture selected ActivityDef; compatible EntityDef
components implementing IActivitySource/IActivityDefinition; declared Services.Providers<IActivitySource>.
Use existing provider alias discovery. Collect same immutable definitions once, then sorted keys.
A native LineService implements IActivitySource exposing selected compatible LineDefs once. Its
native geometry/runtime ownership remains unchanged. Construction component sources already expose
build/removal plans, so native module resolver functions disappear.

IActivityCatalog has PlanKeys and ResolvePlan. WorkModule binds board alias. Captured lookup checks
identity still matches; not a runtime callback. WorkBoard.Post obtains existing BindExecution for
actual target and requires exact alias before jobs/claims/IDs mutate. Sync uses same captured lookup;
bytes/schema remain unchanged with strict build identity. No legacy reader or default fallback.

Engine-side mod proof defines JSON plan binding referencing SAME ActivityDef profile, declares source
through canonical public ActivityWorkerSelection Read/Check/Identity/ValidateGraph APIs (existing
implementations exposed, not another parser/selector or friend-assembly workaround), and
through normal module service aliases, and supplies ordinary target-intent contract. Driver only
issues public SubmitPlan/CancelPlan and advances/views/saves. Duplicate/null sources are composition
negative probes, not gameplay. Native sources remain covered by actual ordinary line execution.

## 3. Files and errors

WorkPlanCatalog/WorkContracts/Work/WorkModule/ConstructionModule/InfrastructureModule/Networks/SaveGame;
engine-side extension proof/content/master/startup/runner and metadata source migrations; current
tracker/P4/stage/AGENTS/scoped evidence. No Oil/host/demo/platform/Draw/business changes.

Null/duplicate/unregistered/mismatched definitions fail explicitly before initialization or posting.
One catalog index is immutable composition metadata, not another authored content registry or ledger.
Broader cross-owner provider purity/mutable fake contract/fault/ETA/team/process/haul gates remain open.

## 4. Verification and sequencing

- [x] Capture/delegate deletion/native source migration and exact posting admission.
- [x] JSON external contract proof through existing public engine flows.
- [x] Build completed slice; ONE exact distinct NEW bounded case; repair only observed failures.
- [x] After final green freeze code/public proof/tests/JSON; docs-only evidence/trackers/hash-link inventory.

Verified2026-10-08,build2.243.0/save139/all bytes unchanged. TWO clean builds5.50s/4.46s; TWO runs
of ONE exact NEW bounded case: first2 passed1 aborted127ms (home startup dependency); repair
authored JSON only, final81 checks0 failures/aborts468ms.109 older assertion files/lines compile
ONLY. [Evidence](../../docs/migration/P4_DECLARED_PLAN_CATALOG_VERIFICATION.md). Source/test/JSON
frozen after green; broader declarations/provider/target/generated/durability/full gates remain open.

Record EVERY invocation. Older metadata/raw fixtures compile/source ONLY. No older/family/full/
platform/stale gates. Full G3/G4/G5/P1–P12 stay open; commit/push ONLY after FULL completion/verification.

