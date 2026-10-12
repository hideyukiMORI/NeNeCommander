[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Root,
    [string[]] $Case = @()
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$declared = @(
    'RecycleWhenLiveFileIsOwnedReturnsReceiptAndRestoresAsync',
    'RecycleWhenLiveDirectoryIsOwnedRestoresItsTreeAsync',
    'PreDeleteWhenLiveShellProposesPermanentDeleteVetoesItAsync'
)
[string[]] $selected = if ($Case.Count -eq 0) { $declared } else { $Case }
if (@($selected | Select-Object -Unique).Count -ne $selected.Count -or
    @($selected | Where-Object { $_ -cnotin $declared }).Count -ne 0) {
    throw 'Select only distinct declared live recycle cases.'
}
$ownedParent = [System.IO.Path]::GetFullPath($Root)
if (-not $ownedParent.StartsWith('D:\NeNeCommander\outputs\', [StringComparison]::OrdinalIgnoreCase) -or
    -not (Test-Path -LiteralPath $ownedParent -PathType Container)) {
    throw 'Live recycle requires an existing task directory beneath D:\NeNeCommander\outputs.'
}
$ancestor = Get-Item -LiteralPath $ownedParent
while ($null -ne $ancestor) {
    if (($ancestor.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'A live recycle ancestor is a link.'
    }
    $ancestor = $ancestor.Parent
}
$run = Join-Path $ownedParent ('recycle-proof-' + [System.IO.Path]::GetRandomFileName())
[void] (New-Item -ItemType Directory -Path $run)
$settingsPath = Join-Path $run 'live.runsettings'
$document = [System.Xml.XmlDocument]::new()
$settings = $document.CreateElement('RunSettings')
[void] $document.AppendChild($settings)
$parameters = $document.CreateElement('TestRunParameters')
[void] $settings.AppendChild($parameters)
$parameter = $document.CreateElement('Parameter')
$parameter.SetAttribute('name', 'NENE_COMMANDER_RECYCLE_TEST_ROOT')
$parameter.SetAttribute('value', $ownedParent)
[void] $parameters.AppendChild($parameter)
$document.Save($settingsPath)
$previousTemp = $env:TEMP
$previousTmp = $env:TMP
$env:TEMP = $ownedParent
$env:TMP = $ownedParent
try {
    $commit = git rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Could not identify the candidate.' }
    $dirty = @(git status --porcelain)
    Write-Output "LiveRecycle commit=$commit dirty=$($dirty.Count -ne 0) declared=$($declared.Count) selected=$($selected.Count)"
    Write-Output "Windows=$([System.Environment]::OSVersion.Version) cases=$($selected -join ',')"
    $filter = 'TestCategory=LiveRecycle&(' + (($selected | ForEach-Object { 'FullyQualifiedName~' + $_ }) -join '|') + ')'
    dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj `
        --configuration Release --no-build --no-restore --settings $settingsPath --filter $filter `
        --results-directory $run --report-trx --report-trx-filename live.trx
    if ($LASTEXITCODE -ne 0) { throw 'Live recycle failed; retain owned evidence and any residue.' }
    [xml] $results = Get-Content -LiteralPath (Join-Path $run 'live.trx') -Raw
    $cells = @($results.SelectNodes("//*[local-name()='UnitTestResult']"))
    if ($cells.Count -ne $selected.Count) { throw 'The selected result count differs.' }
    foreach ($name in $selected) {
        $matched = @($cells | Where-Object { $_.testName -ceq $name })
        if ($matched.Count -ne 1 -or $matched[0].outcome -cne 'Passed') {
            throw 'Every selected live recycle cell must pass without a skip.'
        }
    }
    Write-Output "LiveRecycle passed=$($cells.Count) skipped=0 evidence=$run"
}
finally {
    $env:TEMP = $previousTemp
    $env:TMP = $previousTmp
}
