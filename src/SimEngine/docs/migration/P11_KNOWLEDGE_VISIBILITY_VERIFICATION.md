# P11-D2 — Knowledge-derived visibility evidence

Date:2026-10-06. Current dirty working-source evidence, not committed HEAD or full regression.
Only Beep.SimEngine; no Oil/Tycoon/host gameplay/platform/Draw changes.

## Identity and owners

Build2.93.0/save61, native codecs unchanged. No added visibility state/codec or legacy contract path.
Existing IVisibility gains typed CanSeeContent alongside entity CanSee. One optional native policy
derives facts from the sole CompanyKnowledgeService; current GameSnapshot/count/inspect/diff owners
continue to project actual visibility. Existing scoped private map cursors are unchanged, not rebuilt.

Master-listed knowledge_visibility_policies selects explicit own_entities/unlisted_entities/
unlisted_content/rules. Scenario settings select the policy; no implicit enrollment or default fog.
Native typed entity/activity/transformation rules resolve exact catalog aliases and explicit all/any
knowledge_facts. IVisibilityTarget/VisibilityTargetDef is open for authored provider predicates in the
same catalog. Multiple matching rules are conjunctive; unlisted behavior is explicit. Own-entity
observation is not a catalogue grant. Exactly PlayerId.None is observer authority.

Player catalogue/resource/unit/capacity queries and resource name/identity lookups share content
visibility. Native technology/recipe/template/footprint/storage/recovery definition lookups reuse it.
ContentAccessPolicy checks discovery before other access rules; activity/construction/recipe native
admission checks hidden definitions before capability/bounds/compatibility details. Visible definitions
can remain technology-locked. Standard inventory stock inspector labels/balances respect discovery;
the public stock endpoint proof uses the same public PlayerView resource lookup contract.

Public StockVisibilityTargetDef reads the typed historical native stock observation, not current
inventory. It composes through the actual optional module's content rules, not a second evaluator,
provider registry, scheduler or learned table. Physical work/effects remain existing WorkBoard/Sync.
No view callback grants discovery or fabricates stock.

## Exact gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Visibility: authored knowledge contracts and hidden definition admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Visibility: physical discovery, scoped snapshots and saved continuation"
```

Focused gate:65 contracts/40 physical =105 checks/exactly two suites; zero final failures/aborts.
Final clean build2.21s, zero warnings/errors;404ms contracts,395ms physical. Only these exact names repeat
during repair. All other suite identities migrate/compile only; no third/family/full run. Bounded
900-update physical waits report tick and native order state/blockers, not an engine game/world cap.

One initial identity assertion reused a factory whose immutable catalog objects are legitimately
shared across sessions. The fixture now loads an independent catalog for foreign alias rejection;
engine identity rules were not changed to forbid safe sharing. A test-only snapshot API assumption
was corrected to the actual private transformer view. No gameplay shortcut was introduced.

## Proven

- Optional JSON policy/rule provenance, sole nonpersistent visibility owner, explicit own/unlisted
  behavior, native entity/activity homonym identities, private catalogues and exact observer authority.
- Unknown/hidden research and prototype admission share UnknownDef; hidden construction returns no
  bounds detail, hidden research returns no requirement graph, hidden resource lookup clears identity.
- Missing settings/policy/flags/rules/facts/targets, duplicate rules, empty fact sets and bad modes fail
  before Initialize. Foreign world/independent catalog and invalid negative viewers refuse.
- Existing native recipe provider menus/output/gate/rollout/enqueue previews hide undiscovered recipe
  details; normal queue command receives the same refusal and creates no execution. Real discovery
  reveals recipe/output but still preserves the separate technology lock.
- Public resource predicate hides catalogue/name/store facts and native endpoint stock inspect labels
  until the actual typed observation is earned. Inspector facts appear after normal physical work.
- Real sample consumes8 materials, leaves12 native supply and reveals the exact previously hidden
  foreign entity. Native count/snapshot/first-entered-sight/inspect agree; repeated capture does not
  duplicate deltas. Actual removal yields the existing private removal delta, no phantom entity.
- Retained snapshots/catalogues stay detached. Other company menus remain unknown, even if authored
  own-entity observation allows its existing object. Visibility grants no technology prerequisite.
- Partial physical save/twin continuation and completed learned visibility saves agree. Another
  session remains unknown. Two-fact all waits while any admits one genuine observation.
- Optional no-work/no-space game composes, scopes snapshots and saves without compulsory map/work rules.

## Remaining scope

This is not proof of perfect information-flow confidentiality. Cell-level/partial-layer discovery,
route/fleet geometry, definition relationship payloads, already executing recipe snapshots/automatic
recipes, arbitrary custom features, event/ID/clock side channels and full metadata auditing remain open.
Dynamic survey geometry/refresh/forgetting/sharing, pressure/risk/recovery strategy, provider mutation/
CPU/reentrancy/fault matrices, replay/lockstep/fuzz/stress/allocation and full P11/P1–P12 acceptance stay
open. Other changed lookup paths compile but only directly exercised routes above have current proof.
No full regression, oil extraction migration or platform rendering is claimed.
