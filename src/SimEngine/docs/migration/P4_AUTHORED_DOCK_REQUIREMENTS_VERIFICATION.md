# P4-G2b dock requirements — scoped verification

2026-10-07. Canonical Beep.SimEngine only. Entire goal and plan remain ACTIVE.
No commit/push; publication waits for full completion and verification.

## Actual invocation ledger

All builds used `dotnet build Beep.SimEngine.slnx --no-restore`.

1. Exit1,0 warnings/1 CS0534 error,2.27s: new leaf omitted abstract ReadCore override.
   Added the explicit identity-only reader; selected content rejects extra fields.
2. Exit0,0 warnings/errors,10.82s. Live session89407 completed through its original handle,
   not restarted. First runtime below used this current binary.
3. Exit0,0 warnings/errors,5.08s after the observed scenario corrections.
4. Exit0,0 warnings/errors,2.59s after moving the invalid-field assertion to selected NewGame.

Every runtime used EXACTLY:

`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Dock requirements: authored admission, native queue and saved physical work"`

1. Exit1,42 passed/3 failures,683ms: two wrong aggregate blocker assertions and one bounded
   workflow exception after900 Updates. The first completed worker stayed at home/exit and
   blocked second departure. No engine teleport, duplicate queue or timeout extension added.
2. Exit1,61 passed/1 failed/0 aborted,490ms: extra JSON field tested during lazy catalog
   discovery rather than selected NewGame. This was a proof-stage assertion correction only.
3. Exit0,62 passed/0 failed/0 aborted,499ms.

FOUR builds, THREE runtimes, ONE distinct exact NEW bounded case. 101 older files/101 build
identity lines migrate and compile ONLY. No older/family/full/platform/stale runtime gates.
After final green source/test/JSON/public proof FROZEN; only documentation and evidence edits.

## Single authoritative ownership

Master-listed `dock_work_requirements` supplies DockWorkRequirementDef/IWorkerWorkRequirement.
WorkRequirementContext carries the actual candidate role selected by IWorkerDispatch, or the
actual retained job role. No second role selector. `IWorkDockAdmission` aliases SAME WorkBoard.
Its pure CheckDock validates actual world/live target/worker/company and declared compatible
role, requires an actual IWorkDock, and delegates SAME PreviewDock. No ordinary proximity
fallback when a dock is required. Only this node's live job with matching target/company/
worker/order/node may exclude its own existing slot intent. New candidates cannot borrow
another job's exclusion. Provider geometry remains with IWorkDock/selected spatial contracts.

Existing all-expression evaluator, candidate offers, private requirement projection, assignment,
actual job slot intents, inventory receipts, physical service/departure and Sync remain owners.
No new queue/store/system/claim codec/view/clock; readiness is not acquisition. Existing
assignment rechecks the real slot before native resource acquisition; this slice does NOT
prove arbitrary participant cross-provider transactions or add multi-worker atomic teams.

Build2.235.0/save136; all existing layouts/views unchanged. One unchanged GameClock SHA256:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.

## Actual bounded player proof

Native access/inventory/motion/tile navigation/work/knowledge/world-setup modules only. Shared
survey master/world documents plus actual native-dock-work/startup files are selected through
master document references and explicit startup profile. Fixture assembly changes content
selection only. No raw test-spawn/material/progress/reservation mutation, test worldgen or
business/economy provider. Actual engine startup creates one site with20 steel and three
machines; bay/wait/exit offsets are authored relative geometry. Case coordinates and900-Update
failure limits are proof configuration, not engine/world/platform caps.

Normal hold/release/order/cancel/move commands and Game.Update perform all gameplay. Contract
aliases and content-family lookup refer to actual same owners. Pure all-expression diagnostics
and actual role offers admit two unheld workers. Two parallel node jobs acquire distinct bay/
waiting intents without spending stock or earning progress during travel. Retained readiness
remains satisfied, excluding only each actual job's own slot. Foreign company gets empty
private criteria. The third machine's actual offer reports dock-full; aggregate criteria use
the existing ranked busy-worker result. Execution matches aggregate preview and allocates
no overflow job. Cancelled contender acquires no slot.

Saved twins retain actual travelling/queued jobs. First physical bay work consumes20→12;
waiting node earns no overlapping service work. First node physically departs. Its normal
idle-home behavior leaves it occupying an exit; the player's ordinary MoveVehicle command
clears that real obstacle in both saved worlds. Second worker promotes, services and departs;
stock12→4, positive completed machine identities, released slot intents and hashes agree.
No host scheduler or engine special-case auto-clear was introduced. Completed save/load and
interleaved startup preserve independent real stock/claims. Missing actual dock admits valid
activity intent but readiness blocks with dock.missing and allocates no proximity job. Missing
roles/references and selected extra reservation fields refuse before Initialize.

## Remaining gates

Lifecycle ownership review kept provider admission and queue/receipt execution with existing
owners rather than creating a parallel participant ledger. Native all-expression dock readiness
is verified, NOT all/any remote/entity tool teams, arbitrary providers/mutation safety, dock
policies/role combinations, congestion/replanning matrices, inbound batch acquisition,
full accounting/replay/lockstep/fault/stress/soak/CPU/allocation/full regression or full P1–P12
acceptance. All those remain open. No OilFieldDays/host/demo/Draw/platform/business gameplay edits.

The [scoped source manifest](P4_AUTHORED_DOCK_REQUIREMENTS_SOURCE_MANIFEST.json) hashes inherited
relevant source plus actual new paths and checks local Markdown links/unchanged clock. It is
not a whole-repository audit or rerun of historical runtime proofs. Historical reports remain
historical. Dirty unrelated work is preserved.

Final scoped inventory:409 unique paths,74 Markdown documents,1463 local links,0 broken links
and0 current manifest hash mismatches. `git diff --check` exit0; existing LF/CRLF warnings remain.
