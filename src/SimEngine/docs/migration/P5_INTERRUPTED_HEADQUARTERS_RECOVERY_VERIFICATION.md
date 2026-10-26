# P5-HI1 — Interrupted recovery with actual moving cargo

2026-10-16. Engine2.639.0/save160/replay1 unchanged.
[Stage](../../plans/enhancements/P5_INTERRUPTED_HEADQUARTERS_RECOVERY_DESIGN.md).
One new exact case: final **1,925 passed /0 failed**, no abort. No production repair needed.
Original221 requirements remain209 recorded checked/12 open; broad P5.007/.008/P7.017
and the full goal remain open.

## Existing coverage reviewed; no duplicate recovery implementation

Read the actual CompanyAutopilot recovery/order/home path and existing public headquarters,
constraint and named-port rival proofs, plus original construction cancellation, recovery,
resource guards and tile spatial-update behavior. The controller only submits normal
commands after the same public preview through PlayerDecisionContext; it has no World,
cash/stock setter or alternate construction path.

The existing [funded/poor/scarce recovery case](P5_HEADQUARTERS_CONSTRAINTS_VERIFICATION.md)
already covers150000 cost,149999 refusal,29/30 steel shortage, saved waits and cancellation
before goods are loaded. The existing [physical recovery case](P5_PHYSICAL_HEADQUARTERS_RECOVERY_VERIFICATION.md)
already covers real rebuilding/home selection. They were inspected, not duplicated or
rerun and not counted as current green results in this stage.

Extend HeadquartersRecoveryTests as a partial class with one new registered interruption
case. Reuse its Files/Step/Stock/Total/Diagnostic; its existing Submit helper now optionally
submits the same public command to a saved twin and compares native admission/results.
Old callers retain the same no-twin path. No new driver game logic, controller strategy,
hauling/recovery owner, mutable inventory, persistent company reference or game clock.

## Final authored scenario and behavior

Reuses the existing24x24 resource-only Sandbox fixture, seed17, one human, company_outpost
HQ, bulldozer/crane/two haulers and exactly30 structural steel/20 cement. The existing
positive two-minute demolition/build nodes, one-minute update quantum and hourly controller
cadence remain explicit fixture settings. Original material bills, handling/travel rates,
physical service/dock/packing rules and gameplay modules remain unchanged. There is no
optional Tycoon/account, resource grant or hidden geology dependency.

1. Human founding occurs once. Ordinary earned demolition removes the HQ and packs its real
   stock locally; the company remains founded with its original now-missing HQ reference.
2. Human selects a separated legal recovery position from the actual engine suggestion view,
   at least six tiles from the original HQ, and posts ordinary ConstructCommand. This first
   placement is human intent, not a claim of autonomous placement. The original controller
   then discovers that pending site and physically supplies it from actual reclaimed stock.
3. Wait for a loaded haul truck with actual current-interval spatial displacement, away from
   the original stock footprint. TilePresence.BeginTick resets PreviousPosition through the
   existing World update, so this proves movement rather than an old retained location delta.
   No position, route, speed or motion state is assigned by the driver.
4. Save during that actual loaded movement, take human control through SetControllerCommand,
   and cancel the site through the normal construction command while cargo remains loaded.
   Both original and restored sessions receive identical ordinary inputs.
5. Cancellation can temporarily retain the site under the real active-promises guard. The
   simulation continues: loaded cargo moves after cancellation, returns over game time and
   unloads; another save during loaded return continues identically. Only once claims permit
   does the canceled site retire. No guard is bypassed and cargo is not silently discarded.
6. Every observed interruption step conserves exactly30 steel/20 cement across actual owned
   inventories, including cargo. No live order remains for the canceled site; real recovery
   stock remains available and no unfinished HQ is selected.
7. Re-enable the same native controller. It autonomously places a second, different site,
   reclaims the finite returned goods, builds and selects its actual completed outpost, and
   repairs worker home intent through ordinary commands. Original/twin full hashes agree
   throughout. Materials stay30/20 until one whole productive consumption, then both reach0.
8. Four vehicles remain four. All observed command replies are accepted; exactly one founding
   and two construct commands occur, one per attempt. A separate idle session is unchanged.
   Completed state saves/loads identically. Diagnostic final HQ is11, with home repair at
   game minute503; no re-founding or replacement starting kit is involved.

The original Work/Logistics/ResourceRecovery/Construction/Company/controller owners already
handle this interruption correctly. Hosts/demos/headless remain unchanged; the proof only
chooses player inputs and asserts actual engine facts. No production engine or Oil gameplay,
business policy, asset, account, clock, state layout or API changed in this stage.

## Exact commands and every attempt

From the actual Oil consumer directory:

```powershell
dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v minimal
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Oil recovery interruption: loaded reclaimed cargo cancellation saved return and ordinary rebuilding" --timeout-ms 180000 --termination-timeout-ms 5000 --output-chars 24000
```

Three target build attempts:

- First failed1.59s, four cascading syntax errors from a missing lambda arrow in the new
  test; no stale runtime was launched.
- Clean1.65s, zero warnings/errors, initial loaded-handling version.
- Clean1.51s, zero warnings/errors, strengthened separated-site/moving-cargo version.

Only this same new exact case ran:

| Attempt | Scope | Checks/failures | PID | Suite/process ms |
|---|---|---:|---:|---:|
| Intermediate | Cancellation during loaded handling; first site autonomously placed | 1654/0 | 17188 | 12625/12729 |
| Final | Separated human first placement, loaded travel, moving return, autonomous second placement | 1925/0 | 11636 | 34790/34899 |

Do not add intermediate1,654 to final1,925 or describe them as two independent cases.
Both runtime outputs are untruncated; zero failed runtime assertions, aborted suites,
timeouts or process restarts. Live exec handles98624 and83453 were polled to terminal
completion. Each physical phase has a60-second/900-update limit and ordinary diagnostics;
the whole process has a180-second watchdog and5-second termination grace. Bounds were not
raised or reached. Wall timing is diagnostic only; all gameplay uses original engine time.

Final checkpoints: first supplied trip still waiting at minute97; canceled site retains
active promises at142; restarted site/resource waits at202/292/382/472; selected HQ/home
repair at503. These are observed game-clock coordinates, not a second scheduling clock.
One read-only source probe guessed a nonexistent cargo-return filename; actual existing
owners were found with rg. No behavior conclusion was inferred from that missing file or
truncated source excerpts.

## Current identities

444 core C# files, sorted relative forward-slash path plus uppercase per-file SHA256 joined
with LF and no final LF:
`0327e6ff4473656c75e2e8a889d804e5cc8d8029b027065aa772b5bb27b7973a`.
Exactly the preceding unchanged production core.

| Current Release artifact/source | SHA256 |
|---|---|
| Engine and actual public consumer engine copy | E811B410F353520502501CF34A6E848B47EB67F0B72DEA42E1E645D3ADA0A6D7 |
| Actual OilCo.PublicProof DLL | 1F3A40959828F01D9B677AA6173CF792B2AB9842B3218524AF5A56D23A89BB76 |
| HeadquartersRecoveryTests.cs | 2EA0A060AAED26CFC722E410F1B31AAE2F08F203036B42773DF4CE17D9FBB76F |
| HeadquartersRecoveryTests.Interrupted.cs | 6E689CC83C87DFBF93F32E81B21C074C1DA448179D17E50A70FFBB9B2677AC5D |

No executable edits after the final successful run. No full/profile/family test, older-case
runtime revalidation, world/profile-wide sweep, new production behavior or platform/Draw/
adapter/demo/headless source work. No staging, commit or push before the full goal/plan.

## Remaining scope and next gate

This proves an actual traveling-cargo cancellation/retry workflow using native providers,
not every cargo disposition, removed source/destination, fuel/topology/area/custom-provider
failure or all financial/private/rival strategies. The shortened positive construction
fixture is not proof of the canonical32-hour outpost duration. Saved twins are not the
separate complete lockstep/replay/fuzz/soak gates.

Next reconcile original P5.007 against the existing affordability/named-port/private-rival
proofs and current controller source, running only concrete missing current evidence; do
not add duplicate recovery scenarios or declare this one flow universal. All12 original
open criteria retain their text/state. The lifecycle skill supplied stage/tracker/evidence
structure; explicit focused-only/no-publication instructions override its generic defaults.

Final handoff checks: all221 original IDs/literal texts map209 checked/12 open, with no
frozen-audit diff. Framework diff check exits0 with35 existing LF-to-CRLF warnings/no
other diagnostics; edited consumer sources also pass. New source/documents have no
trailing whitespace or broken local links; privileged-driver search has zero matches.
Staged paths0 and no matching live public-proof verification process remains.
