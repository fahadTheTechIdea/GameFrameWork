# P2-B3b — Single-owner content lifecycle verification

2026-10-06; [stage](../../plans/enhancements/P2_CONTENT_LIFECYCLE_DESIGN.md),
[sole tracker](../../plans/enhancements/MASTER_TRACKER.md).

## Identity and responsibility

Build `beep-simengine-2.167.0`, save94. Existing runtime system/service/component/view schemas
are unchanged. This is a breaking content-authoring API, not an old-save migration or a facade.

Def and ComponentDef embed the same ContentAuthoring permission owner. Public Read/Resolve and
linked/sibling validation entries are non-overridable. Providers implement protected ReadCore,
ResolveCore, ValidateResolvedCore or ValidateCore. Strict JsonReader.End validation is part of
Read's failure boundary. A failed callback or rejected recursive lifecycle operation permanently
invalidates the definition, even if an extension catches the nested exception. Published metadata
cannot run authoring again. No duplicated per-definition permission booleans survive.

The obsolete optional IResolvedContentValidator path is removed. Every definition is validated
through the same mandatory base entry after all references resolve; components validate their actual
sibling owner. ResolvedContent freezes parts and definitions before returning the existing catalog.
Generated definitions use this same pipeline and captured component declarations. Fresh unowned
distinct components preflight before entity attachment; frozen identity/layout/codec/ownership
setters refuse. No second registry, copied prototype authority or state codec is introduced.

SpatialRegionDef.BuildGeometry creates each runtime area through the same Read/Resolve/Validate/
Freeze lifecycle. It uses the existing provider parser/normalization/containment and explicitly
binds the prototype space. Shape JSON cannot replace identity/space or hide unknown keys. Saved
operating areas rebuild through that same owner, not forged resolved flags or a second loader.

Exactly one unchanged GameClock per session. Its SHA256 remains
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
Game-update counts are read-only coordinates of that clock, not additional current-time owners.

## Exact final gate

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Content lifecycle: single authoring owner, publication and generated metadata"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Content lifecycle: shared prototypes, physical sessions and saved continuation"
```

- Build: zero warnings/errors; MSBuild elapsed3.09 seconds.
- Single authoring/publication/generated metadata:1120 passed,0 failed,592ms.
- Shared prototypes/physical sessions/saved continuation:224 passed,0 failed,761ms.
- Total:1344 assertions in two exact suites, zero final failures/aborted suites.
- Only these two exact suite names ran, including development repairs. No third/family/full run.
- 123 files migrated authoring overrides/base calls;30 files migrated sibling-validation overrides.
  These sets overlap, so they are not153 independent classes or scenarios.
- 81 older test metadata files mechanically migrate current build/save literals; compile only.
  Prior dispatch/preparedness/event/content/protocol suites were not rerun.

## Behavioral proof

The contracts suite inventories actual typed aliases in native/public catalogs, deduplicating only
test observations. It checks publication freeze and rejection of repeat Read/Resolve/validation for
the definitions/components present in those catalogs. Public-only proof types have one authoring,
resolution and validation callback. Generated components bind their actual declared codec and slot.
Identity, module, index, provenance, component key/slot/owner/codec/schema and frozen layout edits
refuse; published component reuse cannot steal ownership or join another catalog. Duplicate fresh
component attachment refuses before assigning an owner.

Throwing callbacks and swallowed recursive calls are injected in each read/resolve/validation phase
for both definition kinds. Failed objects cannot rerun callbacks, resolve, validate, publish or
rewrite identity. Unknown-key reads invalidate authoring instead of becoming generated-content
retries. Invalid actual-owner validation cannot retry. Constructor-generated metadata explicitly
resolves, validates and freezes without fabricated JSON. Reflection is test-only catalog inventory
and entry-point inspection, not gameplay discovery or an engine provider registry.

Native rectangular/painted and public point runtime areas are created through ordinary player
commands, previewed without hash changes, frozen before observation, protected from reauthoring and
restored through save/load. Retained shapes remain unchanged; company-private point-area creation
does not leak another company's geometry. Unsupported identity/space/shape keys refuse.

Eight combinations of native/public incident, policy and worker providers use the existing authored
parallel response DAG. Response work takes game time and consumes four real target inputs exactly
once. Save/load during active response preserves current jobs/claims/areas; original and saved twin
remain hash-identical through completion. A separate session sharing the resolved catalog remains
unchanged. Pause stops both sessions' progress through their sole game clocks. Completed response
checkpoints restore. Tests only author content/submit player commands/drive engine Update/observe
facts; no response scheduler or gameplay implementation was added to the harness.

## Development repairs

The first contracts run found the old optional interface implementation made the entry virtual in
CLR metadata; its now-unnecessary interface dispatch path was removed and mandatory validation
calls the base definition owner directly. The same run exposed a one-seat authoring fixture while
requesting two seats; the fixture now explicitly authors two seats. No runtime seat fallback was
introduced. A generated mechanical metadata patch initially used relative paths against the other
checkout and was rejected before applying; it was regenerated with exact SimEngine absolute paths.

## Evidence scope and remaining work

This proves engine-controlled authoring/publication permissions and the exercised catalogs/workflows,
not sandboxing arbitrary extension code. A provider exposing unrelated public mutators, hiding
members, invoking its own protected hooks or mutating globals still violates the cooperation
contract; global provider purity/isolation/reflection audits remain open. This is not a claim that
every provider getter or every malformed content graph has been exhaustively verified.

P2/full framework remains partial. Wider startup/load/protocol/replay/fuzz/soak/allocation gates,
reserved budgets/DAG executors/templates, fuel/traffic/utility physics, incident science/team-supply
reservations and final framework acceptance remain open. No Oil/Tycoon/host/demo/platform/Draw work.

## Source evidence

[Source manifest](P2_CONTENT_LIFECYCLE_SOURCE_MANIFEST.json) records SHA256 for migrated hook/validation/
metadata files and primary lifecycle/proof/test sources, including the unchanged sole clock. It is
verification evidence only, not another content manifest or runtime registry. All212 source
fingerprints and6 stage/evidence links verified after documentation reconciliation. Scoped git diff
whitespace check passed; existing LF-to-CRLF notices are informational. Source searches confirm no
old public authoring override, optional validator or _read/_resolved boolean declaration survives.
An initial broad search also matched an unrelated runtime incident expression over its long-valued
resolved timestamp; declaration-aware inspection confirms that is not an authoring permission flag.
