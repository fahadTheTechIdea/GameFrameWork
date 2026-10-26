# Read-only source-disposition/archive/dependency gate, not source compatibility or gameplay equivalence.
param([switch]$SelfTest)
$ErrorActionPreference = 'Stop'
$frameworkRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$inventory = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'source-inventory.json') -Raw | ConvertFrom-Json
$ownership = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'source-ownership.json') -Raw | ConvertFrom-Json

function AssertSourceOwnership($capture, $assignments) {
    if ($assignments.sourceHead -ne $capture.sourceHead) { throw 'Source capture identity differs from ownership ledger.' }
    if ($assignments.files.Count -ne $capture.files.Count) { throw 'Source disposition count differs from captured inventory.' }
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($entry in $assignments.files) {
        if (-not $seen.Add($entry.sourcePath)) { throw "Duplicate source disposition: $($entry.sourcePath)" }
        if ([string]::IsNullOrWhiteSpace($entry.owner)) { throw "Missing source owner: $($entry.sourcePath)" }
        if ($null -eq $entry.destinations -or $entry.destinations.Count -eq 0) { throw "Missing destination assignment: $($entry.sourcePath)" }
        $targets = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($destination in $entry.destinations) {
            if (-not (Test-Path -LiteralPath $destination -PathType Leaf)) { throw "Missing destination: $destination" }
            $resolved = [System.IO.Path]::GetFullPath($destination)
            if (-not $targets.Add($resolved)) { throw "Duplicate destination assignment: $resolved" }
        }
    }
    foreach ($entry in $capture.files) {
        if (-not $seen.Contains($entry.path)) { throw "Unassigned original file: $($entry.path)" }
    }
}
AssertSourceOwnership $inventory $ownership
if (Test-Path -LiteralPath (Join-Path $inventory.source 'SimEngine')) { throw 'Original runtime directory remains active.' }

# The archive is immutable evidence. Current implementations are intentionally breaking replacements.
$archiveHash = (Get-FileHash -LiteralPath $inventory.recoveryArchive -Algorithm SHA256).Hash
if ($archiveHash -ne $inventory.recoverySha256) { throw 'Recovery archive identity differs from captured inventory.' }
$archive = [System.IO.Compression.ZipFile]::OpenRead($inventory.recoveryArchive)
try {
    if ($archive.Entries.Count -ne $inventory.files.Count) { throw 'Recovery archive has a different source count.' }
    foreach ($entry in $inventory.files) {
        $saved = $archive.GetEntry($entry.path)
        if ($null -eq $saved) { throw "Missing recovery entry: $($entry.path)" }
        $savedStream = $saved.Open()
        $digest = [System.Security.Cryptography.SHA256]::Create()
        try { $savedHash = [System.Convert]::ToHexString($digest.ComputeHash($savedStream)) }
        finally { $digest.Dispose(); $savedStream.Dispose() }
        if ($savedHash -ne $entry.sha256) { throw "Recovery source hash mismatch: $($entry.path)" }
    }
} finally { $archive.Dispose() }

$negativeCount = 0
if ($SelfTest) {
    $capturedJson = $inventory | ConvertTo-Json -Depth 100
    $assignedJson = $ownership | ConvertTo-Json -Depth 100
    $probes = @(
        @{ Label = 'count'; Expected = 'Source disposition count'; Mutate = { param($c, $a) $a.files = @($a.files | Select-Object -Skip 1) } },
        @{ Label = 'duplicate'; Expected = 'Duplicate source disposition'; Mutate = { param($c, $a) $a.files[1].sourcePath = $a.files[0].sourcePath } },
        @{ Label = 'missing-target'; Expected = 'Missing destination:'; Mutate = { param($c, $a) $a.files[0].destinations = @((Join-Path $PSScriptRoot '__not_a_real_disposition_target__.file')) } },
        @{ Label = 'unassigned'; Expected = 'Unassigned original file:'; Mutate = { param($c, $a) $c.files[0].path = 'proof.unassigned.source' } },
        @{ Label = 'owner'; Expected = 'Missing source owner:'; Mutate = { param($c, $a) $a.files[0].owner = '' } }
    )
    foreach ($probe in $probes) {
        $captured = $capturedJson | ConvertFrom-Json -Depth 100
        $assigned = $assignedJson | ConvertFrom-Json -Depth 100
        & $probe.Mutate $captured $assigned
        $failure = $null
        try { AssertSourceOwnership $captured $assigned } catch { $failure = $_.Exception.Message }
        if ($null -eq $failure -or -not $failure.StartsWith($probe.Expected, [System.StringComparison]::Ordinal)) {
            throw "Disposition self-test failed for $($probe.Label): $failure"
        }
        $negativeCount++
    }
}

$frameworkProjects = @(rg --files (Join-Path $frameworkRoot 'src') (Join-Path $frameworkRoot 'tests') (Join-Path $frameworkRoot 'help\quickstart') -g '*.csproj')
foreach ($project in $frameworkProjects) {
    $document = [xml](Get-Content -LiteralPath $project -Raw)
    foreach ($reference in $document.SelectNodes('//ProjectReference')) {
        $resolved = [System.IO.Path]::GetFullPath((Join-Path (Split-Path $project -Parent) $reference.Include))
        if (-not $resolved.StartsWith($frameworkRoot + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Standalone project has an external source dependency: $resolved"
        }
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) { throw "Unresolved framework reference: $resolved" }
    }
}
$runtimeProject = [xml](Get-Content -LiteralPath (Join-Path $frameworkRoot 'src\Beep.SimEngine\Beep.SimEngine.csproj') -Raw)
if ($runtimeProject.SelectNodes('//ProjectReference|//PackageReference').Count -ne 0) { throw 'Framework runtime has a project/package dependency.' }
Write-Output "PASS: $($inventory.files.Count) complete current dispositions and exact archive hashes; $($frameworkProjects.Count) standalone projects; $negativeCount negative self-tests; no original runtime. Source/gameplay equivalence is not asserted."

# Current executable identity, separate from immutable pre-extraction archive evidence.
$records = [System.Collections.Generic.List[string]]::new()
$frameworkFiles = @(rg --files --hidden --no-ignore (Join-Path $frameworkRoot 'src') (Join-Path $frameworkRoot 'tests') (Join-Path $frameworkRoot 'help\quickstart') -g '*.cs' -g '*.json' -g '*.csproj' -g '!bin' -g '!obj')
$frameworkFiles += @((Join-Path $frameworkRoot 'Directory.Build.props'), (Join-Path $frameworkRoot 'Directory.Build.targets'), (Join-Path $frameworkRoot 'Beep.SimEngine.slnx'))
$oilFiles = @(rg --files --hidden --no-ignore $inventory.source -g '*.cs' -g '*.json' -g '*.csproj' -g '*.props' -g '*.targets' -g '*.slnx' -g '!bin' -g '!obj')
foreach ($entry in $frameworkFiles) {
    $relative = [System.IO.Path]::GetRelativePath($frameworkRoot, $entry).Replace('\', '/')
    $records.Add("framework/$relative" + [char]9 + (Get-FileHash -LiteralPath $entry -Algorithm SHA256).Hash)
}
foreach ($entry in $oilFiles) {
    $relative = [System.IO.Path]::GetRelativePath($inventory.source, $entry).Replace('\', '/')
    $records.Add("oil/$relative" + [char]9 + (Get-FileHash -LiteralPath $entry -Algorithm SHA256).Hash)
}
$records.Sort([System.StringComparer]::Ordinal)
$canonical = [string]::Join([char]10, $records) + [char]10
$fingerprint = [System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($canonical)))
Write-Output "Current executable identity: $($records.Count) files; SHA256 $fingerprint"
