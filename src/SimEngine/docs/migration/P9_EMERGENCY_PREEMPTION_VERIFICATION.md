# P9-B4b1 — Emergency activity preemption verification

Date: 2026-10-05. Current working source **2.67.0/save39/work18/work-view22**.
Existing work/native/logistics4/logistics-view8/transporter2/flow4/company-alert1 codecs unchanged.
[Stage](../../plans/enhancements/P9_EMERGENCY_PREEMPTION_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Exact focused gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Emergency preemption: strict policy, pure candidates and protected execution"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Emergency preemption: physical interruption, retained inputs and saved continuation"
```

Final build: **5.66 seconds**, zero warnings/errors. Policy: **114 passed / 494 ms**.
Physical: **60 passed / 457 ms**. **174 checks / two exact suites / zero failures / zero aborted
suites**. Summed reported suite time: 951 ms. Only those two distinct full names ran/repeated;
no third/family/broad/full suite. Seven older test identity literals migrated and compiled only.
Historical counts are not current broad regression evidence.

Earlier focused runs exposed incorrect fixture expectations, not a demonstrated runtime defect:
resource-blocked response nodes need not create a dispatchable job; completed nodes clear their live
job relation; stock demand waits for its authored dispatch interval; actual unavailable ordinary
work follows its existing retry path independently of emergency refusal. Tests now assert those
authoritative states rather than imposing another execution model.

## Proven behavior and ownership

- Required authored preempt_emergencies refuses missing/nonboolean values. Explicit false, absent
  capability, excluded scope and independent profile veto retain ordinary execution. Both selected
  policy capabilities alias the same immutable content instance; no new registry/store/state codec.
- Required WorkCandidateMode migrates every existing caller, with no compatibility overload.
  Existing CopyCandidates/BuildQueue/RankOffer remain sole selection/projection/ranking owners.
  Raw native queue remains idle-only; combined assignment and actual response candidate views can
  project safe potential InterruptedJob. Pure reads preserve hashes, motion and actual worker/claim
  relations. Foreign company sees no private response/incumbent facts; retained offers stay detached.
- Engine safety independently protects noninterruptible work, disallowed emergency participation,
  missing role, closed route, missing resources, outside operating area, unavailable workers, actual
  hauls/loaded cargo and another emergency. An independent profile that accepts all interruptions
  cannot bypass haul/emergency safety. Eligible idle spare takes emergency without interrupting work.
- Actual TryAssign re-runs current eligibility, interrupts only a real same-company ordinary live
  activity, stops old motion, reopens that same job, then performs fresh idle-only native acquisition.
  Changed post-release availability faults normal Update; partial failed admission cannot save/hash.
  No virtual inventory, copied job/worker, compensation/refund or resumable fabricated success.
- Ordinary target-local work consumes eight startup units and holds two real native tool units.
  Interruption retains the spent eight, original job identity/age and earned progress; actual unspent
  hold releases. Resumption pays no additional startup bill. Both already-working and travelling
  incumbent cases physically repair a valid bound incident, then resume/complete the original job.
  Ordinary work earns no progress while the emergency owns its machine; takeover is not instant
  travel/repair. Saved before/after takeover and during emergency continue deterministically, while
  an interleaved untouched session remains unchanged.
- Native partially loaded hauling keeps the actual carrier, destination receipt and cargo under
  emergency pressure. It delivers exactly eight units physically; the pending response then repairs
  through ordinary timed work. Saved cargo and pending response continue identically.
- Independent public-only ShipmentDispatchProfileDef implements the optional contract via explicit
  JSON alongside its existing dispatch policy. No concrete-type switch, new scheduler/system or
  host/demo/platform gameplay. Sole existing node/job/native owners retain durable state.

## Breaking development boundary

Build/save/view identities advance together. Dispatch-profile JSON requires its explicit boolean,
candidate calls require mode, detached candidate constructor requires interrupted-job identity.
No old reader, overload, compatibility loader or parallel model. Proof content is explicitly embedded
and supplied through PointGame's existing master-listed document; no discovery/default authoring.

## Remaining gates

This closes B4b1 **ordinary activity takeover for actual bound emergency activities**, not all B4b.
Standing emergency supply/haul admission needs a subsequent same-owner preemption path. Automatic
active-haul interruption, loaded cargo handoff, ordinary-priority preemption and evacuation/return
arbitration are not implemented. Full alias/provider fault matrices, checkpoint corruption, replay,
lockstep, fuzz/soak/allocation, all-dispatch areas/fleet/services/congestion/replanning/plans and final
acceptance remain open. Existing retained native claims may block a response; the engine does not
pretend those claims were released in preview. No unconditional fairness/progress guarantee under
permanent blockers or endless emergencies. OilFieldDays/Tycoon/hosts/platforms/Draw untouched.
The full framework plan remains partial; only this scoped two-suite gate is confirmed current.
