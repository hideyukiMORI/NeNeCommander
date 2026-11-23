[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $OutputDirectory,
    [Parameter(Mandatory)] [string] $WindowsSdkLicensePath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$destination = [System.IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $destination) { throw 'Proof output must be a new dedicated directory.' }
[void] [System.IO.Directory]::CreateDirectory($destination)
. (Join-Path $PSScriptRoot 'release-notices.ps1')
$payload = Join-Path $destination 'licenses'
$packages = @(Copy-ReleaseNotices -AssetsPath (Join-Path $root 'src/NeNeCommander.App/obj/project.assets.json') -Destination $payload -WindowsSdkLicensePath $WindowsSdkLicensePath)
$index = Get-Content -LiteralPath (Join-Path $payload 'index.json') -Raw | ConvertFrom-Json -AsHashtable
$source = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'theme-catalog/source.json') -Raw | ConvertFrom-Json -AsHashtable
if ($index.packages.Count -ne $packages.Count -or $index.paletteNotices.themes -ne 125 -or $index.paletteNotices.files.Count -ne 2) { throw 'Notice inventories differ.' }
foreach ($file in $index.paletteNotices.files) {
    $path = Join-Path $payload $file.path
    if ((Get-Item -LiteralPath $path).Length -ne $file.bytes -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $file.sha256) { throw 'Copied palette inventory mismatch.' }
}
$notice = Join-Path $payload 'themes/NOTICES.txt'
if ((Get-FileHash -LiteralPath $notice -Algorithm SHA256).Hash.ToLowerInvariant() -cne $source.notice.sha256 -or (Get-Item -LiteralPath $notice).Length -ne $source.notice.bytes) { throw 'Notice is not byte-identical to the bounded upstream block.' }
$attribution = Get-Content -LiteralPath (Join-Path $payload 'themes/attribution.json') -Raw | ConvertFrom-Json -AsHashtable
if ($attribution.themes.Count -ne 125) { throw 'Missing palette attributions.' }
for ($i = 0; $i -lt 125; $i++) {
    $actual = $attribution.themes[$i]
    $expected = $source.themes[$i]
    if ($actual.identifier -cne $expected.name -or $actual.appearance -cne $expected.appearance) { throw 'Attribution catalog/order differs.' }
    foreach ($field in @('author', 'license', 'url')) { if ($actual[$field] -cne $expected.source[$field]) { throw 'Attribution field differs.' } }
}
$archive = Join-Path $destination 'notices.zip'
[System.IO.Compression.ZipFile]::CreateFromDirectory($payload, $archive)
$zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
try {
    $files = @(Get-ChildItem -LiteralPath $payload -File -Recurse)
    if ($zip.Entries.Count -ne $files.Count) { throw 'Notice ZIP has extra or missing entries.' }
    foreach ($file in $files) {
        $name = [System.IO.Path]::GetRelativePath($payload, $file.FullName).Replace('\', '/')
        $entry = $zip.GetEntry($name)
        if ($null -eq $entry -or $entry.Length -ne $file.Length) { throw 'Notice ZIP path/length differs.' }
        $stream = $entry.Open()
        try { $hash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($stream)).ToLowerInvariant() }
        finally { $stream.Dispose() }
        if ($hash -cne (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()) { throw 'Notice ZIP bytes differ.' }
    }
}
finally { $zip.Dispose() }

# A bounded copy proves corrupted notice bytes are refused without altering source input.
$fixture = Join-Path $destination 'corrupt-input'
foreach ($directory in @('eng/theme-catalog', 'docs/release/licenses')) { [void] [System.IO.Directory]::CreateDirectory((Join-Path $fixture $directory)) }
foreach ($relative in @('eng/release-notices.ps1', 'eng/theme-catalog/source.json', 'docs/release/licenses/theme-palette-notices.txt')) {
    Copy-Item -LiteralPath (Join-Path $root $relative) -Destination (Join-Path $fixture $relative)
}
[System.IO.File]::AppendAllText((Join-Path $fixture 'docs/release/licenses/theme-palette-notices.txt'), 'corrupt')
. (Join-Path $fixture 'eng/release-notices.ps1')
$rejected = $false
try { Copy-ThemePaletteNotices -Destination (Join-Path $fixture 'output') | Out-Null }
catch { if ($_.Exception.Message -cne 'Fixed palette notice bytes/hash changed.') { throw }; $rejected = $true }
if (-not $rejected -or (Test-Path -LiteralPath (Join-Path $fixture 'output'))) { throw 'Corrupt palette notice was not rejected before output.' }
$result = [ordered]@{ packages = $packages.Count; palettes = 125; zipFiles = $files.Count; noticeBytes = $source.notice.bytes; noticeSha256 = $source.notice.sha256; allZipBytesMatch = $true; corruptedInputRejected = $true }
[System.IO.File]::WriteAllText((Join-Path $destination 'result.json'), ($result | ConvertTo-Json) + "`n")
Write-Host ($result | ConvertTo-Json -Compress)
