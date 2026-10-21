# GameFrameWork — Claude Code

Read [AGENTS.md](AGENTS.md) for governing project rules; **all C# and agent framework development follows the canonical [Agentic SDLC Framework](https://github.com/fahadTheTechIdea/agentic-sdlc-framework)**, including its engineering constitution, evidence policy, quality gates, operational services and canonical `sdlc-orch` workflow. Use the project's adopted SDLC skills when installed. Preserve existing instructions during adoption.

For world-specific work, read `.claude/skills/world-generator/SKILL.md` and the current advanced world-generation master plan. GameArtsSkills controls mockup approval, source art, modular animated components and measured atlases. Implement C#/.NET code directly on `main` only, with actual build/test evidence and explicit limitations. No additional branches or pull requests.

## Natural-language game start

Recognize ordinary requests such as **"Let's start building a game"**, "I want to make a city-building game", and "Continue my game" without requiring `/sdlc-orch`, `/world-generator`, or an exact trigger phrase. Follow the routing rules in `AGENTS.md` and the canonical SDLC orchestrator. For a new or partly configured game, coordinate the **canonical GameArts wizard** and **GameFrameWork wizard** in either order, resuming existing settings; for an existing game, do not restart setup. The agent performs authorized technical setup internally, not the user. Respect discussion-only or plan-only requests.

