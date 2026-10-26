# P2 declared event ownership — 2026-10-04

P2 remains in progress. Engine event mechanisms live exclusively in Beep.SimEngine; oil domain code
continues to publish the ordinary standard events. No host/demo event implementation was added.

## Implementation

- EventBus retains its sole type-to-queue dictionary and global publication-order list. Queue entries
  now carry immutable EventRegistration identity/schema/module ownership. Register<T> declares each
  payload contract once. Require<T> validates exact ID/schema/type, including forward declarations.
- Subscribe<T> requires a stable handler ID and captures the composing owner. Subscriptions belong to
  module composition, not system Initialize or runtime. Forward subscriptions occupy the same queue
  entry their provider later declares; freeze refuses undeclared payloads before any system Initialize.
  Duplicate event IDs/payloads/handler IDs refuse rather than overwrite. No anonymous compatibility
  overload, implicit runtime queue creation, second dispatcher or event state store remains.
- WorldComposer scopes the existing bus to each module; World freezes it before initialization. Standard
  construction/removal/vehicle-production/entity-replacement events are explicitly declared by sim.
  The external test game's event log subscribes during Compose. Its concurrent-input boundary probe
  is now an owned composition fixture, preserving the original command-phase/checkpoint assertions.
- Registered structs dispatch without payload boxing in global publish order. Nested publications append
  to that same pass; handler invocation order remains canonical composition/local declaration order.
  Sorted diagnostics do not reorder execution. Unregistered runtime payloads fail explicitly; a declared
  payload with no listeners is a valid no-op.
- Queue cleanup tracks publication, not successful dispatch. Handler exceptions reset both the throwing
  queue and every not-yet-delivered queue, avoiding stale payloads on a subsequent diagnostic dispatch.
  Existing event-storm protection and World fault handling remain, without swallowing provider failures.
- PlayerView exposes detached/read-only event and subscription metadata, never executable callbacks.
  Common stable ID/schema validation is centralized in ContractIdentity for views/events. Listener-owned
  authoritative state still uses its existing Sync; transient fully dispatched events are not another
  persistence authority. This contract layer is not a sandbox for arbitrary executable extension code.

Build is **beep-simengine-2.9.0**. Save format stays **5**: no binary payload layout changed. Exact engine
build/module versions continue to identify saves/peers; earlier builds refuse without a compatibility path.

## Verification commands and results

```powershell
# Framework root
dotnet run --project tests/Beep.SimEngine.Tests -- 'Event ownership' -v
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- --all
pwsh -NoProfile -File docs/migration/VerifyExtraction.ps1
Get-FileHash src/Beep.SimEngine/bin/Debug/netstandard2.1/Beep.SimEngine.dll -Algorithm SHA256
git diff --check

# Oil active-v2 root
dotnet build SimEngine.slnx
dotnet run --no-build --project OilCo.Tests
dotnet run --no-build --project OilCo.Tests -- --all
```

Both builds passed with zero warnings/errors: final framework **1.80 s**, oil **2.58 s**.
Focused event tests: **63 passed / 6 suites**, zero failed, **0.1 s**.
Full framework: **867 passed / 79 suites**, zero failed, none skipped, **0.6 s**.
Fast oil, session **89669**, terminal exit 0: **189 passed / 20 suites**, zero failed, **28.6 s**,
**four long suites skipped**. Full oil session **76536** completed with terminal exit 0:
**220 passed / 24 suites**, zero failures, none skipped, **647.8 s**. Combined source-scoped coverage:
**1,087 checks / 103 suites**, zero failures. This closes the event-source gates; it does not prove the
subsequent 2.10.0 state-codec source.

The preceding response/view source's full oil session 86337 is terminal exit 0: **220 / 24 suites**, zero
failed, none skipped, **680.5 s**. Its 2.8.0 DLL was loaded before this event migration. That result is
historical source-scoped evidence, not the current event-source full gate. See
[response/view evidence](P2_RESPONSES_VIEWS_VERIFICATION.md).

Event checks cover invalid identities/schemas, collisions, missing/inactive providers, forward and exact
requirements, early refusal, cross-module ownership, freeze, no legacy subscription overload, immutable
pure metadata, handler/global/nested order, session isolation and deterministic saved continuation.
Non-friend ExtensionProof implements an engine-side event module and ordinary saved listener state
using public contracts. Dispatcher exception cleanup uses explicit white-box fault injection, not host
gameplay. Existing lifecycle/concurrency/lockstep/physical-work checks remain passing.

## Source identity and corrections

Ownership verification passes: **121** original dispositions/recovery hashes; **45** unchanged single-file
C# transfers after namespace normalization and explicit P2 exceptions. Events.cs/Systems.cs are now
authored edits, reducing 47 to 45 without removing their coverage. Standalone dependencies remain clean;
the original runtime is absent. Executable identity: **168 files**, SHA-256
`9C76246B103BE7F8D593D354434DB834BBE9F7E8B9F23DE7020ABC9DB1482EAB`.
Runtime DLL SHA-256:
`2ABDC7552DF99E40635106D3C6EE6EF378B95112A43A4095FD0D7186EBF8CF85`.
Documentation is excluded from that identity. Both repositories' diff checks pass with line-ending
normalization warnings only. Dirty source and recovery archives remain preserved. No Git initialization,
remote configuration, package publication, Tycoon, older engine or platform work was performed.

One patch attempting delete/add of the same path was rejected atomically; a full-file apply_patch update
completed the revision. The first six-suite focused run had **56 passes / 1 failure**: the cross-module
test helper inserted JSON at nested array endings. Restricting its insertion to the final module-array
ending corrected the fixture; all 63 focused checks and the subsequent full framework gate pass. This
did not require loosening manifest validation or changing runtime behavior to accept invalid JSON.

## Remaining scope

P2 still needs complete state-codec ownership/schema declarations and pre-initialization operation/signal
provider binding validation. P3 time/spatial generalization, P4 optional modules/activity DAG, P5 oil
composition and P6 expanded independent/final acceptance proofs remain unimplemented requirements.
