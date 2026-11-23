[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $OutputDirectory,
    [Parameter()]
    [string] $WindowsSdkLicensePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'release-notices.ps1')

function Get-ReleaseInputs {
    param([Parameter(Mandatory)] [string] $RepositoryRoot)
    $paths = @(& git -c core.quotepath=false ls-files)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate tracked release inputs.' }
    foreach ($path in $paths) {
        $file = Get-Item -LiteralPath (Join-Path $RepositoryRoot $path)
        if (($file.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Tracked release input is a link: $path."
        }
        [ordered]@{ path = $path; sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
}

function Assert-ReleaseDestination {
    param([Parameter(Mandatory)] [string] $Path, [Parameter(Mandatory)] [string] $RepositoryRoot)
    if (-not [System.IO.Path]::IsPathFullyQualified($Path)) { throw 'The release destination must be absolute.' }
    $destination = [System.IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
    $prefix = $RepositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if ($destination -eq $RepositoryRoot -or $destination.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Release output must be outside the source checkout.'
    }
    if (Test-Path -LiteralPath $destination) { throw 'The release destination already exists; preserve it and choose a new directory.' }
    $ancestor = [System.IO.Directory]::GetParent($destination)
    while ($null -ne $ancestor) {
        if (Test-Path -LiteralPath $ancestor.FullName) {
            $entry = Get-Item -LiteralPath $ancestor.FullName -Force
            if (-not $entry.PSIsContainer -or ($entry.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw 'The release destination has a non-directory or linked ancestor.'
            }
        }
        $ancestor = $ancestor.Parent
    }
    return $destination
}

$root = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$destination = Assert-ReleaseDestination -Path $OutputDirectory -RepositoryRoot $root
$project = 'src/NeNeCommander.App/NeNeCommander.App.csproj'
[xml] $projectXml = Get-Content -LiteralPath (Join-Path $root $project) -Raw
$version = [string] $projectXml.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') { throw 'The App project must declare one release version.' }
$runtime = 'win-x64'
$name = "NeNeCommander-$version-$runtime"
$payload = Join-Path $destination $name
$archive = Join-Path $destination "$name.zip"
$transcriptStarted = $false
Push-Location $root
try {
    $status = (& git status --porcelain=v1 --untracked-files=all) -join "`n"
    if ($LASTEXITCODE -ne 0 -or $status.Length -ne 0) { throw 'Publish requires a clean, committed source checkout.' }
    $commit = (& git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[0-9a-f]{40}$') { throw 'Cannot identify the release source commit.' }
    $sdk = (& dotnet --version).Trim()
    $pinnedSdk = (Get-Content -LiteralPath 'global.json' -Raw | ConvertFrom-Json).sdk.version
    if ($LASTEXITCODE -ne 0 -or $sdk -cne $pinnedSdk) { throw 'The pinned .NET SDK is not active.' }
    $before = @(Get-ReleaseInputs -RepositoryRoot $root)
    [void] [System.IO.Directory]::CreateDirectory($destination)
    Start-Transcript -LiteralPath (Join-Path $destination 'publish.log') | Out-Null
    $transcriptStarted = $true
    $properties = @('-p:Configuration=Release', '-p:SelfContained=true', '-p:ContinuousIntegrationBuild=true')
    $evaluationArguments = @('msbuild', $project, '-nologo', '-getProperty:RuntimeIdentifier') + $properties
    $restoreArguments = @('restore', $project, '--locked-mode', '--disable-parallel') + $properties
    $publishArguments = @('publish', $project, '--no-restore', '-maxcpucount:4', '-o', $payload, "-p:SourceRevisionId=$commit") + $properties
    Write-Host ('dotnet ' + ($evaluationArguments -join ' '))
    $evaluatedRuntime = (& dotnet @evaluationArguments) -join "`n"
    if ($LASTEXITCODE -ne 0 -or $evaluatedRuntime.Trim() -cne $runtime) {
        throw 'The App project must evaluate to the supported win-x64 runtime.'
    }
    Write-Host ('dotnet ' + ($restoreArguments -join ' '))
    & dotnet @restoreArguments
    if ($LASTEXITCODE -ne 0) { throw 'Locked release restore failed.' }
    Write-Host ('dotnet ' + ($publishArguments -join ' '))
    & dotnet @publishArguments
    if ($LASTEXITCODE -ne 0) { throw 'Release publish failed.' }

    foreach ($required in @('NeNeCommander.App.exe', 'NeNeCommander.App.dll', 'NeNeCommander.App.runtimeconfig.json', 'coreclr.dll', 'hostfxr.dll', 'hostpolicy.dll', 'Microsoft.UI.Xaml.dll', 'NeNeCommander.App.pri')) {
        if (-not (Test-Path -LiteralPath (Join-Path $payload $required) -PathType Leaf)) { throw "Published runtime file is absent: $required." }
    }
    $runtimeConfig = Get-Content -LiteralPath (Join-Path $payload 'NeNeCommander.App.runtimeconfig.json') -Raw | ConvertFrom-Json -AsHashtable
    if ($runtimeConfig.runtimeOptions.ContainsKey('framework') -or $runtimeConfig.runtimeOptions.ContainsKey('frameworks') -or
        -not $runtimeConfig.runtimeOptions.ContainsKey('includedFrameworks')) { throw 'The published application is not .NET self-contained.' }
    $binary = Join-Path $payload 'NeNeCommander.App.exe'
    $binaryVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($binary)
    if ($binaryVersion.ProductVersion -cne "$version+$commit" -or $binaryVersion.FileVersion -cne ($version.Split('-')[0] + '.0')) {
        throw 'The published executable version does not match the release source.'
    }
    if ($binaryVersion.LegalCopyright -cne [string] $projectXml.Project.PropertyGroup.Copyright) {
        throw 'The published executable copyright does not match the App project.'
    }
    $signature = Get-AuthenticodeSignature -LiteralPath $binary
    if ($signature.Status -ne 'NotSigned') { throw 'This unsigned release path received an unexpected application signature state.' }
    $licenses = @(Copy-ReleaseNotices -AssetsPath (Join-Path $root 'src/NeNeCommander.App/obj/project.assets.json') -Destination (Join-Path $payload 'licenses') -WindowsSdkLicensePath $WindowsSdkLicensePath)
    Copy-Item -LiteralPath (Join-Path $root 'docs/release/README.txt') -Destination (Join-Path $payload 'README.txt')
    [System.IO.File]::WriteAllText((Join-Path $payload 'RELEASE.txt'), "NeNe Commander $version`n$($binaryVersion.LegalCopyright)`nSource: $commit`nPlatform: Windows 11 x64`nApplication signature: unsigned`n")

    $after = @(Get-ReleaseInputs -RepositoryRoot $root)
    $afterStatus = (& git status --porcelain=v1 --untracked-files=all) -join "`n"
    if ($LASTEXITCODE -ne 0) { throw 'Cannot verify the source checkout after publish.' }
    if (($before | ConvertTo-Json -Compress) -cne ($after | ConvertTo-Json -Compress) -or
        $afterStatus.Length -ne 0) {
        throw 'Publish changed tracked inputs or the source checkout.'
    }
    $entries = @(Get-ChildItem -LiteralPath $payload -Recurse -Force)
    if (@($entries | Where-Object { ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 }).Count -ne 0) {
        throw 'Publish produced a linked file or directory.'
    }
    $files = @($entries | Where-Object { -not $_.PSIsContainer } | Sort-Object FullName | ForEach-Object {
        [ordered]@{
            path = [System.IO.Path]::GetRelativePath($payload, $_.FullName).Replace('\', '/')
            bytes = $_.Length
            sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    })
    [System.IO.Compression.ZipFile]::CreateFromDirectory($payload, $archive, [System.IO.Compression.CompressionLevel]::Optimal, $true)
    $archiveHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    [xml] $appManifest = Get-Content -LiteralPath 'src/NeNeCommander.App/app.manifest' -Raw
    $manifest = [ordered]@{
        schemaVersion = 1
        product = 'NeNe Commander'
        version = $version
        sourceCommit = $commit
        createdAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        runtimeIdentifier = $runtime
        minimumWindowsBuild = 22000
        applicationSigning = 'unsigned'
        productVersion = $binaryVersion.ProductVersion
        fileVersion = $binaryVersion.FileVersion
        copyright = $binaryVersion.LegalCopyright
        win32AssemblyIdentity = [string] $appManifest.assembly.assemblyIdentity.version
        sdk = $sdk
        powershell = $PSVersionTable.PSVersion.ToString()
        operatingSystem = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
        frameworkPacks = $runtimeConfig.runtimeOptions.includedFrameworks
        commands = @(
            [ordered]@{ executable = 'dotnet'; arguments = $evaluationArguments }
            [ordered]@{ executable = 'dotnet'; arguments = $restoreArguments }
            [ordered]@{ executable = 'dotnet'; arguments = $publishArguments }
        )
        inputsUnchanged = $true
        inputs = $before
        packages = $licenses
        paletteNotices = (Get-Content -LiteralPath (Join-Path $payload 'licenses/index.json') -Raw | ConvertFrom-Json -AsHashtable).paletteNotices
        files = $files
        archive = [ordered]@{ path = "$name.zip"; bytes = (Get-Item -LiteralPath $archive).Length; sha256 = $archiveHash }
        launchCheck = 'not-run'
        releaseApproval = 'not-evaluated; packaging alone does not establish release readiness'
    }
    $manifestPath = Join-Path $destination 'release-manifest.json'
    [System.IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 12) + "`n")
    $manifestHash = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
    [System.IO.File]::WriteAllText((Join-Path $destination 'SHA256SUMS.txt'), "$archiveHash  $name.zip`n$manifestHash  release-manifest.json`n")
    Write-Host "Packaged $archive. Launch and release evidence remain separate."
}
finally {
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
    Pop-Location
}
