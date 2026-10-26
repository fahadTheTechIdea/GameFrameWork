# Read-only original assertion/suite ownership gate. Does not run gameplay or change either repository.
[CmdletBinding()]
param([string]$ManifestPath = (Join-Path $PSScriptRoot 'assertion-ownership.json'), [switch]$SelfTest)
$ErrorActionPreference = 'Stop'
if ($null -eq ('Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree' -as [type])) {
    Add-Type -AssemblyName Microsoft.CodeAnalysis.CSharp
}
function Read-Assertions([string]$Text,[string]$Path) {
    $tree=[Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($Text)
    $errors=@($tree.GetDiagnostics()|Where-Object {$_.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Error})
    if($errors.Count){throw "Invalid C# source: $Path"}
    $ordinal=0
    foreach($node in $tree.GetRoot().DescendantNodes()) {
        if($node -isnot [Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax] -or
           $node.Expression -isnot [Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax] -or
           $node.Expression.Expression.ToString() -ne 'c' -or
           $node.Expression.Name.Identifier.ValueText -notin @('That','Equal','Throws')){continue}
        $ordinal++
        $method=@($node.Ancestors()|Where-Object {$_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax]})[0]
        [pscustomobject]@{path=$Path;ordinal=$ordinal;line=$tree.GetLineSpan($node.Span).StartLinePosition.Line+1;
            method=$method.Identifier.ValueText;kind=$node.Expression.Name.Identifier.ValueText;
            label=$node.ArgumentList.Arguments[-1].Expression.ToString();call=$node.ToString()}
    }
}

function Read-Suites([string]$Text,[string]$Path) {
    $tree=[Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($Text)
    foreach($node in $tree.GetRoot().DescendantNodes()) {
        if($node -isnot [Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax] -or
           $node.Expression -isnot [Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax] -or
           $node.Expression.Expression.ToString() -ne 'TestRunner' -or $node.Expression.Name.Identifier.ValueText -ne 'Add'){continue}
        $method=@($node.Ancestors()|Where-Object {$_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax]})[0]
        if($null -eq $method -or $method.Identifier.ValueText -cne 'Register'){throw "Suite is not added from its actual Register method: $Path"}
        $class=@($node.Ancestors()|Where-Object {$_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax]})[0].Identifier.ValueText
        $handler=$node.ArgumentList.Arguments[1].Expression.ToString()
        $declared=@($tree.GetRoot().DescendantNodes()|Where-Object {$_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax] -and $_.Identifier.ValueText -ceq $handler})
        if($declared.Count -ne 1){throw "Suite lacks one declared current handler: $Path/$handler"}
        [pscustomobject]@{path=$Path;class=$class;label=$node.ArgumentList.Arguments[0].Expression.ToString();handler=$handler}
    }
}
function Resolve-Owner($Owner) {
    $root = switch -CaseSensitive ($Owner.root) { 'framework' {$frameworkRoot} 'oil' {$inventory.source} default {throw 'Unknown assertion owner root'} }
    $path=[IO.Path]::GetFullPath((Join-Path $root $Owner.path))
    if(-not $path.StartsWith([IO.Path]::GetFullPath($root)+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Owner path escapes its declared repository'}
    if(-not (Test-Path -LiteralPath $path -PathType Leaf)){throw "Missing assigned source: $path"}
    return $path
}
$frameworkRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$inventory=Get-Content (Join-Path $PSScriptRoot 'source-inventory.json') -Raw|ConvertFrom-Json
$dispositions=Get-Content (Join-Path $PSScriptRoot 'source-ownership.json') -Raw|ConvertFrom-Json
$manifest=Get-Content -LiteralPath $ManifestPath -Raw|ConvertFrom-Json
$archiveHash=(Get-FileHash -LiteralPath $inventory.recoveryArchive -Algorithm SHA256).Hash.ToLowerInvariant()
if($archiveHash -cne $inventory.recoverySha256.ToLowerInvariant()){throw 'Original archive differs from captured source inventory'}
$originalAssertions=@{};$originalSuites=@{}
$zip=[IO.Compression.ZipFile]::OpenRead($inventory.recoveryArchive)
try {
 foreach($entry in $zip.Entries) {
    $captured=@($inventory.files|Where-Object {$_.path -ceq $entry.FullName})
    if($captured.Count -ne 1){throw "Uncaptured/duplicate archive entry: $($entry.FullName)"}
    $hash=[Security.Cryptography.SHA256]::Create();$stream=$entry.Open()
    try{$actual=([BitConverter]::ToString($hash.ComputeHash($stream))).Replace('-','').ToLowerInvariant()}finally{$stream.Dispose();$hash.Dispose()}
    if($actual -cne $captured[0].sha256.ToLowerInvariant()){throw "Original entry identity differs: $($entry.FullName)"}
    if($entry.FullName -notlike 'OilCo.Tests/*.cs' -or $entry.FullName -like '*TestRunner*' -or $entry.FullName -like '*TestKit*'){continue}
    $reader=[IO.StreamReader]::new($entry.Open());try{$text=$reader.ReadToEnd()}finally{$reader.Dispose()}
    foreach($assertion in Read-Assertions $text $entry.FullName){$id=$entry.FullName+'#'+$assertion.ordinal.ToString('000');$originalAssertions.Add($id,$assertion)}
    foreach($suite in Read-Suites $text $entry.FullName){$id=$entry.FullName+'#'+$suite.label;$originalSuites.Add($id,$suite)}
 }
 if($zip.Entries.Count -ne $inventory.files.Count){throw 'Original archive entry count differs'}
}finally{$zip.Dispose()}
$currentAssertions=@{};$currentSuites=@{};$programs=@{}
function Verify-Owners($Data) {
 if($Data.schema -ne 1 -or $Data.original_archive_sha256 -cne $archiveHash -or $Data.source_head -cne $inventory.sourceHead){throw 'Ownership manifest source identity differs'}
 if(@($Data.assertions).Count -ne $originalAssertions.Count -or @($Data.suites).Count -ne $originalSuites.Count){throw 'Original assertion/suite ownership is incomplete'}
 if($Data.counts.source_assertions -ne $originalAssertions.Count -or $Data.counts.original_suites -ne $originalSuites.Count){throw 'Ownership count claims differ'}
 $reassigned=@($Data.assertions|Where-Object {$_.assignment -cne 'same assertion kind, method and label'}).Count
 if($Data.counts.explicit_reassignments -ne $reassigned){throw 'Explicit reassignment count differs'}
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($row in $Data.assertions) {
    if(-not $seen.Add($row.id) -or -not $originalAssertions.ContainsKey($row.id)){throw "Duplicate/unknown original assertion: $($row.id)"}
    $source=$originalAssertions[$row.id]
    foreach($field in @('path','ordinal','line','method','kind','label','call')){if([string]$row.source.$field -cne [string]$source.$field){throw "Changed original assertion identity: $($row.id)/$field"}}
    if([string]::IsNullOrWhiteSpace($row.assignment)){throw "Unreasoned assignment: $($row.id)"}
    $path=Resolve-Owner $row.owner
    $allowed=@($dispositions.files|Where-Object {$_.sourcePath -ceq $source.path})
    if($allowed.Count -ne 1 -or $path -notin $allowed[0].destinations){throw "Assertion owner differs from explicit source disposition: $($row.id)"}
    if(-not $currentAssertions.ContainsKey($path)){$currentAssertions[$path]=@(Read-Assertions ([IO.File]::ReadAllText($path)) $path)}
    $matches=@($currentAssertions[$path]|Where-Object {$_.ordinal -eq $row.owner.ordinal -and $_.method -ceq $row.owner.method -and $_.kind -ceq $row.owner.kind -and $_.label -ceq $row.owner.label -and $_.call -ceq $row.owner.call})
    if($matches.Count -ne 1){throw "Assigned assertion missing or changed: $($row.id)"}
 }
 $seen.Clear()
 foreach($row in $Data.suites) {
    $id=$row.source.path+'#'+$row.source.label
    if(-not $seen.Add($id) -or -not $originalSuites.ContainsKey($id)){throw 'Duplicate/unknown original suite'}
    foreach($field in @('path','label','handler')){if([string]$row.source.$field -cne [string]$originalSuites[$id].$field){throw "Changed original suite identity: $id/$field"}}
    if([string]::IsNullOrWhiteSpace($row.assignment)){throw 'Original suite assignment is missing'}
    $path=Resolve-Owner $row.owner
    $allowed=@($dispositions.files|Where-Object {$_.sourcePath -ceq $row.source.path})
    if($allowed.Count -ne 1 -or $path -notin $allowed[0].destinations){throw 'Suite owner differs from explicit source disposition'}
    if(-not $currentSuites.ContainsKey($path)){$currentSuites[$path]=@(Read-Suites ([IO.File]::ReadAllText($path)) $path)}
    $match=@($currentSuites[$path]|Where-Object {$_.class -ceq $row.owner.class -and $_.label -ceq $row.owner.label -and $_.handler -ceq $row.owner.handler})
    if($match.Count -ne 1){throw 'Assigned current suite missing or changed'}
    $programRef=[pscustomobject]@{root=$row.owner.root;path=$row.owner.program}
    $program=Resolve-Owner $programRef
    if(-not $programs.ContainsKey($program)){
        $tree=[Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($program))
        $programs[$program]=@($tree.GetRoot().DescendantNodes()|Where-Object {$_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax] -and $_.Expression -is [Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax] -and $_.Expression.Name.Identifier.ValueText -eq 'Register'}|ForEach-Object {$_.Expression.Expression.ToString()})
    }
    if($row.owner.class -cnotin $programs[$program]){throw "Assigned suite class is not registered in its actual runner: $id -> $program/$($row.owner.class)"}
 }
}
Verify-Owners $manifest
$negativeCount=0
if($SelfTest) {
 $expectedErrors=@{
    'remove-assertion'='Original assertion/suite ownership is incomplete'
    'duplicate-assertion'='Duplicate/unknown original assertion:*'
    'missing-owner'='Missing assigned source:*'
    'changed-owner'='Assigned assertion missing or changed:*'
    'unregistered-suite'='Assigned suite class is not registered in its actual runner:*'
    'changed-source'='Changed original assertion identity:*'
    'changed-source-suite'='Changed original suite identity:*'
    'wrong-reassignment-count'='Explicit reassignment count differs'
    'blank-assignment'='Unreasoned assignment:*'
    'outside-disposition'='Assertion owner differs from explicit source disposition:*'
    'remove-suite'='Original assertion/suite ownership is incomplete'
    'duplicate-suite'='Duplicate/unknown original suite'
 }
 foreach($mutation in @('remove-assertion','duplicate-assertion','missing-owner','changed-owner','unregistered-suite',
     'changed-source','changed-source-suite','wrong-reassignment-count','blank-assignment','outside-disposition','remove-suite','duplicate-suite')){
    $copy=$manifest|ConvertTo-Json -Depth 12|ConvertFrom-Json
    switch($mutation){
      'remove-assertion' {$copy.assertions=@($copy.assertions|Select-Object -Skip 1)}
      'duplicate-assertion' {$copy.assertions[1].id=$copy.assertions[0].id}
      'missing-owner' {$copy.assertions[0].owner.path='missing-original-assertion.cs'}
      'changed-owner' {$copy.assertions[0].owner.call='c.That(false, "invented")'}
      'unregistered-suite' {$copy.suites[0].owner.program=$copy.suites[0].owner.path}
      'changed-source' {$copy.assertions[0].source.call='c.That(false, "changed original")'}
      'changed-source-suite' {$copy.suites[0].source.handler='InventedOriginalHandler'}
      'wrong-reassignment-count' {$copy.counts.explicit_reassignments++}
      'blank-assignment' {$copy.assertions[0].assignment=' ';$copy.counts.explicit_reassignments=@($copy.assertions|Where-Object {$_.assignment -cne 'same assertion kind, method and label'}).Count}
      'outside-disposition' {$copy.assertions[0].owner.root='framework';$copy.assertions[0].owner.path='src/Beep.SimEngine/Kernel/SaveGame.cs'}
      'remove-suite' {$copy.suites=@($copy.suites|Select-Object -Skip 1)}
      'duplicate-suite' {$copy.suites[1].source=$copy.suites[0].source}
    }
    $refused=$false;try{Verify-Owners $copy}catch{
        if($_.Exception.Message -notlike $expectedErrors[$mutation]){throw "Wrong refusal for ${mutation}: $($_.Exception.Message)"}
        $refused=$true
    }
    if(-not $refused){throw "Ownership verifier accepted deliberate defect: $mutation"}
    $negativeCount++
 }
}
[pscustomobject]@{status='PASS';sourceAssertions=$originalAssertions.Count;originalSuites=$originalSuites.Count;archiveEntries=$inventory.files.Count;negativeMutations=$negativeCount;explicitReassignments=$manifest.counts.explicit_reassignments;frameworkOwners=@($manifest.assertions|Where-Object {$_.owner.root -ceq 'framework'}).Count;oilOwners=@($manifest.assertions|Where-Object {$_.owner.root -ceq 'oil'}).Count;runtimeSuitesExecuted=0}|ConvertTo-Json -Compress
