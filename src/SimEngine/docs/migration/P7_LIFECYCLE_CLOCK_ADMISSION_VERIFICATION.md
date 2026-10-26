# P7-D12 — Lifecycle clock admission verification

## Current M64 extension — 2026-10-22

Native2.719.0/save161/replay2. [Authoritative current C8 implementation, attempts and limits](P7_LIFECYCLE_COMMIT_VERIFICATION.md#current-m64-operation-admission).
World.RequireCompletedOperations replaces the old internal entity-only name with one entry that
retains entity phases and observes actual frozen ISimulationOperationBoundary declarations.
ResourceTransactions owns the only resource execution depth; no copied flag, duplicate clock,
persisted phase or legacy alias. Actual before/after resource callbacks refuse nine checkpoint/clock
entries, preserve outer permission after a nested coordinator refusal, and continue saved twins
when the caller handles the pure refusal. Propagated errors retain terminal real prefixes.

Latest four literal results2887/0/two actual public replays span explicitly captured build5/build6
tests images with identical final native/proof bytes. See the linked record; do not add historical
D12/M63 counts or claim a final full regression. Inherited contract getter cutoff338/0 is included.
Earlier entity proofs below retain their actual2.624 identity and scope. Original221/210 source-
recorded checked/11open unchanged, full goal ACTIVE. Focused-only including final; no consumer,
business/platform/Draw or staging/commit/push. Broader resource/reference/stress obligations remain.

## Historical D12 initial evidence

## Outcome

Engine **2.624.0/save159/replay1** refuses advancement over unfinished original
entity creation and capability initialization. The existing World lifecycle guard
now reads the original EntityStore mutation flags and original initialization
depth. No second clock, state machine, allocator, persisted field or codec exists.

[Design](../../plans/enhancements/P7_LIFECYCLE_CLOCK_ADMISSION_DESIGN.md).
442 core C# path/content SHA256:
`ee7f8cb6232c256f5f0f6b58ba3f38e7b66458a7b7698c53ac59949a0ecf5fa1`.
Save format and all declared state codec schemas remain unchanged.

## Reproduced engine defect and original-owner repair

Previously World.Step only refused recursively active stepping. Creation started
between updates could call Step/Update/Update(elapsed)/RunTicks from configure,
spatial spawn, OnCreate or a nested initializer. The clock, systems and entity
state advanced over an unfinished creation. An elapsed update advanced the actual
point session from step1 to46 before initialization returned; public update
callbacks and snapshots could escape before completion.

The new 192-context exact case first produced **1768 passed/1160 failed** on the
unmodified2.623 core. It does not merely test a mock guard or fabricated clock.

EntityStore.RequireCompletedLifecycle uses its original _creating/_replacing
flags and is reused by original Sync. World.RequireCompletedEntityLifecycle
replaces the former initialization-only helper, combining that store admission
with the original initialization depth. World.Step calls it before time work;
SimGame.UpdatePhase calls it before controllers, pacing or publication. Existing
save/load/hash entry points reuse the same guard before payload work.

An already faulted game still uses its original stopped/retained diagnostic
publication; it cannot advance and must not be rejected by an interrupted
replacement flag before those diagnostics. UpdatePhase therefore applies the
new admission only while healthy. Existing recursive active-update semantics
remain unchanged. No clock pumping code or physical work rates changed.

## Actual proof scope

The new case spans fresh/restored sessions, direct/prepared parents, six actual
configure/spawn/OnCreate/parent-before/child/parent-after callback stages, four
advancement APIs and handled/propagated errors: 192 independent contexts.
It checks actual parent incompleteness, sole clock coordinate, whole/fractional
pacing budget, Updated callback count, exact previous snapshot object, original
callback cutoff/fault phase/exception identity, completed parent and real child,
saved twin continuation and a separate saved session that actually advances.

Valid nested creation is still permitted; only premature advancement is refused.
Handled pure refusal leaves the actual parent healthy. Propagated refusal goes
through the existing entity.create or entity.initialize fault owner. The explicit
protocol test calls module lifecycle APIs; it is not player gameplay in a host.

Original allocation and initialization-checkpoint cases independently verify
distinct child identities and released guards. The adjacent replacement case
verifies stopped faulted updates, retained auxiliary state, geometry/stock
conservation, supplier claims and native history integrity.

## Adjacent stale fixture repaired, not a finance dependency

The selected replacement case initially stopped at a supplier definition whose
scenario did not select sim.tycoon, before reaching its faulted-update checks.
Its tile leg now uses the sole PhysicalRecoveryTestContent fixture with an
explicit supplier option. That option alone retains the authored supplier,
selects sim.tycoon and master-lists the existing game-mode-properties.json with
all business features disabled. Default physical/cargo callers still remove the
market and activate no business. A dedicated assertion proves no accounts exist.

Actual placement/initial stock comes from the existing world-setup/native entity
owners; the extra expanded prototype is content, not completed test-side state.
The supplier-only protocol subcheck explicitly spawns its actual supplier through
the existing lifecycle, not a gameplay/startup fallback. No Tycoon production
source, default price, opening money, Oil source or business rule changed.

The same old fault case also expected every error to be entity.replace and read
the public history after a terminal fault. Current owners correctly retain first
faults and refuse live views. It now checks exact phase per actual source owner
(resource.transaction, observation.provider, or entity.replace), compares the
original journal's complete codec before/after as explicit protocol evidence,
requires the public read refusal and checks no pending success events. This is
stronger accounting evidence, not deleting the original no-success assertion.

## All focused runtime attempts

Each command used `--bounded-exact` with the literal registered name,
`--timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 4500`.

| Attempt | Result | Process evidence |
|---------|--------|------------------|
| New clock-admission baseline |1768/1160 |34748 exit1;1302ms suite/1451ms process; stdout deliberately bounded/truncated |
| First repair clock / initialization / allocation |2928/0,3288/0,612/0 |34276/30828/5768 exit0;1117/1444/519ms suite;1263/1594/670ms process |
| Healthy-only final core admission: clock / initialization / allocation |2928/0,3288/0,612/0 |1624/34564/3448 exit0;1248/1535/560ms suite;1399/1684/715ms process |
| Adjacent replacement before fixture correction |35/1 |18544 exit1;525/670ms; inactive supplier provider |
| Supplier selected, authored property document not yet listed |35/1 |23872 exit1;726/913ms; missing explicit business Boolean |
| Authored supplier composition corrected |52/2 |31304 exit1;592/748ms; old first-fault expectation and live faulted history read |
| History integrity and first-fault migration |118/2 |30124 exit1;648/801ms; two exact phase expectations corrected against actual original owners |
| Final adjacent replacement |120/0 |14776 exit0;708/867ms |
| Unchanged default physical response composition |93/0 |18488 exit0;1218/1363ms |

Final selected current-core evidence totals **7041 assertions/zero failures**
(2928+3288+612+120+93), not a full regression result. The three core guard cases
ran after the last production edit; subsequent edits touched only the adjacent
replacement test/shared fixture. The two final fixture cases ran after those
test edits. Independent commands were parallelized; no family/profile filter.
Except the explicitly bounded failing baseline, output was untruncated. All
processes terminated with their recorded status; no aborted or live handle.

Targeted compilation command throughout:

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
```

Eight attempts, in order: clean5.39s baseline; clean6.56s first repair;
clean6.58s healthy-only admission; failed6.69s (one test missing System.Linq);
clean9.06s (session44804 polled to exit0); clean6.27s property document;
clean6.44s history migration; clean5.09s final phase expectations.
Every clean build had zero warnings/errors. Builds compile dependencies, not run
their suites. No full/profile/family/Oil/demo-horizon runtime was executed.

## Remaining scope

P7.acceptance.017 remains open: this does not prove all replacement preparation,
provider callbacks, arbitrary mod external state, new topology endpoints, custom
accounting policies, or broad fault/stress matrices. No new original checkbox
closes: 221 requirements/204 recorded checked/17 open; not global revalidation.
Original P6 ownership records are historical source evidence, not a claim all
runtime inputs stayed unchanged after this core repair.

Lifecycle guidance kept this repair in one documented stage using existing owners.
No compatibility shim, fallback world or parallel scheduler, Draw/platform/adapter,
Git staging/commit/push or memory edits. The full goal stays active.

Final metadata verification matched all221 original requirement IDs/literals:
204 recorded checked/17 open, no missing mapping. New design/report links resolve;
git diff --check exits zero with35 existing line-ending warning lines, zero staged
paths. No executable input changed after its applicable final focused proof.
