# P5-A38: Recovery constraints and optional authored fees

## 0. Problem

P5-A37 proved resource-only physical recovery with a complete starting bill. It did not prove the existing optional Tycoon charge, insufficient budget, scarce materials, or cancellation while constrained. Those are necessary before treating rival recovery as strategically honest.

## 1. Goals

- Prove that sufficient money spends the existing150000 construction charge once, not a substitute amount.
- Prove that149999 money cannot start the150000 step, grant missing funds, or consume its materials.
- Prove that29 steel cannot satisfy the original30-steel/20-cement construction bill when money is disabled.
- Preserve one recovery order, typed blockers, stock, account and native continuation through saved waits.
- Cancel constrained projects through the normal engine command without free resources or refunds.

## 2. Design

Production code is unchanged unless verification exposes a defect. The single new public-driver case reuses HeadquartersRecoveryTests.Files and its strict stock/command/clock observers. A small IFileSource wrapper changes only declared scenario stock, optional module/mode configuration and opening account fixture data.

Three variants: Paid (200000 account, complete bill), Poor (149999 account, complete bill), Scarce (no money module,29 steel/20 cement). Money and construction costs are explicitly enabled only for Paid/Poor; trading, running costs and income remain off. Original150000 cost definition/binding and business rules remain untouched.

Starting HQ is an authored outpost. Removal/rebuild node durations remain the positive two-minute fixture values from P5-A37, with original material quantities/handling/travel. The player founds and demolishes, then delegates normal recovery to the engine autopilot. No World access, stock/account mutation, resource grant or alternate scheduler in the driver.

The same existing account, work-cost receipts, resource claims, recovery containers, construction plans, hauling, company state and clock are authoritative.

## 3. API surface

No production/API/save/replay changes. Only shared proof helpers become assembly-internal for reuse; no friend access or compatibility path.

## 4. Files

Oil PublicProof HeadquartersRecoveryTests helpers, new HeadquartersConstraintsTests and Program registration. Framework stage/evidence/master/audit documentation only.

## 5. Errors

Stock observers throw on refused/unavailable reads except genuinely incompatible capabilities, which cannot participate in that material total. Each variant has a bounded update count and wall-clock diagnostic deadline. Blocked projects must remain actual waits/refusals, not healthy instant completions. No hidden budget or default materials may satisfy them.

## 6. Compatibility

No production compatibility change; no duplicate fixture/configuration owner or legacy shim.

## 7. Verification

One new bounded exact case, three authored variants, native saved twins and constrained cancellation. The test process is bounded900000ms; each variant420000ms,390 native update maximum. No older/family/full regression.

## 8. Risks

Current proof is not an unchanged32-hour construction schedule, every financial/credit state, dynamic income funding, all shortages/providers, captured-HQ recovery, all cancellation/cargo interruption paths, full replay/lockstep/soak/performance or full campaign acceptance.

## 9. Out of scope

Business rule/amount changes, new economic providers or accounts, grants, purchase/market behavior, new game clock/work/logistics state, hosts/Draw/platforms, full regression and broad gate closure.

## 10. Status

One new bounded exact case FINAL3244/0 in453681ms, observer453844ms. All three authored variants passed without production changes; [evidence](../../docs/migration/P5_HEADQUARTERS_CONSTRAINTS_VERIFICATION.md). Original221 /194 recorded checked /27 open unchanged; no original gate closes.

