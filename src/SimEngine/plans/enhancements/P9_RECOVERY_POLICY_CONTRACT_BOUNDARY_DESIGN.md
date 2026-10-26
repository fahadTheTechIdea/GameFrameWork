# P9-R1: Direct recovery policy contracts — Design

## 0. Problem

Recovery runtime already consumes `ILogisticsRecoveryPolicy` and `IRecoveryDestinationPolicy`, but native JSON and two external proof definitions still inherit mandatory family bases. Shared destination decisions also refer back to a concrete JSON registration for blocker identities. This leaves a native-authoring dependency in the reusable contract path.

## 1. Goals

1. Delete both obsolete bases; native and external definitions implement contracts directly on `Def`.
2. Keep strict file readers and quantized decisions in their existing declarations, with no compatibility aliases or second recovery coordinator.
3. Prove physical network cargo interrupted by a player remains cargo, waits for explicitly authored demand and returns through normal timed engine work.
4. Exercise native and independent tile geometry and native and independent policy metadata, with partial saves, pause, privacy, replay and conservation.

## 2. Design

`AuthoredLogisticsRecoveryProfileDef` resolves the existing `LogisticsRecoveryProfileDeclaration` directly. `AuthoredRecoveryDestinationPolicyDef` implements `IRecoveryDestinationPolicy` directly. Move unchanged no-demand/quantum-gap blocker identities to `RecoveryDestinationDeclaration`, which owns those decisions. External proof definitions follow the same contracts/readers.

Extend the existing independent tile work composition with one explicit master-listed recovery document. It disables evacuation and selects demand-only residual cargo recovery; no hidden profile or fallback is activated. The native coordinator retains its original jobs, stock, receipt books, chronology and clock. A thin player driver physically loads existing pipe stock, interrupts its carrier, waits without demand, then configures a receiving stock band and releases the carrier. Hosts do not choose the return destination or manufacture recovery state.

## 3. API/state

Delete `LogisticsRecoveryProfileDef` and `RecoveryDestinationPolicyDef`. Consumers use existing contracts. Blocker strings stay unchanged; no aliases. Engine identity2.590.0; save157/replay1 unchanged because no persistent payload changes.

## 4. Files

Recovery policy/declaration files; external recovery proof definitions; mechanical in-scope test callers; independent tile composition; one JSON document/embedding; focused physical recovery test/registration; tracker and verification record.

## 5. Errors

Missing destination references and invalid authored modes still refuse through existing declaration/composition admission. Demand-only recovery reports its unchanged typed no-demand/quantum-gap blockers. Interruption never discards stock or claims instant delivery.

## 6. Compatibility

Development-only contract migration. Old bases and concrete family lookups are removed, not retained through shims.

## 7. Verification

Targeted compile7.01s clean, followed by two exact physical cases288/0 each. The existing exact recovery content-contract case initially97/3 because of stale build/save/private-refusal assertions; those expectations were migrated without changing production behavior. Repair compile4.48s clean and same exact case101/0. Total677 distinct successful focused checks. [All attempts and scope](../../docs/migration/P9_RECOVERY_POLICY_CONTRACT_BOUNDARY_VERIFICATION.md). No full/profile/family execution.

## 8. Risks

Endpoint destruction during delivery, dock queues, arbitrary mod callback purity, mixed resources, allocation/lockstep/fuzz/soak and global acceptance remain broader open gates.

## 9. Exclusions

Oil/Tycoon/platform/Draw/host gameplay, second recovery/work/clock owner, instant delivery, full regression and Git publication.

## 10. Status

Contract migration and scoped physical integration verified. Both providers and both native/direct metadata registrations physically load network cargo, retain it after player interruption, wait without demand, then perform automatic timed return and partial unloading through original engine owners. Full goal and wider recovery gates remain active; no original broad requirement is closed by this slice.
