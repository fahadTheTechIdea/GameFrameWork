# P10-B7b — native outlet reserve-action verification

Current M147/native2.772: sole shared reserve/resource declarations retain original normal
commands/receipts and current policy fields. Native/direct definitions drive the same coupled
physical workflow. [Actual350/0 plus399/0 visibility and limits](P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md#current-m147-reusable-resource-controls-and-detached-quantities--2026-10-23).
The old broader gate below is historical, not a current family rerun.

## Historical B7b evidence

2026-10-06. Build `beep-simengine-2.136.0`; save73/automation3; operations view7/logistics view14.
Focused proof, not P10/P1-P12 completion. Provider settings and rule persistent layouts are unchanged;
the existing outlet command's intent payload changes deliberately, with no old reader or alias.

## Final authoritative gate

```text
dotnet build Beep.SimEngine.slnx --no-restore
Build succeeded: 0 warnings, 0 errors. MSBuild elapsed 3.08 seconds.

dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Automation: threshold and recovery contracts, bindings and private controls"
394 passed, 0 failed in 1 suite (928 ms).

dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Automation: threshold edits, recovery, deferred timing and saved processes"
288 passed, 0 failed in 1 suite (1108 ms).
```

Total:682 checks, exactly two complete suites; zero final failures/aborts. Includes B5/B6/B6b/B7a,
not additive to preceding545. Other suites compile only. No full/family/third gate was run.

## Implementation and scoped proof

- OutletPolicyChange is immutable explicit replacement or native one-resource reserve intent. The
  existing command codec embeds that intent once; normal handler admission and provider commit use
  LogisticsPolicyRules.CheckChange. IOutletPolicy alone stores current settings; no second reserve
  store, controller, queue, registry, command, clock or compatibility property exists.
- Resolve intent against current settings at actual commit. Preserve other resource reserves, export
  priority and destinations. Zero removes exactly one reserve. Negative/fractional/unknown resource,
  wrong native unit and combined-capacity failures refuse without clamping or fabricated defaults.
- Required JSON outlet_reserve_control_actions selects resource/unit/normal command/owner. The two
  proof files are explicitly master-listed and embedded, not auto-scanned. Composition rejects missing
  resources/units and absent/wrong logistics command ownership.
- Required action ContentDependencies aliases actual catalog definitions. Hidden native resource or
  unit withholds the complete profile, target discovery and configure admission through one visibility
  gate. Discovery dispatches CopyTargetIds through the actual contract provider, guards target/time/
  identity purity, sorts IDs and rejects duplicates. Public proof still discovers independent commands.
- LogisticsView's common visibility predicate requires the explicit observer or a seated owner;
  malformed viewers are no observer. New reserve preview shares actual normal command admission.
- Both native and public-only IOutletPolicy owners protect the starting stock, earn dwell/cooldown,
  defer changes, react to player threshold edits and later unrelated live policy edits, and dispatch
  a real carrier. Loading, physical travel and unloading advance through the existing engine owners.
  Reserve/disabled-export edits and rule cancellation do not erase acquired cargo or receiving claims.
  Actual supply is conserved across source/cargo/destination; interrupted saves continue identically.
- Changing other reserves before a due action can make its combined bill invalid. The ordinary
  handler refuses at actual commit, suspends with the typed blocker and leaves those player settings
  intact. Native/public saved refused decisions and physical jobs remain identical.
- Pause uses the sole GameClock. Pending rule saves and pending normal-command inbox saves preserve
  hashes and actual settings. Mutating a caller's submitted command does not change detached intent.
  Explicit disabled export actions also defer through normal rule timing and provider commit.
- Corrupt intent kind bytes refuse; absent replacement and negative reserve construction do not infer
  defaults. Existing generic provider mutation/fault and physical process tests remain in these suites.

Initial repair gate:374/243 checks passed before one exception in each suite. The new source fixture
was authored nonpublic, correctly excluding automatic logistics; it now explicitly enables supply.
The wrong-unit fixture altered only one action, correctly failing the existing equal-unit composition
contract; it now alters both to exercise native reserve preview refusal. No admission rule was relaxed.
Later same-two-suite proof additions and final review produced the gate above.

62 current build assertions and59 current save assertions migrated mechanically through apply_patch;
old outlet command call sites use explicit replacement intent. Those unrelated suites were not run.
Scoped git diff whitespace check passed (existing LF/CRLF notices only); new files were source-reviewed.
Exactly one new GameClock construction remains in World. Clock implementation and current-time codec
were not edited; historical85 clock checks were not rerun or counted in this gate.

## Final source fingerprints (SHA256)

```text
Standard/LogisticsPolicies.cs          5604E2BA7D8F8C1B9166EBD1B41E0E8AF15CA2482FC763CCEA5D42CD5D6DC1AE
Commands/LogisticsPolicyCommands.cs    7531030534D26892783A3A4BE10BF6EB4564AE7D5F3F4525B05D28630B5DFE96
Standard/ControlActionPolicies.cs     31705BF8F768CECA02C2BE7F8DA7B8198D1E0AF291A5940BBEEAD132CD561984
Standard/OutletReserveControlActions.cs 36667D4F88E9E7E5B08A9478EEFDA05E8C6022D58059E0C157625ED09630F2E8
Views/OperationsView.cs               6D34CB1E63728B7F78E480709AF1E15EA19E4D1BB665413CE69029EA6E3C6292
tests/OutletReserveActionTests.cs     572529F6FD9BB639FB3DB1980B16E0A7A3DDFBC0E5D0D27D53127C6521E61D5A
```

Engine files are under src/Beep.SimEngine; test file is under tests/Beep.SimEngine.Tests.
No source changes after this final gate. Documentation is updated separately afterward.

## Remaining requirements

Stock-band actions, runtime action edits, wider provider/fault/global rollback matrices, full utility
consumer allocation/backpressure, replay/lockstep/fuzz/soak/allocations, full acceptance and P1-P12 are
still open. This does not claim new pollution physics, scientific hydrology or completed oil extension.
No Oil/Tycoon/host/demo/platform/Draw changes. Lifecycle skill enforced staged owner-preserving
implementation and focused evidence; it did not trigger a full regression run.
