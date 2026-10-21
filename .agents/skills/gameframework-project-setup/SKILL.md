---
name: gameframework-project-setup
description: Conversational, resumable GameFrameWork settings wizard coordinated with GameArts project setup in either order.
---
# GameFrameWork setup wizard
Users may start with any natural-language game-building request, such as "Let's start building a game", "Build a Godot game", "Set up GameFrameWork", or "Continue my game"; no special command phrase is required. This is an agent-led conversation: no user-executed commands, JSON edits or cloning. When GameFrameWork setup is requested initially, ensure **both** the canonical GameArts project wizard and this wizard run (either order); reuse existing answers and resume state. See `.agents/skills/world-generator/SKILL.md`.

## World wizard questions
Inspect the Godot project and current settings before asking anything. Offer recommended defaults and only gather missing decisions relevant to the project:
1. Game concept and generated world types: city, town, village, mixed, desert, island, forest, industrial, etc.
2. Geography/grid and expected region size, world meters and terrain art scale compatible with GameArts.
3. Settlement identity and rule profiles, historical/cultural and ecological constraints.
4. Simulation behavior, static/dynamic component detail, height, collision, navigation agent classes and animation.
5. Godot target/export integration, chunks/streaming, quality and verification needs.
Confirm only material tradeoffs; store the result at `.gameframework/project.json` with a versioned schema, and stage status in `.gameframework/setup-state.json`. Respect saved `.gamearts/project.json` without overwriting it.

## Completion
Mark GameFrameWork wizard complete when validated and persisted. Complete integrated setup only when GameArts is also complete and cross-system version/scale compatibility passes. Report unresolved dependencies and supported next steps honestly. Preserve existing config and work on `main` for framework-repository changes.
