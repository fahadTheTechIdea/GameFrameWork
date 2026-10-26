# P7-C9 complete resource transaction cutoff — focused evidence

Date2026-10-15. Engine2.602.0/native save159/replay1. No codec, state payload,
resource balance, reservation book, clock or owner changes.

## Implemented at the original owner

All23 methods of IResourceTransaction now require the original healthy World before
arguments, caller collections, content or endpoint work. The three lifecycle APIs
already entered guarded preflight;20 remaining entry paths now explicitly call
World.RequireHealthyUpdate in the same original ResourceTransactions partial class.
Movement/transfer/conversion/claim algorithms remain unchanged, including their
ordinary healthy refusal semantics. The interface documents this uniform rule.

There is no new facade, compatibility path, fault flag, service, ledger or provider.
The existing first WorldFault remains authoritative; stopped calls do not release
receipts or acquire substitutes. Native build identity advances, save format does not.

## Proof design

The new literal reuses ResourceLifecycleCommitTests' actual faulty-provider factory,
endpoint and healthy stock setup. Their visibility is shared internally for this
test, rather than adding another catalog/inventory fixture. The original independent
AtomicInventory still delegates stock/promises to ResourceInventoryState.

Before fault, the real transaction owner acquires one inbound and one outbound
receipt at the actual endpoint. A clock-changing actual commit latches the original
terminal fault. Reflection enumerates the23 existing interface methods and invokes
each with default input and with caller-list probes where the signature supports
them. Probes count/throw on Count/index/enumeration; no caller access is permitted.
Enumeration includes future interface additions rather than silently testing a list
of only previously fixed methods.

Typed valid ReadClaims, ReleaseClaims and AcquireClaims calls exercise real held
endpoint/owner/receipt data. After refusal, every actual token, owner, amount,
resource and direction remains exact. This is repeated on a genuinely restored
checkpoint; local Pause is explicitly applied after loading to arm the real probe.

While each failed session is stopped, two other actual sessions each earn three
Updates. Their clocks advance and hashes match one another, while the failed
session earns no time and retains its first fault. No healthy hash/save or pending
success fact is possible for the failed session.

## Commands

Two targeted builds only:

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
```

Every runtime is one literal with external60000ms/termination5000ms/output7500:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Resource transaction cutoff: every contract entry stops before caller or provider work" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Resource lifecycle commit: terminal provider violations and verified publication" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Atomic hauling: common quantities, all-participant refusal and exclusive carrier acquisition" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 7500
```

## Every attempt

| Attempt | Actual outcome |
|---|---|
| New proof against old engine build | Clean17.66s; session17414 terminal exit0 |
| New literal RED | 224passed/102failed,687ms; PID16976 exit1/867ms. Terminal calls reached caller lists; valid read/release/acquire succeeded and replaced original held tokens, both new/restored sessions. Stdout tail truncated at authored7500 chars; aggregate totals retained. |
| Complete original-interface guards/build identity | Clean21.20s; session84677 terminal exit0 |
| New cutoff literal FINAL | **326/0**,639ms; PID468 exit0/841ms; no output truncation |
| Existing direct lifecycle commit FINAL | **306/0**,739ms; PID32764 exit0/927ms; no output truncation |
| Existing atomic hauling FINAL | **587/0**,1236ms; PID30320 exit0/1398ms; no output truncation |

Both builds zero warnings/errors; all handles terminal. Build completion preceded
all runtime. No timeout, restarted process or executable edit after these final runs.
Three distinct passing literals1219 checks. This is not a full regression count.

## Scope and remaining work

The cutoff proof covers the complete current transaction interface after an actually
latched provider fault, caller-access ordering, real receipt integrity, new/restored
worlds and three live interleaved unaffected/reference Updates. Direct commit and
physical atomic-haul continuation/refusals remain green under the new guards.

This does not prove every possible provider violation in every healthy transaction
method is detected or latched. Arbitrary CLR/private-state behavior, all optional
modules, all conversion/result/settlement shapes, shuffled lockstep/replay, full
interleaved workloads, stress/soak/allocation/performance remain separate gates.
It is not a new sandbox or a complete P7 compound requirement closure.

Original26 recorded open gates and full objective remain open. No original checkbox
or frozen-audit rewrite; no full/profile/family run, Tycoon/Oil/host/platform/renderer
change, staging, commit or push. Prior checkpoint evidence is historical unless one
of the three exact cases above was explicitly rerun here.
