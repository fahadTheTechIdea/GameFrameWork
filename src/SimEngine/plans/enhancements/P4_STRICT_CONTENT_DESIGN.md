# P4-C2a — One authored definition owner, no overlays

Status: focused-verified, build 2.22.0 / save 11; 41 checks / two exact suites, zero failures.
See [verification](../../docs/migration/P4_STRICT_CONTENT_VERIFICATION.md). P4 remains partial.

This independent part of P4-C2 runs before the larger P4-C1 optional-module split. It changes
the existing loader, not module composition or activity scheduling. Construction authoring remains
open: removing generated sites/plans safely also requires P4 graph contracts and P7 transactions.

## Responsibility and breaking migration

ContentSet is the sole master-manifest loader. ContentTypes declares registered sections;
ResolvedContent reads, indexes, resolves and shares the selected immutable definitions.
Each authored definition has one module owner. An extension contributes distinct IDs through its
explicitly listed documents. Dependencies allow reference to existing contracts and definitions,
not mutation of the provider's content.

Remove the special `patches` section, ApplyPatch, dotted removal and JsonValue.MergeFrom together.
No renamed overlay, compatibility field, override priority or second catalog replaces them.
Duplicate IDs in a catalog family remain errors, including collisions through a base-type alias.
Authored diagnostics identify current module/version/document/section/entry and both conflicting owners.

Migrate the extension proof from rewriting `bin` to declaring `extra.bin`. Keep both configurations
independent, retain exact module dependencies, and migrate all references to the ordinary defs file.
Public JSON Set/Remove/Clone remain parser/authoring operations; resolved JSON stays recursively sealed.

## Focused verification

Two exact suites cover rejected old schemas, same/cross-file and cross-owner collisions,
diagnostic provenance, explicitly listed extension files, selected-content caching and identity,
base definitions remaining unchanged, immutable collections, interleaved ordinary player commands,
and save/load with exact active content identities. No broad family or full-suite run.

Bump engine build identity; persisted state format is unchanged. Older builds are refused normally,
not migrated. Update the master with source-specific evidence, without marking P4 complete.

## Still open

P4-C1 optional module decomposition; C2 remaining imports/root requirements; C3 discovery without
resolving inactive providers; arbitrary activity DAG and saved node identities; JSON-authored
construction/salvage; P7 atomic resource acquisition. This slice does not prove those features.
