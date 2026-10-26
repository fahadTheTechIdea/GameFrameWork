# P11-D8 — Typed physical spatial observations

2026-10-07. Scoped implementation verified:66 checks/ONE NEW bounded case,0 failures/aborts396ms.
Two builds: first2 catalog CS0311 errors0 warnings3.12s, base-Def alias repair; final clean5.54s.
Full framework goal stays active. [Evidence](../../docs/migration/P11_SPATIAL_OBSERVATIONS_VERIFICATION.md).

Follow-up [D8a native timed surveys](P11_NATIVE_SURVEY_STARTUP_DESIGN.md) now proves native physical
travel/materials/capture/refresh through engine-only JSON startup/navigation/work. D8 historical
runtime is NOT rerun on2.232.0/save136. Dynamic coverage/sharing/expiry/broad gates remain open.

## 0. Problem

Native KnowledgeEffectDef is restricted to fixed KnowledgeFactDef values. D7 enables remembered
refresh but no native spatial subject uses it. Layer views remain live authorized readings, not history.

## 1. Goals

1. SAME authored provider/physical effect accepts contract-defined subjects, including fixed facts.
2. JSON tile samples capture actual byte/int/fixed data at an owned, physically worked site.
3. Public non-tile subject extends contracts without scheduler or ledger changes.
4. Remembered readings do not follow later world changes until physical reobservation completes.
5. Partial work, private ownership, cancellation and save/load use existing owners and clock.

## 2. Design

IKnowledgeSubjectDefinition/KnowledgeSubjectDef supply explicit retention, target admission, current
capture and payload validation. KnowledgeFactDef becomes its permanent fixed-value implementation.
SAME AuthoredKnowledgeProvider validates subjects; SAME KnowledgeEffectDef resolves `subject`,
checks actual target before capture, and delegates to SAME ICompanyKnowledge. Old `fact` parser removed.

SpatialReadingDef resolves an actual layer, explicit first/latest retention and provider-owned location.
Ownership and actual session/layer/presence precede capture; unknown sight is intentionally not needed
to survey an owned site. Positive physical work ancestry still mandatory. Payload contains stable
space/layer IDs and fixed raw sample value; provider value kind/quantity legality validate it. Native
TileReadingDef supports one authored tile inside target footprint. Public point definition uses its
own actual geometry/value owner. No world-size limit, region copy or second live sample authority.

This is discrete authored site observation, not dynamic sensor coverage or whole-world exploration.
Views expose frozen remembered data through existing KnowledgeView, not raw provider handles.

## 3. API

Required typed subject contract; abstract spatial reading definition extension. Existing effect JSON
uses `subject`; native `tile_readings` section. SAME knowledge provider schema2; build2.231.0/save135.
Ledger codec/view2 and all other state layouts unchanged. No legacy reader or compatibility facade.

## 4. Files

CompanyKnowledge.cs/CompanyKnowledgeModule.cs; new SpatialReadings.cs; existing KnowledgeProof.cs;
public PointReadingProof.cs; actual master JSON/proof registration; one new bounded test; direct old
JSON/identity migration only; tracker/README/AGENTS/help/evidence.

## 5. Errors

Missing subject/layer/invalid policy/location fail authoring. Wrong site/private owner/unselected
space refuses before capture. Invalid historical payload refuses decode. No fabricated sample/default.

## 6. Compatibility

Development direct migration; delete fixed-fact-only effect path and `fact` authoring field.

## 7. Verification

ONE new bounded case, physical point observation/refresh and labeled native tile protocol checks.
Build then exact case; repair only observed failure. Older identity/JSON migrate and compile only.
Freeze code/proof/JSON/tests after green. No family/full/platform/host/OilFieldDays run.

## 8. Risks

Observation is historical, never stock or spend authority. Preserve linked resource quantity legality,
sample ownership and actual presence; permanent snapshots must not overwrite changed values.

## 9. Exclusions

Dynamic sensors, sample sharing/forgetting/expiry, arbitrary global provider purity/privacy, replay/
stress/full gates, OilFieldDays/hosts/demo/Draw/platform/business gameplay, commit/push.

## 10. Sequence

1. Extend SAME authored subject/provider/effect and native tile implementation.
2. Author public non-tile subject and actual JSON physical work; direct old content migration.
3. Build/ONE bounded case, then evidence/tracker/inventory docs only.
