# Optional multiplayer authority and scoped replication: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Optional multiplayer authority and scoped replication SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Admit remote intents through one authority while using Godot's native networking and filtered game views.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. Current controller IDs and replay hashes are local contracts; they do not establish network authority or native lockstep.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| NET-R01 | Admit remote intents through one authority while using Godot's native networking and filtered game views. | NET-A01 |
| NET-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under MultiplayerAuthorityOwner. | NET-A04 |
| NET-R03 | Clients never choose authoritative controller identity or RNG/world-result state. | NET-A01 |
| NET-R04 | Checksum/replay determinism does not imply cross-machine native physics determinism. | NET-A02 |
| NET-R05 | Disconnected peers cannot retain unauthorized ownership. | NET-A03 |
| NET-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | NET-A05 |
| NET-R21 | Restore exact committed state/time/content identity, including pending owned work. | NET-A06 |
| NET-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | NET-A07 |
| NET-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | NET-A08 |

## Invariants and quality boundaries

- Clients never choose authoritative controller identity or RNG/world-result state.
- Checksum/replay determinism does not imply cross-machine native physics determinism.
- Disconnected peers cannot retain unauthorized ownership.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
