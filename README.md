# GameFrameWork

**Advanced 2D world generation for C#/.NET and Godot, with integrated Claude/Codex workflows and GameArtsSkills.**

## Get started — just talk to Claude or Codex
Open your game project in Claude Code or Codex and say:

> **"Set up GameFrameWork for my game."**

The agent should inspect the workspace and guide the user through **two conversational setup wizards**: the canonical **GameArts wizard** and the **GameFrameWork world-generation wizard**. Either can come first. The agent reuses existing settings, asks only missing questions, and keeps each wizard's progress independently so setup can resume without repeating answers. It then configures SDLC and Godot integration and reports what was verified. **Users should not need to clone integration repositories, invoke scripts, copy skill folders, or edit JSON.** Internal setup utilities are for the agent to execute with appropriate permissions, not user-facing onboarding.

Next, simply say **"Generate a desert industrial town"**, **"Create a coastal city with parks and roads"**, or **"Continue my world"**.

The agent must follow the [conversational world-generation skill](.agents/skills/world-generator/SKILL.md), [SDLC engineering guidelines](https://github.com/fahadTheTechIdea/agentic-sdlc-framework), [GameArtsSkills](https://github.com/fahadTheTechIdea/GameArtsSkills), and the [advanced master plan](docs/ADVANCED_WORLD_GENERATION_MASTER_PLAN.md). GameArts mockup and production asset approvals remain distinct. All object placement is rules-driven; separately animated moving parts and measured atlas metadata are mandatory when applicable.

## Two-part product
1. **Claude/Codex framework** — natural-language planning, setup, approvals, production orchestration, engineering SDLC and validation.
2. **C# world engine** — independent domain/world generation libraries, assets, CLI and Godot adapter.

**Implementation status:** the C# engine generates terrain masks/elevations, authored road corridors, region-based parcels, physically sized buildings, surrounding site zones and deliberate planting/public props from a structured mockup design. Building catalogs support custom types and size variants, settlement demand, access widths and site programs. GameArts bundle/file gates, coordinated crossings and portable-package auditing remain available. Local checks use synthetic layouts/artwork and approval authorities. Godot world export still creates a scene skeleton; the separate dummy village is a vector-art test project. Real artwork fidelity, Godot runtime acceptance and live wizard behavior remain unverified. See the [current roadmap](docs/ROADMAP.md) and [verification record](docs/SDLC_VERIFICATION.md).

## Engine generation

The C# engine accepts a `WorldRequest` with the current `MockupWorldDesign` and explicit `Catalog` selection/capability flags: mockup/style identity, visual regions, paths, road profiles, settlement frontage rules, terrain rules, building catalog and explicit landscape/prop grammars. It computes positions and footprints; callers do not supply every repeated object's anchor.

Building scale follows the archetype and capacity context. The catalog now includes **197 building/structure families and 585 size variants in 15 categories**: residential, retail, food/hospitality, offices/finance, education/research, healthcare, civic/safety, culture/worship, sport/recreation, transport/logistics, industry/extraction, agriculture, utilities/communications, public-space structures and linear/water infrastructure. Custom definitions, capacities, dimensions and site rules are data, not new engine classes. A stadium cannot be shrunk into a house plot. Required size or road-access mismatches return diagnostics.

`list-buildings <catalog-id> [category]` returns the complete or category-filtered JSON inventory, including dimensions, capacity units, site rules and placement domains. Specialized families require explicit world-program/composition choices; adding a refinery or airport to the catalog does not make it appear in every settlement. The 187 frontage families use parcel/site planning; the 10 linear/water infrastructure families use dedicated bridge, tunnel and shoreline planning through `MockupWorldDesign.InfrastructureRules`. These profiles are configurable examples, not an exhaustive inventory, measured artwork or engineering standards. [Coverage and examples](docs/SETTLEMENT_IDENTITY_AND_BUILDING_RULES.md).

Site programs derive entrance walks, courts, loading yards, parking, gardens and green buffers within their parent parcels. Separate park zones and planting grammars distribute vegetation intentionally, with explicit species dimensions and clearance checks. No fallback fills vacant cells or relocates a conflict.

The existing inventory lives in the separate **Modern** pack. Shared provider/registry/policy/demand/infrastructure contracts accept independently registered C# packs with their own fixed code and vocabulary. Modern explicitly enables electricity, potable water, wastewater, motor roads, districts and technology policies. Missing catalog/siting context, unknown flags and disabled required capabilities reject generation; there is one current contract and no compatibility path. [Contract and host usage](docs/SETTLEMENT_IDENTITY_AND_BUILDING_RULES.md#host-and-cli-usage).

`list-catalogs` discovers registered packs; `list-buildings modern Healthcare` filters Modern content. Add `--catalog-plugin <trusted-assembly.dll>` to load an external compiled pack. `generate-world <request.json> [output-dir]` uses the same injected catalog contracts and shared operation reporting. Unsupported commands are rejected. [The CI input](tests/fixtures/synthetic-desert-world.json) is explicitly synthetic; it grants no GameArts approval. The current settlement grammar supports rectangular north/south frontage regions and diagonal/curved polyline roads, with continuous source geometry preserved beside the raster output. Full Godot resource compilation is a separate gate.

## Capture a mockup before generating the world

The shared source-pixel capture contract records road polylines, widths/levels/junctions, building roofs versus ground footprints, green/water regions, access points, uncertainty and incomplete coverage. The engine handoff applies reviewed calibrated geometry to the existing design JSON and retains source lineage; it does not require a city-specific C# class. A review viewer overlays annotations on the exact PNG and can toggle the original image.

[Capture, Codex prompts and atlas procedure](docs/GAMEARTS_INTEGRATION_CONTRACT.md) covers actual image references, topology-derived road kits, isolated component frames and the canonical GameArts deterministic atlas builder. `review-capture`, `compile-capture` and `capture-schema` are internal agent/CLI entry points. [Current city design](.gameframework/worlds/modern-city/world-request.json) compiles 99 selected capture features, with authored ground calibration and 49 at-grade junction records resolving all 51 crossings. The actual engine generates 43 buildings, including a 180×140m stadium and 140×90m factory, plus 32 road paths and 44 source-anchored park trees. `generate-world` accepts `--capture` with `--asset-root` and exports a generated geometry map and source comparison. Artwork approval and Godot runtime remain separate gates.

## Audit a coordinated production package

```text
dotnet run --project src/GameFrameWork.Cli -c Release -- verify-world <world.json> <manifest-directory> <trusted-asset-root>
```

This audits the coordinated portable production-document format, not the design-only `WorldDocument` emitted by `generate-world`. The manifest directory contains one `<asset-id>.json` approved-art manifest per dependency. The command returns JSON with `Valid` and `Issues`, exits with 0 on integrity success or 1 on rejection/error, and records correlated log, audit and notification events under `.gameframework/operations` in the current directory. Ctrl+C requests cancellation (exit 130). A successful audit checks declared dependency revisions and file hashes; lifecycle approval, visual fidelity and gameplay readiness require their separate gates.

The repository pins .NET SDK 10.0.402. Current local checks used installed 10.0.401 through an isolated override; the pinned-SDK build remains unverified.

Development policy: only the existing `main` branch.
