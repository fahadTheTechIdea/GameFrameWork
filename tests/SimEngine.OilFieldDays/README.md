# OilFieldDays consumer integration tests

Dependency direction: OilFieldDays consumes SimEngine. This optional consumer suite is outside the standalone engine source/build graph and is not part of GameFrameWork.slnx. Its 18 moved source files preserve the existing assertions; the shared generic test harness is source-linked from src/SimEngine rather than copied. The engine's own tests run independently.

The suite requires authentic OilFieldDays content and its compiled OilCo.Game assembly. Build with `dotnet build tests/SimEngine.OilFieldDays/SimEngine.OilFieldDays.csproj -c Release -p:OilFieldDaysContentRoot=<absolute-content-directory>`. That directory must contain manifest.json, oil/ and sim/ from the actual game. Set `OILFIELDDAYS_GAME_ASSEMBLY` to the actual OilCo.Game.dll when running binary-audit cases. Select exact cases using the existing bounded test runner; do not run broad suites without owner authorization.

No game content is copied or fabricated. Missing content refuses the consumer build explicitly. Consumer compilation/runtime remain unverified until authentic fixtures and binary are available; passing the independent engine build does not pass this integration suite. Original fixture logical names and test assertions are recorded in docs/evidence/2026-10-25-simengine-consumer-test-moves.json at the repository root.
