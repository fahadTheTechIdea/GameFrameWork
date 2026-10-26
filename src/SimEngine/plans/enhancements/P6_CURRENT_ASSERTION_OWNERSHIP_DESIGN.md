# P6-AO2 — Current assertion and suite ownership

## 0. Problem Statement

The original assertion ledger is frozen to an earlier migration checkpoint. Its
read-only verifier currently fails at the retired `OilCo.Tests/DemoTests.cs`
destination. The immutable original archive remains the source of original IDs;
current `source-ownership.json` dispositions and actual Roslyn-parsed invocations
are the destination authority. File existence alone is insufficient.

## 1. Goals

1. Reconcile every one of the 364 original assertion identities and 50 suites to
   one actual current owner, without changing original source identities.
2. Review changed expressions and explicitly explain retired/replaced behavior.
3. Keep the existing verifier strict; test deliberate source/owner/count defects.
4. Report static ownership separately from behavioral acceptance. Do not close
   P6.acceptance.010 solely on this evidence.

## 2. Design

Update the existing `docs/migration/assertion-ownership.json` in place. Follow
current file dispositions, matching method/kind/label only to locate candidates,
then record exact actual invocation, line and ordinal. Fifteen changed expression
candidates and fifteen unmatched old invocations require explicit review.

Retired client-side campaign strategy/coverage maps to actual engine delegation
and no-client-strategy checks. Retired optional fog/default omniscience maps to
the explicit canonical visibility composition and saved module identity. Neither
assignment asserts historical behavioral equivalence. Financial reads select the
existing optional BusinessView; no finance or client source changes are needed.

Current suite handlers/labels must occur in actual Register methods and classes
must be registered by the actual runner. Moved Oil owners select PublicProof,
not the retired OilCo.Tests runner. Preserve all immutable original suite fields.

Extend the same verifier's existing self-tests, not a second audit implementation.
Validate the explicit-reassignment count against actual reasoned rows and check
every original suite identity field. No generated expectation refresh mode.

## 3. API surface

No gameplay API changes. Existing read-only verifier command remains authoritative.

## 4. Files to change

| Action | File | Risk |
|--------|------|------|
| Modify | `docs/migration/assertion-ownership.json` | Medium: reviewed ownership reassignment |
| Modify | `docs/migration/VerifyAssertionOwnership.ps1` | Low: stricter existing validation |
| New | `docs/migration/P6_CURRENT_ASSERTION_OWNERSHIP_VERIFICATION.md` | Low: scoped evidence |
| Modify | tracker, P6 design, README, AGENTS | Low: current status only |

## 5. Error handling matrix

| Invalid input | Required outcome |
|---------------|------------------|
| Missing/changed actual invocation or runner | Fail, never search-and-accept a replacement |
| Original source identity changed | Fail against immutable archive |
| Owner outside explicit source disposition | Fail |
| Reassignment count or reason wrong | Fail |
| Retired behavior | Explicit explanation; no equivalence/runtime claim |

## 6. Backward compatibility

Development-only breaking migration. No legacy runtime or compatibility aliases.

## 7. Verification

Run only `./docs/migration/VerifyAssertionOwnership.ps1 -SelfTest` and the existing
source disposition verifier. Neither runs gameplay. Inspect the complete manifest
delta for immutable-source drift, actual owner changes and explicit reasons.
No build is needed when C# and content inputs remain unchanged.

## 8. Risks

Ownership does not prove execution or equivalence. Historical evidence stays
historical; profile/behavioral gates remain open. Do not silently map retired
coverage to unrelated passing assertions.

## 9. Out of scope

Gameplay changes, Oil source edits, platform/Draw/adapters, full/family/profile
tests, automatic expectation regeneration, Git staging/commit/push.

## 10. Sub-task execution order

1. Reproduce current failure and inventory destination drift.
2. Review all unmatched and changed invocation semantics against actual sources.
3. Reconcile existing ledger, strengthen existing validator and run static negatives.
4. Record exact results, preserve open gates and update current tracker.
