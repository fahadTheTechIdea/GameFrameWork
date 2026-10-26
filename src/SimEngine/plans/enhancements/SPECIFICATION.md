# Standalone simulation-game framework specification

## Goal and scope

`Beep.SimEngine` becomes the reusable headless framework for simulation games. OilFieldDays becomes
an oil-and-gas extension. This is a separation of responsibilities, not a folder rename alone.
The framework must support different authored simulations without assuming oil extraction, machines
as the only workers, a tile map, one-minute ticks, company money, or industrial victory rules.

Do not implement every possible genre now. Prove reusable composition with small independent games.
Population and combat remain non-goals for OilFieldDays; they are not prohibited future framework
extensions. The business/Tycoon module is optional. Survival/resource-only games use physical resources
without money, buyers, sellers or a mandatory economy service. When selected, the module supplies fixed
default resource values, money budgets and production income; explicitly activated Tycoon mode enables
its advanced economic rules. One module and account authority serve both active modes.

## Existing source evidence

Pre-extraction audit scope was the active v2 tree `Beep.OilFieldDays/src/oilCo-engine/src/SimEngine`.
The table/findings below describe that baseline, not current locations or unresolved status;
the master tracker records the completed extraction/access changes and remaining generalization.

| Current owner | Finding | Target disposition |
| --- | --- | --- |
| `SimEngine/Core`, `Math`, `Kernel`, `Hosting` | Deterministic time, entities, commands, saves, lockstep, lifecycle | Standalone framework; remove mandatory domain assumptions |
| `SimEngine/Content` | JSON definitions, resolution, validation, identity | One framework content pipeline, extensible through registered contracts |
| `SimEngine/Map`, `Standard` | Tile routing, work, hauling, construction, production, networks | Optional reusable framework modules, not oil-owned copies |
| `SimEngine/App`, `Commands`, `Views` | Headless input/application and standard facades | Public optional framework surfaces; no drawing SDK |
| `SimEngine/Properties/AssemblyInfo.cs` | Friend access for `OilCo.Game` and `OilCo.Tests` | Remove oil friendship; public module boundary; tests never justify gameplay privileges |
| `OilCo.Game` | Geology, reservoirs, oil operations, companies, research, campaign, AI | Domain behavior remains oil; promote only proven generic mechanisms, deleting the old generic owner |
| `content/manifest.json`, `content/oil` | Authored oil definitions plus reusable state names | Oil manifest explicitly composes framework modules/content and its own definitions |
| `OilCo.Tests` | Framework, standard, oil, and client tests share one runner | Allocate tests by dependency; one owner for test helpers |
| `OilCo.Headless`, `OilCo.LiveDemo`, `OilCo.Harness` | Player scripts and presentation | Remain oil clients; audit predicates, not just imports |

Specific constraints requiring redesign:

- `Core/GameTime.cs` fixes a tick to one game minute and a 365-day calendar.
- `Kernel/Systems.cs` and scenario `frame_minutes`/`horizon_days` inherit that time model.
- `Kernel/World.cs` always creates `TileMap`; entities and snapshots assume positions/footprints.
- `EntityKind` restricts basic identity to structure, vehicle, or marker categories.
- `OilCo.Game/Autopilot.cs` and `Views.cs` use internal `PlayerView.Create`/entity visibility.
- `StandardModule` installs `FreeEconomy` implicitly rather than requiring an explicit policy.
- Work-step dependencies accept earlier authored entries, not a fully validated arbitrary DAG.
- `OilCo.Headless/FieldPlayer.cs` infers generator operation from natural-gas inventory. Inventory
  is not an authoritative running-state view; this must become an engine-reported fact.

These are audit findings, not claims that the redesign is already implemented.

## Target ownership

```text
Beep.SimEngine/
  src/Beep.SimEngine/             one framework runtime initially
  tests/Beep.SimEngine.Tests/     standalone framework/module tests
  tests/ExtensionProof/           external non-oil extension proof
  plans/enhancements/             specification, stages, single master
  Beep.SimEngine.slnx

Beep.OilFieldDays/.../SimEngine/
  OilCo.Game/                     oil-and-gas extension
  content/                       oil master manifest and domain JSON
  OilCo.Tests/                   oil and oil-client tests
  OilCo.Headless/                 scripted player/presentation
  OilCo.LiveDemo/                 scripted player/presentation
  OilCo.Harness/                  scripted player/presentation
  plans/                         oil design and link to extraction tracker
```

The old directory name may remain during extraction to avoid unrelated project churn; it must no
longer contain a second framework runtime. Renaming the oil game layout is not required for ownership.
Initial framework assembly and namespaces become `Beep.SimEngine`; update all consumers together.
No old namespace forwards, wrapper facade, or duplicate engine packages.

Dependency direction: oil engine extension → framework. Clients → approved public game facade.
Framework → neither oil extension nor clients. Test infrastructure never becomes a runtime dependency.

## Lifecycle and entity contract

Retain Initialize → Update inside SimEngine. `Game.Update` advances deterministic simulation time and invokes
each live entity's `Update` once per completed tick in stable order after the declared gameplay phase.
Systems coordinate shared work; capability contracts provide behavior; the entity is the sole owner
of its resolved state transition. Do not let commands or clients independently rewrite entity state.

Initialize and Update publish immutable simulation state. Draw/rendering is a later platform responsibility,
not an engine phase or current implementation. No Draw event, render model or Unity/Godot implementation is included.
Existing convenience calls must use the same lifecycle, never a second update scheduler.

## Data and contracts

- A game master JSON manifest lists modules and all content documents/imports. No directory scanning
  silently enrolls content. File order cannot change deterministic meaning.
- JSON authors prototypes, resources, roles, technology, workflows, requirements, states, policies,
  signals/rules, incidents, objectives, and scenarios. Executable behavior is registered code implementing
  contracts; JSON references stable registered identities, not arbitrary CLR type names.
- One definition registry, resolver, validator, live entity store, command dispatcher, reservation ledger,
  workflow/job authority, save codec, and immutable-view projection per responsibility.
- Optional modules declare requirements, version, content ownership, commands, events, systems, state
  codecs, and view providers. Fail composition before Initialize for invalid or ambiguous ownership.
- No fabricated content defaults or implicit economy fallback. Explicitly authored free economy is valid.
- Default business values are explicit master-listed JSON for every resource and native quantity unit,
  including explicit zero-valued resources. Missing values are errors in monetized scenarios, not free fallbacks.
- Games omitting the business module require no price catalog or dummy economy. Their construction
  requirements use resources, machines, prerequisites and game time, not implicit money requirements.
- With the business module selected, actual committed generation credits its owner once at the active shared-module value: for example,
  one authored gold unit at100000 earns100000. Transfers, reservations, initial stock, previews and
  save/load are not new production. Hosts/tests never compute values, credits or construction budgets.
- Every `IOutletPort` also implements `IInletPort`. Endpoint, transporter, worker, and operation contracts
  must allow a new implementation without modifying a scheduler.
- Seismic and logging are optional oil knowledge branches, never drilling prerequisites.

## Generality and resource bounds

Make time resolution, calendar, frame stepping, and scenario horizon explicit, deterministic configuration.
Make space optional; tile navigation is one provider, not the definition of a simulation world.
Do not impose engine-wide game-map/entity caps copied from a platform. Real allocation limits,
overflow checks, catch-up budgets, and protocol safety bounds remain explicit and distinguishable
from gameplay size rules. World dimensions, when present, come from the game definition.

## Non-goals and migration rules

No adapters, renderer, commercial second game, unrelated Tycoon features, package publishing, repository remote
changes, or cleanup of the older sibling engine. No compatibility promises or legacy facades.
Preserve uncommitted source and untracked authored files. Move each implementation once, update its
consumers, and retire its original build path. A recoverable migration backup is not a buildable second engine.

## 2026-10-04 framework audit and expanded scope

The standalone audit is [ENGINE_AUDIT.md](ENGINE_AUDIT.md), with a complete hashed source/test
inventory. It supersedes the pre-extraction findings for historical locations/status. The subsequent
2.12.0/save 7 standalone build and full framework gate pass; the master records current scoped evidence.

Resources must be extensible definitions/contracts, not sealed item assumptions or unitless totals.
ResolvedContent owns the catalog; IInventory owns balances; P7 extends existing commitments with
typed units/capacity and atomic token protocols. Planet X ore is ordinary explicitly listed content.

Crafting, manufacturing and oil-to-fuel processing use a single TransformerManager evolved from
ProductionSystem (P8), not an additional manager. JSON authors inputs, outputs, time, catalysts,
requirements, ports, utility gates and cancellation. Engine providers implement executable science.

Reusable mechanism code remains in Beep.SimEngine. Oil-only formulas and content remain ordinary
engine-side extensions. Thin hosts expose no rules, mutable world, progress ledger or dispatch logic.
The basic Initialize/Update and per-entity state/update model remains the single simulation loop.
Drawing and platform integration are deferred and outside this implementation.

## Deliverables

The 2026-10-07 clarification supersedes blanket business-exclusion instructions. P4-H1/H2/H3
define the shared default-value module, production settlement/building budgets and explicit Tycoon
activation respectively. Current source has the optional shared TycoonModule, fixed valuation,
accounts, production settlement and work budgets. WorkModule has no mandatory IEconomy requirement.
Advanced Tycoon activation still explicitly refuses without an actual advanced economic provider;
do not interpret configuration properties as implemented advanced business behavior. Historical
focused evidence and remaining gates are recorded in P4 and the master; this is not full completion.

P1–P12 implement this specification with separate designs and acceptance gates, retaining original
stage identities and one master. Detailed HTML help lives in help/index.html. Existing activity and
industrial plans must be reconciled by responsibility: reusable mechanisms belong here; oil gameplay
and content remain oil-owned. Their historical completion claims do not close this extraction.
