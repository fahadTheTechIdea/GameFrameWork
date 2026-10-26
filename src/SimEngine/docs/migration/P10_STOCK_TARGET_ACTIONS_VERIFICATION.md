# P10-B7c — native stock-target action verification

Current M147/native2.772: reusable stock declarations execute all three stock levels through
the existing owner, including real reserve-gated hauling and saved/cancelled continuation.
[Two focused results749/0, complete coupled streams and image distinctions](P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md#current-m147-reusable-resource-controls-and-detached-quantities--2026-10-23).
This does not rerun or certify the historical broad gate below.

## Historical B7c evidence

2026-10-06 current working source. Build `beep-simengine-2.137.0`; save74/automation3;
operations view7/logistics view15. Focused proof, not P10/P1-P12 completion. Existing stock
row/settings and automation persistent layouts remain unchanged; the normal stock command intent
payload deliberately changes, without an old reader/property/overload.

## Final authoritative gate

```text
dotnet build Beep.SimEngine.slnx --no-restore
Build succeeded: 0 warnings, 0 errors. MSBuild elapsed 2.98 seconds.

dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Automation: threshold and recovery contracts, bindings and private controls"
493 passed, 0 failed in 1 suite (656 ms).

dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Automation: threshold edits, recovery, deferred timing and saved processes"
339 passed, 0 failed in 1 suite (772 ms).
```

Total832 checks, exactly two complete suites, zero final failures/aborts. Includes B5/B6/B6b/B7a/B7b,
not additional to preceding682. Other suites compile only; no third/family/full gate was run.

## Implementation and scoped proof

- LogisticsDemandChange is immutable explicit replacement or desired/enabled intent. The existing
  ConfigureStockPolicyCommand embeds its codec; ILogisticsStockPolicies and normal handler use the
  same canonical admission. Desired edits require an actual existing row and preserve its current
  minimum/maximum/priority and creation age. Native quantity, combined capacity and aggregate demand
  checks remain with the sole LogisticsService. No stock controller/table/queue/registry is added.
- Generic SetRequest/ReplaceRequests return logistics.stock-policy-owner for native stock scope;
  generic WithdrawRequests throws before mutation. Native callers use explicit stock intent.
  Disabling or setting a valid zero target keeps one authoritative stock row, not an implicit deletion.
  Read-only CheckRequests remains shared aggregate admission; work/provider scopes remain unchanged.
- ResourceControlActions.cs replaces the reserve-only implementation file. One required native
  resource/unit/normal-command linker serves distinct reserve and stock action contracts. Required
  JSON stock_target_control_actions selects actual resource/native unit/normal stock command owner.
  The proof JSON is explicitly embedded and master-listed; no auto-scan or fabricated default.
- Existing action metadata/visibility and immutable views discover actual resource-specific
  logistics.stock.<resource-id> bindings. Missing rows have typed blockers and no invented targets.
  Different resources on one entity can be controlled independently; same-resource policies conflict.
  Hidden resource/unit dependencies withhold the complete profile and configuration. Foreign/private
  stock facts are withheld; invalid viewers are no observers.
- Actual native and public-only inventory ports exercise preview/command parity, nonnative quantum,
  ordered-band bounds, absent policies, zero targets, normal intent codec and corrupt discriminators.
  Existing public-only inventory/outlet providers require no coordinator modification.
- Physical refill earns dwell/cooldown, applies through the ordinary deferred command, loads8 units,
  travels and unloads. Real source inventory feedback queues a disabled desired4 action while cargo
  is in transit. Lowering/disabling intent or removing its rule does not erase acquired cargo or
  receiving claims. Subsequent stock policy triggers another physical4-unit refill; total20 is
  conserved. Pending rule, paused clock and in-flight saves continue identically in native/public twins.
- Later player limits/priority replace bands before a due action. A still-valid desired12 action
  retains those current fields; a later maximum6 makes desired12 refuse and suspend without changing
  player settings or physical commitments. Pending ordinary command saves detach caller mutation.
- AutomationSystem order290/state3 and normal command dispatch remain unchanged. One GameClock
  is constructed in World; SimGame.Clock aliases it; World.Tick reads its StepIndex, with no separate
  current-time field. GameClock alone embeds the current-time Sync once. Step indices, deadlines and
  elapsed work are coordinates/durations from that clock, not additional clocks. Clock source was
  not edited; historical85 clock checks were not rerun or counted in this gate.

## Repair history and boundaries

Initial source build found two internal native call sites still supplying the replaced stock Settings
signature; they were migrated. The first new-test gate passed485 contract checks and337 execution
checks with two failed physical latch assertions, one for each native/public inventory. Delivery runs
after logistics refresh; the test stopped at delivery completion before the next refresh settled the
latch. The bounded stage now waits for actual delivery, empty cargo and the settled refill latch.
Engine phase order was not changed. Four additional bypass-refusal checks per provider produced
the final493/339 gate above. Only the same two suites were rerun after a clean build.

62 current build assertions and59 current save assertions migrated mechanically through apply_patch.
All existing stock command/stock-service callers migrated; generic native-stock callers now use the
one stock owner. Unrelated suites were not run, and their historical regression claims are not green
evidence for this source. The dirty/untracked working tree was preserved without a commit or reset.

Arbitrary minimum/maximum automated edits, runtime action editing, utilities/backpressure, wider
provider/global rollback/replay/lockstep/fuzz/soak/allocation gates and full framework acceptance remain
open. No Oil/Tycoon/host/demo/platform/Draw changes. No legacy/compatibility/fallback path.

## Source fingerprints (SHA-256)

| File | SHA-256 |
| --- | --- |
| src/Beep.SimEngine/Standard/LogisticsPolicies.cs | 8F1DEAF39C99F035F41998F61D45194808D8C9E0CDC2E4A1B5D8F68CCAE6E6E8 |
| src/Beep.SimEngine/Standard/LogisticsStockPolicies.cs | 5FB5801A44FC8B9048B8EE937158DA0AD552B956B0412BB85B7AD6EBCF0FE8DF |
| src/Beep.SimEngine/Standard/ResourceControlActions.cs | B2E6D677EED4A86CDC7BABC63B6397079CF9932439C2122C85F5489B40CEE2B0 |
| src/Beep.SimEngine/Commands/LogisticsPolicyCommands.cs | C3CE841C3C50FD0135973F3C0AC8DE27A7D0DEA370DE47469356274C69F63EDA |
| tests/Beep.SimEngine.Tests/StockTargetActionTests.cs | 113EDCB3BC2859DFE36DB0DF28EFA22CBEB2D3A50796BC07B8A009B87BDFE8ED |
| tests/ExtensionProof/content/stock-target-automation.json | E18E7BBBBD9B0C5CA5462339E7B940D195B1156ADEFE69E7182A4CBC6899E9B1 |
| src/Beep.SimEngine/Hosting/GameClock.cs (unchanged) | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |
