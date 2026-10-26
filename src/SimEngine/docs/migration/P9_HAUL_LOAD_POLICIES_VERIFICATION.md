# P9-C6 haul load policies — scoped verification

Date: 2026-10-06. Framework only. Build **beep-simengine-2.107.0**, save **65**.
[Stage](../../plans/enhancements/P9_HAUL_LOAD_POLICIES_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Ownership and target design

IHaulLoadProfile/HaulLoadProfileDef supply immutable selected content and a pure admission hook.
Native haul_load_profiles require explicit enabled, minimum_load resource object, rational maximum_wait
and emergency_bypass in master-listed JSON. BillSpec remains the resource parser; actual catalog/native
quantum/capacity and session Time.Ticks remain authoritative. Duration rounding is the existing
up-to-session-quantum conversion, not an invented wall-clock deadline. Each resource minimum is an
alternative threshold, not a simultaneous cargo bill. Explicit zero wait permits positive smaller loads.

IHaulLoadPolicyDefinition binds initial actual profile metadata. IHaulLoadPolicy owns selected content
identity only. Native haul_load_policy binds its disabled state and stores one profile ID through a
staged codec; it does not copy profile settings, inventory, timers or enabled state. Engine checks
actual session aliases/compatibility and scoped pure reads/admission; exact commits must touch the
actual entity without changing owner/time/identity and select the requested profile. Missing, foreign,
invalid or no-op providers fault explicitly. Scoped checks are not global rollback or process sandboxing.
Entity.Update alone settles the native authored disabled-policy entity state.

The existing LogisticsService Preview/BuildQueue/TryAcquire path applies the policy to each automatic
carrier after packing real quantities and re-evaluates before actual native job/claim acquisition.
A waiting carrier does not exclude another ready candidate. No stock, carrier, fuel, role, area,
handling, route, dock or capacity rule is bypassed by deadline expiry. Acquired shipments are not
revoked by later profile changes. Direct explicit engine-side IHaulExecution hauls and physical
return/evacuation are not automatic standing-load batching; no new player-side scheduler is introduced.

Existing route/demand rows own LastDispatchTick initialized to CreatedTick. Successful standing
admission advances it on the route or matching active destination/resource demand rows. Original
fairness age and surviving demand updates remain unchanged. Private offers expose current history
and derived WaitAgeTicks. Failed previews/acquisitions create no wait timer or resource claim.
This is request-level time since creation/last dispatch, including disabled/no-supply time, not a
ready-only episode or per-carrier stopwatch. New positive gaps can therefore use already elapsed
request wait; this behavior is explicit rather than silently promising a fresh full interval.

ConfigureHaulLoadPolicyCommand and private LogisticsView read/preview use the same engine protocol.
Native and independent public policy/profile providers share the same matching/claim owners. The
obsolete proposed umbrella fleet facade is removed from the active P9 design; remaining fleet
settings must extend the actual narrow policy/motion/service responsibilities, not mirror them.

Load policy1/logistics service7/logistics view12 identify current ownership. Save65 directly revises
command/history layout, with no old reader or conversion. WorkBoard22/work system6/movement4 and other
owner payload layouts remain unchanged.

## Final focused commands and results

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Haul load policies: authored contracts, deadlines and canonical admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Haul load policies: physical batches, dispatch history and saved continuation"
```

- Build succeeded: zero warnings/errors, elapsed2.96 seconds.
- Authored contracts, deadlines and canonical admission: **63 passed,0 failed**,372ms.
- Physical batches, dispatch history and saved continuation: **59 passed,0 failed**,372ms.
- Total **122 checks,0 failures,0 aborted suites**, exactly two distinct full named suites.
- Only same-name repair/final reruns. No third, family, preceding or full suite ran.
- Initial restore failed because native content lookup was assigned only in OnCreate. Removed that
  creation-only field; both new and loaded instances now use the resolved definition's actual content.
  Initial test timing assumed dispatch ran on every command tick; corrected to bounded engine cadence.
  Runtime provider faults were initially expected during composition; corrected to exercise the actual
  guarded runtime reader. No engine fallback, changed deadline or weakened contract was introduced.
- One expanded proof build failed on an incorrect JsonValue.Null method reference; corrected it and
  rebuilt before final proof. No-build output after that failed build is not current-source evidence.
- 46 preceding assertion files migrate/compile only:41 current build assertions2.106.0→2.107.0,
  41 save assertions64→65 and3 logistics schema assertions6→7 (some share source lines).
  No current regression result is claimed for these earlier suites.
- No old2.106.0 identity, save64 assertions or logistics6 assertions remain in source/test C#.
  Historical documents retain their original identities and results.
- Scoped tracked-file whitespace and selected-files' trailing-whitespace checks passed (ordinary
  LF/CRLF notices only). All14 source fingerprints below matched the final verified source.

## Proven scope

Strict required fields/null rejection, actual discrete quantum/capacity/time validation, typed command
refusal parity, company-private frozen pure snapshots and wide three-billion-tick authored waits pass.
Deadline just-before/exact boundary, explicit zero wait and native emergency/disabled rule behavior
pass at the profile contract. The broad physical emergency integration matrix was not run.

Independent public policy providers and an authored odd-load profile run through the unchanged
dispatcher. The alternate rule waits for an odd native load, without modifying the scheduler.
Ready standby candidate selection remains available when another carrier waits. Four public fault
modes (mutating/missing read, no-op commit, mutating admission) fail explicitly.

Unknown/truncated native profile decoding rejects before assigning a new selection. Future native
dispatch chronology rejects. Existing whole logistics decoding is not claimed globally transactional.
Saved pending normal profile commands retain admission/continuation identity.

Both native and independent public policy providers complete real full and smaller batches through
ordinary jobs, stock receipts, loading, travel and unloading. After first full delivery, a smaller
request waits; deadline admission carries two actual units, not a fabricated four-unit minimum.
Changing to disabled while loaded preserves the acquired trip. Restoring batching afterward proves a
new request waits from the second successful dispatch, not ancient route creation.
No cargo is duplicated or delivered remotely; source and destination quantities reconcile exactly.

Ordinary stock requests use the same load admission. Saved wait/history, positive small delivery,
subsequent stock-policy edits retaining original age/last dispatch, new short-gap waiting and a second
physical delivery produce identical saved continuations. Interleaved sessions remain isolated.
Every physical stage has a250-tick timeout with current tick/position/worker/job/state/blocker/source/
destination diagnostics. Public proof capabilities own no cargo, receipt, job, wait clock or scheduler;
tests author commands and observe engine outcomes.

## Remaining work

Home depots/preferred routes/service intervals/named tanks or holds, proactive travel/fuel budgets,
congestion and ready-only wait episodes remain open. Broader emergency/fuel/dock/preemption/shared-demand/
public-provider/purity/geometry/replay/lockstep/fuzz/stress/allocation/full acceptance/full P9/P1–P12
are not verified by these two suites. No OilFieldDays/Tycoon/host/demo/Unity/Godot/Draw code changed.
Full framework goal remains active/partial.

## Exact source fingerprints

These identify this focused proof, not a repository-wide or full-regression verification.

| Source | SHA-256 |
|---|---|
| `src/Beep.SimEngine/Standard/HaulLoadPolicies.cs` | `275713f096213c013deea1bd266f750d9074cefffffbb4514ee84aaa8e9ca81d` |
| `src/Beep.SimEngine/Commands/HaulLoadPolicyCommands.cs` | `cb536c8ceefc4054a02f74d1f28b345afcb6eb291d508c80a63990bd5b209b9d` |
| `src/Beep.SimEngine/Standard/Logistics.cs` | `6ec89d078f40484d108c19960786c8d1a743c79bcdf592454dedf29a7f9f31f8` |
| `src/Beep.SimEngine/Standard/LogisticsScheduling.cs` | `538e013c527dbfafe026447eb05fe1b4c5cb1b6f7598a73556a3adabfbba7139` |
| `src/Beep.SimEngine/Standard/LogisticsStockPolicies.cs` | `9b53374f748a0584bbf93e1dfa500b72b63e2ec6cc899794e98891755269cf7a` |
| `src/Beep.SimEngine/Standard/LogisticsModule.cs` | `0b7188302be8facff18ff4d845fa77373e314a8808eb212cc2f42f0c6658c402` |
| `src/Beep.SimEngine/Views/LogisticsView.cs` | `051593f044a67d151a283fbd75d13d98b0fe1c8135b851b40588fa6943be3584` |
| `src/Beep.SimEngine/Kernel/SaveGame.cs` | `73e3aaa9b8b3ae296cfd300cf7944df8f6f56c2116c81270755988a386a7c005` |
| `tests/ExtensionProof/HaulLoadProof.cs` | `66861803617ac1df0c2f0bfe8d874bb704fe115d6cdf47c2a9a334d1b1e8c576` |
| `tests/ExtensionProof/PointSpaceProof.cs` | `e7c2001808394c452832b878d2e20ea2f82fbb96d7d183b1be73742e88dc5290` |
| `tests/ExtensionProof/ExtensionProof.csproj` | `2e7539778a99a0d307296cb1a934a6b74afbdeddcd6135cf8bddb16ca2049f5a` |
| `tests/ExtensionProof/content/haul-load-policies.json` | `ef7fc4b33d8b302ee5b4731cf3ab07056d9cfe706c347d8c7646f30c9fc4e676` |
| `tests/Beep.SimEngine.Tests/HaulLoadPolicyTests.cs` | `fa899c68cc970dae0eb724f2f7da8f15ee35a9f5464811537df8ab8c854f1670` |
| `tests/Beep.SimEngine.Tests/Program.cs` | `4fb924e4f002da64ec667d6696e2e9d02e7e0a35a0278f18540e2e64d4c33cac` |

