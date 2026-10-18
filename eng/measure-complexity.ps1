[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [ValidateSet('Inspect', 'SelfTest')]
    [string] $Mode = 'Inspect'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$resultPath = $null
$locationPushed = $false
try {
    $root = [IO.Path]::GetFullPath($RepositoryRoot)
    if (-not [IO.Directory]::Exists($root)) { throw 'Repository root does not exist.' }
    $output = Join-Path $root ('artifacts/complexity/' + $Mode)
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $resultPath = Join-Path $output 'result.json'
    '{"status":"incomplete","metricVersion":"cs013-v1"}' | Set-Content -LiteralPath $resultPath -Encoding utf8
    Push-Location $root
    $locationPushed = $true
    $sdkVersion = [string] ((Get-Content -LiteralPath (Join-Path $root 'global.json') -Raw | ConvertFrom-Json).sdk.version)
    if ($sdkVersion -cne '10.0.401' -or (& dotnet --version).Trim() -cne $sdkVersion -or $LASTEXITCODE -ne 0) { throw 'The pinned SDK 10.0.401 must be active.' }
    $sdkLine = @(& dotnet --list-sdks | Where-Object { $_ -match ('^' + [regex]::Escape($sdkVersion) + ' \[') })
    if ($sdkLine.Count -ne 1 -or $sdkLine[0] -notmatch '\[(.+)\]$') { throw 'The pinned SDK path is ambiguous.' }
    $sdk = Join-Path $Matches[1] $sdkVersion
    [xml] $bundled = Get-Content -LiteralPath (Join-Path $sdk 'Microsoft.NETCoreSdk.BundledVersions.props') -Raw
    $framework = @($bundled.Project.ItemGroup.KnownFrameworkReference | Where-Object { $_.TargetFramework -ceq 'net10.0' -and $_.Include -ceq 'Microsoft.NETCore.App' })
    if ($framework.Count -ne 1) { throw 'The SDK framework reference pack is ambiguous.' }
    $dotnetRoot = Split-Path -Parent (Split-Path -Parent $sdk)
    $referenceDirectory = Join-Path $dotnetRoot ('packs/Microsoft.NETCore.App.Ref/' + $framework[0].TargetingPackVersion + '/ref/net10.0')
    $compilerDirectory = Join-Path $sdk 'Roslyn/bincore'
    $binary = Join-Path $output 'Complexity.dll'
    $sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'complexity') -Filter '*.cs' -File | Sort-Object Name)
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
    if ($LASTEXITCODE -ne 0) { throw 'Complexity tool compilation failed.' }
    @{ runtimeOptions = @{ tfm = 'net10.0'; framework = @{ name = 'Microsoft.NETCore.App'; version = [string] $framework[0].LatestRuntimeFrameworkVersion } } } |
        ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'Complexity.runtimeconfig.json') -Encoding utf8
    & dotnet $binary $Mode $root $referenceDirectory $output
    $inspectionExit = $LASTEXITCODE
    if ($inspectionExit -notin 0, 1, 2) { throw "Unexpected checker exit: $inspectionExit" }
}
catch {
    if ($null -ne $resultPath) {
        try {
            @{ status = 'error'; metricVersion = 'cs013-v1'; error = $_.Exception.Message } |
                ConvertTo-Json | Set-Content -LiteralPath $resultPath -Encoding utf8
        }
        catch { Write-Error -Message 'Could not write the failed measurement result.' -ErrorAction Continue }
    }
    Write-Error -Message $_.Exception.Message -ErrorAction Continue
    $inspectionExit = 2
}
finally {
    if ($locationPushed) { Pop-Location }
}
exit $inspectionExit
