# Resources, inventory and reservations: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Resources, inventory and reservations SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Provide reusable integer stock, item-instance, container and reservation transactions behind domain contracts.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. Core has candidate-state commits but deliberately owns no inventory, recipe or stock rules.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| RESOURCE-R01 | Provide reusable integer stock, item-instance, container and reservation transactions behind domain contracts. | RESOURCE-A01 |
| RESOURCE-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under ResourceOwner. | RESOURCE-A04 |
| RESOURCE-R03 | A transfer cannot debit one container while failing to credit the other. | RESOURCE-A01 |
| RESOURCE-R04 | A preview never creates a reservation. | RESOURCE-A02 |
| RESOURCE-R05 | No other job may release this action's inputs. | RESOURCE-A03 |
| RESOURCE-R06 | All arithmetic is checked and uses declared integer units. | RESOURCE-A01 |
| RESOURCE-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | RESOURCE-A05 |
| RESOURCE-R21 | Restore exact committed state/time/content identity, including pending owned work. | RESOURCE-A06 |
| RESOURCE-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | RESOURCE-A07 |
| RESOURCE-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | RESOURCE-A08 |

## Invariants and quality boundaries

- A transfer cannot debit one container while failing to credit the other.
- A preview never creates a reservation.
- No other job may release this action's inputs.
- All arithmetic is checked and uses declared integer units.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
