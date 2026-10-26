# P4-N2 — Composed startup profile verification

Historical checkpoint; not rerun after P4-N3. [Sole native point startup evidence](P4_NATIVE_POINT_STARTUP_VERIFICATION.md) now records removal of the remaining duplicate point executor. Statements below describe the dated checkpoint, not current remaining startup code.

Date:2026-10-09. [Stage](../../plans/enhancements/P4_COMPOSED_STARTUP_PROFILES_DESIGN.md).

## Scoped result

The existing `WorldSetupProfileDef` now implements `IWorldSetupProfileDefinition` and `IDependencyNode`. Required JSON `includes` edges resolve exact existing profile definitions. Iterative closure collection reuses the native `DependencyGraph<T>` compiler for profile cycles/topology and the existing actual-entry DAG. Shared profile/entry identities are aliases, never copied startup state. Duplicate direct identities refuse. Only the existing `WorldSetupStage` executes the compiled graph and binds actual homes/results. No additional executor, registry, loader, topological algorithm or game clock was introduced.

All51 existing native-profile JSON documents/52 profiles explicitly gained `includes:[]`; one inline cancellation-profile authoring fixture migrated too. Before/after structural comparison found zero changes to unrelated JSON fields. Missing/null `entries` or `includes` no longer become empty defaults. Core **2.329.0**, save **154**, replay **1**; native/provider state codecs and saved field layouts unchanged. Engine identity/protocol fixture updated together; no compatibility reader.

The five new explicitly embedded/master-listed JSON files author an independent external point scenario: a root includes two branches sharing one profile. The shared source appears once and grants20 actual kits once. Native startup places source12, machine4 and destination22 despite prototype position0; a cross-file dependency binds the real source as worker home. An unselected valid profile creates nothing. Reversed profile/include/entry authoring preserves public entity identity/placement. The driver uses ordinary public work/haul requests, immutable views, Update, saves/hashes and replay; no live World mutation or host gameplay.

## Exact evidence

Both solution builds clean, zero warnings/errors: **6.01s**, **5.60s**.

ONE NEW exact case ran twice:

| Attempt | Passed | Failed | Aborted | Case time | Process |
| --- | ---: | ---: | ---: | ---: | --- |
| First | 39 | 3 | 0 | 515ms | PID28588 exit1, observed670ms |
| Final | 46 | 0 | 0 | 522ms | PID39300 exit0, observed669ms |

Both exited normally; stdout/stderr untruncated; no observer error, timeout or forced termination. Executable freeze observed **2026-10-09 00:42:45 UTC**; subsequent edits documentation/audit only.

First run exposed a real defaulting hole: `JsonReader.Strings` accepted absent includes. WorldSetup's actual profile owner now requires explicit array kinds before reading both fields. The other failure was an incorrect new-test observation: the worker had already begun ordinary return-home movement. The driver now observes actual target reach during work, rather than requiring a completed worker to remain there. No movement shortcut was added.

Final proof covers once-only diamond startup, public actual placement/home, noninstant staffed travel/work, physical pickup/travel/handling of4 kits (source16/destination4), saved travelling and hauling twins/no startup regrant, replay identity, and before-Initialize refusal for missing profile, self/indirect include cycles, repeated include/direct entry, missing includes/entries and null includes.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Composed startup: native profile diamond, point placement, physical work and saved hauling" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

## Limits and next step

`PointStartDef`/`PointStart` still serve unmigrated historical point fixtures; the new scenario declares no old point starts. This prerequisite deliberately does not claim their removal. Next: migrate those fixtures/profiles/authoring paths and delete the duplicate executor/section, using the composed native owner. Existing migrated profiles, P4-N1 and all older tests are compile/source checked only, not rerun on2.329.0. No family/full/Tycoon/Oil/platform runtime. Company/spatial admission retains existing generation timing after initialization; no stronger whole-generation atomicity guarantee. Deep-content stress, performance, mod faults, broad accounting/durability and full P1–P12 gates remain open.

Full active goal/plan are not complete. No staging/commit/push until the user's full-completion-and-verification condition is satisfied.
