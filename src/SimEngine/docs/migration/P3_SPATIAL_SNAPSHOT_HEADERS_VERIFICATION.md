# P3-SH1 actual spatial snapshot header verification

## Engine change

Core2.484.0/save154/replay1. SpatialSnapshot and SpatialWorldSnapshot constructors reject absent/blank space identity; world snapshots reject negative viewer revisions. PlayerView.ViewOf validates non-null selected presence capture, actual presence.Space and returned snapshot.Space against the selected world.Space.Id inside the existing observed provider/target/space boundary. GameSnapshot.Capture similarly validates selected world snapshot identity/non-null/revision inside the existing space-provider observation.

Explicit null remains correct for a genuinely nonspatial session/entity. Viewer-local world revision is not compared to global ObservationVersion, preserving independent private invalidation semantics. Existing spatial providers, entity state, publication stream, first-fault latch and retained snapshot lifecycle stay authoritative; no duplicate snapshot, ledger, clock or saved header state.

Existing independent point definitions now accept optional negative-only capture metadata. World probes trigger after normal access closure; entity probes after real movement reaches an explicitly authored position. Existing point capture/runtime/codec remains the owner. The malformed negative revision snapshot is solely a declared invalid-header probe, not another spatial model.

## Exact build/runtime

One clean solution compile: dotnet build Beep.SimEngine.slnx --no-restore,10.93s,0warnings/errors. No repair compile.

ONE NEW exact FIRST/FINAL286/0/0 in740ms; PID22688 Exited0, process917ms. Zero aborted suites, stream truncation or observer error. Clean compile precedes runtime; no executable edit after FINAL. No old/family/full runtime.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spatial snapshot headers: actual space identity and retained private publications reject malformed captures" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
```

Outer60000ms/termination5000ms/output24000chars. Movement/probe stages bounded1000Updates with actual clock or phase diagnostics.

## Public-player proof

Two point minute/half physical inspection workflows use the actual point master/JSON, ordinary PostOrderCommand, immutable snapshots/views, Game.Update, saves/replay and sole clock. Actual movement publishes the correct selected space; retained prior payload remains unchanged. World viewer revision0 remains legal without global-counter substitution. Owned/admin/other-viewer headers, saved twin continuation, terminal save/replay and untouched interleaved session agree.

Actual native restoration master publishes its selected haul.tiles map, and actual nonspatial process master correctly publishes absent spatial facts. No alternate mock game loop or host geometry.

Six world capture probes run after an ordinary access-control command: null, foreign identity, blank identity, negative world revision, original exception and persisted access mutation. Five entity capture probes run after actual physical movement reaches position2: null, foreign identity, blank identity, original exception and entity revision mutation. Each faults before malformed new facts are published.

Faulted publication uses each viewer's own last healthy world/entity payload/time and visible identities, with no invented Changed/Spawned/Removed deltas. Original callback error survives. Faulted session cannot advance clock, save/hash as healthy or read live entity projections; later calls preserve first fault, and a separate session remains untouched. No rollback is claimed for mutations.

The new driver uses only public seat inputs/views/snapshots/results, Update, saves/replay and reads. No World, TestKit, direct stock/job/progress/geometry writes. Deliberate negative callbacks live in engine-side independent point providers and are JSON-authored, not harness gameplay.

## Qualification

Original221/186checked/35open unchanged; additional211. No original checkbox/text/line change or gate closure. This validates returned base headers and observed faults/retention, not every arbitrary custom payload's deep immutability, every spatial query, visibility/privacy provider, protected cell/delta, raw CLR sandboxing, fuzz/concurrency/performance/stress or full P3/P11/P1-P12 regression. Work binding objects are already immutable and layer metadata already cloned/frozen; no unnecessary alternate snapshot path added. Earlier proofs compile only, not rerun.

Canonical SimEngine only; protected sole-clock/G19/Tycoon unchanged. No Oil, Draw/platform/adapters, staging, commit or push. Full goal AND plan remain incomplete.

## Metadata closure

Fresh read-only metadata closure:1718 source anchors,956 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2906 paths/1030 Markdown files/8672 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2894/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional211. Seven existing executable sources changed: six previously selected and existing GameSnapshot.cs newly included in selected freeze; one new executable driver/three new source files, four existing production sources changed/zero new. Existing view schemas unchanged; returned header validation adds no projected fields. Only the primary P3 row was updated; the distinct historical P3 row remains untouched. No runtime rerun for documentation validation.
