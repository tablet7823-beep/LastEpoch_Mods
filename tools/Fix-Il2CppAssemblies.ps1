<#
.SYNOPSIS
    Repairs the Il2Cpp assemblies MelonLoader generates for Last Epoch.

.DESCRIPTION
    Cpp2IL 2022.1.0-pre-release.21 dumps Unity 6000.4.8 (IL2CPP metadata v39)
    with a NestedClass table the CLR cannot follow. Some compiler-generated
    types lose their declaring type and surface as duplicate top-level types,
    so loading the assembly throws

        BadImageFormatException: Duplicate type with name '<>O'
          in assembly 'UnityEngine.CoreModule'

    MelonLoader's Il2Cpp support module loads every file in Il2CppAssemblies,
    so that one bad assembly takes the whole support module down and the log
    only says "No Support Module Loaded!" (the real exception is swallowed
    unless debug_mode is on).

    Mono.Cecil reads the nesting correctly and rebuilds the metadata tables on
    write, so a plain read/write round trip is enough to fix it. The orphaned
    types - the ones with no reachable declaring type - are dropped, which is
    no loss: nothing could reach them before either.

    Run this after every game patch. MelonLoader regenerates the assemblies
    whenever GameAssembly.dll changes, which brings the breakage back.

.EXAMPLE
    pwsh -File tools\Fix-Il2CppAssemblies.ps1
    pwsh -File tools\Fix-Il2CppAssemblies.ps1 -GamePath "C:\Games\Last Epoch"
#>
[CmdletBinding()]
param(
    [string]$GamePath = "D:\SteamLibrary\steamapps\common\Last Epoch",
    [switch]$VerifyOnly
)

$ErrorActionPreference = 'Stop'

$asmDir = Join-Path $GamePath 'MelonLoader\Il2CppAssemblies'
$cecil  = Join-Path $GamePath 'MelonLoader\net6\Mono.Cecil.dll'

if (-not (Test-Path $asmDir)) {
    throw "Il2CppAssemblies not found at $asmDir. Run the game once with MelonLoader first."
}
if (-not (Test-Path $cecil)) {
    throw "Mono.Cecil.dll not found at $cecil."
}

# Walks the TypeDef table the same way the CLR does, so it sees the same
# broken nesting the runtime chokes on.
function Get-DuplicateTopLevelTypes([string]$Path) {
    $fs = [System.IO.File]::OpenRead($Path)
    try {
        $pe = [System.Reflection.PortableExecutable.PEReader]::new($fs)
        try {
            $md = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
            $seen = @{}
            foreach ($handle in $md.TypeDefinitions) {
                $type = $md.GetTypeDefinition($handle)
                if (-not $type.GetDeclaringType().IsNil) { continue }
                $key = '{0}.{1}' -f $md.GetString($type.Namespace), $md.GetString($type.Name)
                $seen[$key] = 1 + ($seen[$key] ?? 0)
            }
            return @($seen.GetEnumerator() | Where-Object { $_.Value -gt 1 } |
                ForEach-Object { '{0} x{1}' -f $_.Key, $_.Value })
        } finally { $pe.Dispose() }
    } finally { $fs.Dispose() }
}

Add-Type -Path $cecil

$dlls = Get-ChildItem $asmDir -Filter *.dll -File
Write-Host "Scanning $($dlls.Count) assemblies in $asmDir"

$broken = foreach ($dll in $dlls) {
    $dups = Get-DuplicateTopLevelTypes $dll.FullName
    if ($dups.Count) { [pscustomobject]@{ File = $dll; Duplicates = $dups } }
}

if (-not $broken) {
    Write-Host "No duplicate top-level types. Nothing to fix."
    return
}

foreach ($item in $broken) {
    Write-Host "  BROKEN $($item.File.Name): $($item.Duplicates -join ', ')"
}

if ($VerifyOnly) {
    Write-Host "-VerifyOnly given, stopping before any rewrite."
    exit 1
}

foreach ($item in $broken) {
    $path = $item.File.FullName
    $backup = "$path.orig"
    if (-not (Test-Path $backup)) { Copy-Item $path $backup }

    $reader = New-Object Mono.Cecil.ReaderParameters
    $resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
    $resolver.AddSearchDirectory($asmDir)
    $resolver.AddSearchDirectory((Join-Path $GamePath 'MelonLoader\net6'))
    $reader.AssemblyResolver = $resolver

    $temp = Join-Path ([System.IO.Path]::GetTempPath()) "$($item.File.Name).fixed"
    $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($path, $reader)
    try { $assembly.Write($temp) } finally { $assembly.Dispose() }

    Move-Item $temp $path -Force

    $left = Get-DuplicateTopLevelTypes $path
    if ($left.Count) {
        Write-Host "  FAILED $($item.File.Name): still duplicated - $($left -join ', ')"
    } else {
        Write-Host "  FIXED  $($item.File.Name) (original kept as $($item.File.Name).orig)"
    }
}
