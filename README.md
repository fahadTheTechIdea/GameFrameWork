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

**Implementation status:** early C# foundation, art manifest checks, operational services and Godot scene skeleton. Full city algorithms, actual sprite generation, atlas-byte verification, populated Godot maps, navigation, collisions and in-engine runtime validation are not yet complete. The two-wizard setup workflow and schemas are specified; automatic execution, persistence and cross-wizard validation still need live Claude/Codex verification. Do not confuse planned functionality with working features.

Development policy: only the existing `main` branch.
