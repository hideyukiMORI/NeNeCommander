[CmdletBinding()]
param(
    [Parameter()]
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [Parameter()]
    [ValidateSet('Product', 'SelfTest')]
    [string] $Mode = 'Product'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($RepositoryRoot)
$sdkVersion = [string] ((Get-Content -LiteralPath (Join-Path $root 'global.json') -Raw | ConvertFrom-Json).sdk.version)
Push-Location $root
try {
    if ((& dotnet --version).Trim() -cne $sdkVersion -or $LASTEXITCODE -ne 0) { throw 'The pinned SDK must be active.' }
    $sdkLine = @(& dotnet --list-sdks | Where-Object { $_ -match ('^' + [regex]::Escape($sdkVersion) + ' \[') })
    if ($sdkLine.Count -ne 1 -or $sdkLine[0] -notmatch '\[(.+)\]$') { throw 'The pinned SDK path is ambiguous.' }
    $sdk = Join-Path $Matches[1] $sdkVersion
    [xml] $bundled = Get-Content -LiteralPath (Join-Path $sdk 'Microsoft.NETCoreSdk.BundledVersions.props') -Raw
    $framework = @($bundled.Project.ItemGroup.KnownFrameworkReference | Where-Object { $_.TargetFramework -ceq 'net10.0' -and $_.Include -ceq 'Microsoft.NETCore.App' })
    if ($framework.Count -ne 1) { throw 'The SDK framework reference pack is ambiguous.' }
    $dotnetRoot = Split-Path -Parent (Split-Path -Parent $sdk)
    $referenceDirectory = Join-Path $dotnetRoot ('packs/Microsoft.NETCore.App.Ref/' + $framework[0].TargetingPackVersion + '/ref/net10.0')
    $compilerDirectory = Join-Path $sdk 'Roslyn/bincore'
    $output = Join-Path $root 'artifacts/record-closure'
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $binary = Join-Path $output 'RecordClosure.dll'
    $sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'record-closure') -Filter '*.cs' -File | Sort-Object Name)
    $references = @(Get-ChildItem -LiteralPath $referenceDirectory -Filter '*.dll' -File | Sort-Object Name)
    if ($sources.Count -eq 0 -or $references.Count -eq 0) { throw 'Compiler inputs are absent.' }
    $arguments = @('/nologo', '/noconfig', '/nostdlib+', '/target:exe', '/langversion:14.0', '/nullable:enable', '/warnaserror+', '/deterministic+', '/optimize+', ('/out:' + $binary))
    $arguments += @($references | ForEach-Object { '/reference:' + $_.FullName })
    foreach ($name in @('Microsoft.CodeAnalysis.dll', 'Microsoft.CodeAnalysis.CSharp.dll')) {
        $arguments += '/reference:' + (Join-Path $compilerDirectory $name)
        Copy-Item -LiteralPath (Join-Path $compilerDirectory $name) -Destination (Join-Path $output $name) -Force
    }
    $arguments += @($sources.FullName)
    & dotnet (Join-Path $compilerDirectory 'csc.dll') @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Record-closure proof compilation failed.' }
    @{ runtimeOptions = @{ tfm = 'net10.0'; framework = @{ name = 'Microsoft.NETCore.App'; version = [string] $framework[0].LatestRuntimeFrameworkVersion } } } |
        ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'RecordClosure.runtimeconfig.json') -Encoding utf8
    $assemblyPaths = @()
    if ($Mode -eq 'Product') {
        $manifest = Get-Content -LiteralPath (Join-Path $root 'eng/architecture.json') -Raw | ConvertFrom-Json
        foreach ($project in @($manifest.projects | Where-Object { $_.path -like 'src/*' -and $_.name -cne 'NeNeCommander.App' })) {
            $projectDirectory = Split-Path -Parent (Join-Path $root $project.path)
            [xml] $projectXml = Get-Content -LiteralPath (Join-Path $root $project.path) -Raw
            $target = [string] $projectXml.Project.PropertyGroup.TargetFramework
            $assembly = Join-Path $projectDirectory ('bin/Release/' + $target + '/' + $project.name + '.dll')
            if (-not (Test-Path -LiteralPath $assembly -PathType Leaf)) { throw "Build the Release production assemblies first: $assembly" }
            $assemblyPaths += $assembly
        }
        if ($assemblyPaths.Count -ne 4) { throw 'Review the closure proof when the production graph changes.' }
    }
    & dotnet $binary $Mode $referenceDirectory $output @assemblyPaths
    if ($LASTEXITCODE -ne 0) { throw '[CS-002] Record-closure proof failed; see artifacts/record-closure.' }
}
finally {
    Pop-Location
}
