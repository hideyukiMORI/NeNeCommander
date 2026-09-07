[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$testProject = Join-Path $repositoryRoot 'tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj'
$infrastructureAssembly = Join-Path $repositoryRoot 'src/NeNeCommander.Infrastructure.Windows/bin/Release/net10.0-windows10.0.26100.0/NeNeCommander.Infrastructure.Windows.dll'
$identityTypeName = 'NeNeCommander.Infrastructure.Windows.FileOperations.WindowsFileIdentifier'
$rootParameterName = 'NENE_COMMANDER_WSL_TEST_ROOT'
$temporaryRootIdentityParameterName = 'NENE_COMMANDER_WSL_TMP_IDENTITY'
$configuredRootIdentityParameterName = 'NENE_COMMANDER_WSL_ROOT_IDENTITY'
$homeFactParameterName = 'NENE_COMMANDER_WSL_HOME_FACT'
$mountFactParameterName = 'NENE_COMMANDER_WSL_MOUNT_FACT'
$homeFactAccepted = 'HomeOutsideRoot:v1'
$mountFactAccepted = 'NativeWslFileSystem:v1'
$settingsPath = $null
$resultsDirectory = $null
$resultsPath = $null

function Get-LiveWslIdentityMethod {
    if (-not (Test-Path -LiteralPath $infrastructureAssembly -PathType Leaf)) {
        throw 'The current Release infrastructure assembly is unavailable.'
    }

    $assembly = [System.Reflection.Assembly]::LoadFrom($infrastructureAssembly)
    $identityType = $assembly.GetType($identityTypeName, $true, $false)
    $flags = [System.Reflection.BindingFlags]::Static -bor [System.Reflection.BindingFlags]::NonPublic
    $method = $identityType.GetMethod('Describe', $flags)
    if ($null -eq $method -or $method.ReturnType -ne [string]) {
        throw 'The fixed Windows file identity member is unavailable.'
    }
    $parameters = @($method.GetParameters())
    if ($parameters.Count -ne 1 -or $parameters[0].ParameterType -ne [string]) {
        throw 'The fixed Windows file identity member has an unexpected signature.'
    }

    return $method
}

function Get-LiveWslIdentity {
    param(
        [Parameter(Mandatory)]
        [System.Reflection.MethodInfo] $Method,

        [Parameter(Mandatory)]
        [string] $Path
    )

    $identity = [string] $Method.Invoke($null, @($Path))
    if ($identity -cnotmatch '^[0-9A-F]{48}$') {
        throw 'The fixed Windows file identity member returned an invalid value.'
    }

    return $identity
}

function Get-WslText {
    param(
        [Parameter(Mandatory)]
        [string] $Distribution,

        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    $value = (& wsl.exe --distribution $Distribution --exec @Arguments) -join "`n"
    if ($LASTEXITCODE -ne 0) {
        throw 'A fixed read-only WSL admission query failed.'
    }

    return $value.Trim()
}

function Get-WslVersionText {
    $start = [System.Diagnostics.ProcessStartInfo]::new()
    $start.FileName = 'wsl.exe'
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.StandardOutputEncoding = [System.Text.Encoding]::Unicode
    $start.ArgumentList.Add('--version')
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $start
    try {
        [void] $process.Start()
        $standardOutput = $process.StandardOutput.ReadToEnd()
        $standardError = $process.StandardError.ReadToEnd()
        $process.WaitForExit()
        $exitCode = $process.ExitCode
    }
    finally {
        $process.Dispose()
    }
    if ($exitCode -ne 0 -or $standardError.Length -ne 0 -or
        $standardOutput.Length -eq 0 -or $standardOutput.Length -gt 4096) {
        throw 'The bounded WSL version record could not be read.'
    }
    return $standardOutput.Trim()
}

function Add-TestParameter {
    param(
        [Parameter(Mandatory)]
        [System.Xml.XmlDocument] $Document,

        [Parameter(Mandatory)]
        [System.Xml.XmlElement] $Parent,

        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory)]
        [string] $Value
    )

    $parameter = $Document.CreateElement('Parameter')
    $parameter.SetAttribute('name', $Name)
    $parameter.SetAttribute('value', $Value)
    [void] $Parent.AppendChild($parameter)
}

function New-LiveWslRunSettings {
    param(
        [Parameter(Mandatory)]
        [System.Collections.Generic.IReadOnlyDictionary[string, string]] $Parameters
    )

    $path = [System.IO.Path]::GetTempFileName()
    $document = [System.Xml.XmlDocument]::new()
    $runSettings = $document.CreateElement('RunSettings')
    [void] $document.AppendChild($runSettings)
    $testRunParameters = $document.CreateElement('TestRunParameters')
    [void] $runSettings.AppendChild($testRunParameters)
    foreach ($entry in $Parameters.GetEnumerator()) {
        Add-TestParameter -Document $document -Parent $testRunParameters -Name $entry.Key -Value $entry.Value
    }
    $document.Save($path)
    return $path
}

function Assert-LiveWslResults {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw 'The live WSL result record is missing.'
    }
    [xml] $document = Get-Content -LiteralPath $Path -Raw
    $results = @($document.SelectNodes("//*[local-name()='UnitTestResult']"))
    $expected = @(
        'ExecuteAsyncWhenLiveNestedCopyCompletesPreservesSourceAndTargetAsync',
        'ExecuteAsyncWhenLiveCompositeMoveCompletesDeletesSourceAfterVerifiedTargetAsync',
        'ExecuteAsyncWhenLiveSourceContainsOwnedLinkRejectsWithoutEffectAsync')
    if ($results.Count -ne $expected.Count) {
        throw 'The live WSL result record does not contain exactly three required cells.'
    }
    foreach ($name in $expected) {
        $matched = @($results | Where-Object { $_.testName -ceq $name })
        if ($matched.Count -ne 1 -or $matched[0].outcome -cne 'Passed') {
            throw 'A required live WSL cell was missing, skipped, or did not pass.'
        }
    }
}

Push-Location $repositoryRoot
try {
    Write-Host ('LiveWsl start: ' + [DateTimeOffset]::Now.ToString('yyyy-MM-ddTHH:mm:sszzz', [Globalization.CultureInfo]::InvariantCulture))
    $commit = (& git rev-parse --verify HEAD).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw 'The live proof commit could not be identified.'
    }
    Write-Host ('Commit: ' + $commit)
    $snapshotState = if (@(& git status --porcelain=v1 --untracked-files=all).Count -eq 0) {
        'clean'
    }
    else {
        'dirty'
    }
    Write-Host ('Snapshot: ' + $snapshotState)
    Write-Host ('OS: ' + [System.Environment]::OSVersion.VersionString)
    Write-Host (Get-WslVersionText)

    & dotnet build $testProject --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw 'The live WSL test project build failed.'
    }

    $configuredRoot = [System.Environment]::GetEnvironmentVariable($rootParameterName, 'Process')
    if ([string]::IsNullOrWhiteSpace($configuredRoot)) {
        & dotnet test --project $testProject --configuration Release --no-build --no-restore --filter 'TestCategory=LiveWsl'
        throw 'LiveWsl:Unexecuted:RootParameterAbsent'
    }

    $rootMatch = [regex]::Match(
        $configuredRoot,
        '^\\\\wsl\.localhost\\(?<distribution>[A-Za-z0-9][A-Za-z0-9._-]*)\\tmp\\(?<leaf>NeNeCommander-Live-[A-Za-z0-9][A-Za-z0-9._-]*)$')
    if (-not $rootMatch.Success -or -not (Test-Path -LiteralPath $configuredRoot -PathType Container)) {
        throw 'The configured live WSL root is unavailable or has an unsafe shape.'
    }
    if (@(Get-ChildItem -LiteralPath $configuredRoot -Force | Select-Object -First 1).Count -ne 0) {
        throw 'The configured live WSL root is not empty.'
    }

    $distribution = $rootMatch.Groups['distribution'].Value
    $leaf = $rootMatch.Groups['leaf'].Value
    $temporaryRoot = "\\wsl.localhost\$distribution\tmp"
    $linuxRoot = "/tmp/$leaf"
    $identityMethod = Get-LiveWslIdentityMethod
    $temporaryRootIdentity = Get-LiveWslIdentity -Method $identityMethod -Path $temporaryRoot
    $configuredRootIdentity = Get-LiveWslIdentity -Method $identityMethod -Path $configuredRoot

    $actualTemporaryRoot = Get-WslText -Distribution $distribution -Arguments @('readlink', '-f', '--', '/tmp')
    $actualRoot = Get-WslText -Distribution $distribution -Arguments @('readlink', '-f', '--', $linuxRoot)
    $homeText = Get-WslText -Distribution $distribution -Arguments @(
        'sh',
        '-c',
        'set -eu; homes=$(getent passwd); test -n "$homes"; printf "%s\n" "$homes" | cut -d: -f6 | while IFS= read -r home; do readlink -m -- "$home"; done')
    $homes = @($homeText -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $mount = Get-WslText -Distribution $distribution -Arguments @('findmnt', '-T', $linuxRoot, '-n', '-o', 'TARGET,FSTYPE')
    $mountParts = $mount -split '\s+'
    $rootTouchesHome = $false
    foreach ($home in $homes) {
        if (-not $home.StartsWith('/', [System.StringComparison]::Ordinal)) {
            $rootTouchesHome = $true
        }
        elseif ($home -cne '/' -and (
            $home -eq $linuxRoot -or
            $home.StartsWith($linuxRoot + '/', [System.StringComparison]::Ordinal) -or
            $linuxRoot.StartsWith($home.TrimEnd('/') + '/', [System.StringComparison]::Ordinal))) {
            $rootTouchesHome = $true
        }
    }
    if ($homes.Count -eq 0 -or $actualTemporaryRoot -cne '/tmp' -or
        $actualRoot -cne $linuxRoot -or $rootTouchesHome) {
        throw 'The live WSL home and canonical-path admission facts were rejected.'
    }
    if ($mountParts.Count -ne 2 -or $mountParts[0] -cne '/' -or $mountParts[1] -cne 'ext4') {
        throw 'The live WSL root is not on the admitted native WSL filesystem.'
    }

    if ((Get-LiveWslIdentity -Method $identityMethod -Path $temporaryRoot) -cne $temporaryRootIdentity -or
        (Get-LiveWslIdentity -Method $identityMethod -Path $configuredRoot) -cne $configuredRootIdentity) {
        throw 'The live WSL root identity changed after admission queries.'
    }

    $parameters = [System.Collections.Generic.Dictionary[string, string]]::new(
        [System.StringComparer]::Ordinal)
    $parameters.Add($rootParameterName, $configuredRoot)
    $parameters.Add($temporaryRootIdentityParameterName, $temporaryRootIdentity)
    $parameters.Add($configuredRootIdentityParameterName, $configuredRootIdentity)
    $parameters.Add($homeFactParameterName, $homeFactAccepted)
    $parameters.Add($mountFactParameterName, $mountFactAccepted)
    $settingsPath = New-LiveWslRunSettings -Parameters $parameters

    do {
        $resultsDirectory = Join-Path ([System.IO.Path]::GetTempPath()) (
            'NeNeCommander-LiveWsl-' + [System.IO.Path]::GetRandomFileName())
    }
    while (Test-Path -LiteralPath $resultsDirectory)
    [void] [System.IO.Directory]::CreateDirectory($resultsDirectory)
    $resultsPath = Join-Path $resultsDirectory 'live.trx'

    & dotnet test --project $testProject --configuration Release --no-build --no-restore `
        --filter 'TestCategory=LiveWsl' -- --settings $settingsPath --report-trx `
        --report-trx-filename live.trx --results-directory $resultsDirectory
    if ($LASTEXITCODE -ne 0) {
        throw 'The live WSL proof failed.'
    }
    Assert-LiveWslResults -Path $resultsPath
    Write-Host 'LiveWsl result: PASS'
}
finally {
    if ($null -ne $settingsPath -and (Test-Path -LiteralPath $settingsPath -PathType Leaf)) {
        Remove-Item -LiteralPath $settingsPath -Force
    }
    if ($null -ne $resultsPath -and (Test-Path -LiteralPath $resultsPath -PathType Leaf)) {
        Remove-Item -LiteralPath $resultsPath -Force
    }
    if ($null -ne $resultsDirectory -and (Test-Path -LiteralPath $resultsDirectory -PathType Container)) {
        Remove-Item -LiteralPath $resultsDirectory -Force
    }
    Write-Host ('LiveWsl end: ' + [DateTimeOffset]::Now.ToString('yyyy-MM-ddTHH:mm:sszzz', [Globalization.CultureInfo]::InvariantCulture))
    Pop-Location
}
