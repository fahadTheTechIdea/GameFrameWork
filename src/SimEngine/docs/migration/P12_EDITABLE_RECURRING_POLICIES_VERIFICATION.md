# P12-D3b editable recurring policies — focused verification

Date: 2026-10-06. Runtime: Beep.SimEngine only.
Build **beep-simengine-2.152.0**, save **80**; native recurring capability3, public proof2,
WorkSystem15, WorkView43. WorkBoard24, operating areas1/native work policy2, logistics/motion and
GameClock layouts remain unchanged. [Stage](../../plans/enhancements/P12_EDITABLE_RECURRING_POLICIES_DESIGN.md).

## One responsibility, one game clock

RecurringPlanSettings replaces separate enabled/priority state and immutable-only cadence controls.
IRecurringPlanDefinition.Initial supplies explicit JSON intent; IRecurringPlanSource owns actual
settings and existing last successful submission/plan references. Command and private immutable
preview share the same checks. The old configure/preview signature and definition getters are deleted.
Native/public codecs embed the shared exact GameDuration value codec; no legacy reader or fallback.

The existing WorkSystem/WorkBoard remains the only recurring admission/execution path. Current
interval resolves against the scenario's selected quantum; invalid/unaligned durations refuse without
rounding. Due time derives from the original successful posting coordinate. Edits, disabled time,
rejected triggers and active-order refusals do not rebase it. Initial eligible posting stays immediate.
Overflow is a typed blocker; a shorter representable interval can recover without rewriting history.
DueGameTime is a derived value, not another clock. A source priority change does not rewrite admitted
plan priority, jobs, reservations, tools, consumed stock or physical progress.

World constructs GameClock once at Kernel/World.cs:84. World.Tick is only `Clock.StepIndex` at line99.
GameClock source fingerprint remains unchanged from the preceding stage:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
There is no added current-time field, independent timer, update loop or scheduling system.

## Commands and final evidence

```
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Recurring plans: authored schedules, controls and checkpoint ownership"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Recurring plans: physical inspection, maintenance and saved continuation"
```

- Final clean build: **0 warnings, 0 errors**, MSBuild **2.51 s**.
- Contracts/controls/checkpoint ownership: **191 passed, 0 failed**, **597 ms**.
- Physical inspection/maintenance/saved continuation: **133 passed, 0 failed**, **1399 ms**.
- **324 passed, 0 failed, 0 aborted suites**. Only these same two exact names were run this stage.
- Existing recurring methods are included in these counts, not additive historical coverage.
- Scoped source/document whitespace checks and recurring-stage local links pass; Git reports only
  normal line-ending notices. No source changed after the final build and two gates.
- 62 preceding build assertions and59 save assertions in66 source files migrate to current identity.
  RouteOperatingAreaTests' WorkSystem schema assertion migrates14→15. Those suites compile only,
  including the migrated OperationPolicyTests callers; no claim that their behavior was rerun.

Contracts cover native/public providers, immutable initial/current snapshots, private/pure preview
and command parity, fractional periods, null/negative/zero settings, quantum alignment, range overflow,
pending-command save, paused sole-clock updates, twin continuation and original-history retention.
Native malformed settings/truncated history decode stages before commit. Structurally valid but
session-unaligned source decode is refused at the existing whole-session checkpoint/load boundary.

Provider faults include ignored interval edits, equivalent-but-not-exact duration representations,
changed history and changed settings during successful-post acknowledgement. Exact commit guards fault
normal Update before the clock completes; faulted sessions cannot save a partial checkpoint. These are
scoped provider guards, not proof of arbitrary global callback rollback/purity.

Physical cases use native and independently authored public sources, actual machine travel/startup,
spares/tools and positive work. Edits during paused live work preserve its original plan/progress and
spent stock; disable only suppresses future posting. Cancellation retains original posting history;
reenabling waits for the edited real deadline before admitting another plan with its new priority.
The second physical inspection completes while future postings are disabled. Saved interleaved twins
and isolated initial sessions retain exact hashes, settings and stock. An explicitly master-listed
30-second time definition repeats the native physical case at two updates per game minute. All
physical waits are bounded by the existing700-update diagnostic helper.

## Same-gate repairs

An intermediate run found two test assertions expecting pending-intent Duplicate for an already
active native order. Existing ordinary admission correctly returns AlreadyDone first; only assertions
changed, not WorkBoard semantics. A later additional-quantum fixture used `simulation_times` instead
of the authoritative `time_definitions` section and aborted setup. Only that fixture changed; no
legacy parser alias was added. Both exact suites were rebuilt and rerun to the final zero-failure gate.

## Source fingerprints (SHA-256, relative to framework root)

```
62706D1D214E7A3805C428E59AA3AD34BBAD3F1F74041BE963188130D77181ED src/Beep.SimEngine/Standard/RecurringPlans.cs
5BAA810875F5A1E5E8AB6276CCE14BB1CDD771B31A3209E26F36B39D7E1A1426 src/Beep.SimEngine/Standard/WorkRecurringPlans.cs
1E780A0A1C31F79D193192DA66B983C3804FBA00B7C5F6CF56FFAA6B1B8C1102 src/Beep.SimEngine/Commands/RecurringPlanCommands.cs
DF0EE4D96A435A3D77346E2C007B915F497E2794986F4B5A15C2687C2FC0C9BA tests/Beep.SimEngine.Tests/RecurringPlanTests.cs
233FB411B05036C1D7CC47F0AE8BEA5EA6613243B4C3486E02BF747D2F82DECA tests/ExtensionProof/RecurringPlanProof.cs
```

## Open scope

Earlier area/haul/rate/operation gates were not rerun. No third suite, broad family filter, full test
run, Oil/Tycoon/host/demo/platform/Draw work was performed. Full fleet subject/service-dock orchestration,
preparedness/reserve policies, inspection discovery science, contamination/incident breadth, preferred
routes, traffic/congestion/fuel forecasting, broader provider/replay/stress/fuzz/allocation and full
P9/P12/P1–P12 acceptance remain open. This focused slice does not establish a green full baseline or
completion of the framework goal.
