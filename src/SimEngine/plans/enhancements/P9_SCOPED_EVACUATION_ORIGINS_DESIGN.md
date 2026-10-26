# P9-D5h: Exact named evacuation origins

Status: native slice focused-verified; full P8/P9/P1-P12 remain OPEN.
FINAL68 checks in1405ms; FOUR builds, final clean12.99s.
[Evidence](../../docs/migration/P9_SCOPED_EVACUATION_ORIGINS_VERIFICATION.md).

## 0. Problem statement

LogisticsRecovery.RefreshRecoveries discovers only IInventory components and identifies origins by
entity/resource. PreviewDestination assumes from.Get<IOutletPort>(). Named IResourceOutlet stores
already expose Evacuate but cannot enter the existing evacuation path.

## 1. Goals

1. Discover owned public evacuating local and named outlets through existing VisitPublicPorts.
2. One existing chronology per actual endpoint/resource/company; aliases never duplicate work.
3. Exact source port/endpoint flows through original offers, previews, acquisition and job receipts.
4. Current selection determines future intents; acquired jobs retain original actual sources.
5. Saves/hashes/replays retain source identity without a second stock owner or scheduler.
6. Prove timed pickup/delivery, selection edits, aliases, private exclusion and conservation.

## 2. Design

Extend original RecoveryIntent with SourcePort and SourceEndpoint. Existing public-port discovery
observes actual resource outlets across content resources. Existing RecoveryStock resolves the current
binding and matches its exact endpoint. Actual aliases deduplicate by company/resource/endpoint.
Rediscovery precedes pruning; a surviving alternate alias updates only the existing intent's physical
binding, retaining actual-store creation/last-dispatch age. Acquired job fields remain untouched.
Cargo return uses its already-authoritative cargo binding and the same chronology.

RecoveryEntry uses existing length-encoded DemandEntry to disambiguate actual origins. Original
Assignment, RecordRecovery and Acquire recognize that identity; no prefix heuristic.
PreviewDestination accepts exact source bindings and delegates both full and reduced quantities
through the original physical Preview algorithm. Jobs/receipts retain original identities.

Original SyncRecoveries serializes/validates source port and scoped identity, forbids duplicate actual
chronology and keeps expired rows until normal preparation. No compatibility reader.
Core2.324.0/save154/logistics22; WorkBoard49/WorkSystem42 remain unchanged. Existing logistics diagnostics expose actual OutboundPromised; logistics-view36.

## 3. API surface

No new public command/provider/scheduler or parallel endpoint registry. Existing immutable offer
SourcePort/SourceEndpoint carry actual origin even when destination preview is blocked.
Existing diagnostics report actual outbound promises through the same endpoint observation, not jobs or cached balances.

## 4. Files

Standard/LogisticsRecovery.cs, Logistics.cs, LogisticsScheduling.cs and Kernel/SaveGame.cs;
normal master-listed evacuation JSON, composition registration and existing external pool content
provider's required explicit evacuation flag (existing pool authoring migrated, no default); one new exact bounded player case; protocol and tracker evidence.

## 5. Error handling

Missing/currently changed origins lose future intent, not acquired receipt identity. Private stores
are never discovered as automatic evacuation sources. No local inventory fallback. No complete
destination means ordinary waiting; no teleport, deletion or partial-success compensation.

## 6. Compatibility

Development migration only. Native source identities replace the old local-only chronology;
no legacy path or old save reader.

## 7. Verification

Compile as needed; ONE NEW exact bounded case only, with bounded Update stages. Repair/rerun only
that case. Verify normal physical funding and evacuation, complete original source receipts,
current selection edits, saved twins, replay, privacy and local quantity conservation.
No historical/family/full/Oil/platform runtime.

## 8. Risks

Aliases must not double-count a store. Physical geometry and actual resource identity differ.
A source selection edit must not move an acquired claim. Discovery performance breadth is unproven.

## 9. Out of scope

Provider endpoint deletion/fault/corruption, dock/partial-unload/emergency/stress/full accounting,
global durability, full phase closure and commit/push before the full goal and plan finish.

## 10. Execution

Read original owners; migrate exact origin identity; author normal JSON; implement one thin player
proof; focused verification; update evidence/tracker without promoting aggregate acceptance.
