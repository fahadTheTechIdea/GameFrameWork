# P11-A1 — Native technology progression evidence

Date:2026-10-05. Current working-source evidence, not committed HEAD or full regression.
Standalone Beep.SimEngine only; no Oil/Tycoon/host gameplay/platform/Draw changes.

Historical A1 evidence. Current [D1 ownership/evidence](P11_COMPANY_KNOWLEDGE_VERIFICATION.md)
replaces the TechnologyService learned table/Sync with CompanyKnowledgeService's sole ledger. There is
no legacy codec or parallel active path. Current2.92.0/save61 extended technology gates pass146 checks.

## Identity and owners

Build2.87.0/save57; optional technology service1/view1. Existing Work22/work-view29 and all other
native state layouts unchanged. Direct breaking content-access API migration; no string overload,
mutable delegate list, old save reader, compatibility wrapper or inferred initial knowledge.

TechnologyModule is explicitly master-listed and optionally selected. TechnologyDef/ITechnologyDefinition
extends ActivityDef/the existing catalog and work DAG; shared DependencyGraph validates prerequisites.
TechnologyUnlockDef/ITechnologyUnlock resolves actual entity/activity/transformation aliases. The new
IContentDefinition is implemented on Def, not a second content index. Catalog.Owns validates the exact
resolved reference. IContentAccess now consumes this typed identity; the same ContentAccessPolicy wraps
the selected base policy and immutable, stable-ID-sorted declared IContentAccessRule services. Existing
construction/activity/line/transformation callers migrate directly to resolved definitions.

Ordinary PostOrderCommand uses the bare activity ID; PlanIntent uses the existing activity: key. Both
execute through native WorkBoard/orders/nodes/requirements/worker dispatch/material reservations/
supply/movement/progress/plan pause/cancellation. No technology command, queue, scheduler, timer or
parallel material ledger. TechnologyRequirementDef plugs into existing requirement expressions.

One TechnologyService/ITechnologyKnowledge owns only completed (company, technology, tick) records
through sole Sync. Active orders are derived, never copied. The authored final TechnologyCompletionDef
requires a canonical native Completing order and all nodes Done before recording knowledge once.
Learned records validate company/content/time/unique identity and prerequisite chronology on load,
save and hash. Multiple technologies guarding the same content currently all must be learned; this
slice adds no alternative unlock-expression semantics. TechnologyView projects detached company-
private learned/prerequisite/active/blocker facts; explicit observers can see all companies.

## Final exact gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Technology: authored contracts, access identities and private knowledge"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Technology: physical research, unlocks and saved continuation"
```

Final build:0 warnings,0 errors,2.04s.
Contracts:52 passed,0 failed,354ms.
Physical:24 passed,0 failed,269ms.
Total:76 checks/exactly two suites, zero failures/aborts. Only these exact names were repeated during
repair. Initial checks found a doubled activity: prefix in a test command and an unmatched refused
command in twin inputs; both were corrected in the player driver, not worked around in the engine.
Prior-suite build/save assertions mechanically migrated and compiled only. No third/family/full suite
ran; previous stage evidence was not rerun. Physical waits bound900 updates and report tick,
technology identity, learned/active/blocker diagnostics on timeout; this is a test timeout, not an
engine gameplay size/duration cap.

## Proven in this slice

- Explicit master document provenance, selected module/service/view identity and native ActivityDef
  catalog alias; optional omission installs neither knowledge state nor hidden locks.
- Missing/cyclic/duplicate prerequisites, missing/bad unlocks, absent final effect, zero-duration
  research, misplaced completion effect and missing required arrays refuse before Initialize.
- Typed entity/activity homonyms remain distinct. Null, foreign content and foreign-session aliases
  refuse; own construction and activity previews read actual access gates.
- Read-only facts/previews change no hash. Missing prerequisites, foreign targets, active company-wide
  research across targets, already learned research and premature/null completion refuse correctly.
- Actual machine/resource work consumes eight inputs and holds/releases two reusable tools per
  technology. Cancellation grants no knowledge and does not refund spent inputs.
- Pending ordinary plan commands save; paused research earns no work; resume completes actual work.
  Two sequential technologies consume16 of20 physical inputs and open the exact entity/activity.
- Learned facts are detached and private; another company remains unlearned/locked.
- Saved pending/partial/paused/completed continuation and identical command-stream twin hashes agree.
  Completed knowledge retains no second active-work/progress model. Corrupt future completion time
  refuses both checkpoint and hash.

## Still required

Independent unlock/effect providers; native transformation-unlock execution (provider compiles but
not exercised here); parameter effects and effect bounds; utility/cost research provider scenarios;
broader malformed/cross-owner save-state validation, rule-purity/fault/replay/lockstep/fuzz/soak/
allocation matrices; objective chains/deadlines, general surveyed/hidden-world knowledge, authored
controllers/pressure budgets. Repeat/starting technology and alternative research semantics are not
introduced. Oil/P5 adoption must replace its old generic research registration rather than install
two owners; this turn changed no sibling repository. Full P11/P1–P12 acceptance remains open.

Development-lifecycle skill kept design, single-owner implementation and scoped evidence aligned.
