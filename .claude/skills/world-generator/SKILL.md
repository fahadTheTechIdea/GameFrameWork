---
name: world-generator
description: Reusable mockup-free world metadata and optional visual game integration.
---
# World Generator (Claude)
For metadata-only requests, use **Metadata world creation (primary reusable core)** in the canonical skill below. It requires no GameArts setup, mockup, sprites or Godot; do not start the visual wizards for this route.
For source-pixel capture, road/feature review, image-reference prompts and measured atlas production, use the **Source capture and image/atlas handoff** procedure in the canonical skill below and `docs/GAMEARTS_INTEGRATION_CONTRACT.md`; do not maintain a second capture workflow here.
Follow the canonical world-generation workflow at `.agents/skills/world-generator/SKILL.md`. All setup and adoption is agent-operated, including SDLC/GameArts discovery and non-destructive installation. Never instruct the end user to run Python, clone source, copy files, edit configuration or invoke manual commands. Accept normal requests such as "Set up GameFrameWork", "Create a city", "Make a forest world", and "Continue my world". Report genuine permission/tool limitations and request only required art approvals or essential game choices.
