# P6 — Integrated resource-only colony acceptance: focused evidence

[Stage](../../plans/enhancements/P6_INTEGRATED_COLONY_ACCEPTANCE_DESIGN.md).
[Master](../../plans/enhancements/MASTER_TRACKER.md).
[Self-excluded inventory](P6_INTEGRATED_COLONY_ACCEPTANCE_SOURCE_MANIFEST.json).

## Authored gameplay and single ownership
Explicit colony-acceptance-master.json lists all actual code modules and shared/new documents.
The existing Colony.Factory gains only a pure optional master-document input, not another composer.
colony-acceptance.json authors its startup, existing resource-only mode, material/tool/permit
requirements, five-node phased incident DAG, finite pressure points, signals/conditions, objective
dependencies and outcome. Native engine owners execute all behavior; no custom acceptance scheduler.
Incident pressure is automatic engine-side normal command submission, not a test-raised emergency.
Manufacturing/construction reuse original prototypes, transformations, projects and physical hauling.
Shared native crane now declares the existing work_policy component with one explicit original
colony-work.json profile. It supports normal priority/interruptibility/emergency commands rather
than relying on a missing capability or fabricated policy fallback.
No mandatory Tycoon, money, valuation, market, buyer or seller participates in this scenario.

The existing GenericColonyCompositionTests owner gains one partial file and ONE new exact case.
Its older case is not executed. Shared pure observations/commands are reused; no live World access,
reflection/internal reads, stock writes, forced spawns, progress changes, simulated repairs or victory.
Native gameplay uses the existing public modules/contracts/JSON; test predicates observe expected
results only. Bounded stages use2000 Updates plus a30-second failure watchdog and public diagnostics.
The stopwatch is not simulation time; the sole GameClock is unchanged.

Build2.303.0/save144/replay1/board44/work-system41/work-view86 unchanged.
No runtime engine source or schema changed; canonical content identities include authored changes.

## Every executable attempt
Both builds: `dotnet build Beep.SimEngine.slnx --no-restore`.

1. Clean4.24s:0 warnings/errors.
2. Clean3.74s:0 warnings/errors, after the observed missing authored policy capability.

ONE distinct new exact case only:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Generic colony acceptance: authored pressure, physical industry and objective recovery"`

1. 61 passed /2 failed /0 aborted,4108ms. Normal crane policy preview and command refused
   work.not-policy. Remaining physical emergency/industry/objective/replay assertions passed.
2. **63 passed /0 failed /0 aborted**,3517ms, after authoring that actual native capability.

No old/family/full/Oil/Tycoon/platform runtime. Only the SAME observed new-case failure repaired.
Executable source/proof/tests/JSON frozen2026-10-08 14:24:49 UTC; docs/read-only work afterward.

## Evidence actually established
Engine-owned JSON starts12 actual entities in two companies; company0 has50 industrial plus10
preparedness steel, company1 retains its own10 emergency steel. No priced market or fake account.
Normal preview/command configures actual crane policy; manufacturing queues two machines.
Build intent creates a real site, not a completed structure; two outputs do not appear immediately.
Native pressure generates exactly one accepted incident and spends its sole authored pressure point.
Entity.Update settles active stopped state. Detection/isolation/repair/cleanup/restart share the
existing WorkBoard DAG. Actual machine work consumes isolation1 plus repair3 steel, holds native
tools, and blocks the parallel cleanup branch on the authored disabled permit.
Pause stops the sole clock and all industry/response state. Saved pending/active/partial/paused/
cleanup/ended sessions continue identically. Retained incident views stay detached.
Machines are manufactured sequentially; construction completes with real loaded hauling/travel/work.
Objectives observe actual fleet/structure/emergency facts and activate dependents on later Updates.
Unresolved cleanup prevents victory. A normal player permit command cannot instantly resolve it.
Physical cleanup/restart resolves one episode; only the authored native objective effect ends play.
Terminal company0 steel is6: opening60 minus industry50 minus response4. Save/outcome/replay do
not create or duplicate resources. Foreign reserves/episode and objective enrollment stay private.
An interleaved untouched session retains its hash. Canonical replay reproduces final authoritative
hash across ordinary player commands plus engine-generated pressure and objective completion.

## Remaining acceptance scope
This closes the integrated resource-only colony emergency/industry/objective proof slice, not the
full framework plan. Non-tile Planet X extraction/processing/deployment, wider provider atomicity,
accounting/fault/soak/allocation/lockstep gates, Oil parity/rival acceptance and authorized broad
regression remain open. The30-second observer watchdog does not prove termination of a blocked
single synchronous Update; an external process watchdog remains a final robustness gate.
Full goal ACTIVE. No staging/commit/push until full goal AND plan finish and verification.
