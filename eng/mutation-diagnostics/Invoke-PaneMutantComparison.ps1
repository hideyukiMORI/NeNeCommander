[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'PaneMutantEvidence.psm1') -Force

# This entry point is deliberately neither a local retry nor a general selector.
if ($env:GITHUB_ACTIONS -cne 'true' -or $env:GITHUB_EVENT_NAME -cne 'workflow_dispatch' -or
    $env:GITHUB_REPOSITORY -cne 'hideyukiMORI/NeNeCommander' -or $env:GITHUB_REF -cne 'refs/heads/main' -or
    $env:GITHUB_RUN_ID -notmatch '^\d+$' -or $env:GITHUB_RUN_ATTEMPT -cne '1') {
    throw 'This fixed comparison requires one first-attempt manual run on repository main.'
}
$control = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (-not $control.StartsWith('D:\', [StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath 'D:/')) { throw 'The control checkout and task storage must be on D.' }
$runName = 'ci-' + $env:GITHUB_RUN_ID + '-' + $env:GITHUB_RUN_ATTEMPT
$source = 'D:/NeNeCommander/worktrees/issue-439-' + $runName
$evidence = 'D:/NeNeCommander/evidence/issue-439/' + $runName
$output = 'D:/NeNeCommander/outputs/issue-439/' + $runName
foreach ($path in @($source, $evidence, $output)) {
    if (Test-Path -LiteralPath $path) { throw 'A run-owned path already exists; refusing overwrite or retry.' }
}
New-Item -ItemType Directory -Path $evidence, $output, (Join-Path $output 'tmp'), (Join-Path $output 'cli'), (Join-Path $output 'packages') -Force | Out-Null
$manifest = $null
$rawExit = $null
$comparison = $null
$registered = $false
$assemblies = [ordered]@{}
$errors = [Collections.Generic.List[string]]::new()
$commands = [Collections.Generic.List[object]]::new()
$previous = @{}
foreach ($name in @('TEMP', 'TMP', 'DOTNET_CLI_HOME', 'NUGET_PACKAGES')) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
$record = [ordered]@{
    issue = 439; referenceIssue = 429; startedUtc = [DateTimeOffset]::UtcNow.ToString('o');
    controlCommit = $env:GITHUB_SHA; source = $source; evidence = $evidence; output = $output;
    runUrl = 'https://github.com/' + $env:GITHUB_REPOSITORY + '/actions/runs/' + $env:GITHUB_RUN_ID;
    attempt = $env:GITHUB_RUN_ATTEMPT; sourceCommit = $null; rawExit = $null; evidenceValid = $false
}
$record | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $evidence 'result.json') -Encoding utf8
try {
    $manifestPath = Join-Path $PSScriptRoot 'pane-mutant-429.json'
    $manifest = Read-PaneMutantManifest -Path $manifestPath
    Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $evidence 'fixed-manifest.json')
    $record.sourceCommit = $manifest.sourceCommit
    $environment = [ordered]@{
        osVersion = [Environment]::OSVersion.VersionString; architecture = [Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString();
        processorCount = [Environment]::ProcessorCount; culture = (Get-Culture).Name; uiCulture = (Get-UICulture).Name;
        timeZone = (Get-TimeZone).Id; imageOS = $env:ImageOS; imageVersion = $env:ImageVersion;
        runnerOS = $env:RUNNER_OS; runnerArchitecture = $env:RUNNER_ARCH; powershell = $PSVersionTable.PSVersion.ToString()
    }
    $environment | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence 'environment.json') -Encoding utf8
    $env:TEMP = Join-Path $output 'tmp'
    $env:TMP = $env:TEMP
    $env:DOTNET_CLI_HOME = Join-Path $output 'cli'
    $env:NUGET_PACKAGES = Join-Path $output 'packages'
    $checkout = Invoke-PaneCommand -Name 'checkout-source' -Executable 'git' -Arguments @('-c', 'core.autocrlf=true', '-c', 'core.eol=crlf', 'worktree', 'add', '--detach', $source, $manifest.sourceCommit) -Directory $control -Evidence $evidence
    $commands.Add($checkout)
    if ($checkout.exitCode -ne 0) { throw 'Fixed source checkout failed.' }
    $registered = $true
    $head = Invoke-PaneCommand -Name 'source-head' -Executable 'git' -Arguments @('rev-parse', 'HEAD') -Directory $source -Evidence $evidence
    $commands.Add($head)
    if ($head.exitCode -ne 0 -or (Get-Content -LiteralPath (Join-Path $evidence 'source-head.log') -Raw).Trim() -cne $manifest.sourceCommit) { throw 'Source HEAD differs.' }
    $status = Invoke-PaneCommand -Name 'source-status-before' -Executable 'git' -Arguments @('-c', 'core.autocrlf=true', 'status', '--porcelain=v1', '--untracked-files=all') -Directory $source -Evidence $evidence
    $commands.Add($status)
    if ($status.exitCode -ne 0 -or (Get-Item -LiteralPath (Join-Path $evidence 'source-status-before.log')).Length -ne 0) { throw 'Source is not clean before preparation.' }
    Assert-PaneInputHashes -Root $source -Expected $manifest.inputFiles
    $text = [IO.File]::ReadAllText((Join-Path $source $manifest.target.path))
    if ($text.Substring($manifest.target.start, $manifest.target.end - $manifest.target.start) -cne $manifest.target.originalSpan) { throw 'Mutation span differs.' }
    $record.inputsVerifiedBefore = $manifest.inputFiles.Count
    $preparation = @(
        @{ name = 'environment'; arguments = @('--info') },
        @{ name = 'restore'; arguments = @('restore', 'tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj', '-p:Configuration=Release', '--locked-mode') },
        @{ name = 'tool-restore'; arguments = @('tool', 'restore') },
        @{ name = 'build-original'; arguments = @('build', 'tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj', '--configuration', 'Release', '--no-restore') }
    )
    foreach ($step in $preparation) {
        Assert-PaneInputHashes -Root $source -Expected $manifest.inputFiles
        $command = Invoke-PaneCommand -Name $step.name -Executable 'dotnet' -Arguments $step.arguments -Directory $source -Evidence $evidence
        $commands.Add($command)
        if ($command.exitCode -ne 0) { throw ('Preparation failed: ' + $step.name) }
    }
    Assert-PaneInputHashes -Root $source -Expected $manifest.inputFiles
    $toolDirectory = Join-Path $env:NUGET_PACKAGES ('dotnet-stryker/' + $manifest.tools.stryker + '/tools/net10.0/any')
    foreach ($entry in @(@{ file = 'Stryker.Core.dll'; hash = $manifest.tools.coreSha256 }, @{ file = 'Stryker.CLI.dll'; hash = $manifest.tools.cliSha256 })) {
        if ((Get-FileHash -LiteralPath (Join-Path $toolDirectory $entry.file) -Algorithm SHA256).Hash.ToLowerInvariant() -cne $entry.hash) { throw 'Pinned Stryker binary differs.' }
    }
    $toolHashes = [ordered]@{}
    foreach ($file in Get-ChildItem -LiteralPath $env:NUGET_PACKAGES -Recurse -File | Where-Object { $_.Name -match '^(Stryker.*\.dll|testhost.*\.(dll|exe)|Microsoft\.VisualStudio\.TestPlatform\.(MSTest\.TestAdapter|TestFramework)\.dll)$' }) {
        $toolHashes[[IO.Path]::GetRelativePath($env:NUGET_PACKAGES, $file.FullName)] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $toolHashes | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence 'tool-binary-hashes.json') -Encoding utf8
    foreach ($path in @('src/NeNeCommander.Application/bin/Release/net10.0/NeNeCommander.Application.dll', 'tests/NeNeCommander.Application.Tests/bin/Release/net10.0/NeNeCommander.Application.Tests.dll')) {
        $assemblies[$path] = (Get-FileHash -LiteralPath (Join-Path $source $path) -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $assemblies | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence 'original-assemblies.json') -Encoding utf8
    $filter = ($manifest.testIds | ForEach-Object { 'Id=' + $_ }) -join '|'
    $arguments = @('stryker', '--config-file', '../../stryker-config.json', '--project', 'NeNeCommander.Application.csproj', '--break-at', '95', '--skip-version-check', '--mutate', $manifest.target.pattern, '--diag', '--log-to-file', '--test-case-filter', $filter, '--output', (Join-Path $evidence 'stryker-31'))
    $command = Invoke-PaneCommand -Name 'stryker-31' -Executable 'dotnet' -Arguments $arguments -Directory (Join-Path $source 'tests/NeNeCommander.Application.Tests') -Evidence $evidence
    $commands.Add($command)
    $rawExit = $command.exitCode
    $comparison = Get-PaneMutantEvidence -Output (Join-Path $evidence 'stryker-31') -Manifest $manifest -RawExit $rawExit
    $comparison | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $evidence 'comparison.json') -Encoding utf8
}
catch { $errors.Add($_.ToString()) }
finally {
    if ($registered) {
        try {
            Assert-PaneRestoration -Root $source -Inputs $manifest.inputFiles -Assemblies $assemblies
            $status = Invoke-PaneCommand -Name 'source-status-after' -Executable 'git' -Arguments @('-c', 'core.autocrlf=true', 'status', '--porcelain=v1', '--untracked-files=all') -Directory $source -Evidence $evidence
            $commands.Add($status)
            if ($status.exitCode -ne 0 -or (Get-Item -LiteralPath (Join-Path $evidence 'source-status-after.log')).Length -ne 0) { throw 'Diagnostic source is not clean after execution.' }
            $record.restoration = @{ inputsVerified = $manifest.inputFiles.Count; assembliesVerified = $assemblies.Count; status = 'unchanged' }
        }
        catch { $errors.Add('Restoration: ' + $_.ToString()) }
    }
    foreach ($name in $previous.Keys) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
    $record.rawExit = $rawExit
    $record.evidenceValid = $errors.Count -eq 0 -and $null -ne $comparison
    $record.finalExit = Get-PaneDiagnosticExit -RawExit $rawExit -EvidenceValid $record.evidenceValid
    $record.errors = $errors.ToArray()
    $record.completedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    $record.commands = $commands.ToArray()
    $record | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $evidence 'result.json') -Encoding utf8
    $hashes = [ordered]@{}
    foreach ($file in Get-ChildItem -LiteralPath $evidence -Recurse -File -Force) { $hashes[[IO.Path]::GetRelativePath($evidence, $file.FullName)] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
    $hashes | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence 'artifact-hashes.json') -Encoding utf8
    Write-Host ('Fixed comparison: raw exit {0}; evidence valid {1}; final exit {2}; {3}' -f $rawExit, $record.evidenceValid, $record.finalExit, $evidence)
}
exit $record.finalExit
