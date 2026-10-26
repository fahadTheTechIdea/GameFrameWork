# P6-I5 composed external mod and original A/B/C gate — focused evidence

Date2026-10-15. Engine2.605.0/save159/replay1 unchanged. Four current exact cases
237/0 cover original P6.acceptance.008: nonspatial, spatial and external-mod proofs
pass without oil/internal dependencies. Only that original checkbox is closed.
No full/profile/family regression, whole-plan completion or global safety claim.

## One mod, original contracts and owners

ColonyExtensionProofModule is compiled in the separate non-friend ExtensionProof
assembly. It declares existing independent metadata implementations and explicit
dependencies on construction, inventory signals, incident response, tile navigation
and independent tile space. It adds no execution system, state codec, scheduler,
resource balance, copied parser or fallback implementation.

New colony-extension-master.json explicitly lists16 modules, original independent
world/startup/navigation/work/construction files and one new colony-extension.json.
Both new files are embedded. Mod JSON authors actual worker/endpoint/project/site,
two-stage build, typed signals, three-stage incident response and selected startup.
Existing30-second clock resolution,12-steel source,8-material build requirement,
resource/unit/capacity/handling and state declarations are reused or referenced;
new mod clock identity also explicitly authors30 seconds. No budget or Tycoon.

| Addition | Existing contract/declaration implementation | Sole execution owner and live proof | Durability witness |
|---|---|---|---|
| Worker | IndependentWorkerDef / shared WorkerDeclaration | Native Worker, WorkBoard, motion; same actor hauls and earns fabricator work | Partial loading, build and repair saved twins |
| Activity/project/site | IndependentActivityContentDef, IndependentConstructionDef, IndependentConstructionSiteDef | Original graph, ConstructionService/Constructible/Site; physical8-material project completes | Pending and productive project checkpoints; canonical replay |
| Inlet/outlet endpoint | SingleResourcePortDef / IInventoryDefinition | Original SingleResourcePort stock/book and resource transactions; actually receives8, exports8 and later receives2 repair stock | Actual stock/receipts and restored typed signals |
| Automation signal | IndependentInventorySignalDef / shared InventorySignalDeclaration | Stateless native InventorySignal; numeric stock and threshold readiness follow that exact inventory | Manifest binding, restored signal readings and hashes; no second signal state |
| Incident response | IndependentIncidentWorkflowDef, IndependentIncidentCompletionDef, independent activity graph | Native Incident/ResponseWork and original graph/material owners; detect, repair, restart over time | Blocked and partially earned episode saves, final save/replay |

The mod deliberately reuses existing Worker/InventorySignal/Site/Incident runtimes
and declaration classes. The requirement is adding a content extension through
contracts without scheduler edits, not fabricating five new implementations of
already-owned behavior. SingleResourcePort is the existing independent runtime
inventory implementation, not a wrapper over a second stock store.

## Engine source unchanged

Before and after this slice, sorted repository-relative paths and SHA256 values
for all441 `rg --files src/Beep.SimEngine -g '*.cs'` results were joined with LF,
UTF-8 encoded, then SHA256 hashed. Both readings:

`aaa21d3297f20d16a0386876eeaf76df498490b18bddf8517018c7ba5bb0ad82`

Count441 and hash match exactly. This proves current engine C# sources were not
edited during this mod implementation. It is not an audit of every ignored file,
binary, external library or all historical repository changes.

## End-to-end mod behavior

- Six malformed variants refuse before a game is returned: missing worker role,
  inventory resource, signal inventory binding, invalid project site, activity cycle
  and missing response. Omitted required inventory-signals module refuses during
  original module dependency validation. No special mod admission bypass.
- Original depot12 steel is the only source. Player protects4 and physically hauls8
  into the new independently implemented endpoint using the new worker. Partial
  load/actual receipts save; received stock8 and typed readiness true are observed.
  Numeric signal is checked against actual endpoint inventory on every observed update.
- Player creates the independent project. Protected depot4 cannot supply it, so
  actual endpoint outlet exports8 to the site; the same worker then earns timed
  fabricator work. Independent metadata drives the original working entity state.
  Partial build saves; native completion creates one facility, retires site and
  leaves total4. No teleport, duplicate worker or second material owner.
- Hold worker, reject an unsupported incident kind with Codes.InvalidArgument, then
  raise the authored episode through the ordinary command. Timed detection reaches
  WorkResourceCodes.Stock at repair, with zero work and protected total4. Endpoint
  projects stopped state; blocked episode saves and sole-clock pause freezes it.
- Release reserve and worker normally. Native automatic supply physically delivers
  two spares to the new inlet; same worker earns positive repair time. Partial
  response saves, restart finishes and independent completion metadata delegates to
  original ResponseWork/Incident. Episode1 has ResolvedTick > RaisedTick.
- Final total2 equals12 minus8 build minus2 repair. Signal remains bound to real
  consumed endpoint stock0. Retained signal8/opening worker snapshots stay detached.
  Foreign signal/lifecycle facts are withheld. Final save/load and canonical replay
  hashes match; a separate untouched session is unchanged.

Every observed update verifies physical tile nonoverlap and total stock12/4/2 only.
Driver code uses public commands, snapshots, views, clock and save/replay APIs;
no mutable Game.World, private reflection, stock/state writer or game-rule helper.

## All build and runtime attempts

Build command:

`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`

1. Failed4.93s: new negative JSON fixture attempted assignment to read-only array
   indexer. Replaced the authored roles array using existing JsonValue.Set.
2. Clean5.06s, exit0 before first runtime.
3. New Game C literal50/2,1702ms; PID30912 exit1/1842ms. Dependency omission correctly
   throws InvalidOperationException, not the test's expected ContentException.
   The blocked response correctly reports work.resource.stock, not the old generic
   material code; stage wait reached its360-update bound at update468 with total4,
   endpoint0 and active incident. Positive worker/project/port/signal flow had passed.
4. Repair compile failed3.88s from a misspelled new test constant name. Corrected
   to the inspected original WorkResourceCodes.Stock. No runtime after failed compile.
5. Clean4.79s, exit0, zero warnings/errors before final runtimes.
6. Final Game C76/0,1355ms; PID5908 exit0/1492ms.
7. Compiled external boundary10/0,110ms; PID30992 exit0/245ms. ExtensionProof6011
   method bodies/30156 member references and QuickGame.Module5/15; zero violations.
8. Current Game A70/0,542ms; PID29960 exit0/679ms.
9. Current Game B81/0,5718ms; PID28928 exit0/5856ms.
10. Post-run core source count/hash exactly matched the recorded pre-run reading.

All four current finals237/0, no timeout/abort/truncation. No executable edits after
Game C final. Exact cases use the built test DLL, `--bounded-exact`,60000ms timeout
and5000ms termination timeout; Game C output limit5000 chars, others3000.

- A: `Nonspatial process game: authored research, paused plans, operating control and objective replay`
- B: `Independent spatial colony: scarce stock priorities, loaded route detour and alternative-role construction`
- C: `External colony mod: worker project bidirectional endpoint signal and incident response share native execution`
- Boundary: `Framework independence: compiled external modules use public engine contracts only`

This is four selected requirement-specific literals, not a full/profile/family run.
The failed first C attempt and both failed builds are retained above.

## Original requirement closure and limits

The unchanged original sentence is now checked in P6_VERIFICATION_DESIGN.md:
“Nonspatial, spatial and external-mod proofs pass without oil/internal dependencies.”
Its preceding A/B/C specification is covered by these composed scenarios and the
separate-assembly compiled access/reference audit. A's progression/policy/isolation,
B's physical priorities/cancellation/detour/alternative role, and C's all-five
additions now have current runtime evidence rather than disconnected historical tests.
Boundary audit is not a security sandbox or arbitrary third-party mod certification.

No other original checkbox changes. Bookkeeping is one step from the last consolidated
historical221/195checked/26open to221/196recorded checked/25tracked open; this is
not a fresh audit proving all196 current. Full framework/Oil profiles, broad save/
replay/lockstep/isolation, ownership/thin clients and remaining P1–P12 requirements
still need their original evidence. Full goal remains active and unachieved.

No arbitrary new worker/runtime/provider fault matrix, deleted endpoint/recovery,
all-optional-module live interleaving, environmental science, rivals or full
industrial objective-chain acceptance is claimed. No Oil/Tycoon change, platform/
Draw/adapter, host/demo gameplay, legacy shim, staging, commit or push.
