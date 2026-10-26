# P2 authored effects, physical responses and owned views — 2026-10-04

P2 remains in progress. This report covers the response/effect implementation carried forward from
the preceding source and the view-ownership migration implemented today. Neither closes full event,
state-codec or operation/signal-binding schemas, nor P3–P6.

## Single-owner implementation

- WorkEffectDef/IWorkEffect replace the old WorkBoard delegate registry. Standard construction/line
  and oil drilling/survey/repair effects are explicit JSON in manifest-owned effects documents.
  ResolvedContent's existing definition indexes return the same provider instances. RegisterEffect
  and its old delegate are removed; there is no second effect dispatcher or legacy loader.
- ICompositionValidator validates authored and generated work references through the existing board.
  Service aliases validate once before any system Initialize. Effect refusal stops dependent work;
  final-effect refusal prevents upgrade/completion and preserves the typed failure blocker.
- IIncident/IIncidentResponse are named capabilities. Each provider retains one incident state/codec.
  ResponseWork validates and posts normal physical orders through the sole WorkBoard, not another
  response scheduler/store. Orders save/hash their source and episode. Unfinished, forged, wrong-owner,
  wrong-plan or stale-episode completion refuses rather than clearing an incident.
- Responses require positive elapsed machine work. Materials, cash, travel, cancellation and worker
  reservations remain normal work mechanisms. Oil wear owns oil failure probabilities/research;
  storyteller and operating gates query incident contracts. IncidentsView exposes private readonly facts.
- WorldComposer's existing view dictionary now holds immutable ViewFeatureRegistration entries with
  stable ID, positive schema version, contract type and captured module owner. Its factory is private.
  AddViewFeature requires identity/version; there is no old overload. ID/type collisions refuse before
  initialization. RequireViewFeature checks exact ID/version/type after all modules compose.
- The same registry freezes before initialization. Retained composers cannot add declarations.
  PlayerView resolves scoped lazy instances from those entries and exposes sorted, read-only metadata
  without constructing any view. Registered factories returning null fail explicitly, distinct from
  unregistered optional features. Standard, independent proof and oil registrations migrate in place.
- Entity.Update remains the sole entity-transition writer. No host/demo gameplay or platform code
  changed. Providers/factories have contractual deterministic/pure responsibilities; this is not a
  sandbox for arbitrary executable module code. Views are not a second persisted state authority.

Build is `beep-simengine-2.8.0`; save format is **5** (response source/episode and oil episode state).
View metadata adds no mutable simulation state or binary fields; exact module versions and engine build
remain save/peer identities. Earlier builds/layouts refuse deliberately, without a compatibility codec.

## Verification

```powershell
# Framework root
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests -- 'View ownership' -v
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- --all
pwsh -NoProfile -File docs/migration/VerifyExtraction.ps1
Get-FileHash src/Beep.SimEngine/bin/Debug/netstandard2.1/Beep.SimEngine.dll -Algorithm SHA256
git diff --check

# Oil active-v2 root
dotnet build SimEngine.slnx
dotnet run --no-build --project OilCo.Tests -- --all
```

Both final builds succeeded with zero warnings/errors: framework **3.37 s**, oil **9.40 s**.
Focused view tests: **50 passed / 3 suites**, zero failed, **0.1 s**.
Full framework: **804 passed / 73 suites**, zero failed, none skipped, **1.0 s**.
This includes all **71 response checks / 5 suites** and the existing lifecycle, privacy, physical work,
save/load, isolation and lockstep gates. The full oil profile completed under session **86337**:
**220 passed / 24 suites**, zero failed, none skipped, **680.5 s**. Combined source-scoped coverage is
**1,024 checks / 97 suites**, zero failed. The executable source/binary identities below were captured
while the process ran and revalidated before the subsequent event changes. Event implementation
started while that already-loaded process ran; this pass proves the 2.8.0 source, not the later event
source. The process is terminal, exit 0; no response/view regression session remains running.

New view checks cover invalid IDs/versions, duplicate contracts/IDs, exact required contracts, refusal
before Initialize, a consumer composing before its provider, cross-module shadowing, late registration,
lazy null refusal, immutable/pure sorted metadata, scoped caching, session isolation and saved twins.
The external proof assembly compiles against public contracts with no framework friendship. Response
tests also use explicit white-box fault injection; they are not all public-player acceptance scenarios.

## Source and recovery identity

Ownership gate passes: **121** original dispositions and recovery hashes; **47** unchanged single-file
C# transfers after namespace normalization and explicit authored-change exclusions; standalone project
references; no original runtime. PlayerView/SimGame are newly declared authored edits, reducing the
unchanged-source count from 49 to 47 without deleting source or weakening their regression coverage.
Executable identity: **165 files**, SHA-256
`C25E212F482FC1D0735F0CEF2A7704615BC68CD85E247EB18CC7FFED09722E4A`.
Runtime DLL SHA-256:
`277B5555DD6FD05E5A1C94812A6E325D2A160AB8515DAC4827A8D1EF2FBED2CE`.
Documentation is excluded from the executable fingerprint. Both repositories' diff checks pass with
line-ending normalization warnings only. Original dirty source/recovery archives remain intact.
No repository initialization, remote configuration or package publication was performed.

## Corrections and remaining scope

The view registration source was initially created outside the runtime source directory; it was moved
through apply_patch before building, with no duplicate source left. The first build and all focused/full
framework tests then passed. Earlier operation-source 683/65 framework and 201/23 oil passes remain
historical evidence in their report, not current response/view verification.

P2 still needs event identity/ownership, complete state-codec schema declarations and pre-initialization
operation/signal binding validation. Persistent incident propagation/isolation/cleanup, conditional
automation, activity DAG alternatives/reservations and atomic selected-carrier logistics remain later
module work. Configurable time/nonspatial worlds, domain generalization and expanded final proofs are
unimplemented P3–P6 acceptance requirements. No Tycoon, older engine, adapters or renderer was changed.
