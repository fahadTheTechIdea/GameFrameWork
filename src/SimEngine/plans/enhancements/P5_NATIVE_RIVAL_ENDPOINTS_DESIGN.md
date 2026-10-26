# P5-A36: Native rival endpoint and survey decisions

## 0. Problem statement

Oil AutopilotSystem used stock and route commands without named ports after the authoritative engine adopted named resource endpoints. Ship also queried an unnamed inlet prototype instead of live compatible ports, stopped at one unsuitable destination, and Explore used the obsolete order:survey prefix rather than the native activity identity.

## 1. Goals

- Publish actual owned resource-port identities through the existing InventoryView.
- Make all rival stock/route intent identify actual compatible endpoints without hardcoded oil.stock fallback.
- Keep decisions deterministic, based solely on the company's own public views/previews.
- Prevent repeated blocked survey intent using its actual engine-authored plan identity.
- Verify two resource-only rivals, finite physical cargo and saved continuation through normal commands.

## 2. Design

InventoryView.CopyResourcePortIds(EntityId, IList<Code>) observes original provider, target codec, space and clock; checks actual runtime and metadata aliases; stages detached IDs; returns ordinal ordering. Foreign/missing entities clear output and return false. There is no new endpoint registry or stock owner.

Oil SurveyKind publishes Plan from the actual authored activity. Explore compares live order keys against that value rather than guessing a prefix.

KeepStock discovers HQ ports, filters using ReadResourcePort, reads/previews existing policy with that port, then submits ConfigureStockPolicyCommand. The authored strategy keeps one compatible stock-policy target per resource.

Ship discovers source and destination ports from live owned entities, filters through ReadResourcePort, and invokes PreviewRoute and AddRouteCommand with those exact identities. Candidates within each authored sink preference are ordered by footprint distance and stable entity ID. A refused nearer candidate does not prevent trying another. Existing strategy retains one route per well/resource; aliased ports do not multiply routes. Obsolete Nearest helper is removed.

All execution remains ordinary engine commands, existing stock, reservations, physical hauling, WorkBoard and the one clock. No free resources, sales, account, rendering, host strategy, or new AI owner.

## 3. API surface

New detached InventoryView port-ID query. SurveyKind.Plan is read-only-by-copy engine metadata. No command/save schema changes; core2.552.0/save156/replay1.

## 4. Files

- Framework InventoryView.cs, SaveGame.cs and authored protocol identity.
- Oil Autopilot.cs and Views.cs.
- Thin public AutopilotPortsTests.cs plus registration.
- Stage/evidence and existing master/audit records.

## 5. Error handling

No compatible port or route means no command; no guessed endpoint is substituted. Invalid provider identity/purity is handled by existing observation faults. Unavailable/foreign ports never become player-visible candidate IDs. Preview/application refusals retain ordinary engine semantics.

## 6. Compatibility

No legacy prefix, unnamed fallback or compatibility shim.

## 7. Verification

One new bounded exact public case uses canonical modules/master and JSON-authored source/depot prototypes with finite starting crude. Two actual autopilot companies found normally, discover distinct named endpoint aliases, apply stock bands, route through ordinary commands, move cargo over time, preserve privacy, avoid survey duplication, and continue interleaved saved sessions.

The fixture's tagged well-store is a finite authored endpoint, not a simulated completed oil well or proof of extraction. Existing well drilling/production acceptance is separate. No runtime stock, state, finance, or world writes in the test driver.

## 8. Risks

Broader private-provider purity, global spatial/performance, recovery, broken routes, rival economic affordability, full industrial campaign and all compound acceptance remain open. Distance ordering uses existing visible tile facts and does not invent route connectivity.

## 9. Out of scope

HQ recovery, new rival strategy profiles, free-money fallback, Tycoon business changes, platforms/Draw, global durability and full regression.

## 10. Execution

Implementation and one bounded exact case complete: FINAL479/0 in253810ms, observer254105ms. Both earlier failures retained in [evidence](../../docs/migration/P5_NATIVE_RIVAL_ENDPOINTS_VERIFICATION.md). No original acceptance gate is closed by this scoped slice; broader goal/plan remains open.

