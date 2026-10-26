# P5-CB1 — Compiled Oil extension boundary verification

## Result and original requirement

Engine2.628.0/save159/replay1 unchanged. Original **P5.acceptance.002** is verified:
“No oil module needs friend access, mutable host-world exposure or concrete framework internals.”
The authoritative checkbox remains in `P5_OILFIELD_EXTENSION_DESIGN.md`; the frozen
completion audit is an original-requirement index, not rewritten runtime evidence.
Original bookkeeping is now **221 / 206 recorded checked / 15 open**. Historical
checked gates were not globally revalidated. The full goal is **open**.

## Implementation and coverage

`ExternalModuleBoundaryTests.cs` extends the existing sole binary inspector, rather
than adding a second boundary checker. The new exact case loads the real sibling
Release `OilCo.Game.dll`; a missing binary fails explicitly. Portable module code
does not use this test-side reflection.

The inspector visits all actual types and declared methods/constructors, base and
interface types, fields, return/parameter types, generic arguments/constraints,
local/catch types, and IL member/type references, including referenced constructor
parameters. It rejects nonpublic engine access, selected reflection/binding bypasses,
native/indirect entry points and privileged access-bypass attributes. All 16 explicit
shared implementation-owner names must resolve to current engine types; nonexistent
WorkDispatcher/PlanBoard rules were removed and NetComponent's actual nested identity
was corrected. WorkBoard, WorkSystem, Storage, Worker, Supplier, Transformer,
TransformerManager, ResourceTransactions, ResourceInventoryState, ResourceFlowJournal,
NetworkService/NetComponent, CompanyKnowledgeService, TechnologyService, ObjectiveSystem
and BusinessAccounts cannot be direct Oil dependencies.

Actual Oil coverage: **574 bodies / 5,571 IL member references / 270 engine type
shapes / zero violations**. The emitted complete engine-class dependency inventory
was inspected, not just the owner-name filter. Its remaining classes are public SDK
content/module/lifecycle/command APIs, DTOs/views, public contract helpers and the
explicitly selected spatial provider. `Site` is referenced only for its public
FacetBlocker code, not as a retrieved mutable construction part. `Upgrades.Check`
and `Upgrades.TryReplace` are the public sole replacement coordinator, not duplicated
Oil replacement internals. `ResponseWork` and `PhysicalWorkEffectContracts` are
public contract dispatch helpers. ComponentList queries enumerate Oil-owned Well,
PowerSource and PowerUse parts, not concrete generic work/inventory implementations.

`Beep.SimEngine.Maps.TileMap`/its layers are the selected public simulation-space
SDK, **not Godot types**. This audit neither removes that deliberate dependency nor
claims arbitrary-map support for Oil. No renderer, Draw hook or platform was added.

The game facade has no public World property. The actual exported Oil factory,
command and view surfaces and transitively referenced Oil fact types are checked
through arrays, generics and delegate signatures: **94 signatures / zero live-owner
leaks**. World/Entity/component/service types remain legitimate engine-side extension
APIs; they are not exposed to players by the audited host-facing surface.

Five in-memory metadata/IL negatives go through the **same** inspector: private World
field access, Type.GetType binding, P/Invoke, List of actual internal AutomationSystem,
and a WorkBoard return signature. Each must fail for its intended reason. Their
methods are never invoked; no native library is loaded. Public engine generic
definitions closed over Oil's own internal types remain legal, while private
engine-owned generic arguments are still rejected.

## Focused commands and all attempts

Build commands (no full solution/test profile):

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v minimal
```

Three framework test-project builds: 5.27s, 4.41s, 4.25s; actual Oil target build
1.08s. All exit0, zero warnings/errors. The latter includes its normal Headless
project dependency; no host source edit or gameplay run occurred.

Every runtime command used one literal `--bounded-exact`, timeout60000ms,
termination-timeout5000ms; output budget4500/6000/14000 characters. All handles
reported terminal Exited, output untruncated, no observer errors/timeouts.

| Attempt | Exact case | Assertions | Suite/process ms | PID/exit |
|---|---|---:|---:|---|
| Initial | Oil compiled boundary | 10 pass / 1 fail | 60 / 210 | 20700 / 1 |
| Initial | Framework independence | 10 / 0 | 177 / 329 | 13144 / 0 |
| Generic visibility correction | Oil compiled boundary | 14 / 0 | 49 / 182 | 30756 / 0 |
| Same correction | Framework independence | 10 / 0 | 140 / 274 | 25436 / 0 |
| Final, constructor signatures/inventory included | Oil compiled boundary | 14 / 0 | 58 / 193 | 35172 / 0 |
| Final | Framework independence | 10 / 0 | 143 / 279 | 20152 / 0 |
| Final, actual external Oil runner | Oil extension boundary | 8 / 0 | 8 / 104 | 35648 / 0 |

Final selected checks total **32 passed / 0 failed**. Initial Oil failure contained
59 checker false positives: `Type.IsVisible` on a public Component<T>/ComponentDef<T>
closed over Oil's own internal T. Visibility now checks the generic definition and
separately recurses into arguments. No method-specific allowance, removal of an
engine access check, Oil edit or failing engine behavior was hidden. Final constructor
signature inspection increases Oil engine type shapes267→270 and proof shapes1195→1201.

Exact names:

```text
Oil compiled boundary: actual module signatures IL and client facts require no privileged engine owners
Framework independence: compiled external modules use public engine contracts only
Oil extension boundary: one external framework runtime and one domain assembly
```

## Artifact identities

442 core C# sorted path/content SHA256 remains:
`7b1efd6284bb8fe661c8da42bc8be67faad0ae66921b41fdf4150b6cdd096d7c`.
Release framework and actual Oil-local framework copy both:
`B709D2BBBD53B8127CA6F024916A3374E0F0A91A8C9832E44FBEF4CC7E6D6B61`.
Actual Oil assembly:
`09BF74BB5F6D0B1F83B4848ED8160397A4C78A305D90DF4761218235A0F5B2AC`.
Final test assembly:
`6CEC3708FB8A999DE83A2E55FBF654C44C8E36FE1CB37B60896B29F0BF9D3DC5`.
Final checker source:
`355B575220504B9E1566C369EC0BD1D9131F181FC12AC4E24108ABB16E95E4B2`.

## Limits and next work

This proves the current compiled extension boundary, not a CLR security sandbox,
universal future-module safety, immutable ownership of every possible user DTO or
all gameplay semantics. No claim that all 15 Oil files were fully reread this slice.
P5 manifest/content ownership, complete timed scenarios, client fact provenance,
rival parity/privacy, saves/lockstep and Tycoon authorization breadth remain open.
P6 profiles and the remaining P7/P8/P10/P11 gates remain open. No full/family/profile
tests, long campaign, Oil/business/core source changes or Git staging/commit/push.
