# P2 capability slice — 2026-10-01

P2 remains in progress. This report supersedes historical P1 test counts for the current capability
slice, but does not close the full separation/generalization plan.

This report records the capability-slice fingerprint below. Subsequent versioned-composition changes
have separate [current evidence](P2_COMPOSITION_VERIFICATION.md); counts/limitations here are historical
to this slice rather than current completion claims.

## Implemented ownership

- `IEntityCapability` queries use the existing prototype slots/entity indexes. Querying inherited
  contracts returns the sole implementing component; ambiguous owners fail content resolution.
  Spread buckets, creation/removal and restore continue using the existing entity store.
- `IWorker`/`IWorkerRoles`, `IMobility`, `IInventory`, `IInletPort`, `IOutletPort`, `ITransporter`,
  and `INetworkPorts` expose current component state. `IOutletPort : IInletPort` is enforced by type.
- Standard scheduling, hauling, production, construction inventory, player stock/route/movement
  commands and network transfer consume capabilities. MovementSystem is still the single owner
  of the built-in Mover algorithm; it is not a parallel navigation implementation.
- Transport is explicit JSON content. Four oil haul vehicle definitions and the colony truck declare
  `transporter`; it points to existing cargo inventory, with no new buffer or reservation ledger.
- Inlet receiving checks compatibility and unpromised capacity; outlet taking protects stock promised
  to another pickup. Load planning respects cargo reservations. Network pulls return unaccepted
  quantities to the existing network pool.
- External `ContractWorker` and `SingleItemPort` definitions/components compile in the non-friend
  `ExtensionProof` assembly. The worker completes physically supplied, timed construction without a
  built-in Worker; the ports execute ordinary route and stock-target commands without Storage.
  The specialized one-item port owns its one buffer; it is not another inventory beside Storage.
  Entity Update settles the external state source, and state/promises use existing Sync/save/hash.

## Gates

Framework root:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- --all
pwsh -NoProfile -File docs/migration/VerifyExtraction.ps1
```

Build: zero warnings/errors. Full framework: **277 passed, zero failed, 35 suites**, none skipped,
0.4 seconds. The five capability suites add 44 checks to the previous 233 framework checks.
Ownership/recovery: all **121** dispositions/archive entry hashes pass; **68** otherwise unchanged
single-file C# transfers match after namespace normalization. The verifier explicitly names 13 later
capability source migrations rather than falsely treating their edited bytes as P1-identical. Original
recovery archive hashes remain unchanged. New files/split fixtures have separate current test coverage.

Oil root `Beep.OilFieldDays/src/oilCo-engine/src/SimEngine`:

```powershell
dotnet build SimEngine.slnx --no-restore
dotnet run --project OilCo.Tests --no-build -- --all
```

Build: zero warnings/errors. Full oil regression: **186 passed, zero failed, 22 suites**, none skipped,
560.4 seconds. This includes Frontier, programmed-player demo, scale budgets, offshore, incidents,
visibility, research, lockstep and client purity/interaction. Combined current coverage: **463 checks
across 57 suites** (original 410, nine earlier boundary checks, 44 new capability checks). The full
oil runner used the same runtime DLL hash recorded below; later edits only affected framework proofs/docs.

Original repository `git diff --check` passes (Git reports existing LF/CRLF normalization warnings).
Target is not a Git repository. No repository initialization, package publication, Tycoon, older sibling
engine, renderer or platform SDK changes were performed.

Executable-source fingerprint: SHA-256
`918CF7ACA8A74C95F7A576B06DE6247EB6AD36A574F410FFF78EE0CAA4C1EE69` over 136 files.
Scope: all `.cs`, `.json`, `.csproj` beneath framework `src`/`tests`, its two root build policies and
solution, plus oil active-v2 `.cs`, `.json`, `.csproj`, `.props`, `.targets`, `.slnx`; include ignored
source files, exclude `bin`/`obj`. Canonical UTF-8 input is ordinal-sorted `framework/` or `oil/`
relative slash paths, tab, uppercase file SHA-256, LF (including final LF). Documents/archive are
outside this executable fingerprint. Framework runtime DLL and oil test-local runtime DLL match
SHA-256 `2107B43081F5F5AB90FB6CA1F3F6C32C18B42948B305A45F38AB8674EE640CA3`.
`VerifyExtraction.ps1` emits the current source fingerprint using this algorithm.

## Open scope and limitations

Typed roles/requirements; operation/signal/response interfaces; remaining oil concrete inventory
consumers; supplier/economy policy; module versions/provider ownership validation remain P2 work.
Tile space and the minute calendar are still mandatory (P3). Activity DAGs, automatic policy control,
selected-carrier atomic reservations, docking/congestion and other generic module promotions are not
implemented by this slice (P4/P6). The specialized endpoint proof is not a full arbitrary transfer-fault
or logistics stress gate. P5/P6 domain parity and final acceptance remain open.

The first proof draft assumed the custom outlet would be the nearest source, but an ordinary depot
was nearer under the engine's distance rule. The test now establishes the depot's stock target through
a public command, so it proves custom-endpoint matching while respecting another store's policy.
A test-only route-result long/int assignment error was fixed; no runtime fallback was introduced.
