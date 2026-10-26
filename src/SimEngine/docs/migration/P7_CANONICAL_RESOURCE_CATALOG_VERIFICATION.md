# P7-CA1 — Canonical resource ownership verification

Date2026-10-12. Canonical Beep.SimEngine only. Engine2.507.0/save154/replay1; view schemas unchanged.

## Current structural ownership audit

| Concern | Authoritative owner and inspected behavior |
|---|---|
| Catalog and compact identities | ResolvedContent is the sole production IResourceCatalog implementation. Its private resource list and ordinal stable-ID dictionary allocate IDs once. Base/interface indexes reference those actual definitions. Resources and IResourceCatalog.Definitions now share one readonly projection finalized after collection and before resolution. ContentSet caches this typed owner per selected module set; different selections are not parallel catalogs for one world. |
| Native stock/cargo | Storage delegates its actual stock and receipts to ResourceInventoryState, with no second quantity array. Initial stock comes through ResourceTransactions.InitializeStock, not a fallback in OnCreate/load. ResourceInventoryState.Sync restores one sorted native vector and one reservation book, rechecks exact capacity and outstanding promises. |
| Work buffers and networks | Transformer.Wip and NetworkService.Node each embed ResourceInventoryState for their distinct physical endpoint. These are real moved material buffers, not copies of producer/depot balances. Tools retain actual endpoint receipts rather than another tool-stock ledger. |
| Spatial deposits | SpatialResourceEndpoint reads and withdraws from the actual ISpatialResourceQuantity cell. Its persisted book/version contain reservations, not another material quantity. Native layer storage remains the actual deposit balance owner. |
| Promises and history | ResourceReservationBook owns records; Inbound/Outbound promise quantities are derived. ResourceFlowJournal stores historical facts/aggregates, not live inventory used for admission. Temporary port snapshots are detached observations, not writers or cached balances. |
| Optional business | Existing Tycoon BusinessAccounts owns the only _balances array; budget availability derives from its reservations. Supplier is explicit authored creation with a reservation book and no finite-stock copy. The wide physical test composes no BusinessView or business module. All12Tycoon source files remain unchanged. |
| Limits and units | ResourceId.Value and StateSync.Resource use signed int, not byte/enum; catalog/inventory/receipt vectors are dynamic. CapacityPolicyDeclaration calculates actual JSON dimensions and coefficients. No item-count or resource-kind default is added.257 is a bounded fixture, not a game maximum; CLR memory/representability and packet safety remain explicit implementation constraints. |
| Old fields and loads | Current StorageDeclaration/ResourceDeclaration use strict JsonReader fields and reject obsolete implicit capacity/volume/weight/worth/type keys. SaveGame verifies checksum, exact build and save format before building/restoring a session; GameFactory.Load has no migration branch. Current codecs restore exactly one authoritative state layout. |

Inspected sources: ResolvedContent, ContentSet, StorageDeclaration/Storage, ResourceInventoryState, CapacityPolicyDeclaration, ResourceReservationBook, Transformer WIP, NetworkService inventory, SpatialResourceEndpoint, ResourceFlowJournal, optional BusinessAccounts/Supplier, StateSync, SaveGame and GameFactory.Load. Source anchor/freeze metadata identifies the current files, not old overview claims.

## Implementation

The existing catalog publishes its readonly definition collection once, and refuses later collection. Original ID allocation, resolution order, actual aliases, content fingerprint and resource contracts remain unchanged. No additional catalog, stock balance, buffer, scheduler or clock was introduced. Existing proof instrumentation now validates every actual native/independent alias and stable projection, rather than assuming its small fixture always contains two kinds. The public physical helper retains its original expected count2 default.

## Actual build and bounded runtime

ONE solution build: `dotnet build Beep.SimEngine.slnx --no-restore -v minimal`, clean0warnings/errors,7.58s.
NEW FIRST/FINAL:318passed/0failed/0aborted in5384ms; PID21664/process5552ms/Exited0, untruncated/no observer error. No repair or executable edit after FINAL.

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Canonical resources: stable contract aliases wide physical stock and strict authored saves" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

Four authored minute/half-minute completion/cancellation configurations mix255 native andtwo independent resource definitions. A private original Storage contains all257 kinds simultaneously (direct.unused quantity2 respects its actual0.5 discrete-capacity coefficient). The test verifies one row per identity, authored quantities/capacity257 of300, company-private facts, retained catalog metadata, saved twins with no repeated creation grant and interleaved isolation.

Actual supply ID exceeds255 and physically travels by machine into original stock, WIP and reusable tools. Completion consumes two/yields one once; cancellation returns actual input. Current public resource workflow saves/pauses/replays through the same engine owners, not a registered older test case.

Eight obsolete resource/storage field variants andnative/independent duplicate ID refuse before Compose. Checksum-valid old format153 and noncurrent build2.507.9 refuse through both Load andDescribe; current save/hash remain valid afterward. The inspected exact-build comparison has no prior-build migration branch. No business module is needed for work/manufacturing.

## Gate decision and remaining scope

The literal original P7.acceptance.020 is supported by the combined current owner audit and runtime proof: no duplicate resource catalog/balances, hidden resource/item caps, obsolete implicit fields or legacy load path. Only this original gate is checked; its wording/line stay unchanged. Original221 gates become192checked/29open; additional238.

Broader transaction/tool/dock/inbound stress, incident/cargo/spill/topology accounting, selected quality/lot behavior, allocation budgets, global purity, full P1-P12 andthe full goal remain open. ONE authorized final full regression stays deferred until implementation finishes. No Oil/Tycoon business rules/platform/Draw/clock implementation changes; no staging/commit/push.

Metadata-only profile inspection:706registered/included,0excluded/0long markers/0runtime suites; full executor not run.

## Post-pass metadata verification

After FINAL,1821 source anchors,1018 executable inputs and13 quickstart anchors validate with no hash mismatch or executable drift. Repository inventory:3004paths/1083Markdown/8775 local links/0broken;18master rows/0malformed. All221 original gate literals and line anchors validate; only P7.020 checkbox changes. Diff check exits0 (34existing LF/CRLF warning lines);2992dirty paths/0staged. All12Tycoon source files andsole clock match the prior checkpoint. No full regression, staging, commit or push occurred.
