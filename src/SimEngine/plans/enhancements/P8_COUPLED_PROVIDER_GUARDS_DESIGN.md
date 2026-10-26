# P8-NC2: coupled conversion provider guards — Design Document

Current selected qualification on unchanged2.723: [M76 exact evidence](../../docs/migration/P8_ENTITY_CONVERSION_VERIFICATION.md#current-m76-physical-mixed-machine-and-material-result--2026-10-22).
The original atomic-inventory literal139/0/two verifier calls qualifies two healthy
saved flows/eight declared malformed callback modes on captured M75 build2. It was
not rerun after the separate new physical mixed-result test/content. This is not
every IResourceConversionEndpoint implementation or P7-D10's separate CoupledClaimOutlet.
The original implementation rationale below is historical; no new native fix here.

## Historical NC2 design

## 0. Problem Statement

ResourceTransactions.PrepareConversionOutputs treats unknown CapacityStatus as healthy NoCapacity and calls capacity providers without the original full scoped observer. Coupled mutable callbacks verify stock/books but not the game clock explicitly. The atomic extension contract needs the same first-fault boundary as existing observed capabilities.

## 1. Goals

1. Pure output capacity runs inside original EntityObservation with actual provider/entity scope; undefined statuses fault rather than wait.
2. Coupled mutation permits only declared stock/book changes, not clock/identity or an already latched fault.
3. Independent JSON inventory delegates the original ResourceInventoryState, proving the atomic contract without native component inheritance or duplicate balances.
4. Healthy full-buffer tooling/rates/saves/replay stay deterministic; malformed/read-mutation/clock/throw/no-op/extra-stock commits produce no successful conversion prefix or healthy checkpoint.

## 2. Design

Standard/ResourceConversions.cs wraps complete capacity callback plus status validation in EntityObservation.ReadProvider; keep all captured endpoint checks. Before ConvertReserved capture the original global observation scope, verify it afterward and require a healthy world before advancing/verifying expected stock. No new fault owner, coordinator or clock.

Engine-side AtomicInventoryProof delegates the same StorageDeclaration/ResourceInventoryState and exposes IStorage/IResourceConversionEndpoint. Its explicit JSON conversion_behavior selects healthy or a deliberately illegal callback; fault probes contain no player policy or substitute gameplay. Existing connected proof module registers this independent inventory and observes deferred recipe facts for negative-prefix verification.

## 3. API surface

No new production API, JSON or save schema. Existing atomic contract remains mandatory for coupled providers. Independent proof JSON explicitly declares conversion_behavior; all normal storage fields use the shared declaration.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| Modify | Standard/ResourceConversions.cs | 12 | high |
| New | tests/ExtensionProof/AtomicInventoryProof.cs | 100 | medium |
| Modify | connected proof module registration/diagnostics | 8 | low |
| New | JSON/public player proof and stage evidence | scoped | medium |
| Modify | engine/protocol identity and tracker | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Actual finite full net-zero inventory | Ready/time-based commit; same stock/book |
| Unknown capacity status/throw/pure stock or clock change | Original first fault; no healthy wait/result |
| No-op, extra-stock, throw or clock commit | Exact final guard fails before conversion history publication |
| Valid native capacity refusal | Existing typed gameplay blocker remains |

## 6. Backward compatibility

Development only. No shim, alternate transaction, fallback buffer, provider sandbox or rollback fiction.

## 7. Verification

One solution compile and necessary focused repairs. One NEW exact bounded case with public players/JSON/master/Update/immutable views/saves/replay. No historical/family/full runtime; arbitrary provider and multi-endpoint stress remain open.

## 8. Risks

Faulted partial mutations cannot be rolled back honestly; the existing terminal fault prevents healthy saves/results. Independent proof owns no new resource bookkeeping; it delegates original state. Negative callback diagnostics are instrumentation, never gameplay authority.

## 9. Out of scope

Whole multi-endpoint/native-tile/mixed-media/fault/stress acceptance; Oil/platform/Draw/advanced Tycoon/protected G19/clock source edits or publication.

## 10. Sub-task execution order

1. Guard actual capacity and coupled mutation through original observer/fault owner.
2. Author independent endpoint behavior using original declarations/state.
3. Prove healthy full-buffer execution plus deliberately illegal callbacks.
4. Refresh current evidence/master/source audit without narrowing original requirements.

## 11. Scoped result — 2026-10-12

Core2.474.0/save154/replay1. Original capacity observer rejects unknown statuses/pure mutation; original coupled commit scope rejects clock/identity mutation or a latched fault before publishing conversion facts. Independent authored inventory delegates shared storage metadata/state, with no native component inheritance or duplicate stock/book.

ONE NEW exact FINAL139/0/0 in882ms, PID30072 Exited0/process1046ms; two healthy minute/half saved/replayed full-buffer workflows and eight explicit illegal callback modes. FIRST136/1 retained: corrected half-step cancellation accounting, not engine behavior. Clean solution8.31s and focused repair5.48s; all evidence in [verification](../../docs/migration/P8_COUPLED_PROVIDER_GUARDS_VERIFICATION.md). No executable edit after FINAL or old/family/full runtime.

Original221/185checked/36open unchanged; additional200. Multi-endpoint/native-tile/mixed media/arbitrary CLR/provider/inbound/generation/fault/stress/global/full gates remain OPEN; no staging/commit/push before full goal AND plan finish.

## 12. Metadata closure

Fresh read-only metadata closure:1674 source anchors,929 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2862 paths/1008 Markdown files/8639 local links, broken0. All18 master rows parse and all221 original literal/checkbox/line anchors validate with0errors,185checked/36open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2850/staged0. Protected sole-clock/G19 hashes remain exact; goal_complete=false/additional200. Six prior selected inputs changed, three new executable inputs/five new source anchors; two existing production sources/no new production owner. Original requirements unchanged; no runtime rerun for documentation checks.

