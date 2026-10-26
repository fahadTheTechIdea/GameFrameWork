# P4-I2 request intent / I4 core identity — focused evidence

Date: 2026-10-07. Runtime build: `beep-simengine-2.207.0`; save format111.
Stages: [I2 configuration](../../plans/enhancements/P4_GAME_MODE_CONFIGURATION_DESIGN.md),
[I4 identity](../../plans/enhancements/P4_GAME_MODE_VERIFICATION_DESIGN.md).
Status: request capture and core save/peer identity verified; full stages remain open.

## Implemented ownership

- Public immutable GameModeAssignment carries validated stable property ID and tagged value.
  NewGameRequest carries explicit typed override intent; absence is an explicitly empty collection.
- GameModeConfiguration alone defensively captures, ordinal-sorts and refuses duplicate/unavailable
  assignments. Existing resolution applies property defaults → profile → scenario → request and
  records Request provenance. It validates the full result without changing prerequisite flags.
- Existing ScenarioDef creates the effective projection from frozen definitions and captured intent.
  No request mutates the shared ResolvedContent cache or scenario configuration. An empty request
  shares the immutable scenario projection; an unconfigured scenario cannot accept overrides.
- WorldSettings captures only selected profile and canonical request assignments, not a second
  effective property database. World validates profile identity and resolves before composition.
- GameFactory.PreviewGameMode uses that same capture/resolution path without composing or initializing
  providers. This is a typed configuration preview, not enabled-provider/cost enforcement diagnostics.
- GameModeValue.Sync is the one tagged value wire representation used by captured assignments and
  derived identity. Existing WorldSettings.Sync owns intent; existing World.Sync owns effective
  identity/hash. Restore refuses invalid/unknown/noncanonical tags and IDs without partial collection
  installation. Valid-but-forged header intent still fails the derived body identity before Initialize.
- GameFactory load-menu compatibility validates profile and override domains. Load reports invalid
  saved content/settings as corrupt state; it never repairs intent or installs fallback profiles.
- Existing Lockstep.SameSettings compares profile and canonical typed assignments. Different values
  or equal values with different explicit provenance refuse as net.settings-differ before state checks.
- Direct save111 / build2.207.0 migration; no old reader, alias, replay codec, clock or scheduler.
- 89 existing test files migrate144 build/save identity lines. One existing configuration test helper
  becomes internal so the new scenario reuses the same master-listed fixture instead of copying it.
  Those older scenarios compile only; no runtime result is inferred for them.

GameClock.cs SHA256 remains
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
Exactly one `new GameClock` remains in World; none of this slice changes clock ownership.

## Executed gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Game modes: captured request intent, saved identity and peer refusal"
```

One build:0 warnings,0 errors,5.01s reported. One exact new scenario invocation:
**84 passed,0 failed, one suite,134ms**. No repair/rerun or aborted suite. No previous/full/broad family/
business/recovery/platform/Draw suite was run.

The new scenario observes the real configuration/settings/hash/save/peer owners. It covers preview
purity and parity; all three typed value kinds; precedence and provenance; precomposition sharing;
retained caller mutation, including during composition; read-only captured intent; reversed input
byte-identical saves; shared-content isolation; changed-value and equal-value/different-intent hashes;
saved headers, compatibility, restored continuation and interleaving; unknown/locked/type/range/domain/
dependency/duplicate/null/unavailable request refusal before composition; different-settings peer
refusal; shuffled frame messages and four actual peer updates; malformed tag/identity/order/null-text
decoding; forged mode/domain/body mismatch refusal. Fault injection and existing InitializeProbe
measure refusal boundaries; they implement no gameplay rules.

The four-update bound is local to this proof. It is not a soak, physical manufacturing/construction,
positive monetary cost, enabled-provider, income or Tycoon execution proof. Historical99+27 results
belong to build2.206.0; neither scenario was rerun on2.207.0.

## Explicitly open work

- Mandatory explicit-profile migration of all owned scenarios. Unconfigured old fixtures still expose
  null rather than a fabricated default, and cannot take request overrides. I2 is not fully complete.
- I3 actual enabled-provider availability and consumption of monetary configuration. BusinessModule
  still has overlapping setting/mode interpretation. Flags alone do not alter payment behavior.
- Work/construction/manufacturing remain ordinary simulation capabilities, never Tycoon features.
  Existing resource-only proof is historical; disabling positive authored monetary charges needs I3.
- H2 account2/view2/income scaffolding compiles only. Resource commits do not yet invoke settlement;
  do not claim actual production income, budgets, cancellation settlement or advanced Tycoon.
- Configuration preview is pure and typed; complete capability/parameter diagnostics are still open.
- Replay command/checkpoint initial-intent proof, independent executable rule-extension proof,
  durability/fault/provider matrix, physical wait stages, stress/allocation and final acceptance are open.
  No separate mode replay subsystem was invented.
- Full regression remains unexecuted by user instruction. The overall framework goal remains active.

Only Beep.SimEngine source/tests/plans were changed; no OilFieldDays, host/demo gameplay or platform
implementation was added. The lifecycle skill kept the staged source/evidence/tracker aligned while
the user's focused-verification requirement superseded its generic full-regression guidance.
The adjacent source manifest is a scoped hash inventory, not a full-repository cleanliness claim.
