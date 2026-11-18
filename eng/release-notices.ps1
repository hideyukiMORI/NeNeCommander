Set-StrictMode -Version Latest

function Copy-WindowsSdkLicense {
    param([Parameter(Mandatory)] [string] $Destination, [string] $CachePath)
    $source = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../docs/release/licenses/windows-sdk-license.json') -Raw | ConvertFrom-Json
    $name = $source.fileName
    $target = Join-Path $Destination $name
    if (-not (Test-Path -LiteralPath $target)) {
        if ($CachePath) {
            if ((Get-FileHash -LiteralPath $CachePath -Algorithm SHA256).Hash.ToLowerInvariant() -cne $source.sha256) {
                throw 'The explicit Windows SDK license cache has the wrong hash.'
            }
            Copy-Item -LiteralPath $CachePath -Destination $target
        }
        else {
            Invoke-WebRequest -Uri $source.url -OutFile $target -MaximumRedirection 0 -TimeoutSec 60
        }
    }
    if ((Get-Item -LiteralPath $target).Length -ne $source.bytes -or
        (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant() -cne $source.sha256) {
        throw 'The Windows SDK license differs from the recorded upstream document.'
    }
    return $name
}

function Copy-ReleaseNotices {
    param(
        [Parameter(Mandatory)] [string] $AssetsPath,
        [Parameter(Mandatory)] [string] $Destination,
        [string] $WindowsSdkLicensePath
    )

    $assets = Get-Content -LiteralPath $AssetsPath -Raw | ConvertFrom-Json -AsHashtable
    $packages = @{}
    foreach ($entry in $assets.libraries.GetEnumerator()) {
        if ($entry.Value.type -eq 'package') { $packages[$entry.Key] = $entry.Value.path }
    }
    foreach ($framework in $assets.project.frameworks.Values) {
        foreach ($download in @($framework.downloadDependencies)) {
            if ($download.version -notmatch '^\[([^,]+),\s*\1\]$') {
                throw "A publish framework pack is not pinned: $($download.name)."
            }
            $identity = "$($download.name)/$($Matches[1])"
            $packages[$identity] = $identity.ToLowerInvariant()
        }
    }

    $records = foreach ($identity in @($packages.Keys | Sort-Object)) {
        if ($identity -notmatch '^[A-Za-z0-9][A-Za-z0-9_.-]*/[0-9][0-9A-Za-z.+-]*$' -or
            $packages[$identity].Replace('\', '/') -cne $identity.ToLowerInvariant()) {
            throw 'A restored package identity is not a relative ID/version pair.'
        }
        $packagePath = $null
        foreach ($cache in $assets.packageFolders.Keys) {
            $candidate = [System.IO.Path]::GetFullPath((Join-Path $cache $packages[$identity]))
            if (Test-Path -LiteralPath $candidate -PathType Container) { $packagePath = $candidate; break }
        }
        if ($null -eq $packagePath) { throw "Restored package is absent: $identity." }
        $specs = @(Get-ChildItem -LiteralPath $packagePath -Filter '*.nuspec' -File)
        if ($specs.Count -ne 1) { throw "Expected one package manifest: $identity." }
        [xml] $spec = Get-Content -LiteralPath $specs[0].FullName -Raw
        $metadata = $spec.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]')
        $license = $metadata.SelectSingleNode('*[local-name()="license"]')
        $licenseUrl = $metadata.SelectSingleNode('*[local-name()="licenseUrl"]')
        $files = @(Get-ChildItem -LiteralPath $packagePath -File | Where-Object {
            $_.Name -match '^(license|licence|notice|third[-_]?party[-_]?notices|copyright)(\.|$)'
        })
        if ($null -ne $license -and $license.GetAttribute('type') -eq 'file') {
            $licensePath = [System.IO.Path]::GetFullPath((Join-Path $packagePath $license.InnerText))
            if (-not $licensePath.StartsWith($packagePath + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
                throw "License path leaves its package: $identity."
            }
            $files += Get-Item -LiteralPath $licensePath
        }
        $files = @($files + $specs | Sort-Object FullName -Unique)
        $packageDestination = Join-Path $Destination $identity
        foreach ($file in $files) {
            if (($file.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "License input is a link: $identity."
            }
            $relative = [System.IO.Path]::GetRelativePath($packagePath, $file.FullName)
            $target = Join-Path $packageDestination $relative
            [void] [System.IO.Directory]::CreateDirectory((Split-Path -Parent $target))
            Copy-Item -LiteralPath $file.FullName -Destination $target
        }
        $supplementalLicense = $null
        if ($null -ne $licenseUrl -and $licenseUrl.InnerText -eq 'https://aka.ms/WinSDKLicenseURL') {
            $supplementalLicense = Copy-WindowsSdkLicense -Destination $Destination -CachePath $WindowsSdkLicensePath
        }
        [ordered]@{
            package = $identity
            license = $(if ($null -ne $license) { $license.InnerText } else { $null })
            licenseType = $(if ($null -ne $license) { $license.GetAttribute('type') } else { $null })
            licenseUrl = $(if ($null -ne $licenseUrl) { $licenseUrl.InnerText } else { $null })
            supplementalLicense = $supplementalLicense
            files = @($files | ForEach-Object { [System.IO.Path]::GetRelativePath($packagePath, $_.FullName).Replace('\', '/') })
        }
    }
    $index = [ordered]@{
        schemaVersion = 1
        scope = 'Resolved publish packages and framework packs, including build-time dependencies.'
        packages = @($records)
    }
    [System.IO.File]::WriteAllText((Join-Path $Destination 'index.json'), ($index | ConvertTo-Json -Depth 8) + "`n")
    return @($records)
}
