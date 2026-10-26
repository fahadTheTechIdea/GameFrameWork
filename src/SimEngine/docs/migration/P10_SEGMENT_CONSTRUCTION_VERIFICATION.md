# P10-A3 — native physical corridor DAG verification

2026-10-06, framework checkout only. This is scoped current-source evidence, not full P10/P1-P12
completion or a broad regression result. No Oil/Tycoon/host/demo/platform/Draw changes.

## Clean revision and ownership

- Build **beep-simengine-2.125.0**, save **68**, **line_work schema2**. Remove the saved laid counter,
  old one-node corridor, scalar `hours_per_tile` reader and finish-time remainder stamping. No old
  save/content reader or compatibility facade. Existing pool/work/inventory payloads unchanged.
- Explicit JSON rational `duration_per_tile`, `segment_effect`, `finish_effect`; `line_effects`
  declare `operation: segment|finish`. Actual registered references and roles/requirements resolve.
- Existing ActivityGraph generates stable sequential segment nodes. Each uses requirement multiplier
  one and earns its separately quantized positive duration. Checked wide total tick arithmetic
  refuses overflow. Current-segment capacity, not whole-corridor capacity, governs admission.
- Existing WorkBoard owns physical arrival, requirements, spending, reservations, jobs and named
  progress. Existing LineService commits only the earned current tile. No second scheduler/store.
- Before moving the nonphysical intent cursor, actual old receipts must be absent and surplus is
  evacuated through selected IResourceRecovery at the actual old placement. New supply may reserve
  at the new position in the same Update; it is not an old promise moving with cargo.
- Normal order completion only removes a fully earned marker. Failure/cancel preserve built prefix,
  stock and real promises. Loaded marker content/path/order/owner/cursor bindings validate.

## Final gates

`dotnet build Beep.SimEngine.slnx --no-restore`

- Success, **0 warnings/0 errors**, MSBuild **5.14s**.

Exactly these two complete suites, with repair reruns retaining the same names:

| Exact suite | Passed | Failed | Final reported time |
|---|---:|---:|---:|
| Line segments: authored DAG, admission and pure completion | 106 | 0 | 3504ms |
| Line segments: physical supply, recovery and saved continuation | 280 | 0 | 3694ms |

**386 checks, two suites, zero failures/aborts.** Physical stages have bounded Update counts and
order/blocker diagnostics. The suites author content/input, submit normal commands, advance Update,
and observe real owners. The reservation probe uses the existing engine contract, not copied rules.

Admission proves twenty segments fit a one-unit current buffer; stable IDs/dependencies/durations;
explicit missing/wrong effects and zero/old durations refuse; unearned effects stay pure; repeated
tiles/cursor mismatch fail load; overflow command/direct-provider parity allocates nothing.
Execution proves high work rate cannot build multiple future segments per Update, only reached
per-segment material/cash is spent, partial saves/interleaved sessions match, foreign current ownership
refuses without overwriting or finishing the remainder, surplus recovers at the old site, cancellation
retains built prefix, old incoming receipts block movement and cleanup resumes after actual release.

Initial test-only assertions incorrectly assumed removal precedes the later logistics phase and
that the next stage cannot create a fresh reservation in the same Update. They were corrected to
observe existing engine phase semantics. A yielded build briefly overlapped old test binaries and
reported copy-retry warnings; those stale-binary runs are discarded. Final gates above followed the
completed clean build. No third/family/full suite ran.

58 previous build assertion files and56 previous save-format assertions were mechanically revised;
shared JSON line fixtures, old work-start and wide-progress expectations were revised/compiled only.
No preceding suite was rerun; historical evidence is not promoted to current regression evidence.

## Source fingerprints (SHA256)

| File | Digest |
|---|---|
| Standard/Networks.cs | 6BFC078846547CAB83CAA6F64B24C80F3B579239E8F875CD29914D7AFED6F12E |
| Standard/WorkEffects.cs | 9ED21D1A83BCF3EBB86E111F03694611B80754A53BC667D5D9643FF08FA5DA94 |
| Standard/InfrastructureModule.cs | E4CC8FDF75924930E27C05162CDCBC1AAA5890CF292D174680143148387A3DE8 |
| Kernel/SaveGame.cs | 5D82FEF1D078F0C8A7D85BE1D0917FF109216D8757E08CF57FAD0BEE0F5D29CB |
| tests/LineSegmentTests.cs | 6119B6558CC5944E5FAF9770626B47212A6BF494E37F974AA93C2F2C328C83E1 |
| proof/line-segments.json | 5FF08F8BA18F3E80FF3A558C740B6F726436D38B49B44BBB1FF17BEE88294877 |

## Remaining work

Utilities/solvers, provider-neutral non-tile corridors, responsibility consolidation beyond native
line intents, reusable-tool/pending-retry matrices, congestion/fleet/global rollback/replay/stress/
allocation/acceptance and the complete framework roadmap remain open. This proof makes no new
world-size limit and implements no rendering or platform adapter.

Lifecycle guidance kept staged scope, existing SOLID owners, focused verification and documentation
aligned; it did not authorize broad tests or claim the roadmap complete.
