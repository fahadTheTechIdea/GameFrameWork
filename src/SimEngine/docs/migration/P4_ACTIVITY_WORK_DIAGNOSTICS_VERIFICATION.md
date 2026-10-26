# P4-G5a exact activity work diagnostics — scoped verification

2026-10-08. Canonical Beep.SimEngine only. Full goal/plan ACTIVE; no commit/push.

## Actual invocation ledger

1. `dotnet build Beep.SimEngine.slnx`: exit0,0 warnings/errors,13.32s.
   Original live session62770 resumed to completion, not restarted on observation timeout.
2. `dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Activity work diagnostics: exact critical path, private physical progress and saved continuation"`:
   exit1,58 passed checks/0 failed assertions/1 suite aborted498ms. New case incorrectly assumed
   the final order remains live after its last productive Update; no-dock native work retires it.
3. Repair ONLY observed lifecycle assertion: analyze actual completed node records through same
   graph API and verify retired view refusal, no artificial retention. SAME build:exit0,0 warnings/
   errors,4.87s.
4. SAME exact new case:exit0,72 passed checks/0 failures/0 aborted484ms.

TWO builds, TWO runs of ONE distinct NEW bounded case. Not first runtime attempt.110 older build
assertion files/lines source/compile ONLY. Source/public proof/tests/JSON FROZEN after final green;
docs-only afterward. No older/family/full/platform/stale gate or extra runtime invocation.

## Authoritative implementation and focused proof

DependencyGraph<T> owns SAME captured edges, topology and iterative weighted critical-path analysis.
Weights are exact nonnegative BigInteger values with exact ordinal node-set validation, no fabricated
defaults or long/Fix aggregate cap. Equal predecessor and terminal choices use ordinal identity.
Zero-work prefixes/suffixes trim; zero-weight dependency connectors remain; all-zero path is empty.
The immutable result captures values and IDs, never aliases caller weights.

WorkProgress.RemainingRaw owns exact Q32 remainder from full-width whole ticks/fraction and refuses
overshoot. IActivityGraph.AnalyzeWork captures exact progress identity, selected authored durations,
total/remaining weights and delegates SAME compiler. WorkView.TryReadProgress checks company before
building detached actual-order facts. State/blocker/read clock coordinate and completed/unfinished/
earned-unsettled counts remain distinct from nominal work. No gameplay/provider invocation, stock
promise, job dispatch, retained prediction, parallel graph/progress store or timer.

- Numeric proof: long.MaxValue requirements and one Q32 unit boundaries; negative/overshoot/null/
  missing/foreign/case-substituted identity refusal; path weight beyond long and Fix; stable ties,
  zero connectors/prefixes/suffixes; immutable detached path; iterative4096-node analysis.
- Actual master-listed segment profile, not a driver-made gameplay workflow: two real machines,
  nine authored nodes across three tiles. Total39 nominal minutes versus critical30, canonical
  inspect/install chain. Machines held by public command earn no work; clock advances normally.
- Both physical branches actually earn progress; only actual productive work reduces totals/path.
  Twenty repeated private reads preserve hash; foreign/missing orders return false/null, paths
  immutable and old snapshots detached from Update.
- Held partial work saves/restores actual jobs/progress; diagnostic values/path agree. Resume via
  same public machine commands; twin hashes match through completed physical prefix and final
  work/settlement. Completed actual records produce zero remaining/path through SAME graph API;
  retired live view correctly refuses rather than fabricating retention/history.
- Ordinary cancellation during physical work cleans up through actual board; retired read refuses,
  detached pre-cancel snapshot retains unearned work and normal checkpoint restores identically.
  No business module is selected. Every Reach is bounded900 actual Updates with diagnostic snapshot.

Engine2.244.0/save139/Board43/WorkSystem41/View80; persistent payload bytes unchanged. No compatibility
reader/shim, new JSON schema, host/demo/Draw/platform/Oil/business change. One unchanged GameClock.
Lifecycle skill guided staged ownership/design/evidence; explicit focused verification and delayed
publication instructions supersede generic full-regression/early-commit guidance.

## Scope limits and source inventory

This proves nominal remaining-work diagnostics, NOT completion-time ETA. Machine rates/travel,
resource waits/congestion/departure/effect retries cannot be inferred from authored work duration.
EarnedUnsettledNodes is implemented but a live zero-work dock/effect wait is source-only here; this
no-dock fixture does not prove that wait classification at runtime. Provider purity/fault cases,
generated process/team/haul/non-tile/templates, replay/stress/CPU/broader G3-G5 and remaining
P1-P12/full acceptance remain open. Historical reports remain historical, not rerun.

[Source manifest](P4_ACTIVITY_WORK_DIAGNOSTICS_SOURCE_MANIFEST.json).
Scoped533 paths/93 Markdown/1518 local links/0 broken/0 hash mismatches after final documentation
refresh. Git diff-check exit0 with existing LF/CRLF warnings. Own manifest hash excluded; not
whole-repository/runtime acceptance. Preserve dirty work; no Git publication yet.
