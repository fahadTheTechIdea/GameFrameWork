# P2 named operations and typed signals — 2026-10-01

P2 remains in progress. This slice completes a reusable named operation/control and signal-reading
seam, not response execution, conditional automation, full provider schemas or the entire extraction.

## Implementation and ownership

- Core/OperationFacts.cs defines client-safe readonly operation, bounded control and typed signal facts.
  Standard/Operations.cs owns IOperation, IControllableOperation, IAutomationSignalSource, the shared
  authored OperationSettings parser, AutomationSignalDef and the configure-command handler.
- INamedEntityCapability and INamedCapabilityDefinition extend the existing component/definition lists.
  Authored IDs select a single owner inside an entity. Duplicate IDs or missing metadata refuse content;
  runtime provider identity must equal its definition. Unqualified named capability queries refuse.
  There is no parallel named-capability index, operation store or state machine. Existing singular
  inventory, worker and mobility ownership remains unchanged.
- Producer owns enable/target in its existing codec. Mandatory operation JSON replaces its old rate
  field. ProductionSystem checks enabled before spending, progress or output, uses the bounded target,
  and preserves queued work and spent resources when disabled. Normal cancellation remains authoritative.
  Entity Update alone commits entity state; operation facts project existing provider state.
- ConfigureOperationCommand requires entity and operation IDs. Commands and private OperationsView
  previews use one ownership/bounds/provider validation path. Views expose detached facts, not World
  or executable providers. Named enumeration exposes authored IDs without guessing a preferred operation.
- JSON signals use the existing module-owned content registry and manifest. Numeric/boolean readings
  have explicit units and a nullable value; unavailable metrics return a typed blocker, not zero.
  Provider kind/unit/identity violations fault on read. Complete pre-initialization binding schemas
  remain open. Contractual pure/deterministic providers are not a sandbox for arbitrary module code.
- Oil PowerSource exposes its existing saved Running result under the power ID. Offshore platforms
  also expose production under its own ID. Oil fuel/power timing stays in the oil engine assembly.
  FieldPlayer reads actual operation facts instead of duplicating a fuel-stock running heuristic.
- Non-friend ExtensionProof implements DialOperation with its own controls, typed signal, engine-side
  update and existing component codec. It does not wrap Producer or require scheduler/host gameplay.

Build is beep-simengine-2.6.0; save format is 4 for the new producer layout. Earlier build/layouts are
refused deliberately. No compatibility codec, old rate parser or implicit operation-selection API remains.
OilCo.Game and ExtensionProof have no framework friendship. The white-box test assemblies still do;
this report does not claim every oil acceptance scenario uses only public client APIs.

## Gates

```powershell
# Framework root
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- 'Operations:' -v
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- --all
pwsh -NoProfile -File docs/migration/VerifyExtraction.ps1
Get-FileHash src/Beep.SimEngine/bin/Debug/netstandard2.1/Beep.SimEngine.dll -Algorithm SHA256

# Oil active-v2 root
dotnet build SimEngine.slnx
dotnet run --no-build --project OilCo.Tests -- 'Oil operations:' -v
dotnet run --no-build --project OilCo.Tests
dotnet run --no-build --project OilCo.Tests -- --all

# Original repository
git diff --check
```

Both final builds pass with zero warnings/errors (framework 2.12 seconds; oil 3.86 seconds). Focused framework:
**92 passed / six suites**, zero failed, 0.2 seconds. Full framework: **683 passed / 65 suites**, zero
failed, none skipped, **0.5 seconds**. Focused oil: **15 passed / one suite**, zero failed, 0.2 seconds.
Final-source fast oil (session 27011): **170 passed / 19 suites**, zero failed, **four long suites skipped**,
**27.2 seconds**. Full oil (session **33638**) completed: **201 passed / 23 suites**, zero failed,
none skipped, **571.5 seconds**. Frontier, programmed player, allocation budget and storyteller long
scenarios passed. Combined verified coverage: **884 checks / 88 suites**, zero failures.

The final four-check zero-target correction changes only ExtensionProof and framework tests. OilCo.Tests
and oil gameplay have no reference to that assembly; runtime and oil source remained unchanged after
the full oil launch. The rebuilt framework DLL retains the exact hash below. The subsequent full
framework gate covers the corrected proof. No oil scenario is credited with exercising DialOperation.
No regression session remains running.

The six new framework suites cover command/preview parity, authored bounds, disabled consumption,
pause/resume/cancellation, independent providers, two named operations per entity, singular inventory
ownership, private pure immutable facts, invalid content/results, zero-target idle state, saves and twin continuation. The oil
suite proves actual scheduled power/fuel outcomes, refusal semantics, privacy and saved continuation.
Previous requirement-source counts of 591 framework plus 186 oil are historical, not this source's gate.

## Source identity

Ownership verification passes: **121** original dispositions/recovery hashes, **49** unchanged
namespace-normalized single-file C# transfers after excluding declared P2 edits, standalone references
and no original runtime. FieldPlayer is now an explicit authored-change exception rather than falsely
claimed unchanged. Current executable identity: **154 files**, SHA-256
`F98E51B72C4B198EA14290DEDD39B6071BA87182191FEE705E6347E74AB2A463`.
Runtime DLL SHA-256:
`F8213500CCAEDEE1E23841EFA7A62801C96995F565B54BE1B50F1D066F06E7DC`.
Documentation is excluded from the executable fingerprint. Git diff --check passes, with existing
line-ending normalization warnings only.
Dirty source and recovery archives remain preserved; no Git initialization or publishing occurred.

## Issues found and corrected

1. An initial build lacked the IStateSync namespace import in OperationCommands.cs; the explicit import
   corrected it. Both final builds pass.
2. The initial full framework had 590 passes/one failure in the old save-format-3 assertion. Producer
   state requires format 4; the assertion now checks 4 rather than removing the durability check.
3. The initial oil operation suite failed content ownership: offshore_platform legitimately has both
   power and production. A single operation slot would either forbid it or silently pick a provider.
   Named capability IDs now identify independent owners in the existing slots; no platform special case
   or dropped power capability was introduced. Composite and oil gates pass after that correction.
4. Several source-order apply_patch hunks were rejected before editing. Verified exact full-file
   mechanical transformations through apply_patch completed the migration; no shell-write fallback.
5. Final review found the proof operation returned idle at zero target but still contributed a working
   entity state. Its state source now returns inactive at zero; entity Update resolves the authored
   initial state. Four added checks prove accepted bounds, idle facts, settled state and no work.

## Remaining scope

Response/effect contracts, event/state/view ownership and complete provider-binding schemas remain P2.
Rule evaluation, incidents and atomic logistics reservations remain later module/DAG work. P3 time/space,
P4 reusable optional modules/activity DAG, P5 domain composition and P6 expanded proof/final acceptance
are not complete. Inherited tile-map assumptions are not an acceptable universal framework restriction.
No Tycoon, older engine, Unity/Godot adapter or renderer changed.
