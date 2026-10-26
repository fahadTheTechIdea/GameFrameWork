# P2 mobility and paid supply contracts — 2026-10-01

P2 remains in progress. This closes concrete movement dispatch, vehicle rollout metadata and supplier
consumers, not the full framework separation or strategic logistics stages.
This report is historical slice evidence; newer source/gates are recorded in
[typed-role verification](P2_WORKER_ROLES_VERIFICATION.md).

## Implementation and single ownership

- IMobility.Advance executes in the existing MovementSystem phase. That system no longer depends on
  Mover; the unchanged built-in route algorithm moved into its sole Mover state owner. Entity Update
  remains the only entity-state transition owner.
- IMobilityDefinition supplies authored movement class/speed to the existing definition query. Vehicle
  recipes validate both runtime and definition capabilities; rollout uses the declared navigation class.
  Missing metadata refuses content, never silently selects land. MoverDef.Class was renamed in place
  to MovementClass; JSON's existing class field remains its authoritative authored field.
- ISupplier supplies nonmutating bounded offers and paid physical pickup through IEconomy. The built-in
  Supplier is an explicitly authored unlimited catalog. External FiniteSupplier uses its entity's sole
  independent IInventory, with no Storage, Supplier proxy or duplicate stock ledger.
- LogisticsService uses those capabilities through its existing matching/load coordinator. Job.Purchase
  records paid intent in the existing WorkBoard state. An owned-stock transfer does not charge its owner
  merely because its endpoint also implements a supplier. State/hash layout deliberately advances to
  engine build beep-simengine-2.3.0, save format 3; there is no old-layout migration codec.

The non-friend ExtensionProof assembly registers JSON-authored lane mobility and finite supply content.
The tests exercise standard production/commands/movement/jobs, timed travel and paid delivery, invalid
navigation previews, changed-route refusal, entity state projection, incompatible offers, protected stock,
insufficient funds without state changes, physical payment timing, owned free transfers and mid-travel/
mid-purchase save/load/hash continuation. Existing test instrumentation supplies obstacles/fixture stock;
it does not implement scheduling, hauling, movement or state transitions in a host.

## Current gates

```powershell
# Framework root
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- --suite Capabilities
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- --all
pwsh -NoProfile -File docs/migration/VerifyExtraction.ps1
Get-FileHash src/Beep.SimEngine/bin/Debug/netstandard2.1/Beep.SimEngine.dll -Algorithm SHA256

# Oil active-v2 root
dotnet build SimEngine.slnx
dotnet run --no-build --project OilCo.Tests

# Original OilFieldDays repository
git diff --check
```

Both solutions build with zero warnings/errors. Full framework: **452 passed / 48 suites**, zero failed,
none skipped, **0.4 seconds**. Focused capabilities: **131 passed / 11 suites**, zero failed, **0.2 seconds**.
Three new suites plus three additional invalid-content checks add **48 checks** over the 404-check metadata
baseline. No original assertions were removed. Fast oil: **155 passed / 18 suites**, zero failed,
**four long suites skipped**, **25.1 seconds**. The final change after that oil run added framework-only
insufficient-funds assertions; runtime and oil executable sources were unchanged.

This source's full oil profile has not been rerun. The earlier inventory-source full oil success remains
historical evidence only, not a current full-oil claim. Git diff --check reported only existing CRLF
normalization warnings, no whitespace errors. The follow-on no-match concrete lookup search exits 1
because there are no matching oil consumer paths, not because a build/test failed.

Ownership verification passes all **121** dispositions/recovery hashes, **52** unchanged namespace-
normalized single-file C# transfers (declared P2 edits excluded), standalone dependencies and absence of
the old runtime. The framework directory is still not initialized as a Git repository. OilFieldDays HEAD
is `7f9d4761ad0427fc1a199e11d77acb81eac32ca6`; dirty source is identified by the executable fingerprint,
not by claiming HEAD alone contains these changes.

Executable identity: **139 files**, SHA-256
`C3797D1184891C9584996ED0F4FA5B1D1C4136043E691EC449047200C64EA42A`.
Runtime DLL SHA-256:
`708EB38BBE8956BA6ABBFBDE82C1B00549420289A952C9FC83E619755243A741`.

## Failures encountered and corrected

The initial new mobility test incorrectly expected an out-of-lane command to be accepted; the engine's
existing preview correctly refused it. The test now checks that refusal and separately blocks a formerly
reachable route after acceptance. The initial finite-source fixture incorrectly requested seat -1 through
the seat lookup; neutral market removal now uses the engine test fixture's entity lookup.

That route fixture initially assigned a read-only ByteLayer indexer, causing CS0200. It now uses the
authoritative ByteLayer.Set mutation, which invalidates navigation correctly. A no-build run after the
failed build used stale binaries and repeated the old test failures; it is not verification for corrected
source. Clean rebuilds preceded all current successful test results above.

## Remaining acceptance

P2 still needs typed roles/requirements, operation/signal/response providers and full event/state/view
identity/schema ownership. P3–P6 remain planned. Atomic finite-source/cargo/selected-carrier reservation,
dock queues, congestion and broader retry/diagnostic guarantees are not implemented by this slice.
An Offer is not a future purchase reservation; competing paid jobs can still re-evaluate finite stock
at arrival. No Tycoon, renderer/platform, client gameplay, remote or package-publication changes occurred.
