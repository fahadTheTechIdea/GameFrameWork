[CmdletBinding()]
param([switch]$AuditPackages, [string]$DotnetPath = 'dotnet')

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$isolatedDotnet = Join-Path $projectRoot 'artifacts/toolchain/dotnet/dotnet.exe'
if ($DotnetPath -eq 'dotnet' -and (Test-Path -LiteralPath $isolatedDotnet)) { $DotnetPath = $isolatedDotnet }
$verificationRoot = Join-Path $projectRoot 'artifacts/verification'
New-Item -ItemType Directory -Path $verificationRoot -Force | Out-Null
$records = [System.Collections.Generic.List[object]]::new()
$runId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffff')
$previousFailures = @()
$priorPath = Join-Path $verificationRoot 'evidence.json'
if (Test-Path -LiteralPath $priorPath) {
    $prior = Get-Content -Raw -LiteralPath $priorPath | ConvertFrom-Json -AsHashtable
    if ($prior.Contains('previousFailures')) { $previousFailures += @($prior.previousFailures) }
    $failedChecks = @($prior.checks | Where-Object { $_.status -eq 'failed' })
    if ($failedChecks.Count -gt 0) {
        $previousFailures += @{ checkedAtUtc = $prior.checkedAtUtc; checks = $failedChecks; sourceHashes = $prior.sourceHashes }
    }
}
$sourceHashPaths = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src'),(Join-Path $projectRoot 'samples'),(Join-Path $projectRoot 'tests'),(Join-Path $projectRoot 'tools/ScenarioSimulator') -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Extension -in @('.cs','.csproj','.json') })
$sourceHashPaths += @('global.json','Directory.Build.props','Directory.Packages.props','ScenarioEngine.slnx','docs/sdd/contracts/content.schema.json','tools/Verify-ScenarioEngine.ps1') |
    ForEach-Object { Get-Item -LiteralPath (Join-Path $projectRoot $_) }
$sourceHashes = @($sourceHashPaths | ForEach-Object {
    @{ path = [System.IO.Path]::GetRelativePath($projectRoot, $_.FullName).Replace('\','/'); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
})
function Invoke-EngineCheck([string]$Name, [string[]]$Arguments) {
    $logPath = Join-Path $verificationRoot "$Name-$runId.log"
    & $DotnetPath @Arguments 2>&1 | Tee-Object -FilePath $logPath
    $result = $LASTEXITCODE
    $records.Add(@{
        name = $Name; executable = $DotnetPath; command = 'dotnet ' + ($Arguments -join ' ')
        checkedAtUtc = [DateTime]::UtcNow.ToString('o'); exitCode = $result
        status = $(if ($result -eq 0) { 'verified' } else { 'failed' })
        logPath = [System.IO.Path]::GetRelativePath($projectRoot, $logPath).Replace('\','/')
    })
    if ($result -ne 0) { throw "$Name failed with exit code $result" }
}
Push-Location $projectRoot
try {
    Invoke-EngineCheck 'toolchain' @('--info')
    Invoke-EngineCheck 'restore' @('restore','ScenarioEngine.slnx','--locked-mode','--source','https://api.nuget.org/v3/index.json')
    Invoke-EngineCheck 'build' @('build','ScenarioEngine.slnx','-c','Release','--no-restore')
    $trx = Join-Path $verificationRoot 'scenario-engine.trx'
    Invoke-EngineCheck 'engine-tests' @('run','--project','tests/ScenarioEngine.Tests/ScenarioEngine.Tests.csproj','-c','Release','--no-build','--no-restore','--','-noColor','-result-trx',$trx)
    Invoke-EngineCheck 'counter-demo' @('run','--project','tools/ScenarioSimulator/ScenarioSimulator.csproj','-c','Release','--no-build','--no-restore','--','samples/Counter.GameLayer/counter.json','artifacts/counter-demo')
    if ($AuditPackages) {
        Invoke-EngineCheck 'dependency-audit' @('list','ScenarioEngine.slnx','package','--vulnerable','--include-transitive','--source','https://api.nuget.org/v3/index.json')
    }
}
finally {
    Pop-Location
    @{
        checkedAtUtc = [DateTime]::UtcNow.ToString('o')
        environment = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
        scope = 'Standalone C# engine foundation and Counter/Signal plugins; no Godot execution'
        checks = @($records); sourceHashes = $sourceHashes; previousFailures = $previousFailures
        limitations = @('Self-review','Windows only','No Godot host or LostSoldier gameplay','No physical navigation','No power-loss injection or release acceptance')
    } | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $verificationRoot 'evidence.json') -Encoding utf8
}
