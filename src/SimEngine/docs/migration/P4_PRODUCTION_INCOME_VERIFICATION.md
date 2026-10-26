# P4-H2 — Optional committed production income

## Current M144 direct valuation and income contracts — 2026-10-23

Native2.769.0/save161/replay2, master cb61483ab81404c11944931aedb4703ba694edb5
plus retained dirty work. Previous implementation made progress; the intervening TileMap
clarification made no implementation progress. Current source and retained executable
evidence were inspected before continuing this handoff. Goal and original gates stay open.

### Implementation and ownership

TycoonModule selects IBusinessValuePolicyDefinition and IProductionIncomePolicyDefinition;
the mandatory BusinessValuePolicyDef and ProductionIncomePolicyDef bases are deleted,
without aliases. Fixed catalogs enumerate IFixedResourceValueDefinition instead of a
native entry class. Ordinary native registrations remain implementations, not mod bases.
FixedResourceValueDeclaration owns entry reading/linking, FixedResourceValuesDeclaration
owns catalog validation and the original quote calculation, and
PrimaryProductionIncomeDeclaration owns strict cause reading and primary-only evaluation.
Independent non-friend JSON definitions delegate to these SAME owners. Common Def remains
the content lifecycle; this is not a second catalog, policy runtime or financial owner.

Original exact fixed-point multiplication/division, precision/range refusal, explicit zero,
unit/currency validation, duplicate/missing prices and consumed-input exclusion remain.
BusinessAccounts, ResourceTransactions, budgets and settlement algorithms are unchanged.
There are no authored price, cause, amount, default mode, state codec, work duration or
clock changes. Build identity advances2.768→2.769; saves remain strictly build-bound.
No gameplay is implemented in the proof, host, demo, renderer or platform.

### Executed scope

Artifacts: [M144 evidence](../../artifacts/optional-module-durability/4622d193058942df94ce7a36029b65f0/).
Baseline original native income case on2.768:101/0,371ms, observer514ms, exit0.
One targeted Release test-project build:0 warnings/errors,17.49s, observer17844ms, exit0.
Exactly two final literal cases on that tested2.769 image, each bounded60000ms with
termination5000ms and output50000 characters:

- `Business policies: direct JSON valuation entries and income contracts retain native timed settlement`:
  54/0,324ms, observer458ms, PID3052, exit0.
- `Tycoon income: committed primary production and independent physical generation`:
  101/0,340ms, observer477ms, PID19632, exit0.

Final155/0, three actual case invocations including the separate baseline. No timeouts,
truncation, aborted suite, restart, full/profile/family/soak execution. The new case uses
the existing master/producer fixture and original physical execution; it checks independent
policy/value entries, pure quotes, primary generation300000 from three authored100000
outputs, recycling/cancellation non-reward, partial/settled saved twins, isolation,
income-off/module-off generation and seven malformed metadata variants. One complete
recording verifies0→15,5 commands,15 checkpoints, live/verified hash7EA83A602A74D4BA.
This is not every financial provider, full accounting matrix or all-module durability.

Actual OilCo.PublicProof target Release compile after the native migration exits0,
0 warnings/errors, observer2624ms. NO Oil source edit or consumer runtime. Its three
rebuilt/copied binaries are compile-only outputs after the tested input capture.

### Audit and documentation handoff

Fresh audit:450 native source/PDB checksums,138 typed calls,14 primitive state writes,8 layer
writes, zero unknown/unmapped/indirect. Typed caller/type/family/target identities match
2.768 exactly; this is not a global financial-behavior certificate. Native DLL SHA256
D5B6DB487EA51E1EB831CAF60D2EDECBC11D31ECB5BA13C57853E5801E36A786;
PDB DDD603CE27B03CF7336A501A13A52093878A774A8C8114FA5D39082A4342CD33.
Explicit profile metadata1101 registered/1080 included/21 excluded/zero runtime bodies.
Only the new separately owned economic literal row is added; removing it reproduces
the exact prior profile bytes. No old exclusion, timeout or financial JSON is rewritten.

SELF-REVIEW: sole declaration/evaluator/ledger/resource/clock ownership is retained.
First-fault, command results, journal and notifications reuse the existing
[operational mapping](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage);
no parallel logger, monetary event listener or notification owner is added. Independent
review and arbitrary provider mutation/fault breadth are not claimed. Existing designs,
composition evidence, resource census and global banners are reconciled in place;
historical reports below retain their actual image and are not new-runtime passes.
Original221 requirements/210 source-recorded checked/11 open remain unchanged. Next work
must address those actual remaining physical/accounting/provider/durability obligations,
not repeat the resolved native-definition obstruction or run a prohibited full gate.
No stage, commit or push before the entire goal and plan finish.

Final preservation check freezes2262 inputs:16 changes versus the before-capture, including
the new external metadata source. Only three Oil compile-only binaries differ after the
tested capture; zero unexpected post-test source/runtime-image drift. Audit DLL/PDB hashes
still match. Four native C# changes include the build identity; other source changes are
the existing two test owners plus the new content-only proof. Fourteen affected documents
have4801 existing local file targets checked, zero missing targets; this is not anchor or
global semantic verification. Diff check0, changed-source trailing whitespace0, staged0
in both repositories, no live matching verification process. All221 original requirement
texts/statuses and the immutable completion archive hash remain unchanged. Machine-readable
attempts, gate audit, final inputs and structural results are retained beside the logs.
An initial multi-file documentation patch was rejected because its second tracker hunk
was ordered before the first; no partial write occurred. Reordered hunks applied normally.

## Historical implementation evidence

Date:2026-10-07. Current build `beep-simengine-2.211.0`, save115.
Scope: native optional primary-generation settlement at existing resource boundaries, with bounded
timed production/continuation. Not full H2 budget/provider/replay/framework acceptance.

## Final result

One exact new scenario: `Tycoon income: committed primary production and independent physical generation`.
101 checks passed,0 failures,0 aborted suites,355ms. Final clean build0 warnings/errors4.32s.
Four builds and four invocations of THIS SAME new scenario; repair chronology below. No older,
family, full, platform or OilFieldDays suite was run.92 old test metadata files migrate150 identity
lines; that is source/compile migration only. Physical payloads remain board40/work40/work view73/
transformation view9; account3/business view3 already owned the required income fields and remain.

## Implementation and ownership

- TycoonModule resolves explicit income_policy only when effective production income is enabled.
  Requires actual accounts, valuation and matching frozen policy/provider identity. Native
  primary_production_income stays master-listed JSON. Income-off binds no settlement capability;
  module omission requires no currency, value table or account.
- IResourceProductionSettlement/IEconomy/IBusinessAccounts alias the SAME BusinessAccounts instance.
  Existing account Sync owns selected policy, settlement sequence, per-seat counts and exact raw
  cumulative earnings. No new ledger/codec/system, journal subscription, payout event listener or clock.
- Existing ResourceTransactions invokes one optional synchronous generation participant. Inventory
  Produced batches and conversion outputs are admitted before irreversible resource mutations; whole
  conversions aggregate actual consumed/output bills, excluding released tools. No stock transfer,
  purchase, initialization or cancellation-return path invokes payout.
- Actual output vectors/receipts/result are verified, then the ephemeral native settlement commits
  once, then resource success facts publish. Prepared commits are not saved or replayed. Native
  primary-only policies whitelist authored causes and return no eligible bill for consumed-input
  recipes; no reward is reconstructed from stock or retained journal events.
- ResourceInventoryCheck exposes a typed blocker. SettlementRefused reports economic admission
  failure without fake resource success or partial mutation. Continuous native flow checks the same
  optional settlement contract before acquiring inputs, so ordinary account-range refusal blocks
  rather than faulting after acquisition. Actual settlement still occurs only inside resource commits.
- Neutral physical production has no monetary recipient, not an invented player/zero account.
  A custom provider violating its successful prepared commitment after irreversible output is a
  deterministic failure, not a compensation/rollback success; broader provider-fault coverage is open.

## Executed commands / chronology

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Tycoon income: committed primary production and independent physical generation"
```

1. Clean build0 warnings/errors5.15s. New scenario2 passes then one abort: an authored optional
   entity_output field used null, while the actual optional-string reader requires omission.
2. Omit entity_output for resource-only recipes. Clean build0 warnings/errors3.82s. Same scenario2
   passes then one abort: shared multi-dimension storage demanded a capacity measure gold lacked.
3. Author a common volume measure for both gold and water and one explicit shared volume limit;
   the full-output case authors15 gold plus5 water. No capacity/engine fallback is added. Clean
   build0 warnings/errors2.72s; same scenario88 passes0 failures/aborts334ms.
4. Source review found native continuous flow acquired inputs before discovering settlement
   refusal. Add pure optional settlement preflight at existing flow admission and extend the SAME
   scenario with continuous output/range/continuation checks. Clean build0 warnings/errors4.32s;
   final same scenario101 passes0 failures/aborts355ms.

No additional scenario or broad suite invocation. No gameplay code changed after the final gate.

## What this gate proves

- Sole Tycoon account/settlement binding and persistent-owner identity, selected JSON policy and
  immutable capability/account views. Starting stock earns no income.
- Authored one-gold-unit quote100000 and pure public command preview; queue/startup pays nothing.
- Native machine production takes its real three-second game duration, creates physical gold and
  zero-valued water together, then earns100000 once. Two further sequential units earn200000.
  One positive whole batch settlement, not one per resource event. Exact accumulated earnings are visible.
- Save/load during active production, post-output idle continuation and equal twin hashes. Restore
  does not derive additional rewards from existing gold inventory.
- Real consumed-input recycling and player cancellation/WIP return preserve physical materials and
  earn no primary reward even with the recycle cause whitelisted.
- Explicit direct-contract probes: stock transfer plus repeated consumed receipt refusal; pure
  Produced inventory admission/commit; pure whole multi-endpoint admission plus aggregate200000
  payout in one settlement. Invalid generation returns no commit and mutates nothing; neutral
  generation has no fabricated account.
- Income disabled with real accounts, and economic module omitted with no currency/account: the
  same public native machine command physically produces gold, saves and continues, with no reward.
- Full shared storage and account arithmetic range: no partial gold/water output, no payment/count,
  saved blocked twins equal, no session fault. Missing selected income policy/reference refuses.
- Continuous gold flow commits one unit per actual second, earns once per committed conversion,
  and saves/continues identically. Continuous income range is an ordinary blocker, with no stock,
  payment/count or deterministic fault.

Tests submit/observe normal player commands or explicitly identify contract instrumentation;
all production, physical capacity, timing, eligibility, money and refusal logic stays in SimEngine.
The JSON capacity is authored physical storage content, not a platform/world-size limit.

## Remaining work / non-claims

Building budget reservations/cancellation/refunds, full custom policy/valuation/commit fault matrix,
more producer/recovery and fractional/multi-output/neutral physical scenarios, event-fault boundaries,
replay/lockstep/extension proof, broader contention/performance and full H2/framework acceptance remain
open. Enabled trading, advanced Tycoon rules and global legacy fixture/test-ledger migration also remain
open. Primary-only excludes input recipes; net-output/manufacturing rewards require explicit additional
authored contract policy, not a fabricated default. This gate does not prove those policies.

No host/demo gameplay, OilFieldDays, Unity, Godot or Draw changes. One unchanged GameClock;
exactly one construction in World. Dirty unrelated work was preserved; no reset/commit/publish.
Historical evidence and inventories were not updated as if earlier suites had been rerun.

Master: [tracker](../../plans/enhancements/MASTER_TRACKER.md).
Stage: [H2](../../plans/enhancements/P4_PRODUCTION_INCOME_DESIGN.md).
Inventory: [scoped current source manifest](P4_PRODUCTION_INCOME_SOURCE_MANIFEST.json).
