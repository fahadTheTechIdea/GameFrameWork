# Read-only compiled call-site census. Does not compose a game, run tests or implement gameplay.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BuildInputs,
    [string]$Repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')),
    [string]$AssemblyPath = 'tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.dll'
)
$ErrorActionPreference = 'Stop'
$Repository = [IO.Path]::GetFullPath($Repository)
if (![IO.Path]::IsPathRooted($BuildInputs)) { $BuildInputs = Join-Path $Repository $BuildInputs }
if (![IO.Path]::IsPathRooted($AssemblyPath)) { $AssemblyPath = Join-Path $Repository $AssemblyPath }
$build = Get-Content -LiteralPath $BuildInputs -Raw | ConvertFrom-Json
$assemblyRelative = [IO.Path]::GetRelativePath($Repository, $AssemblyPath).Replace('\', '/')
$expectedAssembly = @($build.inputs | Where-Object path -eq $assemblyRelative)
if ($expectedAssembly.Count -ne 1 -or (Get-FileHash -LiteralPath $AssemblyPath).Hash -ne $expectedAssembly[0].sha256) {
    throw 'Assembly does not match the explicit build-input manifest'
}
$sourceRows = @($build.inputs | Where-Object { $_.path -like 'src/Beep.SimEngine/*.cs' })
$actualSources = @(Get-ChildItem -LiteralPath (Join-Path $Repository 'src/Beep.SimEngine') -Recurse -Filter '*.cs' |
    Where-Object { $_.FullName -notmatch '[/\\](bin|obj)[/\\]' } |
    ForEach-Object { [IO.Path]::GetRelativePath($Repository, $_.FullName).Replace('\', '/') })
if (@(Compare-Object ($sourceRows.path | Sort-Object) ($actualSources | Sort-Object)).Count) { throw 'Native source set changed since the explicit build' }
foreach ($row in $sourceRows) {
    if ((Get-FileHash -LiteralPath (Join-Path $Repository $row.path)).Hash -ne $row.sha256) { throw "Native source changed since the explicit build: $($row.path)" }
}
$assembly = [Reflection.Assembly]::LoadFrom($AssemblyPath)
$endpointRoot = $assembly.GetType('Beep.SimEngine.IResourceInlet', $true)
$resourceStateRoot = $assembly.GetType('Beep.SimEngine.IResourceEndpoint', $true)
$transactionRoot = $assembly.GetType('Beep.SimEngine.IResourceTransaction', $true)
$supplierRoot = $assembly.GetType('Beep.SimEngine.Standard.ISupplier', $true)
$stateType = $assembly.GetType('Beep.SimEngine.Standard.ResourceInventoryState', $true)
$bookType = $assembly.GetType('Beep.SimEngine.ResourceReservationBook', $true)
$factType = $assembly.GetType('Beep.SimEngine.ResourceFlowFact', $true)
$quantityRoot = $assembly.GetType('Beep.SimEngine.Kernel.ISpatialResourceQuantity`1', $true)
$mapLayerRoot = $assembly.GetType('Beep.SimEngine.Maps.MapLayer`1', $true)
$spatialEndpointRoot = $assembly.GetType('Beep.SimEngine.Standard.SpatialResourceEndpoint`1', $true)
$sampleLayerRoot = $assembly.GetType('Beep.SimEngine.Maps.ITileWritableSampleLayer', $true)
$sampleSpaceRoot = $assembly.GetType('Beep.SimEngine.Maps.ITileSampleMutationSpace', $true)
$layerProviderRoot = $assembly.GetType('Beep.SimEngine.Kernel.ISpatialLayerProvider', $true)
$spatialCommitRoot = $assembly.GetType('Beep.SimEngine.Kernel.ISpatialMutation', $true)
$quantityCommitType = $assembly.GetType('Beep.SimEngine.Kernel.SpatialResourceChangeCommit', $true)
$mutations = @('Receive','ReceiveReserved','Take','TakeReserved','Add','Remove','ReserveIn','ReserveOut','IncreaseReservedOut','ReleaseIn','ReleaseOut','ConvertReserved')
$bookMutations = @('Add','Release','Consume','Increase')
$supplierMutations = @('ReserveSupply','ReleaseSupply','SupplyReserved')
$transactionMutations = @('Commit','CommitTransfers','CommitMovement','CommitMovements','CommitInventory','CommitConversion','AcquireClaims','ExtendClaim','ReleaseClaims','ConsumeClaims')
$pure = @('Accepts','CheckIncoming','Offer','CheckConversion','Amount','ReservedIn','ReservedOut','Available','Holds','HoldsUnpromised','CanReserveIn','CanReserveOut','CheckReservedIn','CheckReservedOut',
    'Check','CanReserve','Get','Promises','Inspect','ContainsToken','Matches','SupplyQuantum','CanReserveSupply','CheckSupply','ReadClaims','CheckInventoryRemoval','CheckResultDefinition','AllowsQuantity')
$unknown = [Collections.Generic.List[object]]::new()
$calls = [Collections.Generic.List[object]]::new()
$fields = [Collections.Generic.List[object]]::new()
$spatialFields = [Collections.Generic.List[object]]::new()
$indirect = [Collections.Generic.List[object]]::new()
$opcodes = @{}
foreach ($field in [Reflection.Emit.OpCodes].GetFields([Reflection.BindingFlags]'Public,Static')) {
    $code = $field.GetValue($null); $opcodes[([int]$code.Value -band 0xffff)] = $code
}
$pdbPath = [IO.Path]::ChangeExtension($AssemblyPath, '.pdb')
$pdbStream = [IO.File]::OpenRead($pdbPath)
$peStream = [IO.File]::OpenRead($AssemblyPath)
$pdbProvider = $null; $peReader = $null
try {
    $pdbProvider = [Reflection.Metadata.MetadataReaderProvider]::FromPortablePdbStream($pdbStream, [Reflection.Metadata.MetadataStreamOptions]::LeaveOpen, 0)
    $reader = $pdbProvider.GetMetadataReader([Reflection.Metadata.MetadataReaderOptions]::Default, $null)
    $peReader = [Reflection.PortableExecutable.PEReader]::new($peStream, [Reflection.PortableExecutable.PEStreamOptions]::LeaveOpen)
    $pdbId = [byte[]]$reader.DebugMetadataHeader.Id
    $pdbGuid = [guid]::new([byte[]]$pdbId[0..15])
    $codeViews = @($peReader.ReadDebugDirectory() | Where-Object { $_.Type.ToString() -eq 'CodeView' })
    if ($codeViews.Count -ne 1 -or $peReader.ReadCodeViewDebugDirectoryData($codeViews[0]).Guid -ne $pdbGuid) { throw 'Portable PDB does not match the compiled assembly' }
    $documents = @{}; $checkedDocuments = 0
    foreach ($handle in $reader.Documents) {
        $document = $reader.GetDocument($handle); $path = $reader.GetString($document.Name)
        $relative = [IO.Path]::GetRelativePath($Repository, $path).Replace('\', '/')
        $documents[$handle] = $relative
        if ($relative -like 'src/Beep.SimEngine/*.cs' -and $relative -notmatch '/(bin|obj)/') {
            if ($reader.GetGuid($document.HashAlgorithm).ToString() -ne '8829d00f-11b8-4213-878b-770e8597ac16') { throw "Unsupported source checksum: $relative" }
            $checksum = [Convert]::ToHexString($reader.GetBlobBytes($document.Hash))
            if ((Get-FileHash -LiteralPath $path).Hash -ne $checksum) { throw "PDB source checksum changed: $relative" }
            $checkedDocuments++
        }
    }
    function Get-Location($Method, [int]$Offset) {
        $row = $Method.MetadataToken -band 0x00ffffff
        $debug = $reader.GetMethodDebugInformation([Reflection.Metadata.Ecma335.MetadataTokens]::MethodDebugInformationHandle($row))
        $chosen = $null
        foreach ($point in $debug.GetSequencePoints()) {
            if (!$point.IsHidden -and $point.Offset -le $Offset) { $chosen = $point }
        }
        if ($null -eq $chosen) { return @{ path = $null; line = $null } }
        $doc = if ($chosen.Document.IsNil) { $debug.Document } else { $chosen.Document }
        return @{ path = $documents[$doc]; line = $chosen.StartLine }
    }
    function Implements-Generic($Type, $Root) {
        if ($Type.IsGenericType -and $Type.GetGenericTypeDefinition() -eq $Root) { return $true }
        foreach ($contract in $Type.GetInterfaces()) {
            if ($contract.IsGenericType -and $contract.GetGenericTypeDefinition() -eq $Root) { return $true }
        }
        return $false
    }
    function Get-Family($Target) {
        $type = $Target.DeclaringType; $name = $Target.Name
        if ($null -eq $type) { return $null }
        if ($resourceStateRoot.IsAssignableFrom($type) -and $name -eq 'WriteResourceState') { return 'resource-state-encoding' }
        if ($spatialCommitRoot.IsAssignableFrom($type)) {
            if ($Target.IsConstructor) { return 'spatial-publication-construction' }
            if ($name -eq 'Commit') { return 'spatial-publication-entry' }
        }
        if ($type -eq $quantityCommitType -and $name -eq 'Commit') { return 'spatial-prepared-commit' }
        if (Implements-Generic $type $quantityRoot) {
            if ($name -in @('Withdraw','Deposit','PrepareWithdrawal','PrepareDeposit')) { return 'spatial-quantity-entry' }
            if ($name -eq 'BindResourceCell') { return 'spatial-cell-binding' }
            if ($type.IsInterface -and !$Target.IsSpecialName -and $name -notin @('ReadQuantity','CheckWithdrawal','CheckDeposit')) {
                return 'UNKNOWN-spatial-quantity-method'
            }
        }
        if ($sampleSpaceRoot.IsAssignableFrom($type) -and $name -eq 'ApplySamples') { return 'spatial-sample-batch' }
        if ($sampleLayerRoot.IsAssignableFrom($type) -and $name -eq 'SetSample') { return 'spatial-sample-write' }
        if ((Implements-Generic $type $mapLayerRoot) -and $name -in @('Set','Fill')) { return 'spatial-native-value-write' }
        if ($layerProviderRoot.IsAssignableFrom($type) -and $name -eq 'AssignOwner') { return 'spatial-owner-write' }
        if ($Target.IsConstructor -and ($type -eq $stateType -or $type -eq $bookType)) { return 'primitive-construction' }
        if ($type -eq $bookType) {
            if ($name -in $bookMutations) { return 'reservation-book-mutation' }
            if ($name -eq 'Sync') { return 'book-codec' }
            if (!$Target.IsPublic -or $Target.IsSpecialName -or $name -in $pure) { return $null }
            return 'UNKNOWN-book-method'
        }
        if ($transactionRoot.IsAssignableFrom($type)) {
            if ($name -eq 'InitializeStock') { return 'initial-stock-initialization' }
            if ($name -in $transactionMutations) { return 'transaction-entry' }
            if (!$Target.IsPublic -or $Target.IsSpecialName -or $name -like 'Evaluate*' -or $name -in $pure) { return $null }
            return 'UNKNOWN-transaction-method'
        }
        if ($supplierRoot.IsAssignableFrom($type)) {
            if ($name -in $supplierMutations) { return 'supplier-mutation' }
            if (!$Target.IsPublic -or $Target.IsSpecialName -or $name -in $pure -or $name -in @('Initialize','CheckRemoval','OnCreate','Describe','Sync','CheckReplacement','RestoreReplacementState')) { return $null }
            return 'UNKNOWN-supplier-method'
        }
        if ($endpointRoot.IsAssignableFrom($type)) {
            if ($name -in $mutations) { return 'endpoint-mutation' }
            if ($name -eq 'Sync') { return 'endpoint-codec' }
            # The existing staged codec returns the restore action only when reading.
            # Keep this exact call visible; do not whitelist arbitrary endpoint methods.
            if ($type.IsGenericType -and $type.GetGenericTypeDefinition() -eq $spatialEndpointRoot -and $name -eq 'StageSync') { return 'endpoint-staged-codec' }
            if (!$Target.IsPublic -or $Target.IsSpecialName -or $name -in $pure -or $name -in @('Initialize','CheckRemoval','OnCreate','Describe','CheckReplacement','RestoreReplacementState','Bind')) { return $null }
            return 'UNKNOWN-endpoint-method'
        }
        if ($name -eq 'Publish' -and $Target -is [Reflection.MethodInfo] -and $Target.IsGenericMethod -and $Target.GetGenericArguments() -contains $factType) { return 'fact-publication' }
        return $null
    }
    $types = @($assembly.GetTypes() | Sort-Object FullName); $methodCount = 0; $instructions = 0
    foreach ($type in $types) {
        $methods = @($type.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly')) + @($type.GetConstructors([Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly'))
        foreach ($method in ($methods | Sort-Object MetadataToken -Unique)) {
            $body = $method.GetMethodBody(); if ($null -eq $body) { continue }; $methodCount++
            $bytes = $body.GetILAsByteArray(); $offset = 0
            $typeArguments = $type.GetGenericArguments()
            $methodArguments = if ($method -is [Reflection.MethodInfo]) { $method.GetGenericArguments() } else { [type[]]@() }
            while ($offset -lt $bytes.Length) {
                $at = $offset; $value = [int]$bytes[$offset++]; if ($value -eq 0xfe) { $value = 0xfe00 -bor $bytes[$offset++] }
                if (!$opcodes.ContainsKey($value)) { throw "Unknown IL opcode $value in $method" }
                $op = $opcodes[$value]; $instructions++; $operandAt = $offset
                $size = switch ($op.OperandType.ToString()) {
                    'InlineNone' { 0 }
                    { $_ -in @('ShortInlineBrTarget','ShortInlineI','ShortInlineVar') } { 1 }
                    'InlineVar' { 2 }
                    { $_ -in @('InlineI8','InlineR') } { 8 }
                    'InlineSwitch' { 4 + 4 * [BitConverter]::ToInt32($bytes, $offset) }
                    { $_ -in @('InlineBrTarget','InlineField','InlineI','InlineMethod','InlineSig','InlineString','InlineTok','InlineType','ShortInlineR') } { 4 }
                    default { throw "Unsupported IL operand $($op.OperandType)" }
                }
                if ($size -lt 0 -or $offset + $size -gt $bytes.Length) { throw "Invalid IL bounds in $method" }
                $offset += $size
                if ($op.Name -eq 'calli') { $indirect.Add(@{ caller = "$($type.FullName).$method"; il_offset = $at; kind = 'indirect-function-pointer' }) }
                if ($op.OperandType.ToString() -notin @('InlineMethod','InlineField','InlineTok')) { continue }
                $token = [BitConverter]::ToInt32($bytes, $operandAt)
                try { $target = $method.Module.ResolveMember($token, [type[]]$typeArguments, [type[]]$methodArguments) }
                catch { throw "Unresolved compiled member $token in $($type.FullName).$method : $_" }
                if ($target -is [Reflection.FieldInfo]) {
                    if ($op.Name -in @('stfld','stsfld') -and $target.DeclaringType -in @($stateType,$bookType)) {
                        $where = Get-Location $method $at
                        $fields.Add(@{ caller_type = $type.FullName; caller = $method.ToString(); field_owner = $target.DeclaringType.FullName; field = $target.Name; path = $where.path; line = $where.line; il_offset = $at })
                    }
                    if ($op.Name -in @('stfld','stsfld') -and (Implements-Generic $target.DeclaringType $mapLayerRoot)) {
                        $where = Get-Location $method $at
                        $spatialFields.Add(@{ caller_type = $type.FullName; caller = $method.ToString(); field_owner = $target.DeclaringType.FullName; field = $target.Name; path = $where.path; line = $where.line; il_offset = $at })
                    }
                    continue
                }
                if ($target -isnot [Reflection.MethodBase]) { continue }
                if ($target.Name -eq 'Invoke' -and $target.DeclaringType.Namespace -eq 'System.Reflection') {
                    $where = Get-Location $method $at
                    $indirect.Add(@{ caller = "$($type.FullName).$method"; il_offset = $at; kind = 'reflection-invocation'; path = $where.path; line = $where.line })
                }
                $family = Get-Family $target; if ($null -eq $family) { continue }
                $where = Get-Location $method $at
                $entry = @{ caller_type = $type.FullName; caller = $method.ToString(); target_type = $target.DeclaringType.FullName; target = $target.ToString(); family = $family;
                    path = $where.path; line = $where.line; il_offset = $at; opcode = $op.Name;
                    kind = $(if ($op.Name -in @('ldftn','ldvirtftn','ldtoken')) { 'method-reference' } else { 'invocation' }) }
                $calls.Add($entry); if ($family.StartsWith('UNKNOWN', [StringComparison]::Ordinal)) { $unknown.Add($entry) }
            }
        }
    }
    $unmapped = @($calls | Where-Object { !$_.path -or !$_.line })
    [pscustomobject]@{
        schema = 1; engine = $assembly.GetType('Beep.SimEngine.Kernel.EngineInfo', $true).GetField('Build').GetRawConstantValue()
        scope = 'Read-only complete native managed-IL traversal; exact typed resource/coordinator/book and spatial quantity/sample/publication/owner calls, references and selected primitive field writes; matched PDB/source/build inputs. Not gameplay execution, every semantic mutation, a whole-resource accounting certificate, or a sandbox against arbitrary CLR/reflection behavior.'
        build_inputs = [IO.Path]::GetRelativePath($Repository, $BuildInputs).Replace('\','/'); assembly = $assemblyRelative
        assembly_sha256 = (Get-FileHash -LiteralPath $AssemblyPath).Hash; pdb_sha256 = (Get-FileHash -LiteralPath $pdbPath).Hash
        native_sources = $sourceRows.Count; pdb_source_checksums = $checkedDocuments; types = $types.Count; methods_with_il = $methodCount; instructions = $instructions
        calls = $calls; state_field_writes = $fields; spatial_layer_field_writes = $spatialFields; unknown_contract_methods = $unknown; unmapped_calls = $unmapped; indirect_sites = $indirect
    } | ConvertTo-Json -Depth 10
    if ($unknown.Count -ne 0 -or $unmapped.Count -ne 0) { exit 1 }
}
finally {
    if ($null -ne $peReader) { $peReader.Dispose() }
    if ($null -ne $pdbProvider) { $pdbProvider.Dispose() }
    $peStream.Dispose(); $pdbStream.Dispose()
}
