# P6-AR1 — Ownership, composition and client audit agreement

2026-10-16. Engine 2.638.0 / save159 / replay1, unchanged by this stage.
[Stage](../../plans/enhancements/P6_AUDIT_RUNTIME_RECONCILIATION_DESIGN.md).

## Original requirement and decision

P6.acceptance.010: “Runtime ownership, manifest composition and thin-client audits agree
with executable behavior.” This requirement is now checked on the evidence below. Its
literal text and the frozen completion audit are unchanged. This is not closure of the
separate P6.acceptance.007 full-profile gate or of the whole implementation plan.

The reconciliation changes documentation only. No engine, test, client, Oil, business,
audit-tool, assertion-ledger or archived-source code was changed to obtain these results.
The previously completed focused runs were retained across the TileMap clarification;
matching current source/binary identities were checked before recording them, not rerun.

## Source disposition and execution ownership

From the framework root:

```powershell
& ./docs/migration/VerifyExtraction.ps1 -SelfTest
& ./docs/migration/VerifyAssertionOwnership.ps1 -SelfTest
& ./docs/migration/VerifyExecutionOwners.ps1
& ./docs/migration/VerifyCapabilityConsumers.ps1
```

All four passed. The original read-only tools were read and their admission rules were
not weakened. VerifyExtraction checks all 121 original source dispositions and immutable
archive hashes, six standalone projects and five deliberate negative mutations; tool
elapsed 1.989s. Its current executable-input fingerprint covers 2,103 files:
`77BB1623526E242ACFA155CE5D8293DDA644B53317C510D24194D470635564D2`.

VerifyAssertionOwnership reports 364 source assertions, 50 original suites, 121 archive
entries, 12 negative mutations, 57 explicit reassignments, 223 framework owners and 141
Oil owners; tool elapsed 8.651s. It explicitly executes zero runtime suites. Assignment
and original-source integrity are not a claim that all 364 assertions have just passed,
nor proof of archived/current behavioral equivalence.

The current production scans cover 443 C# sources: nine execution-owner groups, 11
reviewed alias witnesses and 1,528 typed capability-query sites, no violations. Combined
tool elapsed 9.874s. Their scope is the declared ownership/query rules, not an arbitrary
CLR sandbox or proof of all provider behavior.

## Target consumers compiled against the current engine

Each command used `dotnet build <project> -c Release --no-restore -v minimal`.
All exited zero, with zero warnings/errors; no full solution or test profile was run.

| Project | Build elapsed |
|---|---:|
| Actual Oil `OilCo.PublicProof/OilCo.PublicProof.csproj`, including Headless and Game | 1.66s |
| Actual Oil `OilCo.LiveDemo/OilCo.LiveDemo.csproj` | 1.17s |
| Actual Oil `OilCo.Harness/OilCo.Harness.csproj` | 1.28s |
| Framework `help/quickstart/Host/QuickGame.Host.csproj` | 1.10s |

Oil project paths are relative to
`C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine`.
The public consumer's copied engine DLL is byte-identical to the current framework DLL,
so its successful parity proof is not evidence against a stale engine copy.

## Focused executable results

The four framework cases used this existing runner, once each:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "<literal case below>" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 14000
```

| Exact case | Checks / failures | Child PID | Suite / process elapsed |
|---|---:|---:|---:|
| Framework independence: compiled external modules use public engine contracts only | 10 / 0 | 7480 | 182 / 310ms |
| Oil compiled boundary: actual module signatures IL and client facts require no privileged engine owners | 14 / 0 | 34472 | 76 / 203ms |
| Composition: manifest ownership, inactive providers and frozen content | 16 / 0 | 34076 | 56 / 182ms |
| Oil master content: every actual document has one owner and resolves through native contracts | 958 / 0 | 2088 | 386 / 516ms |

The independent compiled audit examines 6,123 ExtensionProof method bodies, 30,978 member
references and 1,210 engine types; QuickGame.Module has five bodies, 15 references and six
engine types. Neither has a policy violation. The actual Oil audit examines 574 bodies,
5,571 references and 270 engine types, with no violation; 94 client-surface signatures
expose no live owners. It checks the original SharedOwners policy, actual references,
absence of Oil friendship and absence of public SimGame.World. It does not ban every
concrete domain/module API: engine-side module use and client live-state exposure are
different boundaries.

The manifest case verifies missing/unknown schemas, exact versions, document ownership
and paths, frozen declarations, alternate component-key refusal and inactive providers.
The actual Oil master proof resolves 22 modules, 40 documents and 654 definitions,
including 229 native generic definitions. Six profiles are inspected as authored metadata,
not executed as full scenarios. Seven malformed cases are deliberately refused; unlisted
hostile input is not read. Neither test is a replacement for broad physical acceptance.

The actual consumer's one existing exact case used:

```powershell
dotnet C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine/OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Oil thin clients: native shell input view purity saved state and engine automation handoff" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 6500
```

Result: 28 checks / zero failures; child12520; suite8,335ms / bounded process8,436ms.
This checks query purity, native command/time/save parity, optional-survey handling,
malformed inputs, engine-authored founding suggestions, absence of invented money,
production admission/refusal, presenter purity and engine-autopilot handoff. Its existing
SmallWorldFiles changes only Frontier width/height to24 in oil/spatial-world.json; it
does not execute a full campaign or complete the canonical36-hour production order.

Total: **five distinct exact cases, 1,026 checks, zero failures**. All finished on their
first attempt in this stage; no aborted suite, observation restart, timeout or truncated
runtime output. No family filter, broad bundle, full/profile run or demo horizon.

Quickstart additionally ran once:

```powershell
dotnet help/quickstart/Host/bin/Release/net8.0/QuickGame.Host.dll
```

Exit0, tool0.324s: `Quick setup OK: tick=3; entities=1; hash=319398F538F63CE7`.
Its Main performs exactly three engine updates and verifies startup, snapshot/clock and
save/hash; this is not gameplay implemented by the host.

The preceding [P6-SL1 report](P6_STARTUP_LIFECYCLE_CUTOFF_VERIFICATION.md) separately
records the exact composition acceptance proof, 250/0, on the identical 443-source core
fingerprint below: child19320, suite5,244ms / process5,415ms. It covers dependency/content/
codec/command/system/generation/event/view ownership conflicts before Initialize, plus
valid physical saved execution. That run was not repeated or added to this stage's1,026.

## Complete active thin-client source review

Read all five active Oil console C# bodies (513 lines), their three project files and the
quickstart host/project. Headless references OilCo.Game, which references the external
engine; LiveDemo/Harness reference Headless. QuickGame.Host references its independent
module and the plain engine. The engine project has no package or project dependencies.

- CommandShell parses input, asks original previews/suggestions, submits commands, advances
  the original engine time API, reads scoped results/views and saves opaque engine bytes.
  It contains no route, cost, material, progress or completion algorithm.
- FieldPlayer performs an input/answer founding handshake and requests engine Autopilot;
  it does not own expansion, production, repairs or research decisions.
- TextClient reads scoped engine facts and formats text; presentation cadence is not a
  second game clock. Harness/LiveDemo configure those interfaces; coverage reports engine
  observations rather than manufacturing outcomes.
- Quickstart creates/initializes the game, requests three updates and checks engine facts.

No mutable World escape, host formula/state writer, platform renderer, Draw, Godot or Unity
implementation was introduced. Existing input defaults, selection query parameters and
time batching do not decide gameplay success or supply missing resources.

## Current identities and reproducibility limits

SHA256 for sorted relative C# paths plus uppercase per-file SHA256, joined with LF and no
trailing LF: 443 core files,
`983fd5c2c626b7b002e23cd9a87660fd71473a77f93669dcdfd42953a822d0c4`.

| Current Release artifact | SHA256 |
|---|---|
| Engine, including actual public consumer copy | E587479863CEE1ED162F631D837107C01FB7BF59E856A294D84E519A7AB20B5F |
| Framework test runner | D001E4B31F529E6A715E663E629D8651B4409E28424CF75FBBE3648C61B2D6F5 |
| Beep.SimEngine.ExtensionProof, framework test output | 87D567F5619BE7ECD45DC7EBE4F6E5E702B171B4142E5DD720F50FAD86E10232 |
| Actual OilCo.Game, current rebuilt output | 03246F8AC5B81BE996A64642F75EA2970AED147BB3A0FB2DFE18697789A6D9D9 |
| Actual OilCo.PublicProof | 2DC961B1B29FBABDE4804633DDF799D1F92E16A7DD6DE7169089E7893873431A |

Ten reviewed host inputs use canonical `oil/` or `framework/` prefixes, forward-slash paths
and the same sorted `path:SHA256`/LF procedure:
`793703a51d53e382875c2d39944472fab956aea7a8df7fe72d532bd531223336`.
They are the five Oil .cs bodies, the three Oil client projects, quickstart Host/Program.cs
and Host/QuickGame.Host.csproj. No executable inputs were edited after the observed runs.

Diagnostic bookkeeping, not test failures: an earlier fingerprint formatting command had
a PowerShell syntax error; its corrected combined read exceeded available context and
its unseen output was not relied upon. The final fingerprint recovery initially guessed
the wrong ExtensionProof filename/framework and once used the Oil cwd for framework
relative paths; these read-only misses were corrected by enumerating the actual output
and hashing Beep.SimEngine.ExtensionProof.dll in the framework runner directory. Successful
hash outputs, not the failed lookups, are recorded above. No builds/tests were restarted.

## Scope retained

Only the original audit-agreement requirement closes: **221 original requirements,
209 recorded checked /12 open** after literal-ID mapping. The frozen221-entry audit is
not rewritten. Full profiles remain explicitly unrun/unproven, as do the outstanding
broad timed/rival/save/business-preservation, logistics/incident, production, automation,
exploration/pressure and optional-module durability requirements. No narrow audit claim
waives their provider, stress, privacy, corruption, soak or full-scenario coverage.

The skill's stage/evidence/tracker convention was used; the user's focused-only and
no-publication constraints override generic full-regression/commit guidance. No staging,
commit or push before the whole goal and plan genuinely finish.

Final read-only handoff checks: all221 original IDs/literal requirement texts map;
209 checked/12 open. `git diff --check` exits0, with35 existing LF-to-CRLF warnings
and no other diagnostics. Both new documents have no trailing whitespace or broken local
links. Frozen COMPLETION_AUDIT.json has no Git diff; staged paths0. No matching live
framework-test, Oil-public-proof or quickstart dotnet process remains.
