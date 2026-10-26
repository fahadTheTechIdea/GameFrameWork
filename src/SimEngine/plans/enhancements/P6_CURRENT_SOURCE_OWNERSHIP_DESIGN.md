# P6-SO1: Current source dispositions and boundary audit

## 0. Problem statement

VerifyExtraction.ps1 fails at an obsolete retained fog.json destination. Read-only inspection finds13 missing destination references, including retired rendering/umbrella/economy files and Oil tests moved into the public-contract proof project. Its namespace-only comparison also asserts compatibility with pre-extraction code despite the authorized breaking migration. This cannot establish current P6.010/P6.011 completion.

## 1. Goals

1. Reconcile every missing destination against actual current owners, retaining all121 original source identities and immutable archive hashes.
2. Keep one disposition verifier; remove obsolete namespace-compatibility assertions and authored-change allowlist instead of inventing missing legacy files.
3. Verify current standalone project dependencies and explicit destination ownership, including quickstart module/host projects.
4. Add read-only negative self-tests and run two relevant exact compiled boundary/composition checks. Do not claim whole client/rule/runtime equivalence from a path audit.

## 2. Design

Update existing source-ownership.json entries only for inspected missing destinations, with explicit breaking-migration reasons. Existing source-inventory/archive remain immutable. Trace retired RenderSnapshot to GameSnapshot/PlayerView; optional monetary contract to Standard/Tycoon/Economy; old umbrella registration to actual explicit modules; fog to original shared visibility/content owners; moved test files to OilCo.PublicProof.

Refactor existing VerifyExtraction.ps1 into current source-disposition admission plus exact original archive integrity, dependency resolution and current input fingerprinting. Its role is traceability, not byte-for-byte current behavior equivalence. Remove namespace-normalized source compatibility comparisons and their growing exception list. Keep original source counts, uniqueness, complete assignment, nonempty declared owner/destinations, actual files, archive count/hash and standalone references strict.

An optional -SelfTest tests in-memory malformed dispositions (count, duplicate source, missing target, unassigned source, missing owner) through the same validator. It never edits real sources/manifests. Include quickstart .csproj references in the original standalone scan. No runtime/catalog/tool duplicate is introduced.

## 3. API surface

No engine/content/save/replay change. Existing verifier gains an optional read-only -SelfTest switch. Current2.623/save159/replay1 remains.

## 4. Files

Existing VerifyExtraction.ps1 and source-ownership.json, this stage/evidence and active tracker/checkpoints. Oil source/test/client files are read-only references; no Oil/business/platform implementation edits.

## 5. Error handling

Missing/unassigned/duplicate/ownerless destinations and archive/reference mismatches still fail explicitly. Breaking migration is declared, not silently skipped. No archive change, fabricated replacement file or compatibility path is allowed.

## 6. Compatibility

No source compatibility promise. Original files remain recoverable evidence only, outside active compilation.

## 7. Verification

Run the single verifier with -SelfTest. Build existing affected test project if required, then run only compiled external-module boundary and manifest ownership literals using60s/5s watchdogs. No full/profile/family run. Record baseline failure, all repair attempts, current source identity and remaining thin-client/behavior gaps.

## 8. Risks

A current file path is not proof of equivalent gameplay/assertions or a complete client audit. Keep original assertion-ownership audit separate and do not promote historical paths/labels into current runtime evidence. Source dispositions may legitimately be one-to-many after decomposition, without duplicate state ownership.

## 9. Out of scope

Closing all P6.010/P6.011, migration of original assertion manifests, Oil behavior changes, client gameplay rewrites, renderer/platform/Draw, profiles/full regression and Git publication.

## 10. Execution order

1. Inspect missing references and active owner contracts.
2. Reconcile existing ledger and replace obsolete compatibility checking in its one verifier.
3. Run read-only self-tests/current boundary checks, record evidence, keep broad original gates open.
