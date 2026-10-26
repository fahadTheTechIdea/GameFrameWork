# Complete standalone engine audit — 2026-10-04

## Scope and method

Historical planning audit: the source inventory and line references below predate implementation.
The master and [2.12.0 evidence](../../docs/migration/P2_BINDINGS_LIFECYCLE_VERIFICATION.md) supersede
its current-status claims. Latest scope removes Draw from SimEngine: Initialize/Update publish simulation
data; platform drawing is deferred. The old RenderSnapshot path in this inventory is now GameSnapshot,
using the existing EntityView projection. Do not interpret this preserved audit as the active API.

Read all **76 runtime/project files (12,313 lines)** and all **32 test/proof files (4,437 lines)**:
**108 files / 16,750 lines**. [Hashed inventory](ENGINE_AUDIT_INVENTORY.json) records each file.
Build properties/targets/solution, AGENTS, README, extraction verifier and active plans were also read.
Generated bin/obj/recovery are not active engine code. This is a standalone audit, not a claim that
all current OilCo.Game, Tycoon, clients or older sibling classes were re-audited. P5 requires its own
source-specific inspection. No runtime/source/test implementation changed in this planning revision.

## Current baseline

`dotnet build Beep.SimEngine.slnx --no-restore`: **exit 1**, 0 warnings, 1 error, elapsed **4.94 s**.
Runtime/test-support compile. External proof fails **CS0103** at
[ContractSchemas.cs](../../tests/ExtensionProof/ContractSchemas.cs), line 39, unknown JsonParser.
Current parser is Json.Parse. Source declares **2.11.0/save 7**; binding proof is incomplete/unregistered.
Do not substitute an old --no-build binary as current evidence.

Historical **2.10.0/save 6**: framework **953 checks/84 suites**, zero failures/skips, **0.7 s**.
Full oil session **82663** finished exit 0: **220/24**, zero failures/skips, **640.6 s**.
Combined **1,173/108**. Historical executable identity:
`A9D9AB1E42B97AB1378E82BD888402B277ED192EFFC4756344C9EDA7BAF435DE`.
Those results do not verify newer schema changes. Restore a current gate first in P2.

## Proven foundations and limits

One extracted netstandard2.1 runtime with no oil/platform/test dependency. Existing lifecycle, internal
host World, command inbox, exact module dependencies/versions, owned events/codecs/views and save/hash.
Fixed-point math, world-owned RNG and entity-owned settled state. Independent worker/mobility/inventory/
supplier implementations execute physical work without built-in counterparts or scheduler edits.
Typed pure requirements, pause retention, authored effects, named operations/signals and incident
episode guards are real seams. They do not prove arbitrary time/space, consuming alternatives,
complete colony policy, generic transformation or a finished replay system.

## Findings mapped to the single roadmap

Source paths below are relative to src/Beep.SimEngine.

| ID | Source anchor | Finding/disposition | Stage |
| --- | --- | --- | --- |
| A01 | Hosting/SimGame.cs:253; Kernel/World.cs | Correct single Initialize→Update→Draw/entity Update; retain delegation, no second loop | P3 |
| A02 | Map/TileMap.cs:104; Kernel/Entities.cs:268 | Mandatory dense tile map, 8..4096 bounds and spatial kinds on all entities; optional provider/checked extents | P3 |
| A03 | Core/GameTime.cs; GameClock.cs; Systems.cs | Int-minute ticks/365-day calendar throughout; authored quantum/calendar/wide checked time | P3 |
| A04 | Kernel/World.cs:283; Hosting/RenderSnapshot.cs | Vehicle-only previous position; reused mutable buffers lack durable immutable lifetime | P2/P3 |
| A05 | Content/Defs.cs:44; ResolvedContent.cs:13 | Sealed ItemDef; catalog exists but resource/unit/capacity contracts need extension | P7 |
| A06 | Content/Bill.cs:53; Standard/Inventory.cs:87/105 | Unlike units summed; anonymous inbound/outbound totals; owned atomic commitments | P7 |
| A07 | Standard/Production.cs:14/118/203 | Concrete producer/vehicle outputs; replace single coordinator with TransformerManager, provider-owned WIP | P8 |
| A08 | Standard/Work.cs:95/159/329/574 | Earlier-only dependencies, missing unique-node check, closed ToilKind, one-role/all-only requirements | P4 |
| A09 | Standard/Work.cs | Completed orders/jobs pruned; retain prior-activity evidence in same board | P4 |
| A10 | Standard/Logistics.cs:37; Work.cs | Closed demand source, reserve before selected carrier, whole-transfer load tick, simplistic policy | P9 |
| A11 | Construction.cs; Upgrades.cs | Placement delegates/concrete plans and raw state copies need declared validators/transfer contracts | P2/P4/P9 |
| A12 | Standard/Networks.cs:191 | Line construction/topology/pools coupled; last-tile removal loses contents; transfer responsibilities once | P10 |
| A13 | Operations.cs:126; Core/OperationSchemas.cs | Typed seam; current bindings unverified; foreign-world validation after read; rules absent | P2/P10 |
| A14 | Standard/Incidents.cs | Physical response exists; persistent phases/propagation/preparedness/environment coordination absent | P12 |
| A15 | Standard/StandardModule.cs:16 | Nav/work/economy compulsory in standard bundle; optional capability composition | P4 |
| A16 | Hosting/GameFactory.cs | Menu resolves all modules; unrelated invalid content blocks selection; implicit input fallback | P4 |
| A17 | ResolvedContent.cs:136/263; Json.cs | Ordered patch merge survives; remove loader and select one definition owner | P4 |
| A18 | Content/Defs.cs; Entities.cs; map/views | Raw array/list backing can escape readonly interfaces; freeze/copy content | P2 |
| A19 | PlayerView.cs:137/170; WorkView.cs:194 | Global entity/layer metadata and recipe lookup may leak hidden facts | P2/P11 |
| A20 | GameFactory.Load; World.cs:183 | Initialize precedes saved body codec validation; payload-refusal test is not zero-initialize proof | P2/P6 |
| A21 | Lockstep.cs:253/258/326 | tick*1024+seat key collision; validated captured options/decoded input/safety budgets needed | P3/P6 |
| A22 | Fix.cs; DetRandom.cs; Ids.cs | Numeric/parser/bounds/identity exhaustion unevenly checked; deliberate RNG modular math distinct | P3/P6 |
| A23 | Commands.cs; World.cs; SaveGame.cs | Partial faulted tick/checkpoint semantics unclear; no silently healthy restored checkpoint | P2/P6 |
| A24 | Tuning.cs; Navigation.cs; StandardCommands.cs | Hidden queue/line/rollout limits; path budget conflates no-route; authored policy/typed waits | P3/P9 |
| A25 | PlayerView command/notification feeds | Bounded feed without gap result cannot be canonical replay; existing codec recorder needed | P6 |
| A26 | Standalone module set | Generic progression/objective/knowledge/pressure/controller mechanisms absent; promote reusable coordination once | P11/P5 |

These are actionable inspection findings, not claims that each concern has a failing test.

## Read coverage

Math/Core: fixed arithmetic/vectors/identities/dates/fault/operation/work facts.
Content: parser/reader/merge/bills/defs/states/declarations/indexes/resolver.
Kernel: world/entity/capability/lifecycle/players/visibility/inspection; module/service/event/command/
inbox ownership; state codecs/sync/saves.
Hosting: files/master/factory/clock/cycle/game/views/snapshots/peers.
Standard: inventory/economy/work/roles/requirements/effects; build/upgrades/movement/nav/logistics/
production/networks/operations/incidents/commands/module/tuning.
App/Commands/Views: input/application orchestration, facades and projections.
Tests: every suite plus all external proof providers, helpers and projects.

## Coverage gaps, not lost tests

- Two-seat dense fixtures do not prove configurable time, nonspatial worlds or larger seat keys.
- Production checks whole output capacity, not owned WIP/output tokens across arbitrary providers.
- Requirements prove pure guards/pause, not consuming all/any alternatives or multi-provider tools.
- Codec tests prove no payload read on mismatch, not no Initialize callback.
- Draw hash purity does not prove retained snapshot lifetime.
- Incident proofs lack propagation/environment phases.
- Concurrent commands/lockstep are not a replay verifier or protocol/allocation stress gate.
- TestKit.RunUntil is bounded but does not itself fail an unmet predicate; runner has no watchdog.
- ContractSchemas presently blocks compilation and does not yet have registered binding suites.

## Locked authority map

| Responsibility | One owner |
| --- | --- |
| Definitions/resources/master | ContentTypes, ResolvedContent, ContentSet; no extra catalog/loader |
| Stock/cargo | Actual IInventory/buffer provider; no resource balance cache |
| Reservations/consumption | Existing owned commitment state + transaction protocol, no copied ledger |
| DAG/jobs/progress | WorkBoard/WorkSystem, no ActivityManager beside them |
| Transformations | TransformerManager replaces ProductionSystem; provider owns process state |
| Entity state | Entity.Update + resolver; operation/capability states are facts |
| Lifecycle/input/save | SimGame/World, existing inbox/codecs/Sync |
| Plans/policy/dispatch | Existing work/logistics/construction owners |
| Domain science | Normal engine extension, never host code |

## Research and interpretation

MonoGame documents lifecycle methods/component dispatch and fixed-step updates. Retain the familiar
Initialize/Update/Draw shape; do not adopt renderer timing policy as game authority.
[MonoGame game loop](https://docs.monogame.net/articles/getting_to_know/whatis/game_loop/).

Factorio documents ingredients, products, processing time and mixed multi-output recipes. This supports
declarative transformations rather than vehicle-only production. Our JSON/exact math/transaction design
is an inference, not a copied API or its limits.
[Factorio recipes](https://lua-api.factorio.com/latest/prototypes/RecipePrototype.html).

OpenRA documents modular traits and conditional/prerequisite behavior. This supports optional engine
capabilities and authored conditions. We retain our current module/entity contracts, not import OpenRA,
YAML or military features.
[OpenRA traits](https://docs.openra.net/en/release/traits/).

Primary sources checked 2026-10-04. No third-party engine dependency is introduced.

## Deliverable boundary

Full read, source inventory, current build result and detailed designs/help are recorded.
P2 baseline restoration and all later implementation remain open. All edits in this pass are under
Beep.SimEngine; no oil/Tycoon/platform runtime was modified.

## Documentation and setup verification

- All **108** recorded runtime/test SHA-256 values still match after the planning edits.
- help/index.html is well-formed HTML/XML with **7** inline SVG graphs, **47** links and **36** unique IDs;
  every local file/anchor resolves. Offline CSS/SVG require no external assets or scripts.
- **48** local Markdown links resolve across **15** audit/spec/master/stage documents.
- All **3** quick-setup JSON files parse. `dotnet run --project help/quickstart/Host/QuickGame.Host.csproj`
  exits 0: `Quick setup OK: tick=3; entities=1; hash=9FE630B08F812E68`.
- Quick setup proves current public composition/lifecycle/Draw hash purity/save identity only. Its
  engine-side world generation and thin host are documentation examples, not new framework mechanisms.
  These help files are additional authored examples, not part of the 108-file src/tests audit inventory.
- `git diff --check` exits 0; only existing Windows line-ending normalization warnings are emitted.
- HTML link/structure checks are not browser visual/accessibility certification. Full current solution
  remains red; no all-suite verification was attempted through stale binaries.

