# Current-API quick setup

This example lives wholly in Beep.SimEngine. Game.Module declares available engine modules and their
dependencies; startup intent is JSON executed by the existing WorldSetupModule. Host only loads files,
calls Initialize/Update and checks observations. No host gameplay or alternate simulation loop.
The example is not packaged. The focused test references its actual public module and copies its
actual JSON, so the sample module compiles with the test project rather than silently drifting.

From the framework root:

```powershell
dotnet run --project help/quickstart/Host/QuickGame.Host.csproj
```

Content/manifest.json explicitly lists world, resource, access and startup documents with module
versions/dependencies. Add a new resource file by listing its path under the owning module; no directory
scan enrolls it. `resources` declare phase, traits, dimensional quantity units and capacity measures;
`units` declare dimension, rational scale and discreteness. Unknown/removed sections are rejected.

The authored mode is resource-only with no business properties or Tycoon selection. `space: null`
selects no map; startup explicitly has null placement/home. A spatial game instead selects its own
space/presence and placement providers. The minute quantum and one-quantum Update frame are sample
content, not framework size/time limits. Each created entity has the authored default state.
No custom spawning code, renderer, platform SDK, Tycoon or oil dependency.

Current scoped evidence: both sample projects compile; one bounded exact public-API test passed
28 checks on engine2.304.0/save144/replay1. It covers actual master-listed content, dimensional ore,
authored initialization, idempotence, Update/snapshot settlement, retained immutable snapshots,
save/load continuation and independent sessions. The standalone console executable was compiled,
not separately run. This is not P1–P12 completion or broad regression evidence.
[Verification](../../docs/migration/P6_QUICKSTART_VERIFICATION.md).
Draw/rendering is reserved for a future game-platform project and is not implemented here.
