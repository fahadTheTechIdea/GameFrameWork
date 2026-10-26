# P3-S21 — lifecycle acceptance verification

Current M134 leaves native2.765/save161/replay2 unchanged and qualifies original receiver
selection with area-gated private service only on a discarded far candidate. Known serviced
truck admission and actual two-unit evacuation stay native, with saved twins/finite19stock.
[Focused171+240=411/0 and every fixture failure/diagnosis](P9_RECOVERY_DESTINATION_POLICY_VERIFICATION.md#current-m134-area-gated-evacuation-receiver-dependencies--2026-10-23).
No free fuel, collision exemption, changed128update bound or gameplay in the driver. Six final
replay verifiers pass; prior native census is reused after exact source/DLL/PDB preservation,
not freshly run. Original broader gates remain OPEN; next audit returns to P7.acceptance.017.

## Earlier P3-S21 checkpoint — engine2.418.0 (not rerun for M121)

## Scope and ownership

Verified the original P3.acceptance.005 named lifecycle matrix on core2.418.0/save154/replay1. No production code or schema changed. Existing World.Step, SimGame, Entity.Update and GameSnapshot remain the only execution/state/publication owners. The lifecycle skill guided reuse of these owners and bounded evidence; no compatibility path or duplicate loop was introduced.

The non-friend extension is instrumentation-only: healthy state sources record actual entity ID/sole clock coordinate and return no candidate. Its authored negative modes exercise recursive entity/world updates, throws and saved visibility-state mutation. Original point construction definitions, enabled file-authored work policy, normal ConstructCommand/DemolishCommand, physical hauling and timed work drive the positive scenario. The thin driver cannot access Game.World or mutate stock/progress.

## Verified matrix

- Initialize is idempotent and publishes initial facts without a hidden entity update. Pending commands create no instant structure.
- Real construction-site birth participates once at its originating coordinate. Material hauling and timed machine work produce a different completed target, retire the site, and update the target exactly once in the same completed step. Live entity callbacks are in stable actual ID order.
- Demolition keeps the structure alive during authored timed work, then removal precedes further entity callbacks. Retired identities never update again.
- Retained initial/site/completed snapshots stay detached. Collection mutation refuses, repeated reads do not invoke state sources or alter hashes, and another company sees no private entities.
- Elapsed-frame cap retains whole backlog and fractional pacing; pause earns no gameplay time, resume exposes retained pacing, zero-elapsed frames spend backlog, and the documented diagnostic batch bypass uses the same kernel rather than a second saved clock.
- Updated rejects recursive Update/Initialize/RunTicks. A throwing host observer reports after settlement, does not forge an engine fault, and releases the guard.
- Authored three-minute horizon completes exactly three minute steps, discards terminal pacing, never starts a fourth step, and restores as ended.
- Five source fault modes latch the first fault at coordinate2, run no later entity callback, advance no incomplete step, withhold new snapshot deltas and refuse checkpoint/hash certification. Swallowed recursion cannot recover the session.
- Initial and later private snapshot throw/mutation probes publish no newly captured prefix. A completed step may precede a capture fault while every public viewer retains its prior safe epoch; later updates never retry failing entity/visibility callbacks.
- Pending construction/removal, partial work and terminal saves continue as hash-identical twins. Physical construction/removal replay and separate interleaved session identity pass.

This closes only P3.acceptance.005 as written. Broader provider contracts, startup callback matrices, arbitrary scale/time, private-query breadth, performance, lockstep, full regression and the complete P1–P12 goal remain OPEN. No claim of whole-phase completion or universal CLR callback purity.

## Actual verification attempts

One NEW exact case only:
`Engine loop acceptance: physical births removals once-only entities reentry pause backlog outcome fault and pure snapshots`.

Solution compile1: exit0,13.23s,0 warnings/0 errors, five projects.
Targeted repair compile2: exit0,8.18s,0 warnings/0 errors.
Targeted compile3: exit1,3.33s, one driver CS1061 (`JsonValue.String` does not exist); repaired to existing `Text`.
Targeted compile4: exit0,4.22s,0 warnings/0 errors.

Runtime1:371 passed/5 failed, suite813ms; owned PID15416 exit1, observed961ms.
Runtime2 diagnostic:371 passed/5 failed, suite757ms; owned PID36016 exit1, observed906ms.
Both failures were one fixture assumption across five modes: the source prototype was last in actual ID order. Diagnostics showed callbacks#1,#2,#3 and fault entity#3. Existing deterministic startup DAG orders independent entries by stable identity, not declaration-array order. The authored fault probe now selects destination and explicitly asserts it is the first actual live entity with later peers available; no engine behavior was weakened.

FINAL runtime3:381 passed/0 failed/0 aborted, suite722ms; owned PID28780 exit0, observed867ms, outcome Exited, no stdout/stderr truncation or observer error. No executable edits after this final pass.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Engine loop acceptance: physical births removals once-only entities reentry pause backlog outcome fault and pure snapshots' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

Limits: outer30000ms, owned termination5000ms, captured output30000chars, physical stage256Updates with actual clock/entity/state/plan/blocker diagnostics. No old/family/full/Oil/Tycoon/platform runtime. Builds are compile evidence only for other cases.

## Tracking and publication

Read-only metadata gate after refresh:1243 source anchors+13 quickstart anchors,0 mismatches;2518 indexed paths/866 Markdown files/7277 local links,0 broken;18 master rows,0 malformed; git diff --check exit0 with34 preexisting output lines. Driver boundary matches0; executable drift0; actual git status --porcelain=v1 --untracked-files=all reports2508 dirty paths,0 staged. Original221 words unchanged;204 line anchors moved and only P3.005 checkbox changed. The sole clock and all three protected Tycoon sources retain preceding SHA-256 identities. These are metadata/source checks, not old runtime verification.

Post-FINAL executable freeze:2026-10-10 02:55:36 UTC,582 selected inputs. Only five changed/new inputs versus the preceding579 selection: new driver, instrumentation source, authored addon JSON, Program registration and ExtensionProof embedded-resource registration. Three new executable files; no production engine input change. No executable edits after FINAL. SHA-256 inventory is recorded in the canonical completion audit.

Original221 requirements retain exact wording; only P3.acceptance.005 changes unchecked to checked:159 checked/62 open. Full goal remains active. Protected G19 scope decision pending; no protected catalog/business/clock file edited. No staging, commit or push: publication is authorized only after the full goal AND plan finish.

