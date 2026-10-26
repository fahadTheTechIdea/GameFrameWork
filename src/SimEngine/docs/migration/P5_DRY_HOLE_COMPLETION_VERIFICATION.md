# P5-DH1 — Earned physical dry drilling and saved completion

Current M149/native2.772/save161/replay2 requalifies the ORIGINAL dry-hole scenario629/0,
alongside original successful unsurveyed drilling617/0. Real8h preparation/108h drilling,
physical supplies, productive saved twins, exact removed-target completion, private geology
and once-only failure notification remain unchanged. [Actual target build/two focused runs,
image preservation and original-scope limits](P5_OIL_UNSURVEYED_PHYSICAL_DRILLING_VERIFICATION.md#current-m149-actual-supplied-well-and-dry-hole-completion--2026-10-23).
No source/gameplay/authoring change or new broad gate closure; older records keep their image.

2026-10-16. Current engine2.639.0/save160/replay1 unchanged by this stage.
[Stage](../../plans/enhancements/P5_DRY_HOLE_COMPLETION_DESIGN.md).
One new exact case, first attempt: **621 passed /0 failed**, no aborted suite.
No production engine or Oil gameplay repair was needed. Original221 requirements remain
209 recorded checked/12 open; broader P5.005/.008 and full goal remain open.

## Implemented proof, not host gameplay

Extended the existing consumer DrillingTests as a partial class and added one separately
registered dry case. It reuses the original Files, Stock, Wait and Diagnostic helpers;
there is no second drilling executor, simulation loop, stock/knowledge model or provider.
Only public player commands, original Update, immutable views, notifications and save/load
drive the scenario. No World, TestKit, private geology, direct entity/stock/progress write,
clock jump, instant placement or completion callback is used.

The original [successful blind-well proof](P5_OIL_UNSURVEYED_PHYSICAL_DRILLING_VERIFICATION.md)
remains separately registered and unchanged in behavior; it was not rerun in this stage.
Its historical610-check result is not added to this stage's621.

## Exact scenario inputs and physical behavior

The new read-only DryFiles wraps the original drilling fixture. Existing24x24 Sandbox,
seed17, one human, no extra reservoirs, opening field at start, four starting grader/hauler/
drill/repair machines, normal starting materials and explicit40 spares remain unchanged.
Only the opening conventional field's authored radius changes from8 to1. The original
generator creates that actual field; it is not exposed through views or injected by the
driver. All original reservoir volumes, material bills, work/travel/handling rates,
durations, graph/effect metadata, operation/maintenance policies and money-off mode remain.

Public PlanningView supplies legal Any-intent pad placements. The script selects a legal
position more than six tiles from the public company start, without querying reservoir
membership. A position choice is player input; actual legality, route/supply/work/outcome
decisions remain in engine/module owners. This is a deliberately authored dry-location
scenario, not proof that every blind location is dry or that every legal site is reachable.

Observed physical lifecycle:

1. Normal founding creates HQ1 and the four actual machines2–5. No seismic, survey or logging
   actor exists; public subsurface knowledge is empty and Tycoon is not selected.
2. ConstructCommand creates site6. Real hauling delivers six cement and the grader earns the
   full eight-hour preparation. Site becomes prepared pad7 at game minute620.
3. Public PostOrderCommand admits blind drill_well on pad7, without surveying or knowledge.
   Posting does not grant completion; previews preserve the full hash and hidden geology.
4. Actual rig_up12h, drill72h and casing_cementing24h execute. The scenario observes real
   productive progress in every staffed stage and supply waits between stages; no optional
   seismic/logging node is a prerequisite. It consumes20 mud,8 casing and12 cementing supplies
   from real HQ stock, with exact final deltas.
5. A save is captured during productive drilling. An independent restored game receives each
   subsequent ordinary Update and its full hash matches through the dry result.
6. At minute7370 the original DrillingEffectDef's actual geological outcome is dry. Ordinary
   guarded despawn removes pad7; no producing well is created. Public diagnostics show only
   HQ and the original four machines. No unrelated field is discovered.
7. Company-wide public completion history retains the exact order2 and removed target7.
   No active/completing drill order remains. The company receives exactly one NoteDryHole
   and zero NoteWell notifications. The result is a completed workflow with an unsuccessful
   exploration outcome, not an engine exception, refused order or abandoned plan.
8. Completed history/state saves and loads identically; three further updates of original and
   restored games agree and do not repeat the notification. A separate idle session is unchanged.

The existing engine already handles target removal, completion compaction, reservations and
history correctly in this flow. No removal guard was bypassed, no pad/well compatibility
path added, and no gameplay was moved to a test, headless host or demo.

## Exact commands and observed attempts

From the actual Oil consumer directory:

```powershell
dotnet build OilCo.PublicProof/OilCo.PublicProof.csproj -c Release --no-restore -v minimal
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Oil public dry drilling: blind supplied stages earned failure and saved completion without surveys" --timeout-ms 240000 --termination-timeout-ms 5000 --output-chars 24000
```

One clean targeted build:1.81s, zero warnings/errors. One exact runtime attempt: child25072,
parent34636, exit0, suite61,713ms / bounded process61,811ms. Preparation stage6,276ms,
drilling stage51,579ms. Both stdout and stderr untruncated, no timeout or abort.

The driver retains the existing internal650-second stage and120-second no-progress guards;
the stricter new whole-case240-second process watchdog applies independently. None was
raised, bypassed or reached. Diagnostics every20 frames record real progress and blockers.
Wall timers bound the test only; all gameplay uses the original game clock. This is not a
controlled performance-budget or allocation benchmark.

The live exec handle13466 and actual child/parent process inventory were observed and polled
through terminal completion; observation waits did not restart the scenario. A later
Get-Process query found the child already absent, consistent with the terminal result.
Some read-only source searches used guessed paths and a few larger source excerpts were
truncated; no runtime result or missing-code conclusion was inferred from those misses.
No failed build, test, patch validation or abandoned live process in this stage.

## Current identities

444 core C# files, sorted forward-slash relative path plus uppercase file SHA256 joined
with LF and no trailing LF:
`0327e6ff4473656c75e2e8a889d804e5cc8d8029b027065aa772b5bb27b7973a`.
This is exactly the preceding P5-FP1 production core, not a new engine release.

| Artifact/source | SHA256 |
|---|---|
| Engine Release DLL and actual public consumer copy | E811B410F353520502501CF34A6E848B47EB67F0B72DEA42E1E645D3ADA0A6D7 |
| Actual OilCo.PublicProof Release DLL | B870DB3593122C44802181A55F9590235DB4E85CDEDA4F7F1BA614BE84DF79D8 |
| Consumer DrillingTests.cs | 8A4953AE74ED43BB9C6DF02AA6B422D65F2FEC0485A24D85E563DE6F05B6448E |
| Consumer DrillingTests.Dry.cs | FCAE79008A644E3815009FF2D464DDB98DD315C6F8D484F36599E9A732907C32 |

No executable edits followed the successful runtime. No broad/family/profile/full regression,
older-suite revalidation, optional-business work, Oil production/content changes, platform,
Draw, adapter, headless/demo gameplay, staging, commit or push.

## Remaining scope

This supplies the previously missing actual dry-hole branch with saved physical execution,
not offshore/full-size/all-company/scenario universality or complete P5.005/.008 acceptance.
It is not full canonical replay, lockstep, concurrent-field/failure/provider/private/cargo/
incident/stress/soak acceptance. All12 original open requirements retain their meanings and
checkboxes. The lifecycle skill organized the stage and evidence; the user's focused-only
and no-publication constraints override generic full-regression/commit advice.

Final handoff: all221 original IDs/literal texts map209 checked/12 open, frozen audit
unchanged. Framework diff check exits0 with35 existing LF-to-CRLF warnings/no other
diagnostics; edited consumer sources also pass diff checks. New documents/source have no
trailing whitespace, links resolve, privileged-driver search has zero matches, staged
paths0 and no live public-proof verification process remains.
