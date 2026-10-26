# P6-SO1 current source-disposition audit — verification

## Outcome and scope

The existing ownership verifier is repaired in place. It passes **121 complete source dispositions and exact immutable archive hashes, six standalone project dependency boundaries and five negative self-tests**. Two selected runtime/compiled checks pass **26 assertions, zero failures**. This is source traceability and compiled contract evidence, not complete P6.010/P6.011 or original-assertion behavior equivalence.

Engine unchanged **2.623.0 / save159 / replay1**. Core442 C# path/content SHA256 remains `dce1df4d11b362ef4299f1d232b986f03296ffe73e32e7e9e2ca0f6bfccd2687`. No production/client/Oil/Tycoon implementation changes. Original221/203recorded checked/18open remains unchanged; no original checkbox closes.

## Actual ledger defects and current owners

The initial read-only VerifyExtraction failed at absent `content/fog/fog.json`. Inspecting every declared destination identified13 missing references:

- Fog JSON moved to the explicit Oil visibility content document.
- Former Oil Fog.cs was removed in favor of shared KnowledgeVisibility/SensorVisibility contracts and authored Oil policy.
- Budget/Client/Demo/Game/Input/Net/Offshore/OptionalModule test sources moved from OilCo.Tests to OilCo.PublicProof. Input keeps its separate existing framework validation destination.
- Retired RenderSnapshot is now simulation-only GameSnapshot/PlayerView facts, not a renderer compatibility layer.
- Economy contract resides under the optional Tycoon module.
- The old StandardModule umbrella was removed; explicit Inventory/Work/Motion/Construction/Logistics/Infrastructure/Transformation/Operations/Tycoon modules own their respective registration once.

The original121 identities, source head and recovery archive are unchanged. Only inspected current destination/disposition/owner explanations were reconciled in the existing source-ownership.json; no missing old source was recreated. A one-to-many file decomposition is traceability, not duplicate executable state ownership or a promise of unchanged behavior.

## One verifier, no namespace compatibility fiction

VerifyExtraction previously compared current sources with namespace-normalized pre-extraction text, except a growing authored-change allowlist. That condition cannot describe the explicitly authorized breaking architecture. The obsolete comparison and allowlist were removed, not bypassed with more exceptions or fake files.

The same verifier still requires exact source-head identity,121 assignments, unique source identity, nonempty owner/destinations, real targets, canonical unique targets per source, complete original coverage, absence of the old active runtime, exact archive count/entry hashes, runtime project dependency isolation and resolved in-root project references. Quickstart module/host projects now participate in the same standalone scan.

Optional -SelfTest runs in-memory count/duplicate-source/missing-target/unassigned-source/ownerless mutations through this same validator and requires the intended error for each. No real file or archive is modified by self-tests. There is no second ownership runner or source compatibility path.

## Focused evidence and all executed verification

| Attempt | Result | Evidence |
|---|---|---|
| Original current ownership script |exit1 | Missing old fog JSON destination; stopped before claiming archive/current ownership success |
| Reconciled verifier `./docs/migration/VerifyExtraction.ps1 -SelfTest` |exit0,1.935s tool wall |121 current dispositions/archive hashes;6 projects;5 negative cases; no original runtime |
| Compiled external-module boundary |10/0 | child32476 exit0;130ms suite/292ms process; output untruncated |
| Manifest ownership/inactive/frozen content |16/0 | child17088 exit0;74ms suite/234ms process; output untruncated |

The two literal processes ran in parallel, using60s timeout/5s termination. No full/profile/family runtime, aborted suite or live handle. Existing binaries were built against the unchanged current C# sources in the preceding stage; no new build was needed for JSON/PowerShell-only changes.

```
Framework independence: compiled external modules use public engine contracts only
Composition: manifest ownership, inactive providers and frozen content
```

The compiled audit inspected6,041 ExtensionProof method bodies/30,630 member references and5 QuickGame.Module bodies/15 references, with zero boundary violations. It verifies public engine access/no friendship/reflection bypass, not all possible module gameplay behavior.

A combined read-only host-inventory command initially had an invalid PowerShell foreach pipeline; it failed before execution and was rerun with the correct assignment/pipeline. No semantic result was inferred from that diagnostic. `git diff --check` exits0 for ledger/verifier, with only LF-to-CRLF repository warnings.

## Source identities

- source-ownership.json SHA256: `5A0E063DB48D8EA6DFF5DE823E90BB4DB98B2732BB7E2A4E716E060DC277AC0A`.
- VerifyExtraction.ps1 SHA256: `F7553955EEA24FA382A883303CBCDA678EA292F96C102D984C4BC311D2ACB920`.
- unchanged source-inventory.json SHA256: `D4AE781927BB278FD33FD61B2CF63D4E40CB53292937DAB85950FA300816DEEA`.
- Verifier's selected current executable-input identity:2,079 files, SHA256 `D27AC49694A62F917876E8A558DFEAB32DED93EADD9A27B2A0220D3E7D832C0C`. This includes framework/Oil/quickstart selected inputs and is separate from the442-file core identity and immutable archive identity.

## Client observations and remaining work

The active five Oil client sources total513 lines: LiveDemo47, Harness86, TextClient96, FieldPlayer48 and CommandShell236. Quickstart Host and FieldPlayer were inspected directly. Quickstart only composes, updates, reads snapshots and verifies save/hash. FieldPlayer requests engine-suggested founding and the existing engine autopilot; it does not reimplement expansion/production/repair rules. The other client bodies and all CLI/horizon behavior are **not** claimed fully audited or executed by this slice.

The original assertion-ownership manifest is separate; its moved Oil owner paths and subsequent breaking assertion changes still need explicit current reconciliation. The repaired source verifier does not pretend to prove assertion equivalence or replace that audit. Thus original P6.010/P6.011 remain open, along with the rest of the full plan.

Lifecycle guidance kept this as one staged current-owner repair. No engine/business/domain rule, host source, platform/Draw, compatibility shim, full/profile/family test, staging, commit or push.
