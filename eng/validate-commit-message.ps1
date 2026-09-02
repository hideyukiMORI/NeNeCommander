[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $MessageFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $MessageFile -PathType Leaf)) {
    throw 'Commit message file does not exist.'
}

$subject = Get-Content -LiteralPath $MessageFile -TotalCount 1
if ($null -eq $subject) {
    throw 'Commit subject is missing.'
}

$pattern = '^(?:feat|fix|docs|refactor|test|build|ci|chore)(?:\([a-z0-9][a-z0-9.-]*\))?!?: (?<description>.+) \(#[1-9][0-9]*\)$'
$match = [regex]::Match($subject, $pattern)
if (-not $match.Success) {
    throw 'Commit subject must follow Conventional Commits and end with an Issue number.'
}

if ($subject.Length -gt 100) {
    throw 'Commit subject must contain at most 100 characters.'
}

if ($match.Groups['description'].Value -notmatch '[\u3040-\u30ff\u3400-\u9fff]') {
    throw 'Commit description must contain Japanese text.'
}

Write-Host 'Commit message convention passed.'
