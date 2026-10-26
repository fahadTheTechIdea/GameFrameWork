# P4-G19a shared resource declaration — scoped evidence

2026-10-09; core2.351.0/save154/replay1. G19a prerequisite only, not full resource catalog migration.
Full P1–P12/goal remain open; no staging/commit/push. The lifecycle skill guided this plan/code/evidence
sequence; user/repository instructions prohibit its broad regression suggestion without authorization.

## Implemented ownership

- Content/Resources.cs: sole public ResourceDeclaration extracts the ORIGINAL explicit unit/category/
  phase/traits/capacity reader, unit resolution, dimension uniqueness, sorted immutable measures and
  ordinal HasTrait lookup. Native ResourceDef delegates, retaining original hooks and numeric ID.
- Read returns a fresh private-created declaration. Resolve attempts once; resolved units/measures
  publish only after all references succeed. Unresolved facts cannot masquerade as ready.
- Public ResourceCapacityMeasure rejects null/invalid units and nonpositive coefficients. A coefficient
  is not a native amount; fractional discrete coefficients remain legal with original capacity quantum.
- Existing ResolvedContent, resource IDs, inventories, reservations, flows, work/jobs/hauling,
  UnitConversion, GameClock, state/view schemas unchanged. No copied metadata rules or compatibility facade.
- Explicit master-listed resource-declarations.json uses existing PlanetOreDef authoring hook; native
  supply remains the original resource. ResourceDeclarationModule registers content and reuses the
  InitializeProbe only. Thin test authoring/commands/views/Update/save/replay do not own game logic.

## Actual compilation and runtime

Compilation only: `dotnet build Beep.SimEngine.slnx` succeeded11.19s, zero warnings/errors.
`dotnet build Beep.SimEngine.slnx --no-restore` succeeded4.75s after correcting ambiguous-dimension
test authoring, before runtime; succeeded5.69s after correcting the proof module registration.

Only ONE NEW case, two attempts; no old/family/full/Oil/Tycoon/platform runtime:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- --bounded-exact "Resource declarations: shared JSON metadata and timed native hauling" --timeout-ms 30000 --termination-timeout-ms 2000 --output-chars 24000
```

First attempt11passed/1failed,142ms; PID20776 exited1/observed314ms. Suite aborted at duplicate
SingleResourcePortDef registration: the reused old resource proof module also declared a component
already owned by the point module. No engine workaround: new proof composition registers only the
existing specialized resource kind and the initialization instrumentation it needs.

Final69passed/0failed/0aborted in607ms; PID5992 exited0/observed763ms. Both attempts untruncated,
no observer errors; no runtime edits/reruns after this final pass. Workflow waits capped480 Updates
with actual clock/source/destination/cargo diagnostics; child execution capped30000ms/termination2000ms.

## Narrow proof coverage

Public shared reader/ordinal traits, unresolved metadata and absent argument refusals; public measure
authoring including fractional discrete coefficient, null/zero/negative refusals. Native/specialized/
unused resource metadata, two distinct capacity dimensions and stable ordering, explicit empty traits,
immutable retained facts and original stable/numeric resource lookup with pure observation.

Normal work-policy/haul preview and command parity. Carrier travels to source20, loads partially,
conserves20 units and delivers partially at destination10. Partial loading/delivery saves continue as
hash-identical twins; one clock pauses physical progress. Final source12/destination8/cargo0 after
elapsed game time; completed saves and replay match. No host transfer/progress writer.

17 explicit malformed-content refusals BEFORE Initialize: missing unit/category/phase/traits/capacity/
composition, blank/duplicate traits, empty capacity, zero/negative coefficient, blank capacity unit,
unknown/wrong-kind quantity reference, ambiguous capacity dimensions, unknown key, malformed unused
resource. This does not prove arbitrary CLR provider fault safety or all catalog contract validation.

## Freeze and boundaries

37 named executable inputs frozen2026-10-09 06:47:13UTC; hashes in the machine-readable audit.
Original game clock SHA25635d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.
Three excluded Tycoon files remain byte-identical to the pre-slice check; pending authorization unchanged.

Independent IResourceContentDefinition registration, actual contract catalog/return types, provider
identity/metadata validation, consumer migrations, guarded detached external metadata views and the
broader hauling/manufacturing/direct-provider case remain OPEN. No claim of full G19, full plan,
full regression, competition/Oil/business/platform coverage. Prior proofs compile only, not rerun.

[Stage](../../plans/enhancements/P4_RESOURCE_CONTENT_CONTRACTS_DESIGN.md),
[tracker](../../plans/enhancements/MASTER_TRACKER.md),
[machine inventory](../../plans/enhancements/COMPLETION_AUDIT.json).
