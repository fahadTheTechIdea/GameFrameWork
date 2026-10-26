# P5-A3 — Oil resource-contract migration evidence

Date:2026-10-13. Framework remains2.531.0/save154/replay1; no framework production source change.

## Direct migration

Seven existing OilCo.Game files changed: Autopilot, Campaign, Commands, Company, Operations, Reservoirs and Views. Forty-three resource identity/catalog/codec caller lines directly use ResourceId/ResourceAmount, actual Resource fields, ResourceId/ResourceName/TryResource resolver methods and the Resource state codec. Autopilot resource facts now use native ResourceInfo and PlayerView.CopyResources. No obsolete identity alias, compatibility extension or copied resource catalog was introduced.

ReservoirTypeDef no longer uses Bill/BillSpec for dimensionless composition shares. It resolves actual IResourceDefinition aliases from the existing JSON object, validates positive unique fractions no greater than one, sorts by actual resource identity and publishes a detached read-only metadata list. Primary selection uses the actual aliases/fractions. Extraction reads that list, not a fake physical stock bill. The original geology, inventory and extraction owners remain; later physical transaction migration is still required.

## Quantization and authored source inspection

The original one-percent composition tolerance is removed. The existing Q32 parser truncates each positive decimal by less than one raw unit; a total is accepted only when One.Raw minus summed raw fractions is nonnegative and less than share count. No fraction is normalized or assigned a fabricated default. This is an explicit native-representation bound, not an exact-arbitrary-decimal arithmetic claim.

Read-only inspection of actual content/oil/geology.json found five reservoir definitions: conventional_oil(4shares), tight_oil(4), dry_gas(2), gas_condensate(4), offshore_oil(4). Each authored decimal sum is1. Source inspection does not prove engine composition, malformed-input behavior or physical extraction.

## Every compile observation

P5-A2 left28 errors. Current scoped OilCo.Game Release --no-restore observations:

1. Completed resource callers/composition: exit1, zero warnings,13 errors,3.04s.
2. FINAL parser-resolution/resource-info migration: exit1, zero warnings,12 errors,1.05s.

```text
dotnet build OilCo.Game/OilCo.Game.csproj -c Release --no-restore
```

Both outputs retained untruncated; final has12 unique reported errors. These concern work plans/nodes/effects, incident response plan/priority, signal input, content visibility and queue facts. Further old composition/storage/spatial/caller APIs can emerge after these declaration errors resolve. There is no successful oil build or runtime claim.

Search finds no ItemId/ItemStack/ItemInfo, ItemName/ItemId/TryItem/CopyItems, old resolver/codec Item calls or composition Bill.Total use in actual OilCo.Game. Oil diff-check exit0/one line-ending warning/staged0. No test executor, old suite, family filter or full regression ran.

## Remaining acceptance

Continue work/effect contracts with explicit dependencies and physical replacement/disposition, incident/signal/visibility integration, authored content/module/time/game-mode composition and thin callers. Replace remaining mixed-unit statistics and optional business/research/campaign responsibilities through their sole framework owners. Unsurveyed drilling, normal rivals, timed execution/privacy/save/replay/lockstep and the original gates remain unproven.

Original221/193checked/28open criterion/status/line anchors unchanged; no gate closed. No Tycoon business, older sibling engine, framework clock/source, platform/Draw or oil host/demo/harness/test source change. No staging/commit/push; one final full regression deferred until implementation finishes.

## Metadata/source-only checkpoint

1967 source anchors,1129 selected inputs and13 quickstart anchors match current bytes, with zero input drift after the final observed failing build. Seven previously frozen oil sources changed; one existing inspected geology JSON joins the freeze. All1113 framework inputs,12 Tycoon sources and sole clock remain unchanged. Two new documents only; no new executable class/file.

3099 canonical paths/1139 Markdown documents/8860 local links, zero broken links; eighteen well-formed master rows. All221 current criterion/status/line checks pass unchanged, additional266. Canonical diff-check exit0 with34 existing line-ending warnings, dirty3087/staged0. Oil diff-check exit0/one warning/staged0. Previous framework-driver boundary search is not an oil client audit. No positive build/runtime/full-regression result is inferred.
