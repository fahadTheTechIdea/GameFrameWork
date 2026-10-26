# P5-RA1 — Ordinary rival parity reconciliation

2026-10-16. Engine 2.639.0 / save 160 / replay 1, unchanged.
[Design](../../plans/enhancements/P5_RIVAL_PARITY_RECONCILIATION_DESIGN.md).

Original P5.acceptance.007 is closed: rivals and players have command/preview parity,
affordability and private knowledge enforcement. Three existing exact cases pass:
**3,671 assertions / 0 failures**, all children exited normally, no aborted case.
This is not a full campaign, profile, replay, performance or global regression result.
The original audit now has 221 requirements / 210 recorded checked / 11 open.

## Responsible owners and requirement evidence

The actual consumer root is
`C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine`.
The framework root is `C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine`.

Read the complete actual `OilCo.Game/Autopilot.cs`, including its declaration/profile
and runtime strategy, and the generic `Hosting/PlayerDecisionContext.cs` decision boundary.
Review the original `Standard/PlayerControllers.cs` coordinator/profile validation,
`Kernel/World.cs` check/submit/dispatch, `Kernel/CommandInbox.cs`, and
`Hosting/SimGame.cs` seat submission boundary. Review the three proof bodies, Oil
company/exploration/placement views, `Reservoirs.cs` company knowledge, native work
cost admission and the original optional account/budget owner.

| Original clause | Source and executable evidence |
|---|---|
| Command/preview parity | CompanyAutopilot.Order checks the actual decision budget, calls context.Preview, then context.Submit. PlayerDecisionContext binds the company identity and uses World.Check; the sole PlayerControllersSystem supplies that company's PlayerView and World.Submit. Human PlayerSeat.Preview uses the same World.Check; authoritative dispatch rechecks the same registered handler. The named-port case compares management route preview with actual seat command preview and receives ordinary accepted results, at most six per company. |
| Affordability | Recovery discovers actual existing work/stock and submits native construction/hauling. It has no stock/account setter or privileged recovery command. Paid recovery starts with 200000, spends the authored 150000 once, and consumes exactly 30 steel / 20 cement. A 149999 company persists at the cash blocker; a resource-only company with 29 steel persists at the material blocker. Neither receives money/materials or adopts an unfinished HQ. Saved waits and normal cancellation preserve actual unspent resources. |
| Private knowledge | Each strategy decision reads only its seat view; BestField reads that company's ExplorationView. CopyFields gates known/identified facts on its company knowledge; foreign well counts require actual visibility. Geology.Estimate reads the company's remembered observation, not hidden current reservoir quantity. The canonical earned-survey case finds own coverage, no rival coverage, disjoint field disclosure and NotOwned for the other company's account. The named-port case also clears foreign port output and preserves separate owned routes/cargo. |

External human inputs may pass through the transport/recording boundary in
SimGame.SubmitFor. Derived controller decisions are generated inside deterministic Update
and submitted by the coordinator to the original inbox. This is shared authoritative
gameplay admission, not a claim of byte-identical input transport or a newly proved replay
matrix. P5.acceptance.008 remains open; changing controller submission into external
network input would not be justified by this reconciliation.

The strategy's lists are refreshed observation buffers, not a second durable model.
Controller cadence, bounded replies and saved command correlations belong to the one
generic coordinator. Fleet counts inspect actual per-unit transformation entries; there
is no unsupported queue-count repair. Optional money absence is preserved, not replaced
with a fabricated balance. A pending unaffordable plan is permitted intent, not free
productive construction.

## Exact commands and observed results

Working directory: the actual consumer root above. Each invocation uses this existing
bounded runner, with one literal case name, a 180000 ms child deadline, a 5000 ms
termination deadline and 24000 characters of output per stream:

```powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Oil recovery constraints: optional authored fees scarce materials cancellation and saved waits" --timeout-ms 180000 --termination-timeout-ms 5000 --output-chars 24000
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Oil autopilot: named stock ports physical routes survey identity and private saved rivals" --timeout-ms 180000 --termination-timeout-ms 5000 --output-chars 24000
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Game: companies see only what they know" --timeout-ms 180000 --termination-timeout-ms 5000 --output-chars 24000
```

| Existing exact case | Passed / failed | Suite ms | Bounded process ms | Child PID / exec session |
|---|---:|---:|---:|---|
| Recovery constraints | 3188 / 0 | 22416 | 22537 | 35812 / 4148 |
| Named-port saved rivals | 479 / 0 | 10817 | 10922 | 27164 / 48898 |
| Earned survey privacy | 4 / 0 | 169212 | 169312 | 35524 / 91742 |

Each ran once for this stage. All terminal outputs observed: Exited, exit 0,
stdout-truncated=False, stderr-truncated=False. The privacy handle was resumed after
the intervening TileMap clarification, not restarted. Its final terminal output, rather
than elapsed observation time, establishes the result. No test failure, timeout, retry,
family filter, profile run or full regression occurred. A diagnostic stack invocation's
output was unavailable after context truncation; no call-stack or optimization claim
is derived from it.

The recovery fixture explicitly authors positive two-minute physical demolition/build
nodes and a small map; original fees, material bills, travel and handling remain real.
The port fixture is a 32x32 resource-only two-rival world: finite six-unit crude stocks,
aliased actual ports, native tanker/route/stock policies, 150 updates, hash comparison
at every update and partial reload. Cargo/delivery were observed by checkpoint 90.
The privacy case uses the original canonical 96x96 Frontier, seed 3, actual founding
and SurveyCommand, and the original bounded ten-day physical-work wait. It is not a
shrunken map, clock jump, injected knowledge or shortened survey. Its near-deadline
runtime is a remaining performance concern, not evidence of acceptable global budgets.

Assertion totals in repeated-update checks can vary with actual completion steps;
historical recovery totals are not substituted for this observed 3188 result.

## Correlated evidence, not rerun or added to the total

- [P5-HI1](P5_INTERRUPTED_HEADQUARTERS_RECOVERY_VERIFICATION.md): same-core actual
  loaded travel cancellation, saved physical return, autonomous second recovery,
  home repair, conserved resources and no replacement fleet grants; final 1925 / 0.
- [P5-FP1](P5_FINANCIAL_PRESERVATION_AUDIT_VERIFICATION.md): same-core original
  financial amounts, account/budget/purchase ownership, private saved spending and
  actual Oil research/sales; seven distinct exact finals 4449 / 0.

These support the existing responsible owners; neither is silently counted as a
current additional run or a proof of every game scenario.

## Current identity and structural checks

No executable, test, authored JSON, business-rule, platform, rendering or adapter source
changed in this stage. No compilation was necessary. Current path/content fingerprint
of all 444 core C# files remains
`0327e6ff4473656c75e2e8a889d804e5cc8d8029b027065aa772b5bb27b7973a`.

SHA256 of the actual Release artifacts:

- Framework DLL and actual consumer copy:
  `E811B410F353520502501CF34A6E848B47EB67F0B72DEA42E1E645D3ADA0A6D7`.
- Actual consumer OilCo.Game:
  `2E0C64518B20CA4CCB0AB1FC459BC52D6DE3BACC18742F97C58CD652049DFFA1`.
- Actual consumer OilCo.PublicProof:
  `1F3A40959828F01D9B677AA6173CF792B2AB9842B3218524AF5A56D23A89BB76`.

Current VerifyExecutionOwners and VerifyCapabilityConsumers both exit 0: 444 sources,
nine execution owners, eleven witnesses, 1528 typed query sites, empty owner errors,
forbidden declarations and native runtime query/cast violations. These are structural
checks, not substitutes for runtime behavior or proof that arbitrary mod code is pure.

## Closure boundary and remaining work

Only original P5.acceptance.007 changes from unchecked to checked; its requirement
wording and the original COMPLETION_AUDIT.json are not changed. All three clauses have
source-level owner coverage and mapped actual runtime behavior, with interrupted
recovery and financial migration evidence retained separately.

P5 timed-scenario/save/financial-migration-wide gates, full framework/Oil profile gate,
and broader P7/P8/P10/P11 matrices remain open. This is not proof that every rival profile
wins, every route is deadlock-free, every optional module is replay-safe, all unknown
activities are hidden under every visibility provider, or all callback implementations
are pure. No legacy/compatibility path, duplicate model, second clock or host gameplay
was introduced. The lifecycle skill required evidence/tracker reconciliation; the user's
focused-only rule continues to prohibit its generic full-regression suggestion.

No staging, commit or push before the whole goal and plan finish. The full goal remains
active. Next: map the existing timed-workflow cases against original P5.acceptance.005
and fill a demonstrated physical-work coverage gap without another execution owner.
