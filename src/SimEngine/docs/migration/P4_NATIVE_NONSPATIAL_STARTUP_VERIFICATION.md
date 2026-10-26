# P4-N1 — Native nonspatial startup verification

Date: 2026-10-09. [Stage](../../plans/enhancements/P4_NATIVE_NONSPATIAL_STARTUP_DESIGN.md).

## Result and scope

Scoped implementation verified: local-work, process, and resource providers no longer register `PointStartDef` or execute `PointStart`. Native WorldSetup contracts own authored startup. Core **2.328.0**, save **154**, replay **1**, existing system/view/component schemas and saved layouts unchanged. No compatibility loader, default profile, second startup owner, game clock, platform logic, or business behavior was introduced.

Nine existing JSON documents migrate 20 entries to explicit native fields and selected profiles; seven masters explicitly register `sim.world-setup`. Derived automation, inventory-signal, transformer, transformation-gate, incident, lifecycle, resource-only and optional-money composition paths preserve native selection. Appended fault-fixture entities link native profiles. All unrelated JSON fields outside startup/scenario selection were compared with their pre-edit values and unchanged.

The new driver uses public player commands, `Game.Update`, immutable inspection/entity/work/process facts, saves, hashing and replay. No live world mutation, gameplay calculation, raw spawning, stock assignment, or separate timer is performed in the driver.

## Exact verification

Both solution builds succeeded with zero warnings/errors: **5.72 s**, **5.91 s**.

Only ONE NEW exact case ran, twice:

| Attempt | Passed | Failed | Aborted | Case time | Process result |
| --- | ---: | ---: | ---: | ---: | --- |
| First | 65 | 3 | 0 | 503 ms | PID 43276, exit 1, observed 657 ms |
| Final | 72 | 0 | 0 | 438 ms | PID 26944, exit 0, observed 584 ms |

Both processes exited normally; stdout/stderr untruncated; no observer error, timeout, or forced termination. Executable freeze observed **2026-10-09 00:27:58 UTC**. Only documentation/audit edits afterward.

First-run failures were incorrect new-test expectations, not corrected engine behavior: timed work spends inputs on its first productive update, not accepted intent; nonseated-company admission runs at generation after systems initialize. Tests and design now state those actual boundaries. Missing references, cycles, invalid placement and obsolete startup sections reject before initialization. Company rejection precedes that entry's spawn; this does not prove whole-generation transactional rollback or side-effect-free initialization.

The final case proves three separate nonspatial sessions, exact authored initial entities/stock, no geometry or Tycoon selection, preview purity, noninstant six-update work/process completion, once-only productive consumption/output, saved interleaved twins, no startup/opening grants on restore, self-describing saves and native replay identity. Original historical fixtures are compiled but not rerun.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Native startup: nonspatial work, process and resource contracts with saved continuation" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

## Open work

Spatial `point_starts` still requires a separate explicit-placement migration. Derived fixtures and optional Tycoon fixtures are source/compile checked only; no historical/family/full/Oil/platform runtime was run. This result is not broad regression, concurrent-provider, all-genre, performance, fuzz, lockstep, soak, or full-plan acceptance proof. Native generation still follows initialization; no stronger lifecycle guarantee is claimed.

Full P1–P12 and active implementation goal remain open. No staging, commit, or push: the user's publication condition requires the full goal AND plan to finish and be verified.
