# P5-A34 thin-client native boundary verification

Core2.550.0/save156/replay1 unchanged. No engine/Oil gameplay, Tycoon rule or clock source changed;
client sources and focused proof were migrated. FieldPlayer's old gameplay sequence is removed.

## Builds and exact runtime

Oil consumer SimEngine directory:

```powershell
dotnet build OilCo.Headless/OilCo.Headless.csproj -c Release --no-restore
dotnet build OilCo.LiveDemo/OilCo.LiveDemo.csproj -c Release --no-restore
dotnet build OilCo.Harness/OilCo.Harness.csproj -c Release --no-restore
dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Oil thin clients: native shell input view purity saved state and engine automation handoff" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

Initial Headless build2.45s failed5 old API errors (ItemId and retired controller callbacks/Decide).
After migration, Headless1.95s failed6 remaining scenario/path-enum/objective-name errors. Corrected
against actual native contracts, not new aliases. Headless clean1.94s,LiveDemo clean1.95s,Harness
clean2.25s. All clean builds0 warnings/errors; full solution/OilCo.Tests not built or run.
Proof clean2.95s; intermediate FIRST26/0 in11092ms(PID28988,observer11246). Added explicit disabled-
finance status and optional-survey presentation assertions/corrections; proof clean1.78s.
FINAL28/0 in10830ms(PID11028,observer10964),Exited0,no output truncation/abort/timeout.
No executable edits after FINAL. Final proof rebuild includes Headless; demo/harness target builds
precede only compatible presentation-text refinements, not new public signatures. Not rerun afterward.

## Actual scope

One24x24 authored sandbox factory; real engine modules and public player surfaces. Shell query output
leaves hash unchanged. Resource-only finance prints not enabled; no seismic prerequisite is implied.
Invalid production/controller text refuses. Shell-selected founding plus one engine hour produces
exactly the same hash as a twin receiving ordinary typed founding and the same engine duration.
Inspection/orders/technology views are pure. Engine save roundtrip matches. Named production through
the shell and a saved typed-command twin agree after Update; wrong capability refuses without mutation.
TextPresenter status reads leave hash unchanged and show native scope/disabled finance.

Separate actual session attaches the thin FieldPlayer. It requests only ordinary founding and engine
autopilot. Recorded accepted IDs are oil.found-company/core.set-controller, not invented results;
company is founded and actual Controller is Autopilot within the bounded ten-frame handoff.

## Source boundary and remaining work

Source search in Headless/LiveDemo/Harness finds no mutable World/TestKit/resource/progress writes,
GameTime constants, ItemId, retired production commands, thinking cadence, Showcase or cash thresholds.
Core SimApp, engine gameplay/AI and clock remain existing owners. This search plus focused proof does
not establish all CLI variants, full demo/horizon/coverage, arbitrary controllers/private callbacks,
device console interaction or broad replay/lockstep/session isolation. Original full gate scope stays.

Initial combined inspections were truncated; actual changed files/contracts were inspected in smaller
sections before edits. Several guessed/wrong-worktree paths produced diagnostics; no execution result
was inferred from them. The client gameplay sequence is removed in place, not archived as compatibility.
No full/family runtime, platform/Draw/adapter work or staging/commit/push. Original221/194checked/27open.

## Metadata

2132 source anchors,13 quickstart anchors,1244 selected executable inputs: zero final hash drift.
8706 local links: zero broken;18 master rows valid; new proof boundary search empty.
The proof boundary permits the existing public RunTicks time request (used for typed/shell parity);
it still excludes mutable World/TestKit/resource/progress writers. Separate three-client source
search also excludes retired APIs, host cadence/Showcase and cash-driven gameplay thresholds.
All221 original criterion texts/checkboxes/line anchors match;194 checked/27open,additional298.
Framework diff check exit0 with34 existing diagnostic/warning lines;3201 dirty paths,zero staged.
Oil client/proof diff check exit0,zero staged. Original clock/protected Tycoon hashes unchanged.
No executable edits after FINAL28/0. Full goal/plan, full CLI/demo and broader acceptance remain OPEN.
