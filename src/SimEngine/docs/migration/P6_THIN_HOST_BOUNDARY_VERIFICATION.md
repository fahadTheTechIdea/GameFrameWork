# P6-TC1 active thin console client boundary — verification

## Outcome and original requirement

**Original P6.acceptance.011 is now verified:** no domain formulas, authoritative state writers or gameplay completion rules remain in the active clients. All five current Oil console-source bodies (513 lines), their project references and the quickstart host were inspected. Current target builds pass; the exact thin-client parity literal passes **28 assertions, zero failures**, and quickstart smoke passes.

This closes the source/engine-boundary requirement, not every CLI branch, interactive device, campaign/demo horizon or profile. Original P6.010 current ownership/assertion reconciliation remains open. One original checkbox changes221/203recorded checked/18open to221/204recorded checked/17open, not a global revalidation.

Engine unchanged **2.623.0 / save159 / replay1**. No client/Oil/framework C# or content source was edited. Build artifacts were produced only for verification. No full/profile/family or full demo execution, staging, commit or push.

## Exhaustive active source inventory

| Active source | Lines | Responsibility confirmed by complete source read |
|---|---:|---|
| OilCo.Headless/CommandShell.cs |236 | Parses player input into original commands; uses engine planning/path suggestions/previews; reports actual answers/views; serializes only opaque engine save bytes |
| OilCo.Headless/TextClient.cs |96 | Formats scoped views/calendar/notifications and polls mapped console keys; display cadence only, no simulation/current-time ownership |
| OilCo.Headless/FieldPlayer.cs |48 | Requests ordinary engine-suggested founding then selects original engine autopilot; only input/answer-display handshake state |
| OilCo.LiveDemo/Program.cs |47 | Parses scenario/seed, loads authored content and composes original SimApp/input/presenter/ClockControls/PlayScene |
| OilCo.Harness/Program.cs |86 | Console I/O and scenario/seed selection; demo requests speed input and reports engine-owned coverage/outcome, not its own win/completion rule |
| help/quickstart/Host/Program.cs |complete body | Composes original content/module/factory, initializes/updates, reads seat snapshot and checks engine save/hash |

All active .cs files in those three Oil client projects were enumerated with bin/obj excluded. Their project files were inspected: LiveDemo/Harness depend on Headless; Headless depends on the domain engine; quickstart Host depends on its separate engine module, which references the framework. No mutable World, reflection/native binding, private resource mutation, cash charging or progress/completion writer was found in the complete client bodies. A supplemental targeted source search for those mutating APIs had no hits; the search alone is not the proof.

## Gameplay decisions remain engine-owned

CommandShell delegates founding/site/path selection to PlanningView/InfrastructureView and submits the resulting ordinary intent. Optional field preference, query radius, command arguments and scenario/content IDs are input choices, not placement/cost/path algorithms. Preview and actual execution remain engine responsibilities.

The shell converts an explicit requested duration through ISimulationTime.ExactTicks and batches Game.RunTicks with integer-sized calls. It does not advance another clock, calculate work rates, grant progress or decide outcomes. File.WriteAllBytes stores Game.Save output; there is no client state codec.

FieldPlayer contains no expansion/repair/production/research cadence or fallback. Its founded/outcome/answer checks drive only the two ordinary input requests and display state; actual autopilot is the existing engine controller. The presenter formats engine totals/booleans and optional unavailable finance, never derives objectives or balances. Read-only positional formatting is not routing/placement logic.

Harness and LiveDemo call the framework's existing lifecycle. Coverage reports observe engine results, and terminal exit status reports engine GameState/Outcome. Presentation's hourly text interval is independent of simulation rules. No Draw/platform implementation was added.

## Current runtime proof

The existing public proof composes actual engine modules/content with a declared24x24 test scenario (test size, not engine limit). It checks:

- Console help/status/fields/list/orders/techs/scenarios are pure.
- Disabled accounts print unavailable rather than a fabricated monetary default, and surveys are not imposed as drilling prerequisites.
- Malformed input refuses.
- Shell founding plus an explicit engine hour yields exactly the same hash as typed founding and engine-time advancement.
- Inspection and founded status remain pure; shell-driven state saves/loads identically.
- Named production command plus Update matches its typed saved twin; nonexistent capability refuses without mutation.
- Presenter reads preserve hash.
- FieldPlayer requests only real founding/controller commands, then original engine autopilot takes over within the bounded handoff.

Quickstart initializes and runs three ordinary Updates, confirms the settled snapshot epoch, restores the engine save and verifies identical hash. It reports `tick=3; entities=1; hash=319398F538F63CE7`.

## Every current build and execution

| Target | Result | Evidence |
|---|---|---|
| OilCo.PublicProof Release --no-restore (includes Game/Headless/current framework) |clean,0 warnings/errors |4.89s build, exit0 |
| OilCo.LiveDemo Release --no-restore |clean,0 warnings/errors |1.87s build, exit0 |
| OilCo.Harness Release --no-restore |clean,0 warnings/errors |2.00s build, exit0 |
| QuickGame.Host Release --no-restore |clean,0 warnings/errors |1.87s build, exit0 |
| Existing exact Oil thin-client literal |28/0 | child5828 exit0;15335ms suite/15456ms process; session79045 polled to terminal |
| Quickstart host smoke |exit0 |0.454s tool wall, three updates/hash above |

The public proof uses literal `--bounded-exact "Oil thin clients: native shell input view purity saved state and engine automation handoff" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 4500`. Quickstart runs its bounded three-update Main, not a campaign. Those two independent processes ran in parallel. No failed/aborted suite, truncated output, restarted process or live handle remains. Other Oil.PublicProof cases compiled but were not executed. OilCo.Tests/full solution/profile was not run.

The10 active host/project inputs (five Oil .cs, three Oil .csproj and quickstart Host .cs/.csproj) have combined sorted path/content SHA256 `d6d7b1ab9edfb5eaa3135f7f0ccca0180cd96d00205b15ff8195d27347ecbcb3`. Oil paths are absolute current workspace paths and quickstart paths are framework-relative, as used by the recorded read-only inventory command. This is scoped host evidence, not a new ownership manifest or gameplay input.

## Limits and next work

No claim is made that every shell command/device/error path, full scenario/horizon, multiplayer transport or all future clients are verified. The requirement closed here is the actual active source boundary, supported by current compilation/parity—not full Oil acceptance. Existing assertion ownership still needs explicit current path/semantic reconciliation; P6.010 and P6.007 stay open.

No source outside the framework was changed, no domain/Tycoon rule was migrated, and no host algorithm was added. Lifecycle guidance organized this stage and focused evidence; the user-focused-only/no-publication constraints remain in force.
