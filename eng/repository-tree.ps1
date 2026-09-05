Set-StrictMode -Version Latest

$script:RepositoryTreeExcludedDirectoryNames = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
foreach ($excludedDirectoryName in @('.git', '.vs', 'artifacts', 'bin', 'obj', 'TestResults', 'Generated Files')) {
    [void] $script:RepositoryTreeExcludedDirectoryNames.Add($excludedDirectoryName)
}

function Get-RepositoryTreeFile {
    param(
        [Parameter(Mandatory)]
        [string] $RepositoryRoot,

        [Parameter()]
        [string[]] $Roots = @('.')
    )

    $resolvedRepositoryRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
    $repositoryPrefix = $resolvedRepositoryRoot.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $files = [System.Collections.Generic.List[System.IO.FileInfo]]::new()

    foreach ($relativeRoot in $Roots) {
        $searchRoot = [System.IO.Path]::GetFullPath((Join-Path $resolvedRepositoryRoot $relativeRoot))
        if ($searchRoot -cne $resolvedRepositoryRoot -and
            -not $searchRoot.StartsWith($repositoryPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Repository tree root escaped the repository: $relativeRoot"
        }
        if (-not (Test-Path -LiteralPath $searchRoot -PathType Container)) {
            continue
        }

        $pending = [System.Collections.Generic.Stack[System.IO.DirectoryInfo]]::new()
        $pending.Push([System.IO.DirectoryInfo]::new($searchRoot))
        while ($pending.Count -gt 0) {
            $directory = $pending.Pop()
            foreach ($item in Get-ChildItem -LiteralPath $directory.FullName -Force) {
                if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                    continue
                }
                if ($item.PSIsContainer) {
                    if (-not $script:RepositoryTreeExcludedDirectoryNames.Contains($item.Name)) {
                        $pending.Push($item)
                    }
                    continue
                }
                $files.Add($item)
            }
        }
    }

    return $files
}

function Copy-ProofFoundation {
    param(
        [Parameter(Mandatory)]
        [string] $RepositoryRoot,

        [Parameter(Mandatory)]
        [string] $Destination
    )

    $resolvedRepositoryRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
    $resolvedDestination = [System.IO.Path]::GetFullPath($Destination)
    New-Item -ItemType Directory -Path $resolvedDestination | Out-Null

    foreach ($file in (Get-RepositoryTreeFile -RepositoryRoot $resolvedRepositoryRoot)) {
        $relativePath = [System.IO.Path]::GetRelativePath($resolvedRepositoryRoot, $file.FullName)
        $destinationPath = Join-Path $resolvedDestination $relativePath
        $destinationParent = Split-Path -Parent $destinationPath
        if (-not (Test-Path -LiteralPath $destinationParent -PathType Container)) {
            New-Item -ItemType Directory -Path $destinationParent -Force | Out-Null
        }
        Copy-Item -LiteralPath $file.FullName -Destination $destinationPath
    }
}
