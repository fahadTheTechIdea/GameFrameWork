# Tactical campaign layer: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Tactical campaign layer SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Connect discrete operations through a persistent roster, headquarters, strategic work and branching consequences.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. Outcome identities exist, but campaign mission selection, roster transfers, research and durable continuation are unimplemented.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| CAMPAIGN-R01 | Connect discrete operations through a persistent roster, headquarters, strategic work and branching consequences. | CAMPAIGN-A01 |
| CAMPAIGN-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under TacticalCampaignLayer. | CAMPAIGN-A04 |
| CAMPAIGN-R03 | Replaying a mission result cannot heal or reward twice. | CAMPAIGN-A01 |
| CAMPAIGN-R04 | Equipment deployed in a mission is not independently duplicated in headquarters stock. | CAMPAIGN-A02 |
| CAMPAIGN-R05 | Fail-forward branches are authored, not invented when a mission fails. | CAMPAIGN-A03 |
| CAMPAIGN-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | CAMPAIGN-A05 |
| CAMPAIGN-R21 | Restore exact committed state/time/content identity, including pending owned work. | CAMPAIGN-A06 |
| CAMPAIGN-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | CAMPAIGN-A07 |
| CAMPAIGN-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | CAMPAIGN-A08 |

## Invariants and quality boundaries

- Replaying a mission result cannot heal or reward twice.
- Equipment deployed in a mission is not independently duplicated in headquarters stock.
- Fail-forward branches are authored, not invented when a mission fails.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
