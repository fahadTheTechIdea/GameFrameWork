# P3-OT1 — Minute-based Oil timing requirement verification

## Outcome and exact requirement

Original **P3.acceptance.001** is verified on unchanged **2.628.0/save159/replay1**:

> Minute-based oil sessions retain deterministic physical timing with explicit authored configuration.

Five selected current-core proofs pass **5364/0**. This closes the minute-time
integration requirement, not complete Oil scenario acceptance or a full profile.
[Design](../../plans/enhancements/P3_OIL_MINUTE_TIMING_REVALIDATION_DESIGN.md).
Core442 C# SHA256 remains
`7b1efd6284bb8fe661c8da42bc8be67faad0ae66921b41fdf4150b6cdd096d7c`.
No production/Oil source, clock, content amount, financial rule or protocol changed.

## Requirement-to-current-evidence mapping

| Required part | Current authoritative evidence |
|---------------|--------------------------------|
| Explicit Oil composition/time selection | Actual live manifest uniquely lists oil/game-modes.json and oil/scenarios.json under oil; all six scenario/time bindings checked |
| Minute conversion, not static Oil constants | Runtime selected profiles resolve exact60/1 seconds,60 updates/hour and1440/day; one converter object shared by game/clock |
| One clock and host-independent batching | Actual ten-update frame stride/30-per-second platform pacing, pause, speed, saved time identity, native batched-vs-framed continuation/hash and snapshot age |
| Physical construction and hauling retain time | Supplied canonical pad construction earns at least480 minute coordinates; saved loaded cargo and working machine; six cement consumed once; removal earns at least120 minutes |
| Physical staged activity retains time | Actual linked drilling graph earns at least6480 minutes; ordered machine stages, original requirements, earned completion and saved continuations across stages |
| Sequential manufacturing retains time | Two actual entities each earn their own2160-minute manufacturing interval, nonoverlapping release and saved twins; resource-only and paid modes |
| Actual external domain consumer uses same runtime/time | Full Oil modules' supplied survey case exercises normal founding/preview/order commands, physical ten-charge loading/travel/unloading, six-minute authored work fixture, original completion/private knowledge and saved twins |

The profile proof covers frontier/adventure/continent/sandbox/showcase/gulf through
their actual live IDs. All author one-minute quantum, ten updates/frame and30
updates/second platform pacing. Calendars are explicitly absent; showcase's optional
1500-day horizon remains authored, not a compulsory engine date/calendar.

All22 live Oil content links in ExtensionProof are direct project resource links,
not maintained JSON copies. Added two links for actual scenario/master files follow
that existing mechanism. Sorted logical-name/source-content SHA256 for those22
links is `86a0bb6f7bbecba848c2a19f3e6d69b7a70d507b2378345e13aa8f91f67bee6a`.
Actual manifest/game-modes/scenarios file SHA256 respectively:

- `4368661020E7C8E8B6E925BD4AF4B4775CB85C084FE7CC1C9121A8BD05340272`
- `8CEA4AD9D983CD9C87740FDA9AB964B4251A1FDC557902A9FB7600D4CF103A4D`
- `8639E0A631923B5612093D5821ABE98DA65263F2F6FF80D26894F55D2CC437F8`

These hashes trace current input identity; they are not an alternate content catalog
or proof of every definition's gameplay.

## Fixture scope — no substituted full-game claim

The new six-profile case uses the existing explicit Oil infrastructure fixture,
selecting actual live time/mode profiles. It does **not** generate six full Oil worlds.
Its ordinary native clock/update/save APIs prove selected configuration survives
runtime resolution and save/batching. The initial new fixture attempt correctly
refused unrelated drilling cost bindings that referred to absent selected activities.

The existing construction test keeps actual linked Oil construction/site/storage
content; role/carrier authoring supplies the six cement and deliberately uses
ten-minute load/unload to observe loaded cargo. Original eight-hour grading and
two-hour removal durations are not shortened.

The existing drilling test supplies its actual target with20 mud/eight casing/
twelve cementing supplies as explicit startup inventory, tags it as a legal pad
and assigns a drill role. It retains actual rig-up/drill/casing/completion graph
durations and requirements. It omits the Oil-specific geology completion effect;
it does not claim a full generated-world well was completed here. Optional seismic/
logging are not graph prerequisites. Construction and the real Oil survey supply
the independent physical travel/hauling evidence.

The manufacturing fixture selects actual build_haul_truck content and redirects
only its entity output to the explicit collision-aware proof vehicle. Each36-hour
cycle and original18,000 optional charge remain unchanged. Paid execution confirms
exactly36,000 charged, no outstanding budget reservations and no skipped second cycle.

The external public survey uses actual Oil modules, a declared24x24 sandbox fixture,
selected seismic/haul vehicles, one-update frames and a six-minute survey-work
override. Those are authored proof choices, not engine limits or a claim canonical
survey work was changed. Its native public-command and engine-view path is distinct
from the selected-content framework fixtures; no host/test completes work itself.

## Fixture corrections, not business-rule changes

The two initially failed cases loaded oil-business-costs.json with bindings to
workflows absent from their selected line/production fixtures. They now explicitly
pass one shared SelectLineCosts JSON projection to the existing fixture. Only
bindings for actual loaded line activities remain there. Manufacturing's separate
actual transformation-cost bindings and authored amounts remain selected and proven.
Other construction/drilling fixture projections are unchanged. No runtime fallback,
global Tycoon activation or monetary grant was added.

The old manufacturing metadata assertion expected21 definitions. Live production
now contains17 vehicle definitions and five processes, including explicitly authored
dispose_produced_water. The test now checks22 plus the disposal identity rather
than silently dropping the added entry. This is catalog metadata inspection, not
a claim all22 transformations executed.

## Every current build and runtime attempt

Targeted builds:

| Target | Result |
|--------|--------|
| Framework test project --no-restore, first build |clean0 warnings/errors;6.98s |
| Actual OilCo.PublicProof Release --no-restore |clean0 warnings/errors;4.53s; includes current framework/domain/Headless dependencies |
| Framework test project after selected fixture corrections |clean0 warnings/errors;5.36s |

No dependency suite was executed by compilation. All runtime commands used literal
--bounded-exact names,60s timeout/5s termination and4500-character output bounds:

| Attempt | Result | Terminal evidence |
|---------|--------|-------------------|
| New six-profile case, first attempt |8/1 |21968 exit1;205ms suite/391ms process; strict absent-activity cost-binding refusal |
| Existing supplied construction/removal |182/0 |29896 exit0;20419/20604ms; session39288 polled to terminal |
| Existing staged drilling |4046/0 |26452 exit0;46878/47062ms; session64063 polled to terminal |
| Existing manufacturing, first attempt |439/2 |33340 exit1;25870/26061ms; session59364 polled; stale21 count plus unrelated cost binding; resource-only half had run |
| Actual external Oil supplied survey |116/0 |17316 exit0;33087/33212ms; session51087 polled to terminal |
| Final six-profile case |138/0 |26952 exit0;1067/1213ms |
| Final manufacturing, both money modes |882/0 |22948 exit0;46122/46270ms; session16658 polled to terminal |

The first four framework literals and the Oil target build ran independently in
parallel. External survey then ran against the freshly built Release consumer.
Only the two failed cases were rerun after source-level fixture corrections; the
production core and successful case inputs remained unchanged. No process restarted
because a10s observation returned a live handle. All output was untruncated; no
timeout, aborted suite or live handle remains.

Exact final registrations:

```text
Oil session time: all live scenario profiles retain one minute clock and saved frame identity
Oil construction: canonical supplied pad and timed removal through native DAG
Oil activities: authored drilling DAG material stages and optional survey branches
Oil production: canonical timed manufacturing and optional cost annotations
Oil activity contracts: detached survey order physical supplies saved progress and completion
```

No full/family/profile, long generated-world drilling campaign, full canonical
research/campaign/rival or demo-horizon runtime ran. Oil source was read-only;
consumer build outputs are generated verification artifacts, not source changes.

## Closure and remaining scope

Only original P3.acceptance.001 closes. Expected bookkeeping becomes221 original/
205 recorded checked/16 open, confirmed by exact ID/literal mapping of all221
original requirements with no missing match. This is
not global revalidation. P5.005 retains complete migrated production/construction/
hauling/recovery/research scenario acceptance; the other P5 privacy/rival/content/
save/lockstep, P6 profiles and remaining P7/P8/P10/P11 gates stay open.

Lifecycle guidance organized the requirement matrix and stage/tracker evidence.
No engine default calendar/quantum, parallel clock/state, Oil/financial rules,
Draw/platform/adapter, compatibility shim, memory edit or Git staging/commit/push.

Final metadata: new stage/report links resolve; git diff --check exits zero with
35 existing line-ending warnings and zero staged paths. Production core and
successful case inputs remain unchanged after their applicable final proof.
