# P4-G63 — tile presence bindings verification

## Implemented ownership

Core2.423.0/save154/replay1. TilePresence now inherits Component and consumes its actual ITilePresenceDefinition; native TilePresenceDef is ordinary metadata registration/generated authoring, not required mod base. TilePresenceDeclaration is the single strict JSON reader, resolved binding and composition validator. Actual entity/component aliases, positive dimensions, authored tile-space references, mobile geometry and selected TileMap service identity are required. Generated original line-marker definitions use the same validated declaration with explicit arguments; no fallback geometry or duplicate reader.

TilePresence keeps original tile/interpolation/heading/placed fields, occupant coordination and codec1 exactly. Placement, mover, construction, rollout, recovery, sensor and port consumers already use ITilePresenceDefinition and keep their original owners. TileMapView.TryGetFootprint now requires a healthy session before early returns and stages actual metadata under the original EntityObservation/map guard, publishing only after success. No state, clock, reservation, material ledger, command, scheduler or compatibility copy.

The lifecycle skill guided staged existing-owner reuse. IndependentTilePresenceDef only authors metadata through the same declaration and creates the SAME native TilePresence with codec1. The new master-listed component registration is active in the original Colony scenario; all authored prototype presences are native or independently declared in the two positive variants. Drivers edit JSON intent, submit normal commands and observe public immutable facts, never runtime world/stock/progress. Negative callbacks are instrumentation only, bound to their actual session.

## Requirement-matched proof

ONE NEW exact case: `Tile presence bindings: independent JSON metadata drives original hauling rollout and construction`.

Both native/direct variants use original company resources50 steel and existing physical machine/dock/logistics/work/clock owners. Ordinary manufacturing commands do not instant-spawn; actual trucks travel and supply factory inputs, authored three-hour work consumes10 steel and rolls out exactly one physical movable output with1x1 native geometry. Pending/partial saves and twin hashes survive travel; sole-clock pause earns no movement or material changes. Public footprint previews expose exact3x3 depot and2x2 tower metadata without state changes.

Normal construction at12,16 creates an actual unfinished site, physically receives30 steel and executes the original two-hour preparation then three-hour erection. Completion creates a new target ID and retires the actual site, preserving exact2x2 placement/occupancy and retained detached site facts. Final actual inventory10 proves authored manufacturing/construction debits occurred once, with no resource grants/duplication. Completed save/load grants no opening resources again. Ordinary command stream replays, independent session hashes remain untouched and foreign-company production management refuses private WIP/queue state. Publicly visible buildings are intentionally not hidden by a test-only rule.

Twenty-four early metadata refusals: native/direct missing space/width/height/blocks/movable, nonpositive width/height, unknown fields, missing actual space and oversized mobile shape; four unused direct definitions override width/height/space/mobile-shape inconsistently. All refuse before Initialize. Three later actual footprint callbacks throw, mutate clock or mutate-then-throw; original first error retained, World fault latched, save refused, faulted missing footprint read refuses before empty/success prefix, independent session unchanged.

Additional scoped deliverable131. Original221 acceptance wording/checkboxes159 checked/62 open unchanged. Compound P2.003/P4.003/P3.004/P3.008, arbitrary provider/CLR purity, other geometry, recovery/incident/layer/private/scale/performance/lockstep/Oil/full regression and whole P1-P12 remain OPEN. Protected G19 scope pending. No Tycoon/Oil/platform/Draw/legacy compatibility changes, staging/commit/push.

## Actual compile and runtime evidence

1. Solution build exit1,14.21s,0 warnings/1 error CS1503: new driver used JsonValue.Of(0), whose overload accepts bool rather than int. Fixed authored numeric intent to JsonValue.Number("0"); no production repair.
2. Same-case targeted dependency build exit0,6.25s,0 warnings/0 errors.
3. FINAL targeted dependency build exit0,4.15s,0 warnings/0 errors, all five projects compiled.

FIRST runtime:26 passed/1 failed/1 aborted suite,418ms; owned PID27876 exit1, observed567ms, outcome Exited. Driver selected a prototype with Single while the original snapshot legitimately includes public buildings from both companies. Corrected ordinary command selection to owner0, and privacy assertion to actual private production management rather than pretending public buildings are hidden. No engine visibility rule weakened.

FINAL runtime:1400 passed/0 failed/0 aborted,9095ms; owned PID3724 exit0, observed9260ms, outcome Exited. Both attempts had untruncated stdout/stderr and no observer error. No executable edits after FINAL. The solution live handle48429 was observed terminal exit1, not restarted on observation timeout.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Tile presence bindings: independent JSON metadata drives original hauling rollout and construction' --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
```

Physical stages bounded768 Updates, outer60000ms, owned termination5000ms, output30000 chars. ONE NEW exact case only; no old/family/full/Oil/Tycoon/platform runtime suite. Focused proof is not full regression.

## Post-FINAL source and mechanical audit

Freeze 2026-10-10 04:07:01 UTC:604 selected executable inputs;7 changed/new versus the prior601-input checkpoint. Three newly selected paths include2 new proof/driver files and the existing TileMapView owner. No executable post-FINAL drift. Current1276 source anchors and13 quickstart anchors all match actual hashes. Refreshed204 original gate line anchors;0 original wording or checkbox changes. Original221/159checked/62open and131 additional scoped deliverables remain distinct, with no aggregate phase completion inferred.

Read-only audit indexed2541 paths,876 Markdown documents and7431 local links:0 broken links. All18 master rows have5 columns;0 malformed. git diff --check exit0 with34 existing line-ending warning lines. New driver has0 runtime world/stock/progress-write boundary matches. Actual git status --porcelain=v1 --untracked-files=all reports2531 dirty paths and0 staged; unrelated user changes preserved, no ownership inferred from counts. Sole GameClock.cs and the three protected Tycoon sources match prior SHA256 hashes. Full goal remains active/P1-P12 OPEN; no commit or push.
