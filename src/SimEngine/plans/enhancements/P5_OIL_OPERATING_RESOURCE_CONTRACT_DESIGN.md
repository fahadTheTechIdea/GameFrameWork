# P5-A9: Operating resource transactions and shared authoring identities

## 0. Problem statement

Oil extraction used retired dimensionless IInventory.FreeSpace and wrote products individually, risking a partially accepted mixture after depletion. Power fuel used retired TryConsume; power connectivity depended on old concrete LineService and entity footprints. Wear authoring needed the engine identity grammar but could not access its internal owner.

## 1. Goals

Use the existing IResourceTransaction owner for whole output/fuel bills and native flow facts; consume only unpromised stock; query actual network identity through contracts; use actual tile presence; share the sole identity validator with non-friend extensions. Do not claim Oil gameplay from compiler or metadata checks.

## 2. Design

Extraction builds its exact authored reservoir composition bill for the current elapsed-hour amount. IResourceTransaction.EvaluateInventory admits all native dimensions, filters, quanta, stock promises and optional generation settlement. NoCapacity records StoreFull without depletion; settlement refusals retain their native blocker. Other metadata/protocol failures throw. CommitInventory must publish the exact complete bill before reservoir Remaining, Lifted and domain metrics change. No per-product clamping or dimensionless capacity guess remains.

This is explicit whole-step backpressure, not proportional partial-capacity extraction: a near-full store waits for physical drainage before that whole step fits. Continuous proportional extraction/endpoint integration and interrupted accounting durability remain open; this migration does not claim to finish them.

Power fuel is an ordinary Consumed ResourceInventoryIntent with explicit owner/cause. NoStock stops generation; malformed admission/changed commit throws rather than consuming a prefix. Original transaction stock/receipt/flow ownership remains. Existing cash charges/business behavior are untouched and still pending optional-module migration.

Power connectivity reads detached NetworkConnectionSnapshot endpoint identities via INetworkDiagnostics; no mutable pool or concrete line service. Range checks read placed ITilePresence in the selected space and use original clipped rectangle expansion. Domain power balancing itself remains pending native utility migration; no broader generic utility completion inferred.

Existing ContractIdentity class/Validate/ValidateId become public in place. Native and external readers share the same stable ASCII identity and positive-schema grammar, with no copied validator or arbitrary positive-schema cap. Core2.536.0; save154/replay1 unchanged.

## 3. API surface

Public ContractIdentity.Validate(string,int,string) and ValidateId(string,string), original implementation. Existing IResourceTransaction/INetworkDiagnostics/presence APIs unchanged. No new transaction, scheduler, codec, stock store or host API.

## 4. Files

Framework Kernel/ContractIdentity.cs, Kernel/SaveGame.cs. Oil existing Operations.cs. New PublicContractIdentityTests.cs and independent ContractIdentityProof.cs; Program/protocol/tracker/evidence.

## 5. Error handling

Whole output capacity/settlement waits do not deplete reservoirs. Fuel shortage never spends promises or a partial bill. Unexpected transaction/provider refusal stops through normal engine fault handling. Missing/disconnected network facts are not invented; physical range remains a separate existing eligibility rule. Malformed IDs/schemas refuse through the original grammar.

## 6. Compatibility

Direct development migration; FreeSpace/TryConsume/LineService and private copied identity validation are not restored. No legacy shim.

## 7. Verification

One scoped Oil compile4.24s: exit1,0 warnings,24 errors, reduced from34; Operations.cs has zero diagnostics. Focused framework compile11.94s clean. ONE NEW exact public identity case52/0/0 in19ms/outer218ms. This case validates authoring grammar only, NOT Oil operation runtime or transaction behavior. No old/family/full test runtime.

## 8. Risks

Oil still cannot compile/load: founding8 and composition16 errors. Whole-step backpressure, fuel consumption, mixed products, optional generation settlement, topology changes, partial native provider faults, domain-accounting interruption and full physical scenarios need focused gameplay evidence after integration compiles. Domain custom power and optional economics remain incomplete. No broad acceptance gate closes.

## 9. Out of scope

Tycoon rule changes, new economies/markets, platforms/Draw/Unity/Godot, hosts, old sibling engine, full regression or publishing/commit/push before full goal and plan finish.

## 10. Execution outcome

Operations resource/network/presence calls migrated; sole identity owner made public and proven from an independent assembly. Compiler errors34 to24. All original221 criterion texts/status/line anchors unchanged (193 recorded checked,28 open); goal remains open.

[Evidence](../../docs/migration/P5_OIL_OPERATING_RESOURCE_CONTRACT_VERIFICATION.md).
