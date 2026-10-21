# GameFrameWork — Engineering instructions

## Governing methodology
This repository's C#/.NET code, Claude/Codex skills, Godot adapters, tests, and documentation MUST follow [Agentic SDLC Framework](https://github.com/fahadTheTechIdea/agentic-sdlc-framework) on its canonical `master` branch, with particular attention to:
- `core/ENGINEERING_CONSTITUTION.md`
- `core/ENGINEERING_STANDARDS.md`
- `core/QUALITY_SECURITY_RELIABILITY.md`
- `core/EVIDENCE_POLICY.md`
- `core/LOGGING_AUDIT_NOTIFICATIONS.md`
- `skills/sdlc-orch/SKILL.md` and its canonical orchestrator procedure.

Use that framework's adoption installer in preview mode before incorporating its full skill package into this repository. Preserve any existing AGENTS.md, CLAUDE.md, GameArts skills, and project changes. This file is a project-specific routing rule, not a substitute for the canonical SDLC policies.

## Automatic conversational game-intent routing

When a user says something like **"Let's start building a game"**, "I want to make a game", "Create a 2D city builder", "Build a Godot game", or "Continue my game", treat it as game-development intent even if no skill name is mentioned. This routing is semantic, NOT a keyword-only exact match.

1. Inspect the active project and existing `.gamearts/project.json`, `.gameframework/project.json`, and `.gameframework/setup-state.json` if present. Identify whether the user is starting, continuing, modifying, or discussing a game.
2. Follow the canonical SDLC orchestrator for risk-appropriate engineering; route world creation to `.agents/skills/world-generator/SKILL.md` and first-run setup to `.agents/skills/gameframework-project-setup/SKILL.md`. Invoke the canonical GameArts project-setup skill where installed. Do not duplicate its wizard.
3. For a **new game or incomplete setup**, guide the user through both GameArts and GameFrameWork wizards, in either order; reuse saved answers and cross-validate. Ask small conversational questions only when truly needed.
4. For an **existing configured game**, load configuration and continue the requested work without restarting either wizard. If the request is just conceptual discussion or plan-only, respect that scope rather than creating files.
5. Agents perform authorized installation, code execution and file management themselves; do not instruct users to run shell commands, clone repos, edit JSON or invoke specific skills as the normal onboarding path.
6. Do not silently install software outside an authorized workspace or claim access to missing agent tools. Report precise limitations and maintain honest completion status. Actual agent behavior requires live Claude/Codex evaluation.

## Workflow and authorization
1. Inspect repository state, existing code, tests, active docs and SDK versions first.
2. Record measurable acceptance criteria, material assumptions and risk-proportionate design decisions before implementation.
3. Prefer existing contracts, focused cohesive code, dependency inversion at external boundaries, SOLID/Clean Architecture, KISS/DRY/YAGNI, explicit cancellation, input validation and versioned schemas.
4. Implement changes **directly on `main`**. Do not create branches or PRs unless the owner explicitly changes this rule. Do not overwrite unrelated work.
5. Use approved GameArtsSkills current approved standard (v7.7 at review) manifests and actual sprite/atlas evidence; never invent approvals or claim a visually complete world from a scene skeleton.
6. Before claiming a feature complete, run the applicable build, tests and Godot integration checks. Record command, tested revision, outcome, environment and missing gates; **NOT RUN is not PASS**.
7. Keep the current master plan and tracker updated in place. Avoid duplicate planning docs.
8. Reuse or implement shared Log, Audit and Notification Services for applicable runtime boundaries; UI notifications when a UI exists. Preserve failures and audit important generation, export, approval and asset operations.
9. Validate security, dependency provenance, path handling, asset hashes, config and cancellation as relevant. Agent-skill changes require real agent behavior evaluations before claiming support.
10. Match the scope of governance to the risk; do not build a separate SDLC framework inside GameFrameWork.

## GameFrameWork two parts
- The Claude/Codex agent framework orchestrates planning, approvals, generation and validation.
- The C# engine supplies real reusable generation and Godot export APIs, even when no AI agent is running.

## Specific world requirements
Rules are world/biome/settlement/district/parcel/object scoped; every placement is purposeful and constrained. GameArts is mandatory for actual artwork production. Discover moving components and prefer separated static masters with distinct animated component artwork and measured atlas metadata when appropriate.

## Current baseline caution
The initial generator contains simplified pseudo-random terrain and settlement positions, and Godot exporter only creates a scene skeleton. They are research/prototype artifacts, not compliance with final world-placement, art or functional Godot acceptance criteria.

## Primary world-generation invariant: game mockup fidelity plus intentional design

The approved GameArts mockup and saved game art profile are the **primary visual design source of truth** for world creation. Every object (terrain feature, structure, door/window, road, streetlight, sign, vegetation, grass, flower, vehicle, decorative detail, etc.) must be derived from a traceable scene/region/feature anchor or a coherent style-compatible design grammar. A planner must produce explicit purpose, parent/region, constraints, position/footprint and visual source provenance *before* a compiler places anything. Do not search for empty positions or scatter objects randomly, including seeded random placement. Resolve clashes by revising the game-world design, not by placing objects arbitrarily. Test style and composition fidelity against the approved mockup, and preserve playable movement/collision and animation.

This is a **game** framework: prioritize intended art style, composition, game readability and interactions. Real-world traffic engineering, photometric illumination coverage, physical drainage and other realism optimization are opt-in simulation requirements only, never default acceptance gates. Existing distance-based streetlight, urban buildings and similar routines remain prototypes until refactored to use the visual design plan.


## Final generation responsibility
The **C# engine owns actual world design algorithms and generation**, including deliberate positioning of every terrain, settlement, building, road, planting and accessory element. Claude/Codex orchestrates and GameArts supplies approved visual mockups/style/art and animation atlases. The engine uses the approved mockup's anchors **and compositional regions/design grammars** to calculate precise positions; explicit per-object anchors are not mandatory for repeated procedural details. No arbitrary scattering or vacant-cell fallback. Godot adapter compiles and runs the generated result. Detailed real-life infrastructure simulation is optional unless gameplay requires it.
