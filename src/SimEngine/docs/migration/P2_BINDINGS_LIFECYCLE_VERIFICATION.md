# P2 binding, lifecycle and snapshot verification — 2026-10-04

## Current scope and gate

Only Beep.SimEngine source, its external proof/tests, plans and documentation examples were changed.
No OilFieldDays, Tycoon, Unity/Godot, adapter or platform-rendering source was changed or verified.
The full framework goal remains active; this evidence does not mark P2 or P3–P12 complete.

Current build is **beep-simengine-2.12.0**, save format **7**. The authoritative payload layout did not
change in this slice; build identity still deliberately refuses saves/peers from different engine builds.
No compatibility API, reader or old rendering alias is retained.

| Command/check | Current result |
| --- | --- |
| `dotnet build Beep.SimEngine.slnx --no-restore` | Exit 0, zero warnings/errors, 1.71 s |
| `dotnet run --project tests/Beep.SimEngine.Tests --no-build -- --all` | Exit 0, **1,134 passed / 0 failed / 95 suites**, 0.8 s; no skips/aborted suites |
| `dotnet run --project help/quickstart/Host/QuickGame.Host.csproj` | Exit 0, tick 3 / entity 1 / hash `9FE630B08F812E68` |
| Offline XHTML parsed with XmlReader, resolver disabled | Valid |
| Current oil integration | Not attempted; historical oil passes are not evidence for this source |

## Lifecycle correction: simulation, not Draw

Drawing/rendering belongs to a future game platform and is not part of SimEngine's implementation.
IGameCycle/SimGame expose Initialize and Update. Draw, Drawn and the obsolete SimGame.RunFrame
Update/Draw wrapper were removed; no replacement drawing callback or compatibility overload exists.
The optional thin SimApp orchestration calls Game.Update, not a game drawing phase.

Initialize publishes settled initial data. Update runs the existing clock/kernel/entity path, then
publishes immutable simulation facts before Updated. Diagnostic RunTicks delegates to that same Update
path. A partial, paused, ended or faulted update still reports its actual engine state without inventing
completed work or extra ticks. Reentry guards prohibit nested updates/batches from Update observers.

GameSnapshot replaces the old rendering snapshot, but it does **not** introduce a second entity model:
its records reuse PlayerView.EntityView through the existing ViewOf projection. Previous position is
part of movement facts; platform rendering/interpolation is not implemented. Metadata and collections
are immutable, and value records are detached. Retained snapshots survive arbitrary later updates.
Tests check the absence of the old game APIs and preserve physical arrival/state/save continuation.

## Binding and restore ownership

- Fixed the external fixture's parser reference to the existing Json.Parse; registered all seven new
  binding suites (**94 checks**) instead of skipping the project or running stale binaries.
- Existing component declarations capture operation/signal versions, controllability and typed metric
  bindings once. Copied lists retain canonical identity order and cannot be mutated through casts.
- Authored and generated providers resolve the same active codec/schema owner. Missing references,
  malformed kinds/units/versions and runtime/definition disagreement fail before initialization.
- Controls cannot name a different capability. Generic commands/views enforce captured bounds/units;
  provider facts cannot silently override that declaration.
- Foreign-session, removed and live-but-unbound sources never invoke signal callbacks. Typed
  unavailable remains explicit for absent/unbound sources. Company views check privacy before reads.
- FinishComposition freezes/validates. On load, content and the existing World.Sync codec/capability/
  schedule manifest validate before Initialize or payload reads. Valid loads initialize once, read each
  owner's sole Sync once and call OnLoaded once. No duplicate preflight serializer was introduced.
- Faulted partial ticks cannot be saved as resumable healthy state. Hash diagnostics remain available.

## Snapshot, collection and private-information proofs

Four projection suites add **78 checks**. Visible entity counts, private recipe queries, layer IDs,
map versions and chunk deltas use existing visibility/ownership rules. Private writes neither advance
another company's cursor nor reveal locations or sequence gaps. Seat snapshots are scoped even when
no entity fog provider is installed; authorized observers retain their full facts.

TileMap owns derived public/owner-local invalidation caches. They are not a second gameplay state store,
not saved/hashed, and are rebuilt/invalidate readable chunks after restore. Public fills now invalidate
their affected chunks. Layer values continue through the existing layer Sync only.

Protected collections include module identities, entity/item tags, generated caller arrays, component
slots, entity/map iteration, navigation-neighbor order and public snapshot lists. Catalogue tags are
detached copies. Broader authored scenario/recipe/work arrays still need in-place freezing; this report
does not claim an exhaustive completed immutable-content migration or allocation budget.

## Executable-source identity

The source identity uses all **111 authored src/tests files** returned by `rg --files src tests`, excluding
bin/obj, sorted by path. Each line is `slash-normalized-relative-path|SHA256`, joined with LF, UTF-8,
then SHA-256 hashed. It is not the historical 108-file planning inventory.

- Source identity: `D032917524B3CEE1375DAFD611E878386A16769F01E3DCC35273F22560280993`
- Runtime DLL: `src/Beep.SimEngine/bin/Debug/netstandard2.1/Beep.SimEngine.dll`
- Runtime DLL SHA-256: `983890053C7A640EB832CFA59A7F1130CAFCBF2FBD2A226785652DC9AFA01A74`

Remaining work is owned by the single MASTER_TRACKER: shared authored-content freeze, protocol/options
validation, P3 general time/space, later gameplay stages, oil consumer migration and final cross-game/
replay/robustness gates. No later-stage completion is inferred from this focused current gate.
