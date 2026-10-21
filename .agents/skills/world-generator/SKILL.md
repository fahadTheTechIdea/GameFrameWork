---
name: world-generator
description: Conversationally set up and generate complete 2D game worlds using GameFrameWork, canonical SDLC guidelines, GameArts and Godot.
---
# World Generator — conversational orchestrator

The user talks normally to Claude Code or Codex. **Never ask the user to clone repositories, run Python, edit JSON, copy files, install skill folders or execute commands.** These are internal implementation operations for the agent to perform using its available file, terminal and connected tool permissions. Ask only for genuinely missing world/game preferences or approvals mandated by GameArts. If tools cannot perform an operation, explain the exact limitation, don't instruct the user to execute a setup command as the default workaround.

## Natural-language workflows
- "Set up GameFrameWork for my game": inspect game workspace, current project instructions and installed skills, automatically adopt the canonical SDLC and GameArts workflows non-destructively, apply project configuration defaults, verify installed capabilities, report real outcomes.
- "Generate a coastal town" or "Create a city": inspect settings, resolve settlement/biome/world-specific rules, build a coherent world specification, locate approved art or initiate its mockup approval workflow, drive the C# world engine via its API/CLI, validate results, and integrate into Godot.
- "Change my world rules": update persisted game-specific rules with scope and validation.
- "Continue world generation": inspect state/approvals and resume incomplete work.

## Unified first-run wizard (mandatory)
"Set up GameFrameWork" must run **both** conversational wizards: the canonical GameArts project setup wizard and the GameFrameWork world-generation setup wizard. **Either order is valid**; let the user choose an order if they have a preference, otherwise select a sensible order using existing workspace state. Neither wizard is optional for a complete initial setup.

- Inspect existing `.gamearts/project.json`, GameArts installation and existing GameFrameWork settings. Reuse answers already provided in either system; never repeat already-answered questions.
- Invoke the actual GameArts project-setup skill and its persisted schema, not a duplicate questionnaire. Let it establish art style, sprite scale, perspectives, directions, image masking, mockup/asset approvals and other GameArts options.
- GameFrameWork wizard gathers only the additional, relevant game/world choices: target game/Godot project, world types and settlement profiles, world scale/grid, environmental and construction rules, simulation depth, streaming/LOD requirements, map layers, navigation agent types, collision/elevation, output profiles and generation/validation policy.
- World configuration defaults must respect GameArts settings, particularly orthogonal grid calibration, 16×16 m logical cells and latest approved art master; surface conflicts conversationally rather than silently overriding.
- Present questions in small conversational groups, offering recommended defaults. Allow later "Change my world settings", "Change my art settings", "Resume setup" and "Show my settings" commands.
- Persist GameArts selections only through its canonical workflow; store the separate world settings at `.gameframework/project.json` using a versioned schema. Keep references/version pins instead of duplicating or mutating GameArts settings.
- Maintain a resumable stage record in `.gameframework/setup-state.json` with independent `gameArts` and `gameFrameWork` completion flags. A wizard can run first, finish, or resume without resetting the other. A completed setup means **both** have completed and their cross-contract validations pass.
- If an existing game already completed one wizard, run only the other or any missing fields. Do not claim setup complete when integration verification is missing.
- If the user first says "Set up Game Arts", finish that wizard and offer/automatically continue to the GameFrameWork wizard when setup is part of an integrated GameFrameWork flow; the converse applies when world setup comes first.

## Automatic setup (agent task, NOT end-user instruction)
1. Inspect canonical instructions in GameFrameWork AGENTS.md/CLAUDE.md, active project, SDLC framework and GameArtsSkills. Preserve existing files and avoid duplicate skills.
2. If the canonical SDLC skill package is absent, inspect the canonical source and perform preview-first, collision-aware setup with tools available to the agent. An internal helper such as scripts/adopt_sdlc.py may be used **by the agent itself**, never presented as user-required installation steps.
3. Preserve and compose existing Claude/Codex + GameArts skills; no unapproved overwrites or clobbering.
4. Persist configuration to the game repository and verify discovery and integration. If a tool/permission/checkout is missing, report precisely what could not be completed; never claim installation succeeded.
5. Follow the canonical SDLC orchestrator for coding/design, with risk-based tests and evidence, and GameArts for separate mockup and production-asset approvals.
6. Invoke actual C# world generators and Godot adapters. Do not claim runtime or visual readiness without observed checks.

## Rules
- C# world model and agent workflow are two parts of the same product; GameArts is integral, not optional.
- No arbitrary placement of any objects. Respect world/biome/settlement/district/parcel rules and valid placement provenance.
- Separate independently animated doors/windows/gates/wheels from static masters, with measured sprite sheets/atlases as appropriate.
- Work directly on main only when modifying the GameFrameWork repository. No new branches or pull requests.
- Keep progress and unmet gates explicit. Never manufacture approvals or successful test evidence.
