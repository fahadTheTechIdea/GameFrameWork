# P3-S40 — Physical stocked corridor dismantling and recovery

## 0. Problem

Shared geometry has physical empty-corridor construction and stocked protocol split/removal proofs, but actual native/independent player dismantling, cargo evacuation and final occupied-segment refusal remain unproven together.

## 1. Goals

1. Populate an authored initial network through ordinary rate-limited resource ports, never direct pool stock writes.
2. Use ordinary staffed work to split a stocked corridor, preserving materials and original pool ownership.
3. Refuse removal of stocked final segments, physically haul stored resources through connected port contracts, then dismantle empty segments over game time.
4. Preserve partial saves, pause, replay, private facts and interleaved session isolation on both providers.

## 2. Design

One master-listed JSON add-on selects an authored existing three-segment pipe, a six-unit feed inventory and native push connection, three nonblocking removal worksites and six-minute staffed removal. Existing line endpoint entities gain native connected resource ports with minimum membership1. The existing stock/flow/geometry/work/hauling owners handle all decisions and state.

Positive tests only submit incident-free work/haul commands, drive Update/clock and read immutable pool/work/inventory views. Initial completed terrain is scenario authoring, not a construction bypass or claim that rebuilding is instant; P3-S34 proves physical construction separately. No synthetic recovery stock, salvage grant or duplicate removal scheduler is introduced.

## 3. API/state

Existing contracts and authored modules only; engine2.589.0/save157/replay1 unchanged unless verification discovers an engine defect.

## 4. Files

Existing independent work composition; explicit stocked-removal JSON and embedding; new focused physical stocked removal tests/registration.

## 5. Errors

Original occupied-network, ownership, disconnected/removed endpoint and material/worker/route refusals remain authoritative. No instant draining, teleporting or deleting stocked pools.

## 6. Compatibility

Uses current contracts only; no old fixture composition or parallel model.

## 7. Verification

Targeted compile clean6.17s. Separate exact native/independent cases174/0 each:348 focused checks. [Evidence](../../docs/migration/P3_PHYSICAL_STOCKED_REMOVAL_VERIFICATION.md). No full/profile/family run.

## 8. Risks

Multiple companies, competing reservations, mixed quality/resources, cancellation/replanning/failure recovery, all shapes and global lockstep/fuzz/soak/allocation remain broader open gates.

## 9. Exclusions

Business/Tycoon/Oil/platform/host logic, instant recovery, changing authored amounts/timing to bypass engine rules, full regression and Git publication.

## 10. Status

Authored ordinary physical split/evacuation/empty-segment dismantling verified on both providers. Wider promise/mixed-resource/fault/lifecycle/global matrices remain open; full goal active, no original broad checkbox closes.
