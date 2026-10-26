# P4-N5 — Contract-owned startup content resolution

Status: scoped implementation verified on2.331.0/save154/replay1; ONE NEW bounded exact case67/0/0 in570ms. Follows [startup composition](P4_STARTUP_COMPOSITION_DESIGN.md). Full P1–P12 open.

## Problem and measurable outcomes

ResolvedContent's existing type/identity indexes only alias Def base classes. ContentResolver.Def<T> requires Def inheritance; WorldSetupProfileDef.Graph/entry references consequently require WorldSetupDef despite IWorldSetupDefinition. StartupModule also selects a concrete profile. A separately implemented content contract cannot participate directly.

Revise the existing catalog and startup profile/compiler, not add a contract registry, second loader, parallel graph or compatibility facade. A native profile must include a Def implementing IWorldSetupDefinition without WorldSetupDef inheritance. A separately implemented profile must be selectable through IWorldSetupProfileDefinition and compose native includes through the same compiler. Contract lookups must alias exact actual immutable definitions; ambiguous IDs must never choose the first concrete implementation.

## Design and ownership

- Broaden existing ResolvedContent.All/Count/Find/Get and ContentResolver.Def/OptionalDef generic constraints to class,IContentDefinition. Keep method names/sole indexes.
- Add interface aliases to the existing _byType/_byId indexes for implemented IContentDefinition contracts only. Do not index unrelated capability interfaces. Concrete/base collision rules and concrete Index remain unchanged.
- An interface identity shared by distinct definitions is explicitly ambiguous. Record that in the existing index; Find/Get throw ContentException. Narrow concrete/more specific contracts remain usable. No global ban on ordinary IDs reused across unrelated content kinds.
- IWorldSetupProfileDefinition exposes resolved Entries/Includes plus dependency identity and a DependencyGraph<IWorldSetupDefinition>. Native profile references use content contracts.
- Move the current profile closure/compiler into WorldSetupProfileDef.Compile(IWorldSetupProfileDefinition), reused by native and external definitions. Existing DependencyGraph alone validates topology, missing edges/cycles and canonical order. No recursive limit.
- WorldSetupStage iterates IWorldSetupDefinition. WorldSetupModule resolves the selected profile contract, not concrete class.
- Migrate existing external PointLayerSetupDef and lifecycle admission node to direct Def + IWorldSetupDefinition. Their provider-owned parsing/check/generation stays in place. Native convenience base remains for native entries, not a required compatibility inheritance path.
- Add one external contract root definition that authors a single included profile and calls the same Compile owner. Explicitly master-listed JSON supplies its identity/reference; no scanning/defaults.
- No saved state/layout changes; build/protocol identity2.331.0/save154/replay1.

## Verification and files

Content/ResolvedContent.cs, Defs.cs; Standard/WorldSetup.cs, WorldSetupModule.cs; existing extension proofs/startup JSON; exact protocol marker and new public player case. Historical fixtures compile only. One NEW bounded exact case covers alias identity/count/ownership/ambiguity, direct contract nodes/root, missing/wrong/duplicate contract references, explicit graph cycles, before-Initialize failure, physical work/haul, saved twins/no regrant and replay.

Use existing factory/authoring rather than duplicating player startup logic. Module instrumentation may inspect catalog metadata; player driver uses only immutable facts/commands/views/Update. No raw World or resource/progress edits.

## Limits and execution order

Implement sole catalog aliases → native graph/module contract revision → migrate external nodes/root/JSON → build → one new bounded case (repair/rerun only it) → evidence/tracker/audit. Original221 acceptance records remain unchanged.

Ambiguity is a selected lookup failure, not blanket global identity prohibition. Interface enumeration/table aliasing adds metadata only, not persisted state. No full/family/Oil/Tycoon/platform runtime, business expansion, adapter/Draw, second clock, legacy reader or publication. Broad catalog/provider/mod/fault/scale/global purity/full P1–P12 remain open.

