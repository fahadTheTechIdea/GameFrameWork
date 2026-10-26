# P11-D15 — Authored knowledge forgetting: scoped verification

2026-10-08, canonical Beep.SimEngine only. [Stage](../../plans/enhancements/P11_KNOWLEDGE_FORGETTING_DESIGN.md).
Full P1–P12 goal remains ACTIVE/incomplete; no commit/push before FULL verified completion.

## Implementation and single responsibility

Same CompanyKnowledgeService aliases IKnowledgeForgetting, retains original learned rows and codec3.
Common IKnowledgeSubjectPolicy/KnowledgeSubjectPolicyDef and KnowledgeSubjectKeys now own policy key
contracts/strict nonempty canonical subject parsing/validation for BOTH sharing and forgetting.
Common immutable KnowledgeSubjectSnapshot replaces KnowledgeShareEntrySnapshot/Outgoing directly,
without legacy names/aliases. Sharing retains its directional permission and merge; forgetting retains
owned-company permission and removal. Separate action profiles do not own learned data or clocks.

AuthoredKnowledgeForgettingDef reads master-selected subject/provider keys and explicit companies.
Actual IKnowledgeForgettingProvider opts in and receives complete own-company/provider removed and
retained read-only observations. Provider-wide refusal is projected on its selected entries; another
ready provider cannot override it. Missing observations are idempotent no-ops. Normal command and
private view use the same whole preflight; commit rebuilds it, builds one transient original-ledger
candidate, validates remaining facts through existing providers and commits once. No deletion prefix
on ordinary refusal, new forgotten flag/tombstone/blacklist/history/codec/stock/pacing authority.

Native authored facts/readings opt in; Technology does not. Independent engine-side proof provider
adds prerequisite-bearing subjects/physical capture effects through its declared module and JSON,
then validates whole retained sets through the public capability. No private ledger access, new
scheduler, test knowledge grant, provider-type branch in engine dispatch or parallel storage.
Its immutable authoring captures canonical text and parses detached inputs; original ledger alone
freezes captured observation payloads. Public provider API does not require internal JsonValue.Freeze.

Removing rows revokes original knowledge-derived sight/cursors; not current sensor sight, physical
materials, completed activities or deliberately exported foreign copies. Eligible native source may
start earning dwell again in the same command Update; full authored positive observation is still
required. Actual player sensor disable keeps absent regional history absent. Original physical survey
graph still needs machines/time/materials; forgetting refunds nothing. Expiration still retains history
and is distinct from explicit forgetting. Build2.264.0/save140/ledger schema3/KnowledgeView5; persistent
fields unchanged, no old wire/reader. Sole clock source is unchanged.

## Every build/run attempt

All builds: `dotnet build Beep.SimEngine.slnx --no-restore`.

| Attempt | Exit | Seconds | Warnings/errors | Observed repair |
|---|---|---|---|---|
| 1 | 1 | 4.90 | 0/1 | New public proof incorrectly called internal JsonValue.Freeze; store immutable canonical authoring text and return detached parsed value |
| 2 | 1 | 3.25 | 0/1 | New fixture incorrectly used nonexistent JsonValue.Items; use original Count/index API |
| 3 | 0 | 6.85 | 0/0 | None |
| 4 | 0 | 7.10 | 0/0 | None |

Only runtime command, twice for the SAME exact NEW bounded case:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Knowledge forgetting: private whole removal and physical relearning"`.

1. Exit1,92 passed2 failed0 aborted,532ms: paused saved inbox/twin hashes differed. Authoritative
   GameClock.Sync persists simulation age only, not local pacing controls. Original was paused, newly
   loaded twin was not; it processed pending input. New fixture applies the same explicit Pause to twin.
   Do not change clock serialization or pretend local pause restoration was proven.
2. Exit0,94 passed0 failed0 aborted,663ms. Gameplay/public proof/tests/JSON frozen immediately after green;
   all following changes docs-only. No broader/old/family/platform/stale suites and no stale binary run.

128 older strict build-identity files/lines migrated compile-only.6 old sharing-test API lines migrated
to common snapshot/property names compile-only; existing engine reader migrated in place. Existing
contact factory gains internal assembly access/explicit extra master/doc/module inputs, not gameplay.
Observed663ms is not CPU/allocation budget/full acceptance. Each physical stage bounds1000 Update calls
and emits current clock/position/order/blocker diagnostics if stalled.

## Proven scope

- Original ledger sharing/freshness/forgetting aliases, no economy requirement; actual master policy and
  canonical explicit set. Missing/foreign/malformed/tooling refusal with no leaked facts; absent whole
  removal accepted without invented observations.
- Actual machine movement and positive contact discovery, saved travelling twins. Ordinary sensor disable
  and explicit sharing export facts before removal; recipient state is independent, not global revocation.
- Pure private normal/management previews, frozen entry/data, snapshotted pending intent despite caller
  mutation, paused saved inbox under equal explicit pause controls, resumed saved removal/hash identity.
- Native original learned row absent; original map scope invalidates; permission-first retained cursor
  refuses/clears samples. Foreign exported region remains visible. Retained snapshot keeps old fact.
- Repeated absent removal safe. Actual sensor reenable earns only1of3 observation intervals initially;
  partial saved dwell physically completes a genuinely later new observation. Stock remains20.
- Independent module's positive staffed work physically earns base/dependent facts through original
  WorkBoard/ledger. Whole removed/retained contract vetoes removing base while child remains. Actual
  native participant ready but ordinary command refuses with no native/independent prefix. Removing
  base+child together succeeds, as does saved multi-provider whole removal; foreign copy stays intact.
- Actual native survey spends eight materials20→12 and completes after partial physical saved work.
  Explicit reading removal yields no historical/freshness row and stays removed after load; unselected
  subjects preserved. Positive-time reobserve earns a later original capture; no material refund.
- Interleaved fresh session has no borrowed knowledge; missing profile gives no default deletion power.
 13 invalid JSON/profile forms fail initialization, including actually selected native Technology
  provider lacking forgetting capability. Actually selected independent forgetting provider cannot
  appear in a sharing policy without separate sharing opt-in.

## Open gates and limits

Scope is explicit player-controlled whole forgetting, not automatic deletion on expiry or arbitrary
administrative clear. Automatic aging rules/diplomacy/export revocation/remote exploration, full
research/dependency/time-order/cycle/provider integration, adversarial semantic/codec exceptions and
global mutation, corrupt payloads/ranges, external policy adoption/non-tile/no-work/no-space composition,
large allocations/CPU/replay/lockstep/fuzz/soak and full P11/P1–P12 remain OPEN. Scoped actual-provider
observation guards are not a hostile mod sandbox. Previous runtime gates NOT rerun at2.264.0.
No Oil/Tycoon business source/host/demo/Draw/platform work. Lifecycle review consolidated the common
subject parser/facts and kept all state mutation in the original learned owner.

## Source-only inventory

[Manifest](P11_KNOWLEDGE_FORGETTING_SOURCE_MANIFEST.json) lists selected accumulated source/docs,
not full repository/runtime acceptance. Historical reports/manifests unchanged; own manifest excluded.
Scoped669 paths/137 Markdown/2109 local links/0 broken/0 duplicate paths. Actual `git diff --check`
exit0 with existing LF/CRLF warnings. Sole GameClock source remains:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.
