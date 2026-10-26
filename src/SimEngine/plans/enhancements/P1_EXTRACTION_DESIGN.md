# P1 — Single-owner extraction

## Outcome

One framework runtime lives at `C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine`.
OilFieldDays references it; the original v2 subtree contains no second framework implementation.
This stage changes ownership and project identity without rewriting oil gameplay.

2026-10-07 ownership follow-up G1: [engine-authored startup/native surveys](P11_NATIVE_SURVEY_STARTUP_DESIGN.md)
adds generic setup/profile contracts to the canonical framework, using existing world generation,
placement/creation/value owners instead of new fixture-owned gameplay for this native workflow.
Scoped58-check proof,2.232.0/save136; no claim all older fixture starts/routing are migrated or original
extraction/oil/full gates rerun. The original extraction milestone and broader goal are distinct.

## Inputs and boundaries

Read the active source instructions, solutions, all project references, content sources, and revision
tracker before relocation. The source contains dirty R7 changes and untracked lifecycle/test/state
files. A checkout/export of HEAD is insufficient: it would lose the current implementation.
Exclude the older sibling engine, Tycoon, unrelated files, and generated `bin`, `obj`, `.vs`, caches.

## Implementation sequence

1. Record relative paths, hashes, and Git tracked/untracked/modified status for every in-scope source,
   test, instruction, and authored content file. Record the solution dependency graph and suite inventory.
   Reinspect the target, which currently contains planning documents but no runtime source.
2. Produce an exact move list for the active `SimEngine/` runtime. Move generic tests by dependencies,
   splitting mixed suites rather than moving all `OilCo.Tests`. Keep oil-owned JSON at the oil source.
3. Establish a recoverable snapshot of the in-scope working files and inventory. Do not turn a backup
   into a referenced/buildable engine copy. Do not reset Git, overwrite collisions, or move the entire repo.
4. Create `src/Beep.SimEngine/Beep.SimEngine.csproj`, `Beep.SimEngine.slnx`, and standalone test projects.
   Start with one runtime assembly targeting the existing `netstandard2.1`; no target change is justified
   merely by extraction. Retain deterministic arithmetic and warning-as-error behavior.
5. Transfer framework files into that project and rename assembly/root namespaces to `Beep.SimEngine`.
   Update namespaces, imports, module registrations, test assembly names, and callers as one migration.
   No type forwarding, old facade, alias runtime, or parallel implementation.
6. Update the oil extension's project reference to the explicit target project. Update the original solution
   to reference the standalone framework as needed. Clients still consume only approved public facades.
   Remove references to the original runtime project and its source after the destination is validated.
7. Give test assertions/runner helpers one owner. Prefer a shared test-only support project when both
   solutions genuinely need it; otherwise assign each helper to its actual consumer. No runtime references
   to test support and no copied helper implementations.
8. Reconcile instructions and docs: framework instructions move with framework responsibilities; oil
   instructions stay oil. Replace stale paths with the new owner. Original revision tracker points to this
   master for extraction rather than tracking the same tasks twice.

## File-operation safety

Validate every resolved source/destination against the exact in-scope roots. Use literal paths and
native PowerShell move operations for relocation, and the normal patch tool for source edits.
Existing target files are never overwritten blindly. If a collision occurs, inspect it and stop that move.
Do not recursively delete source/target roots. Retire only the inventoried original files after confirming
their authoritative destination and updated references. Record what moved and where recovery exists.

The target is not currently a Git repository. Do not run `git init`, alter remotes, stage all changes, or
publish anything as part of an assumed requirement. Cross-repository Git history handling must be
explicit; a file move cannot promise preservation of Git history in a new repository automatically.

## Acceptance checklist

- [x] Source inventory includes dirty and untracked R7 files; every file has exactly one disposition.
- [x] Standalone solution builds without discovering OilFieldDays through a project reference.
- [x] Oil solution builds against the new project and runs its pre-extraction gameplay regression.
- [x] Relocated generic tests run independently; remaining oil tests retain their assertions.
- [x] No reference or compile glob includes the retired runtime source location.
- [x] No second framework source tree or compatibility facade remains in the active solutions.
- [x] Final moved-file inventory reconciles hashes; intentional namespace edits are separately recorded.
- [x] Source recovery and updated project paths are documented; unrelated files remain untouched.

## Evidence to record

Both solution build commands, exact test invocations, suite ownership changes, failures, and source
inventory identity. P1 alone does not prove a domain-neutral API: public contracts and oil friendship
are deliberately addressed by P2. Do not advertise the extraction as complete before all stages pass.

## Implemented status

Verified on 2026-10-01. See [P1 verification](../../docs/migration/P1_VERIFICATION.md) for current commands,
counts, ownership/recovery checks and the partial P2 access-boundary slice. Broader stages remain open.
