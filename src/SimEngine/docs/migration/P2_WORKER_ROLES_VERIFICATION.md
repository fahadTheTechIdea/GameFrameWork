# P2 typed worker roles — 2026-10-01

P2 remains in progress. This closes authored typed role identity/requirements, not generic requirement
providers, the full activity DAG or the complete framework separation.

## Implementation and ownership

WorkerRoleId is a readonly stable string identity in the existing public Core/Ids owner. It is not a
closed enum, content-table integer or unchecked string parameter. WorkerRoleDef declares worker_roles
through StandardModule's existing content registry. No second role table/resolver exists. Oil's master
manifest now includes oil/worker-roles.json with its 14 distinct authored roles.

Worker membership, IWorkerRoles.Can, StepDef.Role/Make, Job.Role, WorkBoard.AddJob, generated demolition/
line plans and management views use typed identities directly. There are no old string signatures,
compatibility overloads or implicit conversions. WorkerDef exposes immutable membership; duplicate
memberships, malformed IDs, missing references and nonpositive work rates refuse content. Runtime
programmatic job/plan posting validates declared roles before allocating identity/state. Unstaffed timed
steps use None, continue over deterministic time and reserve no machine.

WorkBoard.Sync persists stable role strings through its sole existing save/hash path; read refuses
unknown role identities with CorruptStateException. Engine build is beep-simengine-2.4.0. Save format
remains 3 because the binary role representation is unchanged; earlier engine builds still refuse.

The non-friend proof worker resolves a new JSON module.fabricate role and executes standard construction
without scheduler changes or a built-in Worker. Tests verify typed facts, physical travel/work, unrelated
machine refusal, job assignment, release, save/load and deterministic continuation. Additional tests
cover invalid role references in workers, construction, orders, demolition and lines, immutable lists,
invalid runtime jobs/corrupt saved roles, and timed work without a fabricated role. Existing assertions
remain; two original IdleWorkers call sites migrated to the typed public API.

The headless shell's only behavior change is formatting CurrentRole.Value. Client purity tests were
not relaxed. Recovery archives and the historical source inventory remain unchanged; the migration
verifier explicitly labels Core/Ids and the shell presentation migration as authored P2 changes.

## Gates and current source

```powershell
# Framework root
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- 'Worker roles' -v
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- --all
pwsh -NoProfile -File docs/migration/VerifyExtraction.ps1
Get-FileHash src/Beep.SimEngine/bin/Debug/netstandard2.1/Beep.SimEngine.dll -Algorithm SHA256

# Oil active-v2 root
dotnet build SimEngine.slnx
dotnet run --no-build --project OilCo.Tests
dotnet run --no-build --project OilCo.Tests -- --all

# Original repository
git diff --check
```

Both solutions build with zero warnings/errors. Focused roles: **44 passed / four suites**, zero failed,
**0.2 seconds**. Full framework: **496 passed / 52 suites**, zero failed, none skipped, **0.6 seconds**.
Four new suites add 44 checks over the 452-check mobility/supply baseline; no assertion was deleted.

Typed-role fast oil: **155 passed / 18 suites**, zero failed, **four long suites skipped**, **26.1 seconds**.
That run preceded the final private CheckEffects-to-CheckPlan rename; public behavior/role signatures
were unchanged. The final typed-role source's full oil profile (session 72663) completed with
**186 passed / 22 suites**, zero failed, none skipped, **593.3 seconds**. Frontier, programmed-player
Demo, allocation-budget and storyteller acceptance all ran. Combined typed-role baseline:
**682 checks / 74 suites**, zero failures. This is evidence for the fingerprints below only, not for
later requirement-provider implementation.

Ownership verification: **121** source dispositions/recovery hashes; **50** unchanged namespace-normalized
single-file C# transfers (explicit P2 edits excluded); standalone dependencies; no original runtime.
Executable identity: **142 files**, SHA-256
`EC8FEA9D2C0422F5EE334FE314FAD62CBC38C51BC948D99EE9CEC99947C5AB1B`.
Runtime DLL SHA-256:
`9BBEFD52E72623852C5BF61FB9FEA469A1117B2F022CAC74EC1B5BC9ECC281D9`.
Git diff --check passes, with only existing line-ending normalization warnings.

## Failures encountered and corrected

Initial oil build failed because the shell coalesced a typed ID with a string. It now formats the ID's
Value; no client-side role decision was introduced. The initial purity run then correctly refused the
ID's Standard namespace placement (**154 passed, one failed**). The ID moved beside other public IDs
in Core/Ids; the unchanged boundary gate passes again.

The first new role test run had **32 passed, 12 failed**. Eleven invalid-content assertions called only
the lazy factory loader and never requested a resolved scenario; each now invokes NewGame and proves
the expected content refusal before play. The external fixture's new worker had not flushed its entity
index before the idle-role query; the fixture now flushes through the existing entity-store boundary.
Scheduling remains entirely inside the engine. Corrected tests/builds pass as recorded above.

## Remaining acceptance

P2 generic requirement/effect providers, operation/signal/response contracts and full event/state/view
identity/schema declarations remain open. Role typing alone does not implement alternate providers,
area/tool/technology requirements, plan DAG validation, policy scheduling or logistics reservation
completion. P3–P6 remain planned. No Tycoon, renderer/platform, client gameplay, Git initialization,
remote or package publication changes occurred.
