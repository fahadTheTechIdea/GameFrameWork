# P5-CF1 — Active client fact provenance verification

## Result and original requirement

Original **P5.acceptance.006** is verified on unchanged2.628.0/save159/replay1:
“All operation/placement/work/route facts used by clients originate in engine views/previews.”
Only that original checkbox closes. The original221 requirements now have208
recorded checked/13 open. Historical checked gates were not globally rerun;
the overall goal remains open.

No engine, Oil, business, test or client source changed. This is current evidence
for functionality already implemented, not a new gameplay feature or copied checker.

## Complete current client inventory

All five current bodies were read, with bin/obj excluded from the inventory:

| Source | Lines | Responsibility |
|---|---:|---|
| OilCo.Headless/CommandShell.cs |236 | Input parsing, view queries, previews, ordinary commands, opaque engine save I/O |
| OilCo.Headless/TextClient.cs |96 | Scoped reports/notifications/state formatting and console-key mapping |
| OilCo.Headless/FieldPlayer.cs |48 | Engine-suggested founding followed by an ordinary engine-autopilot handoff |
| OilCo.LiveDemo/Program.cs |47 | CLI options and existing SimApp lifecycle/input/presentation composition |
| OilCo.Harness/Program.cs |86 | CLI play/demo entry points, engine coverage/state reporting |

All three client project files were read: Headless references OilCo.Game;
LiveDemo/Harness reference Headless. There are no additional active client C# files
in these projects. The complete source read is the provenance proof; the compiled
namespace test alone is not a semantic/dataflow analysis.

## Every consumed fact and its authority

| Client use | Original engine producer / actual delegation |
|---|---|
| Founding availability/coordinate | PlanningView.SuggestFoundingSites invokes ordinary World.Check on the real FoundCompanyCommand; no client ring search |
| Suggested building coordinate, footprint conflicts, known-field preference | PlanningView.SuggestSites uses IConstructionProjects.CheckPlacement and engine visibility/geology; client chooses preference/radius only |
| Roads/pipes/power-line path | InfrastructureView.SuggestLinePath calls the selected ITileLineRouting.PlanPath through the existing observed route reader |
| Final command legality/refusal | PlayerSeat/PlayerDecisionContext.Preview and original inbox CommandResult; a suggestion never bypasses execution admission |
| Stock, route, survey, order, production and research input | Text converted to original typed commands/settings; no local stock movement, work grants, cost, route solver or completion inference |
| Active work/requirements/progress/blocker | WorkView.CopyOrders reads sole WorkBoard orders/jobs, original graph durations and sole game clock; clients only format returned fields |
| Entity position/state and inspect facets | Scoped PlayerView.CopyEntities/TryGetEntity/ViewOf and observed IInspectable dispatch; state comes from engine EntityView, not fuel/work guesses |
| Company/HQ/extraction/sales/finance/outcome | CompanyView.Describe uses original Companies, Campaign and optional IBusinessAccounts/IBusinessSales contracts; clients never maintain accounts or infer winning |
| Objective progress/met/availability/deadline | CompanyView.CopyObjectives reads original Campaign/objective facts; clients format supplied values, not recompute objectives |
| Technology state/availability/cost | ResearchView.CopyTechs uses ITechnologyKnowledge, Research.CheckStart and optional IBusinessContentCharges; no client unlock/economic rules |
| Field knowledge | ExplorationView.CopyFields returns scoped engine-authored facts; no surveys are made prerequisites by the client |
| Scoreboard/entities visible to player | CompanyView.CopyScoreboard and seat snapshot, rather than live engine objects; spectator explicitly uses Observer |
| Time/calendar/pause/speed | SimGame.SimulationTime/Clock/Date and existing ClockControls; run/day converts an explicit input duration through ExactTicks and calls RunTicks |
| News, coverage and terminal result | Engine notifications/CommandResult/CoverageTracker/GameState/Outcome; host chooses presentation/exit status only |

The retired GeneratorRunning/fuel-stock inference does not exist in the complete
current FieldPlayer. Its only persistent local state is input-answer/handoff/logging
state. No expansion/research/repair/production cadence is implemented there.

Client numerical operations are parsing, display rounding, requested time chunking,
query radius/preference and presentation cadence. None determines authoritative
work, resources, path legality, operation status, deadlines or completion. Saving
Game.Save bytes is opaque I/O, not a second client state codec. No Draw/platform
implementation was added; the existing console presenter merely formats data.

## Existing executable checks, not duplicate tests

The original OilCo.Tests/PurityTests remains the sole compiled client-surface check.
It inspects the real Headless/LiveDemo/Harness binaries, asserts the exact public
input/fact exceptions, rejects mutable World/Entity/WorkBoard/TileMap/EntityDef
references and rejects client wall clocks. All38 assertions pass; no exceptions
or expected outcomes were changed.

The existing public thin-client case checks help/status/fields/list/orders/techs/
scenarios query purity, explicit disabled money, unsurveyed-drilling messaging,
malformed input, real founding suggestion/preview/command, shell versus typed engine
hour hash identity, inspection, saved continuation, actual named production command
parity and missing-capability refusal, presenter purity and ordinary founding-to-engine
autopilot handoff. All28 assertions pass on the existing24x24 proof configuration.
That is an authored test world, not an engine size limit or full canonical scenario.

## All current commands and outcomes

```text
dotnet build OilCo.Tests/OilCo.Tests.csproj -c Release --no-restore -v minimal
dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v minimal
```

Both clean:1.89s and1.15s, exit0, zero warnings/errors. OilCo.Tests' normal project
dependencies compile all three client artifacts against current framework/Oil;
building that target is not running its whole test runner.

Each runtime command selected exactly one literal with `--bounded-exact`,
timeout60000ms, termination-timeout5000ms, output4500 characters:

| Exact case | Result | Suite/process ms | Terminal evidence |
|---|---:|---:|---|
| Clients: only the public surface is used |38/0 |10/106 | PID34492, Exited0 |
| Oil thin clients: native shell input view purity saved state and engine automation handoff |28/0 |14903/15007 | PID33804, Exited0; exec session2275 polled to terminal |

**66 passed / 0 failed**, no aborted suite, truncation or observer error. No failed
build/runtime attempt, duplicate process restart, full/family/profile run, interactive
console or demo/campaign horizon. No live handle remains.

## Scoped identities

Eight active input files (five .cs plus three .csproj), sorted absolute forward-slash
path + uppercase file SHA256 joined with LF, outer UTF8 SHA256:
`9c4e8009dcfb3dac709c66b1ac127a4289281ef8427fc21e5cfabebfac9f73c6`.

| Current Release artifact | SHA256 |
|---|---|
| OilCo.Tests.dll | AC0C3BA090E42F1AC6263828B182B67A43FF2EFED914D36343A28113D2FB30FA |
| OilCo.PublicProof.dll | 09737A3E4F5756FA3B05F6EFFE36B4099A99EFBA8321EB3249A02BAEFE8891C9 |
| OilCo.Headless.dll | 1CBE511AF82F6D47354DE40954CC14268C4ABE45D911CC2D1E609CA96E308CF5 |
| OilCo.LiveDemo.dll | C85216EEBAF3EC080AB7128120632DDE44F9DA52FB934B1D73B0977FB3BDAD55 |
| OilCo.Harness.dll | 9908C4C2123CF3AC84696F08268CECF60E4DEDF214F2CF1AD88751D4E2C56599 |

## Limits and next work

This closes provenance of **all facts consumed by the current active clients**;
it does not prove every possible CLI branch, device, route, hazard, custom provider,
future client, multiplayer transport or full-game correctness. World lifecycle,
physical rules, immutable publication/provider purity and private knowledge retain
their own engine gates. P5 timed scenarios, rival parity, saves/lockstep and authorized
finance scope remain open; P6 profiles/ownership agreement and remaining P7/P8/P10/P11
requirements remain open. No source outside framework documentation was modified,
no parallel verifier/gameplay owner was introduced, and nothing was staged/committed/pushed.
