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
