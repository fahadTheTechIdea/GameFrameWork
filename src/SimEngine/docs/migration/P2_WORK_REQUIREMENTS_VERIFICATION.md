# P2 authored work requirement providers — 2026-10-01

P2 remains in progress. This slice provides the generic, nonconsuming requirement-provider seam.
It does not complete the activity DAG, alternatives, resource/tool reservations or framework extraction.

## Implementation and authority

- Core/WorkRequirementResult.cs contains client-safe readonly status, blocker and optional measured
  quantity/unit facts. Standard/WorkRequirements.cs contains the engine-only context and public
  IWorkRequirement/WorkRequirementDef contracts. A module registers concrete authored definitions
  through its existing owned content sections, not a separate provider registry.
- ResolvedContent extends its existing indexes to intermediate definition bases. These entries alias
  the same definitions; they do not duplicate data or Resolve calls. Shared-base identity collisions
  refuse content; concrete Index values retain their concrete table position.
- StepDef reads JSON requirement IDs, resolves them through the shared authority and exposes immutable
  lists. WorkBoard.CheckPlan validates generated references and dependencies before order allocation
  and during restoration. Redundant LinePlan/DemolishPlan internal dependency calls were removed.
- One WorkBoard evaluator drives WorkSystem and WorkView. Guards run before material/cash consumption
  and during timed/staffed progress. Pending/blocked work preserves progress. Invalid runtime references
  fail the order; malformed contract results fault explicitly rather than fabricate readiness.
- JobState.Paused is appended to the existing enum and remains a live assigned job. It uses the existing
  WorkBoard.Sync/hash owner, keeps the machine reserved and is released by normal cancellation. Worker
  state sources report stopped; entity Update remains the transition owner. Changing guards during
  travel does not teleport/freeze the worker; it arrives physically and waits before work progresses.
- WorkView previews and per-entry diagnostics return detached company-private facts. Preview request
  validity is distinct from execution readiness and normal command validity. Pure previews neither
  reserve, spend nor change hashes. Providers are contractually deterministic and nonmutating; arbitrary
  module code is not sandboxed by this API.
- Non-friend ExtensionProof implements an authored site-permit guard, a cash-reserve guard using the
  declared economy, and explicit malformed-provider fault injection. Its stateful permit owns one
  component codec and is changed through an engine-side command. No host/demo/harness gameplay changed.

The runtime retains friend access for white-box OilCo.Tests and Beep.SimEngine.Tests instrumentation;
OilCo.Game and ExtensionProof have none. The ordinary-game extension proof must not rely on test-only
privileges. This gate is not a claim that every oil regression uses only public client APIs.

Engine build is beep-simengine-2.5.0. Save format remains 3: no binary layout changed; Paused is a new enum
value and providers resolve from fingerprinted content. Earlier builds refuse; there is no compatibility
codec. Requirement results are observations, not persisted caches or alternative state authorities.

## Gates and source identity

```powershell
# Framework root
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- Requirements -v
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- --all
pwsh -NoProfile -File docs/migration/VerifyExtraction.ps1
Get-FileHash src/Beep.SimEngine/bin/Debug/netstandard2.1/Beep.SimEngine.dll -Algorithm SHA256

# Oil active-v2 root
dotnet build SimEngine.slnx
dotnet run --no-build --project OilCo.Tests
dotnet run --no-build --project OilCo.Tests -- --all

# Original repository
git diff --check
```

Both solutions build with zero warnings/errors (final framework 2.29 seconds; oil 2.60 seconds).
Focused Requirements substring gate: **104 passed / eight suites**, zero failed, 0.2 seconds;
it also selects the existing nine-check composition service-requirements suite. The seven new suites
contribute **95** checks. Full framework: **591 passed / 59 suites**, zero failed, none skipped,
**0.5 seconds**. Existing assertions are retained.

An earlier five-suite version passed 570 framework checks and fast oil **155 / 18 suites**, zero failed,
**four long suites skipped**, 25.4 seconds. That fast run preceded the generated-plan dependency fix
and two additional suites; it is not the final-source oil gate. Final-source full oil (session **56692**)
completed: **186 passed / 22 suites**, zero failed, none skipped, **555.8 seconds**. Frontier, programmed
player, allocation-budget and storyteller long scenarios all passed. Combined current coverage:
**777 checks / 81 suites**, zero failures. No regression session remains running. The historical typed-role
full oil baseline is documented separately and must not substitute for this newer source.

Ownership verification passes: **121** dispositions/recovery hashes, **50** unchanged namespace-normalized
single-file C# transfers (declared P2 changes excluded), standalone dependencies and no original runtime.
Current executable identity: **146 files**, SHA-256
`17B81562BFCC2A2B3FEC1C7744264613D0F1D50CF051EF5095C829463E383E98`.
Runtime DLL SHA-256:
`EF571FAB7E573E14D630A18746403E6E8F92A492DED7B362D015C4CB716CACBB`.
Documentation is not included in the executable fingerprint. Git diff --check passes, with existing
line-ending normalization warnings only. Original dirty source and recovery archives were preserved.

## Failure found and corrected

The additional generated-plan gate initially returned **102 passed, one failed / eight selected suites**:
an external generated step had no resolved dependency indexes and faulted before a permit command could
receive an answer. StepDef.Make was public, but generated dependency resolution was only internally
accessible. WorkBoard.CheckPlan now performs that validation in the engine before posting and on restore.
Creators no longer call the internal helper. The corrected focused and full framework gates pass.
An earlier apply_patch move/re-add request was rejected before any edit; separate edits completed it.

## Remaining scope

The seam handles deterministic nonconsuming company/target guards. Worker-specific requirements,
alternative provider selection, atomic consumable/tool reservation and arbitrary graph authoring remain
P4. Stable operation/signal/response providers and complete event/state/view ownership schemas remain P2.
P3 configurable time and optional spatial worlds are not implemented; inherited tile-map limits are not
declared acceptable framework policy. No Tycoon, renderer/platform, Git initialization, remote or package
publication changes occurred. Seven passing suites do not close P2–P6.
