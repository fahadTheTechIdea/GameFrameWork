# P9-D5f: Actual-endpoint recovery and same-site automatic supply

Status: native slice focused-verified; full P8/P9/P1-P12 remain OPEN.
Core2.322.0/save153/logistics20/logistics-view35. FIRST100 checks in1717ms;
ONE clean15.41s build. Named reroute source/compile-only.
[Evidence](../../docs/migration/P9_SCOPED_RECOVERY_MATCHING_VERIFICATION.md).

## 0. Problem statement

Logistics.PreviewSource excludes the entire requester site, so two distinct actual stores at one site
cannot automatically supply each other. PreviewDestination scans only IInletPort components, and
recovery demand priority derives an entity-local endpoint. Recovery offers omit selected port facts,
continuing preview rejects named destinations, and replacement receipts use entity-local inlets.

## 1. Goals

1. One existing public-port enumeration serves automatic source and recovery destination selection.
2. Refuse actual endpoint self-transfer, not valid distinct stores sharing a site.
3. Native recovery policy receives exact source/destination endpoint and named destination facts.
4. Original work demands/bands determine recovery gap/priority by actual endpoint across aliases.
5. Recovery dispatch and acquired jobs retain selected names/IDs; selection edits do not move receipts.
6. Cargo return acquires its complete original cargo/destination set through the resource transaction.
7. Explicit replacement uses the original old endpoint and exact admitted replacement.
8. Verify bounded physical same-site supply, residual cargo return and local evacuation to named stores
   through one NEW exact case. Older suites remain compile-only, no broad/full runtime.

## 2. Design

HaulResourcePorts owns deterministic visitation of an optional local inlet followed by authored named
ports in ordinal order. It guards actual identity/company/capacity/version and entity purity. Public
exposure and compatibility are required. The existing source/destination selectors consume this one
enumeration; no port registry, inventory, claim book, scheduler, query state or clock is added.

Recovery compares actual endpoint IDs, allowing alternate stores at the same site and excluding the
current committed destination even through another alias/site. Its existing preference/travel/site
ordering adds ordinal port tie-breaking. Native policies use detached actual endpoint facts and
unchanged bounded native quantity rules. Refusals expose selected named/original provenance.

Existing LogisticsDispatchOffer and TryAcquire carry admitted names/IDs for recovery exactly as for
normal supply. Whole cargo-out/destination-in claims use IResourceTransaction.AcquireClaims.
Replanning resolves the original destination via ReadHaulEndpoint and the new endpoint via PhysicalPort;
it preserves remaining cargo and native itinerary/progress. No replacement of an inaccessible original
with current selection. Missing endpoint breadth is not claimed by source migration alone.

## 3. API surface

RecoveryDestinationFacts adds required SourceEndpoint, DestinationPort and DestinationEndpoint.
No overload or compatibility shim. Existing external/native policies consume the same facts.
No new public gameplay command or host logic. Core identity, logistics semantics and view schema advance;
saved layout and sole GameClock remain unchanged.

## 4. Files to change

Native owners: Standard/HaulResourcePorts.cs, Logistics.cs, LogisticsStockPolicies.cs,
LogisticsRecovery.cs, RecoveryDestinationPolicies.cs and LogisticsModule.cs; Kernel/SaveGame.cs.
Proofs: one normal scoped-recovery JSON/master, composition/resource registration, one NEW thin exact
test case; migrate the existing detached-facts test call and current protocol declaration compile-only.

## 5. Error handling

Actual self-transfer refuses before claims. Incompatible/nonpublic stores are not automatic candidates.
Disabled/no-demand recovery remains typed and physically waits. Original bindings must resolve exactly;
provider mutation/foreign metadata faults normally. No fallback inventory or fabricated cargo.

## 6. Compatibility

Development migration only. No old reader, duplicate subsystem or compatibility overload.

## 7. Verification

Compile Beep.SimEngine.slnx without restoring. Run only the NEW exact bounded case with a15second
process deadline and1000-Update physical stage bounds. Repair/rerun that same case only.
Save/load twins and replay; public preview purity/private facts and physical stock accounting.
Named reroute/dock/removed endpoint/provider-fault matrices remain open unless actually exercised.

## 8. Risks

Shared-site docking must still perform native timed pickup/delivery, never instant stock transfer.
Alias policies must remain independent while stock/demand/gaps remain owned by actual endpoints.
Do not broaden named evacuation source policy; existing local evacuation intent is retained.

## 9. Out of scope

Scoped evacuation-origin authoring, removed provider endpoints, new transport media, full docks/areas/
emergency/provider/stress/allocation/global accounting/lockstep/full durability; Oil/platform/Draw,
business expansion, broad historical/full runtime and publication before full goal completion.

## 10. Execution order

Read native owners; implement shared enumeration/selection/facts; migrate actual recovery acquisition
and original replacement binding; author normal master JSON and thin player test; one focused build/
case; record exact evidence and refresh tracker/audit without promoting full acceptance gates.

