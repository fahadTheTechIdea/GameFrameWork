# Capability profiles and layer composition: requirements

Status: detailed proposed design; runtime implementation and all package-specific acceptance cases are not_run. Updated 25 October 2026, Asia/Kuwait.

This is one chapter of the [Capability profiles and layer composition SDD](README.md). [Shared baseline](../ENGINE_BASELINE.md) defines existing-versus-proposed status, units, trust, operational services and compatibility.

## Context and scope

Select an exact, validated combination of registered game rules, host capabilities and state schemas before launch.

The supported slice begins at validated profile/definition selection and ends at a committed result that can be saved, restored and explained. LayerCapabilities currently describes facts and operation/effect/observation schemas; it does not negotiate a host or optional module profile.

## Observable requirements

| Requirement | Required outcome | Primary check |
| --- | --- | --- |
| CAP-R01 | Select an exact, validated combination of registered game rules, host capabilities and state schemas before launch. | CAP-A01 |
| CAP-R02 | Reuse existing IGameLayer/ILayerRuntime and preserve one candidate commit under GameProfileCatalog. | CAP-A04 |
| CAP-R03 | Profile resolution never loads a DLL named by JSON. | CAP-A01 |
| CAP-R04 | A module failure rolls back the entire candidate transaction. | CAP-A02 |
| CAP-R05 | Counter/Signal layers remain usable without implementing every optional kit. | CAP-A03 |
| CAP-R20 | Validate profile/definition before launch; reject unknown members and unresolved references. | CAP-A05 |
| CAP-R21 | Restore exact committed state/time/content identity, including pending owned work. | CAP-A06 |
| CAP-R22 | Report failed/denied/cancelled outcomes through the existing diagnostics boundary. | CAP-A07 |
| CAP-R23 | Qualify the actual native or headless consumer separately from document/schema validation. | CAP-A08 |

## Invariants and quality boundaries

- Profile resolution never loads a DLL named by JSON.
- A module failure rolls back the entire candidate transaction.
- Counter/Signal layers remain usable without implementing every optional kit.

Authored counts/durations in the definition are proposed bounded settings, not measured capacity or balancing promises. Exceeding a bound returns a diagnostic; silently dropping state, actors, effects or history is forbidden. No networking, hosted content, online inference, account system or new dependency is implied unless that capability is explicitly selected.

## Completion boundary

A complete delivery includes a working consumer, current schema/semantic validation, real state round trips, failure paths and the selected native journey. Required dependencies must be implemented or the feature remains unavailable. The [verification chapter](06_VERIFICATION_DELIVERY.md) records each unrun gate; a populated document is not a passing runtime test.
