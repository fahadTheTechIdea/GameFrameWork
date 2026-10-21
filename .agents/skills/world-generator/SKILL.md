---
name: world-generator
description: Plan, generate, export and validate deterministic 2D game worlds using C# GameFrameWork and Godot.
---
# World Generator
1. Inspect current game settings, profiles and approved GameArts assets.
2. Convert world request into validated dimensions, biome, seed, and structures.
3. If missing artwork is needed, invoke GameArts mockup and asset approval workflows; never auto-approve.
4. Run C# generator and inspect world.json, preserving deterministic seed.
5. Export using the Godot adapter and distinguish scene skeleton from functional tiles/navigation/collision.
6. Validate exported files and, when available, load in Godot and test movement, animated components and clipping.
7. Report files, evidence and unimplemented capabilities; don't claim atlas or playable Godot output until validated.
