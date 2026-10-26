# Design validation only. This does not run the game or prove native-world reachability.
[CmdletBinding()]
param([switch]$NoReport)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sddRoot = Join-Path $projectRoot 'docs/sdd'
$contentSchemaPath = Join-Path $sddRoot 'contracts/content.schema.json'
$layerSchemaPath = Join-Path $sddRoot 'contracts/lost_soldier.schema.json'
$fixturePath = Join-Path $sddRoot 'examples/river_crossing.json'
$layerSchema = Get-Content -Raw -LiteralPath $layerSchemaPath | ConvertFrom-Json -AsHashtable
$checkResults = [System.Collections.Generic.List[object]]::new()
$detailsRoot = Join-Path $sddRoot 'details'
$catalogPath = Join-Path $detailsRoot 'catalog.json'
$detailSources = [System.Collections.Generic.List[string]]::new()
$definitionCount = 0
$definitionNegativeCount = 0

function Assert-Sdd([bool]$Condition, [string]$Code, [string]$Message) {
    if (-not $Condition) { throw "$Code : $Message" }
}

function Test-StrictJson([string]$Raw) {
    $json = [System.Text.Json.JsonDocument]::Parse($Raw)
    try { Assert-UniqueProperties $json.RootElement } finally { $json.Dispose() }
}

function Assert-UniqueProperties($Element) {
    if ($Element.ValueKind -eq [System.Text.Json.JsonValueKind]::Object) {
        $names = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        foreach ($property in $Element.EnumerateObject()) {
            Assert-Sdd ($names.Add($property.Name)) 'JSON_DUPLICATE_PROPERTY' $property.Name
            Assert-UniqueProperties $property.Value
        }
    } elseif ($Element.ValueKind -eq [System.Text.Json.JsonValueKind]::Array) {
        foreach ($child in $Element.EnumerateArray()) { Assert-UniqueProperties $child }
    }
}

function Assert-Schema($Value, [string]$Path, [string]$Code) {
    $json = ConvertTo-Json -Depth 100 -InputObject $Value -Compress
    $valid = Test-Json -Json $json -SchemaFile $Path -ErrorAction SilentlyContinue
    Assert-Sdd $valid $Code 'JSON Schema rejected data'
}

function Get-ConditionNodeCount($Node) {
    $count = 1
    if ($Node.Contains('all') -or $Node.Contains('any')) {
        $children = if ($Node.Contains('all')) { $Node.all } else { $Node.any }
        foreach ($child in $children) { $count += Get-ConditionNodeCount $child }
    } elseif ($Node.Contains('not')) {
        $count += Get-ConditionNodeCount $Node.not
    }
    return $count
}

function Assert-Condition($Node, $FactTypes, [int]$Depth = 1) {
    if ($Depth -eq 1) {
        Assert-Sdd ((Get-ConditionNodeCount $Node) -le 64) 'CONDITION_SIZE' 'Maximum condition nodes is 64'
    }
    Assert-Sdd ($Depth -le 8) 'CONDITION_DEPTH' 'Maximum condition depth is 8'
    if ($Node.Contains('all') -or $Node.Contains('any')) {
        $children = if ($Node.Contains('all')) { $Node.all } else { $Node.any }
        foreach ($child in $children) { Assert-Condition $child $FactTypes ($Depth + 1) }
    } elseif ($Node.Contains('not')) {
        Assert-Condition $Node.not $FactTypes ($Depth + 1)
    } else {
        Assert-Sdd ($FactTypes.Contains($Node.fact)) 'CONDITION_FACT' $Node.fact
        $type = $FactTypes[$Node.fact].type
        $isInteger = ($Node.value -is [long] -or $Node.value -is [int] -or $Node.value -is [bigint])
        $isValid = switch ($type) {
            'boolean' { $Node.value -is [bool] }
            'integer' { $isInteger }
            'string' { $Node.value -is [string] }
        }
        Assert-Sdd $isValid 'CONDITION_TYPE' $Node.fact
        Assert-Sdd (($Node.op -in @('eq','ne')) -or $isInteger) 'CONDITION_OPERATOR' $Node.fact
        if ($isInteger) {
            $definition = $FactTypes[$Node.fact]
            if ($definition.Contains('minimum')) {
                Assert-Sdd ($Node.value -ge $definition.minimum) 'CONDITION_RANGE' $Node.fact
            }
            if ($definition.Contains('maximum')) {
                Assert-Sdd ($Node.value -le $definition.maximum) 'CONDITION_RANGE' $Node.fact
            }
        }
    }
}

function Test-Condition($Node, $Facts) {
    if ($Node.Contains('all')) {
        foreach ($child in $Node.all) { if (-not (Test-Condition $child $Facts)) { return $false } }
        return $true
    }
    if ($Node.Contains('any')) {
        foreach ($child in $Node.any) { if (Test-Condition $child $Facts) { return $true } }
        return $false
    }
    if ($Node.Contains('not')) { return (-not (Test-Condition $Node.not $Facts)) }
    Assert-Sdd ($Facts.Contains($Node.fact)) 'CASE_MISSING_FACT' $Node.fact
    $actual = $Facts[$Node.fact]
    switch ($Node.op) {
        'eq' { return ($actual -ceq $Node.value) }
        'ne' { return ($actual -cne $Node.value) }
        'lt' { return ($actual -lt $Node.value) }
        'lte' { return ($actual -le $Node.value) }
        'gt' { return ($actual -gt $Node.value) }
        'gte' { return ($actual -ge $Node.value) }
    }
    throw 'CONDITION_OPERATOR : unsupported comparison'
}

function Visit-Objective([string]$Id, $Objectives, $Visiting, $Visited) {
    Assert-Sdd (-not $Visiting.Contains($Id)) 'OBJECTIVE_CYCLE' $Id
    if ($Visited.Contains($Id)) { return }
    [void]$Visiting.Add($Id)
    foreach ($dependency in $Objectives[$Id].dependencies) {
        Assert-Sdd ($Objectives.Contains($dependency.objectiveId)) 'PACK_REFERENCE' $dependency.objectiveId
        Visit-Objective $dependency.objectiveId $Objectives $Visiting $Visited
    }
    [void]$Visiting.Remove($Id)
    [void]$Visited.Add($Id)
}

function Assert-Pack($Pack) {
    Assert-Schema $Pack $contentSchemaPath 'PACK_SCHEMA'
    Assert-Sdd ($Pack.manifest.gameLayerId -ceq 'lost_soldier') 'LAYER_UNAVAILABLE' 'Fixture requires lost_soldier'
    $byKind = @{}
    $declaredPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($file in $Pack.manifest.files) {
        Assert-Sdd ($declaredPaths.Add($file.path)) 'PACK_DUPLICATE_PATH' $file.path
        Assert-Sdd ($Pack.documents.Contains($file.path)) 'PACK_REFERENCE' $file.path
        Assert-Sdd ($Pack.documents[$file.path].kind -ceq $file.kind) 'PACK_KIND' $file.path
    }
    Assert-Sdd ($declaredPaths.Count -eq $Pack.documents.Count) 'PACK_UNDECLARED' 'Unexpected document'
    foreach ($document in $Pack.documents.Values) {
        if (-not $byKind.Contains($document.kind)) { $byKind[$document.kind] = @{} }
        Assert-Sdd (-not $byKind[$document.kind].Contains($document.id)) 'PACK_DUPLICATE_ID' $document.id
        $byKind[$document.kind][$document.id] = $document
    }
    $factTypes = $layerSchema['x-facts'].Clone()
    $factTypes['core.tick'] = @{ type = 'integer'; minimum = 0 }
    foreach ($objectiveId in $byKind.objective.Keys) {
        $factTypes["core.objective.$objectiveId.status"] = @{ type = 'string' }
    }
    $localization = @{}
    foreach ($locale in $byKind.localization.Values) {
        if ($locale.locale -ceq 'en') {
            foreach ($key in $locale.entries.Keys) { $localization[$key] = $locale.entries[$key] }
        }
    }
    foreach ($document in $Pack.documents.Values) {
        foreach ($field in @('titleKey', 'summaryKey')) {
            if ($document.Contains($field)) {
                Assert-Sdd ($localization.Contains($document[$field])) 'LOCALIZATION_MISSING' $document[$field]
            }
        }
    }
    foreach ($entry in $Pack.manifest.entryScenarioIds) {
        Assert-Sdd ($byKind.scenario.Contains($entry)) 'PACK_REFERENCE' $entry
    }
    foreach ($scenario in $byKind.scenario.Values) {
        Assert-Schema $scenario.layerData $layerSchemaPath 'LAYER_SCHEMA'
        foreach ($zoneId in $scenario.zoneIds) {
            Assert-Sdd ($byKind.zone.Contains($zoneId)) 'PACK_REFERENCE' $zoneId
        }
        Assert-Sdd ($scenario.startZoneId -cin $scenario.zoneIds) 'PACK_REFERENCE' 'startZoneId'
        Assert-Condition $scenario.closingCondition $factTypes
        $priorities = [System.Collections.Generic.HashSet[long]]::new()
        foreach ($endingId in $scenario.endingIds) {
            Assert-Sdd ($byKind.ending.Contains($endingId)) 'PACK_REFERENCE' $endingId
            $ending = $byKind.ending[$endingId]
            Assert-Sdd ($priorities.Add([long]$ending.priority)) 'ENDING_PRIORITY_DUPLICATE' $endingId
            Assert-Condition $ending.condition $factTypes
        }
        foreach ($zoneId in $scenario.zoneIds) {
            $zone = $byKind.zone[$zoneId]
            Assert-Sdd ($zone.sceneRole -cin $layerSchema['x-asset-roles']) 'ASSET_ROLE' $zone.sceneRole
            $anchorIds = @($zone.anchors | ForEach-Object { $_.id })
            Assert-Sdd (@($anchorIds | Select-Object -Unique).Count -eq $anchorIds.Count) 'ANCHOR_DUPLICATE' $zoneId
            foreach ($exit in $zone.exits) {
                Assert-Sdd ($exit.targetZoneId -cin $scenario.zoneIds) 'PACK_REFERENCE' $exit.targetZoneId
                Assert-Sdd ($exit.anchorId -cin $anchorIds) 'ANCHOR_REFERENCE' $exit.anchorId
                Assert-Condition $exit.condition $factTypes
            }
        }
        # Potential connectivity only: exit conditions and native geometry are not simulated here.
        $seen = [System.Collections.Generic.HashSet[string]]::new()
        $queue = [System.Collections.Generic.Queue[string]]::new()
        $queue.Enqueue($scenario.startZoneId)
        while ($queue.Count -gt 0) {
            $current = $queue.Dequeue()
            if (-not $seen.Add($current)) { continue }
            foreach ($exit in $byKind.zone[$current].exits) { $queue.Enqueue($exit.targetZoneId) }
        }
        Assert-Sdd ($seen.Count -eq $scenario.zoneIds.Count) 'ZONE_UNREACHABLE' 'Potential graph is disconnected'
        foreach ($objectiveId in $scenario.objectiveIds) {
            Assert-Sdd ($byKind.objective.Contains($objectiveId)) 'PACK_REFERENCE' $objectiveId
            $objective = $byKind.objective[$objectiveId]
            foreach ($field in @('activateWhen','completeWhen','failWhen')) { Assert-Condition $objective[$field] $factTypes }
            foreach ($effect in @($objective.successEffects) + @($objective.failureEffects)) {
                Assert-Sdd ($layerSchema['x-effects'].Contains($effect.type)) 'EFFECT_UNKNOWN' $effect.type
                $argumentSchema = ConvertTo-Json -Depth 100 -InputObject $layerSchema['x-effects'][$effect.type] -Compress
                $argumentJson = ConvertTo-Json -Depth 100 -InputObject $effect.arguments -Compress
                Assert-Sdd (Test-Json -Json $argumentJson -Schema $argumentSchema -ErrorAction SilentlyContinue) 'EFFECT_ARGUMENTS' $effect.type
            }
        }
        $visiting = [System.Collections.Generic.HashSet[string]]::new()
        $visited = [System.Collections.Generic.HashSet[string]]::new()
        foreach ($objectiveId in $scenario.objectiveIds) { Visit-Objective $objectiveId $byKind.objective $visiting $visited }
        $actorIds = @($scenario.layerData.actors | ForEach-Object { $_.id })
        Assert-Sdd (@($actorIds | Select-Object -Unique).Count -eq $actorIds.Count) 'ACTOR_DUPLICATE' $scenario.id
        foreach ($actor in $scenario.layerData.actors) {
            Assert-Sdd ($actor.zoneId -cin $scenario.zoneIds) 'PACK_REFERENCE' $actor.zoneId
            Assert-Sdd ($actor.spawnAnchorId -cin @($byKind.zone[$actor.zoneId].anchors | ForEach-Object { $_.id })) 'ANCHOR_REFERENCE' $actor.spawnAnchorId
            if ($actor.role -cin @('sentry','patrol','officer')) {
                $profile = @($scenario.layerData.aiProfiles | Where-Object { $_.id -ceq $actor.aiProfileId })
                Assert-Sdd ($profile.Count -eq 1 -and $profile[0].zoneId -ceq $actor.zoneId) 'AI_PROFILE_REFERENCE' $actor.id
            }
        }
        foreach ($profile in $scenario.layerData.aiProfiles) {
            Assert-Sdd ($profile.zoneId -cin $scenario.zoneIds) 'PACK_REFERENCE' $profile.zoneId
            foreach ($anchor in $profile.patrolAnchorIds) {
                Assert-Sdd ($anchor -cin @($byKind.zone[$profile.zoneId].anchors | ForEach-Object { $_.id })) 'ANCHOR_REFERENCE' $anchor
            }
        }
        foreach ($actorId in $scenario.layerData.startingParty) {
            Assert-Sdd ($actorId -cin $actorIds) 'PARTY_REFERENCE' $actorId
        }
        foreach ($contact in $scenario.layerData.contacts) {
            Assert-Sdd ($contact.actorId -cin $actorIds) 'CONTACT_REFERENCE' $contact.actorId
        }
        foreach ($interaction in $scenario.layerData.interactions) {
            Assert-Sdd ($interaction.zoneId -cin $scenario.zoneIds) 'PACK_REFERENCE' $interaction.zoneId
            Assert-Sdd ($interaction.targetAnchorId -cin @($byKind.zone[$interaction.zoneId].anchors | ForEach-Object { $_.id })) 'ANCHOR_REFERENCE' $interaction.targetAnchorId
            $resultByAction = @{ recover = 'radio_recovered'; repair = 'boat_ready'; trade = 'ferry_ready'; report = 'mission_delivered'; treat = 'treated'; escort = 'contact_rescued'; rest = 'rested' }
            Assert-Sdd ($interaction.completionResult -ceq $resultByAction[$interaction.actionType]) 'LAYER_ACTION_RESULT' $interaction.id
        }
        foreach ($case in $Pack.abstractCases) {
            foreach ($factName in $case.facts.Keys) {
                Assert-Condition @{ fact = $factName; op = 'eq'; value = $case.facts[$factName] } $factTypes
            }
            $matches = @()
            if (Test-Condition $scenario.closingCondition $case.facts) {
                $matches = @($scenario.endingIds | Where-Object { Test-Condition $byKind.ending[$_].condition $case.facts })
                Assert-Sdd ($matches.Count -eq 1) 'ENDING_COVERAGE' $case.id
            }
            $actual = if ($matches.Count -gt 0) { $matches[0] } else { $null }
            Assert-Sdd ($actual -ceq $case.expectedEnding) 'CASE_ENDING' $case.id
        }
    }
}

function Expect-Rejection([string]$Name, [string]$Code, [scriptblock]$Mutation) {
    $mutant = $fixtureRaw | ConvertFrom-Json -AsHashtable
    & $Mutation $mutant
    $caught = $null
    try { Assert-Pack $mutant } catch { $caught = $_.Exception.Message }
    Assert-Sdd ($null -ne $caught -and $caught.StartsWith("$Code :")) 'NEGATIVE_CHECK_FAILED' "$Name expected $Code, got $caught"
    $checkResults.Add(@{ name = $Name; status = 'verified'; expectedCode = $Code })
}

function Visit-SddPackage([string]$Id, $Packages, $Visiting, $Visited) {
    Assert-Sdd ($Packages.Contains($Id)) 'SDD_PACKAGE_REFERENCE' $Id
    Assert-Sdd (-not $Visiting.Contains($Id)) 'SDD_PACKAGE_CYCLE' $Id
    if ($Visited.Contains($Id)) { return }
    [void]$Visiting.Add($Id)
    foreach ($dependency in $Packages[$Id].dependencies) {
        Visit-SddPackage $dependency $Packages $Visiting $Visited
    }
    [void]$Visiting.Remove($Id)
    [void]$Visited.Add($Id)
}

function Get-MarkdownAnchors([string]$Raw) {
    $anchors = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $counts = @{}
    $inFence = $false
    foreach ($line in ($Raw -split '\r?\n')) {
        if ($line -match '^\s*(```|~~~)') { $inFence = -not $inFence; continue }
        if ($inFence -or $line -notmatch '^#{1,6}\s+(.+?)\s*#*\s*$') { continue }
        $heading = $Matches[1].ToLowerInvariant()
        $slug = [regex]::Replace($heading, '[^\p{L}\p{Nd}\p{M}_\- ]', '').Replace(' ', '-')
        $number = if ($counts.ContainsKey($slug)) { $counts[$slug] + 1 } else { 0 }
        $counts[$slug] = $number
        if ($number -gt 0) { $slug += "-$number" }
        [void]$anchors.Add($slug)
    }
    return ,$anchors
}

$fixtureRaw = Get-Content -Raw -LiteralPath $fixturePath
Test-StrictJson $fixtureRaw
$fixture = $fixtureRaw | ConvertFrom-Json -AsHashtable
Assert-Pack $fixture
$checkResults.Add(@{ name = 'schema_semantics_and_12_abstract_cases'; status = 'verified' })

$catalogRaw = Get-Content -Raw -LiteralPath $catalogPath
Test-StrictJson $catalogRaw
$catalog = $catalogRaw | ConvertFrom-Json -AsHashtable
Assert-Sdd ($catalog.catalogFormat -eq 1 -and $catalog.status -ceq 'proposed_design') 'SDD_CATALOG_FORMAT' 'Unsupported package inventory'
$packageById = @{}
$requiredChapters = @('README.md','01_REQUIREMENTS.md','02_ARCHITECTURE.md','03_CONTRACTS_STATE.md','04_AUTHORING.md','05_RUNTIME_RECOVERY.md','06_VERIFICATION_DELIVERY.md')
$prefixes = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($package in $catalog.packages) {
    Assert-Sdd ($package.id -cmatch '^[a-z][a-z0-9-]+$' -and $package.path -ceq $package.id) 'SDD_PACKAGE_PATH' $package.id
    Assert-Sdd (-not $packageById.Contains($package.id)) 'SDD_PACKAGE_DUPLICATE' $package.id
    Assert-Sdd ($prefixes.Add($package.requirementPrefix)) 'SDD_REQUIREMENT_PREFIX' $package.id
    Assert-Sdd ($package.kind -cin @('layer','extension') -and $package.runtimeStatus -ceq 'not_run') 'SDD_PACKAGE_STATUS' $package.id
    $packageById[$package.id] = $package
    $packageRoot = Join-Path $detailsRoot $package.path
    Assert-Sdd (@($package.documents).Count -eq $requiredChapters.Count) 'SDD_PACKAGE_CHAPTERS' $package.id
    foreach ($chapter in $requiredChapters) {
        Assert-Sdd ($chapter -cin $package.documents -and (Test-Path -LiteralPath (Join-Path $packageRoot $chapter))) 'SDD_PACKAGE_CHAPTERS' "$($package.id)/$chapter"
    }
    Assert-Sdd ($package.definitionSchema -ceq 'contracts/definition.schema.json' -and $package.definitionExample -ceq 'examples/definition.json') 'SDD_PACKAGE_ASSETS' $package.id
    $schemaPath = Join-Path $packageRoot $package.definitionSchema
    $examplePath = Join-Path $packageRoot $package.definitionExample
    $schemaRaw = Get-Content -Raw -LiteralPath $schemaPath
    $exampleRaw = Get-Content -Raw -LiteralPath $examplePath
    Test-StrictJson $schemaRaw
    Test-StrictJson $exampleRaw
    $schema = $schemaRaw | ConvertFrom-Json -AsHashtable
    $example = $exampleRaw | ConvertFrom-Json -AsHashtable
    Assert-Sdd ($schema.type -ceq 'object' -and $schema.additionalProperties -eq $false) 'SDD_DEFINITION_SCHEMA' $package.id
    Assert-Schema $example $schemaPath 'SDD_DEFINITION_INVALID'
    $definitionCount++
    $detailSources.Add($schemaPath)
    $detailSources.Add($examplePath)
    $checkResults.Add(@{ name = "definition_$($package.id)"; status = 'verified'; scope = 'Structural schema only; semantic/runtime behavior not_run' })
    foreach ($mutation in @('unknown_field','missing_required','wrong_type')) {
        $bad = $exampleRaw | ConvertFrom-Json -AsHashtable
        if ($mutation -eq 'unknown_field') { $bad['unregisteredSetting'] = $true }
        elseif ($mutation -eq 'missing_required') { $bad.Remove('id') }
        else { $bad['definitionVersion'] = 123 }
        $valid = Test-Json -Json ($bad | ConvertTo-Json -Depth 100 -Compress) -SchemaFile $schemaPath -ErrorAction SilentlyContinue
        Assert-Sdd (-not $valid) 'SDD_NEGATIVE_SCHEMA_FAILED' "$($package.id)/$mutation"
        $definitionNegativeCount++
    }
}
$visitingPackages = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$visitedPackages = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($packageId in $packageById.Keys) { Visit-SddPackage $packageId $packageById $visitingPackages $visitedPackages }
$graphNegativeCases = @(
    @{ name = 'cycle'; code = 'SDD_PACKAGE_CYCLE'; packages = @{ a = @{ dependencies = @('b') }; b = @{ dependencies = @('a') } } },
    @{ name = 'missing_dependency'; code = 'SDD_PACKAGE_REFERENCE'; packages = @{ a = @{ dependencies = @('missing') } } }
)
foreach ($case in $graphNegativeCases) {
    $caught = $null
    try {
        Visit-SddPackage 'a' $case.packages ([System.Collections.Generic.HashSet[string]]::new()) ([System.Collections.Generic.HashSet[string]]::new())
    } catch { $caught = $_.Exception.Message }
    Assert-Sdd ($null -ne $caught -and $caught.StartsWith("$($case.code) :")) 'SDD_GRAPH_NEGATIVE_FAILED' $case.name
    $checkResults.Add(@{ name = "package_graph_rejects_$($case.name)"; status = 'verified'; expectedCode = $case.code })
}
$anchorFixture = Get-MarkdownAnchors "## Same`n~~~text`n## Hidden`n~~~`n## Same"
Assert-Sdd ($anchorFixture.Count -eq 2 -and $anchorFixture.Contains('same') -and $anchorFixture.Contains('same-1') -and -not $anchorFixture.Contains('hidden')) 'SDD_ANCHOR_RULE_FAILED' 'Fenced headings are not anchors; repeated headings get numbered suffixes'
$checkResults.Add(@{ name = 'markdown_anchor_fixture'; status = 'verified'; scope = 'Duplicate headings and fenced-code exclusion' })
$actualPackages = @(Get-ChildItem -LiteralPath $detailsRoot -Directory | Select-Object -ExpandProperty Name)
Assert-Sdd ($actualPackages.Count -eq $packageById.Count -and @($actualPackages | Where-Object { -not $packageById.Contains($_) }).Count -eq 0) 'SDD_UNDECLARED_PACKAGE' 'Package directories must match the inventory'
$detailSources.Add($catalogPath)
$checkResults.Add(@{ name = 'detail_package_inventory_and_dependency_graph'; status = 'verified'; count = $packageById.Count })
$checkResults.Add(@{ name = 'definition_negative_mutations'; status = 'verified'; count = $definitionNegativeCount; scope = 'Unknown-field, missing-ID and invalid-version-type structural checks' })

$markdownFiles = @(Get-ChildItem -LiteralPath $sddRoot -Filter '*.md' -File -Recurse)
$linkCount = 0
$anchorCount = 0
$anchorCache = @{}
foreach ($file in $markdownFiles) {
    $raw = Get-Content -Raw -LiteralPath $file.FullName
    foreach ($match in [regex]::Matches($raw, '\[[^\]]+\]\(([^)]+)\)')) {
        $target = $match.Groups[1].Value
        if ($target -match '^[a-z]+://' -or $target.StartsWith('mailto:')) { continue }
        $parts = $target -split '#', 2
        $fullPath = if ($parts[0]) { [System.IO.Path]::GetFullPath((Join-Path $file.DirectoryName $parts[0])) } else { $file.FullName }
        Assert-Sdd (Test-Path -LiteralPath $fullPath) 'BROKEN_LINK' "$($file.Name): $target"
        if ($parts.Count -eq 2 -and $parts[1] -and [System.IO.Path]::GetExtension($fullPath) -eq '.md') {
            if (-not $anchorCache.ContainsKey($fullPath)) { $anchorCache[$fullPath] = Get-MarkdownAnchors (Get-Content -Raw -LiteralPath $fullPath) }
            Assert-Sdd ($anchorCache[$fullPath].Contains([Uri]::UnescapeDataString($parts[1]))) 'BROKEN_ANCHOR' "$($file.Name): $target"
            $anchorCount++
        }
        $linkCount++
    }
}
$checkResults.Add(@{ name = 'local_links'; status = 'verified'; count = $linkCount; anchors = $anchorCount })

Expect-Rejection 'missing_zone' 'PACK_REFERENCE' { param($p) $p.documents['data/zones/river.json'].exits[0].targetZoneId = 'missing_zone' }
Expect-Rejection 'unknown_fact' 'CONDITION_FACT' { param($p) $p.documents['data/endings/clean_return.json'].condition.all[0].fact = 'lost_soldier.nonexistent' }
Expect-Rejection 'wrong_condition_type' 'CONDITION_TYPE' { param($p) $p.documents['data/endings/clean_return.json'].condition.all[2].value = '30' }
Expect-Rejection 'negative_stock' 'LAYER_SCHEMA' { param($p) $p.documents['scenario.json'].layerData.resources.tools = -1 }
Expect-Rejection 'objective_cycle' 'OBJECTIVE_CYCLE' { param($p) $p.documents['data/objectives/recover_radio.json'].dependencies = @(@{ objectiveId = 'deliver_report'; acceptedStatuses = @('Completed') }) }
Expect-Rejection 'unregistered_effect' 'EFFECT_UNKNOWN' { param($p) $p.documents['data/objectives/deliver_report.json'].successEffects[0].type = 'arbitrary.execute_script' }
Expect-Rejection 'traversal_path' 'PACK_SCHEMA' { param($p) $p.manifest.files[0].path = '../escape.json' }
Expect-Rejection 'ending_priority_collision' 'ENDING_PRIORITY_DUPLICATE' { param($p) $p.documents['data/endings/failure.json'].priority = 400 }
Expect-Rejection 'unknown_scene_role' 'ASSET_ROLE' { param($p) $p.documents['data/zones/river.json'].sceneRole = 'user_script_scene' }

$duplicateCaught = $false
try { Test-StrictJson '{"id":"a","id":"b"}' } catch {
    $duplicateCaught = $_.Exception.Message.StartsWith('JSON_DUPLICATE_PROPERTY :')
}
Assert-Sdd $duplicateCaught 'NEGATIVE_CHECK_FAILED' 'Duplicate JSON keys must be refused'
$checkResults.Add(@{ name = 'duplicate_json_property'; status = 'verified' })

$sourcePaths = @($markdownFiles.FullName) + @($detailSources) + @($contentSchemaPath,$layerSchemaPath,$fixturePath,$PSCommandPath)
$hashes = @($sourcePaths | ForEach-Object {
    @{ path = [System.IO.Path]::GetRelativePath($projectRoot, $_).Replace('\','/'); sha256 = (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant() }
})
$report = @{
    checkedAtUtc = [DateTime]::UtcNow.ToString('o')
    environment = "PowerShell $($PSVersionTable.PSVersion) on $([System.Runtime.InteropServices.RuntimeInformation]::OSDescription)"
    scope = 'Recursive SDD links/anchors, detailed package inventory/dependencies, structural definition schemas, selected fixture references and abstract ending predicates only'
    checks = @($checkResults)
    sourceHashes = $hashes
    runtimeStatus = 'not_run'
    limitations = @('No C# or Godot runtime executed','No physical navigation or ending reachability proved','No asset or commercial readiness claimed','Self-review only')
}
if (-not $NoReport) {
    $reportPath = Join-Path $projectRoot 'plans/SDD_VALIDATION.json'
    $report | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $reportPath -Encoding utf8
}
Write-Output "PASS: $($markdownFiles.Count) SDD documents, $linkCount local links/$anchorCount anchors, $($packageById.Count) detailed packages, $definitionCount definition examples and $definitionNegativeCount definition negative checks."
Write-Output "PASS: existing $($fixture.documents.Count) content documents, $($fixture.abstractCases.Count) abstract ending cases and 10 fixture negative checks."
Write-Output 'This design checker does not execute C# or Godot; standalone engine checks are recorded separately. Native navigation and real Lost Soldier ending routes remain not_run.'
