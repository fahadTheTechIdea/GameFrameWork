# P4-C2a strict authored-content verification

Date: 2026-10-04. Working-tree build `beep-simengine-2.22.0`; save format 11 unchanged.

## Implemented

- ContentTypes accepts registered sections only. The special `patches` allowance is removed.
- ResolvedContent collects authored entries without overlay application. ApplyPatch and dotted-key
  removal are deleted. JsonValue.MergeFrom is removed, not renamed or wrapped.
- The existing base-type catalog aliases retain their collision authority. Duplicate errors identify
  both module owners; authored read/collision errors identify current module/version/file/section/entry.
- ExtensionProof now authors `extra.bin` in `extra/defs.json`. It uses the existing counter/presence
  contracts without rewriting `bin`. Manifest and older fixture references migrate in place.
- ContentSet/ResolvedContent remain the only loader/catalog. No added scheduler, override priority,
  compatibility loader or host gameplay. This slice adds no live persisted layout.

## Exact commands and results

Run from `C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine`:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Strict content: removed overlays, collisions and owner diagnostics"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Strict content: explicit files, selected definitions and saved player sessions"
```

Final build: zero warnings, zero errors. Refusal suite: **20 passed / 0 failed**, 33 ms.
Selection suite: **21 passed / 0 failed**, 99 ms. Combined **41 checks / two exact suites**.
The selection suite initially aborted because its test treated strict unknown-item lookup as a nullable
query. The test was corrected to inspect Find<ItemDef> and separately prove ItemId throws. Only that
suite was rerun; the already-green refusal suite has no subsequent runtime/proof changes.

## What the checks prove

- Old overlay sections, including empty ones, and embedded merge fields refuse explicitly.
- Cross-owner, same-file and cross-file duplicates cannot override. Error context is deterministic;
  failed resolution neither mutates a cached base catalog nor caches a partial invalid one.
- Master-listed extra files add resources/entities to the same selected catalog. Unlisted files are
  not scanned. Inactive definitions remain absent; unknown resource lookup refuses without a default.
- Base and extension configurations retain separate owners/values. Module/component lists are sealed;
  selected modules and document enumeration normalize to stable catalog identity and resource IDs.
- Ordinary player commands and engine ticks remain deterministic across interleaved sessions.
  Save/load/continue preserves identity. Changed active content refuses saved state, while changed
  inactive content leaves a base-only saved identity intact.

## Scope and remaining work

Only Beep.SimEngine changed. No oil, Tycoon, Unity/Godot, adapters, rendering, host or demo changes.
Migrated Framework/Composition/CommandInbox/ContentOwnership fixtures compile; their older suites
were not executed. No broad/full regression, lockstep or oil-integration pass is claimed.

C2a is independent and advanced before optional-module decomposition. C1, remaining import/root
requirements and C3 inactive-provider scenario discovery remain open. Construction still embeds work
and generates site/salvage definitions. Its replacement needs the approved P4 graph and P7 resource
transaction work; no temporary second project model was introduced. Arbitrary DAGs, saved node IDs,
consuming acquisition and the larger framework roadmap remain incomplete.

The lifecycle skill guided staged design, single-owner migration and evidence tracking. Repository/user
instructions override its generic full-regression recommendation; only exact affected suites ran.
