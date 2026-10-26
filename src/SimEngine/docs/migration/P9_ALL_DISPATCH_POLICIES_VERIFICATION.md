# P9-C14 all physical dispatch policy acceptance — 2026-10-10

## Outcome and sole responsibility

Original P9.acceptance.043 is verified with unchanged wording: all dispatch types respect applicable areas, reserves, maximum stock and permissions. Forty current physical variants cover ten category/entry-path classifications multiplied by native/direct ILogisticsPolicyDefinition and native/public IOutletPolicy implementations. No historical test driver is invoked.

All existing live authority is unchanged: WorkSystem.CheckAssignmentCore/WorkPolicyRules, company ready-worker reserve owner, LogisticsService.Preview/PortSpare/ExportPermission/ReceivingCeiling/BuildQueue/PreviewSource/PreviewDestination, original stock/cargo/receipts, incident response, motion, entity Update and sole GameClock. Source inspection confirms native candidate admission and shared haul acquisition use those same contract gates, with explicit ordinary/emergency/continuation semantics. No new production gameplay owner/API/state or engine repair. Core2.424.0/save154/replay1 unchanged.

## Current acceptance matrix

- Demand: original stock-policy replenishment. Player minimum/desired/maximum4 then8 starts the appropriate refill at the actual stock threshold; desired alone does not force refilling below an unchanged minimum.
- Route: original AddRouteCommand and stable route identity.
- Evacuation: explicit native finite public storage, actual original recovery kind.
- CargoReturn: original four-unit carrier inventory; protected two-unit reserve, real unassigned cargo and original return-kind claims, never a spawned replacement.
- Independent: registered non-friend IWorkerDispatchSource uses IHaulExecution; full authored64-bit identity retained.
- Manual: actual HaulResourcesCommand preview/Submit refusal parity under every blocked policy, then ordinary accepted physical hauls; no claimed success prefix on refusal.
- WorkSupply: authored native staffed activity requires eight actual inputs. Partial four-unit supply cannot complete or staff it; only the second delivery enables timed work and exactly eight-unit consumption.
- EmergencySupply: real incident/response demand retains emergency classification. Emergency-ineligible/disabled/wrong-role/outside-area workers cannot dispatch. Protected material20 remains unexportable despite emergency priority. Partial reserve16 permits only4; EmergencyOnly reserve20 release cannot bypass a full receiver or disabled source. Company floor1 remains configured while explicit emergency hauling and response execute.
- NativeWork: original staffed local eight-input activity waits under disabled/roles/area/ordinary inspect-worker-floor policies, then travels and consumes once over game time.
- EmergencyResponse: actual local-input response waits under disabled/roles/area/emergency-eligibility policies, then uses its declared ordinary-worker-floor override and physically consumes eight units.

All eight transfer categories test disabled workers, allowed roles, operating area, ready-worker eligibility/reserves, entire/partial material-or-cargo reserve, destination filters, receiving maximum and disabled export. Each executes two timed deliveries; stock starts with20 finite units per isolated scenario. Normal non-cargo transfer ends source12/destination8/cargo0. Cargo ends source16/destination4/cargo0. Supplied-work variants consume delivered8 once and end source12/destination0/cargo0.

Local work's stock maximum0, disabled outlet, destination filter and export reserve8 restrict new transfers; they do not forbid lawful local consumption of already-supplied8. No invented local-stock prohibition, business rule or instantaneous material/work fallback. Both local categories end protected remote source12/local0/cargo0 through real travel and staffed duration.

Every variant saves pending player input, partial actual assignments/loaded travel and terminal original state; clock pause advances nothing, Update twins remain hash-identical, replay verifies the actual command stream, foreign policy/dispatch observations/commands refuse without leakage, retained settings stay detached and interleaved sessions remain isolated.

## Authored content and thin driver

C13 pure authoring is extracted into one DispatchPolicyScenario, not copied. Existing master construction helper accepts explicit provider selection and work-document intent; new dispatch-policy-activities.json supplies four stable workflow definitions through the same master-listed merged work document. C13's old local scenario factory is removed. Native contracts/content and coordinator execute all rules; the new driver only submits player commands, advances Update and asserts immutable facts. No World/stock/job/progress mutations, default map/teleport, duplicate scheduler/ledger/reservation/clock or compatibility path.

Original automatic source selection retains its first infeasible source, which in this fixture is the empty startup sink; emergency queue summary is NoSupply with the original typed dispatch-unavailable assignment. Cargo return retains its first disallowed destination before the full permitted receiver. These actual aggregate diagnostics are asserted, not substituted with a later blocker. Physical waiting, actual receiving facts and capacity/policy release demonstrate enforcement independently of aggregate blocker wording. This does not claim all detailed diagnostics/provider-purity contracts are complete.

## Actual verification

Only ONE NEW exact case, no old/family/full, Oil, Tycoon or platform runtime:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Dispatch policy acceptance: every physical dispatch path shares area reserve capacity and permission authority' --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
```

Four compilation attempts retained:

1. Solution compile11.89s, exit1, zero warnings/four CS0029 driver Code arguments; live handle12395 observed terminal. Driver fixed to explicit original Code values.
2. Targeted five-project compile8.43s, exit0, zero warnings/errors.
3. Targeted same-case compile4.87s, exit0, zero warnings/errors.
4. FINAL targeted compile4.45s, exit0, zero warnings/errors.

Three same-case runtime attempts:

1. 746passed/1failed/1aborted in1254ms, owned PID34608 exited1/1423ms. Demand restart fixture incorrectly retained minimum1 while stock4: normal hysteresis correctly refused a new refill. Player command now explicitly changes minimum/desired/maximum together; no production behavior weakened.
2. 10704passed/12failed/0aborted in12735ms, owned PID38316 exited1/12883ms; live handle72867 observed terminal. All physical categories completed; emergency aggregate diagnostic expectations corrected to actual first-infeasible-source semantics.
3. FINAL10764passed/0failed/0aborted in13177ms, owned PID15920 exited0/13325ms; live handle56967 observed terminal.

All outputs untruncated, observer errors empty. Outer60000ms/owned termination5000ms/output30000chars; physical phases512Updates with public clock/job/blocker diagnostics. No executable edits after FINAL.

## Completion and publication limits

Only original .043 checkbox changes; all221 original requirement texts retained, now161checked/60unchecked. Additional scoped deliverable135. Neighboring endpoint/road/fuel/breakdown/blocked-output recovery .051 and all interrupted queue/load/delivery/recovery/paused-plan durability .052, broad provider/lockstep/stress/performance and full P1-P12 remain OPEN. Historical C12/G64/C13 runtimes were not rerun after fixture/helper migration. Protected catalog authority remains pending, no Tycoon/business/Oil/platform edit. Nothing staged, committed or pushed before full goal AND plan completion.

Lifecycle guidance influenced per-stage evidence, original-owner reuse and exact coverage; its broad regression suggestion does not override the user's focused-test boundary.

## Post-FINAL metadata/source checks

Executable inventory frozen at2026-10-10 05:13:57UTC:632 selected inputs. Eight executable files new/changed; five newly selected inputs comprise three new files and two unchanged dependencies. Post-FINAL executable drift0. Original gate lines204 refreshed, requirement text changes0; exactly one checkbox change, P9.acceptance.043.

Actual1300 whole source/document SHA256 anchors plus13 quickstart anchors: mismatches0. Actual2559 indexed paths,884 Markdown documents and7559 local links: broken links0. Master tracker18 five-column rows: malformed rows0. git diff --check exit0;34 existing line-ending warnings retained. New driver gameplay-boundary search0 matches.

Actual dirty inventory uses git status --porcelain=v1 --untracked-files=all:2549 paths, staged0. GameClock and all three protected G19 catalog files retain their preceding hashes. GoalComplete=false. No runtime suite invoked by these checks. Documentation/metadata refreshed after FINAL; no executable edit, staging, commit or push.
