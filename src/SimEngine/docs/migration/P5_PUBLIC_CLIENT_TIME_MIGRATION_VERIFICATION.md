# P5-A40 focused client/time migration evidence

Core 2.555.0/save156/replay1 unchanged. Production engine, Oil gameplay and Tycoon rules are unchanged in this stage.

## Source migration

The original ClientTests, InputTests and DemoTests moved into the existing non-friend public runner, with old registrations removed. Client/input scenario intent remains. Demo expectation changes explicitly to thin founding/handoff input because gameplay belongs in the engine; full engine-command coverage remains open.

Session ExactTicks/GameDuration, PressAt and Horizon replace stale clock APIs. BudgetTests retains its long allocation scenario with native time conversion. Existing ThinClientTests.Files is reused rather than duplicated.

## Builds

Public project/dependencies clean twice: dotnet elapsed 1.61s then 2.57s, zero warnings/errors. No executable edits after the latter build.

Remaining old test project diagnostic: exit 1, 152 unique source-location/code errors across GameTests 42, NetTests 21, OffshoreTests 54, OilResponseTests 15 and OptionalModuleTests 20. Build only; no legacy runtime. Prior 133-error checkpoint is historical, not a claim that this compilation is green.

## Exact runtime attempts

Demo thin app lifecycle: 8 passed, zero failures; case 7070ms, observer 7194ms, PID10212, exit0, untruncated. Bound120000ms/termination5000ms/output12000.

Controller handover first attempt: TimedOut, observer120092ms, PID32172, exit-1, untruncated. No completed check count; retained as aborted. Its terminal process was observed before the retry.

Controller handover retry: TimedOut, observer300198ms, PID26400, exit-1, untruncated. Same built executable and original authored two-hour cadence; no completed handback/save result. Both attempts remain aborted, not passing evidence. Diagnosing this runtime is the next open task; no third retry or production shortcut was attempted.

## Evidence boundary

No shell24-hour, presenterthree-day, input26-hour or allocationlong runtime was executed. Compilation is source/API evidence only for these cases. No broad industrial, optional-module, replay/lockstep, physical competition or full profile acceptance is inferred.

Original221/194 recorded checked/27open unchanged. Full goal/plan incomplete; no commit/push. [Design](../../plans/enhancements/P5_PUBLIC_CLIENT_TIME_MIGRATION_DESIGN.md).

## Metadata validation

2162 active source hashes,13 quickstart hashes and1261 selected current input hashes match; no executable drift after build/runtime.8725 local Markdown links resolve,18 master rows are well formed, and all221 original requirement texts/check states/line anchors validate. Framework/Oil diff checks pass with existing line-ending warnings; neither repository has staged changes. Protected game clock and Tycoon supplier/value/account-definition files are unchanged. These checks do not establish full gameplay or profile success.

