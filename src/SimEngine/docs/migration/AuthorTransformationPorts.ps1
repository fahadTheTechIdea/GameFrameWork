# One-time continuous catalyst rate authoring migration. Not a loader or compatibility path.
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$files = @(rg --files (Join-Path $repository 'tests/ExtensionProof/content') (Join-Path $repository 'help/quickstart/Content') -g '*.json')
$changed = @(); $recipes = 0
foreach ($file in $files) {
    $raw = [IO.File]::ReadAllText($file)
    $document = [System.Text.Json.JsonDocument]::Parse($raw)
    try {
        $updated = $raw
        foreach ($section in $document.RootElement.EnumerateObject()) {
            if ($section.Name -ne 'transformations') { continue }
            foreach ($row in $section.Value.EnumerateArray()) {
                if ($row.GetProperty('mode').GetString() -ne 'continuous') { continue }
                $consumption = $row.GetProperty('tool_consumption')
                if ($consumption.ValueKind -eq [System.Text.Json.JsonValueKind]::Array) { continue }
                if ($consumption.ValueKind -ne [System.Text.Json.JsonValueKind]::Object -or @($consumption.EnumerateObject()).Count) {
                    throw "Continuous catalyst migration requires an explicit empty prior completion Bill in $file"
                }
                $original = $row.GetRawText()
                $replacement = [regex]::Replace($original, '"tool_consumption"\s*:\s*\{\s*\}', '"tool_consumption":[]')
                if ($replacement -eq $original -or !$updated.Contains($original)) { throw "Ambiguous continuous rate migration in $file" }
                $updated = $updated.Replace($original, $replacement); $recipes++
            }
        }
        if ($updated -ne $raw) {
            [IO.File]::WriteAllText($file, $updated, [Text.UTF8Encoding]::new($false))
            $changed += [IO.Path]::GetRelativePath($repository, $file).Replace('\','/')
        }
    } finally { $document.Dispose() }
}
[pscustomobject]@{files=$changed;continuous_rate_arrays=$recipes} | ConvertTo-Json -Depth 4
