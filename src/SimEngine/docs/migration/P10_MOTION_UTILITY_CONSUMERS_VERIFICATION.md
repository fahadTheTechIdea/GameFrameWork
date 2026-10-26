# P10-A8 — Motion utility consumer verification

2026-10-06. Framework-only focused evidence; full P10/P1–P12 are not complete.

## Authoritative owners

Build **beep-simengine-2.144.0**, save **76**; native/public motion-input component semantic schema
**2**, motion view **5**. Exposure payload remains two longs. Movement5/work13/operations view9/
utility signal1 and existing resource reservation/transaction codecs remain unchanged.

`IMotionOperatingInputs` now extends `IUtilityConsumer`. Native and public-only providers expose
their actual existing IInventory. One `MotionOperatingContracts.ReadDemand` calculation resolves
actual authored rates, native quantities, ceiling prepayment and accepted-attempt exposure against
the sole session GameClock. `MotionUtilitySnapshot` embeds generic UtilityDemandSnapshot instead of
duplicating its bills/identity. The removed MotionOperatingSnapshot has no alias or compatibility path.

Existing UtilityContracts guards actual named owner/endpoint/version/company/native quantities and
unpromised availability. Existing private OperationsView and explicit master-listed utility signals
read those facts with no motion-specific view/signal coordinator switch. Motion admission additionally
checks reported demand against actual authored rates/exposure before its aggregate transaction;
an independent provider cannot omit a required input or forge draw intent/consumption/progress to
bypass physical movement admission. Generic utility providers remain trusted engine-side contracts,
not a hostile-code sandbox or general rollback guarantee.

There is no new fuel balance, consumption journal, scheduler, reservation owner, codec, clock or loop.
The actual existing resource transaction still commits one whole draw, then verifies each provider's
exact exposure change before actual motion. Every outlet/inlet invariant and timed physical delivery
remain unchanged. Idle, disabled, zero-rate, shortage and inactive unreachable waits do not burn fuel.
One accepted failed route attempt can burn authored attempt inputs without earning distance.

Motion `Drawing` describes next-attempt input readiness, not current motion or enabled control.
It remains prospective while idle or disabled. Explicit empty rates are inactive utility facts but
permit non-consuming motion. Prepaid intervals can have empty next bills while still being ready.
Per-provider satisfaction is not proof of combined shared-stock admission. Generic demand never
claims that incoming promises or traveling cargo are already usable physical fuel.

## Completed gate

`dotnet build Beep.SimEngine.slnx --no-restore` completed before tests: **0 warnings,0 errors**,
MSBuild **3.46s**. Only these two complete exact existing suites ran:

| Exact suite | Passed | Failed | Reported time |
| --- | ---: | ---: | ---: |
| Motion operating inputs: authored contracts and whole resource admission | 96 | 0 | 419ms |
| Motion operating inputs: timed expenditure, physical resupply and saved continuation | 73 | 0 | 652ms |

**169 checks, two suites, zero failures/aborts.** No third, family filter or full regression.
A7 requirement291/A6 network182/A5 running146/B8 resource447/B7e automation1069 were not rerun;
other suites compile only. Current build assertions migrate mechanically, not gameplay behavior.

Proof covers native/public actual consumer identity/store, generic discovery/private views, immutable
old facts, exact authored JSON signals, prospective idle readiness, native promises, combined draws,
typed shortage/range/inactive, native count/liquid quantities, prepayment, pure and forged provider
faults, staged codec faults, real courier loading/travel/unloading/refuel, retained motion goals,
actual journal conservation, resumed saved twins and session isolation. Each physical wait is bounded
to200 game update steps with diagnostic failure state. Pausing the sole GameClock leaves age, hash,
stock and local exposure unchanged; observation and delivery earn no propulsion consumption.

## SHA256 fingerprints

| Source | SHA256 |
| --- | --- |
| Standard/MotionOperatingInputs.cs | E0863DE0C56C0B20493079A665C10674055AC7D156406F53D5826A38D6F2E988 |
| Standard/MotionModule.cs | CFB0393D0D56484C06F91AFEF0CB64B6079A48D45BB9AA242D15B0E6D1E336A4 |
| ExtensionProof/MotionOperatingProof.cs | 84E4C23E767FD9A1D3F70CF6C228E45DCA933985A78EFE881F7A946A42F81289 |
| Beep.SimEngine.Tests/MotionOperatingTests.cs | 505626F622E608C54A4338989363A5DD3C92550633A7DEB47BDF2D651B65A6BE |
| ExtensionProof/content/motion-utility-signals.json | 3073F8109A1ADA99B4838622A33BDCFA86E149952EDF72B5922D20279B48B5A3 |
| Hosting/GameClock.cs | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |

GameClock source is unchanged from A7. Exactly one `new GameClock` remains in World; Tick and
StepIndex are its coordinates, not independent current-time fields. Save/load retains actual
accepted work/last-success coordinates through their existing owners.

## Still open

Broader generic worker/work/incident utility consumers; full network/activity motion integration
acceptance; fleet auto-refuel policies; distance/idle consumption models; electrical/thermal/pressure
physics; global utility priorities; allocation budgets; general rollback/fuzz/replay/lockstep/soak;
full P10 and P1–P12. No OilFieldDays/Tycoon/host/demo/platform/Draw work in this slice.

[Stage](../../plans/enhancements/P10_MOTION_UTILITY_CONSUMERS_DESIGN.md) ·
[Master tracker](../../plans/enhancements/MASTER_TRACKER.md)
