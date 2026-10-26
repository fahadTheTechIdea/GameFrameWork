# P4-H2 — Budget lifecycle and provider execution integrity

## 0. Problem

At the preceding2.214 checkpoint WorkStart trusted budget-provider statuses without verifying whole receipt/account
effects. A mod can report committed while keeping holds or changing another owner's money.
Native debt-authorized work, broken-route release and checkpoint corruption are also unproven.
Continue H2 through the same existing account, WorkBoard, resource, progress and GameClock owners.

## 1. Goals

1. Budget checks are pure; native/provider inspection has a stable declared revision.
2. Whole-set reserve changes only the exact holds, never cash. Spend removes only its receipts
   and debits their total once. Release changes no cash and removes only requested holds.
3. Invalid preflight statuses or false commit facts fault rather than become a success or fallback.
4. Actual route interruption releases unpaid work holds; reopening acquires fresh receipts.
5. Authored timed debt work can underfund holds; actual resource generation income funds resumption.
6. Saved held/blocked/funded progress continues deterministically; named corruption refuses restore.

## 2. Design

Extend IMonetaryBudgets with its existing account revision projection. Native BusinessAccounts
exposes the already-saved hold revision; no new persistent field/store. Add one stateless execution
boundary that captures transient detached receipt/balance facts and verifies contract calls. This
is protocol integrity, not another financial policy, live account, budget ledger or scheduler.
Whole cash vectors cover all actual companies. Native financial rules remain solely in Tycoon.
WorkStart uses this boundary for check/acquire/spend/release and loaded receipt validation. Pure
inspection verifies declared revision and balances; work-scoped orphan detection stays WorkBoard's.

Use new explicit master-listed lifecycle JSON for a timed debt-authorized running-cost activity
and primary-generation funding recipe. Existing native machines travel/build; a player command
closes/reopens actual point-space access. Tests do not charge/credit/allocate/progress work themselves.
Named provider-fault proxies forward the real native service; they own no balance/hold store and
exist only for direct boundary instrumentation, not ordinary game execution. Keep these proof scopes
distinct. Named corrupted node bindings test actual saved restore refusal, then restore original facts.

## 3. API

IMonetaryBudgets.Revision projects the owner revision. Current schema identity changes directly;
no old reader/overload. Existing immutable views remain the diagnostics authority. No player
money command, economic module, rendering, platform, adapter or host/gameplay API is introduced.

## 4. Files

Core/MonetaryBudgets; Standard/MonetaryBudgetExecution and WorkStart;
Standard/Tycoon/BusinessBudgets; current engine identity; lifecycle JSON/embedded declarations;
existing budget fixture factory/new exact scenario; tracker/readme/AGENTS and scoped evidence.

## 5. Errors

NoFunds remains normal cash wait. RangeExceeded has a typed budget admission blocker.
Invalid active receipt is a contract violation, not an invented cash shortage. Mutation during
check, invalid status, false receipt/cash commit or unrelated hold change faults deterministically.
Saved orphan/incorrect references refuse current restore, never reconstruct a replacement hold.

## 6. Compatibility

Direct current engine/save identities; physical/account payloads unchanged. No compatibility path.

## 7. Verification

One completed-slice build plus ONE exact new bounded scenario: route interruption/reassignment,
actual timed debt/income-funded work, saved twins, named orphan/receipt corruption and labeled
provider check/commit violations. No older/full/family suite. Record every actual invocation.

## 8. Risks

Transient captured vectors are validation facts, not balances used to execute gameplay. Do not
pretend a false provider commit can be rolled back. Direct proxy probes are not normal Update
fault/recovery proofs. Account views stay company-private. Debt remains explicitly authored.

## 9. Non-goals

Advanced markets, haul-reserved-money policy, all custom economic provider/replay/stress proofs,
full H2/framework acceptance, platforms/demos or OilFieldDays. Commit/push only after the FULL
goal and plan are finished and verified, as requested; no per-slice publication.

## 10. Execution

- [x] Implement transient budget protocol validation and reuse it from work.
- [x] Author lifecycle JSON and bounded normal player workflows with saved continuation.
- [x] Add labeled provider/corruption probes; build and run only the exact new scenario.
- [x] Record evidence and update master; keep remaining roadmap and publication gates open.

Focused checkpoint2.215.0/save119:51 checks0 failures/aborts571ms; final clean build4.17s.
Three builds/two scenario invocations including one probe constructor repair and a real retry
timestamp defect. Assignment now consumes elapsed backoff. Payloads unchanged;93 older metadata
files/151 identity lines compile only. [Evidence](../../docs/migration/P4_WORK_BUDGET_LIFECYCLE_VERIFICATION.md).
Only this document's narrow execution scope is complete, not H2 or the full master plan/goal.
