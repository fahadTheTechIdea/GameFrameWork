# P6 quickstart verification — 2026-10-08

Canonical repository: C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.
Core2.304.0/save144/replay1 unchanged; sample quick module1.1.0.
Executable inputs frozen at2026-10-08 16:12:01 UTC after green focused run.

## Observed commands

Initial `dotnet build help/quickstart/Host/QuickGame.Host.csproj --no-restore` failed in1.87s:
CS1061, World.SpawnAt no longer exists. No fallback API was reintroduced.

After migration:

- `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore`:
  clean9.42s,0 warnings/errors; actual public sample module compiled too.
- `dotnet build help/quickstart/Host/QuickGame.Host.csproj --no-restore`:
  clean1.83s,0 warnings/errors; standalone host not executed.
- After adding dimensional resource assertions, the same test project build:
  clean5.92s,0 warnings/errors. No runtime suite preceded this final compilation.

ONE distinct NEW exact case, FIRST runtime attempt:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "P6 quickstart: authored initialization and current public lifecycle" --timeout-ms 10000 --termination-timeout-ms 2000 --output-chars 12000
```

28 passed,0 failed/aborted,119ms. ChildPID26032 exited0, observed268ms; neither output truncated,
no observer failure. Existing bounded process owner used, no new execution infrastructure.

## What this proves

The actual documented master files load through ContentSet and the actual public sample module.
Existing authored setup creates one correctly owned/stateful nonspatial entity. Initialize is
idempotent; three Update calls settle engine snapshots at the corresponding sole-clock epochs.
Resource catalogue exposes the explicitly authored ore, dimensional mass unit and solid phase.
Resource-only mode selects no Tycoon. Retained snapshot collections are immutable; saves restore
the same hash and continue identically; a second session starts with its own time/setup.

## What this does not prove

Console output/hash was not observed and no stale historical console hash is reported as current.
The sample is nonspatial and contains no physical jobs/processes; it is not an industrial acceptance
scenario. No older/family/full suite, Oil consumer, platform runtime, stress/allocation or broad
durability gate was run. No engine version/layout/clock change, Git staging, commit or push.
Full P1–P12 and goal remain incomplete. See the
[acceptance inventory](../../plans/enhancements/COMPLETION_AUDIT.md).

## Read-only source/document audit

After documentation changes,358 prior core C# paths were hash-compared with the preceding captured
dependency identity manifest; none changed. Sole GameClock SHA256 remains
35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.
Current inventory:1888 paths,619 Markdown documents,3993 checked local Markdown links,0 broken.
All18 master phase rows retain five columns. Actual `git diff --check` exited0;34 existing line-ending
warnings are not compilation failures. Source anchors in the acceptance inventory cover the named
primary documents/owners only. This metadata audit is not another executable test or full baseline.
