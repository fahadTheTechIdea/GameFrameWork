# P9-D5d: Actual-port stock and export policies

Status: native scoped implementation/focused verification complete; full P9/P1-P12 OPEN.
Core2.319.0/save153/logistics19/policy3/logistics-view33. Final133 checks in1811ms;
nine clean builds, final5.58s. [Evidence](../../docs/migration/P9_SCOPED_STOCK_POLICIES_VERIFICATION.md).

## 0. Problem statement

LogisticsStockPolicies still reads only entity-local IInletPort, despite the original
Demand/Job records already naming physical scoped stores. LogisticsPolicyRules likewise
assumes a local inventory. Named-only stores cannot participate in strategic stock control.

## 1. Goals

1. Extend the existing stock-policy API, commands and private previews with an explicit
   port and optional original endpoint. Aliases of one actual store share one stock row.
2. Calculate hysteresis, protection, aggregate maxima and incoming promises against that
   actual endpoint. Retain original age and physical acquired trips on policy edits.
3. Generalize the original singular IOutletPolicy owner to explicit port bindings, authored
   through reusable JSON profiles. Never apply a local policy to unrelated scoped stock.
4. Make native resource control actions address these same policies through normal commands.
5. Save/hash/replay all actual intent through the existing owners and migrated schemas.

## 2. Design

ILogisticsStockPolicies gains optional Code port and ResourceEndpointId? endpoint arguments.
One native Demand per actual endpoint/resource owns bands, replenishing state and original
source age. Explicit endpoint management can edit original stores after selection changes.
New physical requests use the original named binding; acquired jobs retain their admitted IDs.
Existing receipt books own promises. No policy balances, duplicate inventory or dispatcher.

IOutletPolicy remains one entity capability owning settings for authored ports. Native
logistics_policy authors a nonempty profiles array of {port:null|string, profile:identity}.
Null explicitly addresses local stock; named entries address declared IResourcePortDefinition.
The existing pure named resource binding validates actual metadata and capacity. Settings
commits use the original policy contract and exact entity/provider guards.

## 3. API surface

Existing ConfigureStockPolicyCommand/ConfigureOutletPolicyCommand and LogisticsView methods
gain port/original endpoint identity where applicable. Existing stock/outlet control actions
publish detached stable policy bindings, resolve them freshly, and create the normal commands.

## 4. Files to change

Standard/LogisticsStockPolicies.cs, LogisticsPolicies.cs, LogisticsDemands.cs, Logistics.cs,
HaulResourcePorts.cs, ResourcePorts.cs, ResourceControlActions.cs; native commands/views/module;
master-listed content and one thin bounded player case. Existing external policy proof migrates
its contract directly. No platform, Oil, Tycoon, clock or alternate owner.

## 5. Error handling matrix

Missing or incompatible ports refuse, foreign management is private, native fractional bands
and aggregate maxima refuse. Pure provider mutation faults; committed provider violations fault
without compensation. Selection changes never redirect acquired receipts or original policies.

## 6. Backward compatibility

None. Native component/command/save/view identities migrate directly; no old reader or wrapper.

## 7. Verification

Compile with dotnet build Beep.SimEngine.slnx --no-restore. Run only one NEW exact bounded case
for scoped bands, aliases, export controls, physical supply, edits, saved twins and replay.
All historical cases compile only. Full regression remains open pending user authorization.

## 8. Risks

Aliased stores must not double maxima or receive a second band owner. Capacity checks aggregate
all resources by actual endpoint. Immutable intent cannot authorize later mutations. Original
policy bindings require explicit management after current port selection changes.

## 9. Out of scope

Full P8/P9/P1-P12 completion, broad provider/stress/recovery/dock cases, platform rendering and
publication. Commit/push require full goal AND plan completed and verified.

## 10. Execution order

1. Generalize the original native policy/binding owner and shared admission algorithms.
2. Migrate normal commands, views, JSON and resource control action targets.
3. Add one bounded public-command player case; repair only that case if necessary.
4. Record actual evidence and update the master tracker/completion audit honestly.
