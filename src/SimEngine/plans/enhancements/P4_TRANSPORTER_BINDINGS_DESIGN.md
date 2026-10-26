# P4-G29 — Contract-owned transporter bindings

## 0. Problem

Transporter inherits Component<TransporterDef>, so independent JSON metadata cannot bind the
original stateless carrier. ResourceHandlingProfileDef is redundant abstract definition inheritance.
Original HaulHandling checks a runtime profile but does not compare it with authored metadata,
and profile alias getters run outside its actual-provider observation.

## 1. Goals

1. Independent ITransporterDefinition binds the original Transporter and its empty codec2.
2. TransporterDeclaration is the sole explicit handling-reference reader/resolver and native
   inventory/spatial binding validator.
3. Generic composition checks every loaded runtime/metadata pair and actual handling alias before
   Initialize, including unused entities, through contracts rather than native part lookups.
4. Keep provider-specific layout requirements in the pure definition ValidateBinding contract.
5. Physical progressive loading/travel/unloading remains on existing jobs, stock, receipts and clock.

## 2. Design

Standard/Capabilities.cs adds ITransporterDefinition. Handling returns the actual owned content
provider; ValidateBinding(EntityDef owner) validates that implementation's authored requirements.
TransporterDeclaration reads explicit handling, validates its identity and resolves
IResourceHandlingProfile. Built-in and independent metadata delegate the sole declaration.
Native local-inventory/spatial requirements remain in that declaration; the generic validator
does not impose them on every external cargo implementation. No new worker/motion requirement.

Transporter becomes Component consuming its actual ITransporterDefinition. Cargo still returns
the same entity's existing IInventory. No cargo balance, receipt, capacity, worker or timer fields
are introduced. AuthoredResourceHandlingDef directly implements IResourceHandlingProfile;
the redundant abstract ResourceHandlingProfileDef is deleted.

Original LogisticsService.ValidateComposition invokes HaulHandling.ValidateDefinitions. It checks
all loaded part/runtime metadata pairs, invokes the definition's pure binding validator and observes
actual metadata/profile reads before catalog alias validation. This occurs before the existing
selected-space policy filtering, so unused/other-space bad declarations do not escape checks.

HaulHandling.Check observes the actual transporter, checks matching runtime/metadata and equal
actual Handling references, then validates owned profile identity inside the original profile guard
before CheckHandling. Original Earned native-prefix calculation remains unchanged.
LogisticsService/WorkBoard own dispatch/claims/cargo/handling progress; actual inventory owners
retain stock and promises. GameClock is unchanged. No new loop, coordinator, registry or resource model.

## 3. API and state

ITransporterDefinition: Handling and pure ValidateBinding(EntityDef).
TransporterDeclaration: Read/Resolve/ValidateBinding.
Core2.365.0/save154/replay1. Native transporter empty codec2, job/stock/view schemas unchanged.
No legacy base, compatibility accessor, default handling profile or instant service fallback.

## 4. Files

Capabilities.cs, ResourceHandling.cs, Logistics.cs; original reserve-transporter metadata and
handling proof diagnostic compile migration; existing thin handling authoring/player helper extended
in place; new TransporterBindingProof/TransporterBindingTests; Program and current protocol.
Stage, evidence, master/current checkpoint docs and completion audit updated together.

## 5. Errors

Missing/null/empty/wrong-kind references, bad capability binding, missing or lying metadata/runtime
pairs and unused null metadata refuse before Initialize. Impure runtime metadata callbacks and foreign
same-ID content fault at the original observation boundary before facts/time/receipts can be authorized.
No rollback, fabricated profile, partial healthy checkpoint or semantic substitution is introduced.

## 6. Compatibility

Development-only, no compatibility paths. Existing independent reserve definition now declares the
actual transporter metadata contract and delegates the sole shared declaration.

## 7. Verification

ONE NEW bounded exact case:
Transporter bindings: authored carrier contracts and physical handling without native metadata.

Native and independent metadata drive the SAME transporter. Independent authored handling uses
loading12/unloading8 minutes for quantity8, resolved at60- and30-second game quantums.
Ordinary haul/policy commands produce real approach, native loading prefixes, real delivery travel,
unloading prefixes and conservation. Partial phase saves, sole-clock pause, saved twins, replay,
retained/private facts and an untouched interleaved session are checked.
20 invalid JSON/contract/binding declarations refuse before Initialize. Eight view/normal-Update
fault combinations cover exception, Touch, pause and identity allocation; one same-ID foreign
handling catalog reference refuses. Unused carrier without worker/motion remains valid content.

Status: narrowly verified. Two clean builds6.92s/5.54s; only this new case executed twice.
First577/0/0 in1005ms; final577/0/0 in955ms.
[Actual evidence](../../docs/migration/P4_TRANSPORTER_BINDINGS_VERIFICATION.md).

## 8. Risks/open gates

Alternate remote/service cargo semantics are enabled by provider-specific binding validation but not
proven by this native-layout physical case. All endpoint/cancel/recovery/quantum/tool/dock/fleet/team/
network/stress/concurrency/lockstep/fuzz/soak/allocation/performance and full P1-P12 acceptance remain
OPEN. Scoped observation is not an arbitrary CLR/world sandbox or cross-provider hidden-mutation proof.

## 9. Out of scope

Pending excluded Tycoon/catalog scope, money/business defaults, domain/platform/Draw changes,
historical/family/full/Oil/Tycoon/platform runtime and staging/commit/push.

## 10. Order and source review

Read original content/cargo/handling/dispatch → migrate actual metadata/runtime and sole declaration →
guard original handling aliases → new thin proof/compilation → one bounded exact check →
review generic extensibility → move provider-specific requirements into the contract →
same new case final gate → evidence/tracker/audit. Full goal remains OPEN.
