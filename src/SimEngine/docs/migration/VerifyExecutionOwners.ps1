param([string]$Repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')))
$ErrorActionPreference='Stop'
$files=@(Get-ChildItem -LiteralPath (Join-Path $Repository 'src/Beep.SimEngine') -Recurse -Filter '*.cs' | Where-Object {$_.FullName -notmatch '\\(bin|obj)\\'} | Sort-Object FullName)
$sources=@();$content=@{};$errors=@()
foreach($file in $files){
    $path=[IO.Path]::GetRelativePath($Repository,$file.FullName).Replace('\','/')
    $content[$path]=Get-Content -LiteralPath $file.FullName -Raw
    $sources+=[pscustomobject]@{path=$path;sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
}
$owners=@();$names=@('ResolvedContent','DependencyGraph','ActivityGraph','ResourceInventoryState','ResourceReservationBook','Order','ActivityNodeExecution','Job','WorkBoard')
foreach($name in $names){
    $declarations=@()
    foreach($path in @($content.Keys|Sort-Object)){
        foreach($match in [regex]::Matches($content[$path],'\bclass\s+'+[regex]::Escape($name)+'\b')){
            $declarations+=[pscustomobject]@{path=$path;line=1+[regex]::Matches($content[$path].Substring(0,$match.Index),"`n").Count}
        }
    }
    if($declarations.Count -eq 0 -or $name -ne 'WorkBoard' -and $declarations.Count -ne 1){$errors+='owner declaration count '+$name}
    if($name -eq 'WorkBoard'){
        foreach($entry in $declarations){if($content[$entry.path] -notmatch '\bpartial\s+class\s+WorkBoard\b' -or $content[$entry.path] -notmatch 'namespace Beep\.SimEngine\.Standard\b'){$errors+='noncanonical WorkBoard '+$entry.path}}
    }
    $owners+=[pscustomobject]@{type=$name;declarations=$declarations}
}
$witnesses=@(
    @{path='src/Beep.SimEngine/Standard/ActivityGraph.cs';pattern='Nodes => _topology.Nodes';meaning='ActivityGraph exposes sole immutable DependencyGraph node list'},
    @{path='src/Beep.SimEngine/Standard/ActivityGraph.cs';pattern='_topology = new DependencyGraph<IActivityNodeDefinition>';meaning='Activity topology uses original generic compiler, not another compiler'},
    @{path='src/Beep.SimEngine/Standard/Work.cs';pattern='nodes.Add(node); _byNode.Add(node.Id, node)';meaning='Order list and named index alias the same execution record'},
    @{path='src/Beep.SimEngine/Standard/Work.cs';pattern='private readonly Dictionary<int, Order> _orderById';meaning='WorkBoard indexes its actual orders'},
    @{path='src/Beep.SimEngine/Content/ResolvedContent.cs';pattern='IReadOnlyList<IResourceDefinition> IResourceCatalog.Definitions => Resources';meaning='Resource catalog contract aliases original resolved resource definitions'},
    @{path='src/Beep.SimEngine/Standard/ResourceInventoryState.cs';pattern='new ResourceReservationBook()';meaning='Inventory embeds original endpoint reservation primitive'},
    @{path='src/Beep.SimEngine/Standard/SpatialResources.cs';pattern='new ResourceReservationBook()';meaning='Spatial pools use same endpoint reservation primitive'},
    @{path='src/Beep.SimEngine/Standard/Tycoon/Suppliers.cs';pattern='new ResourceReservationBook()';meaning='Optional supplier promises use same primitive; protected file read only'},
    @{path='src/Beep.SimEngine/Content/ResolvedContent.cs';pattern='if (!types.IsSection(section)) throw new ContentException';meaning='Raw unknown sections fail sole reader; no patches section or loader'},
    @{path='src/Beep.SimEngine/Standard/ResourceInventoryState.cs';pattern='InboundPromises => _reservations.Promises';meaning='Inbound quantities derive from same book, not a second balance'},
    @{path='src/Beep.SimEngine/Standard/ResourceInventoryState.cs';pattern='OutboundPromises => _reservations.Promises';meaning='Outbound quantities derive from same book, not a second balance'}
)
$captured=@()
foreach($witness in $witnesses){
    if(!$content.ContainsKey($witness.path)){$errors+='missing witness source '+$witness.path;continue}
    $index=$content[$witness.path].IndexOf($witness.pattern,[StringComparison]::Ordinal)
    if($index -lt 0){$errors+='missing ownership witness '+$witness.meaning;continue}
    $captured+=[pscustomobject]@{path=$witness.path;line=1+[regex]::Matches($content[$witness.path].Substring(0,$index),"`n").Count;literal=$witness.pattern;meaning=$witness.meaning}
}
$forbidden=@()
foreach($path in @($content.Keys|Sort-Object)){
    foreach($match in [regex]::Matches($content[$path],'\b(?:class|interface|struct)\s+(?:PatchLoader|ContentPatch|StepExecutor|WorkStepExecutor|WorkStepDef|ReservationLedger|ResourceCatalog|GeneratedEntityDef)\b|\b(?:Generators|GenerateDefinitions|AddGenerated)\s*\(')){
        $forbidden+=[pscustomobject]@{path=$path;line=1+[regex]::Matches($content[$path].Substring(0,$match.Index),"`n").Count;literal=$match.Value}
    }
}
if($forbidden.Count -ne 0){$errors+='obsolete owner/loader/executor declaration found'}
[pscustomobject]@{schema=1;scope='Structural current production ownership audit. Unique declarations and reviewed aliases are not arbitrary provider/runtime rollback guarantees.';source_count=$files.Count;owners=$owners;witnesses=$captured;forbidden_declarations=$forbidden;errors=$errors;source_anchors=$sources}|ConvertTo-Json -Depth 8
if($errors.Count -ne 0){exit 1}
