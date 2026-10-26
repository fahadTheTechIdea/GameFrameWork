# Persistent worlds, regions and stable identities: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Persistent worlds, regions and stable identities SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Retain world changes and entity identity across unloading, travel, saves and revisits.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. ZoneDefinition supplies topology, but SessionSnapshot has no reusable active-region/world checkpoint model.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| PERSIST-R01 | Retain world changes and entity identity across unloading, travel, saves and revisits. | PERSIST-A01 |
| PERSIST-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under PersistentWorldOwner. | PERSIST-A04 |
| PERSIST-R03 | Looted/destroyed objects do not respawn merely because a scene reloads. | PERSIST-A01 |
| PERSIST-R04 | Only one region owns an entity at a time. | PERSIST-A02 |
| PERSIST-R05 | Frozen and abstract simulation are explicit gameplay choices, never silent failure substitutes. | PERSIST-A03 |
| PERSIST-R06 | NodePath and instance IDs are never durable entity identities. | PERSIST-A01 |
| PERSIST-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | PERSIST-A05 |
| PERSIST-R21 | Restore exact committed state/time/content identity, including pending owned work. | PERSIST-A06 |
| PERSIST-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | PERSIST-A07 |
| PERSIST-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | PERSIST-A08 |

## Invariants and quality boundaries

- Looted/destroyed objects do not respawn merely because a scene reloads.
- Only one region owns an entity at a time.
- Frozen and abstract simulation are explicit gameplay choices, never silent failure substitutes.
- NodePath and instance IDs are never durable entity identities.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
