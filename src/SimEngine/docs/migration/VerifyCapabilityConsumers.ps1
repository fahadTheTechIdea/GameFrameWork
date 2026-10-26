param([string]$Repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')))
$ErrorActionPreference = 'Stop'
$source = Join-Path $Repository 'src/Beep.SimEngine'
$files = @(Get-ChildItem -LiteralPath $source -Recurse -Filter '*.cs' | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Sort-Object FullName)
$contents = @{}; $parts = @(); $systems = @(); $anchors = @()
foreach ($file in $files) {
    $path = [IO.Path]::GetRelativePath($Repository, $file.FullName).Replace('\','/')
    $text = Get-Content -LiteralPath $file.FullName -Raw; $contents[$path] = $text
    $anchors += [pscustomobject]@{path=$path;sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
    foreach ($match in [regex]::Matches($text,'\bclass\s+(\w+)(?:\s*<[^>]+>)?\s*:\s*Component\b')) {
        if ($match.Groups[1].Value -ne 'Component') { $parts += $match.Groups[1].Value }
    }
    foreach ($match in [regex]::Matches($text,'\bclass\s+(\w+)\s*:\s*GameSystem\b')) { $systems += $match.Groups[1].Value }
}
$parts = @($parts | Sort-Object -Unique); $systems = @($systems | Sort-Object -Unique)
$queries = @(); $violations = @(); $identity = @(); $systemSources = @()
foreach ($path in @($contents.Keys | Sort-Object)) {
    $text = $contents[$path]
    foreach ($match in [regex]::Matches($text,'\.(?:Get|GetAll|TryGet|Has|HasCapability|Components|Component)\s*<([^>]+)>')) {
        $type = $match.Groups[1].Value.Trim()
        $line = 1 + [regex]::Matches($text.Substring(0,$match.Index),"`n").Count
        $queries += [pscustomobject]@{path=$path;line=$line;type=$type}
        if ($parts -contains $type) { $violations += [pscustomobject]@{path=$path;line=$line;kind='native-runtime-slot-query';type=$type} }
    }
    foreach ($type in $parts) {
        foreach ($match in [regex]::Matches($text,'\b(?:is|as)\s+'+[regex]::Escape($type)+'\b|(?<!\w)\(\s*'+[regex]::Escape($type)+'\s*\)')) {
            $prefix = $text.Substring(0,$match.Index)
            if ($prefix -match 'typeof\s*$') { continue }
            $line = 1 + [regex]::Matches($prefix,"`n").Count
            $violations += [pscustomobject]@{path=$path;line=$line;kind='native-runtime-type-branch';type=$type}
        }
        foreach ($match in [regex]::Matches($text,'typeof\s*\(\s*'+[regex]::Escape($type)+'\s*\)')) {
            $line = 1 + [regex]::Matches($text.Substring(0,$match.Index),"`n").Count
            $identity += [pscustomobject]@{path=$path;line=$line;type=$type}
        }
    }
    foreach ($system in $systems) {
        if ($text -match ('\bclass\s+'+[regex]::Escape($system)+'\b')) {
            $systemSources += [pscustomobject]@{system=$system;path=$path;queries=@($queries | Where-Object path -eq $path)}
        }
    }
}
[pscustomobject]@{
    schema=1;scope='All production C# sources, excluding build output; original system partial files included. Structural evidence, not arbitrary-provider or whole-runtime acceptance.'
    source_count=$files.Count;runtime_component_types=$parts;system_types=$systems;system_sources=$systemSources
    native_runtime_query_or_cast_violations=$violations;codec_owner_identity_sites=$identity
    typed_query_sites=$queries.Count;source_anchors=$anchors
} | ConvertTo-Json -Depth 8
if ($violations.Count -ne 0) { exit 1 }
