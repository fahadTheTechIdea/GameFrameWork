# P10-B7e — complete stock-band controls

Current M147/native2.772: shared stock declaration executes minimum/maximum/desired
through the same normal owner and preserves independently edited fields. The new coupled
reserve/stock/physical-haul proof350/0 is paired with visibility399/0 on its explicitly
qualified later test image. [Actual evidence and limits](../../docs/migration/P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md#current-m147-reusable-resource-controls-and-detached-quantities--2026-10-23).
Historical broad stock-band gates below are not newly rerun.

Current replacement follow-up M52/core2.713/save161/replay2: StockControlActionPolicyDef delegates pure replacement prediction and actual prepared target binding to required ILogisticsStockPolicies contracts. The original logistics owner preserves native, pinned-independent and explicit fresh bindings without reading/recreating demand rows or assuming callback order. Normal commands still own all minimum/desired/maximum validation and physical refill; no new controller/state/clock. [Current scoped physical evidence and limits](../../docs/migration/P6_NETWORKED_MOD_DURABILITY_VERIFICATION.md#current-m52-independent-automation-stock-targets--2026-10-22). Original B7e measurements below remain historical, not current-image qualification of all band paths.

Status: native focused slice implemented and verified, 2026-10-06. Build2.139.0/save76,
automation4/operations view8/logistics view15. Continues P10/P1-P12, not completion.
[Evidence](../../docs/migration/P10_STOCK_BAND_CONTROLS_VERIFICATION.md).

## 0. Problem

Before B7e, ResourceControlActions.cs and LogisticsDemandChange contained desired-only stock
partial intent/providers. Minimum/maximum automation was unavailable. Adding separate
providers or commands for each band would duplicate normal stock admission and target ownership.

## 1. Goals

1. Replace desired-only intent with one explicit typed LogisticsStockLevel (minimum/desired/maximum)
   and immutable single-level change in the existing normal ConfigureStockPolicyCommand.
2. Replace StockTargetControlActionPolicyDef/stock_target_control_actions with one
   StockControlActionPolicyDef/stock_control_actions. JSON requires explicit level; no old section,
   class, factory, enum member or inferred desired default remains.
3. Sole LogisticsService resolves the selected level against actual current settings, preserving
   the other two levels, priority, row age and live physical commitments. Reject inverted bands,
   nonnative quantities and individual/combined capacity without clamping or partial mutations.
4. All level controls on one resource claim the same stock-policy binding, preventing conflicting
   partial writers; different resources stay independent. Existing guarded private discovery applies.
5. Existing automation settings/history/dwell/cooldown/deferred normal handlers and sole GameClock
   apply to every band. Level edits can trigger real timed refill or prevent new claims, never instant
   supply, cargo deletion or another controller. Save/load/hashed previews use the same owners.

## 2. API and files

Standard/LogisticsPolicies.cs: LogisticsStockLevel enum; LogisticsDemandChange.ChangeKind.Level and
Level(LogisticsStockLevel, Fix, bool), StockLevel property; embedded intent codec includes selected
level. Remove Desired factory/member. Replacement intent remains explicit complete authored settings.
Standard/LogisticsStockPolicies.cs: existing AdmitStockPolicy selects the changed field and constructs
ordered LogisticsDemandSettings; existing canonical native/capacity/aggregate checks remain owners.
Standard/ResourceControlActions.cs: replace desired-only class with one stock class requiring level
in Read and constructing normal typed Level intent. Shared native resource/unit linker is unchanged.
Standard/AutomationModule.cs: only new stock_control_actions section. Migrate explicitly master-listed
stock JSON and current source/test callers. Kernel/SaveGame.cs: strict breaking identity for payload
and content changes. No compatibility or legacy aliases; development-only schema transition.

## 3. Validation and physical semantics

Missing/unknown JSON levels fail composition. Missing actual stock row yields existing typed blocker.
One selected level must preserve minimum <= desired <= maximum; incompatible later player edits
refuse at actual deferred commit while retaining configured intent/history and physical jobs. Maximum
uses actual combined native receiving capacity. A lower maximum does not revoke acquired receipts;
existing ReceivingCeiling bounds subsequent claims. Minimum changes use the existing refill latch.
Action enabled remains explicit whole-policy enabled intent; priority is preserved, not fabricated.

## 4. Verification

Clean build then exactly these two complete suites, same names for repair reruns:

- Automation: threshold and recovery contracts, bindings and private controls
- Automation: threshold edits, recovery, deferred timing and saved processes

Prove all level kinds, invalid enums/codecs/content, preview/command parity, preserved unrelated bands/
priority/age, same-resource conflicts, private/native/public discovery, later bounds/capacity refusal,
physical refill/max ceilings/in-flight edits, saved normal inbox and automation phases/twins.
Other suites compile only; no third/family/full gate. Preserve current dirty worktree.

## 5. Execution

- [x] Replace desired-only contract/content/provider and migrate every active caller.
- [x] Add native/public level admission, private/conflict, physical and saved proofs.
- [x] Clean build, two exact suites, review and evidence/master/README/help updates.

Clean build, zero warnings/errors;1069 checks/two exact complete suites (622 contracts;447 execution),
zero final failures/aborts. Includes B5/B6/B6b/B7a/B7b/B7c/B7d, not additive. Other suites compile only.
Explicitly embedded/master-listed stock-band-work.json posts actual work demand above a lowered
ceiling; raising the maximum admits only available room through real hauling. No host request logic.

Utilities/backpressure, broader provider/global rollback/replay/lockstep/fuzz/soak/allocations and full
framework acceptance remain open. No Oil/Tycoon/hosts/demo/platform/Draw. The lifecycle skill keeps
the source change and focused proof in one tracked stage.
