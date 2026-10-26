# P6-PE1 — Protocol expectation migration evidence

Status: VERIFIED-COMPILE/STATIC ONLY. No runtime or new gameplay pass claim.

[Stage](../../plans/enhancements/P6_CURRENT_PROTOCOL_EXPECTATIONS_DESIGN.md). Original221/187checked/34open unchanged; additional218. No original gate closes.

## Exact change and verification

31 existing test files contain46 changed lines: only positive historical literal engine-build expectations and stale global save-format expectations now use StateCodecTests.ExpectedBuild/ExpectedSaveFormat. The existing fixture independently reads checked-in current-engine-protocol.json; expected values are not read from EngineInfo. Current engine identity remains beep-simengine-2.489.0/save154/replay1.

Before/after inventories preserve full-source canonicalized nonprotocol SHA256,1,574 c.Equal/That/Throws assertions and31 exact TestRunner.Add registration lines. All31 canonicalized hashes, assertion counts and registration arrays match. Actual file hashes changed only in those31 files. Actual expressions, messages, negative version rejection tests, codec-specific assertions and runtime profiles were preserved. Production and extension proof implementation unchanged.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
```

One compile: exit0,10.58 seconds,0 warnings,0 errors. No focused, old, family or full runtime ran. Prior P10-EP1 FINAL296/0/0 remains historical scoped evidence, not a fresh pass on these changed test inputs. Final full regression is separately authorized only after implementation finishes.

## Canonical nonprotocol source identity

Each hash below is identical before and after applying ONLY the declared positive build/save substitutions to both sources. Assertion count and exact registration lines were compared independently.

| Test source | Nonprotocol SHA256 | Assertions | Registrations |
|---|---|---:|---:|
| tests/Beep.SimEngine.Tests/CommandCompositionTests.cs | ebc4403516d68a0e1060c7b4430cf5a65d7ca45c3d1d34d712be5c6df676cfdd | 26 | 1 |
| tests/Beep.SimEngine.Tests/ContinuousCatalystTests.cs | 7df21d0114367a697e8db3fb13be272b539a08509339ec1b953c5fa443e02bc1 | 55 | 1 |
| tests/Beep.SimEngine.Tests/LogisticsViewDurabilityTests.cs | 6707cecf16c452f30ad112f4c9a20e7d476a172d96daa9b3764768b1b9452620 | 67 | 1 |
| tests/Beep.SimEngine.Tests/MixedClaimGrowthTests.cs | f26d477e096923b07ebea9530c323e65254e1155b2b7c781f691bc4e2933f963 | 32 | 1 |
| tests/Beep.SimEngine.Tests/MovementEnvelopeTests.cs | 6d82e81740200a0b2a0b8449fd53e53d84da73d8fbc82e9b4a2b47b8aee797a7 | 42 | 1 |
| tests/Beep.SimEngine.Tests/NamedDockRecoveryTests.cs | b516ac697343194caa0f06309543cf3f5a5f04ff9b55effd7207760c90395028 | 110 | 1 |
| tests/Beep.SimEngine.Tests/NetworkGraphAdmissionTests.cs | 7ec460a0f76ae34266a9e8a1fda4ebb74aaf61ed6da1df83c8cab67b1a138637 | 24 | 1 |
| tests/Beep.SimEngine.Tests/OwnedSpatialNetworkTests.cs | 7440e048e65aee5fd53015f635649044053c9de29771085db64f711357db0209 | 40 | 1 |
| tests/Beep.SimEngine.Tests/OriginalPortRecoveryTests.cs | 08556ea733cbe2f564f923ed75dc703a74eba1d39b4a240d0d48fae8c31a324e | 50 | 1 |
| tests/Beep.SimEngine.Tests/PortableLinePathTests.cs | be70f6fc08562857727a01c70892e7ccaf4b775b48b87b28efcf2e5fc8a27f41 | 35 | 1 |
| tests/Beep.SimEngine.Tests/ProcessStaffingTests.cs | 6d342966d03af7684ef9f33423a2b8f206b9303769491973bea689df86a4c40f | 55 | 1 |
| tests/Beep.SimEngine.Tests/ProductionStaffingTests.cs | cab080bf6144a3ded4f96ee7ea7dc138853a12bf3df1953b4e1fb2bf5ae00fe2 | 55 | 1 |
| tests/Beep.SimEngine.Tests/ScopedActivityResourceTests.cs | 001d8a252393614d2564a30ea9d0524c47070f6112b2da61557c2f729f192c15 | 43 | 1 |
| tests/Beep.SimEngine.Tests/ScopedEvacuationOriginTests.cs | 756920b62749118f53e186815cb59ce5a986473487230391cb14cd1846dc3a8d | 45 | 1 |
| tests/Beep.SimEngine.Tests/ScopedRecoveryLifecycleTests.cs | 8e86405ebdb10fdf281ecc9f5414a1ed0b7153501d8866dd0f6e2bb7a39ef790 | 49 | 1 |
| tests/Beep.SimEngine.Tests/ScopedHaulTests.cs | ab088a9a0d67cb4023125611540828bd559e344ac4463ecbe733faf7c05bc786 | 45 | 1 |
| tests/Beep.SimEngine.Tests/ScopedSourceRouteTests.cs | 145c4661aed4dfec852bcd8d1251d25d9ea16b789b2637d535834b824456b83c | 46 | 1 |
| tests/Beep.SimEngine.Tests/ScopedRecoveryAccessTests.cs | 64576b3f5ac4d6ed11d5ab6b7be22dd67b234c5f3f9483e9822a07577fe911f2 | 51 | 1 |
| tests/Beep.SimEngine.Tests/ScopedLogisticsDiagnosticTests.cs | c1070db64663d97dfe1433517bf28f78291302417c87bb23e4d64cd5e2c02bf8 | 83 | 1 |
| tests/Beep.SimEngine.Tests/ScopedRecoveryMatchingTests.cs | 88963ac0bb1a999b64efb984276313568e0c5e206d6a72754dcf39094be16359 | 56 | 1 |
| tests/Beep.SimEngine.Tests/ScopedStockPolicyTests.cs | 8abaacd5c90c16f3fd9186069b5491036d4c1b91243754d9fc83b05bee1d6c01 | 57 | 1 |
| tests/Beep.SimEngine.Tests/ScopedSupplyTests.cs | bbf94cd3065829bfe62cc0d4809a44d06ac4b84401ce3f344068cd954ef7c56f | 40 | 1 |
| tests/Beep.SimEngine.Tests/SpatialCorridorExecutionTests.cs | 0e5d2b175e1da495c7b2abaa727e4f252c4275e9a4a0e6317fb9c2c0194d0e4e | 30 | 1 |
| tests/Beep.SimEngine.Tests/SpatialCorridorRemovalTests.cs | 6b6191f2e761c989ac8e87609a0bf4e44f1cf38077018640a468b1235d511f7f | 38 | 1 |
| tests/Beep.SimEngine.Tests/SpatialNetworkGraphTests.cs | 486102f483d60ca7c20e681943c1166651d49a170ac96d6ab4a43e6b49fe4730 | 34 | 1 |
| tests/Beep.SimEngine.Tests/TransformationCatalystTests.cs | 65f656e848d9df5e1e304b7867786d862f4a637c15a28bc4475276842f586da8 | 56 | 1 |
| tests/Beep.SimEngine.Tests/TransformationOutputPortTests.cs | d4e2c2803f714cc48fa10b2ae8ad29df209132d55ec6a22913a1d0507119d51b | 61 | 1 |
| tests/Beep.SimEngine.Tests/TransformationInputPortTests.cs | 590337a8ded2f38ea967dc029800aa5a10f8fa598d200f9a3ef466c7dc8de177 | 81 | 1 |
| tests/Beep.SimEngine.Tests/TransformationOperatingPortTests.cs | 1a4bd3a00b7c071131d7148c20954de73acf26e6092cf99948258deb91d5adf4 | 75 | 1 |
| tests/Beep.SimEngine.Tests/TransformationToolPortTests.cs | 0ecf366771e5fcfa35c3dbf3da6cef1eb58b47e1a47266742f3982451849c604 | 46 | 1 |
| tests/Beep.SimEngine.Tests/WorkAssignmentFactsTests.cs | b61577b3e25d0124d6a6894299f89d2179c1dc3fc14385ba8dc131b7a409970a | 47 | 1 |

## Limits

Compile/static readiness is not execution proof. P6.006 assertion ownership and broad profiles/gameplay/save/replay/lockstep/robustness remain open. No negative expectation was normalized; no business rule, production/proof logic, original gate text/checkbox or caller profile changed. No compatibility path or duplicate protocol owner. Clock, all optional Tycoon files, Oil and platform/Draw untouched. No staging/commit/push before the entire goal AND plan finish.

## Source freeze and metadata closure

Freeze2026-10-12 10:44:53 UTC:1,741 source anchors/976 selected executable inputs/13 quickstart anchors match actual files. Inventory2,927 files/1,043 Markdown documents/8,702 local links/0 broken links;18 valid master rows;221 original literal gate/line anchors validated with0 errors. Original187 checked/34 open unchanged, additional218. Current31 test-input edits include26 previously selected inputs plus5 previously existing test inputs newly frozen; no new executable file or production/proof drift. Sole clock/all optional Tycoon source identities unchanged. git diff --check exit0 with34 pre-existing warning lines;2,915 dirty entries/0 staged. No runtime, no commit/push; current source hashes are compile/static evidence only.
