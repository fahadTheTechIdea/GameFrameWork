# P11-D17 — Optional remote utility signals

> Current continuation D20/build2.574.0: real native timed utility consumption through remote telemetry, native/independent metadata variants. Focused262/0 with physical supply, shortage, shutdown, saves, replay and conservation. Runtime/codec unchanged; D17–D19 counts are historical, not rerun wholesale. Full acceptance stays open.

## 0. Problem

Local execution gates and non-tile utility consumers already exist. There was no engine-owned remote binding for a sensor to use another owned entity's utility facts. Hosts must not forward, cache or calculate those values.

## 1. Goals

Provide one optional contract-based binding owner; author provider/source/metrics through master-listed JSON; use ordinary player commands; preserve exact live signal types, saved references and privacy. Missing or incompatible targets must block, never silently choose another provider.

## 2. Design

`IRemoteSignalDefinition` declares actual compatible provider prototypes and the source capability. `IRemoteSignalBinding` owns target admission and configuration. The sole `RemoteSignal` runtime stores only the selected entity reference and forwards through the existing guarded `AutomationSignalContracts.ReadFrom` reader. It owns no utility balance, consumption loop, cached value, scheduler or clock.

The native `remote_utility_signal` JSON definition declares `id`, a nonempty unique `providers` array, `source` and explicit `target:null`. Live IDs enter through commands, not prototype JSON. Its registered metrics reuse the existing utility metric schema. Native and independent definitions share `RemoteSignalDeclaration`; there is no singular-provider compatibility parser. The independently compiled definition uses that same parser and original runtime/codec, with no duplicate reference owner. `RemoteSignalContracts` validates actual frozen aliases, runtime/schema agreement and each non-remote provider before play, then guards metadata during query/admission. Broad arbitrary mod/fault coverage remains open.

Player admission requires a currently disclosed owned target. Ongoing telemetry requires the same actual live owner and one explicitly authored compatible prototype/source. It does not recursively invoke visibility while sensors themselves evaluate visibility. The original signal reader validates content/schema and source callback purity. Chained remote providers are refused by the shared validator to prevent recursive feedback. A missing/retired/changed provider produces typed unavailability; no substitution occurs, even when another listed provider is supplied.

## 3. API and state

Optional `RemoteSignalsModule` requires operations only. `BindRemoteSignalCommand` binds or explicitly unbinds with EntityId.None; previews and execution use the binding contract. `RemoteSignalsView.ReadBinding` returns detached owner-scoped identity/blocker facts. Existing reference-replacement admission refuses incompatible prototype replacement; compatible replacement retargets the reference before retirement.

Current build2.574.0/save157/replay1. Selected component codec1 and view1; existing payloads unchanged. No legacy reader or compatibility path. Replacement checks now observe the same frozen metadata through RemoteSignalContracts, not an unguarded metadata callback.

## 4. Files

Standard/RemoteSignals.cs; current build/protocol identity; RemoteSensorTests.cs and runner registration; embedded master-listed point-remote-utility.json. No Oil, Tycoon, host, demo, platform or clock changes.

## 5. Errors

Unbound uses signal.remote-unbound. Wrong prototype uses signal.remote-incompatible. Missing/foreign targets are NotFound. Invalid provider/source references or live target IDs in prototype authoring fail before play. Provider errors remain subject to the original first-fault reader, not a local recovery/default.

## 6. Compatibility

Development-only new optional contracts and schema. No fallback implementation, obsolete path or duplicate utility model.

## 7. Verification

One bounded public-command/non-tile focused case, native and independently compiled metadata variants: physical supply, remote readiness and quantities, 77-unit separation, pending saves, saved physical checkpoints, explicit alternative selection with no fallback, unbind revocation, detached retained views, replay, independent session and invalid JSON. Current92/0; D17's37/0 is historical. No direct World/entity/stock/progress writes in the driver.

## 8. Risks

D19 proves selected empty-provider physical replacement/removal through ordinary staffed JSON activities, not whole stock-bearing/reserved lifecycle. D20 adds a separate master-listed native processor: original hauling supplies operating material, original TransformerManager consumes one unit per two productive minutes across a24-minute batch, then produces one authored unit. Remote typed consumed quantities match the actual current processor, shutdown burns nothing, completed execution becomes inactive rather than inventing a lifetime counter. Journal totals prove12 consumed/1 produced and conservation of the20 authored starting units, including current stores/cargo. Both metadata variants save/replay identically. No runtime consumption or telemetry state was added to the proof driver.

Arbitrary metadata/provider faults, ownership changes, incompatible upgrade refusals, broader network/freshness/condition providers and stress/lockstep remain unverified. Chaining is deliberately refused. No original broad acceptance gate closes.

## 9. Exclusions

Automatic target selection, range/communications infrastructure policy, foreign-company telemetry, new consumption algorithms, platform rendering/adapters, full regression and Git publication.

## 10. Execution status

Implemented and focused verified. Initial visibility recursion was fixed by keeping disclosure at link admission and using the original scoped typed source reader during live gates. Initial one-dimensional fixture passage was blocked by another machine; the authored supplier is now reachable at3 and the remote sensor remains at80. Physical collision rules were not changed. See [evidence](../../docs/migration/P11_REMOTE_SIGNALS_VERIFICATION.md). The full goal remains open.
