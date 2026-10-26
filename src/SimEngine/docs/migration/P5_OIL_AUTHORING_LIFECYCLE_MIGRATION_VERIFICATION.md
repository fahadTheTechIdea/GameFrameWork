# P5-A1 — Oil authoring lifecycle migration evidence

Date: 2026-10-13. Framework core unchanged: 2.531.0/save154/replay1.

## Actual integration baseline

OilFieldDays/src/oilCo-engine/src/SimEngine/SimEngine.slnx references the external Beep.SimEngine runtime and testing utility. OilCo.Game/OilCo.Game.csproj targets netstandard2.1 and directly references that runtime, not a local engine copy. Its InternalsVisibleTo declaration grants OilCo.Tests access to its own domain internals; this does not establish that the oil module is free of framework-internal dependencies.

The actual OilCo.Game project fails to compile. Baseline Release --no-restore: exit1, zero warnings, 88 errors, 1.49s. Baseline output was truncated; the terminal count/result was observed. Errors include removed ItemId/ItemStack, OrderDef/IWorkPlan/StepDef/WorkEffectDef, public definition lifecycle overrides, concrete entity-state returns, old Schedule properties, visibility and automation/incident contracts.

## Changes

Nine existing sources migrated in place: Autopilot, Campaign, Commands, Fog, Operations, Research, Reservoirs, Settings and Storyteller. All declaration Read/Resolve/Validate overrides now implement protected ReadCore/ResolveCore/ValidateCore, leaving current Def/ComponentDef in charge of strict once-only authoring/link/validation/freeze. Well/Wear/PowerSource/Lab state metadata, actual resolver aliases and state-source returns use IEntityStateDefinition instead of concrete EntityStateDef.

Fifty-one signature/alias lines changed. Each resulting file was compared with the captured pre-edit source transformed by only those exact replacements; all nine matched (normalizing line endings only). No parsing, domain formula, economic behavior, state writer or scheduling body changed in this slice. No compatibility type, fallback authoring wrapper, duplicate state machine or framework API was introduced.

## Verification and remaining compiler blockers

```text
dotnet build OilCo.Game/OilCo.Game.csproj -c Release --no-restore
```

Completed migration compile: exit1, zero warnings, 35 errors, 1.71s. Current output retained untruncated; 35 unique error locations observed. Search found no remaining public Read/Resolve/Validate overrides or concrete EntityStateDef aliases in OilCo.Game.

Remaining reported groups: removed resource identities/stacks and client queue/resource facts; old work-plan/node/effect APIs; seven Schedule-property overrides; missing content visibility; automation signal input contract; incident response plan/priority. Later compiler passes can expose further removed composition, spatial, resource, time and caller APIs. Reduced compiler count is not a successful build or gameplay proof.

No test executor, old focused suite, broad family or full regression ran. P10-PV1's 543-check result remains historical framework evidence, not oil evidence. Framework source/protocol/clock and all Tycoon source remain unchanged.

## Scope and next work

Continue direct resource/work/time/content composition migration, replace duplicated generic research/campaign/economy responsibilities through their authoritative current owners, and migrate the thin oil callers. Then prove unsurveyed physical drilling, other timed scenarios, normal rival parity, private operational knowledge, save/load/replay/lockstep and actual profile acceptance. None of those original gates is closed by this authoring migration.

Original221/193checked/28open current criteria/status/lines unchanged, no original gate closed. P5 remains incomplete. No older sibling engine, host/demo/harness/test source, platform/Draw or Tycoon change. No staging, commit or push. The single authorized final full regression remains deferred until implementation finishes.

## Metadata/source-only checkpoint

1962 source anchors,1128 selected inputs and13 quickstart anchors match current bytes, with no executable drift after the observed failing build. Fifteen previously existing external oil source/project/solution files join the integrity freeze; all1113 prior framework inputs remain unchanged. 3095 canonical paths/1135 Markdown documents, eighteen well-formed master rows and no broken local links. All221 current criterion/status/line checks pass unchanged; additional264. All12 Tycoon sources and sole clock remain unchanged.

Canonical diff-check exit0 with34 existing LF/CRLF warning lines, dirty3083/staged0. Oil project diff-check exit0/no output/staged0; thirteen game source files inventoried, nine edited and mechanically compared, zero obsolete public lifecycle/concrete-state matches. The metadata driver-boundary search refers to the previous framework driver, not an oil thin-client acceptance audit. No positive runtime result is inferred.

One master documentation patch was refused because it repeated the same file target; no files changed, and the corrected single-target/two-hunk patch updated both existing P5 summary rows. Their distinct table summaries are not separate implementation owners.
