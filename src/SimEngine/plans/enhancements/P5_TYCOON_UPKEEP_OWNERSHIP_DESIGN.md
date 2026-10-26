# P5-A16 — Tycoon-owned periodic upkeep

## Scope and authority

Continue the human-approved financial ownership migration. Preserve all38 original upkeep amounts,
daily timing and into-debt behavior. Physical work, disposal and fuel consumption do not require money.
No platform, Draw, host gameplay, duplicate account ledger, compatibility reader or second clock.

## Stage 1 — File-owned cost contracts

IEntityOperatingCostDefinition declares the actual target prototype, amount, category, positive period,
offset and reason. EntityOperatingCostDef is the JSON parser/resolver; explicit references must resolve.
The selected game time requires exact periods/offsets and offset within period before initialization.
Other providers can implement the same contract through module-owned content without changing scheduling.

Oil's business-operating-costs.json owns38 policies under optional sim.tycoon in the master.
All target IDs, amounts, daily period and ledger.upkeep reason are retained; offset remains30minutes.
Structures and vehicles no longer contain upkeep components. Their other authored data is unchanged.
Oil UpkeepDef/Upkeep, registration, unused ledger constant and CommerceSystem charging loop are deleted.

## Stage 2 — Sole deterministic interpreter and original accounts

EntityOperatingCostSystem is the one periodic-cost interpreter, registered by Tycoon at order601.
It reads actual immutable cost providers under the existing observation guard, checks native mode rules,
and charges living owned matching prototypes through the original IEconomy/BusinessAccounts authority.
Daily upkeep still ignores equipment operation/failure state, as before; ordinary debt is allowed.
There is no last-paid counter, saved ticket or schedule clock. TickInfo is interval-start time; the
GameClock StepIndex counts completed intervals. Save continuation derives due times from that same clock.

Tycoon absent means no definitions/system/account. Tycoon selected with running costs disabled publishes
disabled decisions but never requires or fabricates an account. No cost substitutes free materials.

## Stage 3 — Immutable diagnostics and strict boundary

IBusinessOperatingCosts publishes detached EntityOperatingCostView records.
BusinessView.CopyOperatingCosts delegates to that same interpreter. Original CanObserveOwned masks
unobservable/nonowned/unknown entities; refused queries clear destination. Reads are observation guarded.
Business view version7, periodic system codec1, core2.541.0/save154/replay1. Existing account codec unchanged.

## Stage 4 — Focused verification

One NEW exact thin-driver case tests three native modes over450updates each: absent Tycoon, selected but
money/costs off, and enabled running costs. It projects three canonical policies onto existing native
fixture prototypes, preserving their real amounts/period/offset/reason. This authoring projection is
not full Oil prototype/content admission and introduces no runtime driver financial logic.

Check due boundaries, unchanged between-boundary balances, debt, sole-clock pause, saves immediately
before/after a fee and after debt, saved twin hashes, account-less disabled costs, isolated sessions,
pure fee views, cleared unknown-target queries and invalid target/offset refusals. Separately verify
all38 canonical amounts and42 entity declarations preserve non-upkeep data exactly.

## Remaining work

Sale settlement/statistics, research/power monetary authoring and the remaining physical JSON still
need consolidation/migration. Complete Oil initialization/gameplay, private facts, arbitrary cost
provider fault/mutation tests, pending reservations versus ordinary debt, entity removal/refounding
and broad replay/lockstep/stress gates remain open. No original checkbox closes. Focused tests only;
full regression requires renewed human request. Commit/push waits for full goal AND plan.

[Evidence](../../docs/migration/P5_TYCOON_UPKEEP_OWNERSHIP_VERIFICATION.md)
