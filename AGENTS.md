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
5. Use approved GameArtsSkills v7.5+ manifests and actual sprite/atlas evidence; never invent approvals or claim a visually complete world from a scene skeleton.
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
