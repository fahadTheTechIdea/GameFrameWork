# P4-I1 — Code audit and established-game research

Date: 2026-10-07. Status: research complete; documentation only.
Parent: [Game modes](P4_GAME_MODES_DESIGN.md). Next: [I2](P4_GAME_MODE_CONFIGURATION_DESIGN.md).

## 0. Problem statement

There is no `GameModeConfiguration`, `game_modes` section, or shared mode-property contract in
the current framework source. A scenario composes modules and per-module settings, but optional
rule defaults and their effective engine-wide enforcement are not centrally described.

## 1. Goals

1. Locate existing responsible classes before proposing a new configuration class.
2. Verify comparable public game-setting patterns from primary developer/publisher sources.
3. Distinguish source inspection, earlier test evidence, and unverified implementation.

## 2. Design evidence: actual source

Paths below are relative to the Beep.SimEngine repository; line numbers are inspection anchors,
not immutable API references.

| Source anchor | Observed behavior | Consequence for the plan |
|---|---|---|
| `src/Beep.SimEngine/Kernel/Modules.cs:173` `ScenarioDef` | Modules, time, space, seats, frozen settings; `SettingsFor` at206; no shared mode | Extend this scenario, not a second game/scenario class |
| `src/Beep.SimEngine/Kernel/Modules.cs:226` `GameDefinition`, `Build` at317 | Registers code/content, selects/composes modules, builds one world | Resolve configuration before provider composition/Initialize |
| `src/Beep.SimEngine/Hosting/GameFactory.cs:41`, `NewGame` at73 | Sole public creation/load owner | Add mode metadata/validated requests here, not another factory |
| `src/Beep.SimEngine/Hosting/SimGame.cs:149` | One public game lifecycle; existing `NewGameRequest` at20 | Expose immutable configuration from this session |
| `src/Beep.SimEngine/Kernel/World.cs:10`, `Sync` at23 | Session settings include scenario/modules/time/space and serialize once | Persist additional mode intent in this same owner |
| `src/Beep.SimEngine/Hosting/ContentSet.cs:65`, `ResolveScenario` at198 | Explicit manifest documents; selected-module resolution | Master-listed profile/property files, no scanning or second loader |
| `src/Beep.SimEngine/Content/ContentTypes.cs:42`, `ResolvedContent.cs:13` | Registered native sections; sole frozen definition catalog | Add native mode/property definitions to this registry |
| `src/Beep.SimEngine/Standard/BusinessModule.cs:20` | Policy/mode in module settings; optional accounts; optional income-policy binding | Centralize enablement, retain module-specific provider parameters |
| `src/Beep.SimEngine/Standard/WorkStart.cs:177`, `Work.cs:550` | Work costs check/charge actual IEconomy; composition requires it for cost requirements | Money/budget-off needs real requirement-owner enforcement, not UI hiding |
| `src/Beep.SimEngine/Standard/Transformations.cs:316,325,492` | Positive process cash checks, charges and requires IEconomy | Manufacturing charge needs its own monetary gate |
| `src/Beep.SimEngine/Standard/Logistics.cs:58,67,164` | Priced supply checks/charges IEconomy | Disabled trading must not turn suppliers into free stock |
| `src/Beep.SimEngine/Kernel/SaveGame.cs:44,88` | Content fingerprint and session settings are checked | Extend current save identity, not a mode save subsystem |
| `src/Beep.SimEngine/Hosting/Lockstep.cs:114,152` | Peer hello compares content and `SameSettings` | Same content with different overrides must also refuse |

Source-only H2 observations: `BusinessAccounts` currently declares codec2; `BusinessModule`
registers view2 and can bind `IResourceProductionSettlement`; primary-only income policies exist.
The actual `ResourceTransactions`/`ResourceConversions` source has no settlement invocation yet.
Those additions are not built or tested by this research pass. H1's earlier72 checks used codec1/view1;
they do not prove current H2 integration or a working mode switch. Preserve the unfinished source.

## 3. Research and proposed API direction

**No Man's Sky:** Hello Games documents game modes as presets over configurable difficulty settings,
including independent crafting cost, fuel, technology damage and power options. This supports a
preset plus explicit properties rather than unrelated mode-specific engines. Its live difficulty
changes are not copied into the initial SimEngine design. [Official Waypoint update](https://www.nomanssky.com/waypoint-update/).

**Factorio:** its developer API defines map-generation presets with basic/advanced settings and
omitted-setting defaults; advanced groups include pollution and difficulty settings. This supports
authored grouped settings and deterministic preset resolution. SimEngine will use explicit JSON
property defaults, not undocumented fallback values. [Official MapGenPreset documentation](https://lua-api.factorio.com/latest/types/MapGenPreset.html).

**Cities: Skylines (original game):** the publisher's manual lists separate Unlimited Money, Unlock All,
and Hard Mode modifiers. This supports independent monetary and progression axes rather than one
all-or-nothing “creative” switch. SimEngine's money-off means absence of accounts, not that game's
unlimited-money behavior. [Publisher manual, Game Mods, PDF page7](https://shared.steamstatic.com/store_item_assets/steam/apps/255710/manuals/CitiesSkylines-UserManual_EN.pdf?t=1746722581#page=7).

**Minecraft:** Mojang distinguishes resource collection/building in Survival from unrestricted
building in Creative, and separately describes difficulty levels. This supports keeping physical
resource rules distinct from mode/difficulty labels. We do not import combat, hunger, flight,
instant construction, or a claim that all survival games have no economy. [Official game-mode guide](https://www.minecraft.net/en-us/article/what-minecraft).

These sources document observable configuration patterns, not those games' internal C# architecture.
The recommendation for `GameModeConfiguration` is an inference from those patterns plus the audited
engine, and the survival monetary defaults come from the user's requirements.

## 4. Files to change

This stage changes plans/master/entry-point documentation only. Actual source changes are assigned
to I2–I4. Existing H1/H2/H3 plans must stop equating module selection with compulsory money/income.

## 5. Error handling matrix

| Misinterpretation | Required resolution |
|---|---|
| Survival automatically implies every difficulty rule | Preset explicitly declares its properties |
| Business selected implies every monetary feature | Independent enabled flags and capability checks |
| A UI toggle hides actual cash checks | Enforce at engine owners; proof must execute physical work |
| Old green count proves changed codec/source | Label historical and unverified evidence separately |

## 6. Compatibility

Direct architecture/schema migration is allowed. No legacy settings inference or compatibility layer.

## 7. Verification

Completed: targeted source reads/searches and primary-source browsing. No build, test suite, host,
platform, or runtime acceptance execution. Research links were accessed on2026-10-07.

## 8. Risks

Published gameplay settings do not prove third-party implementation details. Use them as patterns,
not copied internals. Survival and sandbox remain game-authored labels, not compiled universal laws.

## 9. Out of scope

Runtime implementation, OilFieldDays mutation, platform integration, full regression, live mode changes.

## 10. Sub-task execution order

- [x] Inspect creation/content/settings and actual monetary admission owners.
- [x] Research four established games from primary sources.
- [x] Write source-grounded stages and revise conflicting optional-economics plans.
- [ ] Implement and prove I2–I4; research completion is not gameplay completion.
