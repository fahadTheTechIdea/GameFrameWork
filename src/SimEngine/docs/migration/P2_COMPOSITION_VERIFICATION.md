# P2 versioned composition slice — 2026-10-01

This slice implements module identity/provider ownership inside the existing composition and registries.
It does not close P2 or the P1–P6 framework generalization goal.
These counts/fingerprints identify the composition baseline, not subsequent inventory changes.
See [newer inventory evidence](P2_INVENTORY_VERIFICATION.md) for current-source gates.

## Current responsibilities

- `GameModule` requires an exact `ModuleVersion`; dependencies are typed `ModuleRequirement` values,
  not unversioned strings. GameDefinition validates duplicates, missing/version-mismatched references,
  cycles and self-dependencies, then orders ready modules by ordinal ID, independent of enumeration.
- The schema-1 master JSON manifest declares all code modules, including modules with no documents.
  Every module's identity/version/dependency set matches its registered code contract. No unversioned
  manifest fallback exists. Canonical relative document paths reject traversal/absolute/alias paths;
  a document has one owner. Module/document order is canonical before content resolution.
- Existing content entries record provider module ownership and declarations freeze. Inactive component/
  section providers fail explicitly; inactive generators do not run. Definition/component codec types
  cannot be declared under competing aliases. Existing resolver and generated-definition path remain.
- WorldComposer scopes registration to its composing module. Service/command/system entries record
  the owner. Service Bind refuses replacement. Explicit service requirements and persistent codec IDs
  validate before any system Initialize. Aliases use instance identity, not overridable Equals, and
  serialize once through the existing Sync owner. World-generation IDs and system IDs/orders are unique.
- Commands expose immutable registration entries/read-only entry collections. Late providers, commands
  and content declarations refuse mutation; no parallel ownership registry or command bus was added.
- StandardModule requires IEconomy and no longer creates FreeEconomy, including work/haul fallback paths.
  Oil's JSON-authored free policy remains valid; ordinary oil companies and colony ledger bind explicitly.
- Oil extraction, founding, sales, disposal and power fuel consumers use existing IInventory/IWorker.
  Production room, exports/disposal and fuel consumption respect incoming/outgoing inventory promises.
- WorldSettings stores/hashes the active module versions. Save headers, restoration, load-menu matching
  and lockstep identify versions, including code-only modules with unchanged documents. Unsupported old
  builds/formats refuse before settings/body decode. Engine build is `beep-simengine-2.2.0`, save format 2.

## Current gates

Framework root:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- --all
pwsh -NoProfile -File docs/migration/VerifyExtraction.ps1
```

Build: zero warnings/errors. Full framework: **336 passed, zero failed, 40 suites**, none skipped,
0.3 seconds. Five new composition suites add **59 checks** to the prior 277. They exercise external
module registration, canonical code/manifest order, invalid versions/dependencies/cycles, active capability
ownership, early provider/codec collisions, freeze, alias persistence, versioned saves and peer identity.
Existing lifecycle, inbox/lockstep, visibility, physical work and capability proofs remain in the full run.

Ownership/recovery: all **121** original dispositions/archive hashes pass. **52** otherwise unchanged
single-file C# sources still match after namespace normalization; each later P2 capability/composition
migration is named explicitly in the same verifier. Original recovery bytes were not rewritten.

Oil active-v2 root:

```powershell
dotnet build SimEngine.slnx --no-restore
dotnet run --project OilCo.Tests --no-build
dotnet run --project OilCo.Tests --no-build -- --all
```

Build: zero warnings/errors. Fast: **155 passed, zero failed, 18 suites**, four long suites skipped,
28.5 seconds. Full: **186 passed, zero failed, 22 suites**, none skipped, **527.0 seconds**.
Includes Frontier, programmed-player demo, budget reporting, offshore, incidents, privacy, research,
lockstep and thin-client gates. Combined with framework: **522 checks / 62 suites**, zero failures.
This is current executable evidence, not inferred from the earlier capability-slice pass.

Current executable identity (the existing verifier's documented algorithm): **139 files**, SHA-256
`745B2947024735FCDB1C8D780F3A81CEACB37206859E40709AB79ACF3872B516`.
Framework runtime DLL SHA-256:
`DA98BC14040EEBE44ED209F95046E3FEB59C206CC5C7CE69786C29B20B11A58F`.
Target remains a local directory, not an initialized Git repository. Original Git whitespace gate passes,
with the existing LF/CRLF normalization warnings. Tycoon, old sibling engine, rendering/platforms,
repository initialization and package publication remain untouched.

## Failures corrected and remaining acceptance

The mechanical version migration initially added overrides to non-module systems/stages; the compiler
caught them and the migration was restricted to GameModule declarations. Initial composition probes
used a 4x4 map (below the inherited tile-map minimum) and omitted authored default entity state needed
by StandardModule's generated entities. Fixtures were corrected to valid current content, so refusal
checks now reach the intended validation. Removing platform-style map caps remains P3 work.

P2 remains open for typed roles/requirements, supplier abstraction, operation/signal/response interfaces,
remaining standard producer validation/salvage concrete lookups, and complete event/state/view ownership/
schema declarations. Module implementations must keep identity/dependencies stable after declaration;
stronger immutable module-description snapshots should be audited alongside remaining provider ownership.
P3–P6 time/space generalization, DAG/policy/logistics/control promotions and final acceptance remain open.
Imports across explicitly declared content roots and reusable standard content ownership remain P4 work;
relative-path validation alone does not prove those future import requirements complete.
