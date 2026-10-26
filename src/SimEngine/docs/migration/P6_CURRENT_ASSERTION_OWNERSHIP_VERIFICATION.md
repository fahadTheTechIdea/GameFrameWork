# P6-AO2 — Current original-assertion ownership verification

## Outcome

The existing assertion ledger and verifier now agree with current source owners:
**364 original assertions, 50 original suites, 121 immutable archive entries,
223 framework assertion owners, 141 Oil assertion owners, and 12 intended negative
refusals**. No gameplay suite was executed. This is current static ownership,
not behavioral equivalence, a runtime pass count, or full-plan completion.

[Design](../../plans/enhancements/P6_CURRENT_ASSERTION_OWNERSHIP_DESIGN.md).
Engine **2.623.0/save159/replay1** is unchanged. The existing extraction verifier
still fingerprints 2,079 executable inputs as
`D27AC49694A62F917876E8A558DFEAB32DED93EADD9A27B2A0220D3E7D832C0C`,
identical to P6-SO1. No C#/project/content input or Oil source changed.

## Actual reconciliation

The baseline failed at retired `OilCo.Tests/DemoTests.cs`. Complete AST candidate
inspection found 122 stale assertion file paths, 15 changed expressions and 15
old invocations without an exact method/kind/label candidate.

Current file dispositions constrain every candidate. The complete rewrite changed
175 owner records (including line/ordinal changes), 21 suite-owner records and 30
explicit invocation assignments. All original assertion IDs and original source
objects, and all original suite source objects, were compared before/after and
remain unchanged. The immutable archive still supplies every original identity.

The 27 earlier explicit reassignments remain; 30 reviewed current changes make
57 total. Existing unchanged assertions still require exact current method, kind,
label, ordinal and invocation text, not a count or substring acceptance rule.
Candidate matching was a one-off reviewed mechanical data migration, not a
verifier refresh option or runtime expectation generator.

Changed expressions select ordinary contract placement arguments, explicit route
ports/settings, TechnologyCodes.Active, the session clock, actual spatial snapshots,
existing BusinessView sales/account reads, or the current factory/stock-target
observation. They do not introduce client rules or financial defaults.

Specific retired semantics are explicitly recorded rather than presented as
preserved behavior:

- Demo's old client-owned all-order strategy and 300-day content coverage now map
  to actual accepted founding/controller delegation and no-client-strategy checks.
  This does **not** prove all content/order coverage; that acceptance stays separate.
- Default omniscience and the duplicate optional fog module are retired. Actual
  canonical visibility, viewer-scoped publication, opt-in storyteller composition
  and saved module identity own the replacement checks.
- Private survey knowledge belongs to the actual scoped knowledge ledger; foreign
  finance reads use explicit BusinessView refusal rather than company books access.

Current suite labels/handlers are parsed from actual Register methods, with one
declared handler and registration of the class in its real runner. PublicProof
owns the moved Oil tests. The still-retained OilCo.Tests/PurityTests.cs keeps its
actual OilCo.Tests/Program.cs registration; it was not incorrectly moved merely
because other tests moved. No test source or registration was changed.

## Stricter existing verifier

The same VerifyAssertionOwnership.ps1 now checks the explicit reassignment count
and all original suite identity fields. Suite-registration errors identify the
original suite, actual program and class. There is no second ownership checker.

Twelve in-memory negatives run through the same validator and must fail with the
intended error: removed/duplicate original assertion; missing/changed current
owner; unregistered suite; changed original invocation; changed original suite
handler; wrong reassignment count; blank assignment; owner outside the explicit
source disposition; removed suite; duplicate suite. Original archive/manifests/
sources are never mutated by self-tests. Blank-assignment count is kept coherent
inside that negative case so the assignment validator itself is exercised.

## Every executed verification and preparatory failure

| Attempt | Observed result |
|---------|-----------------|
| Baseline assertion verifier -SelfTest | exit1, 2.714s tool wall; missing retired DemoTests path |
| One preparatory read-only identity command | PowerShell parser error from a mistyped property access; no writes |
| First reviewed in-memory mapping | exit1, 5.629s; indiscriminate runner replacement failed registration for retained PurityTests; stopped before any manifest write |
| Corrected reviewed mechanical mapping | exit0, 6.109s; all immutable source identities unchanged and complete strict owner verification before writing the existing ledger |
| Assertion verifier -SelfTest | exit0, 7.558s; 364/50/121 and 12 negatives |
| Extraction verifier -SelfTest | exit0, 1.344s; 121 dispositions/archive hashes, six standalone project boundaries, five intended negatives, unchanged executable fingerprint |
| Final assertion verifier -SelfTest, after intended-error strengthening | exit0, 7.510s; 364/50/121, 57 explicit reassignments and 12 intended negatives |

Commands:

```powershell
./docs/migration/VerifyAssertionOwnership.ps1 -SelfTest
./docs/migration/VerifyExtraction.ps1 -SelfTest
```

The first assertion/extraction final commands ran independently in parallel. All
processes terminated; no observed process timeout or restarted/live handle.
No C# build was necessary or run, and no full/profile/family/exact gameplay test
was run. RuntimeSuitesExecuted is explicitly zero.

## Requirement limits and next action

P6.acceptance.006's existing static ownership claim now has refreshed evidence.
No new original checkbox closes: **221 original requirements, 204 recorded checked,
17 open**, confirmed by exact original-ID/literal-requirement mapping of all 221
entries with no missing match; this is not global revalidation.
P6.acceptance.010 remains open for broader executable-behavior agreement, not
closed merely because a source ledger passes. P6.acceptance.007/profile evidence,
the remaining P3/P5/P7/P8/P10/P11 gates and the full goal remain open.

P6-SO1's compiled composition/boundary evidence and P6-TC1's complete host source
and public parity evidence remain separately scoped. Neither is silently rerun
or expanded by this static result. Historical P6-AO1 evidence remains historical.
No engine/domain/Tycoon rules, Draw/platform/adapters, compatibility code, memory
files, Git staging, commit or push were changed.

Final metadata checks: new design/report local links resolve; `git diff --check`
exits zero (35 existing line-ending warning lines), and zero paths are staged.
Current SHA256: assertion ledger
`C4E4D932FDC1A19FD26847F77953FCAF9F6392A92DD0FF3D0270C0F3E787CB3F`;
verifier `DBA8B70C903225A06437E0D7B0A48D9E7C84C780688FD35DE45116082AACA15A`.
