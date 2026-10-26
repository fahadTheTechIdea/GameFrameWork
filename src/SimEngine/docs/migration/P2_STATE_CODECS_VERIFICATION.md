# P2 explicit state-codec declarations — 2026-10-04

P2 remains in progress. All reusable codec/composition/persistence rules live in Beep.SimEngine.
OilFieldDays only declares schemas for its ordinary extension providers. Hosts/demos are unchanged.

## Implementation

- The existing ContentTypes component entries, World system slots and ServiceContainer persistent
  entries capture immutable StateCodecRegistration kind/ID/schema/module/contract metadata. They
  remain the sole registration authorities. Metadata contains no providers or executable delegates.
- GameSystem and IPersistentService require StateSchemaVersion; Component<TDef> requires an explicit
  stateSchemaVersion argument. Positive versions and stable IDs validate before Initialize. No default
  schema or unversioned registration overload is retained. Identity is scoped by component/system/service
  kind; it is not a CLR type name. Authors remain responsible for versioning their executable contracts.
- Service aliases reuse the same registration by instance identity, captured at first Bind. Later
  provider getter mutations cannot change identity/schema/ownership. Distinct service instances cannot
  share one codec ID. Existing cross-module binding rules remain unchanged.
- System IDs/schemas/schedules capture once at AddSystem. Invalid default/negative-phase schedules
  refuse composition. Execution, fault phases and save sections use captured declarations rather than
  rereading provider getters. Ordered scheduling facts participate in the saved/hash manifest.
- Generated component definitions must resolve the same active declaration as authored components.
  Standard generated SiteDef/LineWorkDef codecs are explicit. Missing or inactive providers refuse
  resolution; generated parts receive stable Key and StateCodec without an inferred identity.
- World.Sync checks the canonical codec manifest before core/map/entity/system/service payload restore.
  Saved count/kind/ID/owner/schema and ordered system schedule mismatches throw CorruptStateException.
  The same IStateSync owns write/read/hash; HashBreakdown adds a codec-identity part. Provider payloads
  stay in their existing Sync methods. No serializer registry, additional state store or restore fallback.
- PlayerView.StateCodecs returns read-only captured declarations, sorted by kind then ordinal ID.
  It never exposes serializers/providers and does not evaluate gameplay. The external proof assembly
  implements the new contracts without friend access or reflection into framework implementation.

Build is **beep-simengine-2.10.0**, save format **6**. Older builds/layouts refuse deliberately.
The existing header/menu Describe checks engine/module/content identity; the runtime body validates
codec declarations at load. This does not yet add codec inspection to save-menu headers/replays.
Core/map payload layout remains identified by the engine build; generalized world-layer capabilities
are P3 work. Operation/signal binding schemas and P3–P6 remain open; this is not extraction completion.

## Commands and current results

```powershell
# Framework root
dotnet run --project tests/Beep.SimEngine.Tests -- --filter 'State codecs'
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- --all
pwsh -NoProfile -File docs/migration/VerifyExtraction.ps1
Get-FileHash src/Beep.SimEngine/bin/Debug/netstandard2.1/Beep.SimEngine.dll -Algorithm SHA256
git diff --check

# Active oil-v2 root
dotnet build SimEngine.slnx --no-restore
dotnet run --no-build --project OilCo.Tests
dotnet run --no-build --project OilCo.Tests -- --all
```

Both builds passed with zero warnings/errors: framework **4.79 s**, oil **3.28 s**.
Focused codecs: **86 checks / 5 suites**, zero failures, **0.1 s**.
Full framework: **953 / 84 suites**, zero failures, none skipped, **0.7 s**.
Fast oil session **59172**, terminal exit 0: **189 / 20 suites**, zero failures, **30.7 s**;
**four long suites skipped**. Full oil session **82663** subsequently finished, terminal exit 0:
**220 checks / 24 suites**, zero failures, none skipped, **640.6 s**. Combined source-scoped
framework/oil coverage: **1,173 / 108**. This closes 2.10.0/save 6 only, not the later unverified
2.11.0 binding-schema source; see the current master baseline.

The preceding event-source full oil session **76536** is terminal exit 0: **220 / 24 suites**, zero
failures, none skipped, **647.8 s**. It used the already loaded 2.9.0 DLL and is not the current codec
source's full gate. See [event evidence](P2_EVENTS_VERIFICATION.md).

New checks cover invalid IDs/versions/schedules, early initialization refusal, alias deduplication,
captured mutable getters/fault identity, generated registration/active ownership, identity/schema/
schedule/order/count restore refusal before provider payload reads, immutable diagnostics, external
component/system/service restoration, interleaved sessions and deterministic saved continuation.
All existing lifecycle/lockstep/concurrency/physical work/operation/response coverage remains passing.

## Source identity and corrections

Ownership gate: **121 original dispositions/recovery hashes**, **41** unchanged namespace-normalized
single-file C# transfers. Four existing oil files now explicitly declare state schemas and are listed
as authored P2 changes, reducing 45 to 41 without removing coverage. Standalone dependencies are clean;
the original runtime remains absent. Current executable identity: **171 files**, SHA-256
`A9D9AB1E42B97AB1378E82BD888402B277ED192EFFC4756344C9EDA7BAF435DE`.
Runtime DLL SHA-256:
`FB1DE57D837D5EF617C34280D450DA76CE9950D125EE65A3BB9EC770E7154B5D`.
Documentation is excluded from the executable identity. Both repository diff checks pass with
line-ending normalization warnings only. Dirty source/recovery archive preserved; no Git initialization,
remote configuration, package publishing, Tycoon, older engine or platform work.

The first new proof build failed because ComponentList has indexed access, not IEnumerable. The proof
now uses its existing Count/indexer contract; no framework collection adapter was added. A mistakenly
supplied oil `--fast` argument selected zero suites and returned exit 1. It is not evidence of a pass;
the correct no-argument fast runner above subsequently passed all 20 suites. No tests were deleted.
