# P4-AG1 — Activity graph disclosure verification

Date2026-10-13. Core2.521.0/save154/replay1; state and view schemas unchanged.

## Original owner and contracts

`WorkBoard.WalkDisclosure` now includes the compiled graph's actual executor and role definitions, requirement aliases, node effects and final effect. Nested `IWorkRequirementGroup` entries are read through the existing observation guard. Every alternative is included, not only the selected branch. Explicit duplicate disclosure aliases still fail; legitimate shared graph references are visited once. No requirement evaluation, reservation, effect execution, copied catalog, authorization cache or scheduler was introduced.

The existing independent visibility proof module adds typed requirement/effect targets using the original knowledge owner. JSON owns the sequential workflow, nested all/any requirements, earlier-completion prerequisite, durations, effects, forgetting and visibility facts. Thin tests submit ordinary player commands and read detached views; no World/resource/progress writes.

## Build and attempts

Release solution:10.37s,exit0,0warnings/errors. Focused fixture repair build:7.78s,exit0,0warnings/errors.

```text
dotnet build Beep.SimEngine.slnx -c Release --no-restore
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Activity graph disclosure: nested requirement and node final effects stay private while earned work continues" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

1. FIRST0passed/1failed/0aborted in266ms,PID1112/process431ms/Exited1: authored any-group repeated one dependency ID; original compiler correctly rejected it. Fixture now uses two distinct prerequisite definitions; the hidden one is the unselected alternative. Engine validation unchanged.
2. PASS246passed/0failed/0aborted in1671ms,PID41600/process1893ms/Exited0.
3. FINAL246passed/0failed/0aborted in1071ms,PID42000/process1229ms/Exited0, after explicit profile assignment repair.

All exact-case streams untruncated/no observer error. No executable edits after FINAL. No existing/family/full runtime executed.

Metadata-only profile attempts: an invalid shorthand was rejected; the actual profile then correctly rejected the newly registered but unassigned case. The original JSON profile now explicitly includes that case. Final `--profile-info tests/Beep.SimEngine.Tests/testdata/framework-validation-profile.json`:721registered/included,0excluded/long markers,0runtime suites,exit0. This is not a regression result.

## Physical proof

Six flows: minute/half-minute time definitions crossed with hidden nested leaf requirement, hidden node effect and hidden final effect. New intent preview/command return UnknownDef with no diagnostic prefix. Normal staffed discovery makes the whole graph visible. Players start actual sequential positive-time work; the first node earns its fact before the final node completes.

Forgetting hides the selected metadata reference. Progress, order details and completion history stay private to the player; foreign progress is also withheld. Accepted work continues through original execution owners. Saves restore identical twins, the sole clock pauses both, the final physical effect earns the original completion, and observer history confirms the same order and positive duration. Re-discovery exposes that earned history rather than creating a replacement order. Retained partial views remain detached. Settled save/load, replay verification and isolated-session hashes pass.

Inner dependency metadata specific to arbitrary providers, hostile metadata callbacks, executor/role hiding as separate runtime cases, cross-workflow projections, fault/lockstep/stress/performance and full acceptance remain unverified. No original compound gate closes.

## Authority and metadata

Tycoon source and business behavior unchanged. User authorizes only mechanical Tycoon API migration and one final full regression after implementation finishes. Goal/plan remain open; no staging, commit or push.

Metadata-only validation:1899source anchors/1081selected executable inputs/13quickstart anchors,0hash mismatch/0post-pass executable drift;3055paths/1113Markdown/8816initial local links/0broken;18master rows/0malformed. Original221literal gates retain their text, checkbox states and line anchors. Diffcheckexit0 with34existing LF/CRLF warning lines; dirty3043/staged0. All12Tycoon source hashes and sole clock unchanged. Added four source files: two executable inputs plus stage/evidence. Seven previously selected executable inputs changed, one production source owner changed plus engine identity; no new production class. Final document-only footer refresh adds one local link and requires anchor refresh, not gameplay rerun.
