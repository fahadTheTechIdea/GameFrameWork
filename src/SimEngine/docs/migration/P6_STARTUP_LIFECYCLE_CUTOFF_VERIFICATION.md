# P6-SL1 — First-fault startup lifecycle verification

2026-10-16. Engine2.638.0/save159/replay1.
[Design](../../plans/enhancements/P6_STARTUP_LIFECYCLE_CUTOFF_DESIGN.md).
This is focused lifecycle/composition evidence, not full regression or broad P6 completion.

## Demonstrated failure and engine implementation

On the original2.637 core, an independent initialization callback catches a real entity
creation failure. World has already latched entity.create, but one later Initialize and
two generation-stage callbacks still execute. The new exact baseline reports2 passed/2
failed and an explicit count3 later-callback diagnostic. The factory eventually throws;
that late refusal does not excuse executing callbacks after the original first fault.

The sole World lifecycle now uses one private InvokeStartup boundary:

- Require original world health before and after each callback.
- Latch a direct callback exception only if no earlier fault exists, then rethrow it.
- System initialization/new-game phases name their captured system metadata IDs; generation
  uses world.generate, without another live provider identity getter.
- Preserve original initialization flag and ordering, generation permission try/finally,
  generation stages, OnNewGame, entity flush, event dispatch and spatial invalidation owners.
- InitializeSystems checks health before its once-only early return; failed partial startup
  cannot silently skip initialization and proceed.

No new public contract, state owner, startup scheduler, clock or rollback. Build identity
advances because failure admission changes; save159/replay1 and persisted layouts do not.
The original entity.create first cause is retained for caught nested failures. Direct
exceptions retain their original type/message and receive the actual startup phase.

## Independent authored proof

A master-listed startup-lifecycle.json authors the diagnostic profile and a deliberately
broken entity using the existing CreationProbe. The independent module only instruments
original callbacks; session counters are transient diagnostics, never saved gameplay or
decision state. Existing PointGame/WorldSetup, resource/worker/motion/logistics owners do
all positive game work. No driver spawns or simulates entities or implements startup rules.

Six failure variants cover direct and caught-create errors in Initialize, generation and
OnNewGame. The requested factory creation fails, no later startup callback executes, the
original first cause/phase remains, generation permission releases, and a terminal world
cannot be reused or saved as healthy.

Positive startup invokes each diagnostic phase exactly once. Normal public hauling moves
four real units from source20 to destination4/source16 over simulation time. Save/load
initializes restored systems once without invoking generation or OnNewGame, continues the
actual in-flight job to identical state, and verifies the original recorded replay. Normal
Update never reruns startup. No optional Tycoon, renderer or Oil dependency.

## Current final exact results

| Exact case | Checks | Child PID | Suite / process ms |
|---|---:|---:|---:|
| Startup lifecycle: first faults stop initialization generation new-game callbacks and preserve real saved work | 64/0 | 32940 | 355/483 |
| Startup composition: selected declarations refuse before Initialize and preserve physical saved work | 92/0 | 33092 | 513/640 |
| Composition acceptance: ownership dependency and order refusals precede Initialize and valid physical saved execution | 250/0 | 19320 | 5244/5415 |

**406 passed, zero failed across three distinct exact cases.** Each uses outer60000ms,
termination5000ms and output7500–8000 chars; all terminate exit0 without truncation, timeout
or abort. No full/profile/family run. New lifecycle and composition acceptance were verified
before a test-only stale protocol assertion correction; core/probe/their case bodies stayed
unchanged. Only the affected startup-composition case was rerun after that one-line correction.

## Every runtime/build attempt

| Attempt | Core | Result | PID | Suite / process ms |
|---|---|---:|---:|---:|
| New lifecycle baseline | 2.637 | 2/2 | 17576 | 151/278 |
| New lifecycle after engine repair | 2.638 | 64/0 | 32940 | 355/483 |
| Existing startup composition first | 2.638 | 91/1 | 19880 | 686/851 |
| Existing composition acceptance | 2.638 | 250/0 | 19320 | 5244/5415 |
| Existing startup composition final | 2.638 | 92/0 | 33092 | 513/640 |

The91/1 result is an old literal save154 expectation; the actual established protocol is159.
It now references StateCodecTests.ExpectedSaveFormat, the independently authored protocol
baseline, not the engine's own value as its expected value. No production layout or behavior
is changed to satisfy it. The two existing first attempts overlap in wall time, so their
timings are not controlled benchmark measurements.

Only targeted Release builds of tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj with
--no-restore -v minimal: four attempts. First fails4.75s with three test-authoring API errors
(JsonValue.Null is a method; no InventoryResourceSnapshot/ReadInventory API). The test uses
actual JsonValue.Null() and LogisticsView.ReadDiagnostics instead. Subsequent builds are
clean/zero warnings/errors:4.32s,6.57s(engine repair),5.13s(final protocol assertion). No stale
binary is launched after the failed compile.

## Current-source structural audits

Existing VerifyExecutionOwners.ps1 and VerifyCapabilityConsumers.ps1 are read-only scans,
not runtime profiles. Both first run on2.637 and finally on2.638:443 production sources,
9 reviewed owner groups/11 ownership witnesses, zero execution errors,1528 typed query sites
and zero native-runtime query/cast violations. Final PowerShell success is true for each.
The first wrapper reports null LASTEXITCODE because successful PowerShell scripts need not
set that native-process variable; final results capture the actual PowerShell success flag.
Do not relabel null as a failed or zero native exit code. Initial exec93765 is polled to its
actual terminal exit0; no restart from an observation timeout.

These structural results agree with the selected lifecycle/composition behavior, not every
possible module/provider, original assertion, client workflow or full P6.010 acceptance.
No audit verifier is weakened or rewritten, and no source ownership ledger is fabricated.

## Final identities and remaining scope

- Core443 sorted C# path/content SHA256:
  `983fd5c2c626b7b002e23cd9a87660fd71473a77f93669dcdfd42953a822d0c4`.
- Release engine DLL:
  `E587479863CEE1ED162F631D837107C01FB7BF59E856A294D84E519A7AB20B5F`.
- Final Release test DLL:
  `D001E4B31F529E6A715E663E629D8651B4409E28424CF75FBBE3648C61B2D6F5`.
- Release independent extension DLL:
  `87D567F5619BE7ECD45DC7EBE4F6E5E702B171B4142E5DD720F50FAD86E10232`.

Original221 literal requirement IDs/texts still map to208 recorded checked/13 open. No
original checkbox or requirement wording changes. P6.007/.010/full goal remain open; stage
fault cutoffs do not prove arbitrary provider purity, all client/manifest workflows, every
optional module, full profiles, stress/soak or all restoration failure combinations.
No Oil/business/platform/Draw source, legacy path, new clock, staging/commit/push.

Final hygiene: diff-check exit0 with35 existing line-ending conversion warnings/no other
diagnostics; explicit five changed/new slice-file whitespace scan clean. One new exact
registration/one embedded document explicitly master-listed in its fixture. Staged paths0;
no matching test/build process remains live. Final changes after runtime verification are
documentation only.
