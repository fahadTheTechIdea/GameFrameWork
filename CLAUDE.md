# GameFrameWork — Claude Code

Read [AGENTS.md](AGENTS.md) for governing project rules; **all C# and agent framework development follows the canonical [Agentic SDLC Framework](https://github.com/fahadTheTechIdea/agentic-sdlc-framework)**, including its engineering constitution, evidence policy, quality gates, operational services and canonical `sdlc-orch` workflow. Use the project's adopted SDLC skills when installed. Preserve existing instructions during adoption.

For world-specific work, read `.claude/skills/world-generator/SKILL.md` and the current advanced world-generation master plan. GameArtsSkills controls mockup approval, source art, modular animated components and measured atlases. Implement C#/.NET code directly on `main` only, with actual build/test evidence and explicit limitations. No additional branches or pull requests.

## Natural-language game start

For engine procedural city requests, follow the canonical skill's **Engine procedural city generation** route and `docs/PROCEDURAL_CITY.md`. The C# engine owns geometry generation from rules; JavaScript renders its spatial output. Do not satisfy this request with only metadata expansion or a hand-arranged schematic.

For metadata-only requests, first use the mockup-free catalog/recipe route in `.agents/skills/world-generator/SKILL.md`. WorldCreator's reusable metadata core supports arbitrary game kinds, hierarchy, connections, constraints and optional coordinates. Do not require either visual wizard, GameArts, mockups or Godot for this scope. The visual rules below apply when visual production/integration is requested. This follows the owner's 2026-10-25 clarification recorded in `AGENTS.md`.

Recognize ordinary requests such as **"Let's start building a game"**, "I want to make a city-building game", and "Continue my game" without requiring `/sdlc-orch`, `/world-generator`, or an exact trigger phrase. Follow the routing rules in `AGENTS.md` and the canonical SDLC orchestrator. For a new or partly configured game, coordinate the **canonical GameArts wizard** and **GameFrameWork wizard** in either order, resuming existing settings; for an existing game, do not restart setup. The agent performs authorized technical setup internally, not the user. Respect discussion-only or plan-only requests.



## Game visual-design source of truth

When generating a game world, the selected GameArts style and approved mockup govern composition, appearance, object families, density, placement grammar and moving-part inventory. Every object requires a validated design-plan source/anchor and gameplay coherence; never scatter arbitrary or merely empty-space candidates. Photometric lighting, regulatory street signs or civil-engineering realism are optional user-selected simulation mechanics, not default objectives. Refer to the master plan's "Governing game-world rule" section.


## Final generation responsibility
The **C# engine owns actual world design algorithms and generation**, including deliberate positioning of every terrain, settlement, building, road, planting and accessory element. Claude/Codex orchestrates and GameArts supplies approved visual mockups/style/art and animation atlases. The engine uses the approved mockup's anchors **and compositional regions/design grammars** to calculate precise positions; explicit per-object anchors are not mandatory for repeated procedural details. No arbitrary scattering or vacant-cell fallback. Godot adapter compiles and runs the generated result. Detailed real-life infrastructure simulation is optional unless gameplay requires it.
