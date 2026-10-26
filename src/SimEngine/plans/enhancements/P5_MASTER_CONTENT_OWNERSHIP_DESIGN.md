# P5-MC1 — Actual Oil master content ownership

Status: verified for original P5.acceptance.003. [All attempts, coverage and limits](../../docs/migration/P5_MASTER_CONTENT_OWNERSHIP_VERIFICATION.md).
Overall P5 and the full plan remain open.

## 0. Problem Statement

Original P5.acceptance.003 is open. Existing focused proofs link selected live Oil
files, not the complete master. A current proof must load every actual file through
Oil's public LoadContent entry point, resolve the complete typed catalog and test
master-only additions/refusals without introducing a second content loader.

## 1. Goals

1. Account for every JSON asset in the current Oil content root: one manifest and
   exactly one declared owner for each of the other 40 files (current inventory).
2. Use actual public Oil content/factory APIs and original ContentSet/ResolvedContent
   owners. Verify raw definition identities/counts match the frozen resolved catalog.
3. Confirm generic resources/entities/activities/recipes/roles/states/capacity/lines
   are native framework definitions/contracts, not copied Oil implementations.
4. Prove a manifest-listed extra resource document resolves without scheduler or
   loader changes, and an unlisted hostile document is never read.
5. Prove intended missing/duplicate/version/dependency/section/identity errors fail
   through the real loader; preserve scope and first diagnostics.

## 2. Design

Reuse the existing actual Oil assembly locator in ExternalModuleBoundaryTests;
extract that locator into a shared internal test helper method, not another resolver.
A new OilManifestContentTests exact case binds delegates only to the public
OilCoGame.LoadContent/Factory methods. No access to Oil's internal Definition method.
Test-side preload/recording implements the existing IFileSource contract, without
world generation, game Update, asset transformation or business changes.

Inventory disk JSON solely as an asset-accounting test; the engine still loads only
manifest-listed files. Resolve all registered modules through ContentSet.Resolve,
check each actual frozen definition's original module/identity/ownership, and inspect
generic contract aliases on those same objects. Resolve each actual scenario through
public factory time/mode metadata APIs; do not call this a six-scenario gameplay run.

Negative documents are isolated in-memory authoring fixtures. No persisted Oil edits,
compatibility fixtures, copied production catalog or fallback defaults.

## 3. API surface

No production APIs or payload changes. One new exact test registration; one reused
test artifact locator. Modules retain all gameplay and content validation decisions.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | ExternalModuleBoundaryTests.cs | Low: share current artifact locator |
| New | OilManifestContentTests.cs | Medium: actual complete content coverage |
| Modify | Program.cs | Low: exact case registration |
| New/modify | evidence, P5 design, master, README, AGENTS | Low: scope/status |

## 5. Error handling matrix

| Input | Required behavior |
|---|---|
| Unlisted asset in actual content root | Accounting test fails; no auto-import |
| Missing listed file, duplicate path/owner/module | Original loader refuses |
| Code/content version/dependency mismatch | Original loader refuses |
| Unknown section or duplicate generic identity | Original typed resolver refuses |
| Extra valid listed resource file | Original catalog resolves same native contract |
| Unlisted invalid file | Never read by engine, no fabricated content |

## 6. Backward compatibility

Development-only; no legacy/compatibility paths.

## 7. Verification

Targeted test-project compile and this one bounded exact case. Rerun the existing
compiled boundary literal only if sharing its locator changes that behavior. No
full/family/profile tests, scenario generation, long simulation or publication.

## 8. Risks

Definition uniqueness is per registered content kind, not globally per raw ID.
Different legitimate kinds may share IDs. Do not reject domain TechDef extending the
native TechnologyDef as though a supported SDK extension were a copied scheduler.
Complete content resolution does not prove physical gameplay/private/rival parity.

## 9. Out of scope

Oil/core/business production edits, world generation, platform/Draw/adapter work,
global regression, commits/push. Close only P5.acceptance.003 if actual evidence proves it.

## 10. Sub-task execution order

1. Inspect real manifest, file inventory, sole loader/resolver and public entry points.
2. Add focused actual-content/negative coverage using those original owners.
3. Investigate failures without weakening original rules or changing Oil data silently.
4. Record all attempts and exact evidence; update original gate only when proven.
