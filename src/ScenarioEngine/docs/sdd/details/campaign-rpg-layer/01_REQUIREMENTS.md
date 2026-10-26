# Recruitment and headquarters campaign RPG: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Recruitment and headquarters campaign RPG SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Support a Suikoden-style architecture: exploration, recruitment, headquarters and several encounter rules sharing one campaign roster.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. No recruitment/headquarters/RPG combat/mode runtime exists; current GameBase must be retained while these optional domain owners are introduced.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| CRPG-R01 | Support a Suikoden-style architecture: exploration, recruitment, headquarters and several encounter rules sharing one campaign roster. | CRPG-A01 |
| CRPG-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under CampaignRpgLayer. | CRPG-A04 |
| CRPG-R03 | A support character need not implement an attack action. | CRPG-A01 |
| CRPG-R04 | Recruitment status is distinct from combat health and temporary availability. | CRPG-A02 |
| CRPG-R05 | Duplicate battle results never repeat rewards, deaths or service unlocks. | CRPG-A03 |
| CRPG-R06 | Entering a battle does not call GameBase.Init again or reset the campaign. | CRPG-A01 |
| CRPG-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | CRPG-A05 |
| CRPG-R21 | Restore exact committed state/time/content identity, including pending owned work. | CRPG-A06 |
| CRPG-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | CRPG-A07 |
| CRPG-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | CRPG-A08 |

## Invariants and quality boundaries

- A support character need not implement an attack action.
- Recruitment status is distinct from combat health and temporary availability.
- Duplicate battle results never repeat rewards, deaths or service unlocks.
- Entering a battle does not call GameBase.Init again or reset the campaign.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
