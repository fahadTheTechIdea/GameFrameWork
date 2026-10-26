# P5-MC1 — Actual master content ownership verification

## Result

Unchanged engine2.628.0/save159/replay1. Original **P5.acceptance.003** is verified:
“All oil content resolves through its explicit master manifest with no duplicate generic definitions.”
The original checkbox text is unchanged in `P5_OILFIELD_EXTENSION_DESIGN.md`.
Current original requirement bookkeeping is **221 / 207 recorded checked / 14 open**;
historical checked requirements are not globally revalidated. The overall goal is open.

## Current executable evidence

New `OilManifestContentTests` binds delegates to **public** OilCoGame.LoadContent
and Factory methods on the actual compiled Oil assembly. It does not call Oil's
internal Definition method or grant Oil friendship. The existing actual artifact
locator was extracted once in ExternalModuleBoundaryTests and shared, not copied.

Actual disk assets are test-side preloaded into the existing IFileSource contract.
Directory enumeration is asset-accounting evidence only: the engine still loads
only the explicit master, never scans/imports unlisted files. Actual bytes are not
rewritten, resized, projected or supplemented in the positive baseline.

The focused case proves:

- All41 current JSON assets equal manifest.json plus40 uniquely owned documents.
- The real loader reads those41 paths exactly once and no other input.
- All22 code/content modules agree with the explicit master/version/dependency rules.
- Full all-module content resolves654 definitions. The source module/ID multiset
  exactly matches the resolved catalog; every definition is frozen and is the
  original canonical object recognized by ResolvedContent.Owns. Legitimate IDs reused
  across different registered kinds are not incorrectly rejected as global duplicates.
- All229 authored resources/entities/activities/transformations/worker roles/entity
  states/capacity policies/lines use exactly one native framework definition object
  per kind/identity, not Oil copies. Resource, activity and transformation contract
  aliases reference the existing native tables. Domain-specific authoring remains
  domain-owned; supported Oil subclasses/extensions are not forbidden as “concrete.”
- Reversed requested module order reuses the same cached catalog. Reversed manifest
  document order yields the same canonical content fingerprint.
- All6 actual scenario menu entries resolve their authored time and game-mode
  metadata through the real public factory. No worlds are generated or updated.
- A deliberately invalid, unlisted in-memory file is never read and cannot alter
  the catalog fingerprint. There is no directory-import/fallback path.
- Adding one valid resource JSON document to the Oil master resolves one new native
  resource contract, changes the expanded set's fingerprint and does not change
  the original set. Only this negative/extension fixture copies one existing row
  under a new ID; no production catalog is copied into the repository.
- Seven isolated in-memory defects are refused by the original loader/resolver for
  the intended diagnostic: missing listed file, duplicate document owner, duplicate
  module, code/content version mismatch, dependency/version mismatch, unknown section
  and duplicate resource definition. No test-side validator substitutes engine rules.

The existing compiled boundary literal also passes after sharing its artifact locator:
574 Oil bodies/5571 references/270 engine type shapes/94 client signatures, zero
violations or live-owner leaks, five intended binary negatives rejected.

## Commands and all attempts

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
```

First build failed in5.46s with one test compile error: IGameModeConfiguration has
ModeId, not Id. The assertion was corrected to the actual public contract; no engine
API/compatibility alias was added. Final build succeeded in5.63s, zero warnings/errors.

Only two literal cases ran, both bounded with timeout60000ms,
termination-timeout5000ms and output6000/14000 characters:

| Exact case | Result | Suite/process ms | Child terminal |
|---|---:|---:|---|
| Oil master content: every actual document has one owner and resolves through native contracts | 958/0 | 486/631 | PID22144, Exited0 |
| Oil compiled boundary: actual module signatures IL and client facts require no privileged engine owners | 14/0 | 48/186 | PID19292, Exited0 |

**972 passed / 0 failed** in the selected checks. Both untruncated, no observer
errors/timeouts, no running handles. No full/profile/family/long-gameplay tests.

## Identities

442 production core C# sorted path/content SHA256 unchanged:
`7b1efd6284bb8fe661c8da42bc8be67faad0ae66921b41fdf4150b6cdd096d7c`.
All41 actual Oil JSON assets, sorted root-relative path + uppercase SHA256 joined
with LF, outer UTF8 SHA256:
`680d739370ab3702cd502053493ad20e367655de8a96993606416a4e5024a358`.
Actual Oil assembly unchanged:
`09BF74BB5F6D0B1F83B4848ED8160397A4C78A305D90DF4761218235A0F5B2AC`.
Final framework test assembly:
`0F4A7A86DEC2F1B47741A1624C7C4C6E701DD06207EE31DBD95C41AA9E82C6BA`.
New content proof source:
`2514C486804B3716EB99B97AF6B276266D9EF80FFAD6D977937FB8FA0DB08779`.
Shared-locator boundary proof source:
`EB29BA8F87E75E6965A7C2E5B0FFAEFE7223A1D14504103A85A7D6EE53AC2D01`.
P5-CB1's earlier report identities remain that checkpoint's evidence, not current
test-assembly identities after this subsequent proof/locator change.

## Scope limits and next work

Complete authored content resolution/identity/native-kind reuse is not proof of
every physical operation, a semantic comparison of arbitrary future differently
named domain content, rival parity, private knowledge or saved session continuity.
The6 profiles above are **metadata resolution only**, not full scenario runs.
P5 timed scenario breadth, all client fact provenance, rival/privacy/save/lockstep
and finance-authorization gates remain open, as do P6 profiles/ownership agreement
and remaining P7/P8/P10/P11 requirements. No production core/Oil/business source or
JSON edits, new content loader/catalog, game logic in clients, Draw/platform/adapters,
Git staging/commit/push or broad test run.
