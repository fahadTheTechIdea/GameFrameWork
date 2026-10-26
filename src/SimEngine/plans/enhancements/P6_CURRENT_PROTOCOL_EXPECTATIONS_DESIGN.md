# P6-PE1 — Current native protocol expectations

Status: VERIFIED-COMPILE/STATIC ONLY; one clean solution compile10.58s,31 preserved canonical nonprotocol source hashes,1,574 assertions and31 registrations. No runtime or gameplay pass claim; original gates unchanged.

## Problem and outcome

32 positive current-engine build assertions in31 engine test files still expect historical2.x identities;14 additional lines contain stale positive global save-format expectations. These cannot pass on the actual current engine and create false regressions before checking gameplay. The existing StateCodecTests fixture independently reads current-engine-protocol.json; it already owns current build/save expectations.

Migrate ONLY positive c.Equal literal build→EngineInfo.Build and stale literal global save→EngineInfo.SaveFormat expectations to that existing fixture. Keep all actual value expressions, messages, behavioral assertions, current stable codec-specific assertions, negative historical/corrupt version tests, registrations, handlers, ordering and exclusions unchanged. No generated expected values from engine constants and no new baseline owner, compatibility reader, game rule or altered runtime profile.

## Verification

Preserve per-file source hash, canonicalized nonprotocol source hash, assertion count and exact registration lines before edits. After edits require identical canonicalized source, assertion counts and registration lines; no positive literal build or stale global save remains. Compile once after the whole mechanical change. Do NOT rerun old/family/full tests; runtime behavior remains unverified until the separately authorized final regression after implementation. Engine2.489.0/save154/replay1 and all production/proof implementation, sole clock, optional Tycoon/Oil/platform/Draw sources remain unchanged.

## Original gates and limits

This improves final validation readiness; it does not close P6.006 assertion migration ownership, profile/durability/gameplay/full gates or count as newly passing gameplay tests. Original221/187checked/34open unchanged. No original assertion is deleted or relaxed to match a runtime-read value. No staging/commit/push before the whole goal AND plan finish.

## Source freeze and metadata closure

Freeze2026-10-12 10:44:53 UTC:1,741 source anchors/976 selected executable inputs/13 quickstart anchors match actual files. Inventory2,927 files/1,043 Markdown documents/8,702 local links/0 broken links;18 valid master rows;221 original literal gate/line anchors validated with0 errors. Original187 checked/34 open unchanged, additional218. Current31 test-input edits include26 previously selected inputs plus5 previously existing test inputs newly frozen; no new executable file or production/proof drift. Sole clock/all optional Tycoon source identities unchanged. git diff --check exit0 with34 pre-existing warning lines;2,915 dirty entries/0 staged. No runtime, no commit/push; current source hashes are compile/static evidence only.
