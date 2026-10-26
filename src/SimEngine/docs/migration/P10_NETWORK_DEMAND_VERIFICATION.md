# P10-A6 — authored network demand and physical utility allocation verification

2026-10-06. Framework-only focused evidence, not full P10/P1–P12 completion.

## Source and ownership

- Build **beep-simengine-2.142.0**, save **76**, LineService **4**, infrastructure view **5**.
  Operations view9/utility signal1 remain unchanged. No persistent layout, scheduler, stock store,
  allocation balance, consumption owner, codec or current-time owner was added.
- Required networks[].demand_policy resolves registered NetworkDemandPolicyDef/INetworkDemandPolicy.
  Explicit capacity_network_demands retains selected capacity filling; utility_network_demands binds
  one required named IUtilityConsumer. All five current network fixture documents migrate in place;
  no default, compatibility reader or parallel old path survives. Build assertions migrate; other
  suites compile only. New proof document is explicitly embedded and master-listed by the fixture.
- Existing LineService captures actual connection legs, including empty compatible endpoints for
  mutation guards. Pure admission precedes time eligibility and arbitration. A bounded temporary
  NetworkDemandContext exposes detached identity, actual readiness/quantum and guarded consumer facts.
  It closes in finally, including faults. Policies cannot execute transfers or obtain mutable handles.
- Utility pull allowance is whole-common-quantum next demand minus actual unpromised contribution
  and inbound commitments. Pushes retain normal finite-stock capacity admission. The same policy is
  read immediately before each actual receipt; two networks sharing a consumer cannot overdeliver
  from stale candidate demand. Zero/blocked demand earns no elapsed eligibility or banked allowance.
- Physical stock, native timed draws, gates, earned-time cursors, selected solvers, success history,
  reservation receipts and resource output remain with their existing engine owners. Public-only
  independent consumer proof observes actual inventory; it does not claim a separate physical burn.
- Existing INetworkDiagnostics/InfrastructureView now expose ReadDemand, using the same actual leg
  capture and admission helper as execution. Existing quantity diagnostics validate private company,
  content, provider, topology and ownership first. Missing/inactive consumer blockers are typed;
  satisfied zero allocation is not a missing value. Pure observations change neither stock nor hash.
- Exactly one GameClock remains. Its source SHA256 is unchanged from A5. World constructs it once;
  StepIndex/Tick are projections. All allowance and native work elapsed facts derive from that owner.
- Captured participant/version and native quantum violations fault normal sim.lines execution; a
  faulted game cannot save. Earlier physical commits are not globally rolled back after a later fault.
  Trusted extension purity guards are not a security sandbox or unrestricted-world rollback guarantee.

## Completed verification

Final completed `dotnet build Beep.SimEngine.slnx --no-restore`: **0 warnings,0 errors**, MSBuild1.81s.
Build completed before final suites; no stale binaries or concurrent build/test.

| Exact complete suite | Passed | Failed | Reported time |
| --- | ---: | ---: | ---: |
| Network solvers: explicit contracts, lifetime and guarded execution | 92 | 0 | 412ms |
| Network solvers: physical selection, refusal and saved continuation | 90 | 0 | 520ms |

**182 checks, two suites, zero final failures/aborts.** Includes the existing solver checks, not an
additive claim. Repair reruns used only these same two names. No third suite, broad family or full
regression was run. A5 running-input146, B8 resource447 and B7e automation1069 are not rerun.

Proof covers mandatory/wrong/missing JSON binding, required consumer identity, independent admission,
actual native quantity bounds/quantum, blocked/negative quantities, context expiry/scope and faults,
mutation of source and empty receiver, native timed consumption and finite-stock conservation,
independent demand satisfaction, full/partial inbound promises and release, disabled/completed work,
private pure typed diagnostics, forged consumer identity refusal, shared-consumer cross-network late
rechecks, saved interleaved continuation, ordinary player commands and paused sole-clock identity.
All scenarios have bounded Update counts. Tests contain no copied allocation or consumption scheduler.
Scoped tracked diff whitespace checks and explicit new-source/stage/evidence whitespace scans pass;
Git emits only existing LF-to-CRLF notices. Actual authored network JSON all names demand_policy.

During repairs the fixture was corrected to use actual ReleaseIn and a valid authored resource output.
The cross-network expectation now accounts for both real source pushes. Empty receivers were added
to the existing observation guards after a hostile policy exposed the previous capture gap.

## SHA256 source fingerprints

Relative paths identify the verified source, not a Git commit.

```text
src/Beep.SimEngine/Standard/NetworkDemands.cs 69F351BED0029B0F851CB90900C46E59CC069C8267E1970FFC1021B9EF777E52
src/Beep.SimEngine/Standard/NetworkFlow.cs 79F275E53D2F9E73B94468C80E050BCED09413C2070FC015A5DF6F507248001F
src/Beep.SimEngine/Standard/NetworkDiagnostics.cs 97B7D2FC1BB122C836C98CD8299808D07A96C0D5F20D509A6A04FB03ABA5AEB0
src/Beep.SimEngine/Standard/Networks.cs DAECB36B7FB29D1941D53D4859D71D1AB406C141A5517BC0F7CAE6C634E7C0CB
src/Beep.SimEngine/Views/InfrastructureView.cs 197125155AA1FE26BEE6DF64D7FFBFE6870196FA9CE899BC627E581B0D670CB6
src/Beep.SimEngine/Standard/InfrastructureModule.cs 87AFDF562799ACBC26082DEF372372EC0B0B8758304AB73A3E7BF5A9F23D0FA0
tests/Beep.SimEngine.Tests/NetworkSolverTests.cs CE5A610A1B5C296D5B269C498F1E1EC4CEA44F567ECD8DCF9F4F26A25854832E
tests/ExtensionProof/NetworkDemandProof.cs 25EF72FCEABD6C01B4DF37FB57A7FFA3B3CD28B0B5D597F1353FDB4CF016E50C
tests/ExtensionProof/content/network-utilities.json 087D332ED3593F6CEABBC85D5D7CC45DFE8113247B311C8C1D1A1BC1F8272B25
src/Beep.SimEngine/Hosting/GameClock.cs 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31
```

## Remaining scope

Electrical/thermal/pressure physics, aggregate bandwidth, global utility priorities, graph-independent
provider breadth, generic utility work requirements, loss/disposal policies, global rollback/replay,
stress/soak/allocation budgets and final full-framework acceptance remain open. No Oil/Tycoon/host/demo/
platform/Draw work, new clock, legacy shim, duplicate state or fabricated free supply was introduced.
